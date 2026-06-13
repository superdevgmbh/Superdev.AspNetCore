using Superdev.AspNetCore.ApplicationModelConventions;
using Superdev.AspNetCore.ExceptionHandling;
using Superdev.AspNetCore.Extensions;
using Superdev.AspNetCore.Options;
using Superdev.AspNetCore.Sample.Options;

namespace Superdev.AspNetCore.Sample
{
    public class Startup
    {
        private readonly IWebHostEnvironment environment;

        public Startup(IWebHostEnvironment env)
        {
            this.environment = env;

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
            services.ConfigureWritable<TestOptions>(this.Configuration.GetSection("Test"));

            services.AddOpenApi();

            services.AddControllers(options =>
            {
                options.Conventions.Add(new EnvironmentRestrictedApplicationModelConvention(this.environment));
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
