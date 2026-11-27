using System.Reflection;
using System.Text.Json.Nodes;

using AAS.TwinEngine.DataEngine.Api.Shared;
using AAS.TwinEngine.DataEngine.Api.SubmodelRepository;
using AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Handler;
using AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Requests;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using NSubstitute;

using Content = AAS.TwinEngine.DataEngine.Api.Shared.ContentModifiers.Content;

namespace AAS.TwinEngine.DataEngine.UnitTests.Api.SubmodelRepository;

public class SubmodelRepositoryContentModifierControllerTests
{
    private static readonly CancellationToken Ct = CancellationToken.None;

    private readonly ISubmodelRepositoryHandler _handler = Substitute.For<ISubmodelRepositoryHandler>();
    private readonly SubmodelRepositoryController _sut;

    public SubmodelRepositoryContentModifierControllerTests()
        => _sut = new SubmodelRepositoryController(Substitute.For<ILogger<SubmodelRepositoryController>>(), _handler);

    public static TheoryData<string, Func<SubmodelRepositoryController, Task>, Content, object> Actions => new()
    {
        { "/submodels/$metadata", c => c.GetAllSubmodelsMetadataAsync("sem", "short", "cur", Ct, 7, null, null), Content.Metadata, new GetAllSubmodelsRequest("sem", "short", 7, "cur", null, null) },
        { "/submodels/$value", c => c.GetAllSubmodelsValueOnlyAsync("sem", "short", "cur", Ct, 7, Level.core, Extent.withBlobValue), Content.Value, new GetAllSubmodelsRequest("sem", "short", 7, "cur", Level.core, Extent.withBlobValue) },
        { "/submodels/$reference", c => c.GetAllSubmodelsReferenceAsync("sem", "short", "cur", Ct, 7, Level.core), Content.Reference, new GetAllSubmodelsRequest("sem", "short", 7, "cur", Level.core, null) },
        { "/submodels/$path", c => c.GetAllSubmodelsPathAsync("sem", "short", "cur", Ct, 7, Level.core), Content.Path, new GetAllSubmodelsRequest("sem", "short", 7, "cur", Level.core, null) },

        { "/submodels/{id}/$metadata", c => c.GetSubmodelMetadataAsync("id", Ct, null, null), Content.Metadata, new GetSubmodelRequest("id", null, null) },
        { "/submodels/{id}/$value", c => c.GetSubmodelValueOnlyAsync("id", Ct, Level.core, Extent.withBlobValue), Content.Value, new GetSubmodelRequest("id", Level.core, Extent.withBlobValue) },
        { "/submodels/{id}/$reference", c => c.GetSubmodelReferenceAsync("id", Ct, Level.core), Content.Reference, new GetSubmodelRequest("id", Level.core, null) },
        { "/submodels/{id}/$path", c => c.GetSubmodelPathAsync("id", Ct, Level.core), Content.Path, new GetSubmodelRequest("id", Level.core, null) },

        { "/submodels/{id}/submodel-elements/$metadata", c => c.GetAllSubmodelElementsMetadataAsync("id", "cur", Ct, 7, null, null), Content.Metadata, new GetAllSubmodelElementsRequest("id", 7, "cur", null, null) },
        { "/submodels/{id}/submodel-elements/$value", c => c.GetAllSubmodelElementsValueOnlyAsync("id", "cur", Ct, 7, Level.core, Extent.withBlobValue), Content.Value, new GetAllSubmodelElementsRequest("id", 7, "cur", Level.core, Extent.withBlobValue) },
        { "/submodels/{id}/submodel-elements/$reference", c => c.GetAllSubmodelElementsReferenceAsync("id", "cur", Ct, 7, Level.core), Content.Reference, new GetAllSubmodelElementsRequest("id", 7, "cur", Level.core, null) },
        { "/submodels/{id}/submodel-elements/$path", c => c.GetAllSubmodelElementsPathAsync("id", "cur", Ct, 7, Level.core), Content.Path, new GetAllSubmodelElementsRequest("id", 7, "cur", Level.core, null) },

        { "/submodels/{id}/submodel-elements/{path}/$metadata", c => c.GetSubmodelElementMetadataAsync("id", "path", Ct, null, null), Content.Metadata, new GetSubmodelElementRequest("id", "path", null, null) },
        { "/submodels/{id}/submodel-elements/{path}/$value", c => c.GetSubmodelElementValueOnlyAsync("id", "path", Ct, Level.core, Extent.withBlobValue), Content.Value, new GetSubmodelElementRequest("id", "path", Level.core, Extent.withBlobValue) },
        { "/submodels/{id}/submodel-elements/{path}/$reference", c => c.GetSubmodelElementReferenceAsync("id", "path", Ct, Level.core), Content.Reference, new GetSubmodelElementRequest("id", "path", Level.core, null) },
        { "/submodels/{id}/submodel-elements/{path}/$path", c => c.GetSubmodelElementPathAsync("id", "path", Ct, Level.core), Content.Path, new GetSubmodelElementRequest("id", "path", Level.core, null) }
    };

