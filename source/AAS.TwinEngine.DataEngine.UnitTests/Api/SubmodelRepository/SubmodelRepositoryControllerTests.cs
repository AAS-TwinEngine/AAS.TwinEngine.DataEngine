using System.Text;
using System.Text.Json.Nodes;

using AAS.TwinEngine.DataEngine.Api.SubmodelRepository;
using AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Handler;
using AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Requests;
using AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Responses;
using AAS.TwinEngine.DataEngine.Api.Shared.Results;
using AAS.TwinEngine.DataEngine.DomainModel.Shared;

using AasCore.Aas3_1;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

using NSubstitute;

namespace AAS.TwinEngine.DataEngine.UnitTests.Api.SubmodelRepository;

public class SubmodelRepositoryControllerTests
{
    private readonly ISubmodelRepositoryHandler _handler;
    private readonly SubmodelRepositoryController _sut;
    private readonly Submodel _expectedSubmodel;
    private readonly Property _expectedElement;
    private readonly string _submodelId;
    private readonly string _idShortPath;

    public SubmodelRepositoryControllerTests()
    {
        var logger = Substitute.For<ILogger<SubmodelRepositoryController>>();
        _expectedSubmodel = new Submodel(
                                         id: "http://mm-software.com/idta/digital-nameplate",
                                         idShort: "DigitalNameplate",
                                         semanticId: new Reference(
                                                                   ReferenceTypes.ExternalReference,
                                                                   [
                                                                       new Key(KeyTypes.Submodel, "http://mm-software.com/idta/digital-nameplate/NameplateSubmodel")
                                                                   ]
                                                                  ));
        _submodelId = "NameplateSubmodel";
        _idShortPath = "ManufacturerName";
        _expectedElement = new Property(
               idShort: "ModelType",
               valueType: DataTypeDefXsd.String,
               value: "",
               semanticId: new Reference(
                   ReferenceTypes.ExternalReference,
                   [
                       new Key(KeyTypes.Property, "http://mm-software.com/idta/digital-nameplate")
                   ]
               ));
        _handler = Substitute.For<ISubmodelRepositoryHandler>();
        _sut = new SubmodelRepositoryController(logger, _handler);
    }

    [Fact]
    public async Task GetSubmodelAsync_ReturnsOkResult_WithJsonObject()
    {
        var encodedId = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(_submodelId));
        var expectedJson = Jsonization.Serialize.ToJsonObject(_expectedSubmodel);
        _handler.GetSubmodel(Arg.Any<GetSubmodelRequest>(), Arg.Any<CancellationToken>())
        .Returns(_expectedSubmodel);

        var result = await _sut.GetSubmodelAsync(encodedId, null, null, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var json = Assert.IsType<JsonObject>(okResult.Value);
        Assert.Equal(expectedJson.ToJsonString(), json.ToJsonString());
    }

    [Fact]
    public async Task GetSubmodelAsync_WithLevelAndExtent_PassesThemToHandler()
    {
        var encodedId = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(_submodelId));
        _handler.GetSubmodel(Arg.Any<GetSubmodelRequest>(), Arg.Any<CancellationToken>())
            .Returns(_expectedSubmodel);

        await _sut.GetSubmodelAsync(encodedId, AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Requests.Level.deep,
            AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Requests.Extent.withBlobValue, CancellationToken.None);

