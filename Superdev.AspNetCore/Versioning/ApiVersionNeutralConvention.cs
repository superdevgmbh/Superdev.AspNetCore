using Asp.Versioning.Conventions;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Superdev.AspNetCore.Infrastructure.Versioning
{
    /// <summary>
    /// Marks all API controllers which don't define [ApiVersion] attributes as [ApiVersionNeutral].
    /// </summary>
    public class ApiVersionNeutralConvention : IControllerConvention
    {
        public bool Apply(IControllerConventionBuilder controller, ControllerModel controllerModel)
        {
            var hasApiVersionAttribute = controllerModel.ControllerType.IsDefined(typeof(ApiVersionAttribute), true);
            if (hasApiVersionAttribute == false)
            {
                controller.IsApiVersionNeutral();
            }

            return true;
        }
    }
}