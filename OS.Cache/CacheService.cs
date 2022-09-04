using OS.Cache.InMemory;
using OS.Cache.Redis;

namespace OS.Cache
{
    internal class CacheService : ICacheService
    {
        private readonly ICacheManager _inMemoryCache;
        private readonly IRedisCache _redisCache;

        public CacheService(ICacheManager inMemoryCache, IRedisCache redisCache)
        {
            _inMemoryCache = inMemoryCache;
            _redisCache = redisCache;
        }

        public async Task<T?> GetOrSet<T>(string key, Action<Options<T>> optionsAction = default)
        {
            var options = new Options<T>();
            optionsAction?.Invoke(options);

            var value = _inMemoryCache.Get<T>(key);
            if (value != null)
            {
                return value;
            }

            if (options.FallbackToRedisForInMemoryCache)
            {
                value = await _redisCache.GetObjectAsync<T>(key);
                if (value != null)
                {
                    _inMemoryCache.Set(key, value, options.ExpireTimeInSeconds);
                    return value;
                }
            }

            if (options.FallbackToCustomFunctionForRedis && options.FallbackFunc != default)
            {
                value = await options.FallbackFunc();
                if (value != null)
                {
                    if (options.ShouldAddToRedisWhenNotExistsInRedis)
                    {
#pragma warning disable CS4014
                        _redisCache.SetObjectAsync(key, value,
                            options.ExpireTimeInSeconds.HasValue
                                ? TimeSpan.FromSeconds(options.ExpireTimeInSeconds.Value)
                                : null);
#pragma warning restore CS4014
                    }
                    return value;
                }
            }

            return value;
        }
    }
}
