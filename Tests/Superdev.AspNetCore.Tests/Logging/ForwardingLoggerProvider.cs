using Microsoft.Extensions.Logging;

namespace Superdev.AspNetCore.Tests.Logging
{
    public delegate void LogMessage(LogLevel logLevel, string categoryName, EventId eventId, string message, Exception? exception);

    internal class ForwardingLoggerProvider : ILoggerProvider
    {
        private readonly LogMessage logAction;

        public ForwardingLoggerProvider(LogMessage logAction)
        {
            this.logAction = logAction;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new ForwardingLogger(categoryName, this.logAction);
        }

        public void Dispose()
        {
        }

        internal class ForwardingLogger : ILogger
        {
            private readonly string categoryName;
            private readonly LogMessage logAction;

            public ForwardingLogger(string categoryName, LogMessage logAction)
            {
                this.categoryName = categoryName;
                this.logAction = logAction;
            }

            IDisposable ILogger.BeginScope<TState>(TState state)
            {
                return null!;
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                this.logAction(logLevel, this.categoryName, eventId, formatter(state, exception), exception);
            }
        }
    }
}