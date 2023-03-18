using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OS.Cache.InMemory;
using OS.Cache.Redis;

namespace OS.Cache
{
    public static class Bootstrapper
    {
        public static IServiceCollection AddCache(this IServiceCollection services, IConfiguration configuration, string? connectionString = null)
        {
            services.AddRedisCache(configuration, connectionString);
            services.AddInMemoryCache(configuration);
            services.TryAddSingleton<ICacheService, CacheService>();
            return services;
        }
    }
}