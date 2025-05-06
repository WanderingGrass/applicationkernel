// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Todd.ApplicationKernel.Serialization.Cloning;

namespace Todd.ApplicationKernel.Serialization.Codecs
{
    /// <summary>
    /// Copier for <see cref="object"/>.
    /// </summary>
    [RegisterCopier]
    public sealed class ObjectCopier : IDeepCopier<object>
    {
        /// <summary>
        /// Creates a deep copy of the provided input.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="context">The context.</param>
        /// <returns>A copy of <paramref name="input" />.</returns>
        public static object DeepCopy(object input, CopyContext context)
        {
            return context.TryGetCopy<object>(input, out var result) ? result
                : input.GetType() == typeof(object) ? input : context.DeepCopy(input);
        }

        object IDeepCopier<object>.DeepCopy(object input, CopyContext context)
        {
            return context.TryGetCopy<object>(input, out var result) ? result
                : input.GetType() == typeof(object) ? input : context.DeepCopy(input);
        }

        object IDeepCopier.DeepCopy(object input, CopyContext context)
            => input is null || input.GetType() == typeof(object) ? input : context.DeepCopy(input);
    }
}
