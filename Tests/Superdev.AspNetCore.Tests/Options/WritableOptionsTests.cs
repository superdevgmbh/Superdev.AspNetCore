using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Superdev.AspNetCore.Infrastructure.Configuration;

namespace Superdev.AspNetCore.Tests.Infrastructure.Configuration
{
    public class WritableOptionsTests : IDisposable
    {
        private readonly string tempDirectory;
        private readonly string settingsFilePath;
        private readonly Mock<IWebHostEnvironment> environmentMock;
        private readonly Mock<IConfigurationRoot> configurationRootMock;
        private readonly Mock<IOptionsMonitor<TestOptions>> optionsMonitorMock;
        private readonly Mock<IOptionsMonitorCache<TestOptions>> optionsMonitorCacheMock;

        public WritableOptionsTests()
        {
            this.tempDirectory = Path.Combine(Path.GetTempPath(), $"WritableOptionsTests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(this.tempDirectory);
            this.settingsFilePath = Path.Combine(this.tempDirectory, "appsettings.json");

            this.environmentMock = new Mock<IWebHostEnvironment>();
            this.environmentMock.SetupGet(x => x.ContentRootPath).Returns(this.tempDirectory);

            this.configurationRootMock = new Mock<IConfigurationRoot>();

            this.optionsMonitorMock = new Mock<IOptionsMonitor<TestOptions>>();
            this.optionsMonitorMock.Setup(x => x.CurrentValue).Returns(() => new TestOptions
            {
                Name = "Current",
                Count = 1,
            });
            this.optionsMonitorMock.Setup(x => x.Get(It.IsAny<string?>())).Returns(() => new TestOptions
            {
                Name = "Current",
                Count = 1,
            });

            this.optionsMonitorCacheMock = new Mock<IOptionsMonitorCache<TestOptions>>();
            this.optionsMonitorCacheMock.Setup(x => x.TryRemove(It.IsAny<string>())).Returns(true);
            this.optionsMonitorCacheMock.Setup(x => x.TryAdd(It.IsAny<string>(), It.IsAny<TestOptions>())).Returns(true);
        }

        [Fact]
        public async Task ShouldReplaceConfiguredSectionWhenUpdatingWholeOptions()
        {
            // Arrange
            await File.WriteAllTextAsync(this.settingsFilePath,
                """
                {
                  "Test": {
                    "Name": "Old",
                    "Count": 5
                  },
                  "Other": {
                    "Enabled": true
                  }
                }
                """);

            var writableOptions = this.CreateWritableOptions();

            // Act
            await writableOptions.UpdateAsync(new TestOptions
            {
                Name = "New",
                Count = 9,
            });

            // Assert
            var jsonObject = await this.ReadSettingsFileAsync();
            jsonObject["Other"]?["Enabled"]?.GetValue<bool>().Should().BeTrue();
            jsonObject["Test"]?["Name"]?.GetValue<string>().Should().Be("New");
            jsonObject["Test"]?["Count"]?.GetValue<int>().Should().Be(9);

            this.optionsMonitorCacheMock.Verify(x => x.TryRemove(Options.DefaultName), Times.Once);
            this.optionsMonitorCacheMock.Verify(x => x.TryAdd(Options.DefaultName, It.Is<TestOptions>(o => o.Name == "New" && o.Count == 9)), Times.Once);
            this.configurationRootMock.Verify(x => x.Reload(), Times.Once);
        }

        [Fact]
        public async Task ShouldUpdateSinglePropertyAndPreserveRemainingValues()
        {
            // Arrange
            await File.WriteAllTextAsync(this.settingsFilePath,
                """
                {
                  "Test": {
                    "Name": "Original",
                    "Count": 3
                  }
                }
                """);

            var sut = this.CreateWritableOptions();

            // Act
            await sut.UpdatePropertyAsync(x => x.Count, 7);

            // Assert
            var jsonObject = await this.ReadSettingsFileAsync();
            jsonObject["Test"]?["Name"]?.GetValue<string>().Should().Be("Original");
            jsonObject["Test"]?["Count"]?.GetValue<int>().Should().Be(7);

            this.optionsMonitorCacheMock.Verify(x => x.TryRemove(Options.DefaultName), Times.Once);
            this.optionsMonitorCacheMock.Verify(
                x => x.TryAdd(
                    Options.DefaultName,
                    It.Is<TestOptions>(o => o.Name == "Original" && o.Count == 7)),
                Times.Once);
        }

        private WritableOptions<TestOptions> CreateWritableOptions()
        {
            return new WritableOptions<TestOptions>(
                this.environmentMock.Object,
                this.configurationRootMock.Object,
                this.optionsMonitorMock.Object,
                this.optionsMonitorCacheMock.Object,
                "Test",
                "appsettings.json");
        }

        private async Task<JsonObject> ReadSettingsFileAsync()
        {
            var content = await File.ReadAllTextAsync(this.settingsFilePath);
            return JsonNode.Parse(content)!.AsObject();
        }

        public class TestOptions
        {
            public string Name { get; set; } = string.Empty;

            public int Count { get; set; }
        }

        public void Dispose()
        {
            if (Directory.Exists(this.tempDirectory))
            {
                Directory.Delete(this.tempDirectory, true);
            }
        }
    }
}
