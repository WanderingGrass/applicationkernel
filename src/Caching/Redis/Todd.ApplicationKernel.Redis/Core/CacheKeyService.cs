﻿using System.Globalization;
using System.Text;

namespace Todd.ApplicationKernel.Redis.Core;

public abstract partial class CacheKeyService : ICacheKeyService
{
    #region Fields

    protected readonly RedisOptions _options;

    #endregion

    #region Ctor

    protected CacheKeyService(RedisOptions option)
    {
        _options = option;
    }

    #endregion

    #region Utilities

    /// <summary>
    /// Prepare the cache key prefix
    /// </summary>
    /// <param name="prefix">Cache key prefix</param>
    /// <param name="prefixParameters">Parameters to create cache key prefix</param>
    protected virtual string PrepareKeyPrefix(string prefix, params object[] prefixParameters)
    {
        return prefixParameters?.Any() ?? false
            ? string.Format(prefix, prefixParameters.Select(CreateCacheKeyParameters).ToArray())
            : prefix;
    }

    /// <summary>
    /// Create the hash value of the passed identifiers
    /// </summary>
    /// <param name="ids">Collection of identifiers</param>
    /// <returns>String hash value</returns>
    protected virtual string CreateIdsHash(IEnumerable<int> ids)
    {
        var identifiers = ids.ToList();

        if (!identifiers.Any())
            return string.Empty;

        var identifiersString = string.Join(", ", identifiers.OrderBy(id => id));
        return HashHelper.CreateHash(Encoding.UTF8.GetBytes(identifiersString), HashAlgorithm);
    }

    /// <summary>
    /// Converts an object to cache parameter
    /// </summary>
    /// <param name="parameter">Object to convert</param>
    /// <returns>Cache parameter</returns>
    protected virtual object CreateCacheKeyParameters(object parameter)
    {
        return parameter switch
        {
            null => "null",
            IEnumerable<int> ids => CreateIdsHash(ids),
            decimal param => param.ToString(CultureInfo.InvariantCulture),
            _ => parameter
        };
    }

    #endregion

    #region Methods

    /// <summary>
    /// Create a copy of cache key and fills it by passed parameters
    /// </summary>
    /// <param name="cacheKey">Initial cache key</param>
    /// <param name="cacheKeyParameters">Parameters to create cache key</param>
    /// <returns>Cache key</returns>
    public virtual CacheKey PrepareKey(CacheKey cacheKey, params object[] cacheKeyParameters)
    {
        return cacheKey.Create(CreateCacheKeyParameters, cacheKeyParameters);
    }

    /// <summary>
    /// Create a copy of cache key using the default cache time and fills it by passed parameters
    /// </summary>
    /// <param name="cacheKey">Initial cache key</param>
    /// <param name="cacheKeyParameters">Parameters to create cache key</param>
    /// <returns>Cache key</returns>
    public virtual CacheKey PrepareKeyForDefaultCache(CacheKey cacheKey, params object[] cacheKeyParameters)
    {
        var key = cacheKey.Create(CreateCacheKeyParameters, cacheKeyParameters);

        key.CacheTime = _options.DefaultCacheTime;
        key.LocalCacheTime = _options.DefaultLocalCacheTime;
        return key;
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets an algorithm used to create the hash value of identifiers need to cache
    /// </summary>
    protected string HashAlgorithm => "SHA1";

    #endregion
}
