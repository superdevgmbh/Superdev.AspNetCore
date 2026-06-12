using System.Globalization;
using System.Text.Json.Serialization;

namespace Superdev.AspNetCore
{
    [DebuggerDisplay("\"{ToString()}\"", Type = nameof(LocalizedString))]
    [JsonConverter(typeof(LocalizedStringJsonConverter))]
    public sealed class LocalizedString : Dictionary<string, string>
    {
        public const string FallbackJsonPropertyName = "null";

        public static KeyNotFoundStrategy KeyNotFoundStrategy { get; set; } = KeyNotFoundStrategy.ReturnDefaultCulture;

        private static string DefaultCultureName => CultureInfo.CurrentUICulture.Name;

        private static readonly char[] LanguageCodeSplitChars =
        {
            '-',
            '_'
        };

        private string? fallbackValue;

        public LocalizedString()
            : base(StringComparer.OrdinalIgnoreCase)
        {
        }

        public LocalizedString(Dictionary<string, string> values)
            : base(StringComparer.OrdinalIgnoreCase)
        {
            foreach (var (key, value) in values)
            {
                if (string.Equals(key, FallbackJsonPropertyName, StringComparison.OrdinalIgnoreCase))
                {
                    this.fallbackValue = value;
                    continue;
                }

                base[key] = value;
            }
        }

        internal string? FallbackValue => this.fallbackValue;

        public new void Add(string? cultureName, string value)
        {
            this[cultureName] = value;
        }

        public new string? this[string? cultureName]
        {
            get
            {
                if (this.TryGetValueByCulture(cultureName, out var value))
                {
                    return value;
                }

                if (this.fallbackValue != null)
                {
                    return this.fallbackValue;
                }

                if (this.TryGetValueByCulture(DefaultCultureName, out var fallbackValue))
                {
                    return fallbackValue;
                }

                if (KeyNotFoundStrategy == KeyNotFoundStrategy.Throw)
                {
                    throw GetKeyNotFoundException(DefaultCultureName);
                }

                return null;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                if (cultureName == null)
                {
                    this.fallbackValue = value;
                    return;
                }

                base[cultureName] = value;
            }
        }

        private bool TryGetValueByCulture(string? cultureName, out string? value)
        {
            if (!string.IsNullOrWhiteSpace(cultureName))
            {
                var parts = cultureName
                    .Split(LanguageCodeSplitChars, StringSplitOptions.RemoveEmptyEntries);

                for (var i = parts.Length; i > 0; i--)
                {
                    var key = string.Join("-", parts.Take(i));
                    if (this.TryGetValue(key, out value))
                    {
                        return true;
                    }
                }
            }

            value = null;
            return false;
        }

        private static Exception GetKeyNotFoundException(string cultureName)
        {
            return new KeyNotFoundException($"Localized string for culture '{cultureName}' not found.");
        }

        /// <summary>
        /// Converts LocalizedString implicitly to string using the default culture.
        /// </summary>
        public static implicit operator string?(LocalizedString localizedString)
        {
            return localizedString[DefaultCultureName];
        }

        /// <summary>
        /// Converts LocalizedString to string using the default culture.
        /// </summary>
        public override string? ToString()
        {
            return this[DefaultCultureName];
        }

        /// <summary>
        /// Converts LocalizedString to string using <paramref name="cultureInfo"/>.
        /// </summary>
        public string? ToString(CultureInfo cultureInfo)
        {
            return this[cultureInfo.Name];
        }
    }

    public enum KeyNotFoundStrategy
    {
        ReturnDefaultCulture,
        Throw
    }
}
