using Todd.ApplicationKernel.CachingCore;

namespace Todd.ApplicationKernel.Redis;
public interface IRedisOptionsBuilder
{
    IRedisOptionsBuilder WithConnectionString(string connectionString);
    RedisOptions Build();
}
