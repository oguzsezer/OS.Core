using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace OS.RabbitMq
{
    public static class Bootstrapper
    {
        public static IServiceCollection AddRabbitMq(this IServiceCollection services,
            IConfiguration configuration, string connectionString = null)
        {
            services.Configure<Settings>(configuration.GetSection(nameof(Settings)));
            services.AddSingleton<IPersistentConnection, PersistentConnection>();
            services.AddSingleton<IEventBus, EventBus>();
            return services;
        }

        /// <summary>
        /// Binds <see cref="IConsumable"/> defined type names as routing-key for the Queue.
        /// <para>When a message is received from the queue, it will be sent via MediatR.Send to it's handler.</para>
        /// </summary>
        /// <param name="app"></param>
        public static void SubscribeToRabbitMqEventBus(this IApplicationBuilder app)
        {
            var eventBus = app.ApplicationServices.GetRequiredService<IEventBus>();
            eventBus.SubscribeAndStartConsuming();
        }
    }
}
