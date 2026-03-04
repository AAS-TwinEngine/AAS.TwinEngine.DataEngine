using AAS.TwinEngine.Plugin.TestPlugin.Infrastructure.Monitoring;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

using NSubstitute;

namespace AAS.TwinEngine.Plugin.TestPlugin.UnitTests.Infrastructure.Monitoring;

public class MockDataHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_ShouldBeHealthy_WhenBothFilesExist()
    {
        // Arrange
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var dataFolder = Path.Combine(tempRoot, "Data");
        Directory.CreateDirectory(dataFolder);

        await File.WriteAllTextAsync(Path.Combine(dataFolder, "mock-metadata.json"), "{}", CancellationToken.None);
        await File.WriteAllTextAsync(Path.Combine(dataFolder, "mock-submodel-data.json"), "[]", CancellationToken.None);

        var hostEnvironment = Substitute.For<IHostEnvironment>();
        hostEnvironment.ContentRootPath.Returns(tempRoot);

        var sut = new MockDataHealthCheck(hostEnvironment);

        // Act
        var result = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldBeUnhealthy_WhenAnyFileIsMissing()
    {
        // Arrange
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var dataFolder = Path.Combine(tempRoot, "Data");
        Directory.CreateDirectory(dataFolder);

        await File.WriteAllTextAsync(Path.Combine(dataFolder, "mock-metadata.json"), "{}", CancellationToken.None);

        var hostEnvironment = Substitute.For<IHostEnvironment>();
        hostEnvironment.ContentRootPath.Returns(tempRoot);

        var sut = new MockDataHealthCheck(hostEnvironment);

        // Act
        var result = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        // Assert
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }
}
