using AAS.TwinEngine.DataEngine.ServiceConfiguration;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using NSubstitute;

namespace AAS.TwinEngine.DataEngine.UnitTests.ServiceConfiguration;

public class ConfigurationWarningLoggerTests
{
    [Fact]
    public void LogDeprecatedSections_ShouldLogWarning_WhenPreComputedSectionExists()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{ConfigurationWarningLogger.PreComputedSection}:ShellDescriptorCron"] = "0 */3 * * * *"
            })
            .Build();
        var logger = Substitute.For<ILogger>();

        // Act
        ConfigurationWarningLogger.LogDeprecatedSections(configuration, logger);

        // Assert
        logger.Received().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(state => state.ToString()!.Contains(ConfigurationWarningLogger.PreComputedSection)),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void LogDeprecatedSections_ShouldNotLogWarning_WhenPreComputedSectionIsMissing()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();
        var logger = Substitute.For<ILogger>();

        // Act
        ConfigurationWarningLogger.LogDeprecatedSections(configuration, logger);

        // Assert
        logger.DidNotReceive().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }
}
