using System.Data;

namespace Connection360.Infrastructure.Persistence
{
    public interface IDbConnectionContext
    {
        IDbConnection Connection { get; }
        IDbTransaction? Transaction { get; }
    }
}
