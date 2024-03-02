using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Todd.ApplicationKernel.Redis.Core;

namespace Todd.ApplicationKernel.Caching;

public class RedisTests: BaseTests
{
    private IDistributedCache _distributedCache;
    private IServiceScopeFactory _serviceScopeFactory;
    [OneTimeSetUp]
    public void Setup()
    {
       
        _distributedCache = GetService<IDistributedCache>();
        _serviceScopeFactory = GetService<IServiceScopeFactory>();
    }
    [Test]
    public async Task CanSetObjectInCacheAndWillTrackIfRemoved()
    {
      
        (await _distributedCache.GetAsync("some_key_1")).Should().NotBeNullOrEmpty();
        (await _distributedCache.GetAsync("some_key_1")).Should().BeNullOrEmpty();
    }
    [Test]
    public async Task CanGetAsyncFromCacheAndWillTrackIfRemoved()
    {
        await _distributedCache.SetAsync("some_key_2", Encoding.UTF8.GetBytes("2"));
        (await _distributedCache.GetAsync("some_key_2")).Should().BeNullOrEmpty();
    }
}
