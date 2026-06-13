namespace Superdev.AspNetCore.ApplicationModelConventions
{
    /// <summary>
    /// Restricts a controller or action to the specified ASP.NET Core environments.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class EnvironmentRestrictedAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentRestrictedAttribute"/> class.
        /// </summary>
        /// <param name="environmentNames">The allowed ASP.NET Core environment names.</param>
        /// <exception cref="ArgumentException">Thrown when no environment names are provided.</exception>
        public EnvironmentRestrictedAttribute(params string[] environmentNames)
        {
            ArgumentNullException.ThrowIfNull(environmentNames);

            if (environmentNames.Length == 0 || environmentNames.Any(string.IsNullOrWhiteSpace))
            {
                throw new ArgumentException("At least one non-empty environment name is required.", nameof(environmentNames));
            }

            this.EnvironmentNames = environmentNames;
        }

        /// <summary>
        /// Gets the allowed ASP.NET Core environment names.
        /// </summary>
        public IReadOnlyCollection<string> EnvironmentNames { get; }

        /// <summary>
        /// Determines whether the current environment is allowed.
        /// </summary>
        /// <param name="environmentName">The current ASP.NET Core environment name.</param>
        /// <returns><c>true</c> if the current environment is allowed; otherwise, <c>false</c>.</returns>
        public bool IsAllowed(string environmentName)
        {
            return this.EnvironmentNames.Contains(environmentName, StringComparer.OrdinalIgnoreCase);
        }
    }
}
