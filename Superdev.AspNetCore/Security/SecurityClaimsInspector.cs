using System.Security.Claims;
using System.Text.RegularExpressions;

namespace Superdev.AspNetCore.Security
{
    public class SecurityClaimsInspector : ISecurityClaimsInspector
    {
        private static readonly Dictionary<ClaimRequirementType, Func<IEnumerable<Claim>, object[], bool>> Strategies = new()
        {
            { ClaimRequirementType.Any, StrategyAny },
            { ClaimRequirementType.Exists, StrategyExists },
            { ClaimRequirementType.RegexPattern, StrategyRegexPattern },
            { ClaimRequirementType.All, StrategyStrict }
        };

        private const string ClaimsNamespace = "";

        public SecurityClaimsInspector()
        {
        }

        public bool Satisifies(ClaimsPrincipal principal, ClaimRequirementType requirementType, string claimType, params object[] values)
        {
            ArgumentNullException.ThrowIfNull(principal);
            ArgumentNullException.ThrowIfNull(values);

            var type = ClaimsNamespace + claimType;

            var matchingClaims = principal.Claims
                .Where(c => c.Type == type)
                .ToList();

            if (matchingClaims.Any(c => c.Value == SecurityClaimValueTypes.Deny))
            {
                return false;
            }

            var strategy = Strategies[requirementType];
            var success = strategy(matchingClaims, values);
            return success;
        }

        private static bool StrategyExists(IEnumerable<Claim> claims, params object[] _)
        {
            return claims.Any();
        }

        private static bool StrategyAny(IEnumerable<Claim> claims, params object[] values)
        {
            if (!claims.Any())
            {
                return false;
            }

            if (values.Length == 0)
            {
                return true;
            }

            return values.Any(v =>
            {
                var expected = $"{v}";
                return claims.Any(c => v is char
                    ? c.Value.Contains(expected)
                    : c.Value == expected);
            });
        }


        private static bool StrategyRegexPattern(IEnumerable<Claim> claims, params object[] values)
        {
            if (!claims.Any())
            {
                return false;
            }

            var pattern = values.FirstOrDefault()?.ToString();

            if (string.IsNullOrWhiteSpace(pattern))
            {
                throw new ArgumentOutOfRangeException(
                    "Authorization attribute is incorrectly configured. Expected a regex pattern.");
            }

            try
            {
                return claims.Any(c => Regex.IsMatch(c.Value, pattern));
            }
            catch (ArgumentException)
            {
                throw new ArgumentOutOfRangeException(
                    "Authorization attribute is incorrectly configured. Regex parsing failed.");
            }
        }


        private static bool StrategyStrict(IEnumerable<Claim> claims, params object[] values)
        {
            if (!claims.Any())
            {
                return false;
            }

            if (values.Length == 0)
            {
                return true;
            }

            return values.All(v =>
            {
                var expected = $"{v}";
                return claims.Any(c => v is char
                        ? c.Value.Contains(expected)
                        : c.Value == expected);
            });
        }
    }
}
