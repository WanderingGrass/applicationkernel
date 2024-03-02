namespace Todd.ApplicationKernel.Redis.Builders;
internal sealed class RedisOptionsBuilder : IRedisOptionsBuilder
{
    private readonly RedisOptions _options = new();

    public IRedisOptionsBuilder WithConnectionString(string connectionString)
    {
        _options.ConnectionString = connectionString;
        return this;
    }
    public RedisOptions Build()
        => _options;
}
