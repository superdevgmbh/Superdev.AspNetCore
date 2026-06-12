using System.Globalization;
using System.Text.Json;

namespace Superdev.AspNetCore.Tests
{
    [Trait(Traits.Category, Traits.UnitTests)]
    public class LocalizedStringTests : IDisposable
    {
        private readonly ITestOutputHelper testOutputHelper;

        public LocalizedStringTests(ITestOutputHelper testOutputHelper)
        {
            this.testOutputHelper = testOutputHelper;
            LocalizedString.KeyNotFoundStrategy = KeyNotFoundStrategy.ReturnDefaultCulture;
        }

        public void Dispose()
        {
            LocalizedString.KeyNotFoundStrategy = KeyNotFoundStrategy.ReturnDefaultCulture;
        }

        [Fact]
        public void ShouldSerializeLocalizedString()
        {
            // Arrange
            var input = new LocalizedStringTestClass
            {
                Title = new LocalizedString
                {
                    ["en"] = "Title",
                    ["de"] = "Titel"
                },
                Description = new LocalizedString
                {
                    ["en"] = "Description",
                    ["de"] = "Beschreibung"
                }
            };

            // Act
            var jsonOutput = JsonSerializer.Serialize(input);

            // Assert
            this.testOutputHelper.WriteLine(jsonOutput);

            jsonOutput.Should().Be(
                "{\"Title\":{\"en\":\"Title\",\"de\":\"Titel\"},\"Description\":{\"en\":\"Description\",\"de\":\"Beschreibung\"}}");
        }

        [Fact]
        public void ShouldDeserializeLocalizedString()
        {
            // Arrange
            var jsonInput = "{\"Title\":{\"en\":\"Title\",\"de\":\"Titel\"},\"Description\":{\"en\":\"Description\",\"de\":\"Beschreibung\"}}";

            // Act
            var output = JsonSerializer.Deserialize<LocalizedStringTestClass>(jsonInput);

            // Assert
            output.Should().NotBeNull();
            output.Title.Should().NotBeNull();
            output.Title["en"].Should().Be("Title");
            output.Title["de"].Should().Be("Titel");
            output.Description.Should().NotBeNull();
            output.Description["en"].Should().Be("Description");
            output.Description["de"].Should().Be("Beschreibung");
        }

        [Fact]
        public void ShouldSerializeLocalizedString_WithFallback()
        {
            // Arrange
            var input = new LocalizedStringTestClass
            {
                Title = new LocalizedString
                {
                    ["en"] = "Title",
                    [null] = "Fallback title"
                }
            };

            // Act
            var jsonOutput = JsonSerializer.Serialize(input);

            // Assert
            jsonOutput.Should().Be(
                "{\"Title\":{\"en\":\"Title\",\"null\":\"Fallback title\"},\"Description\":null}");
        }

        [Fact]
        public void ShouldDeserializeLocalizedString_WithFallback()
        {
            // Arrange
            var jsonInput = "{\"Title\":{\"en\":\"Title\",\"null\":\"Fallback title\"},\"Description\":null}";

            // Act
            var output = JsonSerializer.Deserialize<LocalizedStringTestClass>(jsonInput);

            // Assert
            output.Should().NotBeNull();
            output.Title.Should().NotBeNull();
            output.Title["en"].Should().Be("Title");
            output.Title[null].Should().Be("Fallback title");
            output.Title["fr"].Should().Be("Fallback title");
            output.Description.Should().BeNull();
        }

        [Fact]
        public void ShouldGetString_ImplicitOperator()
        {
            // Arrange
            var localizedString = new LocalizedString
            {
                ["en"] = "Title",
                ["de"] = "Titel"
            };

            // Act
            string? stringValue = localizedString;

            // Assert
            stringValue.Should().Be("Title");
        }

        [Fact]
        public void ShouldGetString_ImplicitOperator_NullReturnsNull()
        {
            // Arrange
            LocalizedString? localizedString = null;

            // Act
            string? stringValue = localizedString;

            // Assert
            stringValue.Should().BeNull();
        }

