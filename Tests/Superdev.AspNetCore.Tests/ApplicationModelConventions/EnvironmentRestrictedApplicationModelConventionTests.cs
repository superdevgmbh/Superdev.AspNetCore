using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Superdev.AspNetCore.ApplicationModelConventions;

namespace Superdev.AspNetCore.Tests.ApplicationModelConventions
{
    [Trait(Traits.Category, Traits.UnitTests)]
    public class EnvironmentRestrictedApplicationModelConventionTests
    {
        [Fact]
        public void Constructor_WhenWebHostEnvironmentIsNull_ThrowsArgumentNullException()
        {
            // Act
            var action = () => new EnvironmentRestrictedApplicationModelConvention(null!);

            // Assert
            action.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Apply_WhenControllerIsRestrictedToAnotherEnvironment_RemovesController()
        {
            // Arrange
            var convention = CreateConvention("Production");
            var application = CreateApplicationModel(typeof(DevelopmentOnlyController));

            // Act
            convention.Apply(application);

            // Assert
            application.Controllers.Should().BeEmpty();
        }

        [Fact]
        public void Apply_WhenControllerIsRestrictedToCurrentEnvironment_KeepsController()
        {
            // Arrange
            var convention = CreateConvention("Development");
            var application = CreateApplicationModel(typeof(DevelopmentOnlyController));

            // Act
            convention.Apply(application);

            // Assert
            application.Controllers.Should().ContainSingle()
                .Which.ControllerType.Should().Be(typeof(DevelopmentOnlyController).GetTypeInfo());
        }

        [Fact]
        public void Apply_WhenControllerHasNoRestriction_KeepsControllerAndActions()
        {
            // Arrange
            var convention = CreateConvention("Production");
            var application = CreateApplicationModel(typeof(UnrestrictedController));

            // Act
            convention.Apply(application);

            // Assert
            var controller = application.Controllers.Should().ContainSingle().Subject;
            controller.Actions.Should().ContainSingle();
        }

        [Fact]
        public void Apply_WhenActionIsRestrictedToAnotherEnvironment_RemovesActionButKeepsController()
        {
            // Arrange
            var convention = CreateConvention("Development");
            var application = CreateApplicationModel(typeof(MixedActionsController));

            // Act
            convention.Apply(application);

            // Assert
            var controller = application.Controllers.Should().ContainSingle().Subject;
            controller.Actions.Select(a => a.ActionMethod.Name)
                .Should().BeEquivalentTo(nameof(MixedActionsController.UnrestrictedAction));
        }

        [Fact]
        public void Apply_WhenActionIsRestrictedToCurrentEnvironment_KeepsAllActions()
        {
            // Arrange
            var convention = CreateConvention("Production");
            var application = CreateApplicationModel(typeof(MixedActionsController));

            // Act
            convention.Apply(application);

            // Assert
            var controller = application.Controllers.Should().ContainSingle().Subject;
            controller.Actions.Select(a => a.ActionMethod.Name)
                .Should().BeEquivalentTo(
                    nameof(MixedActionsController.ProductionAction),
                    nameof(MixedActionsController.UnrestrictedAction));
        }

        private static EnvironmentRestrictedApplicationModelConvention CreateConvention(string environmentName)
        {
            var webHostEnvironment = new Mock<IWebHostEnvironment>();
            webHostEnvironment.SetupGet(e => e.EnvironmentName).Returns(environmentName);
            return new EnvironmentRestrictedApplicationModelConvention(webHostEnvironment.Object);
        }

        private static ApplicationModel CreateApplicationModel(params Type[] controllerTypes)
        {
            var application = new ApplicationModel();

            foreach (var controllerType in controllerTypes)
            {
                var typeInfo = controllerType.GetTypeInfo();
                var controller = new ControllerModel(typeInfo, typeInfo.GetCustomAttributes(inherit: true))
                {
                    Application = application,
                };
                application.Controllers.Add(controller);

                var methods = controllerType.GetMethods(
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                foreach (var method in methods)
                {
                    var action = new ActionModel(method, method.GetCustomAttributes(inherit: true))
                    {
                        Controller = controller,
                    };
                    controller.Actions.Add(action);
                }
            }

            return application;
        }

        [EnvironmentRestricted("Development")]
        private class DevelopmentOnlyController
        {
            public void Get()
            {
            }
        }

        private class UnrestrictedController
        {
            public void Get()
            {
            }
        }

        private class MixedActionsController
        {
            [EnvironmentRestricted("Production")]
            public void ProductionAction()
            {
            }

            public void UnrestrictedAction()
            {
            }
        }
    }
}
