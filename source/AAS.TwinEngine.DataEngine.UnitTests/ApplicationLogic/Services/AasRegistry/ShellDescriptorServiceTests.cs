using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasEnvironment.Providers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasRegistry;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasRepository;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRegistry;
using AAS.TwinEngine.DataEngine.DomainModel.AasRegistry;
using AAS.TwinEngine.DataEngine.DomainModel.AasRepository;
using AAS.TwinEngine.DataEngine.DomainModel.Plugin;
using AAS.TwinEngine.DataEngine.DomainModel.Shared;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRegistry;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using AasCore.Aas3_1;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
    private readonly ISubmodelDescriptorService _submodelDescriptorService = Substitute.For<ISubmodelDescriptorService>();
    private readonly IAasRepositoryService _aasRepositoryService = Substitute.For<IAasRepositoryService>();
    private readonly ILogger<ShellDescriptorService> _logger = Substitute.For<ILogger<ShellDescriptorService>>();
    private readonly IOptions<GeneralConfig> _generalConfig;
    private readonly IOptions<TemplateManagementConfig> _templateManagementConfig;
    private readonly ShellDescriptorService _sut;

    public ShellDescriptorServiceTests()
    {
        var general = new GeneralConfig
        {
            CustomerDomainUrl = new Uri("https://mm-software.com/"),
            DataEngineRepositoryBaseUrl = new Uri("http://localhost:8080/")
        };
        _generalConfig = Options.Create(general);

        var config = new TemplateManagementConfig
        {
            AasTemplateRegistry = new ServiceInstance { ConcurrentOperationsLimit = 10 }
        };
        _templateManagementConfig = Options.Create(config);
        _sut = new ShellDescriptorService(_templateProvider, _shellTemplateMappingProvider, _dataHandler, _pluginDataHandler, _pluginManifestConflictHandler, _logger, _templateManagementConfig, _submodelDescriptorService, _aasRepositoryService, _generalConfig);
    }

    [Fact]
    public async Task GetSubmodelDescriptorByAasIdAsync_ValidatesOwnershipAndReturnsDescriptor()
    {
        const string aasId = "aas-1";
        const string submodelId = "submodel-1";
        var expected = new SubmodelDescriptor { Id = submodelId };

        _submodelDescriptorService.GetSubmodelDescriptorByIdAsync(submodelId, Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.GetSubmodelDescriptorByAasIdAsync(aasId, submodelId, CancellationToken.None);

        Assert.Same(expected, result);
        await _aasRepositoryService.Received(1)
            .ValidateSubmodelBelongsToAasAsync(aasId, submodelId, Arg.Any<CancellationToken>());
        await _submodelDescriptorService.Received(1)
            .GetSubmodelDescriptorByIdAsync(submodelId, Arg.Any<CancellationToken>());
    }

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

        _pluginDataHandler.GetDataForAllShellDescriptorsAsync(1, null, null, null, manifests, cancellationToken).Returns(metaData);

        _dataHandler.FillOut(template, metaData.ShellDescriptors[0]).Returns(expected[0]);

        var result = await _sut.GetAllShellDescriptorsAsync(1, null, null, null, cancellationToken);

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
        _pluginDataHandler.GetDataForAllShellDescriptorsAsync(Arg.Any<int>(), null, null, null, Arg.Any<List<PluginManifest>>(), cancellationToken)
            .Returns(metaData);
        _dataHandler.FillOut(template, Arg.Any<ShellDescriptorMetaData>())
            .Returns(callInfo =>
            {
                var value = callInfo.ArgAt<ShellDescriptorMetaData>(1);
                return filled.Single(x => x.Id == value.Id);
            });

        var result = await _sut.GetAllShellDescriptorsAsync(100, null, null, null, cancellationToken);

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
        _pluginDataHandler.GetDataForAllShellDescriptorsAsync(Arg.Any<int>(), null, null, null, manifests, cancellationToken)
            .Returns(metaData);

        var result = await _sut.GetAllShellDescriptorsAsync(100, null, null, null, cancellationToken);

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
        _pluginDataHandler.GetDataForAllShellDescriptorsAsync(Arg.Any<int>(), null, null, null, manifests, cancellationToken).Returns(metaData);

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

        var result = await _sut.GetAllShellDescriptorsAsync(100, null, null, null, cancellationToken);

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
        _pluginDataHandler.GetDataForAllShellDescriptorsAsync(Arg.Any<int>(), null, null, null, Arg.Any<IReadOnlyList<PluginManifest>>(), Arg.Any<CancellationToken>()).Throws(new MultiPluginConflictException());
        await Assert.ThrowsAsync<InternalDataProcessingException>(() => _sut.GetAllShellDescriptorsAsync(100, null, null, null, CancellationToken.None));
    }

    [Fact]
    public async Task GetAllShellDescriptorsAsync_ShouldThrowInternalDataProcessingException_WhenValidationFailedException()
    {
        _pluginDataHandler
            .GetDataForAllShellDescriptorsAsync(Arg.Any<int>(), null, null, null, Arg.Any<IReadOnlyList<PluginManifest>>(), Arg.Any<CancellationToken>())
            .Throws(new ValidationFailedException());

        await Assert.ThrowsAsync<InternalDataProcessingException>(() => _sut.GetAllShellDescriptorsAsync(100, null, null, null, CancellationToken.None));
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
        _pluginDataHandler.GetDataForAllShellDescriptorsAsync(
                Arg.Any<int>(), Arg.Is<string?>(c => c == null), Arg.Is<AssetKind?>(k => k == null), Arg.Is<string?>(t => t == null),
                Arg.Is<IReadOnlyList<PluginManifest>>(m => m == manifests), Arg.Any<CancellationToken>())
            .Returns(metaData);
        // Throw ResourceNotFoundException for any id so TryBuildShellDescriptorAsync skips the null-id descriptor
        _shellTemplateMappingProvider.GetTemplateId(Arg.Any<string?>()).Throws(new ResourceNotFoundException());

        var result = await _sut.GetAllShellDescriptorsAsync(100, null, null, null, CancellationToken.None);

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
        _pluginDataHandler.GetDataForAllShellDescriptorsAsync(
                Arg.Any<int>(), Arg.Is<string?>(c => c == null), Arg.Is<AssetKind?>(k => k == null), Arg.Is<string?>(t => t == null),
                Arg.Is<IReadOnlyList<PluginManifest>>(m => m == manifests), Arg.Any<CancellationToken>())
            .Returns(metaData);
        _shellTemplateMappingProvider.GetTemplateId("id1").Throws(new ResourceNotFoundException());

        var result = await _sut.GetAllShellDescriptorsAsync(100, null, null, null, CancellationToken.None);

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
    [Fact]
    public async Task GetAllShellDescriptorsAsync_BuildsDescriptorsInParallel()
    {
        var cancellationToken = CancellationToken.None;
        var metadataList = Enumerable.Range(1, 5)
            .Select(i => new ShellDescriptorMetaData { Id = $"id{i}" })
            .ToList();
        var metaData = new ShellDescriptorsMetaData
        {
            PagingMetaData = null,
            ShellDescriptors = metadataList
        };

        // No manifests -> no filter capability -> filters passed through (no client-side fallback needed for null filters)
        _pluginManifestConflictHandler.Manifests.Returns(new List<PluginManifest>());
        _pluginDataHandler.GetDataForAllShellDescriptorsAsync(Arg.Any<int>(), null, null, null, Arg.Any<IReadOnlyList<PluginManifest>>(), cancellationToken)
            .Returns(metaData);
        _shellTemplateMappingProvider.GetTemplateId(Arg.Any<string>()).Returns("template-1");
        _templateProvider.GetShellDescriptorTemplateAsync("template-1", cancellationToken).Returns(GetShellDescriptorTemplate());
        _dataHandler.FillOut(Arg.Any<ShellDescriptor>(), Arg.Any<ShellDescriptorMetaData>())
            .Returns(callInfo =>
            {
                var value = callInfo.ArgAt<ShellDescriptorMetaData>(1);
                return new ShellDescriptor { Id = value.Id };
            });

        var result = await _sut.GetAllShellDescriptorsAsync(100, null, null, null, cancellationToken);

        Assert.NotNull(result);
        Assert.NotNull(result.Result);
        Assert.Equal(5, result.Result.Count);
        Assert.All(result.Result, descriptor => Assert.False(string.IsNullOrWhiteSpace(descriptor.Id)));
        await _pluginDataHandler.Received(1).GetDataForAllShellDescriptorsAsync(
            Arg.Is<int>(l => l == 100),
            Arg.Is<string?>(c => c == null),
            Arg.Is<AssetKind?>(k => k == null),
            Arg.Is<string?>(t => t == null),
            Arg.Any<IReadOnlyList<PluginManifest>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAllShellDescriptorsAsync_WithFilterCapablePlugin_ForwardsAssetKindAndAssetTypeToPlugin()
    {
        var cancellationToken = CancellationToken.None;
        var manifests = new List<PluginManifest>
        {
            new()
            {
                PluginName = "PluginA",
                PluginUrl = new Uri("http://plugin-a"),
                SupportedSemanticIds = [],
                Capabilities = new Capabilities { HasShellDescriptor = true, HasAssetKindTypeFilter = true }
            }
        };

        var metaData = new ShellDescriptorsMetaData
        {
            PagingMetaData = null,
            ShellDescriptors =
            [
                new ShellDescriptorMetaData { Id = "id1" },
                new ShellDescriptorMetaData { Id = "id2" }
            ]
        };

        _pluginManifestConflictHandler.Manifests.Returns(manifests);
        _pluginDataHandler
            .GetDataForAllShellDescriptorsAsync(100, null, AssetKind.Instance, "YXR0cmlidXRl", manifests, cancellationToken)
            .Returns(metaData);

        _shellTemplateMappingProvider.GetTemplateId(Arg.Any<string>()).Returns("template-1");
        _templateProvider.GetShellDescriptorTemplateAsync("template-1", cancellationToken).Returns(GetShellDescriptorTemplate());
        _dataHandler.FillOut(Arg.Any<ShellDescriptor>(), Arg.Any<ShellDescriptorMetaData>())
            .Returns(callInfo =>
            {
                var value = callInfo.ArgAt<ShellDescriptorMetaData>(1);
                return new ShellDescriptor { Id = value.Id, AssetKind = AssetKind.Instance };
            });

        var result = await _sut.GetAllShellDescriptorsAsync(100, null, AssetKind.Instance, "YXR0cmlidXRl", cancellationToken);

        Assert.NotNull(result);
        Assert.NotNull(result.Result);
        Assert.Equal(2, result.Result.Count);
        // Verify filters were forwarded to plugin (not applied client-side)
        await _pluginDataHandler.Received(1).GetDataForAllShellDescriptorsAsync(
            Arg.Is<int>(l => l == 100),
            Arg.Is<string?>(c => c == null),
            Arg.Is<AssetKind?>(k => k == AssetKind.Instance),
            Arg.Is<string>(t => t == "YXR0cmlidXRl"),
            Arg.Is<IReadOnlyList<PluginManifest>>(m => m == manifests),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAllShellDescriptorsAsync_WithNonFilterCapablePlugin_DoesNotForwardFiltersToPlugin()
    {
        var cancellationToken = CancellationToken.None;
        var manifests = new List<PluginManifest>
        {
            new()
            {
                PluginName = "PluginA",
                PluginUrl = new Uri("http://plugin-a"),
                SupportedSemanticIds = [],
                Capabilities = new Capabilities { HasShellDescriptor = true, HasAssetKindTypeFilter = false }
            }
        };

        var page = new ShellDescriptorsMetaData
        {
            PagingMetaData = new PagingMetaData { Cursor = null },
            ShellDescriptors =
            [
                new ShellDescriptorMetaData { Id = "id1" },
                new ShellDescriptorMetaData { Id = "id2" }
            ]
        };

        _pluginManifestConflictHandler.Manifests.Returns(manifests);
        _pluginDataHandler
            .GetDataForAllShellDescriptorsAsync(
                Arg.Any<int>(),
                Arg.Is<string?>(c => c == null),
                Arg.Is<AssetKind?>(k => k == null),
                Arg.Is<string?>(t => t == null),
                Arg.Is<IReadOnlyList<PluginManifest>>(m => m == manifests),
                Arg.Any<CancellationToken>())
            .Returns(page);

        _shellTemplateMappingProvider.GetTemplateId(Arg.Any<string>()).Returns("template-1");
        _templateProvider.GetShellDescriptorTemplateAsync("template-1", cancellationToken).Returns(GetShellDescriptorTemplate());
        _dataHandler.FillOut(Arg.Any<ShellDescriptor>(), Arg.Any<ShellDescriptorMetaData>())
            .Returns(callInfo =>
            {
                var meta = callInfo.ArgAt<ShellDescriptorMetaData>(1);
                return new ShellDescriptor { Id = meta.Id, AssetKind = AssetKind.Instance };
            });

        await _sut.GetAllShellDescriptorsAsync(100, null, AssetKind.Instance, null, cancellationToken);

        // Verify the plugin was called WITHOUT filters (client-side fallback path)
        await _pluginDataHandler.Received(1).GetDataForAllShellDescriptorsAsync(
            Arg.Any<int>(),
            Arg.Is<string?>(c => c == null),
            Arg.Is<AssetKind?>(k => k == null),
            Arg.Is<string?>(t => t == null),
            Arg.Is<IReadOnlyList<PluginManifest>>(m => m == manifests),
            Arg.Any<CancellationToken>());
        // Verify filters were NOT forwarded to the plugin
        await _pluginDataHandler.DidNotReceive().GetDataForAllShellDescriptorsAsync(
            Arg.Any<int>(),
            Arg.Any<string?>(),
            Arg.Is<AssetKind?>(k => k == AssetKind.Instance),
            Arg.Any<string?>(),
            Arg.Is<IReadOnlyList<PluginManifest>>(m => m == manifests),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAllShellDescriptorsAsync_RespectsMaxConcurrency()
    {
        var cancellationToken = CancellationToken.None;
        const int concurrencyLimit = 2;
        var config = new TemplateManagementConfig
        {
            AasTemplateRegistry = new ServiceInstance { ConcurrentOperationsLimit = concurrencyLimit }
        };
        var sut = new ShellDescriptorService(
            _templateProvider, _shellTemplateMappingProvider, _dataHandler,
            _pluginDataHandler, _pluginManifestConflictHandler, _logger, Options.Create(config), _submodelDescriptorService, _aasRepositoryService,
            _generalConfig);

        var currentConcurrency = 0;
        var maxObservedConcurrency = 0;
        var lockObj = new object();

        var metadataList = Enumerable.Range(1, 6)
            .Select(i => new ShellDescriptorMetaData { Id = $"id{i}" })
            .ToList();
        var metaData = new ShellDescriptorsMetaData
        {
            PagingMetaData = null,
            ShellDescriptors = metadataList
        };

        _pluginManifestConflictHandler.Manifests.Returns(new List<PluginManifest>());
        _pluginDataHandler.GetDataForAllShellDescriptorsAsync(Arg.Any<int>(), null, null, null, Arg.Any<IReadOnlyList<PluginManifest>>(), cancellationToken)
            .Returns(metaData);
        _shellTemplateMappingProvider.GetTemplateId(Arg.Any<string>()).Returns("template-1");

        _templateProvider.GetShellDescriptorTemplateAsync("template-1", cancellationToken)
            .Returns(async callInfo =>
            {
                lock (lockObj)
                {
                    currentConcurrency++;
                    if (currentConcurrency > maxObservedConcurrency)
                        maxObservedConcurrency = currentConcurrency;
                }

                await Task.Delay(50, cancellationToken);

                lock (lockObj)
                {
                    currentConcurrency--;
                }

                return GetShellDescriptorTemplate();
            });

        _dataHandler.FillOut(Arg.Any<ShellDescriptor>(), Arg.Any<ShellDescriptorMetaData>())
            .Returns(callInfo =>
            {
                var value = callInfo.ArgAt<ShellDescriptorMetaData>(1);
                return new ShellDescriptor { Id = value.Id };
            });

        var result = await sut.GetAllShellDescriptorsAsync(100, null, null, null, cancellationToken);

        Assert.NotNull(result);
        Assert.Equal(6, result.Result!.Count);
        Assert.True(maxObservedConcurrency <= concurrencyLimit,
            $"Expected max concurrency <= {concurrencyLimit}, but observed {maxObservedConcurrency}");
    }

    [Fact]
    public async Task GetAllShellDescriptorsAsync_WithAssetKindFilterAndNoFilterCapablePlugins_UsesClientSideFallbackWithProgressivePaging()
    {
        var cancellationToken = CancellationToken.None;
        var manifests = new List<PluginManifest>
        {
            new()
            {
                PluginName = "PluginA",
                PluginUrl = new Uri("http://plugin-a"),
                SupportedSemanticIds = [],
                Capabilities = new Capabilities { HasShellDescriptor = true, HasAssetKindTypeFilter = false }
            }
        };

        var firstPage = new ShellDescriptorsMetaData
        {
            PagingMetaData = new PagingMetaData { Cursor = "cursor-1" },
            ShellDescriptors =
            [
                new ShellDescriptorMetaData { Id = "id1" },
                new ShellDescriptorMetaData { Id = "id2" }
            ]
        };

        var secondPage = new ShellDescriptorsMetaData
        {
            PagingMetaData = new PagingMetaData { Cursor = null },
            ShellDescriptors =
            [
                new ShellDescriptorMetaData { Id = "id3" }
            ]
        };

        _pluginManifestConflictHandler.Manifests.Returns(manifests);
        _pluginDataHandler
            .GetDataForAllShellDescriptorsAsync(2, null, null, null, manifests, cancellationToken)
            .Returns(firstPage);
        _pluginDataHandler
            .GetDataForAllShellDescriptorsAsync(20, "cursor-1", null, null, manifests, cancellationToken)
            .Returns(secondPage);

        _shellTemplateMappingProvider.GetTemplateId(Arg.Any<string>()).Returns("template-1");
        _templateProvider.GetShellDescriptorTemplateAsync("template-1", cancellationToken).Returns(GetShellDescriptorTemplate());

        _dataHandler.FillOut(Arg.Any<ShellDescriptor>(), Arg.Any<ShellDescriptorMetaData>())
            .Returns(callInfo =>
            {
                var meta = callInfo.ArgAt<ShellDescriptorMetaData>(1);
                return meta.Id switch
                {
                    "id1" => new ShellDescriptor { Id = "id1", AssetKind = AssetKind.Type },
                    "id2" => new ShellDescriptor { Id = "id2", AssetKind = AssetKind.Instance },
                    "id3" => new ShellDescriptor { Id = "id3", AssetKind = AssetKind.Instance },
                    _ => new ShellDescriptor { Id = meta.Id }
                };
            });

        var result = await _sut.GetAllShellDescriptorsAsync(2, null, AssetKind.Instance, null, cancellationToken);

        Assert.NotNull(result);
        Assert.NotNull(result.Result);
        Assert.Equal(2, result.Result.Count);
        Assert.All(result.Result, descriptor => Assert.Equal(AssetKind.Instance, descriptor.AssetKind));
        Assert.Equal(["id2", "id3"], result.Result.Select(x => x.Id).ToArray());
        Assert.Null(result.PagingMetaData?.Cursor);

        await _pluginDataHandler.Received(1).GetDataForAllShellDescriptorsAsync(2, null, null, null, manifests, cancellationToken);
        await _pluginDataHandler.Received(1).GetDataForAllShellDescriptorsAsync(20, "cursor-1", null, null, manifests, cancellationToken);
    }

    [Fact]
    public async Task GetAllShellDescriptorsAsync_WithAssetTypeFilterAndNoFilterCapablePlugins_FiltersClientSide()
    {
        var cancellationToken = CancellationToken.None;
        var manifests = new List<PluginManifest>
        {
            new()
            {
                PluginName = "PluginA",
                PluginUrl = new Uri("http://plugin-a"),
                SupportedSemanticIds = [],
                Capabilities = new Capabilities { HasShellDescriptor = true, HasAssetKindTypeFilter = false }
            }
        };

        var page = new ShellDescriptorsMetaData
        {
            PagingMetaData = new PagingMetaData { Cursor = null },
            ShellDescriptors =
            [
                new ShellDescriptorMetaData { Id = "idA" },
                new ShellDescriptorMetaData { Id = "idB" }
            ]
        };

        _pluginManifestConflictHandler.Manifests.Returns(manifests);
        _pluginDataHandler
            .GetDataForAllShellDescriptorsAsync(100, null, null, null, manifests, cancellationToken)
            .Returns(page);

        _shellTemplateMappingProvider.GetTemplateId(Arg.Any<string>()).Returns("template-1");
        _templateProvider.GetShellDescriptorTemplateAsync("template-1", cancellationToken).Returns(GetShellDescriptorTemplate());

        _dataHandler.FillOut(Arg.Any<ShellDescriptor>(), Arg.Any<ShellDescriptorMetaData>())
            .Returns(callInfo =>
            {
                var meta = callInfo.ArgAt<ShellDescriptorMetaData>(1);
                return meta.Id switch
                {
                    "idA" => new ShellDescriptor { Id = "idA", AssetType = "Instance" },
                    "idB" => new ShellDescriptor { Id = "idB", AssetType = "Type" },
                    _ => new ShellDescriptor { Id = meta.Id }
                };
            });

        var encodedAssetType = "Instance".EncodeBase64Url();
        var result = await _sut.GetAllShellDescriptorsAsync(100, null, null, encodedAssetType, cancellationToken);

        Assert.NotNull(result);
        Assert.NotNull(result.Result);
        Assert.Single(result.Result);
        Assert.Equal("idA", result.Result[0].Id);

        await _pluginDataHandler.Received(1).GetDataForAllShellDescriptorsAsync(100, null, null, null, manifests, cancellationToken);
    }

    [Fact]
    public async Task GetShellDescriptorByIdAsync_UpdatesSubmodelDescriptorIdAndHref_BasedOnShellProductId()
    {
        var cancellationToken = CancellationToken.None;
        const string shellId = "https://mm-software.com/ids/aas/000-001";
        var metadata = new ShellDescriptorMetaData { Id = shellId, Href = "http://localhost:8080/shells/test" };
        var template = new ShellDescriptor
        {
            Id = shellId,
            Endpoints =
            [
                new EndpointData { ProtocolInformation = new ProtocolInformationData { Href = "http://localhost:8080/shells/test" } }
            ],
            SubmodelDescriptors =
            [
                new SubmodelDescriptor
                {
                    Id = "Nameplate",
                    Endpoints =
                    [
                        new EndpointData
                        {
                            Interface = "SUBMODEL-3.0",
                            ProtocolInformation = new ProtocolInformationData { Href = "http://localhost:8082/submodels/TmFtZXBsYXRl" }
                        }
                    ]
                }
            ]
        };

        var manifests = new List<PluginManifest>();
        _pluginManifestConflictHandler.Manifests.Returns(manifests);
        _pluginDataHandler.GetDataForShellDescriptorAsync(manifests, shellId, cancellationToken).Returns(metadata);
        _shellTemplateMappingProvider.GetTemplateId(shellId).Returns("template-1");
        _shellTemplateMappingProvider.GetProductIdFromRule(shellId).Returns("000-001");
        _templateProvider.GetShellDescriptorTemplateAsync("template-1", cancellationToken).Returns(template);
        _dataHandler.FillOut(template, metadata).Returns(template);

        var result = await _sut.GetShellDescriptorByIdAsync(shellId, cancellationToken);

        Assert.NotNull(result);
        Assert.NotNull(result.SubmodelDescriptors);

        var updatedSubmodel = Assert.Single(result.SubmodelDescriptors!);
        var expectedId = "https://mm-software.com/submodel/000-001/Nameplate";
        var expectedHref = $"http://localhost:8080/submodels/{expectedId.EncodeBase64Url()}";

        Assert.Equal(expectedId, updatedSubmodel.Id);
        Assert.NotNull(updatedSubmodel.Endpoints);
        Assert.Equal(expectedHref, updatedSubmodel.Endpoints![0].ProtocolInformation!.Href);
    }


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
}
