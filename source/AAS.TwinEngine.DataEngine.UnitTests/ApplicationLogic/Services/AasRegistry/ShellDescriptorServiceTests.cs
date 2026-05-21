using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasEnvironment.Providers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasRegistry;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin;
using AAS.TwinEngine.DataEngine.DomainModel.AasRegistry;
using AAS.TwinEngine.DataEngine.DomainModel.Plugin;
using AAS.TwinEngine.DataEngine.DomainModel.Shared;

using AasCore.Aas3_0;

using Microsoft.Extensions.Logging;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace AAS.TwinEngine.DataEngine.UnitTests.ApplicationLogic.Services.AasRegistry;

public class ShellDescriptorServiceTests
{
    private readonly ITemplateProvider _templateProvider = Substitute.For<ITemplateProvider>();
    private readonly IShellTemplateMappingProvider _shellTemplateMappingProvider = Substitute.For<IShellTemplateMappingProvider>();
    private readonly IPluginDataHandler _pluginDataHandler = Substitute.For<IPluginDataHandler>();
    private readonly IShellDescriptorDataHandler _dataHandler = Substitute.For<IShellDescriptorDataHandler>();
    private readonly IPluginManifestConflictHandler _pluginManifestConflictHandler = Substitute.For<IPluginManifestConflictHandler>();
    private readonly ILogger<ShellDescriptorService> _logger = Substitute.For<ILogger<ShellDescriptorService>>();
    private readonly ShellDescriptorService _sut;

    public ShellDescriptorServiceTests() => _sut = new ShellDescriptorService(_templateProvider, _shellTemplateMappingProvider, _dataHandler, _pluginDataHandler, _pluginManifestConflictHandler, _logger);

    [Fact]
    public async Task GetAllShellDescriptorsAsync_ReturnsFilledShellDescriptors()
    {
        var cancellationToken = CancellationToken.None;
        var template = GetShellDescriptorTemplate();
        var metaData = new ShellDescriptorsMetaData
        {
            PagingMetaData = new PagingMetaData { Cursor = "nextCursor" },
            ShellDescriptors = GetShellDescriptorDataList()
        };
        var expected = GetExpectedShellDescriptors();

        _shellTemplateMappingProvider.GetTemplateId(Arg.Any<string>()).Returns("template-1");
        _templateProvider.GetShellDescriptorTemplateAsync("template-1", cancellationToken).Returns(template);

        var manifests = new List<PluginManifest>
        {
         new()
         {
            PluginName = "TestPlugin",
            PluginUrl = new Uri("http://test-plugin"),
            SupportedSemanticIds = [],
            Capabilities = new Capabilities { HasShellDescriptor = true }
         }
        };
        _pluginManifestConflictHandler.Manifests.Returns(manifests);

        _pluginDataHandler.GetDataForAllShellDescriptorsAsync(1, null, manifests, cancellationToken).Returns(metaData);

        _dataHandler.FillOut(template, metaData.ShellDescriptors[0]).Returns(expected[0]);

        var result = await _sut.GetAllShellDescriptorsAsync(1, null, cancellationToken);

        Assert.NotNull(result);
        Assert.NotNull(result.Result);
        Assert.Single(result.Result);
        Assert.False(string.IsNullOrWhiteSpace(result.PagingMetaData?.Cursor));
    }

    [Fact]
    public async Task GetAllShellDescriptorsAsync_ReturnsAll_WhenLimitIsNull()
    {
        var cancellationToken = CancellationToken.None;
        var template = GetShellDescriptorTemplate();
        var shellDescriptorMetaData = Enumerable.Range(1, 3)
            .Select(i => new ShellDescriptorMetaData { Id = $"id{i}" })
            .ToList();

        var metaData = new ShellDescriptorsMetaData
        {
            PagingMetaData = null,
            ShellDescriptors = shellDescriptorMetaData
        };

        var filled = Enumerable.Range(1, 3)
            .Select(i => new ShellDescriptor { Id = $"id{i}" })
            .ToList();

        _shellTemplateMappingProvider.GetTemplateId(Arg.Any<string>()).Returns("template-1");
        _templateProvider.GetShellDescriptorTemplateAsync("template-1", cancellationToken).Returns(template);
        _pluginManifestConflictHandler.Manifests.Returns(new List<PluginManifest>());
        _pluginDataHandler.GetDataForAllShellDescriptorsAsync(null, null, Arg.Any<List<PluginManifest>>(), cancellationToken)
            .Returns(metaData);
        _dataHandler.FillOut(template, Arg.Any<ShellDescriptorMetaData>())
            .Returns(callInfo =>
            {
                var value = callInfo.ArgAt<ShellDescriptorMetaData>(1);
                return filled.Single(x => x.Id == value.Id);
            });

        var result = await _sut.GetAllShellDescriptorsAsync(null, null, cancellationToken);

        Assert.NotNull(result);
        Assert.NotNull(result.Result);
        Assert.Equal(3, result.Result.Count);
        Assert.Null(result.PagingMetaData?.Cursor);
    }

