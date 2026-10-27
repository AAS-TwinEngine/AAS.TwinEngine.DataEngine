using System.Data.Common;

namespace AAS.TwinEngine.ExportService.Infrastructure.State.DataAccess;

/// <summary>
/// Creates a fresh DbConnection for the state store.
/// </summary>
public interface IDbConnectionFactory
{
    DbConnection CreateConnection();
}
