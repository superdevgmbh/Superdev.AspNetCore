using Superdev.AspNetCore.ExceptionHandling;
using Superdev.AspNetCore.Extensions;
using Superdev.AspNetCore.Versioning;

namespace Superdev.AspNetCore.Sample
{
    public class Startup
    {
        public Startup(IWebHostEnvironment env)
        {
            var configurationBuilder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            this.Configuration = configurationBuilder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            // ====== Exception Handling ======
            services.AddProblemDetails();
            services.AddExceptionHandler<ProblemDetailsExceptionHandler>();

            // ====== Services ======
            services.UseSuperdev();

            // ====== API Versioning ======
            services.AddApiVersioning()
                .AddMvc(options =>
                {
                    options.Conventions.Add(new ApiVersionNeutralConvention());
                });

            services.AddOpenApi();

            services.AddControllers(options =>
            {
                options.Filters.Add<ProblemDetailsResultFilter>();
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseExceptionHandler();
            app.UseHttpsRedirection();
            app.UseRouting();
            // app.UseAuthentication();
            // app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapOpenApi();
            });
        }
    }
}
