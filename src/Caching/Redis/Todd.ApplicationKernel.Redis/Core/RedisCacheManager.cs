using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StackExchange.Redis;
using System.Collections.Concurrent;

namespace Todd.ApplicationKernel.Redis.Core;

public class RedisCacheManager : CacheKeyService, IStaticCacheManager
{

    #region Fields
    public ISerializer Serializer { get; }
    private readonly IRedisConnectionWrapper _connectionWrapper;
    private readonly IDistributedCache _distributedCache;
    private IMemoryCache _memorycache;
    private readonly ConcurrentDictionary<string, Lazy<Task<object>>> _ongoing = new();
    public RedisCacheManager(
        IRedisConnectionWrapper connectionWrapper,
        IDistributedCache distributedCache,
        IMemoryCache memorycache,
        RedisOptions options) : base(options)
    {
        _connectionWrapper = connectionWrapper;
        _distributedCache = distributedCache;
        _memorycache = memorycache;
    }

    #endregion

    #region Utilities
    /// <summary>
    /// Prepare cache entry options for the passed key
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <returns>Cache entry options</returns>
    protected virtual DistributedCacheEntryOptions PrepareEntryOptions(CacheKey key)
    {
        //set expiration time for the passed cache key
        return new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(key.CacheTime)
        };
    }

    /// <summary>
    /// Try get a cached item. If it's not in the cache yet, then return default object
    /// </summary>
    /// <typeparam name="T">Type of cached item</typeparam>
    /// <param name="key">Cache key</param>
    protected virtual async Task<(bool isSet, T item)> TryGetItemAsync<T>(string key)
    {
        var json = await _distributedCache.GetStringAsync(key);

        return string.IsNullOrEmpty(json)
            ? (false, default)
            : (true, item: JsonConvert.DeserializeObject<T>(json));
    }
    /// <summary>
    /// Remove the value with the specified key from the cache
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <param name="removeFromInstance">Remove from instance</param>
    protected virtual async Task RemoveAsync(string key, bool removeFromInstance = true)
    {
        _ongoing.TryRemove(key, out _);
        await _distributedCache.RemoveAsync(key);

        if (!removeFromInstance)
            return;
        RemoveLocal(key);
    }
    /// <summary>
    /// Add the specified key and object to the local cache
    /// </summary>
    /// <param name="key">Key of cached item</param>
    /// <param name="value">Value for caching</param>
    protected virtual void SetLocal(CacheKey key, object value)
    {
        _memorycache.Set(key.Key, value, TimeSpan.FromMinutes(key.LocalCacheTime));
    }
    /// <summary>
    /// Remove the value with the specified key from the cache
    /// </summary>
    /// <param name="key">Cache key</param>
    protected virtual void RemoveLocal(string key)
    {
        _memorycache.Remove(key);
    }


    #endregion


    #region Base
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
    public async Task RemoveAsync(CacheKey key, params object[] cacheKeyParameters)
    {
        await RemoveAsync(PrepareKey(key, cacheKeyParameters).Key);
    }

    public async Task RemoveAsync(string[] keys)
    {
        var db = await _connectionWrapper.GetDatabaseAsync();
        db.KeyDelete(keys.Select(key => (RedisKey)key).ToArray());
        foreach (var key in keys)
        {
            _memorycache.Remove(key);
        }
    }

    public async Task<T> GetAsync<T>(CacheKey key, T defaultValue = default)
    {
        var value = await _distributedCache.GetStringAsync(key.Key);

        return value != null
            ? JsonConvert.DeserializeObject<T>(value)
            : defaultValue;
    }
    public async Task SetAsync<T>(CacheKey key, T data)
    {
        if (data == null || (key?.CacheTime ?? 0) <= 0)
            return;

        var lazy = new Lazy<Task<object>>(() => Task.FromResult(data as object), true);

        try
        {
            _ongoing.TryAdd(key.Key, lazy);
            // await the lazy task in order to force value creation instead of directly setting data
            // this way, other cache manager instances can access it while it is being set
            SetLocal(key, await lazy.Value);
            await _distributedCache.SetStringAsync(key.Key, JsonConvert.SerializeObject(data), PrepareEntryOptions(key));
        }
        finally
        {
            _ongoing.TryRemove(new KeyValuePair<string, Lazy<Task<object>>>(key.Key, lazy));
        }
    }

    public async Task<T> GetOrSetAsync<T>(CacheKey key, Func<Task<T>> func = null)
    {
        if (_memorycache.TryGetValue(key.Key, out var data))
            return (T)data;
        var lazy = _ongoing.GetOrAdd(key.Key, _ => new(async () => await func(), true));
        var setTask = Task.CompletedTask;
        try
        {
            if (lazy.IsValueCreated)
                return (T)await lazy.Value;

            var (isSet, item) = await TryGetItemAsync<T>(key.Key);
            if (!isSet)
            {
                item = (T)await lazy.Value;

                if (key.CacheTime == 0 || item == null)
                    return item;

                setTask = _distributedCache.SetStringAsync(
                    key.Key,
                    JsonConvert.SerializeObject(item),
                    PrepareEntryOptions(key));
            }

            SetLocal(key, item);
            return item;
        }
        finally
        {
            _ = setTask.ContinueWith(_ => _ongoing.TryRemove(new KeyValuePair<string, Lazy<Task<object>>>(key.Key, lazy)));
        }
    }
    public RedisResult GetByLuaScript(string script, object obj)
    {
        var db = _connectionWrapper.GetDatabase();
        var prepared = LuaScript.Prepare(script);
        return db.ScriptEvaluate(prepared, new { key = "lock_name", value = obj });
    }
  
    public async Task<bool> ExistsAsync(CacheKey key)
    {
        var db = await _connectionWrapper.GetDatabaseAsync();
        return await db.KeyExistsAsync(key.Key);
    }
    public async Task<bool> KeyExpireAsync(CacheKey key, TimeSpan expiry)
    {
        var db = await _connectionWrapper.GetDatabaseAsync();
        return await db.KeyExpireAsync(key.Key, expiry);
    }
    public async Task<TimeSpan?> KeyTimeToLiveAsync(CacheKey key)
    {
        var db = await _connectionWrapper.GetDatabaseAsync();
        return await db.KeyTimeToLiveAsync(key.Key);
    }
    #endregion


    #region Hash

    public async Task<long> HashIncrementAsync(CacheKey key, string field, long value = 1, TimeSpan? expiry = null)
    {
        var db = await _connectionWrapper.GetDatabaseAsync();
        var result = await db.HashIncrementAsync(key.Key, field, value);
        if (expiry.HasValue)
            db.KeyExpire(key.Key, expiry);
        return result;
    }
    public async Task<long> HashDecrementAsync(CacheKey key, string field, long value = 1, TimeSpan? expiry = null)
    {
        var db = await _connectionWrapper.GetDatabaseAsync();
        var result = await db.HashDecrementAsync(key.Key, field, value);
        if (expiry.HasValue)
            db.KeyExpire(key.Key, expiry);
        return result;
    }
    public async Task<long> HashDeleteAsync(CacheKey key, IEnumerable<string> field)
    {
        var db = await _connectionWrapper.GetDatabaseAsync();
        return await db.HashDeleteAsync(key.Key, Array.ConvertAll(field.ToArray(), item => (RedisValue)item));
    }

    public async Task<long> HashDeleteAsync(CacheKey key, string[] field)
    {
        var db = await _connectionWrapper.GetDatabaseAsync();
        return await db.HashDeleteAsync(key.Key, Array.ConvertAll(field, item => (RedisValue)item));
    }


    public async Task<bool> HashExistsAsync(CacheKey key, string hashField)
    {
        var db = await _connectionWrapper.GetDatabaseAsync();
        return await db.HashExistsAsync(key.Key, hashField);
    }

    public async Task<Dictionary<string, T?>> HashGetAllAsync<T>(CacheKey key, CommandFlags commandFlags = CommandFlags.None)
    {
        var db = await _connectionWrapper.GetDatabaseAsync();
        return (await db.HashGetAllAsync(key.Key, commandFlags).ConfigureAwait(false))
            .ToDictionary(
                x => x.Name.ToString(),
                x => JsonConvert.DeserializeObject<T>(x.Value!),
                comparer: StringComparer.Ordinal);
    }

    public async Task<T?> HashGetAsync<T>(CacheKey key, string field)
    {
        var db = await _connectionWrapper.GetDatabaseAsync();
        var redisValue = await db.HashGetAsync(key.Key, field);
        return redisValue.HasValue ? JsonConvert.DeserializeObject<T>(redisValue!) : default;
    }

    public async Task<bool> HashSetAsync<T>(CacheKey key, string field, T value, bool nx = false)
    {
        var db = await _connectionWrapper.GetDatabaseAsync();
        return await db.HashSetAsync(key.Key, field, Serializer.Serialize(value), nx ? When.NotExists : When.Always);
    }

    #endregion

    #region Sort
    public async Task<bool> SortedSetAddAsync<T>(CacheKey key, T obj, double score)
    {
        var entryBytes = Serializer.Serialize(obj);
        var db = await _connectionWrapper.GetDatabaseAsync();
        return await db.SortedSetAddAsync(key.Key, entryBytes, score);
    }

    public async Task<bool> SortedSetRemoveAsync<T>(
        string key,
        T value,
        CommandFlags commandFlags = CommandFlags.None)
    {
        var entryBytes = Serializer.Serialize(value);
        var db = await _connectionWrapper.GetDatabaseAsync();
        return await db.SortedSetRemoveAsync(key, entryBytes, commandFlags);
    }

    public async Task<List<T>> SortedSetRangeByRankAsync<T>(CacheKey key, long start = 0, long stop = -1)
    {
        var db = await _connectionWrapper.GetDatabaseAsync();
        var result = await db.SortedSetRangeByScoreAsync(key.Key, start, stop);
        return result.Select(m => m == RedisValue.Null ? default : Serializer.Deserialize<T>(m!)).ToList();
    }

    #endregion

    #region List

    public async  Task<long> ListAddToLeftAsync<T>(CacheKey key, T item, When when = When.Always, CommandFlags flags = CommandFlags.None)
    {
        var db = await _connectionWrapper.GetDatabaseAsync();

        if (item == null)
            throw new ArgumentNullException(nameof(item), "item cannot be null.");

        var serializedItem = Serializer.Serialize(item);

        return await db.ListLeftPushAsync(key.Key, serializedItem, when, flags);
    }

    /// <inheritdoc/>
    public async Task<long> ListAddToLeftAsync<T>(CacheKey key, T[] items, CommandFlags flags = CommandFlags.None)
    {
        var db = await _connectionWrapper.GetDatabaseAsync();

        if (items == null)
            throw new ArgumentNullException(nameof(items), "item cannot be null.");

        var serializedItems = items.Select(x => (RedisValue)Serializer.Serialize(x)).ToArray();

        return await db.ListLeftPushAsync(key.Key, serializedItems, flags);
    }

    /// <inheritdoc/>
    public async Task<T?> ListGetFromRightAsync<T>(CacheKey key, CommandFlags flags = CommandFlags.None)
    {
        var db = await _connectionWrapper.GetDatabaseAsync();

        var item = await db.ListRightPopAsync(key.Key, flags).ConfigureAwait(false);

        if (item == RedisValue.Null)
            return default;

        return item == RedisValue.Null
            ? default
            : Serializer.Deserialize<T>(item!);
    }

    #endregion

    public async Task<long> StringDecrementAsync(CacheKey key, long value = 1, TimeSpan? expiry = null)
    {
        var db = await _connectionWrapper.GetDatabaseAsync();
        var result = await db.StringDecrementAsync(key.Key, value);
        if (expiry.HasValue)
            await db.ExecuteAsync(key.Key, expiry.Value);
        return result;
    }


    public async Task<long> StringIncrementAsync(CacheKey key, long value = 1, TimeSpan? expiry = null)
    {
        var db = await _connectionWrapper.GetDatabaseAsync();
        var result = await db.StringIncrementAsync(key.Key, value);
        if (expiry.HasValue)
            await db.ExecuteAsync(key.Key, expiry.Value);
        return result;
    }
    #region PubSub

    public async Task SubscribeAsync<T>(RedisChannel channel, Func<T?, Task> handler, CommandFlags flags = CommandFlags.None)
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        var sub = await _connectionWrapper.GetSubscriberAsync();

        async void Handler(RedisChannel redisChannel, RedisValue value) =>
            await handler(Serializer.Deserialize<T>(value!))
                .ConfigureAwait(false);

        await sub.SubscribeAsync(channel, Handler, flags);
    }
    public async Task<long> PublishAsync(string channel, string input)
    {
        var sub = await _connectionWrapper.GetSubscriberAsync();
        return await sub.PublishAsync(channel, Serializer.Serialize(input));
    }
    public async Task UnsubscribeAsync<T>(RedisChannel channel, Func<T?, Task> handler, CommandFlags flags = CommandFlags.None)
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        var sub = await _connectionWrapper.GetSubscriberAsync();
        await sub.UnsubscribeAsync(channel, (_, value) => handler(Serializer.Deserialize<T>(value!)), flags);
    }

    /// <inheritdoc/>
    public async Task UnsubscribeAllAsync(CommandFlags flags = CommandFlags.None)
    {
        var sub = await _connectionWrapper.GetSubscriberAsync();
        await sub.UnsubscribeAllAsync(flags);
    }

    #endregion




}
