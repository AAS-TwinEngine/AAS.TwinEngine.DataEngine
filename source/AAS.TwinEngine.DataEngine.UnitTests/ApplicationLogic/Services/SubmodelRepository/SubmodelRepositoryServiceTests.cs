using System.Text;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasRepository;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Shared.Providers;
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
    private readonly IFileContentProvider _fileAttachmentStreamProvider = Substitute.For<IFileContentProvider>();
    private readonly ILogger<SubmodelRepositoryService> _logger = Substitute.For<ILogger<SubmodelRepositoryService>>();
    private readonly IOptions<TemplateManagementConfig> _templateManagementOptions;
    private readonly SubmodelRepositoryService _sut;

    private const string SubmodelId = "NameplateSubmodel";
    private const string IdShortPath = "ContactInformation";

    public SubmodelRepositoryServiceTests()
    {
        _templateManagementOptions = Options.Create(new TemplateManagementConfig
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
            _templateManagementOptions,
            _semanticIdHandler,
            _pluginDataHandler,
            _pluginManifestConflictHandler,
            _fileAttachmentStreamProvider,
            Options.Create(new GeneralConfig { MaxFileAttachmentSizeBytes = 30 * 1024 * 1024 }));
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
            .GetFilteredSubmodelTemplateAsync(SubmodelId, Arg.Any<string>(), queryOptions, Arg.Any<CancellationToken>())
            .Returns(template);
        _semanticIdHandler.Extract(Arg.Any<ISubmodel>()).Returns(CreateSubmodelTreeNode(""));
        _pluginDataHandler
            .TryGetValuesAsync(Arg.Any<IReadOnlyList<PluginManifest>>(), Arg.Any<SemanticTreeNode>(), SubmodelId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateSubmodelTreeNode("") as SemanticTreeNode));
        _semanticIdHandler.FillOutTemplate(Arg.Any<ISubmodel>(), Arg.Any<SemanticTreeNode>()).Returns(TestData.CreateFilledSubmodel());

        await _sut.GetSubmodelAsync(SubmodelId, queryOptions, CancellationToken.None);

        await _templateService.Received(1)
            .GetFilteredSubmodelTemplateAsync(SubmodelId, Arg.Any<string>(), queryOptions, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSubmodelAsync_WhenTemplateReturnsNull_ThrowsSubmodelNotFoundException()
    {
        _templateService
            .GetFilteredSubmodelTemplateAsync(SubmodelId, Arg.Any<string>(), Arg.Any<SubmodelQueryOptions?>(), Arg.Any<CancellationToken>())
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
            .GetFilteredSubmodelTemplateAsync(SubmodelId, Arg.Any<string>(), Arg.Any<SubmodelQueryOptions?>(), Arg.Any<CancellationToken>())
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
    public async Task GetSubmodelElementAsync_WhenResourceNotFound_ThrowsSubmodelElementNotFoundException()
    {
        _templateService
            .GetSubmodelTemplateAsync(SubmodelId, IdShortPath, Arg.Any<CancellationToken>())
            .ThrowsAsync(new ResourceNotFoundException());

        await Assert.ThrowsAsync<SubmodelElementNotFoundException>(() =>
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
            .GetFilteredSubmodelTemplateAsync(SubmodelId1, Arg.Any<string>(), Arg.Any<SubmodelQueryOptions?>(), Arg.Any<CancellationToken>())
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
            .GetFilteredSubmodelTemplateAsync(SubmodelId1, Arg.Any<string>(), Arg.Any<SubmodelQueryOptions?>(), Arg.Any<CancellationToken>())
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
    public async Task GetAllSubmodelsAsync_SkipsShellsWithEmptyOrWhitespaceId()
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
                    new ShellDescriptorMetaData { Id = string.Empty }, // should be skipped
                    new ShellDescriptorMetaData { Id = "   " },       // should be skipped
                    new ShellDescriptorMetaData { Id = ValidShellId } // should be included
                ]
            });

        _aasRepositoryTemplateService
            .GetSubmodelRefByIdAsync(ValidShellId, Arg.Any<CancellationToken>())
            .Returns([submodelRef]);

        _templateService
            .GetFilteredSubmodelTemplateAsync(SubmodelId1, Arg.Any<string>(), Arg.Any<SubmodelQueryOptions?>(), Arg.Any<CancellationToken>())
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
    public async Task GetAllSubmodelsAsync_WhenLimitReachedAtAasBoundary_CursorSubmodelIdIsLastDeliveredAndAasIdIsConsumedAas()
    {
        // AAS-1 has 3 submodels, AAS-2 has 2 submodels. limit=5 hits the exact end of AAS-2.
        // AasId in the cursor must be AAS-2 (the consumed AAS), not AAS-1 (the entry cursor),
        // so Plugin(AAS-2 exclusive) on resume skips directly to the next AAS.
        const string Shell1Id = "https://example.com/shells/aas-1";
        const string Shell2Id = "https://example.com/shells/aas-2";
        const string Sm1 = "https://example.com/submodels/sm-1";
        const string Sm2 = "https://example.com/submodels/sm-2";
        const string Sm3 = "https://example.com/submodels/sm-3";
        const string Sm4 = "https://example.com/submodels/sm-4";
        const string Sm5 = "https://example.com/submodels/sm-5";

        _pluginDataHandler
            .GetDataForShellsByAssetIdsAsync(Arg.Any<IReadOnlyList<PluginManifest>>(), Arg.Any<ShellSearchFilter?>(), Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new ShellDescriptorsMetaData
            {
                ShellDescriptors = [new ShellDescriptorMetaData { Id = Shell1Id }, new ShellDescriptorMetaData { Id = Shell2Id }]
            });

        _aasRepositoryTemplateService.GetSubmodelRefByIdAsync(Shell1Id, Arg.Any<CancellationToken>())
            .Returns([
                new Reference(ReferenceTypes.ModelReference, [new Key(KeyTypes.Submodel, Sm1)]),
                new Reference(ReferenceTypes.ModelReference, [new Key(KeyTypes.Submodel, Sm2)]),
                new Reference(ReferenceTypes.ModelReference, [new Key(KeyTypes.Submodel, Sm3)])
            ]);

        _aasRepositoryTemplateService.GetSubmodelRefByIdAsync(Shell2Id, Arg.Any<CancellationToken>())
            .Returns([
                new Reference(ReferenceTypes.ModelReference, [new Key(KeyTypes.Submodel, Sm4)]),
                new Reference(ReferenceTypes.ModelReference, [new Key(KeyTypes.Submodel, Sm5)])
            ]);

        _templateService
            .GetFilteredSubmodelTemplateAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<SubmodelQueryOptions?>(), Arg.Any<CancellationToken>())
            .Returns(TestData.CreateSubmodel());
        _semanticIdHandler.Extract(Arg.Any<ISubmodel>()).Returns(CreateSubmodelTreeNode(""));
        _pluginDataHandler
            .TryGetValuesAsync(Arg.Any<IReadOnlyList<PluginManifest>>(), Arg.Any<SemanticTreeNode>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateSubmodelTreeNode("") as SemanticTreeNode));
        _semanticIdHandler.FillOutTemplate(Arg.Any<ISubmodel>(), Arg.Any<SemanticTreeNode>())
            .Returns(TestData.CreateFilledSubmodel());

        var result = await _sut.GetAllSubmodelsAsync(null, null, limit: 5, null, CancellationToken.None);

        Assert.Equal(5, result.Result.Count);
        Assert.NotNull(result.PagingMetaData?.Cursor);

        var decoded = SubmodelPaginationCursor.Decode(result.PagingMetaData!.Cursor!);
        Assert.Equal(Sm5, decoded!.SubmodelId);  // last delivered submodel
        Assert.Equal(Shell2Id, decoded.AasId);   // the consumed AAS, not the entry cursor
    }

    [Fact]
    public async Task GetAllSubmodelsAsync_WhenLimitReachedAtFirstAasBoundary_CursorAasIdIsConsumedAas()
    {
        // AAS-1 has exactly 2 submodels, limit=2. Limit hit at the end of AAS-1.
        // AasId must be AAS-1 (the consumed AAS) so the next request starts from after AAS-1.
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

        _aasRepositoryTemplateService.GetSubmodelRefByIdAsync(Shell1Id, Arg.Any<CancellationToken>())
            .Returns([
                new Reference(ReferenceTypes.ModelReference, [new Key(KeyTypes.Submodel, Sm1)]),
                new Reference(ReferenceTypes.ModelReference, [new Key(KeyTypes.Submodel, Sm2)])
            ]);

        _templateService
            .GetFilteredSubmodelTemplateAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<SubmodelQueryOptions?>(), Arg.Any<CancellationToken>())
            .Returns(TestData.CreateSubmodel());
        _semanticIdHandler.Extract(Arg.Any<ISubmodel>()).Returns(CreateSubmodelTreeNode(""));
        _pluginDataHandler
            .TryGetValuesAsync(Arg.Any<IReadOnlyList<PluginManifest>>(), Arg.Any<SemanticTreeNode>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateSubmodelTreeNode("") as SemanticTreeNode));
        _semanticIdHandler.FillOutTemplate(Arg.Any<ISubmodel>(), Arg.Any<SemanticTreeNode>())
            .Returns(TestData.CreateFilledSubmodel());

        var result = await _sut.GetAllSubmodelsAsync(null, null, limit: 2, null, CancellationToken.None);

        Assert.Equal(2, result.Result.Count);
        Assert.NotNull(result.PagingMetaData?.Cursor);

        var decoded = SubmodelPaginationCursor.Decode(result.PagingMetaData!.Cursor!);
        Assert.Equal(Sm2, decoded!.SubmodelId);   // last delivered submodel
        Assert.Equal(Shell1Id, decoded.AasId);     // consumed AAS — next request starts after AAS-1
    }

    [Fact]
    public async Task GetAllSubmodelElementsAsync_ReturnsAllElements_WhenSubmodelExists()
    {
        var filledSubmodel = TestData.CreateFilledSubmodel();

        _templateService
            .GetFilteredSubmodelTemplateAsync(SubmodelId, Arg.Any<string>(), Arg.Any<SubmodelQueryOptions?>(), Arg.Any<CancellationToken>())
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
            .GetFilteredSubmodelTemplateAsync(SubmodelId, Arg.Any<string>(), Arg.Any<SubmodelQueryOptions?>(), Arg.Any<CancellationToken>())
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
            .GetFilteredSubmodelTemplateAsync(SubmodelId, Arg.Any<string>(), Arg.Any<SubmodelQueryOptions?>(), Arg.Any<CancellationToken>())
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
    public async Task GetAllSubmodelElementsAsync_WhenSubmodelNotFound_ThrowsSubmodelElementNotFoundException()
    {
        _templateService
            .GetFilteredSubmodelTemplateAsync(SubmodelId, (string?)null, Arg.Any<SubmodelQueryOptions?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ResourceNotFoundException());

        await Assert.ThrowsAsync<SubmodelElementNotFoundException>(() =>
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

    private void ArrangeAttachmentElement(string idShortPath, ISubmodelElement element)
    {
        var template = TestData.CreateSubmodelWithElement(element, idShortPath);
        _templateService.GetSubmodelTemplateAsync(SubmodelId, idShortPath, Arg.Any<CancellationToken>()).Returns(template);
        _semanticIdHandler.Extract(Arg.Any<ISubmodel>()).Returns(CreateSubmodelTreeNode(""));
        _pluginDataHandler.TryGetValuesAsync(Arg.Any<IReadOnlyList<PluginManifest>>(), Arg.Any<SemanticTreeNode>(), SubmodelId, Arg.Any<CancellationToken>()).Returns(CreateSubmodelTreeNode(""));
        _semanticIdHandler.FillOutTemplate(Arg.Any<ISubmodel>(), Arg.Any<SemanticTreeNode>()).Returns(template);
        _semanticIdHandler.Extract(Arg.Any<ISubmodel>(), idShortPath).Returns(element);
    }

    [Fact]
    public async Task GetFileAttachmentAsync_WhenElementIsFileWithHttpUrl_ReturnsStreamWithCorrectMetadata()
    {
        const string IdShortPath = "Documents.ProductImage";
        const string FileUrl = "https://fake-plugin.local/files/product.png";
        const string FileContent = "binary-file-data";

        var fileElement = new AasCore.Aas3_1.File(contentType: "image/png") { Value = FileUrl, IdShort = "ProductImage" };
        ArrangeAttachmentElement(IdShortPath, fileElement);

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(FileContent));
        var fileContentResponse = new FileContentResponse(stream);
        _fileAttachmentStreamProvider.GetFileContentAsync(FileUrl, Arg.Any<CancellationToken>()).Returns(fileContentResponse);

        var result = await _sut.GetFileAttachmentAsync(SubmodelId, IdShortPath, CancellationToken.None);
        await using (result.Content)
        {
            var body = await new StreamReader(result.Content).ReadToEndAsync();
            Assert.Equal(FileContent, body);
            Assert.Equal("product.png", result.FileName);
            Assert.Contains("image/png", result.ContentType);
        }

        await _fileAttachmentStreamProvider.Received(1).GetFileContentAsync(FileUrl, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetFileAttachmentAsync_WhenElementIsNotFile_ThrowsInvalidUserInputException()
    {
        const string IdShortPath = "ManufacturerName";
        var property = new Property(DataTypeDefXsd.String) { IdShort = "ManufacturerName" };
        ArrangeAttachmentElement(IdShortPath, property);
        var ex = await Assert.ThrowsAsync<InvalidUserInputException>(() =>
            _sut.GetFileAttachmentAsync(SubmodelId, IdShortPath, CancellationToken.None));
        Assert.Equal("Invalid User Input.", ex.Message);
    }

    [Fact]
    public async Task GetFileAttachmentAsync_WhenSubmodelNotFound_ThrowsSubmodelElementNotFoundException()
    {
        const string IdShortPath = "Documents.ProductImage";

        _templateService
            .GetSubmodelTemplateAsync(SubmodelId, IdShortPath, Arg.Any<CancellationToken>())
            .ThrowsAsync(new ResourceNotFoundException());

        await Assert.ThrowsAsync<SubmodelElementNotFoundException>(() =>
            _sut.GetFileAttachmentAsync(SubmodelId, IdShortPath, CancellationToken.None));
    }

    [Fact]
    public async Task GetFileAttachmentAsync_WhenFileValueIsEmpty_ThrowsSubmodelElementNotFoundException()
    {
        const string IdShortPath = "Documents.ProductImage";
        var fileElement = new AasCore.Aas3_1.File(contentType: "image/png") { Value = "", IdShort = "ProductImage" };
        ArrangeAttachmentElement(IdShortPath, fileElement);

        await Assert.ThrowsAsync<SubmodelElementNotFoundException>(() =>
            _sut.GetFileAttachmentAsync(SubmodelId, IdShortPath, CancellationToken.None));
    }

    [Fact]
    public async Task GetFileAttachmentAsync_WhenFileUrlIsNotHttpOrHttps_ThrowsInternalDataProcessingException()
    {
        const string IdShortPath = "Documents.ProductImage";
        const string FileUrl = "ftp://fake-plugin.local/files/product.png";

        var fileElement = new AasCore.Aas3_1.File(contentType: "image/png") { Value = FileUrl, IdShort = "ProductImage" };
        ArrangeAttachmentElement(IdShortPath, fileElement);

        await Assert.ThrowsAsync<InternalDataProcessingException>(() =>
            _sut.GetFileAttachmentAsync(SubmodelId, IdShortPath, CancellationToken.None));
    }
}
