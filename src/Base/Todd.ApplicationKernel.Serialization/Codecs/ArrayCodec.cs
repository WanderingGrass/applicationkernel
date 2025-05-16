// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Todd.ApplicationKernel.Serialization.Cloning;
using Todd.ApplicationKernel.Serialization.GeneratedCodeHelpers;

namespace Todd.ApplicationKernel.Serialization.Codecs
{
    /// <summary>
    /// Copier for arrays of rank 1.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    [RegisterCopier]
    public sealed class ArrayCopier<T> : IDeepCopier<T[]>
    {
        private readonly IDeepCopier<T> _elementCopier;

        /// <summary>
        /// Initializes a new instance of the <see cref="ArrayCopier{T}"/> class.
        /// </summary>
        /// <param name="elementCopier">The element copier.</param>
        public ArrayCopier(IDeepCopier<T> elementCopier)
        {
            _elementCopier = GeneratedCodeHelper.UnwrapService(this, elementCopier);
        }

        /// <inheritdoc/>
        public T[] DeepCopy(T[] input, CopyContext context)
        {
            if (context.TryGetCopy<T[]>(input, out var result))
            {
                return result;
            }

            result = new T[input.Length];
            context.RecordCopy(input, result);
            for (var i = 0; i < input.Length; i++)
            {
                result[i] = _elementCopier.DeepCopy(input[i], context);
            }

            return result;
        }
    }
}
