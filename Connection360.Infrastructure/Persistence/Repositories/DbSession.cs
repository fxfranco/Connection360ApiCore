using Npgsql;
using System.Data;
using System.Data.Common;

namespace Connection360.Infrastructure.Persistence.Repositories
{
    public sealed class DbSession : IAsyncDisposable
    {
        private readonly NpgsqlDataSource _dataSource;

        // Cambiamos a DbConnection y DbTransaction de System.Data.Common
        public DbConnection Connection { get; private set; }
        public DbTransaction? Transaction { get; set; }

        public DbSession(NpgsqlDataSource dataSource)
        {
            _dataSource = dataSource;
            // La conexión se crea a demanda usando el DataSource singleton
            Connection = _dataSource.CreateConnection();
        }

        public async Task EnsureConnectionOpenAsync(CancellationToken cancellationToken = default)
        {
            if (Connection.State != ConnectionState.Open)
            {
                if (Connection.State != ConnectionState.Open)
                {
                    await Connection.OpenAsync(cancellationToken);
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (Transaction != null)
            {
                await Transaction.DisposeAsync();
                Transaction = null;
            }

            if (Connection != null)
            {
                await Connection.CloseAsync();
                await Connection.DisposeAsync();
            }
        }
    }
}
