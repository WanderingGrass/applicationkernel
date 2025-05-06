// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Todd.ApplicationKernel.Serialization.Cloning;

namespace Todd.ApplicationKernel.Serialization.Serializers;

/// <summary>
/// Surrogate serializer for <typeparamref name="TField"/>.
/// </summary>
/// <typeparam name="TField">The type which the implementation of this class supports.</typeparam>
/// <typeparam name="TSurrogate">The surrogate type serialized in place of <typeparamref name="TField"/>.</typeparam>
/// <typeparam name="TConverter">The converter type which converts between <typeparamref name="TField"/> and <typeparamref name="TSurrogate"/>.</typeparam>
public sealed class ValueTypeSurrogateCodec<TField, TSurrogate, TConverter>
    :IDeepCopier<TField>
    where TField : struct
    where TSurrogate : struct
    where TConverter : IConverter<TField, TSurrogate>
{
    private readonly IDeepCopier<TSurrogate> _surrogateCopier;
    private readonly TConverter _converter;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueTypeSurrogateCodec{TField, TSurrogate, TConverter}"/> class.
    /// </summary>
    /// <param name="surrogateSerializer">The surrogate serializer.</param>
    /// <param name="surrogateCopier">The surrogate copier.</param>
    /// <param name="converter">The surrogate converter.</param>
    public ValueTypeSurrogateCodec(
        IDeepCopier<TSurrogate> surrogateCopier,
        TConverter converter)
    {
        _surrogateCopier = surrogateCopier;
        _converter = converter;
    }

    /// <inheritdoc/>
    public TField DeepCopy(TField input, CopyContext context)
    {
        var surrogate = _converter.ConvertToSurrogate(in input);
        var copy = _surrogateCopier.DeepCopy(surrogate, context);
        var result = _converter.ConvertFromSurrogate(in copy);

        return result;
    }
 
}
