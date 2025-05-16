using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Reflection;
using Microsoft.Extensions.ObjectPool;
using Todd.ApplicationKernel.Serialization.Invocation;
using Todd.ApplicationKernel.Serialization.Serializers;
using Todd.ApplicationKernel.Serialization.Utilities;

namespace Todd.ApplicationKernel.Serialization.Cloning
{
    public interface IDeepCopierProvider
    {
        IDeepCopier<T> GetDeepCopier<T>();

        IDeepCopier<T> TryGetDeepCopier<T>();

        IDeepCopier GetDeepCopier(Type type);

        IDeepCopier TryGetDeepCopier(Type type);

        IBaseCopier<T> GetBaseCopier<T>() where T : class;

    }
    public interface IDeepCopier
    {
        object DeepCopy(object input, CopyContext context);
    }
    public interface IDeepCopier<T> : IDeepCopier
    {
        /// <summary>
        /// Creates a deep copy of the provided input.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="context">The context.</param>
        /// <returns>A copy of <paramref name="input"/>.</returns>
        T DeepCopy(T input, CopyContext context);

        object IDeepCopier.DeepCopy(object input, CopyContext context) => DeepCopy((T)input, context);
    }
    public interface IOptionalDeepCopier : IDeepCopier
    {
        bool IsShallowCopyable();
    }
    internal sealed class ShallowCopier : IOptionalDeepCopier
    {
        public static readonly ShallowCopier Instance = new();

        public bool IsShallowCopyable() => true;
        public object DeepCopy(object input, CopyContext _) => input;
    }
    public class ShallowCopier<T> : IOptionalDeepCopier, IDeepCopier<T>
    {
        public bool IsShallowCopyable() => true;

        /// <summary>Returns the input value.</summary>
        public T DeepCopy(T input, CopyContext _) => input;

        /// <summary>Returns the input value.</summary>
        public object DeepCopy(object input, CopyContext _) => input;
    }
    public interface IBaseCopier
    {
    }
    public interface IBaseCopier<T> : IBaseCopier where T : class
    {
        /// <summary>
        /// Clones members from <paramref name="input"/> and copies them to <paramref name="output"/>.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="output">The output.</param>
        /// <param name="context">The context.</param>
        void DeepCopy(T input, T output, CopyContext context);
    }
    public interface IDerivedTypeCopier : IDeepCopier
    {
    }
    public interface IGeneralizedCopier : IDeepCopier
    {
        /// <summary>
        /// Returns a value indicating whether the provided type is supported by this implementation.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns><see langword="true"/> if the type is supported type by this implementation; otherwise, <see langword="false"/>.</returns>
        bool IsSupportedType(Type type);
    }

    public interface ISpecializableCopier
    {
        /// <summary>
        /// Returns a value indicating whether the provided type is supported by this implementation.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns><see langword="true"/> if the type is supported type by this implementation; otherwise, <see langword="false"/>.</returns>
        bool IsSupportedType(Type type);

        /// <summary>
        /// Gets an <see cref="IDeepCopier"/> implementation which supports the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>An <see cref="IDeepCopier"/> implementation which supports the specified type.</returns>
        IDeepCopier GetSpecializedCopier(Type type);
    }

    public sealed class CopyContext : IDisposable
    {
        private readonly Dictionary<object, object> _copies = new(ReferenceEqualsComparer.Default);
        private readonly CodecProvider _copierProvider;
        private readonly Action<CopyContext> _onDisposed;
        public CopyContext(CodecProvider codecProvider, Action<CopyContext> onDisposed)
        {
            _copierProvider = codecProvider;
            _onDisposed = onDisposed;
        }
        /// <summary>
        /// 返回所提供对象的先前记录副本（如果存在）。防止对同一对象的多次调用和循环引用
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="original">原始对象</param>
        /// <param name="result">存储对象</param>
        /// <returns></returns>
        public bool TryGetCopy<T>(object original, [NotNullWhen(true)] out T result) where T : class
        {
            if (original is null)
            {
                result = null;
                return true;
            }

            if (_copies.TryGetValue(original, out var existing))
            {
                result = existing as T;
                return true;
            }

            result = null;
            return false;
        }
        public void RecordCopy(object original, object copy)
        {
            _copies[original] = copy;
        }
        public void Reset() => _copies.Clear();
        public T DeepCopy<T>(T value)
        {
            if (!typeof(T).IsValueType)
            {
                if (value is null) return default;
            }

            var copier = _copierProvider.GetDeepCopier(value.GetType());
            return (T)copier.DeepCopy(value, this);
        }

        public void Dispose() => _onDisposed?.Invoke(this);
    }

