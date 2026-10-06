using System.Collections;
using System.Data;
using System.Data.Common;
using Connection360.Etl.Infrastructure.Persistence.Repositories;
using Npgsql;

namespace Connection360.Etl.Infrastructure.Tests.TestHelpers
{
    /// <summary>Una ejecución capturada por <see cref="FakeDbConnection"/>.</summary>
    internal sealed class FakeExecution
    {
        public String Kind { get; init; } = String.Empty;
        public String CommandText { get; init; } = String.Empty;
        public Dictionary<String, Object?> Parameters { get; init; } = new(StringComparer.OrdinalIgnoreCase);
        public DbTransaction? Transaction { get; init; }
        public CancellationToken Token { get; init; }
    }

    /// <summary>
    /// Conexión ADO.NET falsa (sin base de datos real). Captura cada comando ejecutado y devuelve los
    /// resultados configurados, lo que permite probar los repositorios Dapper de punta a punta.
    /// </summary>
    internal sealed class FakeDbConnection : DbConnection
    {
        private ConnectionState _state;

        public FakeDbConnection(ConnectionState initialState = ConnectionState.Open) => _state = initialState;

        public List<FakeExecution> Executions { get; } = new();
        public Int32 OpenCalls { get; private set; }
        public Int32 CloseCalls { get; private set; }
        public Boolean Disposed { get; private set; }
        public FakeDbTransaction? LastTransaction { get; private set; }

        /// <summary>Filas afectadas devueltas por cada ExecuteNonQuery.</summary>
        public Func<FakeExecution, Int32> NonQueryResult { get; set; } = _ => 1;

        /// <summary>Valor devuelto por ExecuteScalar.</summary>
        public Func<FakeExecution, Object?> ScalarResult { get; set; } = _ => null;

        /// <summary>Tabla devuelta por ExecuteReader.</summary>
        public Func<FakeExecution, DataTable> ReaderResult { get; set; } = _ => new DataTable();

        public Exception? ThrowOnExecute { get; set; }

        [System.Diagnostics.CodeAnalysis.AllowNull]
        public override String ConnectionString { get; set; } = "fake";
        public override String Database => "fake";
        public override String DataSource => "fake";
        public override String ServerVersion => "0";
        public override ConnectionState State => _state;

        public override void ChangeDatabase(String databaseName) { }

        public override void Close()
        {
            CloseCalls++;
            _state = ConnectionState.Closed;
        }

        public override void Open()
        {
            OpenCalls++;
            _state = ConnectionState.Open;
        }

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            LastTransaction = new FakeDbTransaction(this, isolationLevel);
            return LastTransaction;
        }

        protected override DbCommand CreateDbCommand() => new FakeDbCommand(this);

        protected override void Dispose(Boolean disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }

    internal sealed class FakeDbTransaction : DbTransaction
    {
        private readonly DbConnection _connection;
        private readonly IsolationLevel _isolationLevel;

        public FakeDbTransaction(DbConnection connection, IsolationLevel isolationLevel)
        {
            _connection = connection;
            _isolationLevel = isolationLevel;
        }

        public Boolean Committed { get; private set; }
        public Boolean RolledBack { get; private set; }
        public Boolean Disposed { get; private set; }

        public override IsolationLevel IsolationLevel => _isolationLevel;
        protected override DbConnection? DbConnection => _connection;

        public override void Commit() => Committed = true;
        public override void Rollback() => RolledBack = true;

        protected override void Dispose(Boolean disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }

    internal sealed class FakeDbCommand : DbCommand
    {
        private readonly FakeDbConnection _connection;
        private readonly FakeDbParameterCollection _parameters = new();

        public FakeDbCommand(FakeDbConnection connection) => _connection = connection;

        [System.Diagnostics.CodeAnalysis.AllowNull]
        public override String CommandText { get; set; } = String.Empty;
        public override Int32 CommandTimeout { get; set; }
        public override CommandType CommandType { get; set; } = CommandType.Text;
        public override Boolean DesignTimeVisible { get; set; }
        public override UpdateRowSource UpdatedRowSource { get; set; }
        protected override DbConnection? DbConnection { get => _connection; set { } }
        protected override DbParameterCollection DbParameterCollection => _parameters;
        protected override DbTransaction? DbTransaction { get; set; }

        public override void Cancel() { }
        public override void Prepare() { }

        protected override DbParameter CreateDbParameter() => new FakeDbParameter();

