using System.Globalization;
using System.Text;

using AAS.TwinEngine.ExportService.ApplicationLogic.Services.State;
using AAS.TwinEngine.ExportService.DomainModel;
using AAS.TwinEngine.ExportService.ServiceConfiguration.Config;

using Microsoft.Extensions.Options;

using Npgsql;

namespace AAS.TwinEngine.ExportService.Infrastructure.State.DataAccess;

public sealed class PostgreSqlStateStore(
    IDbConnectionFactory connectionFactory,
    IOptions<ExportServiceConfig> options,
    ILogger<PostgreSqlStateStore> logger) : IStateStore
{
    private const string EntityKindParameter = "@entity_kind";

    private static readonly CompositeFormat CreateSchemaFormat = CompositeFormat.Parse(StateStoreQueries.CreateSchema);
    private static readonly CompositeFormat SelectByKindFormat = CompositeFormat.Parse(StateStoreQueries.SelectByKind);
    private static readonly CompositeFormat SelectByKindAndIdentifierFormat = CompositeFormat.Parse(StateStoreQueries.SelectByKindAndIdentifier);
    private static readonly CompositeFormat UpsertFormat = CompositeFormat.Parse(StateStoreQueries.Upsert);
    private static readonly CompositeFormat DeleteFormat = CompositeFormat.Parse(StateStoreQueries.Delete);

    private readonly IDbConnectionFactory _connectionFactory = connectionFactory;
    private readonly string _schema = QuoteSchema(options.Value.StateStore.Schema);
    private readonly ILogger<PostgreSqlStateStore> _logger = logger;

    public async Task EnsureSchemaAsync(CancellationToken cancellationToken)
    {
        var sql = string.Format(CultureInfo.InvariantCulture, CreateSchemaFormat, _schema);

        await using var connection = (NpgsqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = CreateCommand(connection, sql);
        _ = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("State store schema '{Schema}' ensured.", _schema);
    }

    public async Task<IReadOnlyList<ExportedEntity>> LoadAsync(EntityKind kind, CancellationToken cancellationToken)
    {
        var sql = string.Format(CultureInfo.InvariantCulture, SelectByKindFormat, _schema);

        await using var connection = (NpgsqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = CreateCommand(connection, sql);
        _ = command.Parameters.AddWithValue(EntityKindParameter, kind.ToString());

        var results = new List<ExportedEntity>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            results.Add(await ReadEntityAsync(reader, cancellationToken).ConfigureAwait(false));
        }

        return results;
    }

    public async Task<ExportedEntity?> GetAsync(EntityKind kind, string identifier, CancellationToken cancellationToken)
    {
        var sql = string.Format(CultureInfo.InvariantCulture, SelectByKindAndIdentifierFormat, _schema);

        await using var connection = (NpgsqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = CreateCommand(connection, sql);
        _ = command.Parameters.AddWithValue(EntityKindParameter, kind.ToString());
        _ = command.Parameters.AddWithValue("@identifier", identifier);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        return await ReadEntityAsync(reader, cancellationToken).ConfigureAwait(false);
    }

    public async Task UpsertAsync(ExportedEntity entity, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var sql = string.Format(CultureInfo.InvariantCulture, UpsertFormat, _schema);

        await using var connection = (NpgsqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = CreateCommand(connection, sql);
        _ = command.Parameters.AddWithValue(EntityKindParameter, entity.Kind.ToString());
        _ = command.Parameters.AddWithValue("@identifier", entity.Identifier);
        _ = command.Parameters.AddWithValue("@content_hash", entity.ContentHash!);
        _ = command.Parameters.AddWithValue("@created_at", entity.CreatedAt);
        _ = command.Parameters.AddWithValue("@last_synced_at", entity.LastSyncedAt);

        _ = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(EntityKind kind, string identifier, CancellationToken cancellationToken)
    {
        var sql = string.Format(CultureInfo.InvariantCulture, DeleteFormat, _schema);

        await using var connection = (NpgsqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = CreateCommand(connection, sql);
        _ = command.Parameters.AddWithValue(EntityKindParameter, kind.ToString());
        _ = command.Parameters.AddWithValue("@identifier", identifier);

        _ = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<ExportedEntity> ReadEntityAsync(NpgsqlDataReader reader, CancellationToken cancellationToken)
    {
        var contentHash = await reader.IsDBNullAsync(2, cancellationToken).ConfigureAwait(false)
            ? null
            : reader.GetString(2);

        return new ExportedEntity(
            Enum.Parse<EntityKind>(reader.GetString(0)),
            reader.GetString(1),
            await reader.GetFieldValueAsync<DateTimeOffset>(3, cancellationToken).ConfigureAwait(false),
            await reader.GetFieldValueAsync<DateTimeOffset>(4, cancellationToken).ConfigureAwait(false),
            contentHash);
    }

    private static NpgsqlCommand CreateCommand(NpgsqlConnection connection, string sql)
    {
        var command = connection.CreateCommand();
        command.CommandText = sql;
        return command;
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
