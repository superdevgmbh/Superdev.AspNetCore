namespace Superdev.AspNetCore.Infrastructure.Security
{
    public enum ClaimRequirementType
    {
        Any,
        Exists,
        RegexPattern,
        All
    };
}