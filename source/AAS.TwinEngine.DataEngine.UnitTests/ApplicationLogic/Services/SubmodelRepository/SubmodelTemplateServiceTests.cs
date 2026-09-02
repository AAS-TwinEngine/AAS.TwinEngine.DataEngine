using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Base;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasEnvironment.Providers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository;
using AAS.TwinEngine.DataEngine.DomainModel.Shared;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

using AasCore.Aas3_1;

using Microsoft.Extensions.Logging;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using File = AasCore.Aas3_1.File;

namespace AAS.TwinEngine.DataEngine.UnitTests.ApplicationLogic.Services.SubmodelRepository;

public class SubmodelTemplateServiceTests
{
    private readonly ITemplateProvider _templateProvider = Substitute.For<ITemplateProvider>();
    private readonly ISubmodelTemplateMappingProvider _mappingProvider = Substitute.For<ISubmodelTemplateMappingProvider>();
    private readonly ILogger<SubmodelTemplateService> _logger = Substitute.For<ILogger<SubmodelTemplateService>>();

    private readonly SubmodelTemplateService _sut;
    private const string SubmodelId = "Nameplate";
    private const string TemplateId = "template-Nameplate";

    public SubmodelTemplateServiceTests() => _sut = new SubmodelTemplateService(_templateProvider, _mappingProvider, _logger);

    #region Constructor

    [Fact]
    public void Constructor_ThrowsInvalidDependencyException_WhenTemplateProviderIsNull()
    {
        Assert.Throws<InvalidDependencyException>(() => new SubmodelTemplateService(null!, _mappingProvider, _logger));
    }

    [Fact]
    public void Constructor_ThrowsInvalidDependencyException_WhenTemplateMappingProviderIsNull()
    {
        Assert.Throws<InvalidDependencyException>(() => new SubmodelTemplateService(_templateProvider, null!, _logger));
    }

    #endregion

    #region GetSubmodelTemplateAsync (single param)

    [Fact]
    public async Task GetSubmodelTemplateAsync_ReturnsSubmodel_WhenValidInput()
    {
        var expectedSubmodel = Substitute.For<ISubmodel>();
        _mappingProvider.GetTemplateId(SubmodelId).Returns(TemplateId);
        _templateProvider.GetFilteredSubmodelTemplateAsync(TemplateId, null, Arg.Any<CancellationToken>())
            .Returns(expectedSubmodel);

        var result = await _sut.GetSubmodelTemplateAsync(SubmodelId, CancellationToken.None);

        Assert.Equal(expectedSubmodel, result);
    }

