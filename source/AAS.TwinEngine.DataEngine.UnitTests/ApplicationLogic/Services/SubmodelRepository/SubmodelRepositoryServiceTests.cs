using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository;
using AAS.TwinEngine.DataEngine.DomainModel.Plugin;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

using AasCore.Aas3_0;

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
    private readonly SubmodelRepositoryService _sut;

    private const string SubmodelId = "NameplateSubmodel";
    private const string IdShortPath = "ContactInformation";

    public SubmodelRepositoryServiceTests()
    {
        _sut = new SubmodelRepositoryService(
            _templateService,
            _semanticIdHandler,
            _pluginDataHandler,
            _pluginManifestConflictHandler);
    }

    [Fact]
    public async Task GetSubmodelAsync_ReturnsFilledSubmodel()
    {
        var semanticId = TestData.CreateSubmodelTreeNode();
        var values = TestData.CreateSubmodelTreeNode();
        var expected = TestData.CreateFilledSubmodel();

        _templateService.GetSubmodelTemplateAsync(SubmodelId, Arg.Any<CancellationToken>())
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

        var result = await _sut.GetSubmodelAsync(SubmodelId, CancellationToken.None);

        Assert.Equal(expected, result);
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
            .GetSubmodelTemplateAsync(SubmodelId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new ResourceNotFoundException());

        await Assert.ThrowsAsync<SubmodelNotFoundException>(() =>
            _sut.GetSubmodelAsync(SubmodelId, CancellationToken.None));
    }

    [Fact]
    public async Task GetSubmodelAsync_WhenResponseParsingFails_ThrowsInternalDataProcessingException()
    {
        _pluginDataHandler
            .TryGetValuesAsync(Arg.Any<IReadOnlyList<PluginManifest>>(), Arg.Any<SemanticTreeNode>(), SubmodelId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new ResponseParsingException());

        await Assert.ThrowsAsync<InternalDataProcessingException>(() =>
            _sut.GetSubmodelAsync(SubmodelId, CancellationToken.None));
    }

    [Fact]
    public async Task GetSubmodelAsync_WhenRequestTimesOut_ThrowsPluginNotAvailableException()
    {
        _pluginDataHandler
            .TryGetValuesAsync(Arg.Any<IReadOnlyList<PluginManifest>>(), Arg.Any<SemanticTreeNode>(), SubmodelId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new RequestTimeoutException());

        await Assert.ThrowsAsync<PluginNotAvailableException>(() =>
            _sut.GetSubmodelAsync(SubmodelId, CancellationToken.None));
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
}
