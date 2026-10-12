using Connection360Notification.Domain;
using Connection360Notification.Domain.Enums;
using Connection360Notification.Infrastructure.Adapters.Output;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Connection360Notification.Infrastructure.Tests.Adapters
{
    public class SmsNotificationChannelTests
    {
        private sealed class ListLogger<T> : ILogger<T>
        {
            public List<(LogLevel Level, String Message)> Entries { get; } = new();
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public Boolean IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, String> formatter)
                => Entries.Add((logLevel, formatter(state, exception)));
        }

        private readonly ListLogger<SmsNotificationChannel> _logger = new();

        private static NotificationMessage Notificacion() =>
            new("CLI-7", NotificationType.Comment, "Contenido confidencial", title: "Titulo visible", id: "N-1");

        [Fact]
        public void Channel_EsTextMessages()
        {
            new SmsNotificationChannel(_logger).Channel.Should().Be(NotificationChannelType.TextMessages);
        }

        [Fact]
        public async Task SendAsync_RegistraElEnvioSimuladoConElClienteYElTituloSinElContenido()
        {
            await new SmsNotificationChannel(_logger).SendAsync(Notificacion(), CancellationToken.None);

            var entrada = _logger.Entries.Should().ContainSingle().Subject;
            entrada.Level.Should().Be(LogLevel.Information);
            entrada.Message.Should().Contain("[SMS]").And.Contain("CLI-7").And.Contain("N-1").And.Contain("Titulo visible");
            entrada.Message.Should().NotContain("Contenido confidencial");
        }

        [Fact]
        public async Task SendAsync_ConNotificacionNula_LanzaArgumentNullException()
        {
            Func<Task> act = () => new SmsNotificationChannel(_logger).SendAsync(null!, CancellationToken.None);

            await act.Should().ThrowAsync<ArgumentNullException>();
        }
    }
}
