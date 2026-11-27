using System.Net;
using System.Text;
using System.Text.Json.Nodes;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasEnvironment.Providers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasRepository;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Providers;
using AAS.TwinEngine.DataEngine.DomainModel.AasRepository;
using AAS.TwinEngine.DataEngine.DomainModel.Shared;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;
using AAS.TwinEngine.DataEngine.ModuleTests.Common;

using AasCore.Aas3_1;

using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;

using NSubstitute;
using NSubstitute.ClearExtensions;
using NSubstitute.ExceptionExtensions;

namespace AAS.TwinEngine.DataEngine.ModuleTests.Api.Services.ContentModifiers;

/// <summary>
/// Route tests for the 14 AAS Repository content modifier routes: the shell <c>$reference</c> routes and the
/// <c>$metadata</c>, <c>$value</c>, <c>$reference</c>, <c>$path</c> variants of the submodel superpath.
/// </summary>
public class AasRepositoryContentModifierRoutesTests : IClassFixture<AasRepositoryContentModifierRoutesTests.RoutesFixture>
{
    private const string OtherAasId = "https://example.com/aas/2";

    private static readonly string EncodedAasId = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(ContentModifierTestData.AasId));
    private static readonly string EncodedSubmodelId = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(ContentModifierTestData.SubmodelId));
    private static readonly string SuperpathRoute = $"/shells/{EncodedAasId}/submodels/{EncodedSubmodelId}";

    private readonly IAasRepositoryService _service;
    private readonly HttpClient _client;

    public AasRepositoryContentModifierRoutesTests(RoutesFixture fixture)
    {
        _client = fixture.Client;
        _service = fixture.Service;
        _service.ClearSubstitute(ClearOptions.All);

        _ = _service.GetShellsByFiltersAsync(Arg.Any<ShellSearchFilter?>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(_ => new Shells
            {
                PagingMetaData = new PagingMetaData { Cursor = "next" },
                Result = [CreateShell(ContentModifierTestData.AasId), CreateShell(OtherAasId)]
            });
        _ = _service.GetShellByIdAsync(ContentModifierTestData.AasId, Arg.Any<CancellationToken>())
            .Returns(_ => CreateShell(ContentModifierTestData.AasId));

        _ = _service.GetSubmodelByAasIdAsync(ContentModifierTestData.AasId, ContentModifierTestData.SubmodelId, Arg.Any<SubmodelQueryOptions>(), Arg.Any<CancellationToken>())
            .Returns(_ => CreateSubmodel());
        _ = _service.GetSubmodelElementByAasIdAsync(ContentModifierTestData.AasId, ContentModifierTestData.SubmodelId, "Group", Arg.Any<SubmodelQueryOptions>(), Arg.Any<CancellationToken>())
            .Returns(_ => CreateGroup());
        _ = _service.GetSubmodelElementByAasIdAsync(ContentModifierTestData.AasId, ContentModifierTestData.SubmodelId, "Name", Arg.Any<SubmodelQueryOptions>(), Arg.Any<CancellationToken>())
            .Returns(_ => CreateName());
        _ = _service.GetSubmodelElementReferenceByAasIdAsync(ContentModifierTestData.AasId, ContentModifierTestData.SubmodelId, "Group", Arg.Any<SubmodelQueryOptions?>(), Arg.Any<CancellationToken>())
            .Returns(_ => new Reference(
                ReferenceTypes.ModelReference,
                [new Key(KeyTypes.Submodel, ContentModifierTestData.SubmodelId), new Key(KeyTypes.SubmodelElementCollection, "Group")]));
        _ = _service.GetAllSubmodelElementsByAasIdAsync(ContentModifierTestData.AasId, ContentModifierTestData.SubmodelId, Arg.Any<SubmodelQueryOptions>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(_ => new SubmodelElementsPage { PagingMetaData = new PagingMetaData { Cursor = "next" }, Result = [CreateName(), CreateGroup()] });
    }

    /// <summary>Boots the web host once for all tests of the class; tests run sequentially and reset the substitute.</summary>
    public sealed class RoutesFixture : IDisposable
    {
        private readonly ConfigTestFactory _factory;

        public RoutesFixture()
        {
            Service = Substitute.For<IAasRepositoryService>();
            _factory = new ConfigTestFactory("v1-config", services =>
            {
                _ = services.AddSingleton(Substitute.For<IPluginManifestProvider>());
                _ = services.AddSingleton(Substitute.For<IPluginManifestConflictHandler>());
                _ = services.AddSingleton(Substitute.For<ITemplateProvider>());
                _ = services.AddSingleton(Service);
            });
            Client = _factory.CreateClient();
        }

        public IAasRepositoryService Service { get; }

        public HttpClient Client { get; }

        public void Dispose()
        {
            Client.Dispose();
            _factory.Dispose();
        }
    }

    [Fact]
    public async Task GetShellsReference_ReturnsModelReferencePerShellAsync()
    {
        var response = await _client.GetAsync(new Uri("/shells/$reference?limit=2", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await SubmodelRepositoryContentModifierRoutesTests.AssertJsonAsync(
            $$"""
            { "paging_metadata": { "cursor": "next" },
              "result": [ { "type": "ModelReference", "keys": [ { "type": "AssetAdministrationShell", "value": "{{ContentModifierTestData.AasId}}" } ] },
                          { "type": "ModelReference", "keys": [ { "type": "AssetAdministrationShell", "value": "{{OtherAasId}}" } ] } ] }
            """,
            response);
    }

    [Fact]
    public async Task GetShellsReference_ForwardsFiltersAndPagingToServiceAsync()
    {
        var response = await _client.GetAsync(new Uri("/shells/$reference?idShort=Product1&limit=3&cursor=abc", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        _ = await _service.Received(1).GetShellsByFiltersAsync(
            Arg.Is<ShellSearchFilter?>(f => f!.IdShort == "Product1"),
            3,
            "abc",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetShellsReference_WithNegativeLimit_Returns400Async()
    {
        var response = await _client.GetAsync(new Uri("/shells/$reference?limit=-1", UriKind.Relative));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetShellReferenceById_ReturnsModelReferenceToShellAsync()
    {
        var response = await _client.GetAsync(new Uri($"/shells/{EncodedAasId}/$reference", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await SubmodelRepositoryContentModifierRoutesTests.AssertJsonAsync(
            $$"""{ "type": "ModelReference", "keys": [ { "type": "AssetAdministrationShell", "value": "{{ContentModifierTestData.AasId}}" } ] }""",
            response);
    }

    [Fact]
    public async Task GetShellReferenceById_WhenShellDoesNotExist_Returns404Async()
    {
        _ = _service.GetShellByIdAsync(ContentModifierTestData.AasId, Arg.Any<CancellationToken>()).Returns((IAssetAdministrationShell?)null);

        var response = await _client.GetAsync(new Uri($"/shells/{EncodedAasId}/$reference", UriKind.Relative));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetShellReferenceById_WithInvalidBase64Identifier_Returns400Async()
    {
        var response = await _client.GetAsync(new Uri("/shells/!!not-base64!!/$reference", UriKind.Relative));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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
    public async Task GetSubmodelContentByAasId_ReturnsExpectedJsonAsync(string routeSuffix, string expectedJson)
    {
        var response = await _client.GetAsync(new Uri($"{SuperpathRoute}{routeSuffix}", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await SubmodelRepositoryContentModifierRoutesTests.AssertJsonAsync(expectedJson, response);
    }

    [Fact]
    public async Task GetSubmodelValueByAasId_WithLevelCoreAndExtent_LoadsSubmodelWithRequestedOptionsAsync()
    {
        var response = await _client.GetAsync(new Uri($"{SuperpathRoute}/$value?level=core&extent=withBlobValue", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        _ = await _service.Received(1).GetSubmodelByAasIdAsync(
            ContentModifierTestData.AasId,
            ContentModifierTestData.SubmodelId,
            Arg.Is<SubmodelQueryOptions>(o => o.Level == "core" && o.Extent == "withBlobValue"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSubmodelElementsValueByAasId_ForwardsPagingAsync()
    {
        var response = await _client.GetAsync(new Uri($"{SuperpathRoute}/submodel-elements/$value?limit=3&cursor=abc", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        _ = await _service.Received(1).GetAllSubmodelElementsByAasIdAsync(
            ContentModifierTestData.AasId,
            ContentModifierTestData.SubmodelId,
            Arg.Any<SubmodelQueryOptions>(),
            3,
            "abc",
            Arg.Any<CancellationToken>());
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
    public async Task GetSubmodelContentByAasId_WithForbiddenModifierCombination_Returns400Async(string routeSuffix)
    {
        var response = await _client.GetAsync(new Uri($"{SuperpathRoute}{routeSuffix}", UriKind.Relative));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("$metadata")]
    [InlineData("$value")]
    [InlineData("$reference")]
    [InlineData("$path")]
    public async Task GetSubmodelElementContentByAasId_WithMaliciousIdShortPath_Returns400Async(string modifier)
    {
        var maliciousPath = Uri.EscapeDataString("../../etc/passwd");

        var response = await _client.GetAsync(new Uri($"{SuperpathRoute}/submodel-elements/{maliciousPath}/{modifier}", UriKind.Relative));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("/$metadata")]
    [InlineData("/$value")]
    [InlineData("/$reference")]
    [InlineData("/$path")]
    [InlineData("/submodel-elements/$value")]
    [InlineData("/submodel-elements/Group/$value")]
    [InlineData("/submodel-elements/Group/$reference")]
    public async Task GetSubmodelContentByAasId_WhenSubmodelDoesNotBelongToShell_Returns404Async(string routeSuffix)
    {
        _ = _service.GetSubmodelByAasIdAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<SubmodelQueryOptions>(), Arg.Any<CancellationToken>()).ThrowsAsync(new SubmodelNotFoundException());
        _ = _service.GetAllSubmodelElementsByAasIdAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<SubmodelQueryOptions>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).ThrowsAsync(new SubmodelNotFoundException());
        _ = _service.GetSubmodelElementByAasIdAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<SubmodelQueryOptions>(), Arg.Any<CancellationToken>()).ThrowsAsync(new SubmodelNotFoundException());
        _ = _service.GetSubmodelElementReferenceByAasIdAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<SubmodelQueryOptions?>(), Arg.Any<CancellationToken>()).ThrowsAsync(new SubmodelNotFoundException());

        var response = await _client.GetAsync(new Uri($"{SuperpathRoute}{routeSuffix}", UriKind.Relative));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetSubmodelContentByAasId_WithInvalidBase64Identifier_Returns400Async()
    {
        var response = await _client.GetAsync(new Uri($"/shells/{EncodedAasId}/submodels/!!not-base64!!/$value", UriKind.Relative));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task BaseRoutes_StillReturnNormalSerializationAsync()
    {
        var response = await _client.GetAsync(new Uri(SuperpathRoute, UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(JsonNode.Parse(await response.Content.ReadAsStringAsync())!["submodelElements"]);
    }

    private static AssetAdministrationShell CreateShell(string id) => new(id, new AssetInformation(AssetKind.Instance));

    private static Submodel CreateSubmodel() => new(ContentModifierTestData.SubmodelId, submodelElements: [CreateName(), CreateGroup()]);

    private static Property CreateName() => new(DataTypeDefXsd.String, idShort: "Name", value: "A");

    private static SubmodelElementCollection CreateGroup()
        => new(idShort: "Group", value: [new Property(DataTypeDefXsd.Int, idShort: "Child", value: "5")]);
}