        private FakeExecution Capture(String kind, CancellationToken token = default)
        {
            var execution = new FakeExecution
            {
                Kind = kind,
                CommandText = CommandText,
                Transaction = DbTransaction,
                Token = token,
            };

            foreach (DbParameter parameter in _parameters.Items)
                execution.Parameters[parameter.ParameterName] = parameter.Value is DBNull ? null : parameter.Value;

            _connection.Executions.Add(execution);

            if (_connection.ThrowOnExecute is not null)
                throw _connection.ThrowOnExecute;

            return execution;
        }

        public override Int32 ExecuteNonQuery()
        {
            FakeExecution execution = Capture("NonQuery");
            return _connection.NonQueryResult(execution);
        }

        public override Object? ExecuteScalar()
        {
            FakeExecution execution = Capture("Scalar");
            return _connection.ScalarResult(execution);
        }

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            FakeExecution execution = Capture("Reader");
            return _connection.ReaderResult(execution).CreateDataReader();
        }
    }

    internal sealed class FakeDbParameter : DbParameter
    {
        public override DbType DbType { get; set; }
        public override ParameterDirection Direction { get; set; } = ParameterDirection.Input;
        public override Boolean IsNullable { get; set; }
        [System.Diagnostics.CodeAnalysis.AllowNull]
        public override String ParameterName { get; set; } = String.Empty;
        public override Int32 Size { get; set; }
        [System.Diagnostics.CodeAnalysis.AllowNull]
        public override String SourceColumn { get; set; } = String.Empty;
        public override Boolean SourceColumnNullMapping { get; set; }
        public override Object? Value { get; set; }

        public override void ResetDbType() { }
    }

    internal sealed class FakeDbParameterCollection : DbParameterCollection
    {
        private readonly List<DbParameter> _items = new();

        public IReadOnlyList<DbParameter> Items => _items;

        public override Int32 Count => _items.Count;
        public override Object SyncRoot => ((ICollection)_items).SyncRoot;

        public override Int32 Add(Object value)
        {
            _items.Add((DbParameter)value);
            return _items.Count - 1;
        }

        public override void AddRange(Array values)
        {
            foreach (Object value in values)
                Add(value);
        }

        public override void Clear() => _items.Clear();
        public override Boolean Contains(Object value) => _items.Contains((DbParameter)value);
        public override Boolean Contains(String value) => IndexOf(value) >= 0;
        public override void CopyTo(Array array, Int32 index) => ((ICollection)_items).CopyTo(array, index);
        public override IEnumerator GetEnumerator() => _items.GetEnumerator();
        public override Int32 IndexOf(Object value) => _items.IndexOf((DbParameter)value);
        public override Int32 IndexOf(String parameterName) => _items.FindIndex(p => p.ParameterName == parameterName);
        public override void Insert(Int32 index, Object value) => _items.Insert(index, (DbParameter)value);
        public override void Remove(Object value) => _items.Remove((DbParameter)value);
        public override void RemoveAt(Int32 index) => _items.RemoveAt(index);
        public override void RemoveAt(String parameterName) => _items.RemoveAt(IndexOf(parameterName));
        protected override DbParameter GetParameter(Int32 index) => _items[index];
        protected override DbParameter GetParameter(String parameterName) => _items[IndexOf(parameterName)];
        protected override void SetParameter(Int32 index, DbParameter value) => _items[index] = value;
        protected override void SetParameter(String parameterName, DbParameter value) => _items[IndexOf(parameterName)] = value;
    }

    /// <summary>
    /// Crea un <see cref="DbSession"/> real (sellado) cuya conexión se reemplaza por una
    /// <see cref="FakeDbConnection"/>: ninguna prueba abre una conexión de red.
    /// </summary>
    internal sealed class FakeSessionScope : IAsyncDisposable
    {
        private readonly NpgsqlDataSource _dataSource;

        public FakeDbConnection Connection { get; }
        public DbSession Session { get; }

        public FakeSessionScope(ConnectionState initialState = ConnectionState.Open)
        {
            Connection = new FakeDbConnection(initialState);
            _dataSource = NpgsqlDataSource.Create("Host=localhost;Database=fake;Username=fake;Password=fake");
            Session = new DbSession(_dataSource);

            // Se descarta la NpgsqlConnection real (nunca abierta) y se reemplaza por la falsa.
            Session.Connection.Dispose();
            typeof(DbSession).GetProperty(nameof(DbSession.Connection))!.GetSetMethod(nonPublic: true)!
                .Invoke(Session, new Object[] { Connection });
        }

        public async ValueTask DisposeAsync()
        {
            await _dataSource.DisposeAsync();
        }
    }
}
