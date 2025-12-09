using System.Text;
using System.Text.Json.Nodes;

using AAS.TwinEngine.DataEngine.Api.SubmodelRepository;
using AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Handler;
using AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Requests;

using AasCore.Aas3_0;

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

        var result = await _sut.GetSubmodelAsync(encodedId, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var json = Assert.IsType<JsonObject>(okResult.Value);
        Assert.Equal(expectedJson.ToJsonString(), json.ToJsonString());
    }

    [Fact]
    public async Task GetSubmodelElementAsync_ReturnsOkResult_WithJsonObject()
    {
        var encodedId = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(_submodelId));
        var expectedJson = Jsonization.Serialize.ToJsonObject(_expectedElement);
        _handler.GetSubmodelElement(Arg.Any<GetSubmodelElementRequest>(), Arg.Any<CancellationToken>())
        .Returns(_expectedElement);

        var result = await _sut.GetSubmodelElementAsync(encodedId, _idShortPath, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var json = Assert.IsType<JsonObject>(okResult.Value);
        Assert.Equal(expectedJson.ToJsonString(), json.ToJsonString());
    }
}
