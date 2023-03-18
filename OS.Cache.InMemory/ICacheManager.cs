namespace OS.Cache.InMemory
{
    public interface ICacheManager
    {
        T? Get<T>(string key);
        void Update<T>(string key, T value, int? expireInSeconds = null);
        void Remove(string key);
        void Set<T>(string key, T value, int? expireInSeconds = null);

        /// <summary>
        /// Tries to read key from cache. If the key does not exists, invokes the provided function <paramref name="fallbackFunc"/> and adds the value returned.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="fallbackFunc"></param>
        /// <param name="expireInSeconds"></param>
        /// <returns></returns>
        public Task<T> GetOrAdd<T>(string key, Func<string, Task<T>> fallbackFunc, int? expireInSeconds = null);
        public bool FlushCache();
    }
}