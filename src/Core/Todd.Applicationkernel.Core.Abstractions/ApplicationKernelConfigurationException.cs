// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Todd.Applicationkernel.Core.Abstractions
{
    /// <summary>
    /// Represents a configuration exception.
    /// </summary>
    [Serializable]
    public sealed class ApplicationKernelConfigurationException : Exception
    {
        /// <inheritdoc />
        public ApplicationKernelConfigurationException(string message)
            : base(message)
        {
        }

        /// <inheritdoc />
        public ApplicationKernelConfigurationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <inheritdoc />
        /// <exception cref="SerializationException">The class name is <see langword="null" /> or <see cref="P:System.Exception.HResult" /> is zero (0).</exception>
        /// <exception cref="ArgumentNullException"><paramref name="info" /> is <see langword="null" />.</exception>
        [Obsolete]
        private ApplicationKernelConfigurationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
