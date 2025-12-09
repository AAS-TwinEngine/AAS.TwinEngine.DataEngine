using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasRegistry;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.AasRegistryProvider.Config;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.AasRegistryProvider.Services;

using Cronos;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

namespace AAS.TwinEngine.DataEngine.UnitTests.Infrastructure.Providers.AasRegistryProvider;

public class ShellDescriptorSyncHostedTests
{
    private readonly IShellDescriptorService _syncService = Substitute.For<IShellDescriptorService>();
    private readonly ILogger<ShellDescriptorSyncHosted> _logger = Substitute.For<ILogger<ShellDescriptorSyncHosted>>();
    private readonly IServiceScopeFactory _scopeFactory = Substitute.For<IServiceScopeFactory>();
    private const string ValidCron = "0/1 * * * * ?";

    private ShellDescriptorSyncHosted CreateService(string? cron = null)
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IShellDescriptorService)).Returns(_syncService);
        serviceProvider.GetRequiredService<IShellDescriptorService>().Returns(_syncService);

        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(serviceProvider);
        _scopeFactory.CreateScope().Returns(scope);

        var config = Options.Create(new AasRegistryPreComputed { ShellDescriptorCron = cron ?? ValidCron, IsPreComputed = true });

        return new ShellDescriptorSyncHosted(_scopeFactory, config, _logger);
    }

    [Fact]
    public async Task RunOnceAsync_Should_Invoke_SyncShellDescriptorsAsync()
    {
        // Arrange
        using var service = CreateService();

        // Act
        await service.RunOnceAsync(CancellationToken.None);

        // Assert
        await _syncService.Received(1).SyncShellDescriptorsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunOnceAsync_Should_Log_And_Throw_ResourceNotFoundException_When_Exception_Occurs()
    {
        // Arrange
        _syncService.When(x => x.SyncShellDescriptorsAsync(Arg.Any<CancellationToken>()))
                    .Do(_ => throw new ResourceNotFoundException());

        using var service = CreateService();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ResourceNotFoundException>(() => service.RunOnceAsync(CancellationToken.None));
    }

    [Theory]
    [InlineData("* * *", typeof(CronFormatException))]
    [InlineData("* * * * * ?", null)] // Valid cron
    public void Should_Throw_When_Cron_Is_Invalid(string cron, Type? expectedExceptionType)
    {
        // Arrange
        var config = Options.Create(new AasRegistryPreComputed { ShellDescriptorCron = cron, IsPreComputed = true });

        // Act & Assert
        if (expectedExceptionType == null)
        {
            using var instance = new ShellDescriptorSyncHosted(_scopeFactory, config, _logger);
        }
        else
        {
            Assert.Throws(expectedExceptionType, () =>
            {
                using var instance = new ShellDescriptorSyncHosted(_scopeFactory, config, _logger);
            });
        }
    }

    [Fact]
    public async Task RunOnceAsync_Should_Not_Invoke_SyncShellDescriptorsAsync_When_PreComputed_Is_False()
    {
        // Arrange
        var config = Options.Create(new AasRegistryPreComputed
        {
            ShellDescriptorCron = ValidCron,
            IsPreComputed = false
        });

        var service = new ShellDescriptorSyncHosted(_scopeFactory, config, _logger);

        // Act
        await service.RunOnceAsync(CancellationToken.None);

        // Assert
        await _syncService.DidNotReceive().SyncShellDescriptorsAsync(Arg.Any<CancellationToken>());

        _logger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Skipping ShellDescriptor sync as Pre-Computed set to false.")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>()
        );
    }

}
