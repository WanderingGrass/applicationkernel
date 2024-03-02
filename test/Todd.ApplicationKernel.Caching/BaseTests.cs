// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using Microsoft.Extensions.DependencyInjection;
using Todd.ApplicationKernel.Base;
using Todd.ApplicationKernel.Redis;

namespace Todd.ApplicationKernel.Caching;

public class BaseTests
{
    private static readonly ServiceProvider _serviceProvider;
    protected BaseTests()
    {
       
    }
    static BaseTests()
    {
        var services = new ServiceCollection();
        services.AddApplicationKernel().AddRedis().Build();
        _serviceProvider = services.BuildServiceProvider();
    }
    public ServiceProvider ServiceProvider => _serviceProvider;

    protected static T GetService<T>()
    {
        return _serviceProvider.GetRequiredService<T>();
    }
    protected static T GetService<T>(IServiceScope scope)
    {
        return scope.ServiceProvider.GetService<T>();
    }

}
