using System.Security.Claims;

namespace Superdev.AspNetCore.Security
{
    public interface ISecurityClaimsInspector
    {
        bool Satisifies(ClaimsPrincipal principal, ClaimRequirementType requirementType, string claimType, params object[] values);
    }
}