        [Fact]
        public void ShouldCreateFromString_ImplicitOperator()
        {
            // Arrange
            const string stringValue = "Fallback title";

            // Act
            LocalizedString? localizedString = stringValue;

            // Assert
            localizedString.Should().NotBeNull();
            localizedString.Should().BeEmpty();
            localizedString[null].Should().Be("Fallback title");
            localizedString["en"].Should().Be("Fallback title");
            localizedString["de-CH"].Should().Be("Fallback title");
        }

        [Fact]
        public void ShouldCreateFromString_ImplicitOperator_NullReturnsNull()
        {
            // Arrange
            const string? stringValue = null;

            // Act
            LocalizedString? localizedString = stringValue;

            // Assert
            localizedString.Should().BeNull();
        }


        [Fact]
        public void ShouldGetString_ArrayIndex_NullReturnsDefault()
        {
            // Arrange
            var localizedString = new LocalizedString
            {
                ["en"] = "Title",
                ["de"] = "Titel"
            };

            // Act
            var stringValue = localizedString[null];

            // Assert
            stringValue.Should().Be("Title");
        }

        [Fact]
        public void ShouldGetString_ArrayIndex_NullReturnsFallback()
        {
            // Arrange
            var localizedString = new LocalizedString
            {
                ["en"] = "Title",
                [null] = "Fallback title"
            };

            // Act
            var stringValue = localizedString[null];

            // Assert
            stringValue.Should().Be("Fallback title");
        }

        [Fact]
        public void ShouldGetString_ArrayIndex()
        {
            // Arrange
            var localizedString = new LocalizedString
            {
                ["en"] = "Title",
                ["de"] = "Titel"
            };

            // Act
            var stringValue = localizedString["DE"];

            // Assert
            stringValue.Should().Be("Titel");
        }

        [Fact]
        public void ShouldGetString_ArrayIndex_SpecificCulture()
        {
            // Arrange
            var localizedString = new LocalizedString
            {
                ["de"] = "Titel",
                ["de-DE"] = "Titel (DE)",
            };

            // Act
            var stringValue = localizedString["de-DE"];

            // Assert
            stringValue.Should().Be("Titel (DE)");
        }

        [Fact]
        public void ShouldGetString_ArrayIndex_MissingCultureReturnsFallback()
        {
            // Arrange
            var localizedString = new LocalizedString
            {
                ["en"] = "Title",
                [null] = "Fallback title"
            };

            // Act
            var stringValue = localizedString["fr-FR"];

            // Assert
            stringValue.Should().Be("Fallback title");
        }

        [Fact]
        public void ShouldGetString_ArrayIndex_MissingCulturePrefersCultureOverFallback()
        {
            // Arrange
            var localizedString = new LocalizedString
            {
                ["de"] = "Titel",
                [null] = "Fallback title"
            };

            // Act
            var stringValue = localizedString["de-CH"];

            // Assert
            stringValue.Should().Be("Titel");
        }

        [Fact]
        public void ShouldGetString_ToString_WithDefaultCulture()
        {
            // Arrange
            var localizedString = new LocalizedString
            {
                ["en"] = "Title",
                ["de"] = "Titel"
            };

            // Act
            var stringValue = localizedString.ToString();

            // Assert
            stringValue.Should().Be("Title");
        }

        [Fact]
        public void ShouldGetString_ToString_WithCustomCulture_deDE()
        {
            // Arrange
            var localizedString = new LocalizedString
            {
                ["en"] = "Title",
                ["de"] = "Titel"
            };

            var cultureInfo = new CultureInfo("de-DE");

            // Act
            var stringValue = localizedString.ToString(cultureInfo);

            // Assert
            stringValue.Should().Be("Titel");
        }

