using Superdev.AspNetCore.Security;
using Superdev.AspNetCore.Services.SystemAbstractions;

namespace Superdev.AspNetCore.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection UseSuperdev(this IServiceCollection services)
        {
            // ===== Authentication ======
            services.AddAuthorizationBuilder()
                .AddPolicy(AuthorizeClaimAttribute.PolicyName, p => p.Requirements.Add(new AuthorizeClaimRequirement()))
                .AddFallbackPolicy("FallbackPolicy", p => p.RequireAuthenticatedUser());

            services.AddSingleton<IAuthorizationHandler, AuthorizeClaimHandler>();
            services.AddSingleton<ISecurityClaimsInspector, SecurityClaimsInspector>();
            // services.AddSingleton<IAuthorizationMiddlewareResultHandler, LoggingAuthorizationMiddlewareResultHandler>();

            // ====== System Abstractions ======
            services.AddSingleton<IFileSystem, FileSystem>();
            services.AddSingleton<IDateTime, SystemDateTime>();
            services.AddSingleton<IDateTimeOffset, SystemDateTimeOffset>();

            return services;
        }
    }
}