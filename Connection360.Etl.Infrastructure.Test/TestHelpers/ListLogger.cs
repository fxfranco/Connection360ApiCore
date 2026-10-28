using Microsoft.Extensions.Logging;

namespace Connection360.Etl.Infrastructure.Tests.TestHelpers
{
    /// <summary>Logger de prueba que conserva los mensajes escritos.</summary>
    internal sealed class ListLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, String Message)> Entries { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public Boolean IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, String> formatter)
            => Entries.Add((logLevel, formatter(state, exception)));
    }
}
