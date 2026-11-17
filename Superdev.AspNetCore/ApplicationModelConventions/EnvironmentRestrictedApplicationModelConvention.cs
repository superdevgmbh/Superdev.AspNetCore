using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Superdev.AspNetCore.ApplicationModelConventions
{
    /// <summary>
    /// Removes controllers and actions which are not allowed in the current ASP.NET Core environment.
    /// <para>
    /// This convention inspects every controller and action for an
    /// <see cref="EnvironmentRestrictedAttribute"/>. Endpoints whose restriction does not include the
    /// current environment are removed from the application model, so they are not routed and do not
    /// appear in API metadata (for example OpenAPI). Endpoints without the attribute are always kept.
    /// </para>
    /// </summary>
    /// <remarks>
    /// Register the convention once when configuring MVC, for example:
    /// <code>
    /// services.AddControllers(options =>
    /// {
    ///     options.Conventions.Add(new EnvironmentRestrictedApplicationModelConvention(webHostEnvironment));
    /// });
    /// </code>
    /// The application model is built once at start-up, so the set of available endpoints is fixed for
    /// the lifetime of the application and reflects the environment at start-up.
    /// </remarks>
    /// <seealso cref="EnvironmentRestrictedAttribute"/>
    public sealed class EnvironmentRestrictedApplicationModelConvention : IApplicationModelConvention
    {
        private readonly string environmentName;

        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentRestrictedApplicationModelConvention"/> class.
        /// </summary>
        /// <param name="webHostEnvironment">The web host environment whose <see cref="IWebHostEnvironment.EnvironmentName"/> is matched against the restrictions.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="webHostEnvironment"/> is <c>null</c>.</exception>
        public EnvironmentRestrictedApplicationModelConvention(IWebHostEnvironment webHostEnvironment)
        {
            ArgumentNullException.ThrowIfNull(webHostEnvironment);

            this.environmentName = webHostEnvironment.EnvironmentName;
        }

        /// <inheritdoc />
        public void Apply(ApplicationModel application)
        {
            for (var controllerIndex = application.Controllers.Count - 1; controllerIndex >= 0; controllerIndex--)
            {
                var controller = application.Controllers[controllerIndex];
                if (!this.IsAllowed(controller.Attributes.OfType<EnvironmentRestrictedAttribute>()))
                {
                    application.Controllers.RemoveAt(controllerIndex);
                    continue;
                }

                for (var actionIndex = controller.Actions.Count - 1; actionIndex >= 0; actionIndex--)
                {
                    var action = controller.Actions[actionIndex];
                    if (!this.IsAllowed(action.Attributes.OfType<EnvironmentRestrictedAttribute>()))
                    {
                        controller.Actions.RemoveAt(actionIndex);
                    }
                }
            }
        }

        private bool IsAllowed(IEnumerable<EnvironmentRestrictedAttribute> attributes)
        {
            return attributes.All(a => a.IsAllowed(this.environmentName));
        }
    }
}
