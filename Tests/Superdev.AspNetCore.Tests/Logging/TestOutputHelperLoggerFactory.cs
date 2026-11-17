using Microsoft.Extensions.Logging;

namespace Superdev.AspNetCore.Tests.Logging
{
    public class TestOutputHelperLoggerFactory : ILoggerFactory
    {
        private readonly ITestOutputHelper testOutputHelper;

        public TestOutputHelperLoggerFactory(ITestOutputHelper testOutputHelper)
        {
            this.testOutputHelper = testOutputHelper ?? throw new ArgumentNullException(nameof(testOutputHelper));
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new TestOutputHelperLogger(this.testOutputHelper, categoryName);
        }

        public void AddProvider(ILoggerProvider provider)
        {
        }

        public void Dispose()
        {
        }
    }
}