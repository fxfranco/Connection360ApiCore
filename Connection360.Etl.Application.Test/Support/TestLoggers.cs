using Microsoft.Extensions.Logging;

namespace Connection360.Etl.Application.Tests.Support
{
    /// <summary>Logger de prueba que conserva los mensajes escritos para poder verificarlos.</summary>
    internal sealed class ListLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, String Message, Exception? Exception)> Entries { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public Boolean IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, String> formatter)
        {
            Entries.Add((logLevel, formatter(state, exception), exception));
        }

        public IEnumerable<String> Messages(LogLevel level) => Entries.Where(e => e.Level == level).Select(e => e.Message);
    }
}
