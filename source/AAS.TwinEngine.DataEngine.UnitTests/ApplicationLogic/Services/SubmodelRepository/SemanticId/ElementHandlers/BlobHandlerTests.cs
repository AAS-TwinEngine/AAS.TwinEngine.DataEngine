using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.ElementHandlers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Helpers.Interfaces;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

using AasCore.Aas3_0;

using NSubstitute;

using static Xunit.Assert;

namespace AAS.TwinEngine.DataEngine.UnitTests.ApplicationLogic.Services.SubmodelRepository.SemanticId.ElementHandlers;

public class BlobHandlerTests
{
    private readonly BlobHandler _sut;
    private readonly ISemanticIdResolver _resolver;

    public BlobHandlerTests()
    {
        _resolver = Substitute.For<ISemanticIdResolver>();
        _sut = new BlobHandler(_resolver);
    }

    [Fact]
    public void CanHandle_Blob_ReturnsTrue()
    {
        var blob = new Blob(contentType: "application/octet-stream", idShort: "Test");

        True(_sut.CanHandle(blob));
    }

    [Fact]
    public void CanHandle_NonBlob_ReturnsFalse()
    {
        var property = new Property(idShort: "Test", valueType: DataTypeDefXsd.String);

        False(_sut.CanHandle(property));
    }

    [Fact]
    public void Extract_ReturnsLeafNode()
    {
        var blob = new Blob(contentType: "image/png", idShort: "MyBlob");
        _resolver.ResolveElementSemanticId(blob, "MyBlob").Returns("http://test/blob");
        _resolver.GetValueType(blob).Returns(DataType.String);
        _resolver.GetCardinality(blob).Returns(Cardinality.One);

        var result = _sut.Extract(blob, _ => null);

        var leaf = IsType<SemanticLeafNode>(result);
        Equal("http://test/blob", leaf.SemanticId);
        Equal(DataType.String, leaf.DataType);
    }

    [Fact]
    public void FillOut_WithLeafNode_SetsBase64Value()
    {
        var blob = new Blob(contentType: "image/png", idShort: "MyBlob");
        var base64 = Convert.ToBase64String(new byte[] { 1, 2, 3 });
        var values = new SemanticLeafNode("http://test/blob", base64, DataType.String, Cardinality.One);

        _sut.FillOut(blob, values, (_, _, _) => { });

        NotNull(blob.Value);
        Equal([1, 2, 3], blob.Value);
    }

    [Fact]
    public void FillOut_WithBranchNode_DoesNotModifyValue()
    {
        var originalBytes = new byte[] { 10, 20, 30 };
        var blob = new Blob(contentType: "image/png", idShort: "MyBlob", value: originalBytes);
        var values = new SemanticBranchNode("http://test/blob", Cardinality.One);

        _sut.FillOut(blob, values, (_, _, _) => { });

        Equal(originalBytes, blob.Value);
    }
}
