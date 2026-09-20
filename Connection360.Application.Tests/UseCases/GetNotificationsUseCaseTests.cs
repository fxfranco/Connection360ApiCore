using Connection360.Application.DTOs;
using Connection360.Application.UseCases;
using FluentAssertions;
using Xunit;

namespace Connection360.Application.Tests.UseCases
{
    public class GetNotificationsUseCaseTests
    {
        private readonly GetNotificationsUseCase _sut = new();

        [Fact]
        public void ExecuteGetNotificationsAllAsync_RetornaUnaListaNoVacia()
        {
            var request = new ClientSummaryRequest { IdClient = "123" };

            var result = _sut.ExecuteGetNotificationsAllAsync(request, CancellationToken.None);

            result.Should().NotBeNull();
            result.Should().NotBeEmpty();
        }

        [Fact]
        public void ExecuteGetNotificationsAllAsync_TodosLosElementosTienenIdPositivo()
        {
            var request = new ClientSummaryRequest { IdClient = "123" };

            var result = _sut.ExecuteGetNotificationsAllAsync(request, CancellationToken.None);

            result.Should().OnlyContain(n => n.IdNotification > 0);
        }
    }
}
