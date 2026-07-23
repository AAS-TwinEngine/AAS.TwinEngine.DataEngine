using System.Net;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRegistry;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Clients;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Clients.Caching;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.SubmodelRegistryProvider.Services;

using AasCore.Aas3_1;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using UnauthorizedAccessException = AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure.UnauthorizedAccessException;

namespace AAS.TwinEngine.DataEngine.UnitTests.Infrastructure.Providers.SubmodelRegistryProvider.Services;

public class SubmodelDescriptorProviderTests
{
    private readonly ILogger<SubmodelDescriptorProvider> _logger = Substitute.For<ILogger<SubmodelDescriptorProvider>>();
    private readonly ICachedGetRequestClient _cachedHttp = Substitute.For<ICachedGetRequestClient>();
    private readonly SubmodelDescriptorProvider _sut;

    public SubmodelDescriptorProviderTests()
    {
        var options = Substitute.For<IOptions<TemplateManagementConfig>>();
        var config = new TemplateManagementConfig
        {
            SubmodelTemplateRegistry = new ServiceInstance { LocalCacheExpirationInMinutes = 5 }
        };
        options.Value.Returns(config);

        _sut = new SubmodelDescriptorProvider(_logger, options, _cachedHttp);
    }

    [Fact]
    public async Task GetDataForSubmodelDescriptorByIdAsync_ReturnsSubmodelDesciptor_WhenResponseIsSuccessful()
    {
        const string Id = "ContactInformation";
        var expectedDescriptor = new SubmodelDescriptor { Id = "ContactInformation" };
        _cachedHttp.GetStringAsync(Arg.Any<string>(), HttpClientNames.SubmodelRegistry, Arg.Any<int>(), Arg.Any<CancellationToken>())
                   .Returns("{ \"id\": \"ContactInformation\" }");

        var result = await _sut.GetDataForSubmodelDescriptorByIdAsync(Id, CancellationToken.None);

        Assert.Equal(expectedDescriptor.Id, result.Id);
    }

    [Fact]
    public async Task GetDataForSubmodelDescriptorByIdAsync_DeserializesAasNestedTypes_WhenResponseContainsAasPayload()
    {
        const string id = "https://mm-software.com/submodel/nameplate";
        const string jsonResponse = """
                                    {
                                        "description": [
                                            {
                                                "language": "en",
                                                "text": "Nameplate Submodel Template"
                                            }
                                        ],
                                        "displayName": [
                                            {
                                                "language": "en",
                                                "text": "Nameplate"
                                            }
                                        ],
                                        "extensions": [
                                            {
                                                "name": "templateSource",
                                                "valueType": "xs:string",
                                                "value": "Nameplate"
                                            }
                                        ],
                                        "administration": {
                                            "version": "1",
                                            "revision": "0"
                                        },
                                        "idShort": "Nameplate",
                                        "id": "https://mm-software.com/submodel/nameplate",
                                        "semanticId": {
                                            "type": "ExternalReference",
                                            "keys": [
                                                {
                                                    "type": "GlobalReference",
                                                    "value": "https://admin-shell.io/zvei/nameplate/2/0/Nameplate"
                                                }
                                            ]
                                        }
                                    }
                                    """;

        _cachedHttp.GetStringAsync(Arg.Any<string>(), HttpClientNames.SubmodelRegistry, Arg.Any<int>(), Arg.Any<CancellationToken>())
                   .Returns(jsonResponse);

        var result = await _sut.GetDataForSubmodelDescriptorByIdAsync(id, CancellationToken.None);

        Assert.NotNull(result.Description);
        Assert.Equal("en", result.Description![0].Language);
        Assert.Equal("Nameplate Submodel Template", result.Description[0].Text);

        Assert.NotNull(result.DisplayName);
        Assert.Equal("Nameplate", result.DisplayName![0].Text);

        Assert.NotNull(result.Extensions);
        Assert.Equal("templateSource", result.Extensions![0].Name);
        Assert.Equal(DataTypeDefXsd.String, result.Extensions[0].ValueType);
        Assert.Equal("Nameplate", result.Extensions[0].Value);

        Assert.NotNull(result.Administration);
        Assert.Equal("1", result.Administration!.Version);
        Assert.Equal("0", result.Administration.Revision);

        Assert.NotNull(result.SemanticId);
        Assert.Equal(ReferenceTypes.ExternalReference, result.SemanticId!.Type);
        Assert.Equal("https://admin-shell.io/zvei/nameplate/2/0/Nameplate", result.SemanticId.Keys[0].Value);
    }

