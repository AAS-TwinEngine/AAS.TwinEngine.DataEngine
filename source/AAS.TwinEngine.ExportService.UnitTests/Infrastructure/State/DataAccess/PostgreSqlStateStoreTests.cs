using AAS.TwinEngine.ExportService.DomainModel;
using AAS.TwinEngine.ExportService.Infrastructure.State.DataAccess;
using AAS.TwinEngine.ExportService.ServiceConfiguration.Config;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Npgsql;

using NSubstitute;

namespace AAS.TwinEngine.ExportService.UnitTests.Infrastructure.State.DataAccess;

public class PostgreSqlStateStoreTests
{
    private readonly IDbConnectionFactory _connectionFactory = Substitute.For<IDbConnectionFactory>();
    private readonly ILogger<PostgreSqlStateStore> _logger = Substitute.For<ILogger<PostgreSqlStateStore>>();

    private PostgreSqlStateStore CreateSut(string schema = "export_service")
    {
        var options = Options.Create(new ExportServiceConfig
        {
            StateStore = new StateStoreConfig
            {
                ConnectionString = "Host=localhost;Database=test;Username=postgres;Password=postgres",
                Schema = schema
            }
        });

        return new PostgreSqlStateStore(_connectionFactory, options, _logger);
    }

    [Theory]
    [InlineData("valid_schema")]
    [InlineData("export_service")]
    [InlineData("Schema123")]
    [InlineData("test_schema_42")]
    public void Constructor_WhenSchemaIsValid_InitializesSuccessfully(string validSchema)
    {
        var sut = CreateSut(validSchema);
        Assert.NotNull(sut);
    }

    [Theory]
    [InlineData("export-service")] // hyphen not allowed
    [InlineData("export.service")] // dot not allowed
    [InlineData("export; DROP TABLE test;")] // SQL injection
    [InlineData("export\"schema")] // quote
    [InlineData("export'schema")] // single quote
    [InlineData("export service")] // space
    [InlineData("export/service")] // slash
    public void Constructor_WhenSchemaContainsInvalidCharacters_ThrowsInvalidOperationException(string invalidSchema)
    {
        var ex = Assert.Throws<InvalidOperationException>(() => CreateSut(invalidSchema));
        Assert.Contains("Invalid schema name", ex.Message);
    }

    [Fact]
    public void StateStoreQueries_ContainExpectedPlaceholdersAndParameters()
    {
        // Assert queries format properly with schema
        var schema = "custom_schema";
        var createSql = string.Format(System.Globalization.CultureInfo.InvariantCulture, StateStoreQueries.CreateSchema, schema);
        Assert.Contains("CREATE SCHEMA IF NOT EXISTS custom_schema", createSql);
        Assert.Contains("CREATE TABLE IF NOT EXISTS custom_schema.exported_entities", createSql);
        Assert.Contains("DROP COLUMN IF EXISTS content_hash", createSql);

        var selectSql = string.Format(System.Globalization.CultureInfo.InvariantCulture, StateStoreQueries.SelectByKind, schema);
        Assert.Contains("FROM custom_schema.exported_entities", selectSql);
        Assert.Contains("@entity_kind", selectSql);

        var upsertSql = string.Format(System.Globalization.CultureInfo.InvariantCulture, StateStoreQueries.Upsert, schema);
        Assert.Contains("INSERT INTO custom_schema.exported_entities", upsertSql);
        Assert.Contains("@entity_kind", upsertSql);
        Assert.Contains("@identifier", upsertSql);
        Assert.Contains("@created_at", upsertSql);
        Assert.Contains("@last_synced_at", upsertSql);

        var deleteSql = string.Format(System.Globalization.CultureInfo.InvariantCulture, StateStoreQueries.Delete, schema);
        Assert.Contains("DELETE FROM custom_schema.exported_entities", deleteSql);
        Assert.Contains("@entity_kind", deleteSql);
        Assert.Contains("@identifier", deleteSql);
    }

    [Fact]
    public async Task EnsureSchemaAsync_WhenCancelled_PropagatesCancellation()
    {
        // Arrange
        _connectionFactory.CreateConnection().Returns(new NpgsqlConnection("Host=localhost;Database=test;Username=postgres;Password=postgres"));
        var sut = CreateSut();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sut.EnsureSchemaAsync(cts.Token));
        _connectionFactory.Received(1).CreateConnection();
    }

    [Fact]
    public async Task LoadAsync_WhenCancelled_PropagatesCancellation()
    {
        // Arrange
        _connectionFactory.CreateConnection().Returns(new NpgsqlConnection("Host=localhost;Database=test;Username=postgres;Password=postgres"));
        var sut = CreateSut();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sut.LoadAsync(EntityKind.Shell, cts.Token));
        _connectionFactory.Received(1).CreateConnection();
    }

    [Fact]
    public async Task UpsertAsync_WhenCancelled_PropagatesCancellation()
    {
        // Arrange
        _connectionFactory.CreateConnection().Returns(new NpgsqlConnection("Host=localhost;Database=test;Username=postgres;Password=postgres"));
        var sut = CreateSut();
        var entity = new ExportedEntity(EntityKind.Shell, "id1", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sut.UpsertAsync(entity, cts.Token));
        _connectionFactory.Received(1).CreateConnection();
    }

    [Fact]
    public async Task DeleteAsync_WhenCancelled_PropagatesCancellation()
    {
        // Arrange
        _connectionFactory.CreateConnection().Returns(new NpgsqlConnection("Host=localhost;Database=test;Username=postgres;Password=postgres"));
        var sut = CreateSut();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sut.DeleteAsync(EntityKind.Shell, "id1", cts.Token));
        _connectionFactory.Received(1).CreateConnection();
    }
}