        [Fact]
        public void ShouldGetString_ToString_WithNonExistingCulture()
        {
            // Arrange
            LocalizedString.KeyNotFoundStrategy = KeyNotFoundStrategy.Throw;

            var localizedString = new LocalizedString
            {
                ["en"] = "Title",
                ["de"] = "Titel"
            };

            var cultureInfo = new CultureInfo("fr-FR");

            // Act
            var stringValue = localizedString.ToString(cultureInfo);

            // Assert
            stringValue.Should().Be("Title");
        }

        [Fact]
        public void ShouldGetString_ToString_WithNonExistingCultureAndFallback_WhenThrowEnabled()
        {
            // Arrange
            LocalizedString.KeyNotFoundStrategy = KeyNotFoundStrategy.Throw;

            var localizedString = new LocalizedString
            {
                [null] = "Fallback title"
            };

            var cultureInfo = new CultureInfo("fr-FR");

            // Act
            var stringValue = localizedString.ToString(cultureInfo);

            // Assert
            stringValue.Should().Be("Fallback title");
        }

        [Fact]
        public void ShouldThrow_WhenCultureAndFallbackAreMissing_AndThrowEnabled()
        {
            // Arrange
            LocalizedString.KeyNotFoundStrategy = KeyNotFoundStrategy.Throw;

            var localizedString = new LocalizedString
            {
                ["de"] = "Titel"
            };

            var cultureInfo = new CultureInfo("fr-FR");

            // Act
            Action action = () => localizedString.ToString(cultureInfo);

            // Assert
            action.Should().Throw<KeyNotFoundException>();
        }

        [Fact]
        public void ShouldSetValues_Constructor()
        {
            // Arrange
            var dict = new Dictionary<string, string>
            {
                ["en"] = "Title",
                ["de"] = "Titel"
            };

            // Act
            var localizedString = new LocalizedString(dict);

            // Assert
            localizedString.Should().HaveCount(2);
            localizedString.Should().Contain(v => v.Key == "en" && v.Value == "Title");
            localizedString.Should().Contain(v => v.Key == "de" && v.Value == "Titel");
        }

        [Fact]
        public void ShouldSetValue_ArrayIndex()
        {
            // Arrange
            var localizedString = new LocalizedString();

            // Act
            localizedString["en"] = "Title";

            // Assert
            localizedString.Should().HaveCount(1);
            localizedString.Should().Contain(v => v.Key == "en" && v.Value == "Title");
        }

        [Fact]
        public void ShouldSetValue_ArrayIndex_NullStoresFallback()
        {
            // Arrange
            var localizedString = new LocalizedString();

            // Act
            localizedString[null] = "Fallback title";

            // Assert
            localizedString.Should().BeEmpty();
            localizedString[null].Should().Be("Fallback title");
            localizedString["fr"].Should().Be("Fallback title");
        }

        [Fact]
        public void ShouldSetValue_CollectionInitializer_NullStoresFallback()
        {
            // Arrange
            var localizedString = new LocalizedString
            {
                { null, "Fallback title" },
                { "en", "Title EN" },
                { "de", "Title DE" },
            };

            // Assert
            localizedString.Should().NotBeNullOrEmpty();
            localizedString["en"].Should().Be("Title EN");
            localizedString["de"].Should().Be("Title DE");
            localizedString["fr"].Should().Be("Fallback title");
            localizedString[null].Should().Be("Fallback title");
        }

        [Fact]
        public void ShouldSetValue_ArrayIndex_SpecificCulture()
        {
            // Arrange
            var localizedString = new LocalizedString();

            // Act
            localizedString["en"] = "Title";
            localizedString["en-US"] = "Title (US)";

            // Assert
            localizedString.Should().HaveCount(2);
            localizedString.Should().Contain(v => v.Key == "en" && v.Value == "Title");
            localizedString.Should().Contain(v => v.Key == "en-US" && v.Value == "Title (US)");
        }

        public class LocalizedStringTestClass
        {
            public LocalizedString? Title { get; init; }

            public LocalizedString? Description { get; init; }
        }
    }
}
