using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Superdev.AspNetCore.Infrastructure.Configuration
{
    public class WritableOptions<T> : IWritableOptions<T> where T : class, new()
    {
        private readonly IWebHostEnvironment environment;
        private readonly IConfiguration configuration;
        private readonly IOptionsMonitor<T> options;
        private readonly IOptionsMonitorCache<T> optionsMonitorCache;
        private readonly string section;
        private readonly string appsettingsFileName;
        private readonly JsonSerializerOptions jsonSerializerOptions;

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

            this.jsonSerializerOptions = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                PropertyNameCaseInsensitive = true,
                WriteIndented = true,
            };
            this.jsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        }

        public T Value => this.options.CurrentValue;

        public T Get(string? name) => this.options.Get(name);

        public Task UpdatePropertyAsync<TValue>(Expression<Func<T, TValue>> propertySelector, TValue value)
        {
            var propertyUpdater = PropertyUpdater<T, TValue>.GetPropertyUpdater(() => propertySelector);

            return this.UpdateAsync(options =>
            {
                propertyUpdater.UpdateValue(options, value);
                return options;
            });
        }

        public Task UpdateAsync(Action<T> options)
        {
            return this.UpdateAsync(t =>
            {
                options(t);
                return t;
            });
        }

        public Task UpdateAsync(T options)
        {
            return this.UpdateAsync(_ => options);
        }

        public async Task UpdateAsync(Func<T, T> options)
        {
            var appsettingsFilePath = this.GetAppsettingsFilePath();
            var appsettingsJsonObject = await GetJsonContentAsync(appsettingsFilePath);
            var sectionObject = this.DeserializeSection(appsettingsJsonObject);

            sectionObject = options(sectionObject);

            appsettingsJsonObject[this.section] = this.SerializeSection(sectionObject);

            var updatedFileContent = appsettingsJsonObject.ToJsonString(this.jsonSerializerOptions);
            await File.WriteAllTextAsync(appsettingsFilePath, updatedFileContent);

            this.optionsMonitorCache.TryRemove(Options.DefaultName);
            this.optionsMonitorCache.TryAdd(Options.DefaultName, sectionObject);

            if (this.configuration is IConfigurationRoot configurationRoot)
            {
                configurationRoot.Reload();
            }
        }

        private string GetAppsettingsFilePath()
        {
            return Path.Combine(this.environment.ContentRootPath, this.appsettingsFileName);
        }

        private static async Task<JsonObject> GetJsonContentAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return new JsonObject();
            }

            var fileContent = await File.ReadAllTextAsync(filePath);
            if (string.IsNullOrWhiteSpace(fileContent))
            {
                return new JsonObject();
            }

            var jsonNode = JsonNode.Parse(fileContent);
            if (jsonNode is JsonObject jsonObject)
            {
                return jsonObject;
            }

            throw new InvalidOperationException($"Configuration file '{filePath}' must contain a JSON object.");
        }

        private JsonNode SerializeSection(T sectionObject)
        {
            var serializedNode = JsonSerializer.SerializeToNode(sectionObject, this.jsonSerializerOptions);
            if (serializedNode is JsonObject jsonObject)
            {
                return jsonObject;
            }

            var result = new JsonObject();

            foreach (var property in typeof(T).GetProperties().Where(p => p.CanRead && p.GetIndexParameters().Length == 0 && p.DeclaringType == typeof(T)))
            {
                var propertyValue = property.GetValue(sectionObject);
                if (propertyValue == null)
                {
                    continue;
                }

                result[property.Name] = JsonSerializer.SerializeToNode(propertyValue, this.jsonSerializerOptions);
            }

            if (serializedNode is JsonArray jsonArray)
            {
                for (var i = 0; i < jsonArray.Count; i++)
                {
                    result[i.ToString()] = jsonArray[i]?.DeepClone();
                }
            }

            return result;
        }

        private T DeserializeSection(JsonObject jsonObject)
        {
            var sectionObject = this.Value;
            if (!jsonObject.TryGetPropertyValue(this.section, out var sectionNode) || sectionNode == null)
            {
                return sectionObject;
            }

            var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            Flatten(sectionNode, this.section, data);

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(data)
                .Build();

            configuration.GetSection(this.section).Bind(sectionObject);
            return sectionObject;
        }

        private static void Flatten(JsonNode node, string path, IDictionary<string, string?> data)
        {
            switch (node)
            {
                case JsonObject jsonObject:
                    foreach (var property in jsonObject)
                    {
                        if (property.Value != null)
                        {
                            Flatten(property.Value, $"{path}:{property.Key}", data);
                        }
                    }
                    break;
                case JsonArray jsonArray:
                    for (var i = 0; i < jsonArray.Count; i++)
                    {
                        if (jsonArray[i] != null)
                        {
                            Flatten(jsonArray[i]!, $"{path}:{i}", data);
                        }
                    }
                    break;
                case JsonValue jsonValue:
                    data[path] = jsonValue.ToString();
                    break;
            }
        }
    }
}
