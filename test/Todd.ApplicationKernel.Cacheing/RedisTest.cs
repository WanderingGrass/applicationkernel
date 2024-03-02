// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Xunit;

namespace Todd.ApplicationKernel.Cacheing;

public class RedisTest
{
    [Fact]
    public void AddStackExchangeRedisCache_RegistersDistributedCacheAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddStackExchangeRedisCache(options => { });

        // Assert
        var distributedCache = services.FirstOrDefault(desc => desc.ServiceType == typeof(IDistributedCache));

        Assert.NotNull(distributedCache);
        Assert.Equal(ServiceLifetime.Singleton, distributedCache.Lifetime);
    }

}
