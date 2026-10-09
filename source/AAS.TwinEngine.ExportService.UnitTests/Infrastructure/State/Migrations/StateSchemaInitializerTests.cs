using AAS.TwinEngine.ExportService.ApplicationLogic.Services.State;
using AAS.TwinEngine.ExportService.Infrastructure.State.Migrations;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NSubstitute;

namespace AAS.TwinEngine.ExportService.UnitTests.Infrastructure.State.Migrations;

public class StateSchemaInitializerTests
{
    private readonly IServiceProvider _serviceProvider = Substitute.For<IServiceProvider>();
    private readonly ILogger<StateSchemaInitializer> _logger = Substitute.For<ILogger<StateSchemaInitializer>>();
    private readonly IStateStore _stateStore = Substitute.For<IStateStore>();

    [Fact]
    public async Task StartAsync_CreatesScopeAndCallsEnsureSchemaAsync()
    {
        // Arrange
        var scope = Substitute.For<IServiceScope>();
        var scopedProvider = Substitute.For<IServiceProvider>();
        scopedProvider.GetService(typeof(IStateStore)).Returns(_stateStore);
        scope.ServiceProvider.Returns(scopedProvider);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);
        _serviceProvider.GetService(typeof(IServiceScopeFactory)).Returns(scopeFactory);

        var sut = new StateSchemaInitializer(_serviceProvider, _logger);
        using var cts = new CancellationTokenSource();

        // Act
        await sut.StartAsync(cts.Token);

        // Assert
        await _stateStore.Received(1).EnsureSchemaAsync(cts.Token);
        scope.Received(1).Dispose();
    }

    [Fact]
    public async Task StopAsync_ReturnsCompletedTask()
    {
        // Arrange
        var sut = new StateSchemaInitializer(_serviceProvider, _logger);

        // Act
        var task = sut.StopAsync(CancellationToken.None);

        // Assert
        await task;
        Assert.True(task.IsCompletedSuccessfully);
    }
}
