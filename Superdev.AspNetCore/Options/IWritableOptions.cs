using System.Linq.Expressions;

namespace Superdev.AspNetCore.Options
{
    /// <summary>
    /// Extends the regular IOptions with write operations that persist the configured section back to the
    /// underlying JSON appsettings file.
    /// </summary>
    /// <remarks>
    /// Updates are written to the appsettings file specified in
    /// <see cref="ServiceCollectionExtensions.ConfigureWritable{T}"/>.
    /// </remarks>
    public interface IWritableOptions<T> : IOptionsSnapshot<T> where T : class, new()
    {
        /// <summary>
        /// Updates a single property on the current options instance and persists the whole section.
        /// </summary>
        Task UpdatePropertyAsync<TValue>(Expression<Func<T, TValue>> propertySelector, TValue value);

        /// <summary>
        /// Applies the provided mutation to the current options value and persists the resulting section.
        /// </summary>
        Task UpdateAsync(Action<T> options);

        /// <summary>
        /// Replaces the configured section with the provided options instance.
        /// </summary>
        Task UpdateAsync(T options);

        /// <summary>
        /// Creates a replacement options value from the current one and persists the resulting section.
        /// </summary>
        Task UpdateAsync(Func<T, T> options);
    }
}
