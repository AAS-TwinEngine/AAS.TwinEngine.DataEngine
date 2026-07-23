using System.Net;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Observability;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Clients;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Clients.Caching;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using AasCore.Aas3_1;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Template = AAS.TwinEngine.DataEngine.Infrastructure.Providers.TemplateProvider.Services.TemplateProvider;
using UnauthorizedAccessException = AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure.UnauthorizedAccessException;
using AAS.TwinEngine.DataEngine.UnitTests.ApplicationLogic.Observability;

namespace AAS.TwinEngine.DataEngine.UnitTests.Infrastructure.Providers.TemplateProvider.Services;

public class TemplateProviderTests
{
    private readonly ICachedGetRequestClient _cachedHttp;
    private readonly Template _sut;
    private const string TemplateId = "Nameplate";

    private ActivityListenerFixture CreateFixture() => new();

    public TemplateProviderTests()
    {
        var logger = Substitute.For<ILogger<Template>>();
        _cachedHttp = Substitute.For<ICachedGetRequestClient>();

        var options = Substitute.For<IOptions<TemplateManagementConfig>>();
        var config = new TemplateManagementConfig
        {
            SubmodelTemplateRepository = new ServiceInstance { LocalCacheExpirationInMinutes = 5 },
            AasTemplateRegistry = new ServiceInstance { LocalCacheExpirationInMinutes = 5 },
            AasTemplateRepository = new ServiceInstance { LocalCacheExpirationInMinutes = 5 },
            ConceptDescriptionTemplateRepository = new ServiceInstance { LocalCacheExpirationInMinutes = 5 },
            SubmodelTemplateRegistry = new ServiceInstance { LocalCacheExpirationInMinutes = 5 }
        };
        options.Value.Returns(config);

        _sut = new Template(logger, options, _cachedHttp);
    }
    
