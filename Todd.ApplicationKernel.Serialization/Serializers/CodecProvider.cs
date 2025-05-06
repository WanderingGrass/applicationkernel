// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Todd.ApplicationKernel.Serialization.Cloning;
using Todd.ApplicationKernel.Serialization.Codecs;
using Todd.ApplicationKernel.Serialization.GeneratedCodeHelpers;

namespace Todd.ApplicationKernel.Serialization.Serializers
{
    public sealed class CodecProvider: ICodecProvider
    {
        private static readonly Type ObjectType = typeof(object);
        private readonly object _initializationLock = new();

        private readonly ConcurrentDictionary<Type, IDeepCopier> _untypedCopiers = new();
        private readonly ConcurrentDictionary<Type, IDeepCopier> _typedCopiers = new();

        private readonly Dictionary<Type, Type> _baseCodecs = new();
        private readonly Dictionary<Type, Type> _valueSerializers = new();
        private readonly Dictionary<Type, Type> _fieldCodecs = new();
        private readonly Dictionary<Type, Type> _copiers = new();
        private readonly Dictionary<Type, Type> _converters = new();
        private readonly Dictionary<Type, Type> _baseCopiers = new();
        private readonly Dictionary<Type, Type> _activators = new();


        private readonly List<IGeneralizedCopier> _generalizedCopiers = new();
        private readonly List<ISpecializableCopier> _specializableCopiers = new();
        private readonly IServiceProvider _serviceProvider;


        private readonly VoidCopier _voidCopier = new();
        private readonly ObjectCopier _objectCopier = new();
        private bool _initialized;

        public IServiceProvider Services => _serviceProvider;

        private void Initialize()
        {
            lock (_initializationLock)
            {
                if (_initialized)
                {
                    return;
                }


                _generalizedCopiers.AddRange(_serviceProvider.GetServices<IGeneralizedCopier>());
      ;
                _specializableCopiers.AddRange(_serviceProvider.GetServices<ISpecializableCopier>());
         

                _initialized = true;
            }
        }

        public IDeepCopier<T> GetDeepCopier<T>()
        {
            var res = TryGetDeepCopier<T>();
            if (res is null) ThrowCopierNotFound(typeof(T));
            return res;
        }

        /// <inheritdoc/>
        public IDeepCopier<T> TryGetDeepCopier<T>()
        {
            var type = typeof(T);
            if (_typedCopiers.TryGetValue(type, out var existing))
                return (IDeepCopier<T>)existing;

            if (TryGetDeepCopier(type) is not { } untypedResult)
                return null;

            var typedResult = untypedResult switch
            {
                IDeepCopier<T> typed => typed,
                IOptionalDeepCopier optional when optional.IsShallowCopyable() => new ShallowCopier<T>(),
                _ => new UntypedCopierWrapper<T>(untypedResult)
            };

            return (IDeepCopier<T>)_typedCopiers.GetOrAdd(type, typedResult);
        }

        /// <inheritdoc/>
        public IDeepCopier GetDeepCopier(Type fieldType)
        {
            var res = TryGetDeepCopier(fieldType);
            if (res is null) ThrowCopierNotFound(fieldType);
            return res;
        }

        /// <inheritdoc/>
        public IDeepCopier TryGetDeepCopier(Type fieldType)
        {
            // If the field type is unavailable, return the void copier which can at least handle references.
            return fieldType is null ? _voidCopier
                : _untypedCopiers.TryGetValue(fieldType, out var existing) ? existing
                : TryCreateCopier(fieldType) is { } res ? _untypedCopiers.GetOrAdd(fieldType, res)
                : null;
        }
        private IDeepCopier TryCreateCopier(Type fieldType)
        {
            if (!_initialized) Initialize();

            ThrowIfUnsupportedType(fieldType);

            if (CreateCopierInstance(fieldType, fieldType.IsConstructedGenericType ? fieldType.GetGenericTypeDefinition() : fieldType) is { } res)
                return res;

            foreach (var specializableCopier in _specializableCopiers)
            {
                if (specializableCopier.IsSupportedType(fieldType))
                    return specializableCopier.GetSpecializedCopier(fieldType);
            }

            foreach (var dynamicCopier in _generalizedCopiers)
            {
                if (dynamicCopier.IsSupportedType(fieldType))
                    return dynamicCopier;
            }

            return fieldType.IsInterface || fieldType.IsAbstract ? _objectCopier : null;
        }


