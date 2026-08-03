using AAS.TwinEngine.DataEngine.Api.AasRegistry.Handler;
using AAS.TwinEngine.DataEngine.Api.AasRegistry.Requests;
using AAS.TwinEngine.DataEngine.Api.AasRegistry.Responses;
using AAS.TwinEngine.DataEngine.Api.SubmodelRegistry.Responses;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasRegistry;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasRepository;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRegistry;
using AAS.TwinEngine.DataEngine.DomainModel.AasRegistry;
using AAS.TwinEngine.DataEngine.DomainModel.AasRepository;
using AAS.TwinEngine.DataEngine.DomainModel.Shared;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRegistry;
using AAS.TwinEngine.DataEngine.UnitTests.Api.Shared.MappingProfiles;

using AasCore.Aas3_1;

using Microsoft.Extensions.Logging;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace AAS.TwinEngine.DataEngine.UnitTests.Api.AasRegistry.Handler;

public class ShellDescriptorHandlerTests
{
    private readonly IShellDescriptorService _shellDescriptorService = Substitute.For<IShellDescriptorService>();
    private readonly IAasRepositoryService _aasRepositoryService = Substitute.For<IAasRepositoryService>();
    private readonly ISubmodelDescriptorService _submodelDescriptorService = Substitute.For<ISubmodelDescriptorService>();
    private readonly ILogger<ShellDescriptorHandler> _logger = Substitute.For<ILogger<ShellDescriptorHandler>>();
    private readonly ShellDescriptorHandler _sut;

    public ShellDescriptorHandlerTests() => _sut = new ShellDescriptorHandler(_logger, _shellDescriptorService, _aasRepositoryService, _submodelDescriptorService);

    [Fact]
    public async Task GetAllShellDescriptors_ReturnsAllShellDescriptors_WhenExists()
    {
        var expectedShellDescriptors = TestDataMapperProfiles.CreateShellDescriptors();
        var request = new GetShellDescriptorsRequest(null, null);
        _shellDescriptorService.GetAllShellDescriptorsAsync(null, null, Arg.Any<CancellationToken>()).Returns(expectedShellDescriptors);

        var result = await _sut.GetAllShellDescriptors(request, CancellationToken.None);

        Assert.IsType<ShellDescriptorsDto>(result);
    }

