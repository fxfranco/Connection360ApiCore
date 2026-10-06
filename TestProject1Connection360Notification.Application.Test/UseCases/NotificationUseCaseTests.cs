using Connection360Notification.Application.DTOs;
using Connection360Notification.Application.UseCases;
using Connection360Notification.Domain;
using Connection360Notification.Domain.Ports.Outbound;
using FluentAssertions;
using Moq;
using Xunit;
using DomainNotificationType = Connection360Notification.Domain.Enums.NotificationType;

namespace Connection360Notification.Application.Tests.UseCases
{
    public class NotificationUseCaseTests
    {
        private readonly Mock<IKafkaProducerService> _kafkaMock = new();
        private readonly NotificationUseCase _sut;

        public NotificationUseCaseTests()
        {
            _sut = new NotificationUseCase(_kafkaMock.Object);
        }

        [Theory]
        [InlineData("ChangeState", DomainNotificationType.ChangeState)]
        [InlineData("Comment", DomainNotificationType.Comment)]
        [InlineData("changestate", DomainNotificationType.ChangeState)]
        [InlineData("COMMENT", DomainNotificationType.Comment)]
        public async Task ExecuteSendAsync_ConTipoValido_PublicaNotificacionConElTipoDeDominio(String tipo, DomainNotificationType esperado)
        {
            NotificationMessage? publicada = null;
            _kafkaMock
                .Setup(k => k.ProduceNotificationAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()))
                .Callback<NotificationMessage, CancellationToken>((n, _) => publicada = n)
                .Returns(Task.CompletedTask);

            await _sut.ExecuteSendAsync(new CreateNotificationRequest("CLI-1", "Contenido", tipo), CancellationToken.None);

            publicada.Should().NotBeNull();
            publicada!.Type.Should().Be(esperado);
            publicada.ClientId.Should().Be("CLI-1");
            publicada.Message.Should().Be("Contenido");
        }

        [Fact]
        public async Task ExecuteSendAsync_ConDatosOpcionales_LosPropagaALaNotificacion()
        {
            var fecha = new DateTime(2024, 3, 4, 5, 6, 7, DateTimeKind.Utc);
            NotificationMessage? publicada = null;
            _kafkaMock
                .Setup(k => k.ProduceNotificationAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()))
                .Callback<NotificationMessage, CancellationToken>((n, _) => publicada = n)
                .Returns(Task.CompletedTask);

            await _sut.ExecuteSendAsync(
                new CreateNotificationRequest("CLI-1", "Contenido", "Comment", "HBL-1", "Titulo", fecha),
                CancellationToken.None);

            publicada!.DocumentNumber.Should().Be("HBL-1");
            publicada.Title.Should().Be("Titulo");
            publicada.MessageDate.Should().Be(fecha);
        }

        [Fact]
        public async Task ExecuteSendAsync_SinDatosOpcionales_UsaValoresPorDefecto()
        {
            NotificationMessage? publicada = null;
            _kafkaMock
                .Setup(k => k.ProduceNotificationAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()))
                .Callback<NotificationMessage, CancellationToken>((n, _) => publicada = n)
                .Returns(Task.CompletedTask);

            await _sut.ExecuteSendAsync(new CreateNotificationRequest("CLI-1", "Contenido", "Comment"), CancellationToken.None);

            publicada!.DocumentNumber.Should().BeEmpty();
            publicada.Title.Should().BeEmpty();
            publicada.MessageDate.Should().NotBe(default);
        }

        [Fact]
        public async Task ExecuteSendAsync_PublicaUnaSolaVezConElTokenRecibido()
        {
            using var cts = new CancellationTokenSource();

            await _sut.ExecuteSendAsync(new CreateNotificationRequest("CLI-1", "Contenido", "Comment"), cts.Token);

            _kafkaMock.Verify(k => k.ProduceNotificationAsync(It.IsAny<NotificationMessage>(), cts.Token), Times.Once);
        }

        [Theory]
        [InlineData("Invalido")]
        [InlineData("")]
        public async Task ExecuteSendAsync_ConTipoInvalido_LanzaArgumentExceptionYNoPublica(String tipo)
        {
            Func<Task> act = () => _sut.ExecuteSendAsync(new CreateNotificationRequest("CLI-1", "Contenido", tipo), CancellationToken.None);

            var ex = await act.Should().ThrowAsync<ArgumentException>();
            ex.Which.Message.Should().Contain("no es válido");
            _kafkaMock.Verify(k => k.ProduceNotificationAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteSendAsync_ConDestinatarioVacio_LanzaArgumentExceptionDelDominio()
        {
            Func<Task> act = () => _sut.ExecuteSendAsync(new CreateNotificationRequest("", "Contenido", "Comment"), CancellationToken.None);

            await act.Should().ThrowAsync<ArgumentException>();
            _kafkaMock.Verify(k => k.ProduceNotificationAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteSendAsync_ConContenidoVacio_LanzaArgumentExceptionDelDominio()
        {
            Func<Task> act = () => _sut.ExecuteSendAsync(new CreateNotificationRequest("CLI-1", " ", "Comment"), CancellationToken.None);

            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task ExecuteSendAsync_CuandoElProductorFalla_PropagaLaExcepcion()
        {
            _kafkaMock
                .Setup(k => k.ProduceNotificationAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("kafka caido"));

            Func<Task> act = () => _sut.ExecuteSendAsync(new CreateNotificationRequest("CLI-1", "Contenido", "Comment"), CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("kafka caido");
        }

        [Fact]
        public async Task ExecuteSendAsync_ConTipoNumericoDefinido_LoAceptaComoValorDelEnum()
        {
            NotificationMessage? publicada = null;
            _kafkaMock
                .Setup(k => k.ProduceNotificationAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()))
                .Callback<NotificationMessage, CancellationToken>((n, _) => publicada = n)
                .Returns(Task.CompletedTask);

            await _sut.ExecuteSendAsync(new CreateNotificationRequest("CLI-1", "Contenido", "1"), CancellationToken.None);

            publicada!.Type.Should().Be(DomainNotificationType.Comment);
        }

        [Fact]
        public async Task ExecuteSendAsync_ConTipoNumericoNoDefinido_LanzaArgumentOutOfRangeExceptionYNoPublica()
        {
            Func<Task> act = () => _sut.ExecuteSendAsync(new CreateNotificationRequest("CLI-1", "Contenido", "99"), CancellationToken.None);

            await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
            _kafkaMock.Verify(k => k.ProduceNotificationAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
