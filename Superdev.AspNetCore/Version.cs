using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.Json.Serialization;

namespace Superdev.AspNetCore
{
    /// <summary>
    /// Represents a Semantic Versioning 2.0.0 version.
    /// </summary>
    [DebuggerDisplay("{ToString()}")]
    [JsonConverter(typeof(VersionJsonConverter))]
    public sealed class Version : IComparable<Version>, IEquatable<Version>
    {
        private readonly string[] prereleaseIdentifiers;
        private readonly string[] buildMetadataIdentifiers;

        /// <summary>
        /// Initializes a new instance of the <see cref="Version"/> class.
        /// </summary>
        /// <param name="major">The major version.</param>
        /// <param name="minor">The minor version.</param>
        /// <param name="patch">The patch version.</param>
        /// <param name="prereleaseIdentifiers">The prerelease identifiers.</param>
        /// <param name="buildMetadataIdentifiers">The build metadata identifiers.</param>
        public Version(int major, int minor, int patch, IReadOnlyList<string>? prereleaseIdentifiers = null, IReadOnlyList<string>? buildMetadataIdentifiers = null)
        {
            this.Major = ValidateVersionComponent(major, nameof(major));
            this.Minor = ValidateVersionComponent(minor, nameof(minor));
            this.Patch = ValidateVersionComponent(patch, nameof(patch));
            this.prereleaseIdentifiers = ValidateIdentifiers(prereleaseIdentifiers, allowLeadingZeroes: false, nameof(prereleaseIdentifiers));
            this.buildMetadataIdentifiers = ValidateIdentifiers(buildMetadataIdentifiers, allowLeadingZeroes: true, nameof(buildMetadataIdentifiers));
        }

        /// <summary>
        /// Gets the major version.
        /// </summary>
        public int Major { get; }

        /// <summary>
        /// Gets the minor version.
        /// </summary>
        public int Minor { get; }

        /// <summary>
        /// Gets the patch version.
        /// </summary>
        public int Patch { get; }

        /// <summary>
        /// Gets the prerelease identifiers.
        /// </summary>
        public IReadOnlyList<string> PrereleaseIdentifiers => this.prereleaseIdentifiers;

        /// <summary>
        /// Gets a value indicating whether this version is a prerelease version.
        /// </summary>
        public bool IsPrerelease => this.prereleaseIdentifiers.Length > 0;

        /// <summary>
        /// Gets the build metadata identifiers.
        /// </summary>
        public IReadOnlyList<string> BuildMetadataIdentifiers => this.buildMetadataIdentifiers;

        /// <summary>
        /// Converts a version string to a <see cref="Version"/>.
        /// </summary>
        /// <param name="value">The version string.</param>
        public static implicit operator Version(string value)
        {
            return Parse(value);
        }

        /// <summary>
        /// Parses a Semantic Versioning 2.0.0 string.
        /// </summary>
        /// <param name="value">The version string.</param>
        /// <returns>The parsed version.</returns>
        public static Version Parse(string value)
        {
            if (!TryParse(value, out var version))
            {
                throw new FormatException($"'{value}' is not a valid Semantic Versioning 2.0.0 value.");
            }

            return version;
        }

        /// <summary>
        /// Tries to parse a Semantic Versioning 2.0.0 string.
        /// </summary>
        /// <param name="value">The version string.</param>
        /// <param name="version">The parsed version when successful.</param>
        /// <returns><c>true</c> when parsing succeeds; otherwise <c>false</c>.</returns>
        public static bool TryParse(string? value, [NotNullWhen(true)] out Version? version)
        {
            version = null;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var trimmedValue = value.Trim();
            var plusIndex = trimmedValue.IndexOf('+');
            var dashIndex = trimmedValue.IndexOf('-');
            var coreEndIndex = new[] { plusIndex, dashIndex }
                .Where(index => index >= 0)
                .DefaultIfEmpty(trimmedValue.Length)
                .Min();

            var corePart = trimmedValue[..coreEndIndex];
            var prereleasePart = dashIndex >= 0
                ? trimmedValue[(dashIndex + 1)..(plusIndex >= 0 ? plusIndex : trimmedValue.Length)]
                : null;
            var buildPart = plusIndex >= 0
                ? trimmedValue[(plusIndex + 1)..]
                : null;

            var coreIdentifiers = corePart.Split('.');
            if (coreIdentifiers.Length != 3)
            {
                return false;
            }

            if (!TryParseNumericIdentifier(coreIdentifiers[0], out var major) ||
                !TryParseNumericIdentifier(coreIdentifiers[1], out var minor) ||
                !TryParseNumericIdentifier(coreIdentifiers[2], out var patch))
            {
                return false;
            }

            if (!TryParseIdentifiers(prereleasePart, allowLeadingZeroes: false, out var prereleaseIdentifiers) ||
                !TryParseIdentifiers(buildPart, allowLeadingZeroes: true, out var buildMetadataIdentifiers))
            {
                return false;
            }

            version = new Version(major, minor, patch, prereleaseIdentifiers, buildMetadataIdentifiers);
            return true;
        }

