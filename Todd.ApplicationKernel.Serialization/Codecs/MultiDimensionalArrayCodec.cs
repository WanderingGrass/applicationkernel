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
    /// Copier for multi-dimensional arrays.
    /// </summary>
    /// <typeparam name="T">The array element type.</typeparam>
    internal sealed class MultiDimensionalArrayCopier<T> : IGeneralizedCopier
    {
        /// <inheritdoc/>
        public object DeepCopy(object original, CopyContext context)
        {
            if (context.TryGetCopy<Array>(original, out var result))
            {
                return result;
            }

            var type = original.GetType();
            var originalArray = (Array)original;
            var elementType = type.GetElementType();
            if (ShallowCopyableTypes.Contains(elementType))
            {
                return originalArray.Clone();
            }

            // We assume that all arrays have lower bound 0. In .NET 4.0, it's hard to create an array with a non-zero lower bound.
            var rank = originalArray.Rank;
            var lengths = new int[rank];
            for (var i = 0; i < rank; i++)
            {
                lengths[i] = originalArray.GetLength(i);
            }

            result = Array.CreateInstance(elementType, lengths);
            context.RecordCopy(original, result);

            if (rank == 1)
            {
                for (var i = 0; i < lengths[0]; i++)
                {
                    result.SetValue(ObjectCopier.DeepCopy(originalArray.GetValue(i), context), i);
                }
            }
            else if (rank == 2)
            {
                for (var i = 0; i < lengths[0]; i++)
                {
                    for (var j = 0; j < lengths[1]; j++)
                    {
                        result.SetValue(ObjectCopier.DeepCopy(originalArray.GetValue(i, j), context), i, j);
                    }
                }
            }
            else
            {
                var index = new int[rank];
                var sizes = new int[rank];
                sizes[rank - 1] = 1;
                for (var k = rank - 2; k >= 0; k--)
                {
                    sizes[k] = sizes[k + 1] * lengths[k + 1];
                }

                for (var i = 0; i < originalArray.Length; i++)
                {
                    int k = i;
                    for (int n = 0; n < rank; n++)
                    {
                        int offset = k / sizes[n];
                        k -= offset * sizes[n];
                        index[n] = offset;
                    }

                    result.SetValue(ObjectCopier.DeepCopy(originalArray.GetValue(index), context), index);
                }
            }

            return result;
        }

        public bool IsSupportedType(Type type) => type.IsArray && !type.IsSZArray;
    }
}
