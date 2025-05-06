// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Todd.ApplicationKernel.Serialization
{
    /// <summary>
    /// Base exception for any serializer exception.
    /// </summary>
    [Serializable]
    [GenerateSerializer]
    public class SerializerException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SerializerException"/> class.
        /// </summary>
        public SerializerException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializerException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public SerializerException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializerException"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (<see langword="Nothing" /> in Visual Basic) if no inner exception is specified.</param>
        public SerializerException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializerException"/> class.
        /// </summary>
        /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo" /> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="T:System.Runtime.Serialization.StreamingContext" /> that contains contextual information about the source or destination.</param>
#if NET8_0_OR_GREATER
        [Obsolete]
#endif
        protected SerializerException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
    /// <summary>
    /// No suitable serializer codec was found for a specified type.
    /// </summary>
    [Serializable]
    [GenerateSerializer]
    public sealed class CodecNotFoundException : SerializerException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CodecNotFoundException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public CodecNotFoundException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CodecNotFoundException"/> class.
        /// </summary>
        /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo" /> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="T:System.Runtime.Serialization.StreamingContext" /> that contains contextual information about the source or destination.</param>
#if NET8_0_OR_GREATER
        [Obsolete]
#endif
        private CodecNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
