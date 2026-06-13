using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Superdev.AspNetCore.ApplicationModelConventions
{
    /// <summary>
    /// Removes controllers and actions which are not allowed in the current ASP.NET Core environment.
    /// </summary>
    public sealed class EnvironmentRestrictedApplicationModelConvention : IApplicationModelConvention
    {
        private readonly string environmentName;

        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentRestrictedApplicationModelConvention"/> class.
        /// </summary>
        /// <param name="webHostEnvironment">The web host environment.</param>
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