    [Fact]
    public async Task GetShellDescriptorTemplateAsync_ReturnsShellDescriptor_WhenValidResponse()
    {
        const string JsonResponse = """
                                    {
                                      "assetKind": "Type",
                                      "assetType": "Type",
                                      "endpoints": [
                                        {
                                          "interface": "AAS-3.0",
                                          "protocolInformation": {
                                            "href": "http://localhost:8081/shells/test",
                                            "endpointProtocol": "http"
                                          }
                                        }
                                      ],
                                      "globalAssetId": "https://admin-shell.io/idta/asset/ContactInformation/1/0",
                                      "idShort": "ContactInformationAAS",
                                      "id": "https://admin-shell.io/idta/aas/ContactInformation/1/0"
                                    }
                                    """;

        _cachedHttp.GetStringAsync(Arg.Any<string>(), HttpClientNames.AasRegistry, Arg.Any<int>(), Arg.Any<CancellationToken>())
                   .Returns(JsonResponse);

        var result = await _sut.GetShellDescriptorTemplateAsync(TemplateId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("https://admin-shell.io/idta/aas/ContactInformation/1/0", result.Id);
        Assert.Equal("ContactInformationAAS", result.IdShort);
        Assert.Equal("https://admin-shell.io/idta/asset/ContactInformation/1/0", result.GlobalAssetId);
    }

    [Fact]
    public async Task GetShellDescriptorTemplateAsync_DeserializesAasNestedTypes_WhenResponseContainsAasPayload()
    {
        const string JsonResponse = """
                                    {
                                        "description": [
                                            {
                                                "language": "en",
                                                "text": "Template Asset Administration Shell for example environments."
                                            }
                                        ],
                                        "displayName": [
                                            {
                                                "language": "en",
                                                "text": "AAS Template"
                                            }
                                        ],
                                        "extensions": [
                                            {
                                                "name": "templateSource",
                                                "valueType": "xs:string",
                                                "value": "ShellTemplate"
                                            }
                                        ],
                                        "administration": {
                                            "version": "1",
                                            "revision": "0"
                                        },
                                        "assetKind": "Instance",
                                        "id": "https://mm-software.com/aas/aasTemplate",
                                        "specificAssetIds": [
                                            {
                                                "name": "SerialNumber",
                                                "value": "SN-9429",
                                                "externalSubjectId": {
                                                    "type": "ExternalReference",
                                                    "keys": [
                                                        {
                                                            "type": "GlobalReference",
                                                            "value": "https://example.com/subjects/serial-number"
                                                        }
                                                    ]
                                                }
                                            }
                                        ]
                                    }
                                    """;

        _cachedHttp.GetStringAsync(Arg.Any<string>(), HttpClientNames.AasRegistry, Arg.Any<int>(), Arg.Any<CancellationToken>())
                   .Returns(JsonResponse);

        var result = await _sut.GetShellDescriptorTemplateAsync(TemplateId, CancellationToken.None);

        Assert.NotNull(result.Description);
        Assert.Equal("en", result.Description![0].Language);
        Assert.Equal("Template Asset Administration Shell for example environments.", result.Description[0].Text);

        Assert.NotNull(result.DisplayName);
        Assert.Equal("AAS Template", result.DisplayName![0].Text);

        Assert.NotNull(result.Extensions);
        Assert.Equal("templateSource", result.Extensions![0].Name);
        Assert.Equal(DataTypeDefXsd.String, result.Extensions[0].ValueType);
        Assert.Equal("ShellTemplate", result.Extensions[0].Value);

        Assert.NotNull(result.Administration);
        Assert.Equal("1", result.Administration!.Version);
        Assert.Equal("0", result.Administration.Revision);

        Assert.NotNull(result.SpecificAssetIds);
        Assert.Equal("SerialNumber", result.SpecificAssetIds![0].Name);
        Assert.NotNull(result.SpecificAssetIds[0].ExternalSubjectId);
        Assert.Equal("https://example.com/subjects/serial-number", result.SpecificAssetIds[0].ExternalSubjectId!.Keys[0].Value);
    }

    [Fact]
    public async Task GetShellDescriptorTemplateAsync_ThrowsResponseParsingException_WhenInvalidJsonResponse()
    {
        _cachedHttp.GetStringAsync(Arg.Any<string>(), HttpClientNames.AasRegistry, Arg.Any<int>(), Arg.Any<CancellationToken>())
                   .Returns("{ invalid json }");

        await Assert.ThrowsAsync<ResponseParsingException>(() => _sut.GetShellDescriptorTemplateAsync(TemplateId, CancellationToken.None));
    }

    [Fact]
    public async Task GetShellDescriptorTemplateAsync_ThrowsResponseParsingException_WhenDeserializationReturnsNull()
    {
        _cachedHttp.GetStringAsync(Arg.Any<string>(), HttpClientNames.AasRegistry, Arg.Any<int>(), Arg.Any<CancellationToken>())
                   .Returns("null");

        await Assert.ThrowsAsync<ResponseParsingException>(() => _sut.GetShellDescriptorTemplateAsync(TemplateId, CancellationToken.None));
    }

    [Fact]
    public async Task GetShellDescriptorTemplateAsync_ThrowsResourceNotFoundException_WhenNotFoundResponse()
    {
        _cachedHttp.GetStringAsync(Arg.Any<string>(), HttpClientNames.AasRegistry, Arg.Any<int>(), Arg.Any<CancellationToken>())
                   .ThrowsAsync(new ResourceNotFoundException());

        await Assert.ThrowsAsync<ResourceNotFoundException>(() => _sut.GetShellDescriptorTemplateAsync(TemplateId, CancellationToken.None));
    }

    [Fact]
    public async Task GetShellDescriptorTemplateAsync_ThrowsException_WhenHttpClientFails()
    {
        _cachedHttp.GetStringAsync(Arg.Any<string>(), HttpClientNames.AasRegistry, Arg.Any<int>(), Arg.Any<CancellationToken>())
                   .ThrowsAsync(new HttpRequestException("Network error"));

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => _sut.GetShellDescriptorTemplateAsync(TemplateId, CancellationToken.None));
        Assert.Equal("Network error", exception.Message);
    }

