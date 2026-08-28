using Microsoft.Extensions.Logging;

namespace Superdev.AspNetCore.Tests.Logging
{
    public class TestOutputHelperLoggerProvider : ILoggerProvider
    {
        private readonly ITestOutputHelper testOutputHelper;

        public TestOutputHelperLoggerProvider(ITestOutputHelper testOutputHelper)
        {
            this.testOutputHelper = testOutputHelper;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new TestOutputHelperLogger(this.testOutputHelper, categoryName);
        }

        public void Dispose()
        {
        }
    }
}