    [Fact]
    public async Task GetDataForSubmodelDescriptorByIdAsync_ReturnsSubmodelDescriptor_WhenResponseIsWrappedInResultProperty()
    {
        const string id = "https://mm-software.com/submodel/wrapped";
        const string jsonResponse = """
                                    {
                                        "result": {
                                            "idShort": "WrappedSubmodel",
                                            "id": "https://mm-software.com/submodel/wrapped",
                                            "description": [
                                                {
                                                    "language": "de",
                                                    "text": "Eingewickeltes Submodell"
                                                }
                                            ]
                                        }
                                    }
                                    """;

        _cachedHttp.GetStringAsync(Arg.Any<string>(), HttpClientNames.SubmodelRegistry, Arg.Any<int>(), Arg.Any<CancellationToken>())
                   .Returns(jsonResponse);

        var result = await _sut.GetDataForSubmodelDescriptorByIdAsync(id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("WrappedSubmodel", result.IdShort);
        Assert.Equal("https://mm-software.com/submodel/wrapped", result.Id);
        Assert.NotNull(result.Description);
        Assert.Single(result.Description!);
        Assert.Equal("de", result.Description![0].Language);
        Assert.Equal("Eingewickeltes Submodell", result.Description[0].Text);
    }

    [Fact]
    public async Task GetDataForSubmodelDescriptorByIdAsync_ReturnsDescriptor_WhenOnlyPrimitiveFieldsArePresent()
    {
        const string id = "simple-id";
        const string jsonResponse = """{ "id": "simple-id", "idShort": "SimpleSubmodel" }""";

        _cachedHttp.GetStringAsync(Arg.Any<string>(), HttpClientNames.SubmodelRegistry, Arg.Any<int>(), Arg.Any<CancellationToken>())
                   .Returns(jsonResponse);

        var result = await _sut.GetDataForSubmodelDescriptorByIdAsync(id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("simple-id", result.Id);
        Assert.Equal("SimpleSubmodel", result.IdShort);
        Assert.Null(result.Description);
        Assert.Null(result.DisplayName);
        Assert.Null(result.Extensions);
        Assert.Null(result.Administration);
        Assert.Null(result.SemanticId);
    }

    [Fact]
    public async Task GetDataForSubmodelDescriptorByIdAsync_DeserializesSupplementalSemanticId_WhenPresent()
    {
        const string id = "https://mm-software.com/submodel/supplemental";
        const string jsonResponse = """
                                    {
                                        "id": "https://mm-software.com/submodel/supplemental",
                                        "idShort": "SupplementalSubmodel",
                                        "supplementalSemanticId": [
                                            {
                                                "type": "ExternalReference",
                                                "keys": [
                                                    {
                                                        "type": "GlobalReference",
                                                        "value": "https://admin-shell.io/supplemental/1/0"
                                                    }
                                                ]
                                            }
                                        ]
                                    }
                                    """;

        _cachedHttp.GetStringAsync(Arg.Any<string>(), HttpClientNames.SubmodelRegistry, Arg.Any<int>(), Arg.Any<CancellationToken>())
                   .Returns(jsonResponse);

        var result = await _sut.GetDataForSubmodelDescriptorByIdAsync(id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(result.SupplementalSemanticId);
        Assert.Single(result.SupplementalSemanticId!);
        Assert.Equal("https://admin-shell.io/supplemental/1/0", result.SupplementalSemanticId![0].Keys[0].Value);
    }

    [Fact]
    public async Task GetDataForSubmodelDescriptorByIdAsync_UsesBase64UrlEncodedIdInRequestPath()
    {
        const string id = "https://mm-software.com/submodel/nameplate";
        var requestPath = string.Empty;
        const string jsonResponse = """{ "id": "https://mm-software.com/submodel/nameplate", "idShort": "Nameplate" }""";

        _cachedHttp.GetStringAsync(Arg.Any<string>(), HttpClientNames.SubmodelRegistry, Arg.Any<int>(), Arg.Any<CancellationToken>())
                   .Returns(info =>
                   {
                       requestPath = info.ArgAt<string>(0);
                       return jsonResponse;
                   });

        _ = await _sut.GetDataForSubmodelDescriptorByIdAsync(id, CancellationToken.None);

        var expectedEncoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(id))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        Assert.Contains(expectedEncoded, requestPath, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetDataForSubmodelDescriptorByIdAsync_ThrowsResponseParsingException_WhenDeserializationFails()
    {
        const string Id = "ContactInformation";
        _cachedHttp.GetStringAsync(Arg.Any<string>(), HttpClientNames.SubmodelRegistry, Arg.Any<int>(), Arg.Any<CancellationToken>())
                   .Returns("This is not valid JSON");

        await Assert.ThrowsAsync<ResponseParsingException>(() => _sut.GetDataForSubmodelDescriptorByIdAsync(Id, CancellationToken.None));
    }

    [Fact]
    public async Task GetDataForSubmodelDescriptorByIdAsync_ThrowsResponseParsingException_WhenDeserializedObjectIsNull()
    {
        const string Id = "null-object-id";
        _cachedHttp.GetStringAsync(Arg.Any<string>(), HttpClientNames.SubmodelRegistry, Arg.Any<int>(), Arg.Any<CancellationToken>())
                   .Returns("null");

        await Assert.ThrowsAsync<ResponseParsingException>(() => _sut.GetDataForSubmodelDescriptorByIdAsync(Id, CancellationToken.None));
    }

    [Fact]
    public async Task GetDataForSubmodelDescriptorByIdAsync_ThrowsResourceNotFoundException_WhenResourceNotFoundIsThrown()
    {
        const string Id = "test-id";
        _cachedHttp.GetStringAsync(Arg.Any<string>(), HttpClientNames.SubmodelRegistry, Arg.Any<int>(), Arg.Any<CancellationToken>())
                   .ThrowsAsync(new ResourceNotFoundException());

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _sut.GetDataForSubmodelDescriptorByIdAsync(Id, CancellationToken.None));
    }

    [Fact]
    public async Task GetDataForSubmodelDescriptorByIdAsync_ThrowsServiceAuthorizationException_WhenUnauthorizedIsThrown()
    {
        const string Id = "auth-fail-id";
        _cachedHttp.GetStringAsync(Arg.Any<string>(), HttpClientNames.SubmodelRegistry, Arg.Any<int>(), Arg.Any<CancellationToken>())
                   .ThrowsAsync(new UnauthorizedAccessException());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _sut.GetDataForSubmodelDescriptorByIdAsync(Id, CancellationToken.None));
    }

    [Fact]
    public async Task GetDataForSubmodelDescriptorByIdAsync_ThrowsRequestTimeoutException_WhenTimeoutIsThrown()
    {
        const string Id = "timeout-id";
        _cachedHttp.GetStringAsync(Arg.Any<string>(), HttpClientNames.SubmodelRegistry, Arg.Any<int>(), Arg.Any<CancellationToken>())
                   .ThrowsAsync(new RequestTimeoutException());

        await Assert.ThrowsAsync<RequestTimeoutException>(() =>
            _sut.GetDataForSubmodelDescriptorByIdAsync(Id, CancellationToken.None));
    }

    [Fact]
    public async Task GetDataForSubmodelDescriptorByIdAsync_ThrowsValidationFailedException_WhenValidationFailedIsThrown()
    {
        const string Id = "badrequest-id";
        _cachedHttp.GetStringAsync(Arg.Any<string>(), HttpClientNames.SubmodelRegistry, Arg.Any<int>(), Arg.Any<CancellationToken>())
                   .ThrowsAsync(new ValidationFailedException());

        await Assert.ThrowsAsync<ValidationFailedException>(() =>
            _sut.GetDataForSubmodelDescriptorByIdAsync(Id, CancellationToken.None));
    }
}
