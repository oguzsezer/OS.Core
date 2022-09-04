namespace OS.Cache
{
    public interface ICacheService
    {
        /// <summary>
        /// <para>1. InMemory</para>
        /// <para>2. Redis</para>
        /// <para>3. Function invocation</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="fallbackFunc"></param>
        /// <param name="optionsAction"></param>
        /// <returns></returns>
        public Task<T?> GetOrSet<T>(string key, Action<Options<T>> optionsAction = default);
    }
}
