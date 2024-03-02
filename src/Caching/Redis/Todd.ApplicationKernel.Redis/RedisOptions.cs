namespace Todd.ApplicationKernel.Redis;

public class RedisOptions
{
    public string ConnectionString { get; set; } = "localhost";
    public int Database { get; set; }
    public int DefaultCacheTime { get; set; }
    public int DefaultLocalCacheTime { get; set; }
}
