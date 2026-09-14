using AAS.TwinEngine.ExportService.Infrastructure.State.DataAccess;
using AAS.TwinEngine.ExportService.ServiceConfiguration.Config;

using Microsoft.Extensions.Options;

using Npgsql;

namespace AAS.TwinEngine.ExportService.UnitTests.Infrastructure.State.DataAccess;

public class PostgreSqlConnectionFactoryTests
{
    [Fact]
    public void Constructor_WhenValidConnectionString_CreatesFactoryAndConnection()
    {
        // Arrange
        const string connString = "Host=localhost;Database=testdb;Username=postgres;Password=postgres";
        var options = Options.Create(new ExportServiceConfig
        {
            StateStore = new StateStoreConfig { ConnectionString = connString }
        });

        // Act
        var factory = new PostgreSqlConnectionFactory(options);
        using var connection = factory.CreateConnection();

        // Assert
        Assert.NotNull(connection);
        var npgsqlConn = Assert.IsType<NpgsqlConnection>(connection);
        Assert.Equal(connString, npgsqlConn.ConnectionString);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WhenConnectionStringNullOrWhitespace_ThrowsInvalidOperationException(string? connString)
    {
        var options = Options.Create(new ExportServiceConfig
        {
            StateStore = new StateStoreConfig { ConnectionString = connString! }
        });

        var ex = Assert.Throws<InvalidOperationException>(() => new PostgreSqlConnectionFactory(options));
        Assert.Contains("ConnectionString is not configured", ex.Message);
    }

    [Fact]
    public void Constructor_WhenConnectionStringMalformed_ThrowsInvalidOperationException()
    {
        const string malformed = "Invalid Key Without Equals Sign";
        var options = Options.Create(new ExportServiceConfig
        {
            StateStore = new StateStoreConfig { ConnectionString = malformed }
        });

        var ex = Assert.Throws<InvalidOperationException>(() => new PostgreSqlConnectionFactory(options));
        Assert.Contains("not a valid PostgreSQL connection string", ex.Message);
    }
}