    [Theory]
    [MemberData(nameof(Actions))]
    public async Task Action_DelegatesToHandlerWithMatchingContentAndRequest(string route, Func<SubmodelRepositoryController, Task> invoke, Content expectedContent, object expectedRequest)
    {
        await invoke(_sut);

        var call = Assert.Single(_handler.ReceivedCalls());
        var arguments = call.GetArguments();
        Assert.Equal(expectedRequest, arguments[0]);
        Assert.Equal(expectedContent, arguments[1]);
        Assert.False(string.IsNullOrEmpty(route));
    }

    [Fact]
    public void ContentModifierRoutes_AreExactlyTheSixteenSpecifiedRoutes()
    {
        string[] expected =
        [
            "$metadata", "$value", "$reference", "$path",
            "{submodelIdentifier}/$metadata", "{submodelIdentifier}/$value", "{submodelIdentifier}/$reference", "{submodelIdentifier}/$path",
            "{submodelIdentifier}/submodel-elements/$metadata", "{submodelIdentifier}/submodel-elements/$value",
            "{submodelIdentifier}/submodel-elements/$reference", "{submodelIdentifier}/submodel-elements/$path",
            "{submodelIdentifier}/submodel-elements/{idShortPath}/$metadata", "{submodelIdentifier}/submodel-elements/{idShortPath}/$value",
            "{submodelIdentifier}/submodel-elements/{idShortPath}/$reference", "{submodelIdentifier}/submodel-elements/{idShortPath}/$path"
        ];

        var actual = typeof(SubmodelRepositoryController)
                     .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                     .Select(m => m.GetCustomAttribute<HttpGetAttribute>()?.Template)
                     .Where(t => t is not null && t.Contains('$', StringComparison.Ordinal))
                     .ToList();

        Assert.Equal(expected.Order(StringComparer.Ordinal), actual.Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task GetSubmodelElementValueOnlyAsync_WhenValueIsNull_ReturnsJsonResultInsteadOfNoContent()
    {
        _ = _handler.GetSubmodelElementContent(Arg.Any<GetSubmodelElementRequest>(), Arg.Any<Content>(), Arg.Any<CancellationToken>()).Returns((JsonNode?)null);

        var result = await _sut.GetSubmodelElementValueOnlyAsync("id", "path", Ct);

        var jsonResult = Assert.IsType<JsonResult>(result);
        Assert.Null(jsonResult.Value);
    }

    [Fact]
    public async Task GetSubmodelPathAsync_ReturnsOkWithHandlerContent()
    {
        var paths = new JsonArray("A", "A.B");
        _ = _handler.GetSubmodelContent(Arg.Any<GetSubmodelRequest>(), Content.Path, Arg.Any<CancellationToken>()).Returns(paths);

        var result = await _sut.GetSubmodelPathAsync("id", Ct);

        Assert.Same(paths, Assert.IsType<OkObjectResult>(result.Result).Value);
    }

    [Fact]
    public async Task GetAllSubmodelElementsValueOnlyAsync_ReturnsOkWithPagedContent()
    {
        var paged = new PagedContentDto { Result = new JsonObject { ["Name"] = "A" } };
        _ = _handler.GetAllSubmodelElementsContent(Arg.Any<GetAllSubmodelElementsRequest>(), Content.Value, Arg.Any<CancellationToken>()).Returns(paged);

        var result = await _sut.GetAllSubmodelElementsValueOnlyAsync("id", null, Ct);

        Assert.Same(paged, Assert.IsType<OkObjectResult>(result.Result).Value);
    }
}
