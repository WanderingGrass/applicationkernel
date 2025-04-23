// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Todd.ApplicationKernel.Base;
using Todd.ApplicationKernel.Discovery.ZooKeeper.Options;

namespace Todd.ApplicationKernel.Discovery.ZooKeeper
{
    public static class ZooKeeperHostingExtensions
    {
        public static IApplicationKernelBuilder UseZooKeeperClustering(
            this IApplicationKernelBuilder builder,
            Action<OptionsBuilder<ZooKeeperClusteringOptions>> configureOptions)
        {
            return builder.ConfigureServices(
                 services =>
                 {
                     configureOptions?.Invoke(services.AddOptions<ZooKeeperClusteringOptions>());
                 });
        }
    }
}
