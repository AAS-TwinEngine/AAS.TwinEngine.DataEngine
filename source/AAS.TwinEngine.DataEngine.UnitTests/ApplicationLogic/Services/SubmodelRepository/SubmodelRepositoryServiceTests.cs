using System.Text;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasRepository;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Shared.Providers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository;
using AAS.TwinEngine.DataEngine.DomainModel.AasRegistry;
using AAS.TwinEngine.DataEngine.DomainModel.AasRepository;
using AAS.TwinEngine.DataEngine.DomainModel.Plugin;
using AAS.TwinEngine.DataEngine.DomainModel.Shared;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using AasCore.Aas3_1;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using ISubmodelTemplateService = AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.ISubmodelTemplateService;

namespace AAS.TwinEngine.DataEngine.UnitTests.ApplicationLogic.Services.SubmodelRepository;

public class SubmodelRepositoryServiceTests
{
    private readonly ISubmodelTemplateService _templateService = Substitute.For<ISubmodelTemplateService>();
    private readonly ISemanticIdHandler _semanticIdHandler = Substitute.For<ISemanticIdHandler>();
    private readonly IPluginDataHandler _pluginDataHandler = Substitute.For<IPluginDataHandler>();
    private readonly IPluginManifestConflictHandler _pluginManifestConflictHandler = Substitute.For<IPluginManifestConflictHandler>();
    private readonly IAasRepositoryTemplateService _aasRepositoryTemplateService = Substitute.For<IAasRepositoryTemplateService>();
    private readonly IFileContentProvider _fileContentProvider = Substitute.For<IFileContentProvider>();
    private readonly ILogger<SubmodelRepositoryService> _logger = Substitute.For<ILogger<SubmodelRepositoryService>>();
    private readonly SubmodelRepositoryService _sut;

    private const string SubmodelId = "NameplateSubmodel";
    private const string IdShortPath = "ContactInformation";

    public SubmodelRepositoryServiceTests()
    {
        var templateManagementOptions = Options.Create(new TemplateManagementConfig
        {
            SubmodelTemplateRepository = new ServiceInstance
            {
                ConcurrentOperationsLimit = 10
            }
        });

        _sut = new SubmodelRepositoryService(
            _logger,
            _templateService,
            _aasRepositoryTemplateService,
            templateManagementOptions,
            _semanticIdHandler,
            _pluginDataHandler,
            _pluginManifestConflictHandler,
            _fileContentProvider,
            Options.Create(new GeneralConfig { MaxFileAttachmentSizeBytes = 30 * 1024 * 1024 }));
    }

    #region GetSubmodelAsync

