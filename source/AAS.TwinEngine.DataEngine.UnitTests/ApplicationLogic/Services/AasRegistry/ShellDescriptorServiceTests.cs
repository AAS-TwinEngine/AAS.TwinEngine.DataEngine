using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasEnvironment.Providers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasRegistry;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasRegistry.Providers;
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
    private readonly IPluginDataHandler _pluginDataHandler = Substitute.For<IPluginDataHandler>();
    private readonly IShellDescriptorDataHandler _dataHandler = Substitute.For<IShellDescriptorDataHandler>();
    private readonly IAasRegistryProvider _aasRegistryProvider = Substitute.For<IAasRegistryProvider>();
    private readonly ILogger<ShellDescriptorService> _logger = Substitute.For<ILogger<ShellDescriptorService>>();
    private readonly IPluginManifestConflictHandler _pluginManifestConflictHandler = Substitute.For<IPluginManifestConflictHandler>();
    private readonly ShellDescriptorService _sut;

    public ShellDescriptorServiceTests() => _sut = new ShellDescriptorService(_templateProvider, _dataHandler, _pluginDataHandler, _aasRegistryProvider, _logger, _pluginManifestConflictHandler);

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

        _templateProvider.GetShellDescriptorsTemplateAsync(cancellationToken).Returns(template);

        var manifests = new List<PluginManifest>
        {
         new()
         {
            PluginName = "TestPlugin",
            PluginUrl = new Uri("http://test-plugin"),
            SupportedSemanticIds = new List<string>(),
            Capabilities = new Capabilities { HasShellDescriptor = true }
         }
        };
        _pluginManifestConflictHandler.Manifests.Returns(manifests);

        _pluginDataHandler.GetDataForAllShellDescriptorsAsync(1, null, manifests, cancellationToken).Returns(metaData);

        _dataHandler.FillOut(template, metaData.ShellDescriptors).Returns(expected);

        var result = await _sut.GetAllShellDescriptorsAsync(1, null, cancellationToken);

        Assert.NotNull(result);
        Assert.NotNull(result.Result);
        Assert.Equal(1, result.Result.Count);
        Assert.False(string.IsNullOrWhiteSpace(result.PagingMetaData?.Cursor));
    }

    [Fact]
    public async Task GetAllShellDescriptorsAsync_ReturnsAll_WhenLimitIsNull()
    {
        var cancellationToken = CancellationToken.None;
        var template = GetShellDescriptorTemplate();
        var metaData = new ShellDescriptorsMetaData
        {
            PagingMetaData = null,
            ShellDescriptors = GetShellDescriptorDataList()
        };

        var filled = Enumerable.Range(1, 3)
            .Select(i => new ShellDescriptor { Id = $"id{i}" })
            .ToList();

        _templateProvider.GetShellDescriptorsTemplateAsync(cancellationToken).Returns(template);
        _pluginManifestConflictHandler.Manifests.Returns(new List<PluginManifest>());
        _pluginDataHandler.GetDataForAllShellDescriptorsAsync(null, null, Arg.Any<List<PluginManifest>>(), cancellationToken)
            .Returns(metaData);
        _dataHandler.FillOut(template, metaData.ShellDescriptors).Returns(filled);

        var result = await _sut.GetAllShellDescriptorsAsync(null, null, cancellationToken);

        Assert.NotNull(result);
        Assert.NotNull(result.Result);
        Assert.Equal(3, result.Result.Count);
        Assert.Null(result.PagingMetaData?.Cursor);
    }

    [Fact]
    public async Task GetShellDescriptorByIdAsync_ReturnsFilledShellDescriptor()
    {
        var cancellationToken = CancellationToken.None;
        const string Id = "aasId";
        var template = GetShellDescriptorTemplate();
        var metaData = GetShellDescriptorData();
        var expected = GetExpectedShellDescriptor();
        _templateProvider.GetShellDescriptorsTemplateAsync(cancellationToken).Returns(template);
        var manifests = new List<PluginManifest>
        {
            new()
            {
                PluginName = "TestPlugin",
                PluginUrl = new Uri("http://test-plugin"),
                SupportedSemanticIds = new List<string>(),
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
    public async Task SyncShellDescriptorsAsync_ShouldUpdate_ExistingDescriptors()
    {
        var filled = new List<ShellDescriptor>
        {
            new() { Id = "1" }
        };
        var existing = new ShellDescriptor { Id = "1" };
        var metaData = new ShellDescriptorsMetaData
        {
            PagingMetaData = new PagingMetaData { Cursor = "nextCursor" },
            ShellDescriptors = [new ShellDescriptorMetaData { Id = "1" }]
        };

        _aasRegistryProvider.GetAllAsync(Arg.Any<CancellationToken>()).Returns([existing]);
        var manifests = new List<PluginManifest>
        {
            new()
            {
                PluginName = "TestPlugin",
                PluginUrl = new Uri("http://test-plugin"),
                SupportedSemanticIds = new List<string>(),
                Capabilities = new Capabilities { HasShellDescriptor = true }
            }
        };
        _pluginManifestConflictHandler.Manifests.Returns(manifests);
        _pluginDataHandler.GetDataForAllShellDescriptorsAsync(null, null, manifests, Arg.Any<CancellationToken>()).Returns(metaData);
        _dataHandler.FillOut(existing, metaData.ShellDescriptors).Returns(filled);

        await _sut.SyncShellDescriptorsAsync(CancellationToken.None);

        await _aasRegistryProvider.Received(1).PutAsync("1", null, Arg.Any<CancellationToken>());
        await _aasRegistryProvider.DidNotReceive().CreateAsync(Arg.Any<ShellDescriptor>(), Arg.Any<CancellationToken>());
        await _aasRegistryProvider.DidNotReceive().DeleteByIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SyncShellDescriptorsAsync_ShouldCreate_NewDescriptors()
    {
        // Arrange
        var metaData = new ShellDescriptorsMetaData
        {
            PagingMetaData = new PagingMetaData { Cursor = "nextCursor" },
            ShellDescriptors = [new ShellDescriptorMetaData { Id = "2" }]
        };
        var template = new ShellDescriptor { Id = "template" };
        var filled = new List<ShellDescriptor>
        {
            new() { Id = "2" }
        };

        _aasRegistryProvider.GetAllAsync(Arg.Any<CancellationToken>()).Returns([]);
        var manifests = new List<PluginManifest>
        {
            new()
            {
                PluginName = "TestPlugin",
                PluginUrl = new Uri("http://test-plugin"),
                SupportedSemanticIds = new List<string>(),
                Capabilities = new Capabilities { HasShellDescriptor = true }
            }
        };
        _pluginManifestConflictHandler.Manifests.Returns(manifests);
        _pluginDataHandler.GetDataForAllShellDescriptorsAsync(null, null, manifests, Arg.Any<CancellationToken>()).Returns(metaData);
        _templateProvider.GetShellDescriptorsTemplateAsync(Arg.Any<CancellationToken>()).Returns(template);
        _dataHandler.FillOut(template, metaData.ShellDescriptors).Returns(filled);

        // Act
        await _sut.SyncShellDescriptorsAsync(CancellationToken.None);

        // Assert
        await _aasRegistryProvider.Received(1).CreateAsync(Arg.Any<ShellDescriptor>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SyncShellDescriptorsAsync_ShouldDelete_MissingDescriptors()
    {
        // Arrange
        var existing = new ShellDescriptor { Id = "1" };
        var metaData = new ShellDescriptorsMetaData
        {
            PagingMetaData = new PagingMetaData { Cursor = "nextCursor" },
            ShellDescriptors = []
        };
        _aasRegistryProvider.GetAllAsync(Arg.Any<CancellationToken>()).Returns([existing]);
        var manifests = new List<PluginManifest>
        {
            new()
            {
                PluginName = "TestPlugin",
                PluginUrl = new Uri("http://test-plugin"),
                SupportedSemanticIds = new List<string>(),
                Capabilities = new Capabilities { HasShellDescriptor = true }
            }
        };
        _pluginManifestConflictHandler.Manifests.Returns(manifests);
        _pluginDataHandler.GetDataForAllShellDescriptorsAsync(null, null, manifests, Arg.Any<CancellationToken>()).Returns(metaData);

        // Act
        await _sut.SyncShellDescriptorsAsync(CancellationToken.None);

        // Assert
        await _aasRegistryProvider.Received(1).DeleteByIdAsync("1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SyncShellDescriptorsAsync_ShouldHandle_EmptyListsGracefully()
    {
        // Arrange
        var metaData = new ShellDescriptorsMetaData
        {
            PagingMetaData = new PagingMetaData { Cursor = "nextCursor" },
            ShellDescriptors = []
        };
        _aasRegistryProvider.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns([]);

        _pluginDataHandler.GetDataForAllShellDescriptorsAsync(null, null, Arg.Any<IReadOnlyList<PluginManifest>>(), Arg.Any<CancellationToken>()).Returns(metaData);

        // Act
        var ex = await Record.ExceptionAsync(() => _sut.SyncShellDescriptorsAsync(CancellationToken.None));

        // Assert
        Assert.Null(ex);
        await _aasRegistryProvider.DidNotReceive().CreateAsync(Arg.Any<ShellDescriptor>(), Arg.Any<CancellationToken>());
        await _aasRegistryProvider.DidNotReceive().DeleteByIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _aasRegistryProvider.DidNotReceive().PutAsync(Arg.Any<string>(), Arg.Any<ShellDescriptor>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SyncShellDescriptorsAsync_WhenRegistryDescriptorHasNoId_ShouldLogAndReturn()
    {
        var metaData = new ShellDescriptorsMetaData
        {
            PagingMetaData = new PagingMetaData(),
            ShellDescriptors = [new ShellDescriptorMetaData { Id = "valid" }]
        };

        _aasRegistryProvider
            .GetAllAsync(Arg.Any<CancellationToken>())
            .Returns([new ShellDescriptor { Id = "" }]);

        _pluginDataHandler
            .GetDataForAllShellDescriptorsAsync(null, null, Arg.Any<IReadOnlyList<PluginManifest>>(), Arg.Any<CancellationToken>())
            .Returns(metaData);

        var exception = await Record.ExceptionAsync(
            () => _sut.SyncShellDescriptorsAsync(CancellationToken.None));

        Assert.Null(exception);

        await _aasRegistryProvider
            .DidNotReceive()
            .CreateAsync(Arg.Any<ShellDescriptor>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SyncShellDescriptorsAsync_WhenRegistryThrowsResourceNotFoundException_ShouldNotThrow()
    {
        _aasRegistryProvider
            .GetAllAsync(Arg.Any<CancellationToken>())
            .Throws(new ResourceNotFoundException());

        var exception = await Record.ExceptionAsync(
            () => _sut.SyncShellDescriptorsAsync(CancellationToken.None));

        Assert.Null(exception);
    }

    [Fact]
    public async Task SyncShellDescriptorsAsync_WhenRegistryThrowsResponseParsingException_ShouldNotThrow()
    {
        _aasRegistryProvider
            .GetAllAsync(Arg.Any<CancellationToken>())
            .Throws(new ResponseParsingException());

        var exception = await Record.ExceptionAsync(
            () => _sut.SyncShellDescriptorsAsync(CancellationToken.None));

        Assert.Null(exception);
    }

    [Fact]
    public async Task SyncShellDescriptorsAsync_WhenRegistryThrowsRequestTimeoutException_ShouldNotThrow()
    {
        _aasRegistryProvider
            .GetAllAsync(Arg.Any<CancellationToken>())
            .Throws(new RequestTimeoutException());

        var exception = await Record.ExceptionAsync(
            () => _sut.SyncShellDescriptorsAsync(CancellationToken.None));

        Assert.Null(exception);
    }

    [Fact]
    public async Task SyncShellDescriptorsAsync_WhenPluginMetadataHasNoId_ShouldLogAndReturn()
    {
        var metaData = new ShellDescriptorsMetaData
        {
            PagingMetaData = new PagingMetaData(),
            ShellDescriptors = [new ShellDescriptorMetaData { Id = "" }]
        };

        _aasRegistryProvider
            .GetAllAsync(Arg.Any<CancellationToken>())
            .Returns([new ShellDescriptor { Id = "1" }]);

        _pluginDataHandler
            .GetDataForAllShellDescriptorsAsync(null, null, Arg.Any<IReadOnlyList<PluginManifest>>(), Arg.Any<CancellationToken>())
            .Returns(metaData);

        var exception = await Record.ExceptionAsync(
            () => _sut.SyncShellDescriptorsAsync(CancellationToken.None));

        Assert.Null(exception);
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
