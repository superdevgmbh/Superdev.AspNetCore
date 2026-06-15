using System.Globalization;
using System.Resources;
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
        public static implicit operator string?(LocalizedString? localizedString)
        {
            return localizedString?[DefaultCultureName];
        }

        /// <summary>
        /// Converts string implicitly to LocalizedString,
        /// storing <paramref name="value"/> as culture-independent fallback value.
        /// </summary>
        public static implicit operator LocalizedString?(string? value)
        {
            if (value == null)
            {
                return null;
            }

            return new LocalizedString
            {
                [null] = value
            };
        }

        /// <summary>
        /// Creates <see cref="LocalizedString"/> with the value of <paramref name="resourceKey"/>,
        /// read from <paramref name="resourceManager"/> for each of the given <paramref name="cultures"/>.
        /// Cultures whose resource value is missing or empty are skipped.
        /// </summary>
        /// <remarks>
        /// Reading every supported culture up front makes the result culture-stable, so a value built once
        /// (e.g. at startup) can still be resolved per request via <see cref="LocalizedString.ToString(CultureInfo)"/>.
        /// </remarks>
        public static LocalizedString FromResource(ResourceManager resourceManager, string resourceKey, IEnumerable<CultureInfo> cultures)
        {
            ArgumentNullException.ThrowIfNull(resourceManager);
            ArgumentNullException.ThrowIfNull(resourceKey);
            ArgumentNullException.ThrowIfNull(cultures);

            var localizedString = new LocalizedString();

            foreach (var culture in cultures)
            {
                var value = resourceManager.GetString(resourceKey, culture);
                if (!string.IsNullOrEmpty(value))
                {
                    localizedString.Add(culture.Name, value);
                }
            }

            return localizedString;
        }

        /// <summary>
        /// Returns the full, lossless representation of this <see cref="LocalizedString"/>:
        /// all culture entries plus the culture-independent fallback value under the
        /// <see cref="FallbackJsonPropertyName"/> ("null") key.
        /// </summary>
        /// <remarks>
        /// This is the symmetric counterpart to <see cref="LocalizedString(Dictionary{string,string})"/>
        /// and mirrors the JSON representation, so the result round-trips without losing the fallback:
        /// <c>new LocalizedString(localizedString.ToDictionary())</c>.
        /// Unlike enumerating the instance directly (which omits the fallback), this is the
        /// representation to map from when converting to or from other <c>LocalizedString</c> types.
        /// </remarks>
        public IReadOnlyDictionary<string, string> ToDictionary()
        {
            var dictionary = new Dictionary<string, string>(this, StringComparer.OrdinalIgnoreCase);

            if (this.fallbackValue != null)
            {
                dictionary[FallbackJsonPropertyName] = this.fallbackValue;
            }

            return dictionary;
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
