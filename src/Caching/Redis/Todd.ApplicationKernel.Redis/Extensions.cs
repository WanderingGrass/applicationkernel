using Todd.ApplicationKernel.Redis.Builders;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Todd.ApplicationKernel.Base;
using Todd.ApplicationKernel.CachingCore;

namespace Todd.ApplicationKernel.Redis;

public static class Extensions
{
    private const string SectionName = "redis";
    private const string RegistryName = "persistence.redis";
    public static IApplicationKernelBuilder AddRedis(this IApplicationKernelBuilder builder, string sectionName = SectionName)
    {
        if (string.IsNullOrWhiteSpace(sectionName))
        {
            sectionName = SectionName;
        }
        var options = builder.GetOptions<RedisOptions>(sectionName);
        return builder.AddRedis(options);
    }
    public static IApplicationKernelBuilder AddRedis(this IApplicationKernelBuilder builder,
        Func<IRedisOptionsBuilder, IRedisOptionsBuilder> buildOptions)
    {
        var options = buildOptions(new RedisOptionsBuilder()).Build();
        return builder.AddRedis(options);
    }

    public static IApplicationKernelBuilder AddRedis(this IApplicationKernelBuilder builder, RedisOptions options)
    {
        if (!builder.TryRegister(RegistryName))
        {
            return builder;
        }


        builder.Services
            .AddSingleton(options)
            .AddSingleton<IConnectionMultiplexer>(sp => ConnectionMultiplexer.Connect(options.ConnectionString))
            .AddTransient(sp => sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase(options.Database))
            .AddStackExchangeRedisCache(o =>
            {
                o.Configuration = options.ConnectionString;
            });
        builder.Services.AddTransient<IRedisConnectionWrapper, RedisConnectionWrapper>();
        builder.Services.AddScoped<IStaticCacheManager, RedisCacheManager>();
        builder.Services.AddScoped<ICacheKeyService, RedisCacheManager>();
        return builder;
    }
}
