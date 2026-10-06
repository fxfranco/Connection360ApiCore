using System.Collections;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Connection360.Infrastructure.Persistence.Repositories;
using Npgsql;

namespace Connection360.Infrastructure.Tests.TestHelpers
{
    /// <summary>Habilita el mapeo snake_case (igual que Connection360.Api/Program.cs) antes de que Dapper cachee mapeos.</summary>
    internal static class DapperTestInitializer
    {
        [ModuleInitializer]
        internal static void Init() => Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    /// <summary>Comando ejecutado contra <see cref="FakeDbConnection"/>, registrado para verificar SQL y parametros.</summary>
    internal sealed class RecordedCommand
    {
        public String CommandText { get; init; } = String.Empty;
        public Dictionary<String, Object?> Parameters { get; init; } = new();
        public DbTransaction? Transaction { get; init; }
    }

    /// <summary>
    /// DbConnection en memoria: no abre ningun socket. Responde a los comandos con un DataTable
    /// (lectores), un escalar o un numero de filas afectadas configurables, y registra cada comando.
    /// </summary>
    internal sealed class FakeDbConnection : DbConnection
    {
        private ConnectionState _state = ConnectionState.Closed;

        public DataTable? ReaderResult { get; set; }
        public Object? ScalarResult { get; set; }
        public Int32 NonQueryResult { get; set; }
        public Exception? ExceptionToThrow { get; set; }

        public List<RecordedCommand> Commands { get; } = new();
        public Int32 OpenCount { get; private set; }
        public Int32 CloseCount { get; private set; }
        public Boolean Disposed { get; private set; }
        public FakeDbTransaction? LastTransaction { get; private set; }

        [AllowNull]
        public override String ConnectionString { get; set; } = "fake";
        public override String Database => "fake";
        public override String DataSource => "fake";
        public override String ServerVersion => "0";
        public override ConnectionState State => _state;

        public override void ChangeDatabase(String databaseName) { }

        public override void Open()
        {
            OpenCount++;
            _state = ConnectionState.Open;
        }

        public override void Close()
        {
            CloseCount++;
            _state = ConnectionState.Closed;
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

        public static DataTable Table(String[] columns, params Object?[][] rows)
        {
            var table = new DataTable();
            for (Int32 i = 0; i < columns.Length; i++)
            {
                // El tipo de la columna se infiere del primer valor no nulo; si no hay filas, object.
                Type type = rows.Select(r => r[i]).FirstOrDefault(v => v != null)?.GetType() ?? typeof(String);
                table.Columns.Add(columns[i], type);
            }
            foreach (Object?[] row in rows)
            {
                table.Rows.Add(row.Select(v => v ?? DBNull.Value).ToArray());
            }
            return table;
        }
    }

    internal sealed class FakeDbTransaction : DbTransaction
    {
        private readonly FakeDbConnection _connection;
        private readonly IsolationLevel _isolationLevel;

        public FakeDbTransaction(FakeDbConnection connection, IsolationLevel isolationLevel)
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
        private readonly FakeParameterCollection _parameters = new();

        public FakeDbCommand(FakeDbConnection connection) => _connection = connection;

        [AllowNull]
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

        protected override DbParameter CreateDbParameter() => new FakeParameter();

        private void Record()
        {
            _connection.Commands.Add(new RecordedCommand
            {
                CommandText = CommandText,
                Parameters = _parameters.Items.ToDictionary(p => p.ParameterName.TrimStart('@'), p => p.Value is DBNull ? null : p.Value),
                Transaction = DbTransaction
            });
            if (_connection.ExceptionToThrow != null)
            {
                throw _connection.ExceptionToThrow;
            }
        }

        public override Int32 ExecuteNonQuery()
        {
            Record();
            return _connection.NonQueryResult;
        }

        public override Object? ExecuteScalar()
        {
            Record();
            return _connection.ScalarResult;
        }

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            Record();
            return (_connection.ReaderResult ?? new DataTable()).CreateDataReader();
        }
    }

    internal sealed class FakeParameter : DbParameter
    {
        public override DbType DbType { get; set; }
        public override ParameterDirection Direction { get; set; } = ParameterDirection.Input;
        public override Boolean IsNullable { get; set; }
        [AllowNull]
        public override String ParameterName { get; set; } = String.Empty;
        public override Int32 Size { get; set; }
        [AllowNull]
        public override String SourceColumn { get; set; } = String.Empty;
        public override Boolean SourceColumnNullMapping { get; set; }
        public override Object? Value { get; set; }
        public override void ResetDbType() { }
    }

    internal sealed class FakeParameterCollection : DbParameterCollection
    {
        private readonly List<DbParameter> _items = new();

        public IReadOnlyList<DbParameter> Items => _items;
        public override Int32 Count => _items.Count;
        public override Object SyncRoot => ((ICollection)_items).SyncRoot;

        public override Int32 Add(Object value) { _items.Add((DbParameter)value); return _items.Count - 1; }
        public override void AddRange(Array values) { foreach (Object v in values) Add(v); }
        public override void Clear() => _items.Clear();
        public override Boolean Contains(Object value) => _items.Contains((DbParameter)value);
        public override Boolean Contains(String value) => _items.Any(p => p.ParameterName == value);
        public override void CopyTo(Array array, Int32 index) => ((ICollection)_items).CopyTo(array, index);
        public override IEnumerator GetEnumerator() => _items.GetEnumerator();
        public override Int32 IndexOf(Object value) => _items.IndexOf((DbParameter)value);
        public override Int32 IndexOf(String parameterName) => _items.FindIndex(p => p.ParameterName == parameterName);
        public override void Insert(Int32 index, Object value) => _items.Insert(index, (DbParameter)value);
        public override void Remove(Object value) => _items.Remove((DbParameter)value);
        public override void RemoveAt(Int32 index) => _items.RemoveAt(index);
        public override void RemoveAt(String parameterName) => _items.RemoveAll(p => p.ParameterName == parameterName);
        protected override DbParameter GetParameter(Int32 index) => _items[index];
        protected override DbParameter GetParameter(String parameterName) => _items.First(p => p.ParameterName == parameterName);
        protected override void SetParameter(Int32 index, DbParameter value) => _items[index] = value;
        protected override void SetParameter(String parameterName, DbParameter value) => _items[IndexOf(parameterName)] = value;
    }

    /// <summary>Fabrica DbSession reales cuya conexion es un <see cref="FakeDbConnection"/> (sin base de datos).</summary>
    internal static class TestDbSession
    {
        public static DbSession Create(FakeDbConnection connection)
        {
            // NpgsqlDataSource.Create no abre conexiones; CreateConnection tampoco.
            var dataSource = NpgsqlDataSource.Create("Host=localhost;Database=test;Username=test;Password=test");
            var session = new DbSession(dataSource);
            session.Connection.Dispose();
            typeof(DbSession).GetProperty(nameof(DbSession.Connection))!.SetValue(session, connection);
            return session;
        }
    }
}
