using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Base;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasEnvironment.Providers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository;

using AasCore.Aas3_0;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using File = AasCore.Aas3_0.File;

namespace AAS.TwinEngine.DataEngine.UnitTests.ApplicationLogic.Services.SubmodelRepository;

public class SubmodelTemplateServiceTests
{
    private readonly ITemplateProvider _templateProvider = Substitute.For<ITemplateProvider>();
    private readonly ISubmodelTemplateMappingProvider _mappingProvider = Substitute.For<ISubmodelTemplateMappingProvider>();
    private readonly SubmodelTemplateService _sut;
    private const string SubmodelId = "Nameplate";
    private const string TemplateId = "template-Nameplate";

    public SubmodelTemplateServiceTests() => _sut = new SubmodelTemplateService(_templateProvider, _mappingProvider);

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenTemplateProviderIsNull()
    {
        ITemplateProvider? templateProvider = null;

        var ex = Assert.Throws<ArgumentNullException>(() => new SubmodelTemplateService(templateProvider!, _mappingProvider));
        Assert.Equal("templateProvider", ex.ParamName);
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenTemplateMappingProviderIsNull()
    {
        ISubmodelTemplateMappingProvider? templateMappingProvider = null;

        var ex = Assert.Throws<ArgumentNullException>(() => new SubmodelTemplateService(_templateProvider, templateMappingProvider!));
        Assert.Equal("submodelTemplateMappingProvider", ex.ParamName);
    }

    [Fact]
    public async Task GetSubmodelTemplateAsync_ReturnsSubmodel_WhenValidInput()
    {
        var expectedSubmodel = Substitute.For<ISubmodel>();
        _mappingProvider.GetTemplateId(SubmodelId).Returns(TemplateId);
        _templateProvider.GetSubmodelTemplateAsync(TemplateId, Arg.Any<CancellationToken>())
            .Returns(expectedSubmodel);

        var result = await _sut.GetSubmodelTemplateAsync(SubmodelId, CancellationToken.None);

        Assert.Equal(expectedSubmodel, result);
    }

    [Fact]
    public async Task GetSubmodelTemplateAsync_ThrowsBadRequestException_WhenSubmodelIdIsNull()
    {
        string? submodelId = null;

        var exception = await Assert.ThrowsAsync<InternalDataProcessingException>(() => _sut.GetSubmodelTemplateAsync(submodelId!, CancellationToken.None));
        Assert.Equal("Internal Server Error.", exception.Message);
    }

    [Fact]
    public async Task GetSubmodelTemplateAsync_ReturnsNull_WhenElementNotFound()
    {
        const string IdShortPath = "InvalidElement";
        var expectedSubmodel = TestData.CreateSubmodel();
        _mappingProvider.GetTemplateId(SubmodelId).Returns(TemplateId);
        _templateProvider.GetSubmodelTemplateAsync(TemplateId, Arg.Any<CancellationToken>())
            .Returns(expectedSubmodel);

        var exception = await Assert.ThrowsAsync<InternalDataProcessingException>(() => _sut.GetSubmodelTemplateAsync(SubmodelId, IdShortPath, CancellationToken.None));
        Assert.Equal("Internal Server Error.", exception.Message);
    }

    [Fact]
    public async Task GetSubmodelTemplateAsync_ThrowsException_WhenSubmodelIdIsInvalid()
    {
        string? submodelId = null;

        var exception = await Assert.ThrowsAsync<InternalDataProcessingException>(() => _sut.GetSubmodelTemplateAsync(submodelId!, "idShort", CancellationToken.None));
        Assert.Equal("Internal Server Error.", exception.Message);
    }

    [Fact]
    public async Task GetSubmodelTemplateAsync_ReturnsElement_WhenSingleProperty()
    {
        const string IdShortPath = "ManufacturerName";
        var expectedSubmodel = TestData.CreateSubmodel();
        var expectedElement = TestData.CreateSubmodelWithoutExtraElements();
        _mappingProvider.GetTemplateId(SubmodelId).Returns(TemplateId);
        _templateProvider.GetSubmodelTemplateAsync(TemplateId, Arg.Any<CancellationToken>())
            .Returns(expectedSubmodel);

        var result = await _sut.GetSubmodelTemplateAsync(SubmodelId, IdShortPath, CancellationToken.None);
        Assert.Equal(GetSemanticId(expectedElement), GetSemanticId(result));
        Assert.Equal(expectedElement.SubmodelElements!.Count, result.SubmodelElements!.Count);
        Assert.Single(expectedElement.SubmodelElements);
    }

    [Fact]
    public async Task GetSubmodelTemplateAsync_ReturnsCustomSubmodel_WhenNestedProperty()
    {
        const string IdShortPath = "ContactInformation.ContactName";
        var expectedSubmodel = TestData.CreateSubmodel();
        var expectedElement = TestData.CreateSubmodelWithoutExtraElementsNested();
        _mappingProvider.GetTemplateId(SubmodelId).Returns(TemplateId);
        _templateProvider.GetSubmodelTemplateAsync(TemplateId, Arg.Any<CancellationToken>())
            .Returns(expectedSubmodel);

        var result = await _sut.GetSubmodelTemplateAsync(SubmodelId, IdShortPath, CancellationToken.None);

        Assert.Equal(GetSemanticId(expectedElement), GetSemanticId(result));
        Assert.Equal(expectedElement.SubmodelElements!.Count, result.SubmodelElements!.Count);
        Assert.Single(expectedElement.SubmodelElements);
    }

    private static string GetSemanticId(IHasSemantics hasSemantics) => hasSemantics.SemanticId?.Keys?.FirstOrDefault()?.Value ?? string.Empty;

    [Fact]
    public async Task GetSubmodelTemplateAsync_ReturnsNotFoundException_WhenNotFindTheSubmodelElement()
    {
        const string IdShortPath = "ContactInformation0.InvalidIdShort";
        var expectedSubmodel = TestData.CreateSubmodel();
        _mappingProvider.GetTemplateId(SubmodelId).Returns(TemplateId);
        _templateProvider.GetSubmodelTemplateAsync(TemplateId, Arg.Any<CancellationToken>())
            .Returns(expectedSubmodel);

        var exception = await Assert.ThrowsAsync<InternalDataProcessingException>(() => _sut.GetSubmodelTemplateAsync(SubmodelId, IdShortPath, CancellationToken.None));
    }

    [Fact]
    public async Task GetSubmodelElementTemplateAsync_ThrowsBadRequestException_WhenSubmodelIdIsEmpty()
    {
        const string IdShortPath = "ContactInformation0";

        var exception = await Assert.ThrowsAsync<InternalDataProcessingException>(() => _sut.GetSubmodelTemplateAsync("", IdShortPath, CancellationToken.None));
        Assert.IsType<InternalDataProcessingException>(exception);
    }

    [Fact]
    public async Task GetSubmodelTemplateAsync_ReturnsSubmodel_WhenPathContainsListIndex()
    {
        var expectedSubmodel = TestData.CreateSubmodelWithModel3DList();
        var path = "Model3D[0].ModelDataFile";
        _mappingProvider.GetTemplateId(SubmodelId).Returns(TemplateId);
        _templateProvider.GetSubmodelTemplateAsync(TemplateId, Arg.Any<CancellationToken>())
                         .Returns(expectedSubmodel);

        var result = await _sut.GetSubmodelTemplateAsync(SubmodelId, path, CancellationToken.None);

        Assert.Equal(GetSemanticId(expectedSubmodel), GetSemanticId(result));

        var list = result.SubmodelElements?.FirstOrDefault() as SubmodelElementList;
        Assert.NotNull(list);
        Assert.Single(list.Value!);
        var collection = list.Value![0] as SubmodelElementCollection;
        Assert.Single(collection!.Value!);
        var file = collection!.Value!.FirstOrDefault() as File;
        Assert.NotNull(file);
        Assert.Equal("ModelDataFile", file.IdShort);
        Assert.Equal("https://localhost/ModelDataFile.glb", file.Value);
    }

    [Fact]
    public async Task GetSubmodelTemplateAsync_WithListIndexPath_ReturnsSubmodelWithCorrectIndexedElement()
    {
        var expectedSubmodel = TestData.CreateSubmodelWithModel3DList();
        const string Path = "Model3D[0]";
        _mappingProvider.GetTemplateId(SubmodelId).Returns(TemplateId);
        _templateProvider.GetSubmodelTemplateAsync(TemplateId, Arg.Any<CancellationToken>())
                         .Returns(expectedSubmodel);

        var result = await _sut.GetSubmodelTemplateAsync(SubmodelId, Path, CancellationToken.None);

        Assert.Equal(GetSemanticId(expectedSubmodel), GetSemanticId(result));

        var list = result.SubmodelElements?.FirstOrDefault() as SubmodelElementList;
        Assert.NotNull(list);
        Assert.Single(list.Value!);
        var collection = list!.Value?[0] as SubmodelElementCollection;
        Assert.Equal(2, collection?.Value?.Count);
        var file = collection!.Value!.FirstOrDefault() as File;
        Assert.Equal("ModelFile", file?.IdShort);
        Assert.Equal("https://localhost/ModelFile.glb", file?.Value);
    }

    [Fact]
    public async Task GetSubmodelTemplateAsync_ThrowsNotFoundException_WhenListIndexIsOutOfRange()
    {
        var submodel = TestData.CreateSubmodelWithModel3DList();
        const string Path = "Model3D[5].ModelFile1";
        _mappingProvider.GetTemplateId(SubmodelId).Returns(TemplateId);
        _templateProvider.GetSubmodelTemplateAsync(TemplateId, Arg.Any<CancellationToken>())
                         .Returns(submodel);

        await Assert.ThrowsAsync<InternalDataProcessingException>(() => _sut.GetSubmodelTemplateAsync(SubmodelId, Path, CancellationToken.None));
    }

    [Fact]
    public async Task GetSubmodelTemplateAsync_ThrowsNotFoundException_WhenListElementNotFound()
    {
        var submodel = TestData.CreateSubmodelWithModel3DList();
        const string Path = "NonExistentList[0].ModelFile1";
        _mappingProvider.GetTemplateId(SubmodelId).Returns(TemplateId);
        _templateProvider.GetSubmodelTemplateAsync(TemplateId, Arg.Any<CancellationToken>())
                         .Returns(submodel);

        await Assert.ThrowsAsync<InternalDataProcessingException>(() => _sut.GetSubmodelTemplateAsync(SubmodelId, Path, CancellationToken.None));
    }

    [Fact]
    public async Task GetSubmodelTemplateAsync_ThrowsNotFoundException_WhenPathSegmentHasListElement_AndIsInvalid()
    {
        var submodel = TestData.CreateSubmodelWithModel3DList();
        const string Path = "Model3D[0].NonExistentFile";
        _mappingProvider.GetTemplateId(SubmodelId).Returns(TemplateId);
        _templateProvider.GetSubmodelTemplateAsync(TemplateId, Arg.Any<CancellationToken>())
                         .Returns(submodel);

        await Assert.ThrowsAsync<InternalDataProcessingException>(() => _sut.GetSubmodelTemplateAsync(SubmodelId, Path, CancellationToken.None));
    }

    [Fact]
    public async Task GetSubmodelTemplateAsync_ThrowsSubmodelNotFoundException_WhenResourceNotFound()
    {
        _mappingProvider.GetTemplateId(SubmodelId).Returns(TemplateId);
        _templateProvider.GetSubmodelTemplateAsync(TemplateId, Arg.Any<CancellationToken>())
                         .ThrowsAsync(new ResourceNotFoundException());

        await Assert.ThrowsAsync<SubmodelNotFoundException>(
            () => _sut.GetSubmodelTemplateAsync(SubmodelId, CancellationToken.None));
    }

    [Fact]
    public async Task GetSubmodelTemplateAsync_ThrowsInternalDataProcessingException_WhenResponseParsingFails()
    {
        _mappingProvider.GetTemplateId(SubmodelId).Returns(TemplateId);
        _templateProvider.GetSubmodelTemplateAsync(TemplateId, Arg.Any<CancellationToken>())
                         .ThrowsAsync(new ResponseParsingException());

        var exception = await Assert.ThrowsAsync<InternalDataProcessingException>(
            () => _sut.GetSubmodelTemplateAsync(SubmodelId, CancellationToken.None));

        Assert.Equal("Internal Server Error.", exception.Message);
    }

    [Fact]
    public async Task GetSubmodelTemplateAsync_ThrowsTemplateRequestFailedException_WhenRequestTimesOut()
    {
        _mappingProvider.GetTemplateId(SubmodelId).Returns(TemplateId);
        _templateProvider.GetSubmodelTemplateAsync(TemplateId, Arg.Any<CancellationToken>())
                         .ThrowsAsync(new RequestTimeoutException());

        await Assert.ThrowsAsync<TemplateRequestFailedException>(
            () => _sut.GetSubmodelTemplateAsync(SubmodelId, CancellationToken.None));
    }

    [Fact]
    public async Task GetSubmodelTemplateAsync_ThrowsRepositoryNotAvailableException_WhenServiceUnavailable()
    {
        _mappingProvider.GetTemplateId(SubmodelId).Returns(TemplateId);
        _templateProvider.GetSubmodelTemplateAsync(TemplateId, Arg.Any<CancellationToken>())
                         .ThrowsAsync(new ServiceUnavailableException("http://fake-url"));

        await Assert.ThrowsAsync<RepositoryNotAvailableException>(
            () => _sut.GetSubmodelTemplateAsync(SubmodelId, CancellationToken.None));
    }

    [Fact]
    public async Task GetSubmodelTemplateAsync_WithIdShortPath_ThrowsSubmodelElementNotFoundException_WhenResourceNotFound()
    {
        _mappingProvider.GetTemplateId(SubmodelId).Returns(TemplateId);
        _templateProvider.GetSubmodelTemplateAsync(TemplateId, Arg.Any<CancellationToken>())
                         .ThrowsAsync(new ResourceNotFoundException());

        await Assert.ThrowsAsync<SubmodelElementNotFoundException>(
            () => _sut.GetSubmodelTemplateAsync(SubmodelId, "SomePath", CancellationToken.None));
    }

}