    [Fact]
    public async Task GetSubmodelAsync_ReturnsFilledSubmodel()
    {
        var expected = TestData.CreateFilledSubmodel();
        ArrangeSubmodelBuild(SubmodelId, expected);

        var result = await _sut.GetSubmodelAsync(SubmodelId, null, CancellationToken.None);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task GetSubmodelAsync_WhenQueryOptionsProvided_PassesThemToTemplateService()
    {
        var queryOptions = new SubmodelQueryOptions("deep", "withBlobValue");
        ArrangeSubmodelBuild(SubmodelId, TestData.CreateFilledSubmodel());

        await _sut.GetSubmodelAsync(SubmodelId, queryOptions, CancellationToken.None);

        await _templateService.Received(1)
            .GetFilteredSubmodelTemplateAsync(SubmodelId, queryOptions, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSubmodelAsync_WhenTemplateReturnsNull_ThrowsSubmodelNotFoundException()
    {
        _templateService
            .GetFilteredSubmodelTemplateAsync(SubmodelId, Arg.Any<SubmodelQueryOptions?>(), Arg.Any<CancellationToken>())
            .Returns((ISubmodel?)null);

        await Assert.ThrowsAsync<SubmodelNotFoundException>(() =>
            _sut.GetSubmodelAsync(SubmodelId, null, CancellationToken.None));
    }

    [Fact]
    public async Task GetSubmodelAsync_WhenResourceNotFound_ThrowsSubmodelNotFoundException()
    {
        _templateService
            .GetFilteredSubmodelTemplateAsync(SubmodelId, Arg.Any<SubmodelQueryOptions?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ResourceNotFoundException());

        await Assert.ThrowsAsync<SubmodelNotFoundException>(() =>
            _sut.GetSubmodelAsync(SubmodelId, null, CancellationToken.None));
    }

    [Fact]
    public async Task GetSubmodelAsync_WhenResponseParsingFails_ThrowsInternalDataProcessingException()
    {
        ArrangeSubmodelBuild_PluginThrows(new ResponseParsingException());

        await Assert.ThrowsAsync<InternalDataProcessingException>(() =>
            _sut.GetSubmodelAsync(SubmodelId, null, CancellationToken.None));
    }

    [Fact]
    public async Task GetSubmodelAsync_WhenRequestTimesOut_ThrowsPluginNotAvailableException()
    {
        ArrangeSubmodelBuild_PluginThrows(new RequestTimeoutException());

        await Assert.ThrowsAsync<PluginNotAvailableException>(() =>
            _sut.GetSubmodelAsync(SubmodelId, null, CancellationToken.None));
    }

    [Fact]
    public async Task GetSubmodelAsync_WhenUnauthorized_ThrowsServiceUnAuthorizedException()
    {
        ArrangeSubmodelBuild_PluginThrows(new AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure.UnauthorizedAccessException());

        await Assert.ThrowsAsync<ServiceUnAuthorizedException>(() =>
            _sut.GetSubmodelAsync(SubmodelId, null, CancellationToken.None));
    }

    [Fact]
    public async Task GetSubmodelAsync_WhenMultiPluginConflict_ThrowsInternalDataProcessingException()
    {
        ArrangeSubmodelBuild_PluginThrows(new MultiPluginConflictException());

        await Assert.ThrowsAsync<InternalDataProcessingException>(() =>
            _sut.GetSubmodelAsync(SubmodelId, null, CancellationToken.None));
    }

    [Fact]
    public async Task GetSubmodelAsync_SetsSubmodelIdOnResult()
    {
        var filledSubmodel = TestData.CreateFilledSubmodel();
        ArrangeSubmodelBuild(SubmodelId, filledSubmodel);

        var result = await _sut.GetSubmodelAsync(SubmodelId, null, CancellationToken.None);

        Assert.Equal(SubmodelId, result.Id);
    }

    #endregion

    #region GetSubmodelElementAsync

    [Fact]
    public async Task GetSubmodelElementAsync_ReturnsFilledSubmodelElement()
    {
        var submodel = TestData.CreateSubmodelWithoutExtraElementsNested();
        var filledSubmodel = TestData.CreateFilledSubmodelWithOutExtraElements();
        var expected = TestData.CreateFilledContactInformation();

        _templateService
            .GetSubmodelTemplateAsync(SubmodelId, IdShortPath, Arg.Any<SubmodelQueryOptions?>(), Arg.Any<CancellationToken>())
            .Returns(submodel);

        ArrangeSemanticPipeline(submodel, filledSubmodel);
        _semanticIdHandler.Extract(filledSubmodel, IdShortPath).Returns(expected);

        var result = await _sut.GetSubmodelElementAsync(SubmodelId, IdShortPath, null, CancellationToken.None) as SubmodelElementCollection;

        Assert.Equal(expected.SemanticId, result?.SemanticId);
        Assert.Equal(expected.Value, result?.Value);
    }

    [Fact]
    public async Task GetSubmodelElementAsync_IdShortWithIndex_ReturnsFilledSubmodelElement()
    {
        var submodel = TestData.CreateSubmodelWithoutExtraElementsNested();
        const string IdShortPathWithNestedElement = "ContactInformation0.ContactName";
        var filledSubmodel = TestData.CreateFilledSubmodelWithOutExtraElements();
        var expected = TestData.CreateFilledContactName();

        _templateService
            .GetSubmodelTemplateAsync(SubmodelId, IdShortPathWithNestedElement, Arg.Any<SubmodelQueryOptions?>(), Arg.Any<CancellationToken>())
            .Returns(submodel);

        ArrangeSemanticPipeline(submodel, filledSubmodel);
        _semanticIdHandler.Extract(filledSubmodel, IdShortPathWithNestedElement).Returns(expected);

        var result = await _sut.GetSubmodelElementAsync(SubmodelId, IdShortPathWithNestedElement, null, CancellationToken.None) as Property;

        Assert.Equal(expected.SemanticId, result?.SemanticId);
        Assert.Equal(expected.Value, result?.Value);
    }

    [Fact]
    public async Task GetSubmodelElementAsync_WhenResourceNotFound_ThrowsSubmodelElementNotFoundException()
    {
        _templateService
            .GetSubmodelTemplateAsync(SubmodelId, IdShortPath, Arg.Any<SubmodelQueryOptions?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ResourceNotFoundException());

        await Assert.ThrowsAsync<SubmodelElementNotFoundException>(() =>
            _sut.GetSubmodelElementAsync(SubmodelId, IdShortPath, null, CancellationToken.None));
    }

    [Fact]
    public async Task GetSubmodelElementAsync_WhenResponseParsingFails_ThrowsInternalDataProcessingException()
    {
        _templateService
            .GetSubmodelTemplateAsync(SubmodelId, IdShortPath, Arg.Any<SubmodelQueryOptions?>(), Arg.Any<CancellationToken>())
            .Returns(TestData.CreateSubmodel());
        _semanticIdHandler.Extract(Arg.Any<ISubmodel>()).Returns(CreateSubmodelTreeNode(""));
        _pluginDataHandler
            .TryGetValuesAsync(Arg.Any<IReadOnlyList<PluginManifest>>(), Arg.Any<SemanticTreeNode>(), SubmodelId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new ResponseParsingException());

        await Assert.ThrowsAsync<InternalDataProcessingException>(() =>
            _sut.GetSubmodelElementAsync(SubmodelId, IdShortPath, null, CancellationToken.None));
    }

    [Fact]
    public async Task GetSubmodelElementAsync_WhenRequestTimesOut_ThrowsPluginNotAvailableException()
    {
        _templateService
            .GetSubmodelTemplateAsync(SubmodelId, IdShortPath, Arg.Any<SubmodelQueryOptions?>(), Arg.Any<CancellationToken>())
            .Returns(TestData.CreateSubmodel());
        _semanticIdHandler.Extract(Arg.Any<ISubmodel>()).Returns(CreateSubmodelTreeNode(""));
        _pluginDataHandler
            .TryGetValuesAsync(Arg.Any<IReadOnlyList<PluginManifest>>(), Arg.Any<SemanticTreeNode>(), SubmodelId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new RequestTimeoutException());

        await Assert.ThrowsAsync<PluginNotAvailableException>(() =>
            _sut.GetSubmodelElementAsync(SubmodelId, IdShortPath, null, CancellationToken.None));
    }

    #endregion

    #region GetAllSubmodelsAsync

    [Fact]
    public async Task GetAllSubmodelsAsync_ReturnsEmpty_WhenNoShellsFound()
    {
        _pluginDataHandler
            .GetDataForShellsByAssetIdsAsync(Arg.Any<IReadOnlyList<PluginManifest>>(), Arg.Any<ShellSearchFilter?>(), Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new ShellDescriptorsMetaData { ShellDescriptors = [] });

        var result = await _sut.GetAllSubmodelsAsync(null, null, null, null, CancellationToken.None);

        Assert.Empty(result.Result);
    }

    [Fact]
    public async Task GetAllSubmodelsAsync_BuildsSubmodelForEachSubmodelId()
    {
        const string ShellId = "https://example.com/shells/001";
        const string SubmodelId1 = "https://example.com/submodels/Nameplate";

        ArrangeShellsResponse([new ShellDescriptorMetaData { Id = ShellId }]);
        ArrangeSubmodelRefsForShell(ShellId, [SubmodelId1]);
        ArrangeValidateSemanticIdFilter(SubmodelId1, true);
        ArrangeSubmodelBuild(SubmodelId1, TestData.CreateFilledSubmodel());

        var result = await _sut.GetAllSubmodelsAsync(null, null, null, null, CancellationToken.None);

        Assert.Single(result.Result);
    }

    [Fact]
    public async Task GetAllSubmodelsAsync_SkipsShell_WhenSubmodelRefsFails()
    {
        const string ShellId = "https://example.com/shells/001";

        ArrangeShellsResponse([new ShellDescriptorMetaData { Id = ShellId }]);
        _aasRepositoryTemplateService
            .GetSubmodelRefByIdAsync(ShellId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new ResourceNotFoundException());

        var result = await _sut.GetAllSubmodelsAsync(null, null, null, null, CancellationToken.None);

        Assert.Empty(result.Result);
    }

    [Fact]
    public async Task GetAllSubmodelsAsync_FiltersShellsByIdShort_WhenIdShortProvided()
    {
        const string IdShort = "M&M01";
        var filter = new SubmodelSearchFilter { IdShort = IdShort };

        _pluginDataHandler
            .GetDataForShellsByAssetIdsAsync(
                Arg.Any<IReadOnlyList<PluginManifest>>(),
                Arg.Is<ShellSearchFilter?>(f => f != null && f.IdShort == IdShort),
                Arg.Any<int?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(new ShellDescriptorsMetaData { ShellDescriptors = [] });

        await _sut.GetAllSubmodelsAsync(filter, null, null, null, CancellationToken.None);

        await _pluginDataHandler.Received(1)
            .GetDataForShellsByAssetIdsAsync(
                Arg.Any<IReadOnlyList<PluginManifest>>(),
                Arg.Is<ShellSearchFilter?>(f => f != null && f.IdShort == IdShort),
                Arg.Any<int?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAllSubmodelsAsync_WhenSemanticIdFilterProvided_LooksUpFilteredTemplateId()
    {
        const string SemanticId = "https://example.com/semanticId";
        const string FilteredTemplateId = "Nameplate";
        const string ShellId = "https://example.com/shells/001";
        const string SubmodelId1 = "https://example.com/submodels/Nameplate";
        var filter = new SubmodelSearchFilter { SemanticId = SemanticId };

        _templateService
            .GetFilteredSubmodelTemplateIdAsync(SemanticId, Arg.Any<CancellationToken>())
            .Returns(FilteredTemplateId);

        ArrangeShellsResponse([new ShellDescriptorMetaData { Id = ShellId }]);
        ArrangeSubmodelRefsForShell(ShellId, [SubmodelId1]);
        _templateService.ValidateSemanticIdFilter(SubmodelId1, FilteredTemplateId).Returns(true);
        ArrangeSubmodelBuild(SubmodelId1, TestData.CreateFilledSubmodel());

        var result = await _sut.GetAllSubmodelsAsync(filter, null, null, null, CancellationToken.None);

        Assert.Single(result.Result);
        await _templateService.Received(1).GetFilteredSubmodelTemplateIdAsync(SemanticId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAllSubmodelsAsync_WhenSemanticIdNotFound_ThrowsSubmodelNotFoundException()
    {
        const string SemanticId = "https://example.com/unknown-semantic-id";
        var filter = new SubmodelSearchFilter { SemanticId = SemanticId };

        _templateService
            .GetFilteredSubmodelTemplateIdAsync(SemanticId, Arg.Any<CancellationToken>())
            .Returns((string?)null);

        await Assert.ThrowsAsync<SubmodelNotFoundException>(() =>
            _sut.GetAllSubmodelsAsync(filter, null, null, null, CancellationToken.None));
    }

    [Fact]
    public async Task GetAllSubmodelsAsync_FiltersSubmodels_WhenValidateSemanticIdFilterReturnsFalse()
    {
        const string SemanticId = "https://example.com/semanticId";
        const string FilteredTemplateId = "Nameplate";
        const string ShellId = "https://example.com/shells/001";
        const string SubmodelId1 = "https://example.com/submodels/sm-1";
        const string SubmodelId2 = "https://example.com/submodels/sm-2";
        var filter = new SubmodelSearchFilter { SemanticId = SemanticId };

        _templateService.GetFilteredSubmodelTemplateIdAsync(SemanticId, Arg.Any<CancellationToken>()).Returns(FilteredTemplateId);
        ArrangeShellsResponse([new ShellDescriptorMetaData { Id = ShellId }]);
        ArrangeSubmodelRefsForShell(ShellId, [SubmodelId1, SubmodelId2]);
        _templateService.ValidateSemanticIdFilter(SubmodelId1, FilteredTemplateId).Returns(true);
        _templateService.ValidateSemanticIdFilter(SubmodelId2, FilteredTemplateId).Returns(false);
        ArrangeSubmodelBuild(SubmodelId1, TestData.CreateFilledSubmodel());

        var result = await _sut.GetAllSubmodelsAsync(filter, null, null, null, CancellationToken.None);

        Assert.Single(result.Result);
    }

    [Fact]
    public async Task GetAllSubmodelsAsync_SkipsShellsWithEmptyOrWhitespaceId()
    {
        const string ValidShellId = "https://example.com/shells/valid";
        const string SubmodelId1 = "https://example.com/submodels/Nameplate";

        ArrangeShellsResponse([
            new ShellDescriptorMetaData { Id = string.Empty },
            new ShellDescriptorMetaData { Id = "   " },
            new ShellDescriptorMetaData { Id = ValidShellId }
        ]);
        ArrangeSubmodelRefsForShell(ValidShellId, [SubmodelId1]);
        ArrangeValidateSemanticIdFilter(SubmodelId1, true);
        ArrangeSubmodelBuild(SubmodelId1, TestData.CreateFilledSubmodel());

        var result = await _sut.GetAllSubmodelsAsync(null, null, null, null, CancellationToken.None);

        Assert.Single(result.Result);
        await _aasRepositoryTemplateService.Received(1).GetSubmodelRefByIdAsync(ValidShellId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAllSubmodelsAsync_WhenLimitReachedAtAasBoundary_CursorContainsLastSubmodelAndConsumedAas()
    {
        const string Shell1Id = "https://example.com/shells/aas-1";
        const string Shell2Id = "https://example.com/shells/aas-2";
        const string Sm1 = "https://example.com/submodels/sm-1";
        const string Sm2 = "https://example.com/submodels/sm-2";
        const string Sm3 = "https://example.com/submodels/sm-3";
        const string Sm4 = "https://example.com/submodels/sm-4";
        const string Sm5 = "https://example.com/submodels/sm-5";

        ArrangeShellsResponse([
            new ShellDescriptorMetaData { Id = Shell1Id },
            new ShellDescriptorMetaData { Id = Shell2Id }
        ]);
        ArrangeSubmodelRefsForShell(Shell1Id, [Sm1, Sm2, Sm3]);
        ArrangeSubmodelRefsForShell(Shell2Id, [Sm4, Sm5]);
        ArrangeValidateSemanticIdFilterForAll(true);
        ArrangeSubmodelBuildForAny(TestData.CreateFilledSubmodel());

        var result = await _sut.GetAllSubmodelsAsync(null, null, limit: 5, null, CancellationToken.None);

        Assert.Equal(5, result.Result.Count);
        Assert.NotNull(result.PagingMetaData?.Cursor);

        var decoded = SubmodelPaginationCursor.Decode(result.PagingMetaData!.Cursor!);
        Assert.Equal(Sm5, decoded!.SubmodelId);
        Assert.Equal(Shell2Id, decoded.AasId);
    }

    [Fact]
    public async Task GetAllSubmodelsAsync_WhenLimitReachedMidAas_CursorAasIdIsCurrentShell()
    {
        const string Shell1Id = "https://example.com/shells/aas-1";
        const string Sm1 = "https://example.com/submodels/sm-1";
        const string Sm2 = "https://example.com/submodels/sm-2";
        const string Sm3 = "https://example.com/submodels/sm-3";

        _pluginDataHandler
            .GetDataForShellsByAssetIdsAsync(Arg.Any<IReadOnlyList<PluginManifest>>(), Arg.Any<ShellSearchFilter?>(), Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new ShellDescriptorsMetaData
            {
                ShellDescriptors = [new ShellDescriptorMetaData { Id = Shell1Id }],
                PagingMetaData = new PagingMetaData { Cursor = "next-page-token" }
            });
        ArrangeSubmodelRefsForShell(Shell1Id, [Sm1, Sm2, Sm3]);
        ArrangeValidateSemanticIdFilterForAll(true);
        ArrangeSubmodelBuildForAny(TestData.CreateFilledSubmodel());

        var result = await _sut.GetAllSubmodelsAsync(null, null, limit: 2, null, CancellationToken.None);

        Assert.Equal(2, result.Result.Count);
        Assert.NotNull(result.PagingMetaData?.Cursor);

        var decoded = SubmodelPaginationCursor.Decode(result.PagingMetaData!.Cursor!);
        Assert.Equal(Sm2, decoded!.SubmodelId);
    }

    [Fact]
    public async Task GetAllSubmodelsAsync_WhenLimitReachedAtExactAasBoundary_CursorAasIdIsConsumedAas()
    {
        const string Shell1Id = "https://example.com/shells/aas-1";
        const string Sm1 = "https://example.com/submodels/sm-1";
        const string Sm2 = "https://example.com/submodels/sm-2";

        _pluginDataHandler
            .GetDataForShellsByAssetIdsAsync(Arg.Any<IReadOnlyList<PluginManifest>>(), Arg.Any<ShellSearchFilter?>(), Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new ShellDescriptorsMetaData
            {
                ShellDescriptors = [new ShellDescriptorMetaData { Id = Shell1Id }],
                PagingMetaData = new PagingMetaData { Cursor = "next-page-token" }
            });
        ArrangeSubmodelRefsForShell(Shell1Id, [Sm1, Sm2]);
        ArrangeValidateSemanticIdFilterForAll(true);
        ArrangeSubmodelBuildForAny(TestData.CreateFilledSubmodel());

        var result = await _sut.GetAllSubmodelsAsync(null, null, limit: 2, null, CancellationToken.None);

        Assert.Equal(2, result.Result.Count);
        Assert.NotNull(result.PagingMetaData?.Cursor);

        var decoded = SubmodelPaginationCursor.Decode(result.PagingMetaData!.Cursor!);
        Assert.Equal(Sm2, decoded!.SubmodelId);
        Assert.Equal(Shell1Id, decoded.AasId);
    }

    [Fact]
    public async Task GetAllSubmodelsAsync_NoCursor_WhenAllResultsFitInPage()
    {
        const string ShellId = "https://example.com/shells/001";
        const string SubmodelId1 = "https://example.com/submodels/sm-1";

        ArrangeShellsResponse([new ShellDescriptorMetaData { Id = ShellId }]);
        ArrangeSubmodelRefsForShell(ShellId, [SubmodelId1]);
        ArrangeValidateSemanticIdFilter(SubmodelId1, true);
        ArrangeSubmodelBuild(SubmodelId1, TestData.CreateFilledSubmodel());

        var result = await _sut.GetAllSubmodelsAsync(null, null, limit: 100, null, CancellationToken.None);

        Assert.Single(result.Result);
        Assert.Null(result.PagingMetaData?.Cursor);
    }

    [Fact]
    public async Task GetAllSubmodelsAsync_DefaultsTo100_WhenLimitIsNull()
    {
        ArrangeShellsResponse([]);

        await _sut.GetAllSubmodelsAsync(null, null, null, null, CancellationToken.None);

        await _pluginDataHandler.Received(1).GetDataForShellsByAssetIdsAsync(
            Arg.Any<IReadOnlyList<PluginManifest>>(),
            Arg.Any<ShellSearchFilter?>(),
            100,
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAllSubmodelsAsync_SkipsSubmodelRefsWithNullOrEmptyValues()
    {
        const string ShellId = "https://example.com/shells/001";
        const string ValidSubmodelId = "https://example.com/submodels/valid";

        ArrangeShellsResponse([new ShellDescriptorMetaData { Id = ShellId }]);
        _aasRepositoryTemplateService.GetSubmodelRefByIdAsync(ShellId, Arg.Any<CancellationToken>())
            .Returns(new List<IReference>
            {
                new Reference(ReferenceTypes.ModelReference, [new Key(KeyTypes.Submodel, "")]),
                new Reference(ReferenceTypes.ModelReference, [new Key(KeyTypes.Submodel, ValidSubmodelId)])
            });
        ArrangeValidateSemanticIdFilter(ValidSubmodelId, true);
        ArrangeSubmodelBuild(ValidSubmodelId, TestData.CreateFilledSubmodel());

        var result = await _sut.GetAllSubmodelsAsync(null, null, null, null, CancellationToken.None);

        Assert.Single(result.Result);
    }

    #endregion

    #region GetAllSubmodelElementsAsync

    [Fact]
    public async Task GetAllSubmodelElementsAsync_ReturnsAllElements_WhenSubmodelExists()
    {
        var filledSubmodel = TestData.CreateFilledSubmodel();
        ArrangeSubmodelBuild(SubmodelId, filledSubmodel);

        var result = await _sut.GetAllSubmodelElementsAsync(SubmodelId, null, null, null, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(filledSubmodel.SubmodelElements!.Count, result.Result.Count);
        Assert.Null(result.PagingMetaData?.Cursor);
    }

    [Fact]
    public async Task GetAllSubmodelElementsAsync_ReturnsEmptyList_WhenSubmodelHasNoElements()
    {
        var emptySubmodel = new Submodel(
            id: "http://example.com/idta/empty",
            idShort: "Empty",
            submodelElements: []);

        _templateService
            .GetFilteredSubmodelTemplateAsync(SubmodelId, Arg.Any<SubmodelQueryOptions?>(), Arg.Any<CancellationToken>())
            .Returns(emptySubmodel);
        _semanticIdHandler.Extract(Arg.Any<ISubmodel>()).Returns(CreateSubmodelTreeNode(""));
        _pluginDataHandler
            .TryGetValuesAsync(Arg.Any<IReadOnlyList<PluginManifest>>(), Arg.Any<SemanticTreeNode>(), SubmodelId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateSubmodelTreeNode("") as SemanticTreeNode));
        _semanticIdHandler.FillOutTemplate(Arg.Any<ISubmodel>(), Arg.Any<SemanticTreeNode>()).Returns(emptySubmodel);

        var result = await _sut.GetAllSubmodelElementsAsync(SubmodelId, null, null, null, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result.Result);
    }

    [Fact]
    public async Task GetAllSubmodelElementsAsync_ReturnsPagedResult_WhenLimitApplied()
    {
        var filledSubmodel = TestData.CreateFilledSubmodel();
        ArrangeSubmodelBuild(SubmodelId, filledSubmodel);

        var result = await _sut.GetAllSubmodelElementsAsync(SubmodelId, null, limit: 2, cursor: null, CancellationToken.None);

        Assert.Equal(2, result.Result.Count);
        Assert.NotNull(result.PagingMetaData?.Cursor);
    }

    [Fact]
    public async Task GetAllSubmodelElementsAsync_WhenTemplateNull_ThrowsSubmodelElementNotFoundException()
    {
        _templateService
            .GetFilteredSubmodelTemplateAsync(SubmodelId, Arg.Any<SubmodelQueryOptions?>(), Arg.Any<CancellationToken>())
            .Returns((ISubmodel?)null);

        await Assert.ThrowsAsync<SubmodelElementNotFoundException>(() =>
            _sut.GetAllSubmodelElementsAsync(SubmodelId, null, null, null, CancellationToken.None));
    }

    [Fact]
    public async Task GetAllSubmodelElementsAsync_WhenResourceNotFound_ThrowsSubmodelElementNotFoundException()
    {
        _templateService
            .GetFilteredSubmodelTemplateAsync(SubmodelId, Arg.Any<SubmodelQueryOptions?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ResourceNotFoundException());

        await Assert.ThrowsAsync<SubmodelElementNotFoundException>(() =>
            _sut.GetAllSubmodelElementsAsync(SubmodelId, null, null, null, CancellationToken.None));
    }

    [Fact]
    public async Task GetAllSubmodelElementsAsync_WhenResponseParsingFails_ThrowsInternalDataProcessingException()
    {
        _templateService
            .GetFilteredSubmodelTemplateAsync(SubmodelId, Arg.Any<SubmodelQueryOptions?>(), Arg.Any<CancellationToken>())
            .Returns(TestData.CreateSubmodel());
        _semanticIdHandler.Extract(Arg.Any<ISubmodel>()).Returns(CreateSubmodelTreeNode(""));
        _pluginDataHandler
            .TryGetValuesAsync(Arg.Any<IReadOnlyList<PluginManifest>>(), Arg.Any<SemanticTreeNode>(), SubmodelId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new ResponseParsingException());

        await Assert.ThrowsAsync<InternalDataProcessingException>(() =>
            _sut.GetAllSubmodelElementsAsync(SubmodelId, null, null, null, CancellationToken.None));
    }

    #endregion

    #region GetFileAttachmentAsync

    [Fact]
    public async Task GetFileAttachmentAsync_WhenElementIsFileWithHttpUrl_ReturnsStreamWithCorrectMetadata()
    {
        const string fileIdShortPath = "Documents.ProductImage";
        const string FileUrl = "https://fake-plugin.local/files/product.png";
        const string FileContent = "binary-file-data";

        var fileElement = new AasCore.Aas3_1.File(contentType: "image/png") { Value = FileUrl, IdShort = "ProductImage" };
        ArrangeAttachmentElement(fileIdShortPath, fileElement);

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(FileContent));
        var fileContentResponse = new FileContentResponse(stream);
        _fileContentProvider.GetFileContentAsync(FileUrl, Arg.Any<CancellationToken>()).Returns(fileContentResponse);

        var result = await _sut.GetFileAttachmentAsync(SubmodelId, fileIdShortPath, CancellationToken.None);
        await using (result.Content)
        {
            var body = await new StreamReader(result.Content).ReadToEndAsync();
            Assert.Equal(FileContent, body);
            Assert.Equal("product.png", result.FileName);
            Assert.Contains("image/png", result.ContentType);
        }

        await _fileContentProvider.Received(1).GetFileContentAsync(FileUrl, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetFileAttachmentAsync_WhenContentTypeIsEmpty_DefaultsToOctetStream()
    {
        const string fileIdShortPath = "Documents.ProductImage";
        const string FileUrl = "https://fake-plugin.local/files/data.bin";

        var fileElement = new AasCore.Aas3_1.File(contentType: "") { Value = FileUrl, IdShort = "ProductImage" };
        ArrangeAttachmentElement(fileIdShortPath, fileElement);

        var stream = new MemoryStream([0x01, 0x02]);
        _fileContentProvider.GetFileContentAsync(FileUrl, Arg.Any<CancellationToken>()).Returns(new FileContentResponse(stream));

        var result = await _sut.GetFileAttachmentAsync(SubmodelId, fileIdShortPath, CancellationToken.None);
        await using (result.Content)
        {
            Assert.Equal("application/octet-stream", result.ContentType);
        }
    }

    [Fact]
    public async Task GetFileAttachmentAsync_WhenElementIsNotFile_ThrowsInvalidUserInputException()
    {
        const string fileIdShortPath = "ManufacturerName";
        var property = new Property(DataTypeDefXsd.String) { IdShort = "ManufacturerName" };
        ArrangeAttachmentElement(fileIdShortPath, property);

        await Assert.ThrowsAsync<InvalidUserInputException>(() =>
            _sut.GetFileAttachmentAsync(SubmodelId, fileIdShortPath, CancellationToken.None));
    }

    [Fact]
    public async Task GetFileAttachmentAsync_WhenSubmodelNotFound_ThrowsSubmodelElementNotFoundException()
    {
        const string fileIdShortPath = "Documents.ProductImage";

        _templateService
            .GetSubmodelTemplateAsync(SubmodelId, fileIdShortPath, Arg.Any<SubmodelQueryOptions?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ResourceNotFoundException());

        await Assert.ThrowsAsync<SubmodelElementNotFoundException>(() =>
            _sut.GetFileAttachmentAsync(SubmodelId, fileIdShortPath, CancellationToken.None));
    }

    [Fact]
    public async Task GetFileAttachmentAsync_WhenFileValueIsEmpty_ThrowsSubmodelElementNotFoundException()
    {
        const string fileIdShortPath = "Documents.ProductImage";
        var fileElement = new AasCore.Aas3_1.File(contentType: "image/png") { Value = "", IdShort = "ProductImage" };
        ArrangeAttachmentElement(fileIdShortPath, fileElement);

        await Assert.ThrowsAsync<SubmodelElementNotFoundException>(() =>
            _sut.GetFileAttachmentAsync(SubmodelId, fileIdShortPath, CancellationToken.None));
    }

    [Fact]
    public async Task GetFileAttachmentAsync_WhenFileValueIsNull_ThrowsSubmodelElementNotFoundException()
    {
        const string fileIdShortPath = "Documents.ProductImage";
        var fileElement = new AasCore.Aas3_1.File(contentType: "image/png") { Value = null, IdShort = "ProductImage" };
        ArrangeAttachmentElement(fileIdShortPath, fileElement);

        await Assert.ThrowsAsync<SubmodelElementNotFoundException>(() =>
            _sut.GetFileAttachmentAsync(SubmodelId, fileIdShortPath, CancellationToken.None));
    }

    [Fact]
    public async Task GetFileAttachmentAsync_WhenFileUrlIsNotHttpOrHttps_ThrowsInternalDataProcessingException()
    {
        const string fileIdShortPath = "Documents.ProductImage";
        const string FileUrl = "ftp://fake-plugin.local/files/product.png";
        var fileElement = new AasCore.Aas3_1.File(contentType: "image/png") { Value = FileUrl, IdShort = "ProductImage" };
        ArrangeAttachmentElement(fileIdShortPath, fileElement);

        await Assert.ThrowsAsync<InternalDataProcessingException>(() =>
            _sut.GetFileAttachmentAsync(SubmodelId, fileIdShortPath, CancellationToken.None));
    }

    [Fact]
    public async Task GetFileAttachmentAsync_WhenFileUrlIsRelative_ThrowsInternalDataProcessingException()
    {
        const string fileIdShortPath = "Documents.ProductImage";
        const string FileUrl = "/relative/path/file.png";
        var fileElement = new AasCore.Aas3_1.File(contentType: "image/png") { Value = FileUrl, IdShort = "ProductImage" };
        ArrangeAttachmentElement(fileIdShortPath, fileElement);

        await Assert.ThrowsAsync<InternalDataProcessingException>(() =>
            _sut.GetFileAttachmentAsync(SubmodelId, fileIdShortPath, CancellationToken.None));
    }

    [Fact]
    public async Task GetFileAttachmentAsync_ExtractsFileNameFromUrl()
    {
        const string fileIdShortPath = "Documents.ProductImage";
        const string FileUrl = "https://fake-plugin.local/files/my-document.pdf";

        var fileElement = new AasCore.Aas3_1.File(contentType: "application/pdf") { Value = FileUrl, IdShort = "ProductImage" };
        ArrangeAttachmentElement(fileIdShortPath, fileElement);

        var stream = new MemoryStream([0x01]);
        _fileContentProvider.GetFileContentAsync(FileUrl, Arg.Any<CancellationToken>()).Returns(new FileContentResponse(stream));

        var result = await _sut.GetFileAttachmentAsync(SubmodelId, fileIdShortPath, CancellationToken.None);
        await using (result.Content)
        {
            Assert.Equal("my-document.pdf", result.FileName);
        }
    }

    [Fact]
    public async Task GetFileAttachmentAsync_UsesIdShortAsFileName_WhenUrlHasNoPath()
    {
        const string fileIdShortPath = "Documents.ProductImage";
        const string FileUrl = "https://fake-plugin.local/";

        var fileElement = new AasCore.Aas3_1.File(contentType: "image/png") { Value = FileUrl, IdShort = "ProductImage" };
        ArrangeAttachmentElement(fileIdShortPath, fileElement);

        var stream = new MemoryStream([0x01]);
        _fileContentProvider.GetFileContentAsync(FileUrl, Arg.Any<CancellationToken>()).Returns(new FileContentResponse(stream));

        var result = await _sut.GetFileAttachmentAsync(SubmodelId, fileIdShortPath, CancellationToken.None);
        await using (result.Content)
        {
            Assert.Equal("ProductImage", result.FileName);
        }
    }

    #endregion

    #region Helpers

    public static SemanticBranchNode CreateSubmodelTreeNode(string value)
    {
        var submodel = new SemanticBranchNode("http://example.com/idta/digital-nameplate/semantic-id", Cardinality.Unknown);
        var contactInformation = new SemanticBranchNode("http://example.com/idta/digital-nameplate/contact-information", Cardinality.ZeroToMany);
        var contactName = new SemanticLeafNode("http://example.com/idta/digital-nameplate/contact-name", value, DataType.String, Cardinality.One);
        submodel.AddChild(contactInformation);
        contactInformation.AddChild(contactName);
        return submodel;
    }

    private void ArrangeSubmodelBuild(string submodelId, Submodel filledSubmodel)
    {
        _templateService
            .GetFilteredSubmodelTemplateAsync(submodelId, Arg.Any<SubmodelQueryOptions?>(), Arg.Any<CancellationToken>())
            .Returns(TestData.CreateSubmodel());
        _semanticIdHandler.Extract(Arg.Any<ISubmodel>()).Returns(CreateSubmodelTreeNode(""));
        _pluginDataHandler
            .TryGetValuesAsync(Arg.Any<IReadOnlyList<PluginManifest>>(), Arg.Any<SemanticTreeNode>(), submodelId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateSubmodelTreeNode("") as SemanticTreeNode));
        _semanticIdHandler.FillOutTemplate(Arg.Any<ISubmodel>(), Arg.Any<SemanticTreeNode>()).Returns(filledSubmodel);
    }

    private void ArrangeSubmodelBuildForAny(Submodel filledSubmodel)
    {
        _templateService
            .GetFilteredSubmodelTemplateAsync(Arg.Any<string>(), Arg.Any<SubmodelQueryOptions?>(), Arg.Any<CancellationToken>())
            .Returns(TestData.CreateSubmodel());
        _semanticIdHandler.Extract(Arg.Any<ISubmodel>()).Returns(CreateSubmodelTreeNode(""));
        _pluginDataHandler
            .TryGetValuesAsync(Arg.Any<IReadOnlyList<PluginManifest>>(), Arg.Any<SemanticTreeNode>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateSubmodelTreeNode("") as SemanticTreeNode));
        _semanticIdHandler.FillOutTemplate(Arg.Any<ISubmodel>(), Arg.Any<SemanticTreeNode>()).Returns(filledSubmodel);
    }

    private void ArrangeSubmodelBuild_PluginThrows(Exception exception)
    {
        _templateService
            .GetFilteredSubmodelTemplateAsync(SubmodelId, Arg.Any<SubmodelQueryOptions?>(), Arg.Any<CancellationToken>())
            .Returns(TestData.CreateSubmodel());
        _semanticIdHandler.Extract(Arg.Any<ISubmodel>()).Returns(CreateSubmodelTreeNode(""));
        _pluginDataHandler
            .TryGetValuesAsync(Arg.Any<IReadOnlyList<PluginManifest>>(), Arg.Any<SemanticTreeNode>(), SubmodelId, Arg.Any<CancellationToken>())
            .ThrowsAsync(exception);
    }

    private void ArrangeSemanticPipeline(ISubmodel template, Submodel filled)
    {
        _semanticIdHandler.Extract(template).Returns(CreateSubmodelTreeNode(""));
        _pluginDataHandler
            .TryGetValuesAsync(Arg.Any<IReadOnlyList<PluginManifest>>(), Arg.Any<SemanticTreeNode>(), SubmodelId, Arg.Any<CancellationToken>())
            .Returns(CreateSubmodelTreeNode("Test John Doe"));
        _semanticIdHandler.FillOutTemplate(template, Arg.Any<SemanticBranchNode>()).Returns(filled);
    }

    private void ArrangeShellsResponse(List<ShellDescriptorMetaData> descriptors)
    {
        _pluginDataHandler
            .GetDataForShellsByAssetIdsAsync(Arg.Any<IReadOnlyList<PluginManifest>>(), Arg.Any<ShellSearchFilter?>(), Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new ShellDescriptorsMetaData { ShellDescriptors = descriptors });
    }

    private void ArrangeSubmodelRefsForShell(string shellId, List<string> submodelIds)
    {
        var refs = submodelIds.Select(id => (IReference)new Reference(ReferenceTypes.ModelReference, [new Key(KeyTypes.Submodel, id)])).ToList();
        _aasRepositoryTemplateService.GetSubmodelRefByIdAsync(shellId, Arg.Any<CancellationToken>()).Returns(refs);
    }

    private void ArrangeValidateSemanticIdFilter(string submodelId, bool result)
    {
        _templateService.ValidateSemanticIdFilter(submodelId, Arg.Any<string>()).Returns(result);
    }

    private void ArrangeValidateSemanticIdFilterForAll(bool result)
    {
        _templateService.ValidateSemanticIdFilter(Arg.Any<string>(), Arg.Any<string>()).Returns(result);
    }

    private void ArrangeAttachmentElement(string idShortPath, ISubmodelElement element)
    {
        var template = TestData.CreateSubmodelWithElement(element, idShortPath);
        _templateService.GetSubmodelTemplateAsync(SubmodelId, idShortPath, Arg.Any<SubmodelQueryOptions?>(), Arg.Any<CancellationToken>()).Returns(template);
        _semanticIdHandler.Extract(Arg.Any<ISubmodel>()).Returns(CreateSubmodelTreeNode(""));
        _pluginDataHandler.TryGetValuesAsync(Arg.Any<IReadOnlyList<PluginManifest>>(), Arg.Any<SemanticTreeNode>(), SubmodelId, Arg.Any<CancellationToken>()).Returns(CreateSubmodelTreeNode(""));
        _semanticIdHandler.FillOutTemplate(Arg.Any<ISubmodel>(), Arg.Any<SemanticTreeNode>()).Returns(template);
        _semanticIdHandler.Extract(Arg.Any<ISubmodel>(), idShortPath).Returns(element);
    }

    #endregion
}