    [Fact]
    public async Task GetAllShellDescriptorsAsync_ReturnsEmptyResult_WhenShellDescriptorsMetadataIsNull()
    {
        var cancellationToken = CancellationToken.None;
        var manifests = new List<PluginManifest>();
        var metaData = new ShellDescriptorsMetaData
        {
            PagingMetaData = new PagingMetaData { Cursor = "nextCursor" },
            ShellDescriptors = null
        };

        _pluginManifestConflictHandler.Manifests.Returns(manifests);
        _pluginDataHandler.GetDataForAllShellDescriptorsAsync(null, null, manifests, cancellationToken)
            .Returns(metaData);

        var result = await _sut.GetAllShellDescriptorsAsync(null, null, cancellationToken);

        Assert.NotNull(result);
        Assert.NotNull(result.Result);
        Assert.Empty(result.Result);
        Assert.Equal("nextCursor", result.PagingMetaData?.Cursor);
    }

    [Fact]
    public async Task GetAllShellDescriptorsAsync_UsesTemplatePerShellId_WhenMultipleIdsReturned()
    {
        var cancellationToken = CancellationToken.None;
        var metaData = new ShellDescriptorsMetaData
        {
            PagingMetaData = null,
            ShellDescriptors =
            [
                new ShellDescriptorMetaData { Id = "id1" },
                new ShellDescriptorMetaData { Id = "id2" },
                new ShellDescriptorMetaData { Id = "id3" },
                new ShellDescriptorMetaData { Id = "id4" }
            ]
        };

        var manifests = new List<PluginManifest>();
        _pluginManifestConflictHandler.Manifests.Returns(manifests);
        _pluginDataHandler.GetDataForAllShellDescriptorsAsync(null, null, manifests, cancellationToken).Returns(metaData);

        _shellTemplateMappingProvider.GetTemplateId("id1").Returns("template-1");
        _shellTemplateMappingProvider.GetTemplateId("id2").Returns("template-2");
        _shellTemplateMappingProvider.GetTemplateId("id3").Returns("template-3");
        _shellTemplateMappingProvider.GetTemplateId("id4").Returns("template-4");

        _templateProvider.GetShellDescriptorTemplateAsync("template-1", cancellationToken).Returns(GetShellDescriptorTemplate());
        _templateProvider.GetShellDescriptorTemplateAsync("template-2", cancellationToken).Returns(GetShellDescriptorTemplate());
        _templateProvider.GetShellDescriptorTemplateAsync("template-3", cancellationToken).Returns(GetShellDescriptorTemplate());
        _templateProvider.GetShellDescriptorTemplateAsync("template-4", cancellationToken).Returns(GetShellDescriptorTemplate());

        _dataHandler.FillOut(Arg.Any<ShellDescriptor>(), Arg.Any<ShellDescriptorMetaData>())
            .Returns(callInfo =>
            {
                var value = callInfo.ArgAt<ShellDescriptorMetaData>(1);
                return new ShellDescriptor { Id = value.Id };
            });

        var result = await _sut.GetAllShellDescriptorsAsync(null, null, cancellationToken);

        Assert.NotNull(result);
        Assert.NotNull(result.Result);
        Assert.Equal(4, result.Result.Count);
        _shellTemplateMappingProvider.Received(1).GetTemplateId("id1");
        _shellTemplateMappingProvider.Received(1).GetTemplateId("id2");
        _shellTemplateMappingProvider.Received(1).GetTemplateId("id3");
        _shellTemplateMappingProvider.Received(1).GetTemplateId("id4");
        await _templateProvider.Received(1).GetShellDescriptorTemplateAsync("template-1", cancellationToken);
        await _templateProvider.Received(1).GetShellDescriptorTemplateAsync("template-2", cancellationToken);
        await _templateProvider.Received(1).GetShellDescriptorTemplateAsync("template-3", cancellationToken);
        await _templateProvider.Received(1).GetShellDescriptorTemplateAsync("template-4", cancellationToken);
    }

