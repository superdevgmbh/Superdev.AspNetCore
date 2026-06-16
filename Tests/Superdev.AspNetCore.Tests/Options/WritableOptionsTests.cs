using System.Globalization;
using System.Linq.Expressions;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Superdev.AspNetCore.Options;

namespace Superdev.AspNetCore.Tests.Options
{
    [Trait(Traits.Category, Traits.UnitTests)]
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
            this.optionsMonitorMock.Setup(x => x.CurrentValue).Returns(() => this.CreateCurrentOptions());
            this.optionsMonitorMock.Setup(x => x.Get(It.IsAny<string?>())).Returns(() => this.CreateCurrentOptions());

            this.optionsMonitorCacheMock = new Mock<IOptionsMonitorCache<TestOptions>>();
            this.optionsMonitorCacheMock.Setup(x => x.TryRemove(It.IsAny<string>())).Returns(true);
            this.optionsMonitorCacheMock.Setup(x => x.TryAdd(It.IsAny<string>(), It.IsAny<TestOptions>())).Returns(true);
        }

        [Fact]
        public async Task UpdateAsync_WhenReplacingWholeSection_PreservesOtherSections()
        {
            // Arrange
            await this.WriteSettingsAsync(
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

            var sut = this.CreateWritableOptions();

            // Act
            await sut.UpdateAsync(new TestOptions
            {
                Name = "New",
                Count = 9,
            });

            // Assert
            var jsonObject = await this.ReadSettingsFileAsync();
            jsonObject["Other"]?["Enabled"]?.GetValue<bool>().Should().BeTrue();
            jsonObject["Test"]?["Name"]?.GetValue<string>().Should().Be("New");
            jsonObject["Test"]?["Count"]?.GetValue<int>().Should().Be(9);
            this.VerifyReloadAndCacheRefresh(Times.Once());
        }

        [Fact]
        public async Task UpdatePropertyAsync_WhenUpdatingScalarProperty_PreservesRemainingValues()
        {
            // Arrange
            await this.WriteSettingsAsync(
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
            this.optionsMonitorCacheMock.Verify(
                x => x.TryAdd(
                    Microsoft.Extensions.Options.Options.DefaultName,
                    It.Is<TestOptions>(o => o.Name == "Original" && o.Count == 7)),
                Times.Once);
            this.configurationRootMock.Verify(x => x.Reload(), Times.Once);
        }

        [Fact]
        public async Task UpdatePropertyAsync_WhenUpdatingSectionProperty_PreservesUnrelatedSections()
        {
            // Arrange
            await this.WriteSettingsAsync(
                """
                {
                  "Test": {
                    "Count": 3
                  },
                  "Other": {
                    "Enabled": true
                  }
                }
                """);

            var sut = this.CreateWritableOptions();

            // Act
            await sut.UpdatePropertyAsync(x => x.Count, 7);

            // Assert
            var jsonObject = await this.ReadSettingsFileAsync();
            jsonObject["Other"]?["Enabled"]?.GetValue<bool>().Should().BeTrue();
            jsonObject["Test"]?["Count"]?.GetValue<int>().Should().Be(7);
        }

        [Fact]
        public async Task UpdatePropertyAsync_WhenUpdatingComplexProperty_DoesNotRewriteSiblingValues()
        {
            // Arrange
            await this.WriteSettingsAsync(
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
        public async Task UpdatePropertyAsync_WhenUpdatingScalarValue_DoesNotRewriteCollections()
        {
            // Arrange
            await this.WriteSettingsAsync(
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
        public async Task UpdatePropertyAsync_WhenUpdatingCollection_WritesComplexItems()
        {
            // Arrange
            await this.WriteSettingsAsync(
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
        public async Task UpdateAsync_WhenDelegateLeavesCollectionUntouched_DoesNotDuplicateCollectionItems()
        {
            // Arrange: the file already contains the same single collection item that the
            // current options value (IOptionsMonitor.CurrentValue) exposes.
            await this.WriteSettingsAsync(
                """
                {
                  "Test": {
                    "Name": "Original",
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

            var sut = this.CreateWritableOptions();

            // Act: the delegate only mutates a scalar and leaves the collection untouched.
            await sut.UpdateAsync(o => o.Name = "Updated");

            // Assert: the collection must not be duplicated.
            var jsonObject = await this.ReadSettingsFileAsync();
            jsonObject["Test"]?["Name"]?.GetValue<string>().Should().Be("Updated");
            jsonObject["Test"]?["ButtonMappings"]?.AsArray().Count.Should().Be(1);
            jsonObject["Test"]?["ButtonMappings"]?[0]?["Page"]?.GetValue<string>().Should().Be("PageA");
        }

        [Fact]
        public async Task UpdateAsync_WhenAppliedRepeatedly_DoesNotAccumulateCollectionItems()
        {
            // Arrange
            await this.WriteSettingsAsync(
                """
                {
                  "Test": {
                    "Name": "Original",
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

            var sut = this.CreateWritableOptions();

            // Act: apply several updates that never touch the collection.
            await sut.UpdateAsync(o => o.Count = 1);
            await sut.UpdateAsync(o => o.Count = 2);
            await sut.UpdateAsync(o => o.Count = 3);

            // Assert: the collection size stays stable.
            var jsonObject = await this.ReadSettingsFileAsync();
            jsonObject["Test"]?["Count"]?.GetValue<int>().Should().Be(3);
            jsonObject["Test"]?["ButtonMappings"]?.AsArray().Count.Should().Be(1);
        }

        [Fact]
        public async Task UpdateAsync_WhenScalarIsAbsentFromFile_PreservesCurrentScalarValue()
        {
            // Arrange: the writable file owns only the collection. The RunSetup ("Enabled"-style)
            // flag is supplied by another configuration source and is therefore present on the
            // current options value (CreateCurrentOptions -> RunSetup = true) but NOT in the file.
            await this.WriteSettingsAsync(
                """
                {
                  "Test": {
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

            var sut = this.CreateWritableOptions();

            // Act: the update only touches the collection and must not affect RunSetup.
            await sut.UpdateAsync(o => o.ButtonMappings = new List<ButtonMappingSettings>
            {
                new ButtonMappingSettings
                {
                    Page = "PageA",
                    ButtonId = 1,
                    GpioPin = 6,
                    Default = true
                },
            });

            // Assert: the flag must survive; it must not be reset to its default (false).
            var jsonObject = await this.ReadSettingsFileAsync();
            jsonObject["Test"]?["RunSetup"]?.GetValue<bool>().Should().BeTrue();
        }

        [Fact]
        public async Task UpdateAsync_WhenCollectionIsAbsentFromFile_PreservesCurrentCollection()
        {
            // Arrange: the file owns only a scalar; the collection is supplied elsewhere and is
            // therefore present on the current value (CreateCurrentOptions -> one ButtonMapping).
            await this.WriteSettingsAsync(
                """
                {
                  "Test": {
                    "Name": "Original"
                  }
                }
                """);

            var sut = this.CreateWritableOptions();

            // Act: only the scalar is updated; the collection must be preserved, not emptied.
            await sut.UpdateAsync(o => o.Name = "Updated");

            // Assert
            var jsonObject = await this.ReadSettingsFileAsync();
            jsonObject["Test"]?["Name"]?.GetValue<string>().Should().Be("Updated");
            jsonObject["Test"]?["ButtonMappings"]?.AsArray().Count.Should().Be(1);
            jsonObject["Test"]?["ButtonMappings"]?[0]?["Page"]?.GetValue<string>().Should().Be("PageA");
        }

        [Fact]
        public async Task UpdateAsync_WhenFileIsMissing_CreatesConfigurationFile()
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

        [Fact]
        public async Task UpdateAsync_WhenFileIsEmpty_TreatsContentAsEmptyJsonObject()
        {
            // Arrange
            await this.WriteSettingsAsync(string.Empty);

            var sut = this.CreateWritableOptions();

            // Act
            await sut.UpdateAsync(new TestOptions
            {
                Name = "Created",
                Count = 11,
            });

            // Assert
            var jsonObject = await this.ReadSettingsFileAsync();
            jsonObject["Test"]?["Name"]?.GetValue<string>().Should().Be("Created");
            jsonObject["Test"]?["Count"]?.GetValue<int>().Should().Be(11);
        }

        [Theory]
        [MemberData(nameof(TolerantJsonInputs))]
        public async Task UpdateAsync_WhenJsonUsesTolerantFormatting_ParsesAndWritesSuccessfully(string fileContent, Encoding? encoding)
        {
            // Arrange
            await this.WriteSettingsAsync(fileContent, encoding);

            var sut = this.CreateWritableOptions();

            // Act
            await sut.UpdateAsync(new TestOptions
            {
                Name = "Updated",
                Count = 42,
            });

            // Assert
            var jsonObject = await this.ReadSettingsFileAsync();
            jsonObject["Test"]?["Name"]?.GetValue<string>().Should().Be("Updated");
            jsonObject["Test"]?["Count"]?.GetValue<int>().Should().Be(42);
            jsonObject["Other"]?["Enabled"]?.GetValue<bool>().Should().BeTrue();
        }

        [Theory]
        [MemberData(nameof(TolerantJsonInputs))]
        public async Task UpdatePropertyAsync_WhenJsonUsesTolerantFormatting_ParsesAndWritesSuccessfully(string fileContent, Encoding? encoding)
        {
            // Arrange
            await this.WriteSettingsAsync(fileContent, encoding);

            var sut = this.CreateWritableOptions();

            // Act
            await sut.UpdatePropertyAsync(x => x.Count, 8);

            // Assert
            var jsonObject = await this.ReadSettingsFileAsync();
            jsonObject["Test"]?["Count"]?.GetValue<int>().Should().Be(8);
            jsonObject["Other"]?["Enabled"]?.GetValue<bool>().Should().BeTrue();
        }

        [Fact]
        public async Task UpdateAsync_WhenRootJsonIsNotAnObject_ThrowsInvalidOperationException()
        {
            // Arrange
            await this.WriteSettingsAsync(
                """
                [
                  {
                    "Name": "Wrong"
                  }
                ]
                """);

            var sut = this.CreateWritableOptions();

            // Act
            var act = () => sut.UpdateAsync(new TestOptions { Name = "Updated" });

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage($"Configuration file '{this.settingsFilePath}' must contain a JSON object.");
        }

        [Fact]
        public async Task UpdateAsync_WhenConfiguredSectionIsNotAnObject_ThrowsInvalidOperationException()
        {
            // Arrange
            await this.WriteSettingsAsync(
                """
                {
                  "Test": true
                }
                """);

            var sut = this.CreateWritableOptions();

            // Act
            var act = () => sut.UpdateAsync(new TestOptions { Name = "Updated" });

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Configuration section 'Test' must contain a JSON object.");
        }

        [Fact]
        public async Task UpdatePropertyAsync_WhenConfiguredSectionIsNotAnObject_ThrowsInvalidOperationException()
        {
            // Arrange
            await this.WriteSettingsAsync(
                """
                {
                  "Test": true
                }
                """);

            var sut = this.CreateWritableOptions();

            // Act
            var act = () => sut.UpdatePropertyAsync(x => x.Count, 5);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Configuration section 'Test' must contain a JSON object.");
        }

        [Fact]
        public async Task UpdatePropertyAsync_WhenValueIsNull_WritesJsonNull()
        {
            // Arrange
            await this.WriteSettingsAsync(
                """
                {
                  "Test": {
                    "AccessPoint": {
                      "SSID": "old-ssid",
                      "PSK": "old-psk"
                    }
                  }
                }
                """);

            var sut = this.CreateWritableOptions();

            // Act
            await sut.UpdatePropertyAsync(x => x.AccessPoint, null!);

            // Assert
            var jsonObject = await this.ReadSettingsFileAsync();
            jsonObject["Test"]?["AccessPoint"]?.GetValueKind().Should().Be(JsonValueKind.Null);
        }

        [Fact]
        public async Task UpdatePropertyAsync_WhenSelectorTargetsNestedProperty_RejectsExpression()
        {
            // Arrange
            await this.WriteSettingsAsync(
                """
                {
                  "Test": {
                    "AccessPoint": {
                      "SSID": "old-ssid",
                      "PSK": "old-psk"
                    }
                  }
                }
                """);

            var sut = this.CreateWritableOptions();
            Expression<Func<TestOptions, string>> selector = x => x.AccessPoint!.SSID;

            // Act
            var act = () => sut.UpdatePropertyAsync(selector, "new-ssid");

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithParameterName("propertySelector")
                .WithMessage("Only direct top-level properties are supported.*");
        }

        [Fact]
        public async Task UpdatePropertyAsync_WhenSelectorIsNotPropertyAccess_RejectsExpression()
        {
            // Arrange
            await this.WriteSettingsAsync(
                """
                {
                  "Test": {
                    "Count": 3
                  }
                }
                """);

            var sut = this.CreateWritableOptions();
            Expression<Func<TestOptions, int>> selector = x => x.Count + 1;

            // Act
            var act = () => sut.UpdatePropertyAsync(selector, 7);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithParameterName("propertySelector")
                .WithMessage("The property selector must target a direct property access.*");
        }

        public static IEnumerable<object?[]> TolerantJsonInputs()
        {
            yield return
            [
                """
                {
                  "Test": {
                    "Name": "Original",
                    "Count": 3
                  },
                  "Other": {
                    "Enabled": true
                  }
                }
                """,
                null
            ];

            yield return
            [
                """
                {
                  // Line comment
                  "Test": {
                    "Name": "Original",
                    "Count": 3
                  },
                  "Other": {
                    "Enabled": true
                  }
                }
                """,
                null
            ];

            yield return
            [
                """
                {
                  "Test": {
                    /*
                      Block comment
                    */
                    "Name": "Original",
                    "Count": 3
                  },
                  "Other": {
                    "Enabled": true
                  }
                }
                """,
                null
            ];

            yield return
            [
                """
                {
                  "Test": {
                    "Name": "Original",
                    "Count": 3,
                  },
                  "Other": {
                    "Enabled": true,
                  },
                }
                """,
                null
            ];

            yield return
            [
                """
                {
                  "Test": {
                    "Name": "Original",
                    "Count": 3
                  },
                  "Other": {
                    "Enabled": true
                  }
                }
                """,
                new UTF8Encoding(true)
            ];
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

        private TestOptions CreateCurrentOptions()
        {
            return new TestOptions
            {
                Name = "Current",
                Count = 1,
                RunSetup = true,
                AccessPoint = new AccessPointSettings
                {
                    SSID = "current-ssid",
                    PSK = "current-psk",
                },
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
            };
        }

        private async Task WriteSettingsAsync(string content, Encoding? encoding = null)
        {
            encoding ??= new UTF8Encoding(false);
            await File.WriteAllTextAsync(this.settingsFilePath, content, encoding);
        }

        private async Task<JsonObject> ReadSettingsFileAsync()
        {
            var content = await File.ReadAllTextAsync(this.settingsFilePath);
            return JsonNode.Parse(content)!.AsObject();
        }

        private void VerifyReloadAndCacheRefresh(Times times)
        {
            this.optionsMonitorCacheMock.Verify(x => x.TryRemove(Microsoft.Extensions.Options.Options.DefaultName), times);
            this.optionsMonitorCacheMock.Verify(x => x.TryAdd(Microsoft.Extensions.Options.Options.DefaultName, It.IsAny<TestOptions>()), times);
            this.configurationRootMock.Verify(x => x.Reload(), times);
        }

        public class TestOptions
        {
            public string Name { get; set; } = string.Empty;

            public int Count { get; set; }

            public bool RunSetup { get; set; }

            public AccessPointSettings? AccessPoint { get; set; } = new();

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
