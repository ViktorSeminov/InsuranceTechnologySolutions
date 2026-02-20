using Claims.Application.Auditing;
using Claims.Application.Common.Events;
using Claims.Infrastructure.Auditing;
using Claims.Infrastructure.Events;

namespace Claims.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            services.AddScoped<IEventDispatcher, InMemoryEventDispatcher>();
            services.AddScoped<IAuditer, Auditer>();
            return services;
        }
    }
}
