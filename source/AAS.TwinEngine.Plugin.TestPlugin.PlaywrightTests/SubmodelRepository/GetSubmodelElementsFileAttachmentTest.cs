namespace AAS.TwinEngine.Plugin.TestPlugin.PlaywrightTests.SubmodelRepository;

/// <summary>
/// Tests for GET /submodels/{submodelIdentifier}/submodel-elements/{idShortPath}/attachment
/// </summary>
public class GetSubmodelElementsFileAttachmentTest : ApiTestBase
{
    private const string FileElementIdShortPath = "Documents%255B0%255D.DocumentVersions%255B1%255D.PreviewFile";

    [Fact]
    public async Task GetSubmodelElementFileAttachment_HandoverDocumentation_ShouldReturnBinary_WithContentTypeFromFileElement()
    {
        // Arrange
        var encodedPath = EncodeIdShortPathForRoute(FileElementIdShortPath);
        var attachmentUrl = $"/submodels/{SubmodelIdentifierHandoverDocumentation}/submodel-elements/{encodedPath}/attachment";

        // Act
        var attachmentResponse = await ApiContext.GetAsync(attachmentUrl);

        // Assert
        Assert.Equal(200, attachmentResponse.Status);
    }

    [Fact]
    public async Task GetSubmodelElementFileAttachment_WhenElementIsNotFile_ShouldReturn400()
    {
        // Arrange
        var url = $"/submodels/{SubmodelIdentifierContact}/submodel-elements/ContactInformation1/attachment";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        Assert.Equal(400, response.Status);
    }

    [Fact]
    public async Task GetSubmodelElementFileAttachment_WhenElementPathDoesNotExist_ShouldReturn404()
    {
        // Arrange
        var missingPath = EncodeIdShortPathForRoute("Documents[999].DocumentVersions[0].DigitalFiles[0]");
        var url = $"/submodels/{SubmodelIdentifierHandoverDocumentation}/submodel-elements/{missingPath}/attachment";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        Assert.Equal(404, response.Status);
    }

    [Fact]
    public async Task GetSubmodelElementFileAttachment_WhenSubmodelDoesNotExist_ShouldReturn404()
    {
        // Arrange
        var missingSubmodel = Base64EncodeUrl("https://mm-software.com/submodel/000-001/DoesNotExist");
        var path = EncodeIdShortPathForRoute(FileElementIdShortPath);
        var url = $"/submodels/{missingSubmodel}/submodel-elements/{path}/attachment";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        Assert.Equal(404, response.Status);
    }

    private static string EncodeIdShortPathForRoute(string idShortPath) => Uri.EscapeDataString(Uri.EscapeDataString(idShortPath));
}
