using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Superdev.AspNetCore.Options;
using MicrosoftOptions = Microsoft.Extensions.Options.Options;

namespace Superdev.AspNetCore.Tests.Options
{
    [Trait(Traits.Category, Traits.UnitTests)]
    public class ServiceCollectionExtensionsTests : IDisposable
    {
        private const string SectionName = "Test";
        private const string AppsettingsFileName = "appsettings.json";

        private readonly string tempDirectory;
        private readonly Mock<IWebHostEnvironment> environmentMock;

        public ServiceCollectionExtensionsTests()
        {
            this.tempDirectory = Path.Combine(Path.GetTempPath(), $"ServiceCollectionExtensionsTests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(this.tempDirectory);
            File.WriteAllText(Path.Combine(this.tempDirectory, AppsettingsFileName), """{ "Test": { "Name": "Old" } }""");

            this.environmentMock = new Mock<IWebHostEnvironment>();
            this.environmentMock.SetupGet(e => e.ContentRootPath).Returns(this.tempDirectory);
        }

        /// <summary>
        /// The options are bound to a configuration root that is not the <see cref="IConfiguration"/> in DI:
        /// a write must reload that root, or rebinding the options rolls the write back.
        /// </summary>
        [Fact]
        public async Task ConfigureWritable_WithSeparateConfigurationRoot_KeepsWrittenValueWhenOptionsAreRebound()
        {
            // Arrange
            var configuration = this.BuildConfiguration();
            var services = this.CreateServices(hostConfiguration: new ConfigurationBuilder().Build());
            services.ConfigureWritable<TestOptions>(configuration, SectionName, AppsettingsFileName);
            await using var serviceProvider = services.BuildServiceProvider();

            // Act
            await serviceProvider.GetRequiredService<IWritableOptions<TestOptions>>().UpdateAsync(o => o.Name = "New");
            serviceProvider.GetRequiredService<IOptionsMonitorCache<TestOptions>>().TryRemove(MicrosoftOptions.DefaultName);

            // Assert
            serviceProvider.GetRequiredService<IOptionsMonitor<TestOptions>>().CurrentValue.Name.Should().Be("New");
        }

        [Fact]
        public async Task ConfigureWritable_WithSectionOfRegisteredConfiguration_KeepsWrittenValueWhenOptionsAreRebound()
        {
            // Arrange
            var configuration = this.BuildConfiguration();
            var services = this.CreateServices(hostConfiguration: configuration);
            services.ConfigureWritable<TestOptions>(configuration.GetSection(SectionName), AppsettingsFileName);
            await using var serviceProvider = services.BuildServiceProvider();

            // Act
            await serviceProvider.GetRequiredService<IWritableOptions<TestOptions>>().UpdateAsync(o => o.Name = "New");
            serviceProvider.GetRequiredService<IOptionsMonitorCache<TestOptions>>().TryRemove(MicrosoftOptions.DefaultName);

            // Assert
            serviceProvider.GetRequiredService<IOptionsMonitor<TestOptions>>().CurrentValue.Name.Should().Be("New");
        }

        [Fact]
        public void ConfigureWritable_WithNullConfigurationRoot_ThrowsArgumentNullException()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var act = () => services.ConfigureWritable<TestOptions>((IConfigurationRoot)null!, SectionName);

            // Assert
            act.Should().Throw<ArgumentNullException>().WithParameterName("configuration");
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public void ConfigureWritable_WithEmptySectionName_ThrowsArgumentException(string sectionName)
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder().Build();

            // Act
            var act = () => services.ConfigureWritable<TestOptions>(configuration, sectionName);

            // Assert
            act.Should().Throw<ArgumentException>().WithParameterName("sectionName");
        }

        private IConfigurationRoot BuildConfiguration()
        {
            return new ConfigurationBuilder()
                .SetBasePath(this.tempDirectory)
                .AddJsonFile(AppsettingsFileName, optional: false, reloadOnChange: false)
                .Build();
        }

        private ServiceCollection CreateServices(IConfiguration hostConfiguration)
        {
            var services = new ServiceCollection();
            services.AddSingleton(this.environmentMock.Object);
            services.AddSingleton(hostConfiguration);
            return services;
        }

        public void Dispose()
        {
            if (Directory.Exists(this.tempDirectory))
            {
                Directory.Delete(this.tempDirectory, true);
            }
        }

        public class TestOptions
        {
            public string Name { get; set; } = string.Empty;
        }
    }
}
