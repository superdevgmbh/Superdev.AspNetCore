namespace Superdev.AspNetCore.Sample.Options
{
    /// <summary>
    /// Sample options used to demonstrate the writable options API.
    /// </summary>
    public class TestOptions
    {
        /// <summary>
        /// Gets or sets a simple scalar value.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the feature is enabled.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets a display name used in the demo endpoints.
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets nested options to demonstrate complex object persistence.
        /// </summary>
        public TestNestedOptions Nested { get; set; } = new();
    }

    /// <summary>
    /// Nested sample options to demonstrate whole-object and bulk updates.
    /// </summary>
    public class TestNestedOptions
    {
        /// <summary>
        /// Gets or sets a nested message value.
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }
}
