using Connection360.Domain.Entities.Persistence;
using Connection360.Infrastructure.Persistence.Repositories;
using Connection360.Infrastructure.Tests.TestHelpers;
using FluentAssertions;
using Xunit;

namespace Connection360.Infrastructure.Tests.Persistence.Repositories
{
    /// <summary>
    /// Repositorio Dapper de connection360write.collaborator_notification_channels sobre una DbSession
    /// con conexion en memoria: se verifica el SQL, los parametros y el mapeo de resultados.
    /// </summary>
    public class CollaboratorNotificationChannelsRepositoryTests
    {
        private static readonly String[] Columns = { "id_notification_channel", "id_collaborator", "application", "email", "text_messages" };

        private readonly FakeDbConnection _connection = new();
        private readonly CollaboratorNotificationChannelsRepository _sut;

        public CollaboratorNotificationChannelsRepositoryTests()
        {
            _sut = new CollaboratorNotificationChannelsRepository(TestDbSession.Create(_connection));
        }

        [Fact]
        public async Task ListAllAsync_MapeaTodasLasFilas()
        {
            _connection.ReaderResult = FakeDbConnection.Table(Columns,
                new Object?[] { 1L, 10L, true, false, true },
                new Object?[] { 2L, 11L, false, true, false });

            List<CollaboratorNotificationChannels> list = (await _sut.ListAllAsync()).ToList();

            list.Should().HaveCount(2);
            list[0].IdNotificationChannel.Should().Be(1);
            list[0].IdCollaborator.Should().Be(10);
            list[0].Application.Should().BeTrue();
            list[0].Email.Should().BeFalse();
            list[0].TextMessages.Should().BeTrue();
            list[1].Email.Should().BeTrue();
            _connection.Commands.Single().CommandText.Should().Contain("FROM connection360write.collaborator_notification_channels");
        }

        [Fact]
        public async Task GetByIdAsync_RetornaLaFilaYPropagaElParametro()
        {
            _connection.ReaderResult = FakeDbConnection.Table(Columns, new Object?[] { 5L, 10L, true, true, true });

            CollaboratorNotificationChannels? result = await _sut.GetByIdAsync(5);

            result.Should().NotBeNull();
            result!.IdNotificationChannel.Should().Be(5);
            RecordedCommand command = _connection.Commands.Single();
            command.CommandText.Should().Contain("WHERE id_notification_channel = @Id");
            command.Parameters["Id"].Should().Be(5L);
        }

        [Fact]
        public async Task GetByIdAsync_SinFilas_RetornaNull()
        {
            _connection.ReaderResult = FakeDbConnection.Table(Columns);

            (await _sut.GetByIdAsync(99)).Should().BeNull();
        }

        [Fact]
        public async Task GetByCollaboratorIdAsync_RetornaLaFilaYPropagaElParametro()
        {
            _connection.ReaderResult = FakeDbConnection.Table(Columns, new Object?[] { 5L, 10L, true, false, true });

            CollaboratorNotificationChannels? result = await _sut.GetByCollaboratorIdAsync(10);

            result!.IdCollaborator.Should().Be(10);
            result.Email.Should().BeFalse();
            RecordedCommand command = _connection.Commands.Single();
            command.CommandText.Should().Contain("WHERE id_collaborator = @IdCollaborator");
            command.Parameters["IdCollaborator"].Should().Be(10L);
        }

        [Fact]
        public async Task GetByCollaboratorIdAsync_SinFilas_RetornaNull()
        {
            _connection.ReaderResult = FakeDbConnection.Table(Columns);

            (await _sut.GetByCollaboratorIdAsync(1)).Should().BeNull();
        }

        [Fact]
        public async Task CrearAsync_PropagaCamposYRetornaElIdGenerado()
        {
            _connection.ScalarResult = 6L;
            var channels = new CollaboratorNotificationChannels(0, 10, true, false, true);

            Int64 id = await _sut.CrearAsync(channels);

            id.Should().Be(6);
            RecordedCommand command = _connection.Commands.Single();
            command.CommandText.Should().Contain("INSERT INTO connection360write.collaborator_notification_channels").And.Contain("RETURNING id_notification_channel");
            command.Parameters["IdCollaborator"].Should().Be(10L);
            command.Parameters["Application"].Should().Be(true);
            command.Parameters["Email"].Should().Be(false);
            command.Parameters["TextMessages"].Should().Be(true);
        }

        [Theory]
        [InlineData(1, true)]
        [InlineData(0, false)]
        public async Task UpdateAsync_RetornaTrueSoloSiAfectoFilas(Int32 rows, Boolean expected)
        {
            _connection.NonQueryResult = rows;
            var channels = new CollaboratorNotificationChannels(15, 10, true, false, true);

            Boolean result = await _sut.UpdateAsync(channels);

            result.Should().Be(expected);
            RecordedCommand command = _connection.Commands.Single();
            command.CommandText.Should().Contain("UPDATE connection360write.collaborator_notification_channels")
                .And.Contain("WHERE id_notification_channel = @IdNotificationChannel")
                .And.Contain("AND id_collaborator = @IdCollaborator");
            command.Parameters["IdNotificationChannel"].Should().Be(15L);
            command.Parameters["IdCollaborator"].Should().Be(10L);
            command.Parameters["Application"].Should().Be(true);
            command.Parameters["Email"].Should().Be(false);
            command.Parameters["TextMessages"].Should().Be(true);
        }
    }
}
