using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Superdev.AspNetCore.Options
{
    public sealed class WritableOptions<T> : IWritableOptions<T> where T : class, new()
    {
        private static readonly PropertyInfo[] WritableSectionProperties = typeof(T).GetProperties()
            .Where(p => p is { CanRead: true, CanWrite: true } && p.GetIndexParameters().Length == 0 && p.DeclaringType == typeof(T))
            .ToArray();

        private static readonly JsonReaderOptions JsonReaderOptions = new()
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };

        private static readonly JsonWriterOptions JsonWriterOptions = new()
        {
            Indented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        // Serializes read-modify-write cycles against the same appsettings file within this process.
        // Without it, two concurrent updates race on the file handle (and on last-writer-wins section
        // content). Keyed by file path so unrelated writable files don't contend with each other.
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> FileLocks = new(StringComparer.OrdinalIgnoreCase);

        private readonly IWebHostEnvironment environment;
        private readonly IConfiguration configuration;
        private readonly IOptionsMonitor<T> options;
        private readonly IOptionsMonitorCache<T> optionsMonitorCache;
        private readonly string section;
        private readonly string appsettingsFileName;
        private readonly JsonSerializerOptions jsonSerializerOptions;
        private readonly byte[] sectionUtf8;
        private byte[] currentJsonContent = Array.Empty<byte>();

        public WritableOptions(
            IWebHostEnvironment environment,
            IConfiguration configuration,
            IOptionsMonitor<T> options,
            IOptionsMonitorCache<T> optionsMonitorCache,
            string section,
            string appsettingsFileName)
        {
            this.environment = environment;
            this.configuration = configuration;
            this.options = options;
            this.optionsMonitorCache = optionsMonitorCache;
            this.section = section;
            this.appsettingsFileName = appsettingsFileName;
            this.sectionUtf8 = Encoding.UTF8.GetBytes(section);

            this.jsonSerializerOptions = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                PropertyNameCaseInsensitive = true,
                WriteIndented = true,
            };
            this.jsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        }

        /// <inheritdoc />
        public T Value => this.options.CurrentValue;

        /// <inheritdoc />
        public T Get(string? name) => this.options.Get(name);

        /// <inheritdoc />
        public Task UpdatePropertyAsync<TValue>(Expression<Func<T, TValue>> propertySelector, TValue value)
        {
            var propertyUpdater = PropertyUpdater<T, TValue>.GetPropertyUpdater(propertySelector);
            return this.UpdatePropertyAsync(propertyUpdater, value);
        }

        /// <inheritdoc />
        public Task UpdateAsync(Action<T> options)
        {
            return this.UpdateAsync(t =>
            {
                options(t);
                return t;
            });
        }

        /// <inheritdoc />
        public Task UpdateAsync(T options)
        {
            return this.UpdateAsync(_ => options);
        }

        /// <inheritdoc />
        public async Task UpdateAsync(Func<T, T> options)
        {
            var appsettingsFilePath = this.GetAppsettingsFilePath();
            var fileLock = GetFileLock(appsettingsFilePath);

            await fileLock.WaitAsync();
            try
            {
                var jsonContent = await this.ReadJsonContentAsync(appsettingsFilePath);
                var sectionObject = this.DeserializeSection(jsonContent);

                sectionObject = options(sectionObject);

                var serializedSection = this.SerializeSection(sectionObject);
                try
                {
                    await this.WriteUpdatedJsonAsync(appsettingsFilePath, jsonContent, writer => this.WriteRootObjectWithReplacedSection(writer, serializedSection));
                }
                finally
                {
                    serializedSection.Dispose();
                }

                this.RefreshConfiguration(sectionObject);
            }
            finally
            {
                fileLock.Release();
            }
        }

        private async Task UpdatePropertyAsync<TValue>(PropertyUpdater<T, TValue> propertyUpdater, TValue value)
        {
            var appsettingsFilePath = this.GetAppsettingsFilePath();
            var fileLock = GetFileLock(appsettingsFilePath);

            await fileLock.WaitAsync();
            try
            {
                var jsonContent = await this.ReadJsonContentAsync(appsettingsFilePath);
                var sectionObject = this.DeserializeSection(jsonContent);

                propertyUpdater.UpdateValue(sectionObject, value);

                await this.WriteUpdatedJsonAsync(appsettingsFilePath, jsonContent, writer => this.WriteRootObjectWithUpdatedProperty(writer, propertyUpdater.Name, value));

                this.RefreshConfiguration(sectionObject);
            }
            finally
            {
                fileLock.Release();
            }
        }

        private string GetAppsettingsFilePath()
        {
            return Path.Combine(this.environment.ContentRootPath, this.appsettingsFileName);
        }

        private static SemaphoreSlim GetFileLock(string filePath)
        {
            return FileLocks.GetOrAdd(filePath, _ => new SemaphoreSlim(1, 1));
        }

        // The configuration file is also read by the reload-on-change file watcher (and potentially
        // by other processes), so opening it for writing can transiently fail with a sharing violation
        // even when no other writer is active. Retry a few times with a short backoff to ride out that
        // window instead of surfacing the IOException to the caller.
        private static async Task ExecuteWithRetryAsync(Func<Task> action)
        {
            const int maxAttempts = 5;
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    await action();
                    return;
                }
                catch (Exception ex) when ((ex is IOException || ex is UnauthorizedAccessException) && attempt < maxAttempts)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(20 * attempt));
                }
            }
        }

        private async Task<byte[]> ReadJsonContentAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return Array.Empty<byte>();
            }

            var content = Array.Empty<byte>();
            await ExecuteWithRetryAsync(async () => content = await File.ReadAllBytesAsync(filePath));
            return content;
        }

        private async Task WriteUpdatedJsonAsync(string filePath, byte[] jsonContent, Action<Utf8JsonWriter> writeRootObject)
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await ExecuteWithRetryAsync(async () =>
            {
                await using var stream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

                if (HasUtf8Bom(jsonContent))
                {
                    await stream.WriteAsync(Encoding.UTF8.GetPreamble());
                }

                await using var writer = new Utf8JsonWriter(stream, JsonWriterOptions);
                writeRootObject(writer);
                await writer.FlushAsync();
                stream.SetLength(stream.Position);
            });
        }

        private void WriteRootObjectWithReplacedSection(Utf8JsonWriter writer, JsonDocument serializedSection)
        {
            using var root = this.GetRootObjectOrEmpty();
            writer.WriteStartObject();

            var isWritten = false;
            foreach (var element in root.RootElement.EnumerateObject())
            {
                if (!element.NameEquals(this.sectionUtf8))
                {
                    element.WriteTo(writer);
                    continue;
                }

                writer.WritePropertyName(this.section);
                serializedSection.RootElement.WriteTo(writer);
                isWritten = true;
            }

            if (!isWritten)
            {
                writer.WritePropertyName(this.section);
                serializedSection.RootElement.WriteTo(writer);
            }

            writer.WriteEndObject();
        }

        private void WriteRootObjectWithUpdatedProperty<TValue>(Utf8JsonWriter writer, string propertyName, TValue value)
        {
            using var root = this.GetRootObjectOrEmpty();
            writer.WriteStartObject();

            var isSectionWritten = false;
            foreach (var element in root.RootElement.EnumerateObject())
            {
                if (!element.NameEquals(this.sectionUtf8))
                {
                    element.WriteTo(writer);
                    continue;
                }

                writer.WritePropertyName(this.section);
                this.WriteSectionObjectWithUpdatedProperty(writer, element.Value, propertyName, value);
                isSectionWritten = true;
            }

            if (!isSectionWritten)
            {
                writer.WritePropertyName(this.section);
                writer.WriteStartObject();
                this.WritePropertyValue(writer, propertyName, value);
                writer.WriteEndObject();
            }

            writer.WriteEndObject();
        }

        private void WriteSectionObjectWithUpdatedProperty<TValue>(Utf8JsonWriter writer, JsonElement sectionElement, string propertyName, TValue value)
        {
            if (sectionElement.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException($"Configuration section '{this.section}' must contain a JSON object.");
            }

            writer.WriteStartObject();

            var isPropertyWritten = false;
            foreach (var property in sectionElement.EnumerateObject())
            {
                if (!string.Equals(property.Name, propertyName, StringComparison.Ordinal))
                {
                    property.WriteTo(writer);
                    continue;
                }

                this.WritePropertyValue(writer, propertyName, value);
                isPropertyWritten = true;
            }

            if (!isPropertyWritten)
            {
                this.WritePropertyValue(writer, propertyName, value);
            }

            writer.WriteEndObject();
        }

        private void WritePropertyValue<TValue>(Utf8JsonWriter writer, string propertyName, TValue value)
        {
            writer.WritePropertyName(propertyName);
            JsonSerializer.Serialize(writer, value, this.jsonSerializerOptions);
        }

        private JsonDocument GetRootObjectOrEmpty()
        {
            if (this.currentJsonContent.Length == 0)
            {
                return JsonDocument.Parse("{}");
            }

            var utf8Json = RemoveUtf8Bom(this.currentJsonContent);
            if (utf8Json.Length == 0 || IsOnlyWhitespace(utf8Json))
            {
                return JsonDocument.Parse("{}");
            }

            var reader = new Utf8JsonReader(utf8Json, JsonReaderOptions);
            var document = JsonDocument.ParseValue(ref reader);

            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                document.Dispose();
                throw new InvalidOperationException($"Configuration file '{this.GetAppsettingsFilePath()}' must contain a JSON object.");
            }

            return document;
        }

        private JsonDocument SerializeSection(T sectionObject)
        {
            var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
            {
                writer.WriteStartObject();

                var propertyInfos = typeof(T).GetProperties();
                var properties = propertyInfos
                    .Where(p => p.CanRead && p.GetIndexParameters().Length == 0 && p.DeclaringType == typeof(T))
                    .ToArray();

                foreach (var property in properties)
                {
                    var propertyValue = property.GetValue(sectionObject);
                    if (propertyValue == null)
                    {
                        continue;
                    }

                    try
                    {
                        var serializedProperty = JsonSerializer.SerializeToElement(propertyValue, property.PropertyType, this.jsonSerializerOptions);
                        writer.WritePropertyName(property.Name);
                        serializedProperty.WriteTo(writer);
                    }
                    catch (JsonException)
                    {
                        // Skip runtime-only properties that cannot be persisted as configuration.
                    }
                    catch (NotSupportedException)
                    {
                        // Skip runtime-only properties that cannot be persisted as configuration.
                    }
                }

                writer.WriteEndObject();
            }

            stream.Position = 0;
            return JsonDocument.Parse(stream);
        }

        private T DeserializeSection(byte[] jsonContent)
        {
            this.currentJsonContent = jsonContent;

            // Bind onto a fresh instance rather than the shared IOptionsMonitor.CurrentValue:
            // the configuration binder appends to existing collections/arrays instead of replacing
            // them, so binding the file's section onto an already-populated value would duplicate
            // every collection item (and would mutate the cached options instance in place).
            var sectionObject = new T();
            using var root = this.GetRootObjectOrEmpty();

            if (!root.RootElement.TryGetProperty(this.section, out var sectionElement))
            {
                // Nothing persisted for this section yet: fall back entirely to the current value.
                this.OverlayCurrentValue(sectionObject, persistedPropertyNames: null);
                return sectionObject;
            }

            if (sectionElement.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException($"Configuration section '{this.section}' must contain a JSON object.");
            }

            var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            Flatten(sectionElement, this.section, data);

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(data)
                .Build();

            configuration.GetSection(this.section).Bind(sectionObject);

            // The file is authoritative for the properties it specifies (binding above already
            // applied them). For every other top-level property, overlay the current value so that
            // settings coming from other configuration sources (environment variables, layered
            // appsettings files, ...) are preserved instead of being reset to their type default.
            var persistedPropertyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in sectionElement.EnumerateObject())
            {
                persistedPropertyNames.Add(property.Name);
            }

            this.OverlayCurrentValue(sectionObject, persistedPropertyNames);
            return sectionObject;
        }

        private void OverlayCurrentValue(T target, ISet<string>? persistedPropertyNames)
        {
            T? currentValue = null;

            foreach (var property in WritableSectionProperties)
            {
                if (persistedPropertyNames != null && persistedPropertyNames.Contains(property.Name))
                {
                    continue;
                }

                currentValue ??= this.Value;
                property.SetValue(target, property.GetValue(currentValue));
            }
        }

        private void RefreshConfiguration(T sectionObject)
        {
            if (this.configuration is IConfigurationRoot configurationRoot)
            {
                configurationRoot.Reload();
            }

            this.optionsMonitorCache.TryRemove(Microsoft.Extensions.Options.Options.DefaultName);
            this.optionsMonitorCache.TryAdd(Microsoft.Extensions.Options.Options.DefaultName, sectionObject);
        }

        private static void Flatten(JsonElement element, string path, IDictionary<string, string?> data)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var property in element.EnumerateObject())
                    {
                        Flatten(property.Value, $"{path}:{property.Name}", data);
                    }
                    break;
                case JsonValueKind.Array:
                    var index = 0;
                    foreach (var item in element.EnumerateArray())
                    {
                        Flatten(item, $"{path}:{index}", data);
                        index++;
                    }
                    break;
                case JsonValueKind.String:
                    data[path] = element.GetString();
                    break;
                case JsonValueKind.Number:
                case JsonValueKind.True:
                case JsonValueKind.False:
                case JsonValueKind.Null:
                    data[path] = element.ToString();
                    break;
            }
        }

        private static bool HasUtf8Bom(byte[] content)
        {
            return content.Length >= Encoding.UTF8.Preamble.Length &&
                   content.AsMemory(0, Encoding.UTF8.Preamble.Length).Span.SequenceEqual(Encoding.UTF8.Preamble);
        }

        private static ReadOnlySpan<byte> RemoveUtf8Bom(byte[] content)
        {
            if (!HasUtf8Bom(content))
            {
                return content;
            }

            return content.AsSpan(Encoding.UTF8.Preamble.Length);
        }

        private static bool IsOnlyWhitespace(ReadOnlySpan<byte> utf8Json)
        {
            foreach (var b in utf8Json)
            {
                if (!char.IsWhiteSpace((char)b))
                {
                    return false;
                }
            }

            return true;
        }

    }
}
