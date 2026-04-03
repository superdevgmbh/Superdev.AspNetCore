using System.Globalization;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Superdev.AspNetCore.Options;

namespace Superdev.AspNetCore.Tests.Options
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

            var sequence = new MockSequence();
            this.configurationRootMock.InSequence(sequence).Setup(x => x.Reload());
            this.optionsMonitorCacheMock.InSequence(sequence).Setup(x => x.TryRemove(Microsoft.Extensions.Options.Options.DefaultName)).Returns(true);
            this.optionsMonitorCacheMock.InSequence(sequence).Setup(x => x.TryAdd(Microsoft.Extensions.Options.Options.DefaultName, It.IsAny<TestOptions>())).Returns(true);

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

            this.optionsMonitorCacheMock.Verify(x => x.TryRemove(Microsoft.Extensions.Options.Options.DefaultName), Times.Once);
            this.optionsMonitorCacheMock.Verify(x => x.TryAdd(Microsoft.Extensions.Options.Options.DefaultName, It.Is<TestOptions>(o => o.Name == "New" && o.Count == 9)), Times.Once);
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

            var sequence = new MockSequence();
            this.configurationRootMock.InSequence(sequence).Setup(x => x.Reload());
            this.optionsMonitorCacheMock.InSequence(sequence).Setup(x => x.TryRemove(Microsoft.Extensions.Options.Options.DefaultName)).Returns(true);
            this.optionsMonitorCacheMock.InSequence(sequence).Setup(x => x.TryAdd(Microsoft.Extensions.Options.Options.DefaultName, It.IsAny<TestOptions>())).Returns(true);

            var sut = this.CreateWritableOptions();

            // Act
            await sut.UpdatePropertyAsync(x => x.Count, 7);

            // Assert
            var jsonObject = await this.ReadSettingsFileAsync();
            jsonObject["Test"]?["Name"]?.GetValue<string>().Should().Be("Original");
            jsonObject["Test"]?["Count"]?.GetValue<int>().Should().Be(7);

            this.optionsMonitorCacheMock.Verify(x => x.TryRemove(Microsoft.Extensions.Options.Options.DefaultName), Times.Once);
            this.optionsMonitorCacheMock.Verify(
                x => x.TryAdd(
                    Microsoft.Extensions.Options.Options.DefaultName,
                    It.Is<TestOptions>(o => o.Name == "Original" && o.Count == 7)),
                Times.Once);
            this.configurationRootMock.Verify(x => x.Reload(), Times.Once);
        }

        [Fact]
        public async Task ShouldUpdateSingleComplexPropertyWithoutSerializingWholeObjectGraph()
        {
            // Arrange
            await File.WriteAllTextAsync(this.settingsFilePath,
                """
                {
                  "Test": {
                    "Name": "Original",
                    "Count": 3,
                    "AccessPoint": {
                      "SSID": "old-ssid",
                      "PSK": "old-psk"
                    }
                  }
                }
                """);

            this.optionsMonitorMock.Setup(x => x.CurrentValue).Returns(() => new TestOptions
            {
                Name = "Current",
                Count = 1,
                AccessPoint = new AccessPointSettings
                {
                    SSID = "current-ssid",
                    PSK = "current-psk",
                },
                CultureInfo = CultureInfo.InvariantCulture,
            });
            this.optionsMonitorMock.Setup(x => x.Get(It.IsAny<string?>())).Returns(() => new TestOptions
            {
                Name = "Current",
                Count = 1,
                AccessPoint = new AccessPointSettings
                {
                    SSID = "current-ssid",
                    PSK = "current-psk",
                },
                CultureInfo = CultureInfo.InvariantCulture,
            });

            var sut = this.CreateWritableOptions();

            // Act
            await sut.UpdatePropertyAsync(x => x.AccessPoint, new AccessPointSettings
            {
                SSID = "new-ssid",
                PSK = "new-psk",
            });

            // Assert
            var jsonObject = await this.ReadSettingsFileAsync();
            jsonObject["Test"]?["Name"]?.GetValue<string>().Should().Be("Original");
            jsonObject["Test"]?["Count"]?.GetValue<int>().Should().Be(3);
            jsonObject["Test"]?["AccessPoint"]?["SSID"]?.GetValue<string>().Should().Be("new-ssid");
            jsonObject["Test"]?["AccessPoint"]?["PSK"]?.GetValue<string>().Should().Be("new-psk");
        }

        [Fact]
        public async Task ShouldUpdateScalarPropertyWithoutSerializingWholeObjectGraph()
        {
            // Arrange
            await File.WriteAllTextAsync(this.settingsFilePath,
                """
                {
                  "Test": {
                    "Name": "Original",
                    "RunSetup": true,
                    "ButtonMappings": [
                      {
                        "Page": "PageA",
                        "ButtonId": 1,
                        "GpioPin": 6,
                        "Default": true
                      }
                    ]
                  }
                }
                """);

            this.optionsMonitorMock.Setup(x => x.CurrentValue).Returns(() => new TestOptions
            {
                Name = "Current",
                RunSetup = true,
                ButtonMappings = new List<ButtonMappingSettings>
                {
                    new()
                    {
                        Page = "PageA",
                        ButtonId = 1,
                        GpioPin = 6,
                        Default = true,
                    }
                },
                CultureInfo = CultureInfo.InvariantCulture,
            });
            this.optionsMonitorMock.Setup(x => x.Get(It.IsAny<string?>())).Returns(() => new TestOptions
            {
                Name = "Current",
                RunSetup = true,
                ButtonMappings = new List<ButtonMappingSettings>
                {
                    new()
                    {
                        Page = "PageA",
                        ButtonId = 1,
                        GpioPin = 6,
                        Default = true,
                    }
                },
                CultureInfo = CultureInfo.InvariantCulture,
            });

            var sut = this.CreateWritableOptions();

            // Act
            await sut.UpdatePropertyAsync(x => x.RunSetup, false);

            // Assert
            var jsonObject = await this.ReadSettingsFileAsync();
            jsonObject["Test"]?["Name"]?.GetValue<string>().Should().Be("Original");
            jsonObject["Test"]?["RunSetup"]?.GetValue<bool>().Should().BeFalse();
            jsonObject["Test"]?["ButtonMappings"]?[0]?["Page"]?.GetValue<string>().Should().Be("PageA");
        }

        [Fact]
        public async Task ShouldUpdateCollectionPropertyWithComplexItems()
        {
            // Arrange
            await File.WriteAllTextAsync(this.settingsFilePath,
                """
                {
                  "Test": {
                    "RunSetup": true,
                    "ButtonMappings": []
                  },
                  "Other": {
                    "Enabled": true
                  }
                }
                """);

            var sut = this.CreateWritableOptions();
            var buttonMappings = new List<ButtonMappingSettings>
            {
                new()
                {
                    Page = "MeteoSwissWeatherPage",
                    ButtonId = 1,
                    GpioPin = 6,
                    Default = false,
                },
                new()
                {
                    Page = "MeteoSwissWeatherStationPage",
                    ButtonId = 2,
                    GpioPin = 5,
                    Default = true,
                }
            };

            // Act
            await sut.UpdatePropertyAsync(x => x.ButtonMappings, buttonMappings);

            // Assert
            var jsonObject = await this.ReadSettingsFileAsync();
            jsonObject["Other"]?["Enabled"]?.GetValue<bool>().Should().BeTrue();
            jsonObject["Test"]?["ButtonMappings"]?.AsArray().Count.Should().Be(2);
            jsonObject["Test"]?["ButtonMappings"]?[0]?["Page"]?.GetValue<string>().Should().Be("MeteoSwissWeatherPage");
            jsonObject["Test"]?["ButtonMappings"]?[1]?["Default"]?.GetValue<bool>().Should().BeTrue();
        }

        [Fact]
        public async Task ShouldCreateMissingConfigurationFileWhenUpdatingWholeOptions()
        {
            // Arrange
            var sut = this.CreateWritableOptions();

            // Act
            await sut.UpdateAsync(new TestOptions
            {
                Name = "Created",
                RunSetup = true,
                AccessPoint = new AccessPointSettings
                {
                    SSID = "created-ssid",
                    PSK = "created-psk",
                },
                ButtonMappings = new List<ButtonMappingSettings>
                {
                    new()
                    {
                        Page = "SetupPage",
                        ButtonId = 3,
                        GpioPin = 16,
                        Default = true,
                    }
                },
                CultureInfo = CultureInfo.InvariantCulture,
            });

            // Assert
            File.Exists(this.settingsFilePath).Should().BeTrue();

            var jsonObject = await this.ReadSettingsFileAsync();
            jsonObject["Test"]?["Name"]?.GetValue<string>().Should().Be("Created");
            jsonObject["Test"]?["RunSetup"]?.GetValue<bool>().Should().BeTrue();
            jsonObject["Test"]?["AccessPoint"]?["SSID"]?.GetValue<string>().Should().Be("created-ssid");
            jsonObject["Test"]?["ButtonMappings"]?[0]?["ButtonId"]?.GetValue<int>().Should().Be(3);
            jsonObject["Test"]?["CultureInfo"].Should().BeNull();
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

            public bool RunSetup { get; set; }

            public AccessPointSettings AccessPoint { get; set; } = new();

            public ICollection<ButtonMappingSettings> ButtonMappings { get; set; } = new List<ButtonMappingSettings>();

            public CultureInfo CultureInfo { get; set; } = CultureInfo.InvariantCulture;
        }

        public class AccessPointSettings
        {
            public string SSID { get; set; } = string.Empty;

            public string PSK { get; set; } = string.Empty;
        }

        public class ButtonMappingSettings
        {
            public string Page { get; set; } = string.Empty;

            public int ButtonId { get; set; }

            public int GpioPin { get; set; }

            public bool Default { get; set; }
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
