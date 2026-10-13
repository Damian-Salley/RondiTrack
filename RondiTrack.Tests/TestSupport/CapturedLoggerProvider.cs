using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace RondiTrack.Tests.TestSupport;

// Captures application logs so tests can check for sensitive information.
public class CapturedLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentQueue<(LogLevel Level, string Message)> _logs = new();

    public IReadOnlyCollection<(LogLevel Level, string Message)> Logs =>
        _logs.ToArray();

    public ILogger CreateLogger(string categoryName) =>
        new CapturedLogger(this);

    public void Dispose()
    {
    }

    private sealed class CapturedLogger : ILogger
    {
        private readonly CapturedLoggerProvider _provider;

        public CapturedLogger(CapturedLoggerProvider provider)
        {
            _provider = provider;
        }

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            // Capture the formatted message and exception details.
            var message = formatter(state, exception);

            if (exception is not null)
                message += exception.ToString();

            _provider._logs.Enqueue((logLevel, message));
        }

    }
    // Clear previously captured logs before a test.
    public void Clear()
    {
        while (_logs.TryDequeue(out _))
        {
        }
    }
}