    [Fact]
    public async Task GetAllShellDescriptors_WithLimitAndCursor_PassesCorrectValuesToService()
    {
        var expectedShellDescriptors = TestDataMapperProfiles.CreateShellDescriptors();
        var request = new GetShellDescriptorsRequest(50, "aGVsbG8=");

        _shellDescriptorService.GetAllShellDescriptorsAsync(50, "aGVsbG8=", Arg.Any<CancellationToken>())
                               .Returns(expectedShellDescriptors);

        var result = await _sut.GetAllShellDescriptors(request, CancellationToken.None);

        Assert.NotNull(result);
        Assert.IsType<ShellDescriptorsDto>(result);
        await _shellDescriptorService.Received(1).GetAllShellDescriptorsAsync(50, "aGVsbG8=", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAllShellDescriptors_ShellDescriptorsIsNull_ThrowsShellDescriptorNotFoundException()
    {
        _shellDescriptorService.GetAllShellDescriptorsAsync(null, null, Arg.Any<CancellationToken>())!.Returns((ShellDescriptors)null!);
        var request = new GetShellDescriptorsRequest(null, null);

        await Assert.ThrowsAsync<ShellDescriptorNotFoundException>(() => _sut.GetAllShellDescriptors(request, CancellationToken.None));
    }

    [Fact]
    public async Task GetShellDescriptorByIdAsync_ReturnShellDescriptor_WhenExists()
    {
        const string Id = "AasId";
        var encodedId = Id.EncodeBase64Url();
        var request = new GetShellDescriptorRequest(encodedId);
        var shellDescriptor = Substitute.For<ShellDescriptor>();
        _shellDescriptorService.GetShellDescriptorByIdAsync(Id, Arg.Any<CancellationToken>()).Returns(shellDescriptor);

        var result = await _sut.GetShellDescriptorById(request, CancellationToken.None);

        Assert.IsType<ShellDescriptorDto>(result);
        await _shellDescriptorService.Received().GetShellDescriptorByIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetShellDescriptorByIdAsync_ShellDescriptorIsNull_ThrowsShellDescriptorNotFoundException()
    {
        const string Id = "AasId";
        var encodedId = Id.EncodeBase64Url();
        var request = new GetShellDescriptorRequest(encodedId);
        _shellDescriptorService.GetShellDescriptorByIdAsync(Id, Arg.Any<CancellationToken>())!.Returns((ShellDescriptor)null!);

        await Assert.ThrowsAsync<ShellDescriptorNotFoundException>(() => _sut.GetShellDescriptorById(request, CancellationToken.None));
    }

    [Fact]
    public async Task HandleSubmodelElement_InvalidBase64SubmodelId_ThrowsInvalidUserInputException()
    {
        const string InvalidEncodedId = "!!invalid_base64@@";
        var request = new GetShellDescriptorRequest(InvalidEncodedId);

        await Assert.ThrowsAsync<InvalidUserInputException>(() => _sut.GetShellDescriptorById(request, CancellationToken.None));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task GetShellTemplateAsync_InvalidAasIdentifier_ThrowsInvalidUserInputException(string aasIdentifier)
    {
        var encodedId = aasIdentifier.EncodeBase64Url();
        var request = new GetShellDescriptorRequest(encodedId);

        var exception = await Assert.ThrowsAsync<InvalidUserInputException>(() => _sut.GetShellDescriptorById(request, CancellationToken.None));
        Assert.Equal("Invalid User Input.", exception.Message);
    }

    [Fact]
    public async Task GetSubmodelDescriptorByAasId_ReturnsDescriptor_WhenSubmodelBelongsToAas()
    {
        const string AasId = "AasId";
        const string SubmodelId = "SubmodelId";
        var request = new GetSubmodelDescriptorByAasRequest(AasId.EncodeBase64Url(), SubmodelId.EncodeBase64Url());
        _aasRepositoryService.GetSubmodelRefByIdAsync(AasId, null, null, Arg.Any<CancellationToken>())
            .Returns(CreateSubmodelRefWithId(SubmodelId));
        _submodelDescriptorService.GetSubmodelDescriptorByIdAsync(SubmodelId, Arg.Any<CancellationToken>())
            .Returns(new SubmodelDescriptor { Id = SubmodelId });

        var result = await _sut.GetSubmodelDescriptorByAasId(request, CancellationToken.None);

        Assert.IsType<SubmodelDescriptorDto>(result);
        await _submodelDescriptorService.Received(1)
            .GetSubmodelDescriptorByIdAsync(SubmodelId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSubmodelDescriptorByAasId_SubmodelNotInAas_ThrowsSubmodelDescriptorNotFoundException()
    {
        const string AasId = "AasId";
        const string SubmodelId = "SubmodelId";
        var request = new GetSubmodelDescriptorByAasRequest(AasId.EncodeBase64Url(), SubmodelId.EncodeBase64Url());
        _aasRepositoryService.GetSubmodelRefByIdAsync(AasId, null, null, Arg.Any<CancellationToken>())
            .Returns(CreateSubmodelRefWithId("OtherSubmodelId"));

        await Assert.ThrowsAsync<SubmodelDescriptorNotFoundException>(
            () => _sut.GetSubmodelDescriptorByAasId(request, CancellationToken.None));
    }

    [Fact]
    public async Task GetSubmodelDescriptorByAasId_AasNotFound_PropagatesException()
    {
        const string AasId = "AasId";
        const string SubmodelId = "SubmodelId";
        var request = new GetSubmodelDescriptorByAasRequest(AasId.EncodeBase64Url(), SubmodelId.EncodeBase64Url());
        _aasRepositoryService.GetSubmodelRefByIdAsync(AasId, null, null, Arg.Any<CancellationToken>())
            .ThrowsAsync(new TemplateNotFoundException());

        await Assert.ThrowsAsync<TemplateNotFoundException>(
            () => _sut.GetSubmodelDescriptorByAasId(request, CancellationToken.None));
    }

    [Fact]
    public async Task GetAllSubmodelDescriptorsByAasId_ReturnsDescriptors_ForAllSubmodelsInAas()
    {
        const string AasId = "AasId";
        const string SubmodelId1 = "SubmodelId1";
        const string SubmodelId2 = "SubmodelId2";
        var request = new GetSubmodelDescriptorsByAasRequest(AasId.EncodeBase64Url(), null, null);
        _aasRepositoryService.GetSubmodelRefByIdAsync(AasId, null, null, Arg.Any<CancellationToken>())
            .Returns(CreateSubmodelRefWithIds(SubmodelId1, SubmodelId2));
        _submodelDescriptorService.GetSubmodelDescriptorByIdAsync(SubmodelId1, Arg.Any<CancellationToken>())
            .Returns(new SubmodelDescriptor { Id = SubmodelId1 });
        _submodelDescriptorService.GetSubmodelDescriptorByIdAsync(SubmodelId2, Arg.Any<CancellationToken>())
            .Returns(new SubmodelDescriptor { Id = SubmodelId2 });

        var result = await _sut.GetAllSubmodelDescriptorsByAasId(request, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(2, result.Result?.Count);
    }

    [Fact]
    public async Task GetAllSubmodelDescriptorsByAasId_AasNotFound_PropagatesException()
    {
        const string AasId = "AasId";
        var request = new GetSubmodelDescriptorsByAasRequest(AasId.EncodeBase64Url(), null, null);
        _aasRepositoryService.GetSubmodelRefByIdAsync(AasId, null, null, Arg.Any<CancellationToken>())
            .ThrowsAsync(new TemplateNotFoundException());

        await Assert.ThrowsAsync<TemplateNotFoundException>(
            () => _sut.GetAllSubmodelDescriptorsByAasId(request, CancellationToken.None));
    }

    private static SubmodelRef CreateSubmodelRefWithId(string submodelId)
        => new()
        {
            PagingMetaData = new PagingMetaData(),
            Result = [new Reference(ReferenceTypes.ModelReference, [new Key(KeyTypes.Submodel, submodelId)], null)]
        };

    private static SubmodelRef CreateSubmodelRefWithIds(params string[] submodelIds)
        => new()
        {
            PagingMetaData = new PagingMetaData(),
            Result = submodelIds.Select(id => (IReference)new Reference(ReferenceTypes.ModelReference, [new Key(KeyTypes.Submodel, id)], null)).ToList()
        };
}