        private IDeepCopier CreateCopierInstance(Type fieldType, Type searchType)
        {
            if (searchType == ObjectType)
                return _objectCopier;

            object[] constructorArguments = null;
            if (_copiers.TryGetValue(searchType, out var copierType))
            {
                if (copierType.IsGenericTypeDefinition)
                {
                    copierType = copierType.MakeGenericType(fieldType.GetGenericArguments());
                }
            }
            else if (ShallowCopyableTypes.Contains(fieldType))
            {
                return ShallowCopier.Instance;
            }
            else if (fieldType.IsArray)
            {
                // Depending on the type of the array, select the base array copier or the multi-dimensional copier.
                var arrayCopierType = fieldType.IsSZArray ? typeof(ArrayCopier<>) : typeof(MultiDimensionalArrayCopier<>);
                copierType = arrayCopierType.MakeGenericType(fieldType.GetElementType());
            }
            else if (TryGetSurrogateCodec(fieldType, searchType, out var surrogateCodecType, out constructorArguments))
            {
                copierType = surrogateCodecType;
            }
            else if (searchType.BaseType is { } baseType)
            {
                // Find copiers which generalize over all subtypes.
                if (CreateCopierInstance(fieldType, baseType) is IDerivedTypeCopier baseCopier)
                {
                    return baseCopier;
                }
                else if (baseType.IsGenericType
                    && baseType.IsConstructedGenericType
                    && CreateCopierInstance(fieldType, baseType.GetGenericTypeDefinition()) is IDerivedTypeCopier genericBaseCopier)
                {
                    return genericBaseCopier;
                }
            }

            return copierType != null ? (IDeepCopier)GetServiceOrCreateInstance(copierType, constructorArguments) : null;
        }
        private object GetServiceOrCreateInstance(Type type, object[] constructorArguments = null)
        {
            var result = GeneratedCodeHelper.TryGetService(type);
            if (result != null)
            {
                return result;
            }

            result = _serviceProvider.GetService(type);
            if (result != null)
            {
                return result;
            }

            result = ActivatorUtilities.CreateInstance(_serviceProvider, type, constructorArguments ?? Array.Empty<object>());
            return result;
        }
        private bool TryGetSurrogateCodec(Type fieldType, Type searchType, out Type surrogateCodecType, out object[] constructorArguments)
        {
            if (_converters.TryGetValue(searchType, out var converterType))
            {
                if (converterType.IsGenericTypeDefinition)
                {
                    converterType = converterType.MakeGenericType(fieldType.GetGenericArguments());
                }

                var converterInterfaceArgs = Array.Empty<Type>();
                foreach (var @interface in converterType.GetInterfaces())
                {
                    if (@interface.IsConstructedGenericType && @interface.GetGenericTypeDefinition() == typeof(IConverter<,>)
                        && @interface.GenericTypeArguments[0] == fieldType)
                    {
                        converterInterfaceArgs = @interface.GetGenericArguments();
                    }
                }

                if (converterInterfaceArgs is { Length: 0 })
                {
                    throw new InvalidOperationException($"A registered type converter {converterType} does not implement {typeof(IConverter<,>)}");
                }

                var typeArgs = new Type[3] { converterInterfaceArgs[0], converterInterfaceArgs[1], converterType };
                constructorArguments = new object[] { GetServiceOrCreateInstance(converterType) };
                if (typeArgs[0].IsValueType)
                {
                    surrogateCodecType = typeof(ValueTypeSurrogateCodec<,,>).MakeGenericType(typeArgs);
                }
                else
                {
                    surrogateCodecType = typeof(SurrogateCodec<,,>).MakeGenericType(typeArgs);
                }

                return true;
            }

            surrogateCodecType = null;
            constructorArguments = null;
            return false;
        }

        private static void ThrowIfUnsupportedType(Type fieldType)
        {
            if (fieldType.IsGenericTypeDefinition)
            {
                ThrowGenericTypeDefinition(fieldType);
            }

            if (fieldType.IsPointer)
            {
                ThrowPointerType(fieldType);
            }

            if (fieldType.IsByRef)
            {
                ThrowByRefType(fieldType);
            }
        }

        private static void ThrowPointerType(Type fieldType) => throw new NotSupportedException($"Type {fieldType} is a pointer type and is therefore not supported.");

        private static void ThrowByRefType(Type fieldType) => throw new NotSupportedException($"Type {fieldType} is a by-ref type and is therefore not supported.");

        private static void ThrowGenericTypeDefinition(Type fieldType) => throw new InvalidOperationException($"Type {fieldType} is a non-constructed generic type and is therefore unsupported.");

        private static void ThrowCodecNotFound(Type fieldType) => throw new CodecNotFoundException($"Could not find a codec for type {fieldType}.");

        private static void ThrowCopierNotFound(Type type) => throw new CodecNotFoundException($"Could not find a copier for type {type}.");

        private static void ThrowBaseCodecNotFound(Type fieldType) => throw new KeyNotFoundException($"Could not find a base type serializer for type {fieldType}.");

        private static void ThrowValueSerializerNotFound(Type fieldType) => throw new KeyNotFoundException($"Could not find a value serializer for type {fieldType}.");

        private static void ThrowActivatorNotFound(Type type) => throw new KeyNotFoundException($"Could not find an activator for type {type}.");

        private static void ThrowBaseCopierNotFound(Type type) => throw new KeyNotFoundException($"Could not find a base type copier for type {type}.");

        public IBaseCopier<T> GetBaseCopier<T>() where T : class
        {
            throw new NotImplementedException();
        }
    }
}
