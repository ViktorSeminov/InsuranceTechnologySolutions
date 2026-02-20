using Claims.Application.Auditing;
using Claims.Application.Claims;
using Claims.Application.Common.Events;
using Claims.Application.Covers;
using Claims.Domain.Events;

namespace Claims.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            // Domain event handlers
            services.AddScoped<IEventHandler<AuditEvent>, AuditEventHandler>();

            // Application services
            services.AddScoped<ICoverService, CoverService>();
            services.AddScoped<IClaimsService, ClaimService>();

            return services;
        }
    }
}
