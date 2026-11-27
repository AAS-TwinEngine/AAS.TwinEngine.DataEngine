using System.Net;
using System.Text;
using System.Text.Json.Nodes;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasEnvironment.Providers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Providers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository;
using AAS.TwinEngine.DataEngine.DomainModel.Shared;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;
using AAS.TwinEngine.DataEngine.ModuleTests.Common;

using AasCore.Aas3_1;

using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;

using NSubstitute;
using NSubstitute.ClearExtensions;

namespace AAS.TwinEngine.DataEngine.ModuleTests.Api.Services.ContentModifiers;

/// <summary>
/// Route tests for the 16 Submodel Repository content modifier routes
/// (<c>$metadata</c>, <c>$value</c>, <c>$reference</c>, <c>$path</c>). The service layer is substituted so that
/// routing, parameter validation, handler wiring and serialization are tested end to end.
/// </summary>
public class SubmodelRepositoryContentModifierRoutesTests : IClassFixture<SubmodelRepositoryContentModifierRoutesTests.RoutesFixture>
{
    private static readonly string EncodedSubmodelId = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(ContentModifierTestData.SubmodelId));
    private static readonly string SubmodelRoute = $"/submodels/{EncodedSubmodelId}";

    private readonly ISubmodelRepositoryService _service;
    private readonly HttpClient _client;

    public SubmodelRepositoryContentModifierRoutesTests(RoutesFixture fixture)
    {
        _client = fixture.Client;
        _service = fixture.Service;
        _service.ClearSubstitute(ClearOptions.All);

        _ = _service.GetSubmodelAsync(ContentModifierTestData.SubmodelId, Arg.Any<SubmodelQueryOptions?>(), Arg.Any<CancellationToken>())
            .Returns(_ => CreateSubmodel());
        _ = _service.GetSubmodelElementAsync(ContentModifierTestData.SubmodelId, "Group", Arg.Any<SubmodelQueryOptions?>(), Arg.Any<CancellationToken>())
            .Returns(_ => CreateGroup());
        _ = _service.GetSubmodelElementAsync(ContentModifierTestData.SubmodelId, "Name", Arg.Any<SubmodelQueryOptions?>(), Arg.Any<CancellationToken>())
            .Returns(_ => CreateName());
        _ = _service.GetSubmodelElementReferenceAsync(ContentModifierTestData.SubmodelId, "Group", Arg.Any<SubmodelQueryOptions?>(), Arg.Any<CancellationToken>())
            .Returns(_ => new Reference(
                ReferenceTypes.ModelReference,
                [new Key(KeyTypes.Submodel, ContentModifierTestData.SubmodelId), new Key(KeyTypes.SubmodelElementCollection, "Group")]));
        _ = _service.GetAllSubmodelElementsAsync(ContentModifierTestData.SubmodelId, Arg.Any<SubmodelQueryOptions?>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(_ => new SubmodelElementsPage { PagingMetaData = new PagingMetaData { Cursor = "next" }, Result = [CreateName(), CreateGroup()] });
        _ = _service.GetAllSubmodelsAsync(Arg.Any<SubmodelSearchFilter?>(), Arg.Any<SubmodelQueryOptions?>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(_ => new SubmodelList { PagingMetaData = new PagingMetaData { Cursor = "next" }, Result = [CreateSubmodel()] });
    }

    /// <summary>Boots the web host once for all tests of the class; tests run sequentially and reset the substitute.</summary>
    public sealed class RoutesFixture : IDisposable
    {
        private readonly ConfigTestFactory _factory;

        public RoutesFixture()
        {
            Service = Substitute.For<ISubmodelRepositoryService>();
            _factory = new ConfigTestFactory("v1-config", services =>
            {
                _ = services.AddSingleton(Substitute.For<IPluginManifestProvider>());
                _ = services.AddSingleton(Substitute.For<IPluginManifestConflictHandler>());
                _ = services.AddSingleton(Substitute.For<ITemplateProvider>());
                _ = services.AddSingleton(Service);
            });
            Client = _factory.CreateClient();
        }

        public ISubmodelRepositoryService Service { get; }

        public HttpClient Client { get; }

        public void Dispose()
        {
            Client.Dispose();
            _factory.Dispose();
        }
    }

    [Theory]
    [InlineData("/$metadata", ContentModifierTestData.SubmodelMetadata)]
    [InlineData("/$value", ContentModifierTestData.SubmodelValue)]
    [InlineData("/$reference", ContentModifierTestData.SubmodelReference)]
    [InlineData("/$path", ContentModifierTestData.SubmodelPath)]
    [InlineData("/submodel-elements/$metadata", ContentModifierTestData.ElementsMetadata)]
    [InlineData("/submodel-elements/$value", ContentModifierTestData.ElementsValue)]
    [InlineData("/submodel-elements/$reference", ContentModifierTestData.ElementsReference)]
    [InlineData("/submodel-elements/$path", ContentModifierTestData.ElementsPath)]
    [InlineData("/submodel-elements/Group/$metadata", ContentModifierTestData.GroupMetadata)]
    [InlineData("/submodel-elements/Group/$value", ContentModifierTestData.GroupValue)]
    [InlineData("/submodel-elements/Group/$reference", ContentModifierTestData.GroupReference)]
    [InlineData("/submodel-elements/Group/$path", ContentModifierTestData.GroupPath)]
    [InlineData("/submodel-elements/Name/$metadata", ContentModifierTestData.NameMetadata)]
    [InlineData("/submodel-elements/Name/$value", ContentModifierTestData.NameValue)]
    public async Task GetSubmodelContent_ReturnsExpectedJsonAsync(string routeSuffix, string expectedJson)
    {
        var response = await _client.GetAsync(new Uri($"{SubmodelRoute}{routeSuffix}", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await AssertJsonAsync(expectedJson, response);
    }

    [Theory]
    [InlineData("/$metadata", ContentModifierTestData.ListSubmodelsMetadata)]
    [InlineData("/$value", ContentModifierTestData.ListSubmodelsValue)]
    [InlineData("/$reference", ContentModifierTestData.ListSubmodelsReference)]
    [InlineData("/$path", ContentModifierTestData.ListSubmodelsPath)]
    public async Task GetAllSubmodelsContent_ReturnsExpectedPagedJsonAsync(string routeSuffix, string expectedJson)
    {
        var response = await _client.GetAsync(new Uri($"/submodels{routeSuffix}?limit=1", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await AssertJsonAsync(expectedJson, response);
    }

    [Fact]
    public async Task GetAllSubmodelsContent_ForwardsFiltersAndPagingToServiceAsync()
    {
        var response = await _client.GetAsync(new Uri("/submodels/$path?idShort=Nameplate&limit=7&cursor=abc&level=core", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        _ = await _service.Received(1).GetAllSubmodelsAsync(
            Arg.Is<SubmodelSearchFilter?>(f => f!.IdShort == "Nameplate"),
            Arg.Is<SubmodelQueryOptions?>(o => o!.Level == "core"),
            7,
            "abc",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSubmodelValue_WithLevelCoreAndExtent_LoadsSubmodelWithRequestedOptionsAsync()
    {
        var response = await _client.GetAsync(new Uri($"{SubmodelRoute}/$value?level=core&extent=withBlobValue", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        _ = await _service.Received(1).GetSubmodelAsync(
            ContentModifierTestData.SubmodelId,
            Arg.Is<SubmodelQueryOptions?>(o => o!.Level == "core" && o.Extent == "withBlobValue"),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("/$metadata")]
    [InlineData("/$reference?level=core")]
    public async Task GetSubmodelContent_ForMetadataAndReference_AlwaysLoadsSubmodelWithDefaultOptionsAsync(string routeSuffix)
    {
        var response = await _client.GetAsync(new Uri($"{SubmodelRoute}{routeSuffix}", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        _ = await _service.Received(1).GetSubmodelAsync(
            ContentModifierTestData.SubmodelId,
            Arg.Is<SubmodelQueryOptions?>(o => o!.Level == "deep" && o.Extent == "withoutBlobValue"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSubmodelElementsValue_ForwardsPagingAsync()
    {
        var response = await _client.GetAsync(new Uri($"{SubmodelRoute}/submodel-elements/$value?limit=3&cursor=abc", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        _ = await _service.Received(1).GetAllSubmodelElementsAsync(
            ContentModifierTestData.SubmodelId,
            Arg.Any<SubmodelQueryOptions?>(),
            3,
            "abc",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSubmodelElementValue_WhenPropertyHasNoValue_ReturnsJsonNullWithOkAsync()
    {
        _ = _service.GetSubmodelElementAsync(ContentModifierTestData.SubmodelId, "Empty", Arg.Any<SubmodelQueryOptions?>(), Arg.Any<CancellationToken>())
            .Returns(_ => new Property(DataTypeDefXsd.String, idShort: "Empty"));

        var response = await _client.GetAsync(new Uri($"{SubmodelRoute}/submodel-elements/Empty/$value", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("null", (await response.Content.ReadAsStringAsync()).Trim());
    }

    [Theory]
    [InlineData("/$metadata?level=deep")]
    [InlineData("/$metadata?level=core")]
    [InlineData("/$metadata?extent=withBlobValue")]
    [InlineData("/$reference?level=deep")]
    [InlineData("/submodel-elements/$metadata?level=core")]
    [InlineData("/submodel-elements/$metadata?extent=withBlobValue")]
    [InlineData("/submodel-elements/$reference?level=deep")]
    [InlineData("/submodel-elements/Group/$metadata?level=core")]
    [InlineData("/submodel-elements/Group/$reference?level=deep")]
    [InlineData("/submodel-elements/Name/$path")]
    [InlineData("/submodel-elements/Name/$metadata?extent=withBlobValue")]
    public async Task GetSubmodelContent_WithForbiddenModifierCombination_Returns400Async(string routeSuffix)
    {
        var response = await _client.GetAsync(new Uri($"{SubmodelRoute}{routeSuffix}", UriKind.Relative));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("/submodels/$metadata?level=deep")]
    [InlineData("/submodels/$metadata?extent=withBlobValue")]
    [InlineData("/submodels/$reference?level=deep")]
    [InlineData("/submodels/$value?limit=-1")]
    [InlineData("/submodels/$path?cursor=%25%25")]
    public async Task GetAllSubmodelsContent_WithInvalidParameters_Returns400Async(string route)
    {
        var response = await _client.GetAsync(new Uri(route, UriKind.Relative));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("$metadata")]
    [InlineData("$value")]
    [InlineData("$reference")]
    [InlineData("$path")]
    public async Task GetSubmodelElementContent_WithMaliciousIdShortPath_Returns400Async(string modifier)
    {
        var maliciousPath = Uri.EscapeDataString("../../etc/passwd");

        var response = await _client.GetAsync(new Uri($"{SubmodelRoute}/submodel-elements/{maliciousPath}/{modifier}", UriKind.Relative));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("/$metadata")]
    [InlineData("/$value")]
    [InlineData("/$reference")]
    [InlineData("/$path")]
    [InlineData("/submodel-elements/$value")]
    [InlineData("/submodel-elements/Group/$value")]
    public async Task GetSubmodelContent_WithInvalidBase64Identifier_Returns400Async(string routeSuffix)
    {
        var response = await _client.GetAsync(new Uri($"/submodels/!!not-base64!!{routeSuffix}", UriKind.Relative));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("/$metadata")]
    [InlineData("/$value")]
    [InlineData("/$reference")]
    [InlineData("/$path")]
    public async Task GetSubmodelContent_WhenSubmodelDoesNotExist_Returns404Async(string routeSuffix)
    {
        var unknownId = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes("https://example.com/submodel/unknown"));
        _ = _service.GetSubmodelAsync("https://example.com/submodel/unknown", Arg.Any<SubmodelQueryOptions?>(), Arg.Any<CancellationToken>())
            .Returns((ISubmodel)null!);

        var response = await _client.GetAsync(new Uri($"/submodels/{unknownId}{routeSuffix}", UriKind.Relative));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetSubmodelElementReference_WhenElementDoesNotExist_Returns404Async()
    {
        _ = _service.GetSubmodelElementReferenceAsync(ContentModifierTestData.SubmodelId, "Missing", Arg.Any<SubmodelQueryOptions?>(), Arg.Any<CancellationToken>())
            .Returns((IReference)null!);

        var response = await _client.GetAsync(new Uri($"{SubmodelRoute}/submodel-elements/Missing/$reference", UriKind.Relative));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task BaseRoutes_StillReturnNormalSerializationAsync()
    {
        var submodelResponse = await _client.GetAsync(new Uri(SubmodelRoute, UriKind.Relative));
        var elementResponse = await _client.GetAsync(new Uri($"{SubmodelRoute}/submodel-elements/Group", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, submodelResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, elementResponse.StatusCode);
        var submodel = JsonNode.Parse(await submodelResponse.Content.ReadAsStringAsync());
        Assert.NotNull(submodel!["submodelElements"]);
        Assert.Equal("SubmodelElementCollection", JsonNode.Parse(await elementResponse.Content.ReadAsStringAsync())!["modelType"]!.GetValue<string>());
    }

    internal static async Task AssertJsonAsync(string expectedJson, HttpResponseMessage response)
    {
        var actual = JsonNode.Parse(await response.Content.ReadAsStringAsync());
        var expected = JsonNode.Parse(expectedJson);
        Assert.True(JsonNode.DeepEquals(expected, actual), $"Expected: {expected?.ToJsonString()}{System.Environment.NewLine}Actual:   {actual?.ToJsonString()}");
    }

    private static Submodel CreateSubmodel() => new(ContentModifierTestData.SubmodelId, submodelElements: [CreateName(), CreateGroup()]);

    private static Property CreateName() => new(DataTypeDefXsd.String, idShort: "Name", value: "A");

    private static SubmodelElementCollection CreateGroup()
        => new(idShort: "Group", value: [new Property(DataTypeDefXsd.Int, idShort: "Child", value: "5")]);
}