        await _handler.Received(1).GetSubmodel(
            Arg.Is<GetSubmodelRequest>(r => r.level == AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Requests.Level.deep
                                         && r.extent == AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Requests.Extent.withBlobValue),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSubmodelElementAsync_ReturnsOkResult_WithJsonObject()
    {
        var encodedId = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(_submodelId));
        var expectedJson = Jsonization.Serialize.ToJsonObject(_expectedElement);
        _handler.GetSubmodelElement(Arg.Any<GetSubmodelElementRequest>(), Arg.Any<CancellationToken>())
        .Returns(_expectedElement);

        var result = await _sut.GetSubmodelElementAsync(encodedId, _idShortPath, null, null, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var json = Assert.IsType<JsonObject>(okResult.Value);
        Assert.Equal(expectedJson.ToJsonString(), json.ToJsonString());
    }

    [Fact]
    public async Task GetSubmodelElementAsync_WithLevelAndExtent_PassesThemToHandler()
    {
        var encodedId = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(_submodelId));
        _handler.GetSubmodelElement(Arg.Any<GetSubmodelElementRequest>(), Arg.Any<CancellationToken>())
            .Returns(_expectedElement);

        await _sut.GetSubmodelElementAsync(encodedId, _idShortPath, Level.deep, Extent.withBlobValue, CancellationToken.None);

        await _handler.Received(1).GetSubmodelElement(
            Arg.Is<GetSubmodelElementRequest>(r => r.level == Level.deep && r.extent == Extent.withBlobValue),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAllSubmodelsAsync_ReturnsOkResult_WithSubmodelsDto()
    {
        var expectedDto = new SubmodelsDto
        {
            PagingMetaData = new AAS.TwinEngine.DataEngine.Api.Shared.PagingMetaDataDto { Cursor = null },
            Result = []
        };
        _handler.GetAllSubmodels(Arg.Any<GetAllSubmodelsRequest>(), Arg.Any<CancellationToken>())
            .Returns(expectedDto);

        var result = await _sut.GetAllSubmodelsAsync(null, null, null, null, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<SubmodelsDto>(okResult.Value);
        Assert.Empty(dto.Result!);
    }

    [Fact]
    public async Task GetAllSubmodelsAsync_PassesQueryParamsToHandler()
    {
        const string SemanticId = "https://example.com/id";
        const string IdShort = "Nameplate";
        const int Limit = 5;
        var expectedDto = new SubmodelsDto { PagingMetaData = new AAS.TwinEngine.DataEngine.Api.Shared.PagingMetaDataDto(), Result = [] };
        _handler.GetAllSubmodels(Arg.Any<GetAllSubmodelsRequest>(), Arg.Any<CancellationToken>())
            .Returns(expectedDto);
        var request = new GetAllSubmodelsRequest { SemanticId = SemanticId, IdShort = IdShort, Limit = Limit };

        await _sut.GetAllSubmodelsAsync(SemanticId, IdShort, Limit, null, CancellationToken.None);

        await _handler.Received(1).GetAllSubmodels(
            Arg.Is<GetAllSubmodelsRequest>(r => r.SemanticId == SemanticId && r.IdShort == IdShort && r.Limit == Limit),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetFileAttachmentAsync_ReturnsFileContentStreamResult_WhenHandlerCompletes()
    {
        var encodedId = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(_submodelId));
        _handler.GetFileAttachment(Arg.Any<GetSubmodelElementRequest>(), Arg.Any<CancellationToken>())
            .Returns(new FileAttachmentResult(Stream.Null, "application/pdf", "document.pdf", 100 * 1024 * 1024));

        var result = await _sut.GetFileAttachmentAsync(encodedId, _idShortPath, CancellationToken.None);

        Assert.IsType<FileContentStreamResult>(result);
    }

    [Fact]
    public async Task GetFileAttachmentAsync_PassesRouteValuesToHandler()
    {
        var encodedId = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(_submodelId));
        _handler.GetFileAttachment(Arg.Any<GetSubmodelElementRequest>(), Arg.Any<CancellationToken>())
            .Returns(new FileAttachmentResult(Stream.Null, "application/octet-stream", null, 100 * 1024 * 1024));

        await _sut.GetFileAttachmentAsync(encodedId, _idShortPath, CancellationToken.None);

        await _handler.Received(1).GetFileAttachment(
            Arg.Is<GetSubmodelElementRequest>(r => r.SubmodelId == encodedId && r.IdShortPath == _idShortPath),
            Arg.Any<CancellationToken>());
    }
}
