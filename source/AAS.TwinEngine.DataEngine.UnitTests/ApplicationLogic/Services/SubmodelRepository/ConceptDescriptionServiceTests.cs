using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasEnvironment.Providers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository;

using AasCore.Aas3_0;

using Microsoft.Extensions.Logging;

using NSubstitute;

namespace AAS.TwinEngine.DataEngine.UnitTests.ApplicationLogic.Services.SubmodelRepository;

public class ConceptDescriptionServiceTests
{
    private readonly ITemplateProvider _templateProvider;
    private readonly ILogger<ConceptDescriptionService> _logger;
    private readonly ConceptDescriptionService _sut;

    public ConceptDescriptionServiceTests()
    {
        _templateProvider = Substitute.For<ITemplateProvider>();
        _logger = Substitute.For<ILogger<ConceptDescriptionService>>();
        _sut = new ConceptDescriptionService(_templateProvider, _logger);
    }

    [Fact]
    public async Task GetConceptDescriptionById_LogsAndReturnsExpectedResult()
    {
        const string cdIdentifier = "test-id";
        var cancellationToken = CancellationToken.None;
        var expectedConceptDescription = Substitute.For<IConceptDescription>();
        _templateProvider
            .GetConceptDescriptionByIdAsync(cdIdentifier, cancellationToken)
            .Returns(Task.FromResult<IConceptDescription?>(expectedConceptDescription));

        var result = await _sut.GetConceptDescriptionById(cdIdentifier, cancellationToken);

        Assert.Equal(expectedConceptDescription, result);
        _logger.Received(1).Log(
                                LogLevel.Information,
                                Arg.Any<EventId>(),
                                Arg.Is<object>(o => o.ToString()!.Contains($"Fetching concept description by ID: {cdIdentifier}")),
                                Arg.Any<Exception>(),
                                Arg.Any<Func<object, Exception?, string>>()
                               );

        await _templateProvider.Received(1).GetConceptDescriptionByIdAsync(cdIdentifier, cancellationToken);
    }
}