    internal static class ShallowCopyableTypes
    {
        private static readonly ConcurrentDictionary<Type, bool> Types = new()
        {
            [typeof(decimal)] = true,
            [typeof(DateTime)] = true,
#if NET6_0_OR_GREATER
            [typeof(DateOnly)] = true,
            [typeof(TimeOnly)] = true,
#endif
            [typeof(DateTimeOffset)] = true,
            [typeof(TimeSpan)] = true,
            [typeof(IPAddress)] = true,
            [typeof(IPEndPoint)] = true,
            [typeof(string)] = true,
            [typeof(CancellationToken)] = true,
            [typeof(Guid)] = true,
            [typeof(BitVector32)] = true,
            [typeof(CompareInfo)] = true,
            [typeof(CultureInfo)] = true,
            [typeof(Version)] = true,
            [typeof(Uri)] = true,
#if NET7_0_OR_GREATER
            [typeof(UInt128)] = true,
            [typeof(Int128)] = true,
#endif
#if NET5_0_OR_GREATER
            [typeof(Half)] = true,
#endif
        };

        public static bool Contains(Type type)
        {
            if (Types.TryGetValue(type, out var result))
            {
                return result;
            }

            return Types.GetOrAdd(type, IsShallowCopyableInternal(type));
        }

        private static bool IsShallowCopyableInternal(Type type)
        {
            if (type.IsPrimitive || type.IsEnum)
            {
                return true;
            }

            if (type.IsSealed && type.IsDefined(typeof(ImmutableAttribute), false))
            {
                return true;
            }

            if (type.IsConstructedGenericType)
            {
                var def = type.GetGenericTypeDefinition();

                if (def == typeof(Nullable<>)
                    || def == typeof(Tuple<>)
                    || def == typeof(Tuple<,>)
                    || def == typeof(Tuple<,,>)
                    || def == typeof(Tuple<,,,>)
                    || def == typeof(Tuple<,,,,>)
                    || def == typeof(Tuple<,,,,,>)
                    || def == typeof(Tuple<,,,,,,>)
                    || def == typeof(Tuple<,,,,,,,>))
                {
                    return Array.TrueForAll(type.GenericTypeArguments, a => Contains(a));
                }
            }

            if (type.IsValueType && !type.IsGenericTypeDefinition)
            {
                return Array.TrueForAll(type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic), f => Contains(f.FieldType));
            }

            if (typeof(Exception).IsAssignableFrom(type))
                return true;

            if (typeof(Type).IsAssignableFrom(type))
                return true;

            return false;
        }
    }

    /// <summary>
    /// Converts an untyped copier into a strongly-typed copier.
    /// </summary>
    internal sealed class UntypedCopierWrapper<T> : IDeepCopier<T>
    {
        private readonly IDeepCopier _copier;

        public UntypedCopierWrapper(IDeepCopier copier) => _copier = copier;

        public T DeepCopy(T original, CopyContext context) => (T)_copier.DeepCopy(original, context);

        public object DeepCopy(object original, CopyContext context) => _copier.DeepCopy(original, context);
    }

    public sealed class CopyContextPool
    {
        private readonly ConcurrentObjectPool<CopyContext, PoolPolicy> _pool;

        /// <summary>
        /// Initializes a new instance of the <see cref="CopyContextPool"/> class.
        /// </summary>
        /// <param name="codecProvider">The codec provider.</param>
        public CopyContextPool(CodecProvider codecProvider)
        {
            var sessionPoolPolicy = new PoolPolicy(codecProvider, Return);
            _pool = new ConcurrentObjectPool<CopyContext, PoolPolicy>(sessionPoolPolicy);
        }

        /// <summary>
        /// Gets a <see cref="CopyContext"/>.
        /// </summary>
        /// <returns>A <see cref="CopyContext"/>.</returns>
        public CopyContext GetContext() => _pool.Get();

        /// <summary>
        /// Returns the specified copy context to the pool.
        /// </summary>
        /// <param name="context">The context.</param>
        private void Return(CopyContext context) => _pool.Return(context);

        private readonly struct PoolPolicy : IPooledObjectPolicy<CopyContext>
        {
            private readonly CodecProvider _codecProvider;
            private readonly Action<CopyContext> _onDisposed;

            public PoolPolicy(CodecProvider codecProvider, Action<CopyContext> onDisposed)
            {
                _codecProvider = codecProvider;
                _onDisposed = onDisposed;
            }

            public CopyContext Create() => new(_codecProvider, _onDisposed);

            public bool Return(CopyContext obj)
            {
                obj.Reset();
                return true;
            }
        }
    }
}
