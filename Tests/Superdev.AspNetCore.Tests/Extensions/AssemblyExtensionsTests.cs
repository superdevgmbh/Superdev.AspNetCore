using System.Reflection;
using System.Reflection.Emit;
using Superdev.AspNetCore.Extensions;
using VersionHashFormat = Superdev.AspNetCore.Extensions.AssemblyExtensions.VersionHashFormat;

namespace Superdev.AspNetCore.Tests.Extensions
{
    [Trait(Traits.Category, Traits.UnitTests)]
    public class AssemblyExtensionsTests
    {
        [Fact]
        public void GetAssemblyVersion_WhenAssemblyHasVersion_ReturnsVersionString()
        {
            // Arrange
            var assembly = CreateAssembly(new Version(1, 2, 3, 4));

            // Act
            var result = assembly.GetAssemblyVersion();

            // Assert
            result.Should().Be("1.2.3.4");
        }

        [Fact]
        public void GetAssemblyInformationalVersion_WhenAttributeExists_ReturnsInformationalVersion()
        {
            // Arrange
            var assembly = CreateAssembly(informationalVersion: "1.2.3-beta+build.5");

            // Act
            var result = assembly.GetAssemblyInformationalVersion();

            // Assert
            result.Should().Be("1.2.3-beta+build.5");
        }

        [Theory]
        [InlineData(
            VersionHashFormat.Full,
            "1.2.3-beta+1234567890abcdef",
            "1.2.3-beta+1234567890abcdef")]
        [InlineData(
            VersionHashFormat.Short,
            "1.2.3-beta+1234567890abcdef",
            "1.2.3-beta+1234567")]
        [InlineData(
            VersionHashFormat.Short,
            "1.2.3-beta+123",
            "1.2.3-beta+123")]
        [InlineData(
            VersionHashFormat.None,
            "1.2.3-beta+1234567890abcdef",
            "1.2.3-beta")]
        public void GetAssemblyInformationalVersion_WhenHashFormatIsSpecified_ReturnsFormattedVersion(VersionHashFormat hashFormat, string informationalVersion,
            string expected)
        {
            // Arrange
            var assembly = CreateAssembly(informationalVersion: informationalVersion);

            // Act
            var result = assembly.GetAssemblyInformationalVersion(hashFormat);

            // Assert
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData(VersionHashFormat.Short, "1.2.3-beta")]
        [InlineData(VersionHashFormat.None, "1.2.3-beta")]
        [InlineData(VersionHashFormat.Short, "1.2.3-beta+")]
        [InlineData(VersionHashFormat.None, "1.2.3-beta+")]
        public void GetAssemblyInformationalVersion_WhenVersionDoesNotContainHash_ReturnsInformationalVersion(VersionHashFormat hashFormat, string informationalVersion)
        {
            // Arrange
            var assembly = CreateAssembly(informationalVersion: informationalVersion);

            // Act
            var result = assembly.GetAssemblyInformationalVersion(hashFormat);

            // Assert
            result.Should().Be(informationalVersion);
        }

        [Fact]
        public void GetAssemblyInformationalVersion_WhenAttributeDoesNotExist_ReturnsNull()
        {
            // Arrange
            var assembly = CreateAssembly();

            // Act
            var result = assembly.GetAssemblyInformationalVersion();

            // Assert
            result.Should().BeNull();
        }

        private static Assembly CreateAssembly(Version? version = null, string? informationalVersion = null)
        {
            var assemblyName = new AssemblyName($"AssemblyExtensionsTests_{Guid.NewGuid():N}")
            {
                Version = version,
            };

            var assembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);

            if (informationalVersion is not null)
            {
                var constructor = typeof(AssemblyInformationalVersionAttribute).GetConstructor([typeof(string)])!;
                assembly.SetCustomAttribute(new CustomAttributeBuilder(constructor, [informationalVersion]));
            }

            return assembly;
        }
    }
}
