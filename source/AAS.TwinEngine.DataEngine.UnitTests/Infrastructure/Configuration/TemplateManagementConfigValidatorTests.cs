using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config.Helpers;

namespace AAS.TwinEngine.DataEngine.UnitTests.Infrastructure.Configuration;

public class TemplateManagementConfigValidatorTests
{
    private readonly TemplateManagementConfigValidator _sut = new();

    private static TemplateManagementConfig CreateValidConfig() => new()
    {
        AasTemplateRepository = new ServiceInstance { BaseUrl = new Uri("http://localhost:8081"), ConcurrentOperationsLimit = 10 },
        SubmodelTemplateRepository = new ServiceInstance { BaseUrl = new Uri("http://localhost:8081"), ConcurrentOperationsLimit = 10 },
        ConceptDescriptionTemplateRepository = new ServiceInstance { BaseUrl = new Uri("http://localhost:8081"), ConcurrentOperationsLimit = 10 },
        AasTemplateRegistry = new ServiceInstance { BaseUrl = new Uri("http://localhost:8082"), ConcurrentOperationsLimit = 10 },
        SubmodelTemplateRegistry = new ServiceInstance { BaseUrl = new Uri("http://localhost:8083"), ConcurrentOperationsLimit = 10 },
    };

    [Fact]
    public void Validate_ValidConfig_Succeeds()
    {
        var config = CreateValidConfig();

        var result = _sut.Validate(null, config);

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Validate_AasTemplateRepository_ConcurrentOperationsLimitNotPositive_Fails(int limit)
    {
        var config = CreateValidConfig();
        config.AasTemplateRepository.ConcurrentOperationsLimit = limit;

        var result = _sut.Validate(null, config);

        Assert.True(result.Failed);
        Assert.Contains("AasTemplateRepository.ConcurrentOperationsLimit", result.FailureMessage);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_SubmodelTemplateRepository_ConcurrentOperationsLimitNotPositive_Fails(int limit)
    {
        var config = CreateValidConfig();
        config.SubmodelTemplateRepository.ConcurrentOperationsLimit = limit;

        var result = _sut.Validate(null, config);

        Assert.True(result.Failed);
        Assert.Contains("SubmodelTemplateRepository.ConcurrentOperationsLimit", result.FailureMessage);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_ConceptDescriptionTemplateRepository_ConcurrentOperationsLimitNotPositive_Fails(int limit)
    {
        var config = CreateValidConfig();
        config.ConceptDescriptionTemplateRepository.ConcurrentOperationsLimit = limit;

        var result = _sut.Validate(null, config);

        Assert.True(result.Failed);
        Assert.Contains("ConceptDescriptionTemplateRepository.ConcurrentOperationsLimit", result.FailureMessage);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_AasTemplateRegistry_ConcurrentOperationsLimitNotPositive_Fails(int limit)
    {
        var config = CreateValidConfig();
        config.AasTemplateRegistry.ConcurrentOperationsLimit = limit;

        var result = _sut.Validate(null, config);

        Assert.True(result.Failed);
        Assert.Contains("AasTemplateRegistry.ConcurrentOperationsLimit", result.FailureMessage);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_SubmodelTemplateRegistry_ConcurrentOperationsLimitNotPositive_Fails(int limit)
    {
        var config = CreateValidConfig();
        config.SubmodelTemplateRegistry.ConcurrentOperationsLimit = limit;

        var result = _sut.Validate(null, config);

        Assert.True(result.Failed);
        Assert.Contains("SubmodelTemplateRegistry.ConcurrentOperationsLimit", result.FailureMessage);
    }

    [Fact]
    public void Validate_AllEndpoints_MissingConcurrentOperationsLimit_ReportsAllErrors()
    {
        var config = CreateValidConfig();
        config.AasTemplateRepository.ConcurrentOperationsLimit = 0;
        config.SubmodelTemplateRepository.ConcurrentOperationsLimit = 0;
        config.ConceptDescriptionTemplateRepository.ConcurrentOperationsLimit = 0;
        config.AasTemplateRegistry.ConcurrentOperationsLimit = 0;
        config.SubmodelTemplateRegistry.ConcurrentOperationsLimit = 0;

        var result = _sut.Validate(null, config);

        Assert.True(result.Failed);
        Assert.Contains("AasTemplateRepository.ConcurrentOperationsLimit", result.FailureMessage);
        Assert.Contains("SubmodelTemplateRepository.ConcurrentOperationsLimit", result.FailureMessage);
        Assert.Contains("ConceptDescriptionTemplateRepository.ConcurrentOperationsLimit", result.FailureMessage);
        Assert.Contains("AasTemplateRegistry.ConcurrentOperationsLimit", result.FailureMessage);
        Assert.Contains("SubmodelTemplateRegistry.ConcurrentOperationsLimit", result.FailureMessage);
    }
}
