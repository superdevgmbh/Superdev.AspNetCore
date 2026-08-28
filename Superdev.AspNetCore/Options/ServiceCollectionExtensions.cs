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
        /// </remarks>
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
