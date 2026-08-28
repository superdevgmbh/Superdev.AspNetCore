namespace Superdev.AspNetCore.Security
{
    public sealed class AuthorizeClaimHandler : AuthorizationHandler<AuthorizeClaimRequirement>
    {
        private readonly ILogger logger;
        private readonly ISecurityClaimsInspector securityClaimsInspector;

        public AuthorizeClaimHandler(
            ILogger<AuthorizeClaimHandler> logger,
            ISecurityClaimsInspector securityClaimsInspector)
        {
            this.logger = logger;
            this.securityClaimsInspector = securityClaimsInspector;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AuthorizeClaimRequirement requirement)
        {
            if (context.Resource is not HttpContext httpContext)
            {
                context.Fail();
                return Task.CompletedTask;
            }

            if (context.User.Identity?.IsAuthenticated != true)
            {
                context.Fail();
                return Task.CompletedTask;
            }

            var endpoint = httpContext.GetEndpoint();
            if (endpoint == null)
            {
                context.Fail();
                return Task.CompletedTask;
            }

            var authorizeClaimAttributes = endpoint.Metadata.GetOrderedMetadata<AuthorizeClaimAttribute>();

            if (authorizeClaimAttributes.Count == 0)
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            foreach (var attr in authorizeClaimAttributes)
            {
                var success = this.securityClaimsInspector.Satisifies(context.User, attr.RequirementType, attr.ClaimType, attr.Values);

                if (!success)
                {
                    this.logger.LogInformation($"Authorization failed for claim '{attr.ClaimType}'");
                    context.Fail();
                    return Task.CompletedTask;
                }
            }

            context.Succeed(requirement);
            return Task.CompletedTask;
        }
    }
}
