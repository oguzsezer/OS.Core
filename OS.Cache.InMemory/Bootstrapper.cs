using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace OS.Cache.InMemory
{
    public static class Bootstrapper
    {
        /// <summary>
        /// Configures MemoryCache.
        /// <para>Reads <see cref="CacheOptions"/> section for <see cref="CacheOptions.ExpireTimeInSeconds"/> key for default expire time. If not provided, default value is 60seconds</para>
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        public static void AddInMemoryCache(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddMemoryCache();
            services.Configure<CacheOptions>(configuration.GetSection(nameof(CacheOptions)));
            services.TryAddSingleton<ICacheManager, CacheManager>();
        }
    }
}
