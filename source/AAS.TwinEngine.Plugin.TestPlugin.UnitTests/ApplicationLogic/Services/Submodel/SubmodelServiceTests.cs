using Aas.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Services.Submodel;
using Aas.TwinEngine.Plugin.TestPlugin.DomainModel.Submodel;

using NSubstitute;

namespace Aas.TwinEngine.Plugin.TestPlugin.UnitTests.ApplicationLogic.Services.Submodel;

public class SubmodelServiceTests
{
    private readonly SemanticBranchNode _sampleInputTree;
    private readonly SemanticBranchNode _enrichedTree;
    private readonly ISubmodelProvider _repository;
    private readonly SubmodelService _sut;
    private const string SubmodelId = "ContactInformation";

    public SubmodelServiceTests()
    {
        _repository = Substitute.For<ISubmodelProvider>();
        _sut = new SubmodelService(_repository);
        _sampleInputTree = new SemanticBranchNode("ContactInformation", DataType.Object);
        _sampleInputTree.AddChild(new SemanticLeafNode("Name", DataType.String, ""));
        _enrichedTree = new SemanticBranchNode("ContactInformation", DataType.Object);
        _enrichedTree.AddChild(new SemanticLeafNode("Name", DataType.String, "John"));
    }

    [Fact]
    public async Task GetProductDataAsync_ReturnsEnricSematicTree_OnHappyPath()
    {
        _repository
            .EnrichWithData(_sampleInputTree, SubmodelId)
            .Returns(_enrichedTree);

        var actual = await _sut.GetValuesBySemanticIds(_sampleInputTree, SubmodelId);

        Assert.Equal(actual, _enrichedTree);
        _repository.Received(1).EnrichWithData(_sampleInputTree, SubmodelId);
    }

    [Fact]
    public async Task GetProductDataAsync_ReturnsSameTreeNode_WhenValueNotFound()
    {
        _repository
            .EnrichWithData(_sampleInputTree, SubmodelId)
            .Returns(_sampleInputTree);

        var actual = await _sut.GetValuesBySemanticIds(_sampleInputTree, SubmodelId);

        Assert.Equal(_sampleInputTree, actual);
        _repository.Received(1).EnrichWithData(_sampleInputTree, SubmodelId);
    }
}
