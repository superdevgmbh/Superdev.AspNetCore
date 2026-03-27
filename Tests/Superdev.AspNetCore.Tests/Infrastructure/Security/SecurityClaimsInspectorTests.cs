using System.Security.Claims;
using Superdev.AspNetCore.Infrastructure.Security;

namespace Superdev.AspNetCore.Tests.Infrastructure.Security
{
    public class SecurityClaimsInspectorTests
    {
        private readonly SecurityClaimsInspector securityClaimsInspector = new();

        [Fact]
        public void Exists_ReturnsTrue_WhenClaimExists()
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
        public void Exists_ReturnsFalse_WhenClaimDoesNotExist()
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
        public void Any_ReturnsTrue_WhenAnyRequiredPermissionExists()
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
        public void Any_ReturnsFalse_WhenNoRequiredPermissionExists()
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
        public void All_ReturnsTrue_WhenAllPermissionsExistAcrossMultipleClaims()
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
        public void All_ReturnsFalse_WhenOnePermissionIsMissing()
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
        public void Deny_OverridesAllOtherPermissions()
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
        public void Crud_All_ReturnsTrue_WhenAllCharsExistInSingleClaim()
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
        public void Crud_Any_ReturnsTrue_WhenAnyCharExists()
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
        public void Regex_ReturnsTrue_WhenAnyClaimMatchesPattern()
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
        public void Regex_ReturnsFalse_WhenNoClaimMatchesPattern()
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