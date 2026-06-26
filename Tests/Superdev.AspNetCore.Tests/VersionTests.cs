using System.Text.Json;

namespace Superdev.AspNetCore.Tests
{
    [Trait(Traits.Category, Traits.UnitTests)]
    public class VersionTests
    {
        [Theory]
        [InlineData("1.0.0")]
        [InlineData("1.2.3-alpha")]
        [InlineData("1.2.3-alpha.1")]
        [InlineData("1.2.3+build.5")]
        [InlineData("1.2.3-alpha+build.5")]
        public void Parse_ValidSemanticVersion_ReturnsVersion(string value)
        {
            // Act
            var version = Version.Parse(value);

            // Assert
            version.ToString().Should().Be(value);
        }

        [Theory]
        [InlineData("1.0")]
        [InlineData("v1.0.0")]
        [InlineData("01.2.3")]
        [InlineData("1.02.3")]
        [InlineData("1.2.03")]
        [InlineData("1.2.3-")]
        [InlineData("1.2.3-alpha..1")]
        [InlineData("1.2.3-alpha_1")]
        [InlineData("1.2.3+")]
        [InlineData("1.2.3+build..5")]
        public void TryParse_InvalidSemanticVersion_ReturnsFalse(string value)
        {
            // Act
            var result = Version.TryParse(value, out var version);

            // Assert
            result.Should().BeFalse();
            version.Should().BeNull();
        }

        [Fact]
        public void CompareTo_BuildMetadataDiffers_IgnoresBuildMetadata()
        {
            // Arrange
            var left = Version.Parse("1.2.3+build.1");
            var right = Version.Parse("1.2.3+build.2");

            // Act
            var result = left.CompareTo(right);

            // Assert
            result.Should().Be(0);
            left.Should().NotBe(right);
        }

        [Fact]
        public void CompareTo_PrereleaseAndRelease_UsesSemanticVersionPrecedence()
        {
            // Arrange
            var versions = new[]
            {
                Version.Parse("1.0.0"),
                Version.Parse("1.0.0-rc.1"),
                Version.Parse("1.0.0-alpha"),
                Version.Parse("1.0.1"),
                Version.Parse("1.1.0"),
                Version.Parse("2.0.0"),
            };

            // Act
            var ordered = versions.Order().ToArray();

            // Assert
            ordered.Select(version => version.ToString()).Should().Equal(
                "1.0.0-alpha",
                "1.0.0-rc.1",
                "1.0.0",
                "1.0.1",
                "1.1.0",
                "2.0.0");
        }

        [Fact]
        public void IsPrerelease_PrereleaseVersion_ReturnsTrue()
        {
            // Arrange
            var version = Version.Parse("1.2.3-beta.1");

            // Act
            var isPrerelease = version.IsPrerelease;

            // Assert
            isPrerelease.Should().BeTrue();
        }

        [Fact]
        public void ImplicitConversion_FromString_ParsesVersion()
        {
            // Act
            Version version = "1.2.3";

            // Assert
            version.Major.Should().Be(1);
            version.Minor.Should().Be(2);
            version.Patch.Should().Be(3);
        }

        [Fact]
        public void SerializeAndDeserialize_Version_RoundTripsAsJsonString()
        {
            // Arrange
            var version = Version.Parse("1.2.3-alpha+build.5");

            // Act
            var json = JsonSerializer.Serialize(version);
            var deserialized = JsonSerializer.Deserialize<Version>(json);

            // Assert
            json.Should().Be("\"1.2.3-alpha\\u002Bbuild.5\"");
            deserialized.Should().Be(version);
        }
    }
}
