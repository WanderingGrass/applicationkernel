// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Diagnostics.CodeAnalysis;
using Todd.ApplicationKernel.Serialization.Cloning;

namespace Todd.ApplicationKernel.Serialization.Serializers;
/// <summary>
/// Surrogate serializer for <typeparamref name="TField"/>.
/// </summary>
/// <typeparam name="TField">The type which the implementation of this class supports.</typeparam>
/// <typeparam name="TSurrogate">The surrogate type serialized in place of <typeparamref name="TField"/>.</typeparam>
/// <typeparam name="TConverter">The converter type which converts between <typeparamref name="TField"/> and <typeparamref name="TSurrogate"/>.</typeparam>
public sealed class SurrogateCodec<TField, TSurrogate, TConverter>
    : IDeepCopier<TField>,  IBaseCopier<TField>
    where TField : class
    where TSurrogate : struct
    where TConverter : IConverter<TField, TSurrogate>
{
    private readonly Type _fieldType = typeof(TField);
    private readonly IDeepCopier<TSurrogate> _surrogateCopier;
    private readonly IPopulator<TField, TSurrogate>? _populator;
    private readonly TConverter _converter;

    /// <summary>
    /// Initializes a new instance of the <see cref="SurrogateCodec{TField, TSurrogate, TConverter}"/> class.
    /// </summary>
    /// <param name="surrogateSerializer">The surrogate serializer.</param>
    /// <param name="surrogateCopier">The surrogate copier.</param>
    /// <param name="converter">The surrogate converter.</param>
    public SurrogateCodec(
        IDeepCopier<TSurrogate> surrogateCopier,
        TConverter converter)
    {
        _surrogateCopier = surrogateCopier;
        _converter = converter;
        _populator = converter as IPopulator<TField, TSurrogate>;
    }

    /// <inheritdoc/>
    public TField DeepCopy(TField input, CopyContext context)
    {
        if (context.TryGetCopy<TField>(input, out var result))
        {
            return result;
        }

        var surrogate = _converter.ConvertToSurrogate(in input);
        var copy = _surrogateCopier.DeepCopy(surrogate, context);
        result = _converter.ConvertFromSurrogate(in copy);

        context.RecordCopy(input, result);
        return result;
    }

    /// <inheritdoc/>
    public void DeepCopy(TField input, TField output, CopyContext context)
    {
        if (_populator is null) ThrowNoPopulatorException();

        var surrogate = _converter.ConvertToSurrogate(in input);
        var copy = _surrogateCopier.DeepCopy(surrogate, context);
        _populator.Populate(copy, output);
    }

    [DoesNotReturn]
    private void ThrowNoPopulatorException() => throw new NotSupportedException($"Surrogate type {typeof(TConverter)} does not implement {typeof(IPopulator<TField, TSurrogate>)} and therefore cannot be used in an inheritance hierarchy.");
}
