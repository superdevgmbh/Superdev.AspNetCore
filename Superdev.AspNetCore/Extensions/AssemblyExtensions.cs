using System.Reflection;

namespace Superdev.AspNetCore.Extensions
{
    public static class AssemblyExtensions
    {
        public static string? GetAssemblyVersion(this Assembly assembly)
        {
            var version = assembly.GetName().Version?.ToString();
            return version;
        }

        public static string? GetAssemblyInformationalVersion(this Assembly assembly, VersionHashFormat hashFormat = VersionHashFormat.Full)
        {
            var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if(version != null)
            {
                return FormatVersionHash(version, hashFormat);
            }

            return null;
        }

        private static string? FormatVersionHash(string? version, VersionHashFormat hashFormat)
        {
            if (string.IsNullOrEmpty(version) || hashFormat == VersionHashFormat.Full)
            {
                return version;
            }

            var hashSeparatorIndex = version.LastIndexOf('+');
            if (hashSeparatorIndex < 0 || hashSeparatorIndex == version.Length - 1)
            {
                return version;
            }

            var versionPrefix = version[..hashSeparatorIndex];
            if (hashFormat == VersionHashFormat.None)
            {
                return versionPrefix;
            }

            var hash = version[(hashSeparatorIndex + 1)..];
            return $"{versionPrefix}+{hash[..Math.Min(hash.Length, 7)]}";
        }
    }
}