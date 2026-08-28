using System.Security.Claims;
using Superdev.AspNetCore.Security;

namespace Superdev.AspNetCore.Tests.Security
{
    [Trait(Traits.Category, Traits.UnitTests)]
    public class SecurityClaimsInspectorTests
    {
        private readonly SecurityClaimsInspector securityClaimsInspector = new();

        [Fact]
        public void Satisifies_WhenExistsRequirementAndClaimExists_ReturnsTrue()
        {
            // Arrange
            var principal = CreatePrincipal(
                new Claim("Permission", "employees.read")
            );

            // Act
            var result = this.securityClaimsInspector.Satisifies(
                principal,
                ClaimRequirementType.Exists,
                "Permission");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void Satisifies_WhenExistsRequirementAndClaimDoesNotExist_ReturnsFalse()
        {
            // Arrange
            var principal = CreatePrincipal();

            // Act
            var result = this.securityClaimsInspector.Satisifies(
                principal,
                ClaimRequirementType.Exists,
                "Permission");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Satisifies_WhenAnyRequirementAndRequiredPermissionExists_ReturnsTrue()
        {
            // Arrange
            var principal = CreatePrincipal(
                new Claim("Permission", "employees.read"),
                new Claim("Permission", "employees.write")
            );

            // Act
            var result = this.securityClaimsInspector.Satisifies(
                principal,
                ClaimRequirementType.Any,
                "Permission",
                "employees.read");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void Satisifies_WhenAnyRequirementAndRequiredPermissionDoesNotExist_ReturnsFalse()
        {
            // Arrange
            var principal = CreatePrincipal(
                new Claim("Permission", "employees.write")
            );

            // Act
            var result = this.securityClaimsInspector.Satisifies(
                principal,
                ClaimRequirementType.Any,
                "Permission",
                "employees.read");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Satisifies_WhenAllRequirementAndPermissionsExistAcrossMultipleClaims_ReturnsTrue()
        {
            // Arrange
            var principal = CreatePrincipal(
                new Claim("Permission", "employees.read"),
                new Claim("Permission", "employees.write")
            );

            // Act
            var result = this.securityClaimsInspector.Satisifies(
                principal,
                ClaimRequirementType.All,
                "Permission",
                "employees.read",
                "employees.write");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void Satisifies_WhenAllRequirementAndOnePermissionIsMissing_ReturnsFalse()
        {
            // Arrange
            var principal = CreatePrincipal(
                new Claim("Permission", "employees.read")
            );

            // Act
            var result = this.securityClaimsInspector.Satisifies(
                principal,
                ClaimRequirementType.All,
                "Permission",
                "employees.read",
                "employees.write");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Satisifies_WhenDenyClaimExists_ReturnsFalse()
        {
            // Arrange
            var principal = CreatePrincipal(
                new Claim("Permission", "employees.read"),
                new Claim("Permission", SecurityClaimValueTypes.Deny)
            );

            // Act
            var result = this.securityClaimsInspector.Satisifies(
                principal,
                ClaimRequirementType.Any,
                "Permission",
                "employees.read");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Satisifies_WhenAllRequirementAndCrudCharsExistInSingleClaim_ReturnsTrue()
        {
            // Arrange
            var principal = CreatePrincipal(
                new Claim("employees", "CRUD")
            );

            // Act
            var result = this.securityClaimsInspector.Satisifies(
                principal,
                ClaimRequirementType.All,
                "employees",
                SecurityClaimValueTypes.Read,
                SecurityClaimValueTypes.Update);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void Satisifies_WhenAnyRequirementAndCrudCharExists_ReturnsTrue()
        {
            // Arrange
            var principal = CreatePrincipal(
                new Claim("employees", "R")
            );

            // Act
            var result = this.securityClaimsInspector.Satisifies(
                principal,
                ClaimRequirementType.Any,
                "employees",
                SecurityClaimValueTypes.Read,
                SecurityClaimValueTypes.Update);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void Satisifies_WhenRegexPatternRequirementAndClaimMatchesPattern_ReturnsTrue()
        {
            // Arrange
            var principal = CreatePrincipal(
                new Claim("Permission", "employees.read"),
                new Claim("Permission", "orders.read")
            );

            // Act
            var result = this.securityClaimsInspector.Satisifies(
                principal,
                ClaimRequirementType.RegexPattern,
                "Permission",
                "^employees\\.");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void Satisifies_WhenRegexPatternRequirementAndNoClaimMatchesPattern_ReturnsFalse()
        {
            // Arrange
            var principal = CreatePrincipal(
                new Claim("Permission", "orders.read")
            );

            // Act
            var result = this.securityClaimsInspector.Satisifies(
                principal,
                ClaimRequirementType.RegexPattern,
                "Permission",
                "^employees\\.");

            // Assert
            result.Should().BeFalse();
        }

        private static ClaimsPrincipal CreatePrincipal(params Claim[] claims)
        {
            var identity = new ClaimsIdentity(claims, "Test");
            return new ClaimsPrincipal(identity);
        }
    }
}
