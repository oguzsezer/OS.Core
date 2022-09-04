namespace OS.Cache
{
    public class Options<T>
    {
        /// <summary>
        /// Will fallback to redis when does not exists in in-memory cache
        /// </summary>
        public bool FallbackToRedisForInMemoryCache { get; set; }
        public bool ShouldAddToRedisWhenNotExistsInRedis { get; set; }
        public int? ExpireTimeInSeconds { get; set; }
        /// <summary>
        /// Will fallback to provided <see cref="FallbackFunc"/> when does not exists in redis (already did fallback to redis from in-memory)
        /// </summary>
        public bool FallbackToCustomFunctionForRedis { get; set; }
        public Func<Task<T>>? FallbackFunc { get; set; }
    }
}
