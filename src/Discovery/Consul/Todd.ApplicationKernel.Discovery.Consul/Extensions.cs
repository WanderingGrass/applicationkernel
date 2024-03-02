// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Todd.ApplicationKernel.Base;
using Todd.ApplicationKernel.Discovery.Consul.Options;

namespace Todd.ApplicationKernel.Discovery.Consul
{
    public static class Extensions
    {
        public static IApplicationKernelBuilder AddConsul(this IApplicationKernelBuilder builder,
            Action<ConsulClusteringOptions> configureOptions)
        {
            if (configureOptions != null)
            {
                builder.Configure(configureOptions);
            }
            return builder;
        }
    }
}
