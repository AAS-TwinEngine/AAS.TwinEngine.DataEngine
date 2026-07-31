using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasRepository;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin;
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
    private readonly ILogger<SubmodelRepositoryService> _logger = Substitute.For<ILogger<SubmodelRepositoryService>>();
    private readonly IOptions<TemplateManagementConfig> _templateManagementOptions;
    private readonly SubmodelRepositoryService _sut;

    private const string SubmodelId = "NameplateSubmodel";
    private const string IdShortPath = "ContactInformation";

    public SubmodelRepositoryServiceTests()
    {
        _templateManagementOptions = Options.Create(new TemplateManagementConfig
        {
            SubmodelTemplateRegistry = new ServiceInstance
            {
                ConcurrentOperationsLimit = 10
            }
        });

        _sut = new SubmodelRepositoryService(
            _logger,
            _templateService,
            _semanticIdHandler,
            _pluginDataHandler,
            _pluginManifestConflictHandler,
            _aasRepositoryTemplateService,
            _templateManagementOptions);
    }

    [Fact]
    public async Task GetSubmodelAsync_ReturnsFilledSubmodel()
    {
        var semanticId = TestData.CreateSubmodelTreeNode();
        var values = TestData.CreateSubmodelTreeNode();
        var expected = TestData.CreateFilledSubmodel();

        _templateService
            .GetFilteredSubmodelTemplateAsync(SubmodelId, (string?)null, Arg.Any<SubmodelQueryOptions?>(), Arg.Any<CancellationToken>())
            .Returns(TestData.CreateSubmodel());
        _semanticIdHandler.Extract(Arg.Any<Submodel>()).Returns(semanticId);

        _pluginDataHandler
            .TryGetValuesAsync(
                Arg.Any<IReadOnlyList<PluginManifest>>(),
                Arg.Any<SemanticTreeNode>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(values));

        _semanticIdHandler.FillOutTemplate(Arg.Any<Submodel>(), values)
            .Returns(expected);

        var result = await _sut.GetSubmodelAsync(SubmodelId, null, CancellationToken.None);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task GetSubmodelAsync_WhenQueryOptionsProvided_PassesThemToTemplateService()
    {
        var queryOptions = new SubmodelQueryOptions("deep", "withBlobValue");
        var template = TestData.CreateSubmodel();

        _templateService
            .GetFilteredSubmodelTemplateAsync(SubmodelId, (string?)null, queryOptions, Arg.Any<CancellationToken>())
            .Returns(template);
        _semanticIdHandler.Extract(Arg.Any<ISubmodel>()).Returns(CreateSubmodelTreeNode(""));
        _pluginDataHandler
            .TryGetValuesAsync(Arg.Any<IReadOnlyList<PluginManifest>>(), Arg.Any<SemanticTreeNode>(), SubmodelId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateSubmodelTreeNode("") as SemanticTreeNode));
        _semanticIdHandler.FillOutTemplate(Arg.Any<ISubmodel>(), Arg.Any<SemanticTreeNode>()).Returns(TestData.CreateFilledSubmodel());

        await _sut.GetSubmodelAsync(SubmodelId, queryOptions, CancellationToken.None);

        await _templateService.Received(1)
            .GetFilteredSubmodelTemplateAsync(SubmodelId, (string?)null, queryOptions, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSubmodelAsync_WhenTemplateReturnsNull_ThrowsSubmodelNotFoundException()
    {
        _templateService
            .GetFilteredSubmodelTemplateAsync(SubmodelId, (string?)null, Arg.Any<SubmodelQueryOptions?>(), Arg.Any<CancellationToken>())
            .Returns((ISubmodel?)null);

        await Assert.ThrowsAsync<SubmodelNotFoundException>(() =>
            _sut.GetSubmodelAsync(SubmodelId, null, CancellationToken.None));
    }

    [Fact]
    public async Task GetSubmodelElementAsync_ReturnsFilledSubmodelElement()
    {
        var submodel = TestData.CreateSubmodelWithoutExtraElementsNested();
        var filledSubmodel = TestData.CreateFilledSubmodelWithOutExtraElements();
        var expected = TestData.CreateFilledContactInformation();

        _templateService
            .GetSubmodelTemplateAsync(SubmodelId, IdShortPath, Arg.Any<CancellationToken>())
            .Returns(submodel);

        var semanticTree = CreateSubmodelTreeNode("");
        _semanticIdHandler.Extract(submodel).Returns(semanticTree);

        _pluginDataHandler.TryGetValuesAsync(Arg.Any<IReadOnlyList<PluginManifest>>(), Arg.Any<SemanticTreeNode>(), SubmodelId, Arg.Any<CancellationToken>()).Returns(CreateSubmodelTreeNode("Test John Doe"));

        _semanticIdHandler.FillOutTemplate(submodel, Arg.Any<SemanticBranchNode>())
            .Returns(filledSubmodel);
        _semanticIdHandler.Extract(filledSubmodel, IdShortPath).Returns(expected);

        var result = await _sut.GetSubmodelElementAsync(SubmodelId, IdShortPath, CancellationToken.None) as SubmodelElementCollection;

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
            .GetSubmodelTemplateAsync(SubmodelId, IdShortPathWithNestedElement, Arg.Any<CancellationToken>())
            .Returns(submodel);

        var semanticTree = CreateSubmodelTreeNode("");
        _semanticIdHandler.Extract(submodel).Returns(semanticTree);

        _pluginDataHandler.TryGetValuesAsync(Arg.Any<IReadOnlyList<PluginManifest>>(), Arg.Any<SemanticTreeNode>(), SubmodelId, Arg.Any<CancellationToken>()).Returns(CreateSubmodelTreeNode("Test John Doe"));

        _semanticIdHandler.FillOutTemplate(submodel, Arg.Any<SemanticBranchNode>())
            .Returns(filledSubmodel);
        _semanticIdHandler.Extract(filledSubmodel, IdShortPathWithNestedElement).Returns(expected);

        var result = await _sut.GetSubmodelElementAsync(SubmodelId, IdShortPathWithNestedElement, CancellationToken.None) as Property;

        Assert.Equal(expected.SemanticId, result?.SemanticId);
        Assert.Equal(expected.Value, result?.Value);
    }

    [Fact]
    public async Task GetSubmodelAsync_WhenResourceNotFound_ThrowsPluginRequestFailedException()
    {
        _templateService
            .GetFilteredSubmodelTemplateAsync(SubmodelId, (string?)null, Arg.Any<SubmodelQueryOptions?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ResourceNotFoundException());

        await Assert.ThrowsAsync<SubmodelNotFoundException>(() =>
            _sut.GetSubmodelAsync(SubmodelId, null, CancellationToken.None));
    }

    [Fact]
    public async Task GetSubmodelAsync_WhenResponseParsingFails_ThrowsInternalDataProcessingException()
    {
        _pluginDataHandler
            .TryGetValuesAsync(Arg.Any<IReadOnlyList<PluginManifest>>(), Arg.Any<SemanticTreeNode>(), SubmodelId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new ResponseParsingException());

        await Assert.ThrowsAsync<InternalDataProcessingException>(() =>
            _sut.GetSubmodelAsync(SubmodelId, null, CancellationToken.None));
    }

    [Fact]
    public async Task GetSubmodelAsync_WhenRequestTimesOut_ThrowsPluginNotAvailableException()
    {
        _pluginDataHandler
            .TryGetValuesAsync(Arg.Any<IReadOnlyList<PluginManifest>>(), Arg.Any<SemanticTreeNode>(), SubmodelId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new RequestTimeoutException());

        await Assert.ThrowsAsync<PluginNotAvailableException>(() =>
            _sut.GetSubmodelAsync(SubmodelId, null, CancellationToken.None));
    }

    [Fact]
    public async Task GetSubmodelElementAsync_WhenResourceNotFound_ThrowsPluginRequestFailedException()
    {
        _templateService
            .GetSubmodelTemplateAsync(SubmodelId, IdShortPath, Arg.Any<CancellationToken>())
            .ThrowsAsync(new ResourceNotFoundException());

        await Assert.ThrowsAsync<SubmodelNotFoundException>(() =>
            _sut.GetSubmodelElementAsync(SubmodelId, IdShortPath, CancellationToken.None));
    }

    [Fact]
    public async Task GetSubmodelElementAsync_WhenResponseParsingFails_ThrowsInternalDataProcessingException()
    {
        _pluginDataHandler
            .TryGetValuesAsync(Arg.Any<IReadOnlyList<PluginManifest>>(), Arg.Any<SemanticTreeNode>(), SubmodelId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new ResponseParsingException());

        await Assert.ThrowsAsync<InternalDataProcessingException>(() =>
            _sut.GetSubmodelElementAsync(SubmodelId, IdShortPath, CancellationToken.None));
    }

    [Fact]
    public async Task GetSubmodelElementAsync_WhenRequestTimesOut_ThrowsPluginNotAvailableException()
    {
        _pluginDataHandler
            .TryGetValuesAsync(Arg.Any<IReadOnlyList<PluginManifest>>(), Arg.Any<SemanticTreeNode>(), SubmodelId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new RequestTimeoutException());

        await Assert.ThrowsAsync<PluginNotAvailableException>(() =>
            _sut.GetSubmodelElementAsync(SubmodelId, IdShortPath, CancellationToken.None));
    }

    public static SemanticBranchNode CreateSubmodelTreeNode(string value)
    {
        var submodel = new SemanticBranchNode("http://example.com/idta/digital-nameplate/semantic-id", Cardinality.Unknown);
        var contactInformation = new SemanticBranchNode("http://example.com/idta/digital-nameplate/contact-information", Cardinality.ZeroToMany);
        var contactName = new SemanticLeafNode("http://example.com/idta/digital-nameplate/contact-name", value, DataType.String, Cardinality.One);
        submodel.AddChild(contactInformation);
        contactInformation.AddChild(contactName);
        return submodel;
    }

    private static Reference SubmodelReference(string submodelId) =>
        new(ReferenceTypes.ModelReference, [new Key(KeyTypes.Submodel, submodelId)]);

    // Arranges the template + value pipeline so every collected submodel id yields one built submodel.
    private void ArrangeSubmodelBuildPipeline()
    {
        _templateService
            .GetFilteredSubmodelTemplateAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<SubmodelQueryOptions?>(), Arg.Any<CancellationToken>())
            .Returns(TestData.CreateSubmodel());

        ArrangeValuePipeline();
    }

    private void ArrangeValuePipeline()
    {
        _semanticIdHandler.Extract(Arg.Any<ISubmodel>()).Returns(CreateSubmodelTreeNode(""));
        _pluginDataHandler
            .TryGetValuesAsync(Arg.Any<IReadOnlyList<PluginManifest>>(), Arg.Any<SemanticTreeNode>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateSubmodelTreeNode("") as SemanticTreeNode));
        _semanticIdHandler.FillOutTemplate(Arg.Any<ISubmodel>(), Arg.Any<SemanticTreeNode>()).Returns(TestData.CreateFilledSubmodel());
    }

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
        var filledSubmodel = TestData.CreateFilledSubmodel();

        var submodelRef = new Reference(ReferenceTypes.ModelReference, [new Key(KeyTypes.Submodel, SubmodelId1)]);

        _pluginDataHandler
            .GetDataForShellsByAssetIdsAsync(Arg.Any<IReadOnlyList<PluginManifest>>(), Arg.Any<ShellSearchFilter?>(), Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new ShellDescriptorsMetaData { ShellDescriptors = [new ShellDescriptorMetaData { Id = ShellId }] });

        _aasRepositoryTemplateService
            .GetSubmodelRefByIdAsync(ShellId, Arg.Any<CancellationToken>())
            .Returns([submodelRef]);

        _templateService
            .GetFilteredSubmodelTemplateAsync(SubmodelId1, (string?)null, Arg.Any<SubmodelQueryOptions?>(), Arg.Any<CancellationToken>())
            .Returns(TestData.CreateSubmodel());

        _semanticIdHandler.Extract(Arg.Any<ISubmodel>()).Returns(CreateSubmodelTreeNode(""));
        _pluginDataHandler
            .TryGetValuesAsync(Arg.Any<IReadOnlyList<PluginManifest>>(), Arg.Any<SemanticTreeNode>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateSubmodelTreeNode("") as SemanticTreeNode));
        _semanticIdHandler.FillOutTemplate(Arg.Any<ISubmodel>(), Arg.Any<SemanticTreeNode>()).Returns(filledSubmodel);

        var result = await _sut.GetAllSubmodelsAsync(null, null, null, null, CancellationToken.None);

        Assert.Single(result.Result);
    }

    [Fact]
    public async Task GetAllSubmodelsAsync_SkipsSubmodel_WhenFilteredTemplateReturnsNull()
    {
        const string ShellId = "https://example.com/shells/001";
        const string SubmodelId1 = "https://example.com/submodels/Nameplate";
        var submodelRef = new Reference(ReferenceTypes.ModelReference, [new Key(KeyTypes.Submodel, SubmodelId1)]);

        _pluginDataHandler
            .GetDataForShellsByAssetIdsAsync(Arg.Any<IReadOnlyList<PluginManifest>>(), Arg.Any<ShellSearchFilter?>(), Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new ShellDescriptorsMetaData { ShellDescriptors = [new ShellDescriptorMetaData { Id = ShellId }] });

        _aasRepositoryTemplateService
            .GetSubmodelRefByIdAsync(ShellId, Arg.Any<CancellationToken>())
            .Returns([submodelRef]);

        _templateService
            .GetFilteredSubmodelTemplateAsync(SubmodelId1, (string?)null, Arg.Any<SubmodelQueryOptions?>(), Arg.Any<CancellationToken>())
            .Returns((ISubmodel?)null);

        var result = await _sut.GetAllSubmodelsAsync(null, null, null, null, CancellationToken.None);

        Assert.Empty(result.Result);
    }

    [Fact]
    public async Task GetAllSubmodelsAsync_SkipsShell_WhenSubmodelRefsFails()
    {
        const string ShellId = "https://example.com/shells/001";

        _pluginDataHandler
            .GetDataForShellsByAssetIdsAsync(Arg.Any<IReadOnlyList<PluginManifest>>(), Arg.Any<ShellSearchFilter?>(), Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new ShellDescriptorsMetaData { ShellDescriptors = [new ShellDescriptorMetaData { Id = ShellId }] });

        _aasRepositoryTemplateService
            .GetSubmodelRefByIdAsync(ShellId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new ResourceNotFoundException());

        var result = await _sut.GetAllSubmodelsAsync(null, null, null, null, CancellationToken.None);

        Assert.Empty(result.Result);
    }

    [Fact]
    public async Task GetAllSubmodelsAsync_DeduplicatesSubmodelIds_AcrossShells()
    {
        const string SubmodelId1 = "https://example.com/submodels/Shared";
        var submodelRef = new Reference(ReferenceTypes.ModelReference, [new Key(KeyTypes.Submodel, SubmodelId1)]);
        var filledSubmodel = TestData.CreateFilledSubmodel();

        _pluginDataHandler
            .GetDataForShellsByAssetIdsAsync(Arg.Any<IReadOnlyList<PluginManifest>>(), Arg.Any<ShellSearchFilter?>(), Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new ShellDescriptorsMetaData
            {
                ShellDescriptors =
                [
                    new ShellDescriptorMetaData { Id = "https://example.com/shells/001" },
                    new ShellDescriptorMetaData { Id = "https://example.com/shells/002" }
                ]
            });

        _aasRepositoryTemplateService
            .GetSubmodelRefByIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([submodelRef]);

        _templateService
            .GetFilteredSubmodelTemplateAsync(SubmodelId1, (string?)null, Arg.Any<SubmodelQueryOptions?>(), Arg.Any<CancellationToken>())
            .Returns(TestData.CreateSubmodel());

        _semanticIdHandler.Extract(Arg.Any<ISubmodel>()).Returns(CreateSubmodelTreeNode(""));
        _pluginDataHandler
            .TryGetValuesAsync(Arg.Any<IReadOnlyList<PluginManifest>>(), Arg.Any<SemanticTreeNode>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateSubmodelTreeNode("") as SemanticTreeNode));
        _semanticIdHandler.FillOutTemplate(Arg.Any<ISubmodel>(), Arg.Any<SemanticTreeNode>()).Returns(filledSubmodel);

        var result = await _sut.GetAllSubmodelsAsync(null, null, null, null, CancellationToken.None);

        Assert.Single(result.Result);
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
        var submodelRef = new Reference(ReferenceTypes.ModelReference, [new Key(KeyTypes.Submodel, SubmodelId1)]);
        var filledSubmodel = TestData.CreateFilledSubmodel();

        _pluginDataHandler
            .GetDataForShellsByAssetIdsAsync(Arg.Any<IReadOnlyList<PluginManifest>>(), Arg.Any<ShellSearchFilter?>(), Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new ShellDescriptorsMetaData { ShellDescriptors = [new ShellDescriptorMetaData { Id = ShellId }] });

        _templateService
            .GetFilteredSubmodelTemplateIdAsync(SemanticId, Arg.Any<CancellationToken>())
            .Returns(FilteredTemplateId);

        _aasRepositoryTemplateService
            .GetSubmodelRefByIdAsync(ShellId, Arg.Any<CancellationToken>())
            .Returns([submodelRef]);

        _templateService
            .GetFilteredSubmodelTemplateAsync(SubmodelId1, FilteredTemplateId, Arg.Any<SubmodelQueryOptions?>(), Arg.Any<CancellationToken>())
            .Returns(TestData.CreateSubmodel());

        _semanticIdHandler.Extract(Arg.Any<ISubmodel>()).Returns(CreateSubmodelTreeNode(""));
        _pluginDataHandler
            .TryGetValuesAsync(Arg.Any<IReadOnlyList<PluginManifest>>(), Arg.Any<SemanticTreeNode>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateSubmodelTreeNode("") as SemanticTreeNode));
        _semanticIdHandler.FillOutTemplate(Arg.Any<ISubmodel>(), Arg.Any<SemanticTreeNode>()).Returns(filledSubmodel);

        var result = await _sut.GetAllSubmodelsAsync(filter, null, null, null, CancellationToken.None);

        Assert.Single(result.Result);
        await _templateService.Received(1).GetFilteredSubmodelTemplateIdAsync(SemanticId, Arg.Any<CancellationToken>());
        await _templateService.Received(1).GetFilteredSubmodelTemplateAsync(SubmodelId1, FilteredTemplateId, Arg.Any<SubmodelQueryOptions?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAllSubmodelsAsync_WhenSemanticIdNotFound_ThrowsSubmodelNotFoundException()
    {
        const string SemanticId = "https://example.com/unknown-semantic-id";
        var filter = new SubmodelSearchFilter { SemanticId = SemanticId };

        _pluginDataHandler
            .GetDataForShellsByAssetIdsAsync(Arg.Any<IReadOnlyList<PluginManifest>>(), Arg.Any<ShellSearchFilter?>(), Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new ShellDescriptorsMetaData { ShellDescriptors = [new ShellDescriptorMetaData { Id = "https://example.com/shells/001" }] });

        // GetFilteredSubmodelTemplateIdAsync returns null — semantic ID not found in any template
        _templateService
            .GetFilteredSubmodelTemplateIdAsync(SemanticId, Arg.Any<CancellationToken>())
            .Returns((string?)null);

        await Assert.ThrowsAsync<SubmodelNotFoundException>(() =>
            _sut.GetAllSubmodelsAsync(filter, null, null, null, CancellationToken.None));
    }

    [Fact]
    public async Task GetAllSubmodelsAsync_SkipsShellsWithNullOrEmptyId()
    {
        const string ValidShellId = "https://example.com/shells/valid";
        const string SubmodelId1 = "https://example.com/submodels/Nameplate";
        var submodelRef = new Reference(ReferenceTypes.ModelReference, [new Key(KeyTypes.Submodel, SubmodelId1)]);
        var filledSubmodel = TestData.CreateFilledSubmodel();

        _pluginDataHandler
            .GetDataForShellsByAssetIdsAsync(Arg.Any<IReadOnlyList<PluginManifest>>(), Arg.Any<ShellSearchFilter?>(), Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new ShellDescriptorsMetaData
            {
                ShellDescriptors =
                [
                    new ShellDescriptorMetaData { Id = null },        // should be skipped
                    new ShellDescriptorMetaData { Id = "   " },       // should be skipped
                    new ShellDescriptorMetaData { Id = ValidShellId } // should be included
                ]
            });

        _aasRepositoryTemplateService
            .GetSubmodelRefByIdAsync(ValidShellId, Arg.Any<CancellationToken>())
            .Returns([submodelRef]);

        _templateService
            .GetFilteredSubmodelTemplateAsync(SubmodelId1, (string?)null, Arg.Any<SubmodelQueryOptions?>(), Arg.Any<CancellationToken>())
            .Returns(TestData.CreateSubmodel());

        _semanticIdHandler.Extract(Arg.Any<ISubmodel>()).Returns(CreateSubmodelTreeNode(""));
        _pluginDataHandler
            .TryGetValuesAsync(Arg.Any<IReadOnlyList<PluginManifest>>(), Arg.Any<SemanticTreeNode>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateSubmodelTreeNode("") as SemanticTreeNode));
        _semanticIdHandler.FillOutTemplate(Arg.Any<ISubmodel>(), Arg.Any<SemanticTreeNode>()).Returns(filledSubmodel);

        var result = await _sut.GetAllSubmodelsAsync(null, null, null, null, CancellationToken.None);

        Assert.Single(result.Result);
        // Only the valid shell should have been queried for submodel refs
        await _aasRepositoryTemplateService.Received(1).GetSubmodelRefByIdAsync(ValidShellId, Arg.Any<CancellationToken>());
        await _aasRepositoryTemplateService.DidNotReceive().GetSubmodelRefByIdAsync(null!, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAllSubmodelsAsync_WhenLimitReachedMidProduct_ReturnsResumeCursor()
    {
        const string AasId = "https://example.com/shells/001";
        const string SubmodelId1 = "https://example.com/submodels/SM-1";
        const string SubmodelId2 = "https://example.com/submodels/SM-2";
        const string SubmodelId3 = "https://example.com/submodels/SM-3";

        _pluginDataHandler
            .GetDataForShellsByAssetIdsAsync(Arg.Any<IReadOnlyList<PluginManifest>>(), Arg.Any<ShellSearchFilter?>(), Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new ShellDescriptorsMetaData { ShellDescriptors = [new ShellDescriptorMetaData { Id = AasId }] });

        _aasRepositoryTemplateService
            .GetSubmodelRefByIdAsync(AasId, Arg.Any<CancellationToken>())
            .Returns([SubmodelReference(SubmodelId1), SubmodelReference(SubmodelId2), SubmodelReference(SubmodelId3)]);

        ArrangeSubmodelBuildPipeline();

        var result = await _sut.GetAllSubmodelsAsync(null, null, limit: 2, cursor: null, CancellationToken.None);

        Assert.Equal(2, result.Result.Count);

        var nextCursor = SubmodelPageCursor.TryDecode(result.PagingMetaData?.Cursor);
        Assert.NotNull(nextCursor);
        Assert.Null(nextCursor!.PluginPageCursor);
        Assert.Equal(AasId, nextCursor.CurrentAasId);
        Assert.Equal(SubmodelId2, nextCursor.LastSubmodelId);
    }

    [Fact]
    public async Task GetAllSubmodelsAsync_WithResumeCursor_SkipsAlreadyDeliveredSubmodels()
    {
        const string AasId = "https://example.com/shells/001";
        const string SubmodelId1 = "https://example.com/submodels/SM-1";
        const string SubmodelId2 = "https://example.com/submodels/SM-2";
        const string SubmodelId3 = "https://example.com/submodels/SM-3";

        var resumeCursor = new SubmodelPageCursor(null, AasId, SubmodelId2).Encode();

        _pluginDataHandler
            .GetDataForShellsByAssetIdsAsync(Arg.Any<IReadOnlyList<PluginManifest>>(), Arg.Any<ShellSearchFilter?>(), Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new ShellDescriptorsMetaData { ShellDescriptors = [new ShellDescriptorMetaData { Id = AasId }] });

        _aasRepositoryTemplateService
            .GetSubmodelRefByIdAsync(AasId, Arg.Any<CancellationToken>())
            .Returns([SubmodelReference(SubmodelId1), SubmodelReference(SubmodelId2), SubmodelReference(SubmodelId3)]);

        ArrangeSubmodelBuildPipeline();

        var result = await _sut.GetAllSubmodelsAsync(null, null, limit: 100, cursor: resumeCursor, CancellationToken.None);

        // Only the submodel after the resume point should remain, and the data is exhausted.
        Assert.Single(result.Result);
        Assert.Null(result.PagingMetaData?.Cursor);
        await _templateService.Received(1).GetFilteredSubmodelTemplateAsync(SubmodelId3, (string?)null, Arg.Any<SubmodelQueryOptions?>(), Arg.Any<CancellationToken>());
        await _templateService.DidNotReceive().GetFilteredSubmodelTemplateAsync(SubmodelId1, (string?)null, Arg.Any<SubmodelQueryOptions?>(), Arg.Any<CancellationToken>());
        await _templateService.DidNotReceive().GetFilteredSubmodelTemplateAsync(SubmodelId2, (string?)null, Arg.Any<SubmodelQueryOptions?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAllSubmodelsAsync_WhenLimitSpansPluginPages_AdvancesPluginCursor()
    {
        const string AasId1 = "https://example.com/shells/001";
        const string AasId2 = "https://example.com/shells/002";
        const string NextPluginCursor = "PLUGIN-PAGE-2";
        const string SubmodelId1 = "https://example.com/submodels/SM-1";
        const string SubmodelId2 = "https://example.com/submodels/SM-2";
        const string SubmodelId3 = "https://example.com/submodels/SM-3";
        const string SubmodelId4 = "https://example.com/submodels/SM-4";

        // First plugin page (null cursor) points to the next page.
        _pluginDataHandler
            .GetDataForShellsByAssetIdsAsync(Arg.Any<IReadOnlyList<PluginManifest>>(), Arg.Any<ShellSearchFilter?>(), Arg.Any<int?>(), Arg.Is<string?>(c => c == null), Arg.Any<CancellationToken>())
            .Returns(new ShellDescriptorsMetaData
            {
                ShellDescriptors = [new ShellDescriptorMetaData { Id = AasId1 }],
                PagingMetaData = new PagingMetaData { Cursor = NextPluginCursor }
            });

        // Second plugin page (advanced cursor) ends the data.
        _pluginDataHandler
            .GetDataForShellsByAssetIdsAsync(Arg.Any<IReadOnlyList<PluginManifest>>(), Arg.Any<ShellSearchFilter?>(), Arg.Any<int?>(), Arg.Is<string?>(c => c == NextPluginCursor), Arg.Any<CancellationToken>())
            .Returns(new ShellDescriptorsMetaData { ShellDescriptors = [new ShellDescriptorMetaData { Id = AasId2 }] });

        _aasRepositoryTemplateService
            .GetSubmodelRefByIdAsync(AasId1, Arg.Any<CancellationToken>())
            .Returns([SubmodelReference(SubmodelId1), SubmodelReference(SubmodelId2)]);
        _aasRepositoryTemplateService
            .GetSubmodelRefByIdAsync(AasId2, Arg.Any<CancellationToken>())
            .Returns([SubmodelReference(SubmodelId3), SubmodelReference(SubmodelId4)]);

        ArrangeSubmodelBuildPipeline();

        var result = await _sut.GetAllSubmodelsAsync(null, null, limit: 3, cursor: null, CancellationToken.None);

        Assert.Equal(3, result.Result.Count);

        var nextCursor = SubmodelPageCursor.TryDecode(result.PagingMetaData?.Cursor);
        Assert.NotNull(nextCursor);
        Assert.Equal(NextPluginCursor, nextCursor!.PluginPageCursor);
        Assert.Equal(AasId2, nextCursor.CurrentAasId);
        Assert.Equal(SubmodelId3, nextCursor.LastSubmodelId);
    }

    [Fact]
    public async Task GetAllSubmodelElementsAsync_ReturnsAllElements_WhenSubmodelExists()
    {
        var filledSubmodel = TestData.CreateFilledSubmodel();

        _templateService
            .GetFilteredSubmodelTemplateAsync(SubmodelId, (string?)null, Arg.Any<SubmodelQueryOptions?>(), Arg.Any<CancellationToken>())
            .Returns(TestData.CreateSubmodel());

        _semanticIdHandler.Extract(Arg.Any<ISubmodel>()).Returns(CreateSubmodelTreeNode(""));
        _pluginDataHandler
            .TryGetValuesAsync(Arg.Any<IReadOnlyList<PluginManifest>>(), Arg.Any<SemanticTreeNode>(), SubmodelId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateSubmodelTreeNode("") as SemanticTreeNode));
        _semanticIdHandler.FillOutTemplate(Arg.Any<ISubmodel>(), Arg.Any<SemanticTreeNode>()).Returns(filledSubmodel);

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
            .GetFilteredSubmodelTemplateAsync(SubmodelId, (string?)null, Arg.Any<SubmodelQueryOptions?>(), Arg.Any<CancellationToken>())
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

        _templateService
            .GetFilteredSubmodelTemplateAsync(SubmodelId, (string?)null, Arg.Any<SubmodelQueryOptions?>(), Arg.Any<CancellationToken>())
            .Returns(TestData.CreateSubmodel());

        _semanticIdHandler.Extract(Arg.Any<ISubmodel>()).Returns(CreateSubmodelTreeNode(""));
        _pluginDataHandler
            .TryGetValuesAsync(Arg.Any<IReadOnlyList<PluginManifest>>(), Arg.Any<SemanticTreeNode>(), SubmodelId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateSubmodelTreeNode("") as SemanticTreeNode));
        _semanticIdHandler.FillOutTemplate(Arg.Any<ISubmodel>(), Arg.Any<SemanticTreeNode>()).Returns(filledSubmodel);

        var result = await _sut.GetAllSubmodelElementsAsync(SubmodelId, null, limit: 2, cursor: null, CancellationToken.None);

        Assert.Equal(2, result.Result.Count);
        Assert.NotNull(result.PagingMetaData?.Cursor);
    }

    [Fact]
    public async Task GetAllSubmodelElementsAsync_WhenSubmodelNotFound_ThrowsSubmodelNotFoundException()
    {
        _templateService
            .GetFilteredSubmodelTemplateAsync(SubmodelId, (string?)null, Arg.Any<SubmodelQueryOptions?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ResourceNotFoundException());

        await Assert.ThrowsAsync<SubmodelNotFoundException>(() =>
            _sut.GetAllSubmodelElementsAsync(SubmodelId, null, null, null, CancellationToken.None));
    }

    [Fact]
    public async Task GetAllSubmodelElementsAsync_WhenResponseParsingFails_ThrowsInternalDataProcessingException()
    {
        _templateService
            .GetFilteredSubmodelTemplateAsync(SubmodelId, (string?)null, Arg.Any<SubmodelQueryOptions?>(), Arg.Any<CancellationToken>())
            .Returns(TestData.CreateSubmodel());

        _semanticIdHandler.Extract(Arg.Any<ISubmodel>()).Returns(CreateSubmodelTreeNode(""));
        _pluginDataHandler
            .TryGetValuesAsync(Arg.Any<IReadOnlyList<PluginManifest>>(), Arg.Any<SemanticTreeNode>(), SubmodelId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new ResponseParsingException());

        await Assert.ThrowsAsync<InternalDataProcessingException>(() =>
            _sut.GetAllSubmodelElementsAsync(SubmodelId, null, null, null, CancellationToken.None));
    }
}
