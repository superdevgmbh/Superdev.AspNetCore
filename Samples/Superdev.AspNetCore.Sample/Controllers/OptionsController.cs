using Microsoft.AspNetCore.Mvc;
using Superdev.AspNetCore.Options;
using Superdev.AspNetCore.Sample.Options;

namespace Superdev.AspNetCore.Sample.Controllers
{
    /// <summary>
    /// Demonstrates how to read and update writable options via HTTP endpoints.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class OptionsController : ControllerBase
    {
        private readonly IWritableOptions<TestOptions> testOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionsController"/> class.
        /// </summary>
        /// <param name="testOptions">The writable options instance for the sample configuration.</param>
        public OptionsController(IWritableOptions<TestOptions> testOptions)
        {
            this.testOptions = testOptions;
        }

        /// <summary>
        /// Returns the current options value from the regular options pipeline.
        /// </summary>
        [HttpGet]
        public ActionResult<TestOptions> Get()
        {
            var currentOptions = this.testOptions.Value;
            return this.Ok(currentOptions);
        }

        /// <summary>
        /// Increments the counter by one using a partial update.
        /// </summary>
        [HttpPost("increment-count")]
        public async Task<ActionResult<TestOptions>> IncrementCountAsync()
        {
            var updatedCount = this.testOptions.Value.Count + 1;
            await this.testOptions.UpdatePropertyAsync(x => x.Count, updatedCount);
            return this.Ok(this.testOptions.Value);
        }

        /// <summary>
        /// Toggles the enabled flag using a single-property update.
        /// </summary>
        [HttpPost("toggle-enabled")]
        public async Task<ActionResult<TestOptions>> ToggleEnabledAsync()
        {
            var isEnabled = !this.testOptions.Value.Enabled;
            await this.testOptions.UpdatePropertyAsync(x => x.Enabled, isEnabled);
            return this.Ok(this.testOptions.Value);
        }

        /// <summary>
        /// Replaces the whole options section with the request body.
        /// </summary>
        /// <param name="options">The replacement options value.</param>
        [HttpPut]
        public async Task<ActionResult<TestOptions>> ReplaceAsync([FromBody] TestOptions options)
        {
            await this.testOptions.UpdateAsync(options);
            return this.Ok(this.testOptions.Value);
        }

        /// <summary>
        /// Updates a few fields in one write operation.
        /// </summary>
        [HttpPost("demo-bulk-update")]
        public async Task<ActionResult<TestOptions>> DemoBulkUpdateAsync()
        {
            await this.testOptions.UpdateAsync(current =>
            {
                current.Count += 5;
                current.Enabled = true;
                current.DisplayName = $"Updated at {DateTime.UtcNow:O}";
                current.Nested.Message = "Updated with UpdateAsync(Action<T>).";
            });
            return this.Ok(this.testOptions.Value);
        }
    }
}
