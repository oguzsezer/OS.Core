using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;

namespace OS.Cache.Redis
{
    public static class Bootstrapper
    {
        /// <summary>
        /// Configures redis connection.
        /// <para>Reads ConnectionStrings section for "Redis" key when <paramref name="connectionString"/>  not provided.</para>
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static IServiceCollection AddRedisCache(this IServiceCollection services, IConfiguration configuration, string? connectionString = null)
        {
            var redisConnectionString = string.IsNullOrWhiteSpace(connectionString) ? configuration.GetConnectionString("Redis") : connectionString;
            if (string.IsNullOrWhiteSpace(redisConnectionString))
            {
                throw new ArgumentException("Configuration error. Either connectionString parameter must be provided or ConnectionStrings section must have a Redis key.",
                    redisConnectionString);
            }

            var options = ConfigurationOptions.Parse(redisConnectionString);
            options.ClientName = $"{Assembly.GetExecutingAssembly().GetName().Name}_{Environment.MachineName}";
            var multiplexer = ConnectionMultiplexer.Connect(options);
            services.TryAddSingleton<IConnectionMultiplexer>(multiplexer);
            services.TryAddTransient<IRedisCache, RedisCache>();
            return services;
        }
    }
}