    [Fact]
    public async Task GetShellDescriptorByIdAsync_ReturnsFilledShellDescriptor()
    {
        var cancellationToken = CancellationToken.None;
        const string Id = "aasId";
        var template = GetShellDescriptorTemplate();
        var metaData = GetShellDescriptorData();
        var expected = GetExpectedShellDescriptor();
        _shellTemplateMappingProvider.GetTemplateId(Arg.Any<string>()).Returns("template-1");
        _templateProvider.GetShellDescriptorTemplateAsync("template-1", cancellationToken).Returns(template);
        var manifests = new List<PluginManifest>
        {
            new()
            {
                PluginName = "TestPlugin",
                PluginUrl = new Uri("http://test-plugin"),
                SupportedSemanticIds = [],
                Capabilities = new Capabilities { HasShellDescriptor = true }
            }
        };
        _pluginManifestConflictHandler.Manifests.Returns(manifests);
        _pluginDataHandler.GetDataForShellDescriptorAsync(manifests, Id, cancellationToken).Returns(metaData);
        _dataHandler.FillOut(template, metaData).Returns(expected);

        var result = await _sut.GetShellDescriptorByIdAsync(Id, cancellationToken);

        Assert.Equal(expected, result);
    }
    [Fact]
    public async Task GetShellDescriptorByIdAsync_ShouldThrowException_WhenManifestConflict()
    {
        _pluginDataHandler.GetDataForShellDescriptorAsync(Arg.Any<IReadOnlyList<PluginManifest>>(), "aasId", Arg.Any<CancellationToken>())
                          .Throws(new MultiPluginConflictException());

        await Assert.ThrowsAsync<InternalDataProcessingException>(() => _sut.GetShellDescriptorByIdAsync("aasId", CancellationToken.None));
    }

    [Fact]
    public async Task GetShellDescriptorByIdAsync_ShouldThrowException_WhenInvalidRequest()
    {
        _pluginDataHandler.GetDataForShellDescriptorAsync(Arg.Any<IReadOnlyList<PluginManifest>>(), "aasId", Arg.Any<CancellationToken>())
                          .Throws(new PluginMetaDataInvalidRequestException());
        await Assert.ThrowsAsync<InvalidUserInputException>(() => _sut.GetShellDescriptorByIdAsync("aasId", CancellationToken.None));
    }

    [Fact]
    public async Task GetAllShellDescriptorsAsync_ShouldThrowException_WhenManifestConflict()
    {
        _pluginDataHandler.GetDataForAllShellDescriptorsAsync(null, null, Arg.Any<IReadOnlyList<PluginManifest>>(), Arg.Any<CancellationToken>()).Throws(new MultiPluginConflictException());
        await Assert.ThrowsAsync<InternalDataProcessingException>(() => _sut.GetAllShellDescriptorsAsync(null, null, CancellationToken.None));
    }

    [Fact]
    public async Task GetAllShellDescriptorsAsync_ShouldThrowInternalDataProcessingException_WhenValidationFailedException()
    {
        _pluginDataHandler
            .GetDataForAllShellDescriptorsAsync(null, null, Arg.Any<IReadOnlyList<PluginManifest>>(), Arg.Any<CancellationToken>())
            .Throws(new ValidationFailedException());

        await Assert.ThrowsAsync<InternalDataProcessingException>(() => _sut.GetAllShellDescriptorsAsync(null, null, CancellationToken.None));
    }