    [Fact]
    public async Task GetShellDescriptorTemplateAsync_UsesBase64UrlEncodedTemplateIdInRequestPath()
    {
        var templateId = "https://admin-shell.io/idta/asset/shell-descriptor-template";
        var requestPath = string.Empty;
        const string JsonResponse = """
                                    {
                                      "assetKind": "Type",
                                      "assetType": "Type",
                                      "idShort": "ContactInformationAAS",
                                      "id": "https://admin-shell.io/idta/aas/ContactInformation/1/0"
                                    }
                                    """;

        _cachedHttp.GetStringAsync(Arg.Any<string>(), HttpClientNames.AasRegistry, Arg.Any<int>(), Arg.Any<CancellationToken>())
                   .Returns(info =>
                   {
                       requestPath = info.ArgAt<string>(0);
                       return JsonResponse;
                   });

        _ = await _sut.GetShellDescriptorTemplateAsync(templateId, CancellationToken.None);

        var expectedEncodedTemplateId = Convert.ToBase64String(Encoding.UTF8.GetBytes(templateId)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        Assert.EndsWith($"shell-descriptors/{expectedEncodedTemplateId}", requestPath, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetShellDescriptorTemplateAsync_ReturnsShellDescriptor_WhenResponseIsWrappedInResultProperty()
    {
        const string JsonResponse = """
                                    {
                                      "result": {
                                        "assetKind": "Instance",
                                        "globalAssetId": "https://admin-shell.io/idta/asset/wrapped/1/0",
                                        "idShort": "WrappedAAS",
                                        "id": "https://admin-shell.io/idta/aas/wrapped/1/0"
                                      }
                                    }
                                    """;

        _cachedHttp.GetStringAsync(Arg.Any<string>(), HttpClientNames.AasRegistry, Arg.Any<int>(), Arg.Any<CancellationToken>())
                   .Returns(JsonResponse);

        var result = await _sut.GetShellDescriptorTemplateAsync(TemplateId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("https://admin-shell.io/idta/aas/wrapped/1/0", result.Id);
        Assert.Equal("WrappedAAS", result.IdShort);
        Assert.Equal("https://admin-shell.io/idta/asset/wrapped/1/0", result.GlobalAssetId);
        Assert.Equal(AssetKind.Instance, result.AssetKind);
    }

    [Fact]
    public async Task GetShellDescriptorTemplateAsync_DeserializesAssetKindAndAssetType_FromEnumStrings()
    {
        const string JsonResponse = """
                                    {
                                      "assetKind": "Type",
                                      "assetType": "Instance",
                                      "id": "https://mm-software.com/aas/typed",
                                      "idShort": "TypedAAS"
                                    }
                                    """;

        _cachedHttp.GetStringAsync(Arg.Any<string>(), HttpClientNames.AasRegistry, Arg.Any<int>(), Arg.Any<CancellationToken>())
                   .Returns(JsonResponse);

        var result = await _sut.GetShellDescriptorTemplateAsync(TemplateId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(AssetKind.Type, result.AssetKind);
        Assert.Equal(AssetKind.Instance, result.AssetType);
    }

    [Fact]
    public async Task GetShellDescriptorTemplateAsync_ReturnsNullAssetKindAndAssetType_WhenFieldsAreMissing()
    {
        const string JsonResponse = """
                                    {
                                      "id": "https://mm-software.com/aas/no-kind",
                                      "idShort": "NoKindAAS"
                                    }
                                    """;

        _cachedHttp.GetStringAsync(Arg.Any<string>(), HttpClientNames.AasRegistry, Arg.Any<int>(), Arg.Any<CancellationToken>())
                   .Returns(JsonResponse);

        var result = await _sut.GetShellDescriptorTemplateAsync(TemplateId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Null(result.AssetKind);
        Assert.Null(result.AssetType);
        Assert.Null(result.GlobalAssetId);
        Assert.Null(result.Description);
        Assert.Null(result.Extensions);
        Assert.Null(result.Administration);
        Assert.Null(result.SpecificAssetIds);
        Assert.Null(result.SubmodelDescriptors);
    }

    [Fact]
    public async Task GetShellTemplateAsync_ReturnsShell_WhenValidResponse()
    {
        _cachedHttp.GetStringAsync(Arg.Any<string>(), HttpClientNames.AasTemplateRepository, Arg.Any<int>(), Arg.Any<CancellationToken>())
                   .Returns(ProviderTestData.ValidateShellResponse);

        var result = await _sut.GetShellTemplateAsync(TemplateId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("https://admin-shell.io/idta/aas/ContactInformation/1/0", result.Id);
        Assert.Equal("ContactInformationAAS", result.IdShort);
    }

    [Fact]
    public async Task GetShellTemplateAsync_ThrowsResponseParsingException_WhenInvalidJsonResponse()
    {
        _cachedHttp.GetStringAsync(Arg.Any<string>(), HttpClientNames.AasTemplateRepository, Arg.Any<int>(), Arg.Any<CancellationToken>())
                   .Returns("{ invalid json }");

        await Assert.ThrowsAsync<ResponseParsingException>(() => _sut.GetShellTemplateAsync(TemplateId, CancellationToken.None));
    }

    [Fact]
    public async Task GetShellTemplateAsync_ThrowsResourceNotFoundException_WhenNotFoundResponse()
    {
        _cachedHttp.GetStringAsync(Arg.Any<string>(), HttpClientNames.AasTemplateRepository, Arg.Any<int>(), Arg.Any<CancellationToken>())
                   .ThrowsAsync(new ResourceNotFoundException());

        await Assert.ThrowsAsync<ResourceNotFoundException>(() => _sut.GetShellTemplateAsync(TemplateId, CancellationToken.None));
    }

    [Fact]
    public async Task GetShellTemplateAsync_ThrowsException_WhenHttpClientFails()
    {
        _cachedHttp.GetStringAsync(Arg.Any<string>(), HttpClientNames.AasTemplateRepository, Arg.Any<int>(), Arg.Any<CancellationToken>())
                   .ThrowsAsync(new HttpRequestException("Network error"));

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => _sut.GetShellTemplateAsync(TemplateId, CancellationToken.None));
        Assert.Equal("Network error", exception.Message);
    }

    [Fact]
    public async Task GetAssetInformationTemplateAsync_ReturnsAssetInformation_WhenValidResponse()
    {
        _cachedHttp.GetStringAsync(Arg.Any<string>(), HttpClientNames.AasTemplateRepository, Arg.Any<int>(), Arg.Any<CancellationToken>())
                   .Returns(ProviderTestData.ValidateAssetInformationResponse);

        var result = await _sut.GetAssetInformationTemplateAsync(TemplateId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("https://mm-software.de/shell/1", result.GlobalAssetId);
    }

    [Fact]
    public async Task GetAssetInformationTemplateAsync_ThrowsResponseParsingException_WhenInvalidJsonResponse()
    {
        _cachedHttp.GetStringAsync(Arg.Any<string>(), HttpClientNames.AasTemplateRepository, Arg.Any<int>(), Arg.Any<CancellationToken>())
                   .Returns("{ invalid json }");

        await Assert.ThrowsAsync<ResponseParsingException>(() => _sut.GetAssetInformationTemplateAsync(TemplateId, CancellationToken.None));
    }

    [Fact]
    public async Task GetAssetInformationTemplateAsync_ThrowsResourceNotFoundException_WhenNotFoundResponse()
    {
        _cachedHttp.GetStringAsync(Arg.Any<string>(), HttpClientNames.AasTemplateRepository, Arg.Any<int>(), Arg.Any<CancellationToken>())
                   .ThrowsAsync(new ResourceNotFoundException());

        await Assert.ThrowsAsync<ResourceNotFoundException>(() => _sut.GetAssetInformationTemplateAsync(TemplateId, CancellationToken.None));
    }

    [Fact]
    public async Task GetAssetInformationTemplateAsync_ThrowsException_WhenHttpClientFails()
    {
        _cachedHttp.GetStringAsync(Arg.Any<string>(), HttpClientNames.AasTemplateRepository, Arg.Any<int>(), Arg.Any<CancellationToken>())
                   .ThrowsAsync(new HttpRequestException("Network error"));

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => _sut.GetAssetInformationTemplateAsync(TemplateId, CancellationToken.None));
        Assert.Equal("Network error", exception.Message);
    }

    [Fact]
    public async Task GetSubmodelRefByIdAsync_ReturnsSubmodelRefs_WhenValidResponse()
    {
        _cachedHttp.GetStringAsync(Arg.Any<string>(), HttpClientNames.AasTemplateRepository, Arg.Any<int>(), Arg.Any<CancellationToken>())
                   .Returns(ProviderTestData.ValidateSubmodelRefResponse);

        var result = await _sut.GetSubmodelRefByIdAsync(TemplateId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("urn:uuid:submodel-123", result[0].Keys![0].Value);
    }

    [Fact]
    public async Task GetSubmodelRefByIdAsync_ThrowsResourceNotFoundException_WhenResultArrayMissing()
    {
        _cachedHttp.GetStringAsync(Arg.Any<string>(), HttpClientNames.AasTemplateRepository, Arg.Any<int>(), Arg.Any<CancellationToken>())
                   .Returns("{ \"unexpected\": [] }");

        await Assert.ThrowsAsync<ResourceNotFoundException>(() => _sut.GetSubmodelRefByIdAsync(TemplateId, CancellationToken.None));
    }

    [Fact]
    public async Task GetSubmodelRefByIdAsync_ThrowsResourceNotFoundException_WhenResultArrayIsEmpty()
    {
        _cachedHttp.GetStringAsync(Arg.Any<string>(), HttpClientNames.AasTemplateRepository, Arg.Any<int>(), Arg.Any<CancellationToken>())
                   .Returns("{ \"result\": [] }");

        await Assert.ThrowsAsync<ResourceNotFoundException>(() => _sut.GetSubmodelRefByIdAsync(TemplateId, CancellationToken.None));
    }

    [Fact]
    public async Task GetSubmodelRefByIdAsync_ThrowsResponseParsingException_WhenInvalidJson()
    {
        _cachedHttp.GetStringAsync(Arg.Any<string>(), HttpClientNames.AasTemplateRepository, Arg.Any<int>(), Arg.Any<CancellationToken>())
                   .Returns("{ invalid json }");

        await Assert.ThrowsAsync<ResponseParsingException>(() => _sut.GetSubmodelRefByIdAsync(TemplateId, CancellationToken.None));
    }

    [Fact]
    public async Task GetSubmodelRefByIdAsync_ThrowsHttpRequestException_WhenHttpFails()
    {
        _cachedHttp.GetStringAsync(Arg.Any<string>(), HttpClientNames.AasTemplateRepository, Arg.Any<int>(), Arg.Any<CancellationToken>())
                   .ThrowsAsync(new HttpRequestException("Network error"));

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => _sut.GetSubmodelRefByIdAsync(TemplateId, CancellationToken.None));
        Assert.Equal("Network error", exception.Message);
    }

    [Fact]
    public async Task GetConceptDescriptionByIdAsync_ReturnsConceptDescription_WhenResponseIsValid()
    {
        const string CdIdentifier = "test-id";
        _cachedHttp.GetStringAsync(Arg.Any<string>(), HttpClientNames.ConceptDescriptorTemplateRepository, Arg.Any<int>(), Arg.Any<CancellationToken>())
                   .Returns(ProviderTestData.ValidConceptDescription);

        var result = await _sut.GetConceptDescriptionByIdAsync(CdIdentifier, CancellationToken.None);

        Assert.NotNull(result);
    }

    [Theory]
    [InlineData(typeof(JsonException))]
    [InlineData(typeof(RequestTimeoutException))]
    [InlineData(typeof(ValidationFailedException))]
    [InlineData(typeof(ResourceNotFoundException))]
    [InlineData(typeof(UnauthorizedAccessException))]
    public async Task GetConceptDescriptionByIdAsync_ReturnsNull_OnHandledExceptions(Type exceptionType)
    {
        const string CdIdentifier = "test-id";
        var exception = (Exception)Activator.CreateInstance(exceptionType)!;
        _cachedHttp.GetStringAsync(Arg.Any<string>(), HttpClientNames.ConceptDescriptorTemplateRepository, Arg.Any<int>(), Arg.Any<CancellationToken>())
                   .ThrowsAsync(exception);

        var result = await _sut.GetConceptDescriptionByIdAsync(CdIdentifier, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetSubmodelTemplateAsync_StartsFetchTemplateSpan_WithTemplateIdTag()
    {
        const string TemplateIdForSpan = "Nameplate";
        using var fixture = CreateFixture();

        _cachedHttp.GetStringAsync(Arg.Any<string>(), HttpClientNames.SubmodelTemplateRepository, Arg.Any<int>(), Arg.Any<CancellationToken>())
                   .Returns(ProviderTestData.ValidateSubmodelResponse);

        _ = await _sut.GetFilteredSubmodelTemplateAsync(TemplateIdForSpan, null, CancellationToken.None);

        var capturedActivities = fixture.Activities.ToArray();
        var span = Assert.Single(capturedActivities.Where(a => a.OperationName == DataEngineTracing.Spans.GetSubmodelTemplate));
        Assert.Equal(DataEngineTracing.Spans.GetSubmodelTemplate, span.OperationName);
        Assert.Equal(TemplateIdForSpan, span.GetTagItem(DataEngineTracing.Attributes.TemplateId));
    }
}
