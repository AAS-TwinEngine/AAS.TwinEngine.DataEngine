using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.ElementHandlers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Helpers.Interfaces;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

using AasCore.Aas3_0;

using NSubstitute;

using static Xunit.Assert;

using File = AasCore.Aas3_0.File;

namespace AAS.TwinEngine.DataEngine.UnitTests.ApplicationLogic.Services.SubmodelRepository.SemanticId.ElementHandlers;

public class FileHandlerTests
{
    private readonly FileHandler _sut;
    private readonly ISemanticIdResolver _resolver;

    public FileHandlerTests()
    {
        _resolver = Substitute.For<ISemanticIdResolver>();
        _sut = new FileHandler(_resolver);
    }

    [Fact]
    public void CanHandle_File_ReturnsTrue()
    {
        var file = new File(contentType: "image/png", idShort: "Test");

        True(_sut.CanHandle(file));
    }

    [Fact]
    public void CanHandle_NonFile_ReturnsFalse()
    {
        var property = new Property(idShort: "Test", valueType: DataTypeDefXsd.String);

        False(_sut.CanHandle(property));
    }

    [Fact]
    public void Extract_ReturnsLeafNode()
    {
        var file = new File(contentType: "image/png", idShort: "Thumbnail");
        _resolver.ResolveElementSemanticId(file, "Thumbnail").Returns("http://test/thumbnail");
        _resolver.GetValueType(file).Returns(DataType.String);
        _resolver.GetCardinality(file).Returns(Cardinality.ZeroToOne);

        var result = _sut.Extract(file, _ => null);

        var leaf = IsType<SemanticLeafNode>(result);
        Equal("http://test/thumbnail", leaf.SemanticId);
        Equal(DataType.String, leaf.DataType);
    }

    [Fact]
    public void FillOut_WithLeafNode_SetsFileValue()
    {
        var file = new File(contentType: "image/png", idShort: "Thumbnail", value: "");
        var values = new SemanticLeafNode("http://test/thumbnail", "https://localhost/image.png", DataType.String, Cardinality.One);

        _sut.FillOut(file, values, (_, _, _) => { });

        Equal("https://localhost/image.png", file.Value);
    }

    [Fact]
    public void FillOut_WithBranchNode_DoesNotModifyValue()
    {
        var file = new File(contentType: "image/png", idShort: "Thumbnail", value: "original");
        var values = new SemanticBranchNode("http://test/thumbnail", Cardinality.One);

        _sut.FillOut(file, values, (_, _, _) => { });

        Equal("original", file.Value);
    }
}