        /// <inheritdoc />
        public int CompareTo(Version? other)
        {
            ArgumentNullException.ThrowIfNull(other);

            var majorComparison = this.Major.CompareTo(other.Major);
            if (majorComparison != 0)
            {
                return majorComparison;
            }

            var minorComparison = this.Minor.CompareTo(other.Minor);
            if (minorComparison != 0)
            {
                return minorComparison;
            }

            var patchComparison = this.Patch.CompareTo(other.Patch);
            if (patchComparison != 0)
            {
                return patchComparison;
            }

            return ComparePrereleaseIdentifiers(this.prereleaseIdentifiers, other.prereleaseIdentifiers);
        }

        /// <inheritdoc />
        public bool Equals(Version? other)
        {
            if (other is null)
            {
                return false;
            }

            return this.Major == other.Major &&
                   this.Minor == other.Minor &&
                   this.Patch == other.Patch &&
                   this.prereleaseIdentifiers.SequenceEqual(other.prereleaseIdentifiers, StringComparer.Ordinal) &&
                   this.buildMetadataIdentifiers.SequenceEqual(other.buildMetadataIdentifiers, StringComparer.Ordinal);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is Version other && this.Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(this.Major);
            hashCode.Add(this.Minor);
            hashCode.Add(this.Patch);

            foreach (var identifier in this.prereleaseIdentifiers)
            {
                hashCode.Add(identifier, StringComparer.Ordinal);
            }

            foreach (var identifier in this.buildMetadataIdentifiers)
            {
                hashCode.Add(identifier, StringComparer.Ordinal);
            }

            return hashCode.ToHashCode();
        }

        /// <summary>
        /// Returns the Semantic Versioning 2.0.0 string representation.
        /// </summary>
        /// <returns>The formatted version string.</returns>
        public override string ToString()
        {
            var builder = new StringBuilder()
                .Append(this.Major.ToString(CultureInfo.InvariantCulture))
                .Append('.')
                .Append(this.Minor.ToString(CultureInfo.InvariantCulture))
                .Append('.')
                .Append(this.Patch.ToString(CultureInfo.InvariantCulture));

            if (this.prereleaseIdentifiers.Length > 0)
            {
                builder.Append('-').Append(string.Join('.', this.prereleaseIdentifiers));
            }

            if (this.buildMetadataIdentifiers.Length > 0)
            {
                builder.Append('+').Append(string.Join('.', this.buildMetadataIdentifiers));
            }

            return builder.ToString();
        }

