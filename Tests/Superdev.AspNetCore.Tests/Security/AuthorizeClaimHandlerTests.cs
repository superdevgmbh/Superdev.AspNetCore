using System.Security.Claims;
using Superdev.AspNetCore.Security;
using Superdev.AspNetCore.Tests.Logging;

namespace Superdev.AspNetCore.Tests.Security
{
    [Trait(Traits.Category, Traits.UnitTests)]
    public class AuthorizeClaimHandlerTests
    {
        private readonly Mock<ISecurityClaimsInspector> securityClaimsInspectorMock;
        private readonly AuthorizeClaimHandler handler;

        public AuthorizeClaimHandlerTests(ITestOutputHelper testOutputHelper)
        {
            var logger = new TestOutputHelperLogger<AuthorizeClaimHandler>(testOutputHelper);
            this.securityClaimsInspectorMock = new Mock<ISecurityClaimsInspector>();

            this.handler = new AuthorizeClaimHandler(
                logger,
                this.securityClaimsInspectorMock.Object);
        }

        [Fact]
        public async Task HandleAsync_WhenNoAuthorizeClaimAttributesArePresent_Succeeds()
        {
            // Arrange
            var user = CreateAuthenticatedUser();
            var endpoint = CreateEndpointWithAttributes();

            var context = CreateContext(user, endpoint);

            // Act
            await this.handler.HandleAsync(context);

            // Assert
            context.HasSucceeded.Should().BeTrue();
        }

        [Fact]
        public async Task HandleAsync_WhenUserIsUnauthenticated_Fails()
        {
            // Arrange
            var user = CreateUnauthenticatedUser();
            var endpoint = CreateEndpointWithAttributes(
                new AuthorizeClaimAttribute("Permission", "employees.read"));

            var context = CreateContext(user, endpoint);

            // Act
            await this.handler.HandleAsync(context);

            // Assert
            context.HasSucceeded.Should().BeFalse();
        }

        [Fact]
        public async Task HandleAsync_WhenAllAuthorizeClaimAttributesAreSatisfied_Succeeds()
        {
            // Arrange
            var user = CreateAuthenticatedUser(
                new Claim("Permission", "employees.read"));

            var endpoint = CreateEndpointWithAttributes(new AuthorizeClaimAttribute(ClaimRequirementType.All, "Permission", "employees.read"));

            this.securityClaimsInspectorMock
                .Setup(i => i.Satisifies(user, ClaimRequirementType.All, "Permission", It.IsAny<object[]>()))
                .Returns(true);

            var context = CreateContext(user, endpoint);

            // Act
            await this.handler.HandleAsync(context);

            // Assert
            context.HasSucceeded.Should().BeTrue();
        }

        [Fact]
        public async Task HandleAsync_WhenAnyAuthorizeClaimAttributeFails_Fails()
        {
            // Arrange
            var user = CreateAuthenticatedUser(
                new Claim("Permission", "employees.read"));

            var endpoint = CreateEndpointWithAttributes(
                new AuthorizeClaimAttribute(
                    ClaimRequirementType.All,
                    "Permission",
                    "employees.write"));

            this.securityClaimsInspectorMock
                .Setup(i => i.Satisifies(user, ClaimRequirementType.All, "Permission", It.IsAny<object[]>()))
                .Returns(false);

            var context = CreateContext(user, endpoint);

            // Act
            await this.handler.HandleAsync(context);

            // Assert
            context.HasSucceeded.Should().BeFalse();
        }

        [Fact]
        public async Task HandleAsync_WhenMultipleAuthorizeClaimAttributesArePresent_EvaluatesAllWithAndSemantics()
        {
            // Arrange
            var user = CreateAuthenticatedUser(
                new Claim("Permission", "employees.read"),
                new Claim("Permission", "employees.write"));

            var endpoint = CreateEndpointWithAttributes(new AuthorizeClaimAttribute(ClaimRequirementType.All, "Permission", "employees.read"),
                new AuthorizeClaimAttribute(ClaimRequirementType.All, "Permission", "employees.write"));

            this.securityClaimsInspectorMock
                .Setup(i => i.Satisifies(user, It.IsAny<ClaimRequirementType>(), "Permission", It.IsAny<object[]>()))
                .Returns(true);

            var context = CreateContext(user, endpoint);

            // Act
            await this.handler.HandleAsync(context);

            // Assert
            context.HasSucceeded.Should().BeTrue();

            this.securityClaimsInspectorMock.Verify(
                i => i.Satisifies(user, It.IsAny<ClaimRequirementType>(), "Permission", It.IsAny<object[]>()),
                Times.Exactly(2));
        }

        [Fact]
        public async Task HandleAsync_WhenEndpointIsMissing_Fails()
        {
            // Arrange
            var user = CreateAuthenticatedUser();

            var context = CreateContext(user, null);

            // Act
            await this.handler.HandleAsync(context);

            // Assert
            context.HasSucceeded.Should().BeFalse();
        }


        private static AuthorizationHandlerContext CreateContext(ClaimsPrincipal user, Endpoint? endpoint)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.User = user;

            if (endpoint != null)
            {
                httpContext.SetEndpoint(endpoint);
            }

            var requirement = new AuthorizeClaimRequirement();

            return new AuthorizationHandlerContext(new[] { requirement }, user, httpContext);
        }

        private static Endpoint CreateEndpointWithAttributes(params AuthorizeClaimAttribute[] attributes)
        {
            return new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(attributes), "test-endpoint");
        }

        private static ClaimsPrincipal CreateAuthenticatedUser(params Claim[] claims)
        {
            var identity = new ClaimsIdentity(claims, "TestAuth");
            return new ClaimsPrincipal(identity);
        }

        private static ClaimsPrincipal CreateUnauthenticatedUser()
        {
            return new ClaimsPrincipal(new ClaimsIdentity());
        }
    }
}
