using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace OS.Cache.InMemory
{
    internal class CacheManager : ICacheManager
    {
        private readonly ILogger<CacheManager> _logger;
        private readonly IMemoryCache _memoryCache;
        private readonly int _cacheExpireTime;

        public CacheManager(IOptions<CacheOptions> cacheOptions,
            ILogger<CacheManager> logger,
            IMemoryCache memoryCache)
        {
            _cacheExpireTime = (cacheOptions?.Value?.ExpireTimeInSeconds).GetValueOrDefault(1);
            _logger = logger;
            _memoryCache = memoryCache;
        }

        public T? Get<T>(string key)
        {
            return _memoryCache.TryGetValue(key, out var value)
                ? (T)value
                : default;
        }

        public async Task<T> GetOrAdd<T>(string key, Func<string, Task<T>> fallbackFunc, int? expireInSeconds = null)
        {
            try
            {
                if (_memoryCache.TryGetValue(key, out var value))
                {
                    return (T)value;
                }

                var obj = await fallbackFunc(key);
                _memoryCache.Set(key, obj,
                    new MemoryCacheEntryOptions().SetSlidingExpiration(expireInSeconds.HasValue
                        ? TimeSpan.FromSeconds(expireInSeconds.Value)
                        : TimeSpan.FromSeconds(_cacheExpireTime)));
                return obj;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }

        public bool FlushCache()
        {
            try
            {
                ((MemoryCache)_memoryCache).Compact(1.0);
                _logger.LogInformation("Cache flushed");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while flushing memory cache");
            }

            return false;
        }

        public void Update<T>(string key, T value, int? expireInSeconds = null)
        {
            try
            {
                _memoryCache.Remove(key);
                _memoryCache.Set(key, value,
                    new MemoryCacheEntryOptions().SetSlidingExpiration(expireInSeconds.HasValue
                        ? TimeSpan.FromSeconds(expireInSeconds.Value)
                        : TimeSpan.FromSeconds(_cacheExpireTime)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }

        public void Remove(string key)
        {
            try
            {
                _memoryCache.Remove(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }

        public void Set<T>(string key, T value, int? expireInSeconds = null)
        {
            try
            {
                _memoryCache.Set(key, value,
                    new MemoryCacheEntryOptions().SetSlidingExpiration(expireInSeconds.HasValue
                        ? TimeSpan.FromSeconds(expireInSeconds.Value)
                        : TimeSpan.FromSeconds(_cacheExpireTime)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }
    }
}
