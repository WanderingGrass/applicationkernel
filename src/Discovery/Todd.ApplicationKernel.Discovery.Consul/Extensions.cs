// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Todd.Applicationkernel.Core.Discovery;
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
                builder.Services.AddSingleton<IServiceDiscoveryProvider, ConsulServiceDiscoveryProvider>();
            }
            return builder;
        }
        public static IApplicationKernelBuilder AddConsul(this IApplicationKernelBuilder builder,
             Action<OptionsBuilder<ConsulClusteringOptions>> configureOptions)
        {

            return builder.ConfigureServices(
                 services =>
                 {
                     configureOptions?.Invoke(services.AddOptions<ConsulClusteringOptions>());
                     services.AddSingleton<IServiceDiscoveryProvider, ConsulServiceDiscoveryProvider>();
                 });

        }
    }
}
