using Superdev.AspNetCore.ApplicationModelConventions;

namespace Superdev.AspNetCore.Tests.ApplicationModelConventions
{
    [Trait(Traits.Category, Traits.UnitTests)]
    public class EnvironmentRestrictedAttributeTests
    {
        [Fact]
        public void Constructor_WhenEnvironmentNamesIsNull_ThrowsArgumentNullException()
        {
            // Act
            var action = () => new EnvironmentRestrictedAttribute(null!);

            // Assert
            action.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_WhenNoEnvironmentNamesAreProvided_ThrowsArgumentException()
        {
            // Act
            var action = () => new EnvironmentRestrictedAttribute();

            // Assert
            action.Should().Throw<ArgumentException>()
                .WithParameterName("environmentNames");
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void Constructor_WhenAnyEnvironmentNameIsNullOrWhiteSpace_ThrowsArgumentException(string? environmentName)
        {
            // Act
            var action = () => new EnvironmentRestrictedAttribute("Development", environmentName!);

            // Assert
            action.Should().Throw<ArgumentException>()
                .WithParameterName("environmentNames");
        }

        [Fact]
        public void Constructor_WhenEnvironmentNamesAreProvided_SetsEnvironmentNames()
        {
            // Act
            var attribute = new EnvironmentRestrictedAttribute("Development", "Staging");

            // Assert
            attribute.EnvironmentNames.Should().Equal("Development", "Staging");
        }

        [Theory]
        [InlineData("Development")]
        [InlineData("development")]
        [InlineData("DEVELOPMENT")]
        public void IsAllowed_WhenEnvironmentMatchesCaseInsensitively_ReturnsTrue(string environmentName)
        {
            // Arrange
            var attribute = new EnvironmentRestrictedAttribute("Development");

            // Act
            var result = attribute.IsAllowed(environmentName);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsAllowed_WhenEnvironmentDoesNotMatch_ReturnsFalse()
        {
            // Arrange
            var attribute = new EnvironmentRestrictedAttribute("Development", "Staging");

            // Act
            var result = attribute.IsAllowed("Production");

            // Assert
            result.Should().BeFalse();
        }
    }
}
