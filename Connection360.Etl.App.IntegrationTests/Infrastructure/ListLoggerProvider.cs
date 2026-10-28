using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Connection360.Etl.App.IntegrationTests.Infrastructure
{
    /// <summary>Entrada de log capturada en memoria (categoría, nivel, mensaje ya formateado y excepción).</summary>
    public sealed record LogEntry(String Category, LogLevel Level, String Message, Exception? Exception);

    /// <summary>
    /// ILoggerProvider en memoria, seguro para uso concurrente: permite verificar lo que
    /// escriben el orquestador y sus bucles sin depender de la consola.
    /// </summary>
    public sealed class ListLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentQueue<LogEntry> _entries = new();

        public IReadOnlyList<LogEntry> Entries => _entries.ToArray();

        public ILogger CreateLogger(String categoryName) => new ListLogger(this, categoryName);

        /// <summary>Logger tipado (ILogger&lt;T&gt;) que escribe en este mismo proveedor.</summary>
        public ILogger<T> CreateLogger<T>() => new TypedLogger<T>(new ListLogger(this, typeof(T).FullName ?? typeof(T).Name));

        /// <summary>Espera (con sondeo corto y límite de tiempo) a que aparezca una entrada que cumpla el predicado.</summary>
        public async Task<LogEntry> WaitForAsync(Func<LogEntry, Boolean> predicate, TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);
            while (true)
            {
                var match = _entries.FirstOrDefault(predicate);
                if (match is not null)
                    return match;

                try
                {
                    await Task.Delay(10, cts.Token);
                }
                catch (OperationCanceledException)
                {
                    throw new TimeoutException("No apareció la entrada de log esperada. Entradas: " + String.Join(" | ", _entries.Select(e => e.Message)));
                }
            }
        }

        public void Dispose()
        {
        }

        private void Add(LogEntry entry) => _entries.Enqueue(entry);

        private sealed class ListLogger : ILogger
        {
            private readonly ListLoggerProvider _owner;
            private readonly String _category;

            public ListLogger(ListLoggerProvider owner, String category)
            {
                _owner = owner;
                _category = category;
            }

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public Boolean IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, String> formatter)
            {
                _owner.Add(new LogEntry(_category, logLevel, formatter(state, exception), exception));
            }
        }

        private sealed class TypedLogger<T> : ILogger<T>
        {
            private readonly ILogger _inner;

            public TypedLogger(ILogger inner) => _inner = inner;

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => _inner.BeginScope(state);

            public Boolean IsEnabled(LogLevel logLevel) => _inner.IsEnabled(logLevel);

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, String> formatter)
                => _inner.Log(logLevel, eventId, state, exception, formatter);
        }
    }
}
