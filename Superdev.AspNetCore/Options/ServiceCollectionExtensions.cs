namespace Superdev.AspNetCore.Infrastructure.Configuration
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers a configuration section as both regular options and writable options backed by the specified
        /// appsettings JSON file.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="section">The configuration section.</param>
        /// <param name="appsettingsFileName">The name of the appsettings file. Default: appsettings.json.</param>
        public static void ConfigureWritable<T>(this IServiceCollection services, IConfigurationSection section, string appsettingsFileName = "appsettings.json")
            where T : class, new()
        {
            services.Configure<T>(section);
            services.AddTransient<IWritableOptions<T>>(provider =>
            {
                var environment = provider.GetRequiredService<IWebHostEnvironment>();
                var configuration = provider.GetRequiredService<IConfiguration>();
                var options = provider.GetRequiredService<IOptionsMonitor<T>>();
                var optionsMonitorCache = provider.GetRequiredService<IOptionsMonitorCache<T>>();
                return new WritableOptions<T>(environment, configuration, options, optionsMonitorCache, section.Key, appsettingsFileName);
            });
        }
    }
}
