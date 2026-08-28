namespace Superdev.AspNetCore.ApplicationModelConventions
{
    /// <summary>
    /// Restricts a controller or action to the specified ASP.NET Core environments.
    /// <para>
    /// Apply this attribute to a controller class or an action method to declare in which
    /// environments (for example <c>Development</c>, <c>Staging</c> or <c>Production</c>) the
    /// endpoint should be available. Controllers and actions which are not allowed in the current
    /// environment are removed from the application model by
    /// <see cref="EnvironmentRestrictedApplicationModelConvention"/> and therefore behave as if they
    /// did not exist (no routing, not listed in API metadata such as OpenAPI).
    /// </para>
    /// <para>
    /// The convention has to be registered once during start-up, otherwise the attribute has no
    /// effect. See <see cref="EnvironmentRestrictedApplicationModelConvention"/> for details.
    /// </para>
    /// </summary>
    /// <remarks>
    /// Environment names are matched case-insensitively against the names defined by
    /// <see cref="Microsoft.Extensions.Hosting.Environments"/>. Because attribute arguments must be
    /// compile-time constants, pass the names as string literals (or your own <c>const</c> values)
    /// rather than the <c>static readonly</c> fields of <c>Environments</c>.
    /// </remarks>
    /// <example>
    /// Restrict a whole controller to the development environment:
    /// <code>
    /// [ApiController]
    /// [EnvironmentRestricted("Development")]
    /// public class DiagnosticsController : ControllerBase
    /// {
    /// }
    /// </code>
    /// Restrict a single action to multiple environments:
    /// <code>
    /// [HttpGet("seed")]
    /// [EnvironmentRestricted("Development", "Staging")]
    /// public IActionResult Seed() => this.Ok();
    /// </code>
    /// </example>
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
