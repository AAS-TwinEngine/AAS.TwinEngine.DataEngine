using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

using AAS.TwinEngine.ExportService.ApplicationLogic.Services.State;
using AAS.TwinEngine.ExportService.DomainModel;
using AAS.TwinEngine.ExportService.ServiceConfiguration.Config;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Npgsql;

namespace AAS.TwinEngine.ExportService.Infrastructure.State.DataAccess;

public sealed class PostgreSqlStateStore : IStateStore
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly string _schema;
    private readonly ILogger<PostgreSqlStateStore> _logger;

    public PostgreSqlStateStore(
        IDbConnectionFactory connectionFactory,
        IOptions<ExportServiceConfig> options,
        ILogger<PostgreSqlStateStore> logger)
    {
        _connectionFactory = connectionFactory;
        _schema = QuoteSchema(options.Value.StateStore.Schema);
        _logger = logger;
    }

    [SuppressMessage("Major Code Smell", "S2077:Formatting SQL queries is security-sensitive",
        Justification = "Schema is whitelist-validated in QuoteSchema; PostgreSQL does not allow identifiers as query parameters.")]
    public async Task EnsureSchemaAsync(CancellationToken cancellationToken)
    {
        var sql = string.Format(CultureInfo.InvariantCulture, StateStoreQueries.CreateSchema, _schema);

        await using var connection = (NpgsqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        _ = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("State store schema '{Schema}' ensured.", _schema);
    }

    [SuppressMessage("Major Code Smell", "S2077:Formatting SQL queries is security-sensitive",
        Justification = "Schema is whitelist-validated in QuoteSchema; PostgreSQL does not allow identifiers as query parameters.")]
    public async Task<IReadOnlyList<ExportedEntity>> LoadAsync(EntityKind kind, CancellationToken cancellationToken)
    {
        var sql = string.Format(CultureInfo.InvariantCulture, StateStoreQueries.SelectByKind, _schema);

        await using var connection = (NpgsqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        _ = command.Parameters.AddWithValue("@entity_kind", kind.ToString());

        var results = new List<ExportedEntity>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            results.Add(new ExportedEntity(
                Enum.Parse<EntityKind>(reader.GetString(0)),
                reader.GetString(1),
                reader.GetFieldValue<DateTimeOffset>(2),
                reader.GetFieldValue<DateTimeOffset>(3)));
        }

        return results;
    }

    [SuppressMessage("Major Code Smell", "S2077:Formatting SQL queries is security-sensitive",
        Justification = "Schema is whitelist-validated in QuoteSchema; PostgreSQL does not allow identifiers as query parameters.")]
    public async Task UpsertAsync(ExportedEntity entity, CancellationToken cancellationToken)
    {
        var sql = string.Format(CultureInfo.InvariantCulture, StateStoreQueries.Upsert, _schema);

        await using var connection = (NpgsqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        _ = command.Parameters.AddWithValue("@entity_kind", entity.Kind.ToString());
        _ = command.Parameters.AddWithValue("@identifier", entity.Identifier);
        _ = command.Parameters.AddWithValue("@created_at", entity.CreatedAt);
        _ = command.Parameters.AddWithValue("@last_synced_at", entity.LastSyncedAt);

        _ = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    [SuppressMessage("Major Code Smell", "S2077:Formatting SQL queries is security-sensitive",
        Justification = "Schema is whitelist-validated in QuoteSchema; PostgreSQL does not allow identifiers as query parameters.")]
    public async Task DeleteAsync(EntityKind kind, string identifier, CancellationToken cancellationToken)
    {
        var sql = string.Format(CultureInfo.InvariantCulture, StateStoreQueries.Delete, _schema);

        await using var connection = (NpgsqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        _ = command.Parameters.AddWithValue("@entity_kind", kind.ToString());
        _ = command.Parameters.AddWithValue("@identifier", identifier);

        _ = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static string QuoteSchema(string schema)
    {
        if (schema.Any(ch => !char.IsLetterOrDigit(ch) && ch != '_'))
        {
            throw new InvalidOperationException(
                $"Invalid schema name '{schema}'. Only letters, digits and underscores are allowed.");
        }

        return schema;
    }
}
