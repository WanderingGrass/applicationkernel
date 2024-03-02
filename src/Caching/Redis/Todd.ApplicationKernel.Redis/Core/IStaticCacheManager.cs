using StackExchange.Redis;

namespace Todd.ApplicationKernel.Redis.Core;

/// <summary>
/// Represents a manager for caching between HTTP requests (long term caching)
/// </summary>
public interface IStaticCacheManager : IDisposable, ICacheKeyService
{


    /// <summary>
    /// 异步获取缓存
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <returns></returns>
    Task<T> GetAsync<T>(CacheKey key, T defaultValue = default);



    /// <summary>
    ///获取或者创建缓存 
    /// localExpiredTime参数大于0并且小于expiredTime数据将缓存到本地内存
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <param name="func"></param>
    /// <returns></returns>
    Task<T> GetOrSetAsync<T>(CacheKey key, Func<Task<T>> func = null);



    /// <summary>
    ///新增缓存
    /// </summary>
    /// <param name="key">缓存key值,key值必须满足规则：模块名:类名:业务方法名:参数.不满足规则将不会被缓存</param>
    /// <param name="data">Value for caching</param>

    Task SetAsync<T>(CacheKey key, T data);



    /// <summary>
    /// 是否存在
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    Task<bool> ExistsAsync(CacheKey key);



    /// <summary>
    /// 移除key
    /// </summary>
    /// <param name="key"></param>
    /// <returns>删除的个数</returns>
    Task RemoveAsync(CacheKey cacheKey, params object[] cacheKeyParameters);



    /// <summary>
    /// 批量移除key
    /// </summary>
    /// <param name="keys"></param>
    /// <returns></returns>
    Task RemoveAsync(string[] keys);



    /// <summary>
    /// 向有序集合添加一个或多个成员，或者更新已存在成员的分数
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <param name="obj"></param>
    /// <param name="score"></param>
    /// <returns></returns>
    Task<bool> SortedSetAddAsync<T>(CacheKey key, T obj, double score);

    Task<bool> SortedSetRemoveAsync<T>(
        string key,
        T value,
        CommandFlags commandFlags = CommandFlags.None);
    /// <summary>
    /// 通过索引区间返回有序集合成指定区间内的成员
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <param name="start"></param>
    /// <param name="stop"></param>
    /// <returns></returns>
    Task<List<T>> SortedSetRangeByRankAsync<T>(CacheKey key, long start = 0, long stop = -1);


    /// <summary>
    ///   获取在哈希表中指定 key 的所有字段和值
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <returns></returns>
    Task<Dictionary<string, T?>> HashGetAllAsync<T>(CacheKey key, CommandFlags commandFlags = CommandFlags.None);



    /// <summary>
    /// 删除hash中的字段
    /// </summary>
    /// <param name="key"></param>
    /// <param name="field"></param>
    /// <returns></returns>
    Task<long> HashDeleteAsync(CacheKey key, IEnumerable<string> field);

    /// <summary>
    /// 删除hash中的字段
    /// </summary>
    /// <param name="key"></param>
    /// <param name="field"></param>
    /// <returns></returns>
    Task<long> HashDeleteAsync(CacheKey key, string[] field);

    /// <summary>
    /// 检查 hash条目中的 key是否存在
    /// </summary>
    /// <param name="key"></param>
    /// <param name="hashField"></param>
    /// <returns></returns>
    Task<bool> HashExistsAsync(CacheKey key, string hashField);



    /// <summary>
    /// 设置或更新Hash
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <param name="field"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    Task<bool> HashSetAsync<T>(CacheKey key, string field, T value, bool nx = false);

    /// <summary>
    /// 获取Hash
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <param name="field"></param>
    /// <returns></returns>
    Task<T?> HashGetAsync<T>(CacheKey key, string field);


    Task<long> HashIncrementAsync(CacheKey key, string field, long value = 1, TimeSpan? expiry = null);



    Task<long> HashDecrementAsync(CacheKey key, string field, long value = 1, TimeSpan? expiry = null);

    /// <summary>
    /// lua脚本
    /// obj :new {key=key}
    /// </summary>
    /// <param name="script"></param>
    /// <param name="obj"></param>
    RedisResult GetByLuaScript(string script, object obj);



    /// <summary>
    /// value递增
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    Task<long> StringIncrementAsync(CacheKey key, long value = 1, TimeSpan? expiry = null);


    /// <summary>
    /// value递减
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="expiry">过期时间</param>
    /// <returns></returns>
    /// <remarks>TODO 待优化为脚本批量操作</remarks>
    Task<long> StringDecrementAsync(CacheKey key, long value = 1, TimeSpan? expiry = null);

    /// <summary>
    /// 返回具有超时的键的剩余生存时间
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    Task<TimeSpan?> KeyTimeToLiveAsync(CacheKey key);



    /// <summary>
    /// 设置一个超时键
    /// </summary>
    /// <param name="key"></param>
    /// <param name="expiry"></param>
    /// <returns>true:设置成功，false：设置失败</returns>
    Task<bool> KeyExpireAsync(CacheKey key, TimeSpan expiry);


    /// <summary>
    /// 发布
    /// </summary>
    /// <param name="channel"></param>
    /// <param name="input"></param>
    Task<long> PublishAsync(string channel, string input);


    Task SubscribeAsync<T>(RedisChannel channel, Func<T?, Task> handler, CommandFlags flags = CommandFlags.None);

    /// <summary>
    ///     Unregisters a callback handler to process messages published to a channel.
    /// </summary>
    /// <typeparam name="T">The type of the expected object.</typeparam>
    /// <param name="channel">The pub/sub channel name</param>
    /// <param name="handler">The function to run when a message has received.</param>
    /// <param name="flag">Behaviour markers associated with a given command</param>
    Task UnsubscribeAsync<T>(RedisChannel channel, Func<T?, Task> handler, CommandFlags flag = CommandFlags.None);

    /// <summary>
    ///     Unregisters all callback handlers on a channel.
    /// </summary>
    /// <param name="flag">Behaviour markers associated with a given command</param>
    Task UnsubscribeAllAsync(CommandFlags flag = CommandFlags.None);


    Task<long> ListAddToLeftAsync<T>(CacheKey key, T item, When when = When.Always,
        CommandFlags flags = CommandFlags.None);

    Task<long> ListAddToLeftAsync<T>(CacheKey key, T[] items, CommandFlags flags = CommandFlags.None);
    Task<T?> ListGetFromRightAsync<T>(CacheKey key, CommandFlags flags = CommandFlags.None);
}
