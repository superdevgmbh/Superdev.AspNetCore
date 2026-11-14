namespace Superdev.AspNetCore.Options
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers a configuration section as both regular options and writable options backed by the specified
        /// appsettings JSON file.
        /// </summary>
        /// <remarks>
        /// Persistence is file-based and intended for occasional configuration changes, not as a
        /// high-throughput or transactional data store. Each update rewrites the configured section as a
        /// whole (last writer wins).
        /// <para>
        /// Concurrent writes within the same process are serialized by an in-process lock keyed by the
        /// resolved file path. Writes from other processes or application instances that target the same
        /// file are not coordinated. Opening the file for writing can transiently fail (for example while
        /// a <c>reloadOnChange</c> file watcher re-reads the file after a previous write), so writes are
        /// retried a few times with a short backoff before the <see cref="System.IO.IOException"/> is
        /// surfaced; a file that stays locked longer than that window still fails.
        /// </para>
        /// <para>
        /// When <paramref name="appsettingsFileName"/> is also registered with <c>reloadOnChange: true</c>,
        /// every write triggers the watcher to re-read the file. Writable options already update the options
        /// cache and reload configuration after a write, so this watcher reload is largely redundant for the
        /// writable file; registering that file with <c>reloadOnChange: false</c> removes the watcher and the
        /// associated transient sharing violations.
        /// </para>
        /// <para>
        /// After a write, the <see cref="IConfiguration"/> registered in the service collection is reloaded.
        /// <paramref name="section"/> must therefore belong to that configuration. If it comes from a separately
        /// built <see cref="IConfigurationRoot"/> (for example one built in a <c>Startup</c> constructor), use
        /// <see cref="ConfigureWritable{T}(IServiceCollection, IConfigurationRoot, string, string)"/>; otherwise
        /// that root keeps the file contents loaded before the write, and the next rebind of the options (any
        /// reload of that root) rolls the written values back.
        /// </para>
        /// </remarks>
        /// <param name="services">The service collection.</param>
        /// <param name="section">The configuration section.</param>
        /// <param name="appsettingsFileName">The name of the appsettings file. Default: appsettings.json.</param>
        public static void ConfigureWritable<T>(this IServiceCollection services, IConfigurationSection section, string appsettingsFileName = "appsettings.json")
            where T : class, new()
        {
            ArgumentNullException.ThrowIfNull(section);

            ConfigureWritable<T>(services, section, appsettingsFileName, provider => provider.GetRequiredService<IConfiguration>());
        }

        /// <summary>
        /// Registers the section <paramref name="sectionName"/> of <paramref name="configuration"/> as both regular
        /// options and writable options backed by the specified appsettings JSON file. After a write,
        /// <paramref name="configuration"/> itself is reloaded, so the options keep the written values when they
        /// are rebound later.
        /// </summary>
        /// <remarks>
        /// Use this overload when the options are bound to a configuration root that is not the
        /// <see cref="IConfiguration"/> registered in the service collection.
        /// </remarks>
        /// <example>
        /// <code>
        /// services.ConfigureWritable&lt;MaintenanceOptions&gt;(this.Configuration, "Maintenance", "appsettings.maintenance.json");
        /// </code>
        /// </example>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The configuration root the options are bound to.</param>
        /// <param name="sectionName">The name of the configuration section.</param>
        /// <param name="appsettingsFileName">The name of the appsettings file. Default: appsettings.json.</param>
        public static void ConfigureWritable<T>(this IServiceCollection services, IConfigurationRoot configuration, string sectionName, string appsettingsFileName = "appsettings.json")
            where T : class, new()
        {
            ArgumentNullException.ThrowIfNull(configuration);
            ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);

            ConfigureWritable<T>(services, configuration.GetSection(sectionName), appsettingsFileName, _ => configuration);
        }

        private static void ConfigureWritable<T>(
            IServiceCollection services,
            IConfigurationSection section,
            string appsettingsFileName,
            Func<IServiceProvider, IConfiguration> configurationFactory)
            where T : class, new()
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentException.ThrowIfNullOrWhiteSpace(appsettingsFileName);

            services.Configure<T>(section);
            services.AddTransient<IWritableOptions<T>>(provider =>
            {
                var environment = provider.GetRequiredService<IWebHostEnvironment>();
                var configuration = configurationFactory(provider);
                var options = provider.GetRequiredService<IOptionsMonitor<T>>();
                var optionsMonitorCache = provider.GetRequiredService<IOptionsMonitorCache<T>>();
                return new WritableOptions<T>(environment, configuration, options, optionsMonitorCache, section.Key, appsettingsFileName);
            });
        }
    }
}