    [Fact]
    public async Task GetSubmodelTemplateAsync_ThrowsInternalDataProcessingException_WhenSubmodelIdIsNull()
    {
        await Assert.ThrowsAsync<InternalDataProcessingException>(() =>
            _sut.GetSubmodelTemplateAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetSubmodelTemplateAsync_ThrowsInternalDataProcessingException_WhenSubmodelIdIsEmpty()
    {
        await Assert.ThrowsAsync<InternalDataProcessingException>(() =>
            _sut.GetSubmodelTemplateAsync("", CancellationToken.None));
    }

    [Fact]
    public async Task GetSubmodelTemplateAsync_ThrowsInternalDataProcessingException_WhenSubmodelIdIsWhitespace()
    {
        await Assert.ThrowsAsync<InternalDataProcessingException>(() =>
            _sut.GetSubmodelTemplateAsync("   ", CancellationToken.None));
    }

    [Fact]
    public async Task GetSubmodelTemplateAsync_ThrowsSubmodelNotFoundException_WhenResourceNotFound()
    {
        _mappingProvider.GetTemplateId(SubmodelId).Returns(TemplateId);
        _templateProvider.GetFilteredSubmodelTemplateAsync(TemplateId, null, Arg.Any<CancellationToken>())
            .ThrowsAsync(new ResourceNotFoundException());

        await Assert.ThrowsAsync<SubmodelNotFoundException>(() =>
            _sut.GetSubmodelTemplateAsync(SubmodelId, CancellationToken.None));
    }

    [Fact]
    public async Task GetSubmodelTemplateAsync_ThrowsInternalDataProcessingException_WhenResponseParsingFails()
    {
        _mappingProvider.GetTemplateId(SubmodelId).Returns(TemplateId);
        _templateProvider.GetFilteredSubmodelTemplateAsync(TemplateId, null, Arg.Any<CancellationToken>())
            .ThrowsAsync(new ResponseParsingException());

        var exception = await Assert.ThrowsAsync<InternalDataProcessingException>(() =>
            _sut.GetSubmodelTemplateAsync(SubmodelId, CancellationToken.None));

        Assert.Equal("Internal Server Error.", exception.Message);
    }

    [Fact]
    public async Task GetSubmodelTemplateAsync_ThrowsTemplateRequestFailedException_WhenRequestTimesOut()
    {
        _mappingProvider.GetTemplateId(SubmodelId).Returns(TemplateId);
        _templateProvider.GetFilteredSubmodelTemplateAsync(TemplateId, null, Arg.Any<CancellationToken>())
            .ThrowsAsync(new RequestTimeoutException());

        await Assert.ThrowsAsync<TemplateRequestFailedException>(() =>
            _sut.GetSubmodelTemplateAsync(SubmodelId, CancellationToken.None));
    }

    [Fact]
    public async Task GetSubmodelTemplateAsync_ThrowsRepositoryNotAvailableException_WhenServiceUnavailable()
    {
        _mappingProvider.GetTemplateId(SubmodelId).Returns(TemplateId);
        _templateProvider.GetFilteredSubmodelTemplateAsync(TemplateId, null, Arg.Any<CancellationToken>())
            .ThrowsAsync(new ServiceUnavailableException("http://fake-url"));

        await Assert.ThrowsAsync<RepositoryNotAvailableException>(() =>
            _sut.GetSubmodelTemplateAsync(SubmodelId, CancellationToken.None));
    }

    #endregion

    #region GetSubmodelTemplateAsync (with idShortPath)

    [Fact]
    public async Task GetSubmodelTemplateAsync_ReturnsElement_WhenSingleProperty()
    {
        const string idShortPath = "ManufacturerName";
        var expectedSubmodel = TestData.CreateSubmodel();
        var expectedElement = TestData.CreateSubmodelWithoutExtraElements();
        _mappingProvider.GetTemplateId(SubmodelId).Returns(TemplateId);
        _templateProvider.GetFilteredSubmodelTemplateAsync(TemplateId, null, Arg.Any<CancellationToken>())
            .Returns(expectedSubmodel);

        var result = await _sut.GetSubmodelTemplateAsync(SubmodelId, idShortPath, null, CancellationToken.None);

        Assert.Equal(GetSemanticId(expectedElement), GetSemanticId(result));
        Assert.Equal(expectedElement.SubmodelElements!.Count, result.SubmodelElements!.Count);
        Assert.Single(expectedElement.SubmodelElements);
    }

    [Fact]
    public async Task GetSubmodelTemplateAsync_ReturnsCustomSubmodel_WhenNestedProperty()
    {
        const string idShortPath = "ContactInformation.ContactName";
        var expectedSubmodel = TestData.CreateSubmodel();
        var expectedElement = TestData.CreateSubmodelWithoutExtraElementsNested();
        _mappingProvider.GetTemplateId(SubmodelId).Returns(TemplateId);
        _templateProvider.GetFilteredSubmodelTemplateAsync(TemplateId, null, Arg.Any<CancellationToken>())
            .Returns(expectedSubmodel);

        var result = await _sut.GetSubmodelTemplateAsync(SubmodelId, idShortPath, null, CancellationToken.None);

        Assert.Equal(GetSemanticId(expectedElement), GetSemanticId(result));
        Assert.Equal(expectedElement.SubmodelElements!.Count, result.SubmodelElements!.Count);
        Assert.Single(expectedElement.SubmodelElements);
    }

    [Fact]
    public async Task GetSubmodelTemplateAsync_ThrowsSubmodelElementNotFoundException_WhenElementNotFound()
    {
        const string idShortPath = "InvalidElement";
        _mappingProvider.GetTemplateId(SubmodelId).Returns(TemplateId);
        _templateProvider.GetFilteredSubmodelTemplateAsync(TemplateId, null, Arg.Any<CancellationToken>())
            .Returns(TestData.CreateSubmodel());

        await Assert.ThrowsAsync<SubmodelElementNotFoundException>(() =>
            _sut.GetSubmodelTemplateAsync(SubmodelId, idShortPath, null, CancellationToken.None));
    }

    [Fact]
    public async Task GetSubmodelTemplateAsync_ThrowsSubmodelElementNotFoundException_WhenNestedPathInvalid()
    {
        const string idShortPath = "ContactInformation0.InvalidIdShort";
        _mappingProvider.GetTemplateId(SubmodelId).Returns(TemplateId);
        _templateProvider.GetFilteredSubmodelTemplateAsync(TemplateId, null, Arg.Any<CancellationToken>())
            .Returns(TestData.CreateSubmodel());

        await Assert.ThrowsAsync<SubmodelElementNotFoundException>(() =>
            _sut.GetSubmodelTemplateAsync(SubmodelId, idShortPath, null, CancellationToken.None));
    }

    [Fact]
    public async Task GetSubmodelTemplateAsync_WithIdShortPath_ThrowsInternalDataProcessingException_WhenSubmodelIdIsEmpty()
    {
        await Assert.ThrowsAsync<InternalDataProcessingException>(() =>
            _sut.GetSubmodelTemplateAsync("", "ContactInformation0", null, CancellationToken.None));
    }

    [Fact]
    public async Task GetSubmodelTemplateAsync_WithIdShortPath_ThrowsInternalDataProcessingException_WhenSubmodelIdIsNull()
    {
        await Assert.ThrowsAsync<InternalDataProcessingException>(() =>
            _sut.GetSubmodelTemplateAsync(null!, "idShort", null, CancellationToken.None));
    }

    [Fact]
    public async Task GetSubmodelTemplateAsync_ThrowsInvalidDependencyException_WhenIdShortPathIsEmpty()
    {
        await Assert.ThrowsAsync<InvalidDependencyException>(() =>
            _sut.GetSubmodelTemplateAsync(SubmodelId, "", null, CancellationToken.None));
    }

    [Fact]
    public async Task GetSubmodelTemplateAsync_ThrowsInvalidDependencyException_WhenIdShortPathIsWhitespace()
    {
        await Assert.ThrowsAsync<InvalidDependencyException>(() =>
            _sut.GetSubmodelTemplateAsync(SubmodelId, "   ", null, CancellationToken.None));
    }

    [Fact]
    public async Task GetSubmodelTemplateAsync_WithIdShortPath_ThrowsSubmodelElementNotFoundException_WhenResourceNotFound()
    {
        _mappingProvider.GetTemplateId(SubmodelId).Returns(TemplateId);
        _templateProvider.GetFilteredSubmodelTemplateAsync(TemplateId, null, Arg.Any<CancellationToken>())
            .ThrowsAsync(new ResourceNotFoundException());

        await Assert.ThrowsAsync<SubmodelElementNotFoundException>(() =>
            _sut.GetSubmodelTemplateAsync(SubmodelId, "SomePath", null, CancellationToken.None));
    }

    [Fact]
    public async Task GetSubmodelTemplateAsync_WithIdShortPath_ThrowsInternalDataProcessingException_WhenResponseParsingFails()
    {
        _mappingProvider.GetTemplateId(SubmodelId).Returns(TemplateId);
        _templateProvider.GetFilteredSubmodelTemplateAsync(TemplateId, Arg.Any<SubmodelQueryOptions?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ResponseParsingException());

        await Assert.ThrowsAsync<InternalDataProcessingException>(() =>
            _sut.GetSubmodelTemplateAsync(SubmodelId, "SomePath", null, CancellationToken.None));
    }

    [Fact]
    public async Task GetSubmodelTemplateAsync_WithIdShortPath_ThrowsTemplateRequestFailedException_WhenRequestTimesOut()
    {
        _mappingProvider.GetTemplateId(SubmodelId).Returns(TemplateId);
        _templateProvider.GetFilteredSubmodelTemplateAsync(TemplateId, Arg.Any<SubmodelQueryOptions?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new RequestTimeoutException());

        await Assert.ThrowsAsync<TemplateRequestFailedException>(() =>
            _sut.GetSubmodelTemplateAsync(SubmodelId, "SomePath", null, CancellationToken.None));
    }

    [Fact]
    public async Task GetSubmodelTemplateAsync_WithIdShortPath_ThrowsRepositoryNotAvailableException_WhenServiceUnavailable()
    {
        _mappingProvider.GetTemplateId(SubmodelId).Returns(TemplateId);
        _templateProvider.GetFilteredSubmodelTemplateAsync(TemplateId, Arg.Any<SubmodelQueryOptions?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ServiceUnavailableException("down"));

        await Assert.ThrowsAsync<RepositoryNotAvailableException>(() =>
            _sut.GetSubmodelTemplateAsync(SubmodelId, "SomePath", null, CancellationToken.None));
    }

    [Fact]
    public async Task GetSubmodelTemplateAsync_WithIdShortPath_PassesQueryOptionsToProvider()
    {
        const string idShortPath = "ManufacturerName";
        var queryOptions = new SubmodelQueryOptions("deep", "withBlobValue");
        _mappingProvider.GetTemplateId(SubmodelId).Returns(TemplateId);
        _templateProvider.GetFilteredSubmodelTemplateAsync(TemplateId, queryOptions, Arg.Any<CancellationToken>())
            .Returns(TestData.CreateSubmodel());

        await _sut.GetSubmodelTemplateAsync(SubmodelId, idShortPath, queryOptions, CancellationToken.None);

        await _templateProvider.Received(1).GetFilteredSubmodelTemplateAsync(TemplateId, queryOptions, Arg.Any<CancellationToken>());
    }

    #endregion

    #region GetSubmodelTemplateAsync (list index paths)

    [Fact]
    public async Task GetSubmodelTemplateAsync_ReturnsSubmodel_WhenPathContainsListIndex()
    {
        var expectedSubmodel = TestData.CreateSubmodelWithModel3DList();
        const string path = "Model3D[0].ModelDataFile";
        _mappingProvider.GetTemplateId(SubmodelId).Returns(TemplateId);
        _templateProvider.GetFilteredSubmodelTemplateAsync(TemplateId, null, Arg.Any<CancellationToken>())
            .Returns(expectedSubmodel);

        var result = await _sut.GetSubmodelTemplateAsync(SubmodelId, path, null, CancellationToken.None);

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
        const string path = "Model3D[0]";
        _mappingProvider.GetTemplateId(SubmodelId).Returns(TemplateId);
        _templateProvider.GetFilteredSubmodelTemplateAsync(TemplateId, null, Arg.Any<CancellationToken>())
            .Returns(expectedSubmodel);

        var result = await _sut.GetSubmodelTemplateAsync(SubmodelId, path, null, CancellationToken.None);

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
    public async Task GetSubmodelTemplateAsync_Supports_UrlEncoded_ListIndex()
    {
        var submodel = TestData.CreateSubmodelWithModel3DList();
        const string path = "Model3D%5B0%5D.ModelDataFile";

        _mappingProvider.GetTemplateId(SubmodelId).Returns(TemplateId);
        _templateProvider.GetFilteredSubmodelTemplateAsync(TemplateId, null, Arg.Any<CancellationToken>())
            .Returns(submodel);

        var result = await _sut.GetSubmodelTemplateAsync(SubmodelId, path, null, CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetSubmodelTemplateAsync_Throws_When_ListIndex_IsNegative()
    {
        var submodel = TestData.CreateSubmodelWithModel3DList();
        const string path = "Model3D[-1]";

        _mappingProvider.GetTemplateId(SubmodelId).Returns(TemplateId);
        _templateProvider.GetFilteredSubmodelTemplateAsync(TemplateId, null, Arg.Any<CancellationToken>())
            .Returns(submodel);

        await Assert.ThrowsAsync<SubmodelElementNotFoundException>(() =>
            _sut.GetSubmodelTemplateAsync(SubmodelId, path, null, CancellationToken.None));
    }

    [Fact]
    public async Task GetSubmodelTemplateAsync_ReturnsSubmodel_WhenTypeValueListElementIsSubmodelCollection_AndListIndexExceedsAvailableElements()
    {
        var expectedSubmodel = TestData.CreateSubmodelWithModel3DList();
        var submodel = TestData.CreateSubmodelWithModel3DList();
        const string path = "Model3D[5].ModelDataFile";
        _mappingProvider.GetTemplateId(SubmodelId).Returns(TemplateId);
        _templateProvider.GetFilteredSubmodelTemplateAsync(TemplateId, null, Arg.Any<CancellationToken>())
            .Returns(submodel);

        var result = await _sut.GetSubmodelTemplateAsync(SubmodelId, path, null, CancellationToken.None);

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
    public async Task GetSubmodelTemplateAsync_ThrowsInternalDataProcessingException_WhenTypeValueListElementIsProperty_AndListIndexExceedsAvailableElements()
    {
        var submodel = TestData.CreateSubmodelWithPropertyInsideList();
        const string path = "listProperty[2]";
        _mappingProvider.GetTemplateId(SubmodelId).Returns(TemplateId);
        _templateProvider.GetFilteredSubmodelTemplateAsync(TemplateId, null, Arg.Any<CancellationToken>())
            .Returns(submodel);

        await Assert.ThrowsAsync<InternalDataProcessingException>(() =>
            _sut.GetSubmodelTemplateAsync(SubmodelId, path, null, CancellationToken.None));
    }

    [Fact]
    public async Task GetSubmodelTemplateAsync_ThrowsNotFoundException_WhenPathSegmentHasListElement_AndIsInvalid()
    {
        var submodel = TestData.CreateSubmodelWithModel3DList();
        const string path = "Model3D[0].NonExistentFile";
        _mappingProvider.GetTemplateId(SubmodelId).Returns(TemplateId);
        _templateProvider.GetFilteredSubmodelTemplateAsync(TemplateId, null, Arg.Any<CancellationToken>())
            .Returns(submodel);

        await Assert.ThrowsAsync<SubmodelElementNotFoundException>(() =>
            _sut.GetSubmodelTemplateAsync(SubmodelId, path, null, CancellationToken.None));
    }

    #endregion

    #region ValidateSemanticIdFilter

    [Fact]
    public async Task ValidateSemanticIdFilter_ReturnsTrue_WhenTemplateIdMatchesFilteredTemplateId()
    {
        _mappingProvider.GetTemplateId(SubmodelId).Returns(TemplateId);

        var result = await _sut.ValidateSemanticIdFilter(SubmodelId, TemplateId);

        Assert.True(result);
    }

    [Fact]
    public async Task ValidateSemanticIdFilter_ReturnsFalse_WhenTemplateIdDoesNotMatchFilteredTemplateId()
    {
        _mappingProvider.GetTemplateId(SubmodelId).Returns(TemplateId);

        var result = await _sut.ValidateSemanticIdFilter(SubmodelId, "different-template-id");

        Assert.False(result);
    }

    [Fact]
    public async Task ValidateSemanticIdFilter_ReturnsFalse_WhenMappingThrowsResourceNotFoundException()
    {
        _mappingProvider.GetTemplateId(SubmodelId).Throws(new ResourceNotFoundException());

        var result = await _sut.ValidateSemanticIdFilter(SubmodelId, TemplateId);

        Assert.False(result);
    }

    [Fact]
    public async Task ValidateSemanticIdFilter_ThrowsInternalDataProcessingException_WhenSubmodelIdIsNull()
    {
        await Assert.ThrowsAsync<InternalDataProcessingException>(() =>
            _sut.ValidateSemanticIdFilter(null!, TemplateId));
    }

    [Fact]
    public async Task ValidateSemanticIdFilter_ThrowsInternalDataProcessingException_WhenSubmodelIdIsEmpty()
    {
        await Assert.ThrowsAsync<InternalDataProcessingException>(() =>
            _sut.ValidateSemanticIdFilter("", TemplateId));
    }

    #endregion

    #region GetFilteredSubmodelTemplateAsync

    [Fact]
    public async Task GetFilteredSubmodelTemplateAsync_ReturnsTemplate_WhenValid()
    {
        var expectedSubmodel = Substitute.For<ISubmodel>();
        _mappingProvider.GetTemplateId(SubmodelId).Returns(TemplateId);
        _templateProvider
            .GetFilteredSubmodelTemplateAsync(TemplateId, null, Arg.Any<CancellationToken>())
            .Returns(expectedSubmodel);

        var result = await _sut.GetFilteredSubmodelTemplateAsync(SubmodelId, null, CancellationToken.None);

        Assert.Equal(expectedSubmodel, result);
    }

    [Fact]
    public async Task GetFilteredSubmodelTemplateAsync_PassesQueryOptionsToProvider()
    {
        var queryOptions = new SubmodelQueryOptions("deep", "withBlobValue");
        _mappingProvider.GetTemplateId(SubmodelId).Returns(TemplateId);
        _templateProvider
            .GetFilteredSubmodelTemplateAsync(TemplateId, queryOptions, Arg.Any<CancellationToken>())
            .Returns(Substitute.For<ISubmodel>());

        await _sut.GetFilteredSubmodelTemplateAsync(SubmodelId, queryOptions, CancellationToken.None);

        await _templateProvider.Received(1).GetFilteredSubmodelTemplateAsync(TemplateId, queryOptions, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetFilteredSubmodelTemplateAsync_ThrowsInternalDataProcessingException_WhenSubmodelIdIsNull()
    {
        await Assert.ThrowsAsync<InternalDataProcessingException>(() =>
            _sut.GetFilteredSubmodelTemplateAsync(null!, null, CancellationToken.None));
    }

    [Fact]
    public async Task GetFilteredSubmodelTemplateAsync_ThrowsInternalDataProcessingException_WhenSubmodelIdIsEmpty()
    {
        await Assert.ThrowsAsync<InternalDataProcessingException>(() =>
            _sut.GetFilteredSubmodelTemplateAsync("", null, CancellationToken.None));
    }

    [Fact]
    public async Task GetFilteredSubmodelTemplateAsync_ThrowsInternalDataProcessingException_WhenResponseParsingFails()
    {
        _mappingProvider.GetTemplateId(SubmodelId).Returns(TemplateId);
        _templateProvider
            .GetFilteredSubmodelTemplateAsync(TemplateId, null, Arg.Any<CancellationToken>())
            .ThrowsAsync(new ResponseParsingException());

        await Assert.ThrowsAsync<InternalDataProcessingException>(() =>
            _sut.GetFilteredSubmodelTemplateAsync(SubmodelId, null, CancellationToken.None));
    }

    [Fact]
    public async Task GetFilteredSubmodelTemplateAsync_ThrowsTemplateRequestFailedException_WhenRequestTimesOut()
    {
        _mappingProvider.GetTemplateId(SubmodelId).Returns(TemplateId);
        _templateProvider
            .GetFilteredSubmodelTemplateAsync(TemplateId, null, Arg.Any<CancellationToken>())
            .ThrowsAsync(new RequestTimeoutException());

        await Assert.ThrowsAsync<TemplateRequestFailedException>(() =>
            _sut.GetFilteredSubmodelTemplateAsync(SubmodelId, null, CancellationToken.None));
    }

    [Fact]
    public async Task GetFilteredSubmodelTemplateAsync_ThrowsRepositoryNotAvailableException_WhenServiceUnavailable()
    {
        _mappingProvider.GetTemplateId(SubmodelId).Returns(TemplateId);
        _templateProvider
            .GetFilteredSubmodelTemplateAsync(TemplateId, null, Arg.Any<CancellationToken>())
            .ThrowsAsync(new ServiceUnavailableException("down"));

        await Assert.ThrowsAsync<RepositoryNotAvailableException>(() =>
            _sut.GetFilteredSubmodelTemplateAsync(SubmodelId, null, CancellationToken.None));
    }

    #endregion

    #region GetFilteredSubmodelTemplateIdAsync

    [Fact]
    public async Task GetFilteredSubmodelTemplateIdAsync_ReturnsSubmodelId_WhenTemplateFound()
    {
        const string SemanticId = "https://example.com/semanticId";
        var submodelTemplate = Substitute.For<ISubmodel>();
        submodelTemplate.Id.Returns(SubmodelId);
        _templateProvider
            .GetFilteredSubmodelTemplateBySemanticIdAsync(SemanticId, Arg.Any<CancellationToken>())
            .Returns(submodelTemplate);

        var result = await _sut.GetFilteredSubmodelTemplateIdAsync(SemanticId, CancellationToken.None);

        Assert.Equal(SubmodelId, result);
    }

    [Fact]
    public async Task GetFilteredSubmodelTemplateIdAsync_ReturnsNull_WhenNoTemplateFound()
    {
        const string SemanticId = "https://example.com/unknownSemanticId";
        _templateProvider
            .GetFilteredSubmodelTemplateBySemanticIdAsync(SemanticId, Arg.Any<CancellationToken>())
            .Returns((ISubmodel?)null);

        var result = await _sut.GetFilteredSubmodelTemplateIdAsync(SemanticId, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetFilteredSubmodelTemplateIdAsync_ThrowsInternalDataProcessingException_WhenResponseParsingFails()
    {
        const string SemanticId = "https://example.com/semanticId";
        _templateProvider
            .GetFilteredSubmodelTemplateBySemanticIdAsync(SemanticId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new ResponseParsingException());

        await Assert.ThrowsAsync<InternalDataProcessingException>(() =>
            _sut.GetFilteredSubmodelTemplateIdAsync(SemanticId, CancellationToken.None));
    }

    [Fact]
    public async Task GetFilteredSubmodelTemplateIdAsync_ThrowsTemplateRequestFailedException_WhenRequestTimesOut()
    {
        const string SemanticId = "https://example.com/semanticId";
        _templateProvider
            .GetFilteredSubmodelTemplateBySemanticIdAsync(SemanticId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new RequestTimeoutException());

        await Assert.ThrowsAsync<TemplateRequestFailedException>(() =>
            _sut.GetFilteredSubmodelTemplateIdAsync(SemanticId, CancellationToken.None));
    }

    [Fact]
    public async Task GetFilteredSubmodelTemplateIdAsync_ThrowsRepositoryNotAvailableException_WhenServiceUnavailable()
    {
        const string SemanticId = "https://example.com/semanticId";
        _templateProvider
            .GetFilteredSubmodelTemplateBySemanticIdAsync(SemanticId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new ServiceUnavailableException("down"));

        await Assert.ThrowsAsync<RepositoryNotAvailableException>(() =>
            _sut.GetFilteredSubmodelTemplateIdAsync(SemanticId, CancellationToken.None));
    }

    #endregion

    private static string GetSemanticId(IHasSemantics hasSemantics) => hasSemantics.SemanticId?.Keys?.FirstOrDefault()?.Value ?? string.Empty;
}
