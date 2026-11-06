using AAS.TwinEngine.ExportService.ServiceConfiguration.Config;

namespace AAS.TwinEngine.ExportService.UnitTests.ServiceConfiguration.Config;

public class ExportServiceConfigValidatorTests
{
    private readonly ExportServiceConfigValidator _sut = new();

    [Fact]
    public void Validate_WhenConfigurationIsValid_ReturnsSuccess()
    {
        // Act
        var result = _sut.Validate(null, CreateValidConfig());

        // Assert
        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData("Sources.Shells", "BaseUrl")]
    [InlineData("Sources.Shells", "Path")]
    [InlineData("Targets.Submodels", "BaseUrl")]
    [InlineData("Targets.Submodels", "Path")]
    public void Validate_WhenEnabledEndpointIsMissingBaseUrlOrPath_ReturnsFailure(string endpoint, string property)
    {
        // Arrange
        var config = CreateValidConfig();
        var invalidEndpoint = endpoint == "Sources.Shells" ? config.Sources.Shells : config.Targets.Submodels;
        if (property == "BaseUrl")
        {
            invalidEndpoint.BaseUrl = string.Empty;
        }
        else
        {
            invalidEndpoint.Path = string.Empty;
        }

        // Act
        var result = _sut.Validate(null, config);

        // Assert
        Assert.True(result.Failed);
        Assert.Contains(result.Failures, failure => failure.Contains($"{endpoint}.{property}", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/healthz")]
    [InlineData("/status/ready")]
    [InlineData("/")]
    public void Validate_WhenHealthEndpointStartsWithSingleSlash_ReturnsSuccess(string healthEndpoint)
    {
        var config = CreateValidConfig();
        config.Sources.Shells.HealthEndpoint = healthEndpoint;

        var result = _sut.Validate(null, config);

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("file:///tmp/health")]
    [InlineData("https://dependency.test/health")]
    [InlineData("//dependency.test/health")]
    [InlineData("///health")]
    [InlineData("health")]
    [InlineData("/health\\ready")]
    public void Validate_WhenHealthEndpointIsNotAValidRelativePath_ReturnsFailure(string healthEndpoint)
    {
        // Arrange
        var config = CreateValidConfig();
        config.Sources.Shells.HealthEndpoint = healthEndpoint;

        // Act
        var result = _sut.Validate(null, config);

        // Assert
        Assert.True(result.Failed);
        Assert.Contains(result.Failures, failure => failure.Contains("Sources.Shells.HealthEndpoint", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_WhenDisabledEndpointIsMissingBaseUrlAndPath_ReturnsSuccess()
    {
        // Arrange
        var config = CreateValidConfig();
        config.Sources.Shells.Enabled = false;
        config.Sources.Shells.BaseUrl = string.Empty;
        config.Sources.Shells.Path = string.Empty;

        // Act
        var result = _sut.Validate(null, config);

        // Assert
        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_WhenSourceDisabled_DoesNotRequireMatchingTargetConfiguration()
    {
        var config = CreateValidConfig();
        config.Sources.Submodels = new EndpointConfig { Enabled = false };
        config.Targets.Submodels = new EndpointConfig();

        var result = _sut.Validate(null, config);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_WhenSourceEnabled_RequiresTargetEvenWhenLegacyTargetFlagIsFalse()
    {
        var config = CreateValidConfig();
        config.Targets.Submodels = new EndpointConfig
        {
            Enabled = false,
            BaseUrl = "https://target.test",
            Path = string.Empty
        };

        var result = _sut.Validate(null, config);

        Assert.True(result.Failed);
        Assert.Contains(result.Failures, failure => failure.Contains("Targets.Submodels.Path", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("not a cron")]
    [InlineData("")]
    public void Validate_WhenCronExpressionIsInvalid_ReturnsFailure(string cronExpression)
    {
        // Arrange
        var config = CreateValidConfig();
        config.Scheduler.CronExpression = cronExpression;

        // Act
        var result = _sut.Validate(null, config);

        // Assert
        Assert.True(result.Failed);
        Assert.Contains(result.Failures, failure => failure.Contains("Scheduler.CronExpression", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_WhenRunTimeoutIsNotPositive_ReturnsFailure()
    {
        // Arrange
        var config = CreateValidConfig();
        config.Scheduler.RunTimeoutMinutes = 0;

        // Act
        var result = _sut.Validate(null, config);

        // Assert
        Assert.True(result.Failed);
        Assert.Contains(result.Failures, failure => failure.Contains("Scheduler.RunTimeoutMinutes", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_WhenHttpTimeoutIsNotPositive_ReturnsFailure()
    {
        // Arrange
        var config = CreateValidConfig();
        config.Resilience.TimeoutInSeconds = 0;

        // Act
        var result = _sut.Validate(null, config);

        // Assert
        Assert.True(result.Failed);
        Assert.Contains(result.Failures, failure => failure.Contains("Resilience.TimeoutInSeconds", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(0.5)]
    [InlineData(double.PositiveInfinity)]
    public void Validate_WhenBackoffMultiplierIsInvalid_ReturnsFailure(double multiplier)
    {
        // Arrange
        var config = CreateValidConfig();
        config.Resilience.BackoffMultiplier = multiplier;

        // Act
        var result = _sut.Validate(null, config);

        // Assert
        Assert.True(result.Failed);
        Assert.Contains(result.Failures, failure => failure.Contains("Resilience.BackoffMultiplier", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_WhenEnabledSourceLimitIsNotPositive_ReturnsFailure()
    {
        // Arrange
        var config = CreateValidConfig();
        config.Sources.Shells.Limit = 0;

        // Act
        var result = _sut.Validate(null, config);

        // Assert
        Assert.True(result.Failed);
        Assert.Contains(result.Failures, failure => failure.Contains("Sources.Shells.Limit", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_WhenConnectionStringIsMissing_ReturnsFailure()
    {
        // Arrange
        var config = CreateValidConfig();
        config.StateStore.ConnectionString = " ";

        // Act
        var result = _sut.Validate(null, config);

        // Assert
        Assert.True(result.Failed);
        Assert.Contains(result.Failures, failure => failure.Contains("StateStore.ConnectionString", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WhenMaxDegreeOfParallelismIsNotPositive_ReturnsFailure(int degree)
    {
        // Arrange
        var config = CreateValidConfig();
        config.Performance.MaxDegreeOfParallelism = degree;

        // Act
        var result = _sut.Validate(null, config);

        // Assert
        Assert.True(result.Failed);
        Assert.Contains(result.Failures, failure => failure.Contains("Performance.MaxDegreeOfParallelism", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_WhenSourcePageRequestDelayIsNegative_ReturnsFailure()
    {
        var config = CreateValidConfig();
        config.Performance.SourcePageRequestDelayMilliseconds = -1;

        var result = _sut.Validate(null, config);

        Assert.True(result.Failed);
        Assert.Contains(result.Failures, failure => failure.Contains("Performance.SourcePageRequestDelayMilliseconds", StringComparison.Ordinal));
    }

    private static ExportServiceConfig CreateValidConfig()
    {
        var endpoint = new EndpointConfig
        {
            Enabled = true,
            BaseUrl = "https://example.test",
            Path = "/api/entities"
        };

        return new ExportServiceConfig
        {
            Scheduler = new SchedulerConfig
            {
                CronExpression = "*/5 * * * *",
                RunTimeoutMinutes = 30
            },
            StateStore = new StateStoreConfig { ConnectionString = "Host=localhost;Database=exportstate" },
            Sources = new SourceEndpointsConfig
            {
                ShellDescriptors = endpoint,
                SubmodelDescriptors = endpoint,
                Shells = endpoint,
                Submodels = endpoint,
                ConceptDescriptions = endpoint
            },
            Targets = new TargetEndpointsConfig
            {
                ShellDescriptors = endpoint,
                SubmodelDescriptors = endpoint,
                Shells = endpoint,
                Submodels = endpoint,
                ConceptDescriptions = endpoint
            }
        };
    }
}