    [Fact]
    public async Task GetAllShellDescriptorsAsync_ShouldSkipDescriptor_WhenMetadataIdMissing()
    {
        var manifests = new List<PluginManifest>();
        var metaData = new ShellDescriptorsMetaData
        {
            PagingMetaData = null,
            ShellDescriptors =
            [
                new ShellDescriptorMetaData { Id = null }
            ]
        };

        _pluginManifestConflictHandler.Manifests.Returns(manifests);
        _pluginDataHandler.GetDataForAllShellDescriptorsAsync(null, null, manifests, Arg.Any<CancellationToken>())
            .Returns(metaData);

        var result = await _sut.GetAllShellDescriptorsAsync(null, null, CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(result.Result);
        Assert.Empty(result.Result);
    }

    [Fact]
    public async Task GetAllShellDescriptorsAsync_ShouldSkipDescriptor_WhenTemplateMappingFails()
    {
        var manifests = new List<PluginManifest>();
        var metaData = new ShellDescriptorsMetaData
        {
            PagingMetaData = null,
            ShellDescriptors =
            [
                new ShellDescriptorMetaData { Id = "id1" }
            ]
        };

        _pluginManifestConflictHandler.Manifests.Returns(manifests);
        _pluginDataHandler.GetDataForAllShellDescriptorsAsync(null, null, manifests, Arg.Any<CancellationToken>())
            .Returns(metaData);
        _shellTemplateMappingProvider.GetTemplateId("id1").Throws(new ResourceNotFoundException());

        var result = await _sut.GetAllShellDescriptorsAsync(null, null, CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(result.Result);
        Assert.Empty(result.Result);
    }

    [Fact]
    public async Task GetShellDescriptorByIdAsync_ShouldThrowShellDescriptorNotFoundException_WhenTemplateNotFound()
    {
        var cancellationToken = CancellationToken.None;
        const string id = "aasId";
        var manifests = new List<PluginManifest>();
        var metaData = new ShellDescriptorMetaData { Id = id };

        _pluginManifestConflictHandler.Manifests.Returns(manifests);
        _pluginDataHandler.GetDataForShellDescriptorAsync(manifests, id, cancellationToken).Returns(metaData);
        _shellTemplateMappingProvider.GetTemplateId(id).Returns("template-1");
        _templateProvider.GetShellDescriptorTemplateAsync("template-1", cancellationToken).Throws(new ResourceNotFoundException());

        await Assert.ThrowsAsync<ShellDescriptorNotFoundException>(() => _sut.GetShellDescriptorByIdAsync(id, cancellationToken));
    }

    #region Test Data Helpers

    private static List<ShellDescriptorMetaData> GetShellDescriptorDataList()
    => [
        new()
        {
            GlobalAssetId = "GlobalAssetId_SensorWeatherStation",
            IdShort = "idShort1",
            Id = "SensorWeatherStation",
            SpecificAssetIds =
            [
                new SpecificAssetId
                (
                    "idShort1Name",
                    "idShort1Value"
                )
            ],
            Href = "http://endpoint1.com"
        }
    ];

    private static ShellDescriptorMetaData GetShellDescriptorData() => new()
    {
        GlobalAssetId = "GlobalAssetId_ContactInformation",
        IdShort = "idShort2",
        Id = "ContactInformation",
        SpecificAssetIds =
        [
            new SpecificAssetId
            (
               "idShort1Name", "idShort1Value"
            )
        ],
        Href = "http://endpoint1.com"
    };

    private static ShellDescriptor GetShellDescriptorTemplate() => new()
    {
        Id = "ContactInformation",
        IdShort = "idShort2",
        GlobalAssetId = "GlobalAssetId_ContactInformation",
        SpecificAssetIds = null,
        Endpoints =
        [
            new EndpointData() {
                ProtocolInformation = new ProtocolInformationData() { Href = "http://endpoint123.com" }
            }
        ]
    };

    private static ShellDescriptor GetExpectedShellDescriptor() => new()
    {
        Id = "ContactInformation",
        IdShort = "idShort2",
        GlobalAssetId = "GlobalAssetId_ContactInformation",
        SpecificAssetIds = null,
        Endpoints =
        [
            new EndpointData() {
                ProtocolInformation = new ProtocolInformationData() { Href = "http://endpoint123.com" }
            }
        ]
    };

    private static List<ShellDescriptor> GetExpectedShellDescriptors() => [
        new()
        {
            Id = "ContactInformation",
            IdShort = "idShort2",
            GlobalAssetId = "GlobalAssetId_ContactInformation",
            SpecificAssetIds = null,
            Endpoints =
            [
                new EndpointData()
                {
                    ProtocolInformation = new ProtocolInformationData()
                    {
                        Href = "http://endpoint123.com"
                    }
                }
            ]
        }
    ];

    #endregion
}
