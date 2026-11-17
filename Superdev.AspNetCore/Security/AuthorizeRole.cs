namespace Superdev.AspNetCore.Security
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public sealed class AuthorizeRoleAttribute : AuthorizeAttribute
    {
        public AuthorizeRoleAttribute(string roles)
        {
            this.Roles = roles;
        }

        public AuthorizeRoleAttribute(params string[] roles)
        {
            this.Roles = string.Join(",", roles.Select(r => r.ToString()));
        }
    }
}
