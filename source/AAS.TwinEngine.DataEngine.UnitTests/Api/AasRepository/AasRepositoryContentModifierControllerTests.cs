using System.Reflection;
using System.Text.Json.Nodes;

using AAS.TwinEngine.DataEngine.Api.AasRepository;
using AAS.TwinEngine.DataEngine.Api.AasRepository.Handler;
using AAS.TwinEngine.DataEngine.Api.AasRepository.Requests;
using AAS.TwinEngine.DataEngine.Api.Shared;
using AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Requests;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using NSubstitute;

using Content = AAS.TwinEngine.DataEngine.Api.Shared.ContentModifiers.Content;

namespace AAS.TwinEngine.DataEngine.UnitTests.Api.AasRepository;

public class AasRepositoryContentModifierControllerTests
{
    private static readonly CancellationToken Ct = CancellationToken.None;

    private readonly IAasRepositoryHandler _handler = Substitute.For<IAasRepositoryHandler>();
    private readonly AasRepositoryController _sut;

    public AasRepositoryContentModifierControllerTests()
        => _sut = new AasRepositoryController(Substitute.For<ILogger<AasRepositoryController>>(), _handler);

    public static TheoryData<string, Func<AasRepositoryController, Task>, Content, object> Actions => new()
    {
        { "/shells/{aas}/submodels/{sm}/$metadata", c => c.GetSubmodelMetadataByAasIdAsync("aas", "sm", Ct, null, null), Content.Metadata, new GetSubmodelByAasRequest("aas", "sm", null, null) },
        { "/shells/{aas}/submodels/{sm}/$value", c => c.GetSubmodelValueOnlyByAasIdAsync("aas", "sm", Ct, Level.core, Extent.withBlobValue), Content.Value, new GetSubmodelByAasRequest("aas", "sm", Level.core, Extent.withBlobValue) },
        { "/shells/{aas}/submodels/{sm}/$reference", c => c.GetSubmodelReferenceByAasIdAsync("aas", "sm", Ct, Level.core), Content.Reference, new GetSubmodelByAasRequest("aas", "sm", Level.core, null) },
        { "/shells/{aas}/submodels/{sm}/$path", c => c.GetSubmodelPathByAasIdAsync("aas", "sm", Ct, Level.core), Content.Path, new GetSubmodelByAasRequest("aas", "sm", Level.core, null) },

        { "/shells/{aas}/submodels/{sm}/submodel-elements/$metadata", c => c.GetAllSubmodelElementsMetadataByAasIdAsync("aas", "sm", "cur", Ct, 7, null, null), Content.Metadata, new GetAllSubmodelElementsByAasRequest("aas", "sm", 7, "cur", null, null) },
        { "/shells/{aas}/submodels/{sm}/submodel-elements/$value", c => c.GetAllSubmodelElementsValueOnlyByAasIdAsync("aas", "sm", "cur", Ct, 7, Level.core, Extent.withBlobValue), Content.Value, new GetAllSubmodelElementsByAasRequest("aas", "sm", 7, "cur", Level.core, Extent.withBlobValue) },
        { "/shells/{aas}/submodels/{sm}/submodel-elements/$reference", c => c.GetAllSubmodelElementsReferenceByAasIdAsync("aas", "sm", "cur", Ct, 7, Level.core), Content.Reference, new GetAllSubmodelElementsByAasRequest("aas", "sm", 7, "cur", Level.core, null) },
        { "/shells/{aas}/submodels/{sm}/submodel-elements/$path", c => c.GetAllSubmodelElementsPathByAasIdAsync("aas", "sm", "cur", Ct, 7, Level.core), Content.Path, new GetAllSubmodelElementsByAasRequest("aas", "sm", 7, "cur", Level.core, null) },

        { "/shells/{aas}/submodels/{sm}/submodel-elements/{path}/$metadata", c => c.GetSubmodelElementMetadataByAasIdAsync("aas", "sm", "path", Ct, null, null), Content.Metadata, new GetSubmodelElementByAasRequest("aas", "sm", "path", null, null) },
        { "/shells/{aas}/submodels/{sm}/submodel-elements/{path}/$value", c => c.GetSubmodelElementValueOnlyByAasIdAsync("aas", "sm", "path", Ct, Level.core, Extent.withBlobValue), Content.Value, new GetSubmodelElementByAasRequest("aas", "sm", "path", Level.core, Extent.withBlobValue) },
        { "/shells/{aas}/submodels/{sm}/submodel-elements/{path}/$reference", c => c.GetSubmodelElementReferenceByAasIdAsync("aas", "sm", "path", Ct, Level.core), Content.Reference, new GetSubmodelElementByAasRequest("aas", "sm", "path", Level.core, null) },
        { "/shells/{aas}/submodels/{sm}/submodel-elements/{path}/$path", c => c.GetSubmodelElementPathByAasIdAsync("aas", "sm", "path", Ct, Level.core), Content.Path, new GetSubmodelElementByAasRequest("aas", "sm", "path", Level.core, null) }
    };

