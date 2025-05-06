// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Todd.ApplicationKernel.Serialization
{
    public static class ServiceCollectionExtensions
    {
     
    }
    /// <summary>
    /// Holds a reference to a service.
    /// </summary>
    /// <typeparam name="T">The service type.</typeparam>
    internal interface IServiceHolder<T>
    {
        /// <summary>
        /// Gets the service.
        /// </summary>
        /// <value>The service.</value>
        T Value { get; }
    }
}