        /// <summary>
        /// Determines whether two versions are equal.
        /// </summary>
        public static bool operator ==(Version? left, Version? right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Determines whether two versions are not equal.
        /// </summary>
        public static bool operator !=(Version? left, Version? right)
        {
            return !Equals(left, right);
        }

        /// <summary>
        /// Determines whether one version is lower than another.
        /// </summary>
        public static bool operator <(Version left, Version right)
        {
            ArgumentNullException.ThrowIfNull(left);
            return left.CompareTo(right) < 0;
        }

        /// <summary>
        /// Determines whether one version is lower than or equal to another.
        /// </summary>
        public static bool operator <=(Version left, Version right)
        {
            ArgumentNullException.ThrowIfNull(left);
            return left.CompareTo(right) <= 0;
        }

        /// <summary>
        /// Determines whether one version is greater than another.
        /// </summary>
        public static bool operator >(Version left, Version right)
        {
            ArgumentNullException.ThrowIfNull(left);
            return left.CompareTo(right) > 0;
        }

        /// <summary>
        /// Determines whether one version is greater than or equal to another.
        /// </summary>
        public static bool operator >=(Version left, Version right)
        {
            ArgumentNullException.ThrowIfNull(left);
            return left.CompareTo(right) >= 0;
        }

        private static int ValidateVersionComponent(int value, string parameterName)
        {
            return value < 0
                ? throw new ArgumentOutOfRangeException(parameterName, "Version components must not be negative.")
                : value;
        }

        private static string[] ValidateIdentifiers(IReadOnlyList<string>? identifiers, bool allowLeadingZeroes, string parameterName)
        {
            if (identifiers is null || identifiers.Count == 0)
            {
                return Array.Empty<string>();
            }

            var validated = new string[identifiers.Count];
            for (var index = 0; index < identifiers.Count; index++)
            {
                var identifier = identifiers[index];
                if (!IsValidIdentifier(identifier, allowLeadingZeroes))
                {
                    throw new ArgumentException("Identifiers must follow Semantic Versioning 2.0.0 rules.", parameterName);
                }

                validated[index] = identifier;
            }

            return validated;
        }

        private static bool TryParseNumericIdentifier(string value, out int component)
        {
            component = default;
            return !string.IsNullOrEmpty(value) &&
                   (value.Length == 1 || value[0] != '0') &&
                   value.All(char.IsAsciiDigit) &&
                   int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out component);
        }

        private static bool TryParseIdentifiers(string? value, bool allowLeadingZeroes, [NotNullWhen(true)] out string[]? identifiers)
        {
            identifiers = null;
            if (value is null)
            {
                identifiers = Array.Empty<string>();
                return true;
            }

            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            var segments = value.Split('.');
            if (segments.Any(segment => !IsValidIdentifier(segment, allowLeadingZeroes)))
            {
                return false;
            }

            identifiers = segments;
            return true;
        }

        private static bool IsValidIdentifier(string identifier, bool allowLeadingZeroes)
        {
            if (string.IsNullOrEmpty(identifier) || !identifier.All(IsValidIdentifierCharacter))
            {
                return false;
            }

            if (!allowLeadingZeroes && IsNumericIdentifier(identifier) && identifier.Length > 1 && identifier[0] == '0')
            {
                return false;
            }

            return true;
        }

        private static bool IsValidIdentifierCharacter(char character)
        {
            return char.IsAsciiLetterOrDigit(character) || character == '-';
        }

        private static bool IsNumericIdentifier(string identifier)
        {
            return identifier.All(char.IsAsciiDigit);
        }

        private static int ComparePrereleaseIdentifiers(IReadOnlyList<string> left, IReadOnlyList<string> right)
        {
            if (left.Count == 0 && right.Count == 0)
            {
                return 0;
            }

            if (left.Count == 0)
            {
                return 1;
            }

            if (right.Count == 0)
            {
                return -1;
            }

            for (var index = 0; index < Math.Min(left.Count, right.Count); index++)
            {
                var leftIdentifier = left[index];
                var rightIdentifier = right[index];
                var leftNumeric = IsNumericIdentifier(leftIdentifier);
                var rightNumeric = IsNumericIdentifier(rightIdentifier);

                if (leftNumeric && rightNumeric)
                {
                    var numericComparison = int.Parse(leftIdentifier, CultureInfo.InvariantCulture)
                        .CompareTo(int.Parse(rightIdentifier, CultureInfo.InvariantCulture));
                    if (numericComparison != 0)
                    {
                        return numericComparison;
                    }

                    continue;
                }

                if (leftNumeric != rightNumeric)
                {
                    return leftNumeric ? -1 : 1;
                }

                var ordinalComparison = string.CompareOrdinal(leftIdentifier, rightIdentifier);
                if (ordinalComparison != 0)
                {
                    return ordinalComparison;
                }
            }

            return left.Count.CompareTo(right.Count);
        }
    }
}