    [Theory]
    [MemberData(nameof(Actions))]
    public async Task SuperpathAction_DelegatesToHandlerWithMatchingContentAndRequest(string route, Func<AasRepositoryController, Task> invoke, Content expectedContent, object expectedRequest)
    {
        await invoke(_sut);

        var call = Assert.Single(_handler.ReceivedCalls());
        var arguments = call.GetArguments();
        Assert.Equal(expectedRequest, arguments[0]);
        Assert.Equal(expectedContent, arguments[1]);
        Assert.False(string.IsNullOrEmpty(route));
    }

    [Fact]
    public async Task GetShellsReferenceAsync_DelegatesToHandlerAndReturnsOk()
    {
        var paged = new PagedContentDto { Result = new JsonArray() };
        _ = _handler.GetShellReferencesAsync(Arg.Is<GetShellsByAssetIdsRequest>(r => r.AssetIds!.SequenceEqual(new[] { "a" }) && r.IdShort == "short" && r.Limit == 7 && r.Cursor == "cur"), Arg.Any<CancellationToken>()).Returns(paged);

        var result = await _sut.GetShellsReferenceAsync(["a"], "short", "cur", Ct, 7);

        Assert.Same(paged, Assert.IsType<OkObjectResult>(result.Result).Value);
    }

    [Fact]
    public async Task GetShellReferenceByIdAsync_DelegatesToHandlerAndReturnsOk()
    {
        var reference = new JsonObject { ["type"] = "ModelReference" };
        _ = _handler.GetShellReferenceByIdAsync(new GetShellRequest("aas"), Arg.Any<CancellationToken>()).Returns(reference);

        var result = await _sut.GetShellReferenceByIdAsync("aas", Ct);

        Assert.Same(reference, Assert.IsType<OkObjectResult>(result.Result).Value);
    }

    [Fact]
    public async Task GetSubmodelElementValueOnlyByAasIdAsync_WhenValueIsNull_ReturnsJsonResultInsteadOfNoContent()
    {
        _ = _handler.GetSubmodelElementContentByAasIdAsync(Arg.Any<GetSubmodelElementByAasRequest>(), Arg.Any<Content>(), Arg.Any<CancellationToken>()).Returns((JsonNode?)null);

        var result = await _sut.GetSubmodelElementValueOnlyByAasIdAsync("aas", "sm", "path", Ct);

        Assert.Null(Assert.IsType<JsonResult>(result).Value);
    }

    [Fact]
    public void ContentModifierRoutes_AreExactlyTheFourteenSpecifiedRoutes()
    {
        string[] expected =
        [
            "$reference", "{aasIdentifier}/$reference",
            "{aasIdentifier}/submodels/{submodelIdentifier}/$metadata", "{aasIdentifier}/submodels/{submodelIdentifier}/$value",
            "{aasIdentifier}/submodels/{submodelIdentifier}/$reference", "{aasIdentifier}/submodels/{submodelIdentifier}/$path",
            "{aasIdentifier}/submodels/{submodelIdentifier}/submodel-elements/$metadata", "{aasIdentifier}/submodels/{submodelIdentifier}/submodel-elements/$value",
            "{aasIdentifier}/submodels/{submodelIdentifier}/submodel-elements/$reference", "{aasIdentifier}/submodels/{submodelIdentifier}/submodel-elements/$path",
            "{aasIdentifier}/submodels/{submodelIdentifier}/submodel-elements/{idShortPath}/$metadata", "{aasIdentifier}/submodels/{submodelIdentifier}/submodel-elements/{idShortPath}/$value",
            "{aasIdentifier}/submodels/{submodelIdentifier}/submodel-elements/{idShortPath}/$reference", "{aasIdentifier}/submodels/{submodelIdentifier}/submodel-elements/{idShortPath}/$path"
        ];

        var actual = typeof(AasRepositoryController)
                     .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                     .Select(m => m.GetCustomAttribute<HttpGetAttribute>()?.Template)
                     .Where(t => t is not null && t.Contains('$', StringComparison.Ordinal))
                     .ToList();

        Assert.Equal(expected.Order(StringComparer.Ordinal), actual.Order(StringComparer.Ordinal));
    }
}
