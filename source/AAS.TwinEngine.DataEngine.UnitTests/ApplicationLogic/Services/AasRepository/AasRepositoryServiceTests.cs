using AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Requests;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Base;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasRepository;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Shared.Providers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository;
using AAS.TwinEngine.DataEngine.DomainModel.AasRegistry;
using AAS.TwinEngine.DataEngine.DomainModel.AasRepository;
using AAS.TwinEngine.DataEngine.DomainModel.Plugin;
using AAS.TwinEngine.DataEngine.DomainModel.Shared;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using AasCore.Aas3_1;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace AAS.TwinEngine.DataEngine.UnitTests.ApplicationLogic.Services.AasRepository;

public class AasRepositoryServiceTests
{
    private readonly IAasRepositoryTemplateService _templateService = Substitute.For<IAasRepositoryTemplateService>();
    private readonly IPluginDataHandler _pluginDataHandler = Substitute.For<IPluginDataHandler>();
    private readonly IPluginManifestConflictHandler _pluginManifestConflictHandler = Substitute.For<IPluginManifestConflictHandler>();
    private readonly ISubmodelRepositoryService _submodelRepositoryService = Substitute.For<ISubmodelRepositoryService>();
    private readonly ILogger<AasRepositoryService> _logger = Substitute.For<ILogger<AasRepositoryService>>();
    private readonly IOptions<TemplateManagementConfig> _templateManagementConfig = Substitute.For<IOptions<TemplateManagementConfig>>();
    private readonly IFileContentProvider _fileContentProvider = Substitute.For<IFileContentProvider>();
    private readonly IOptions<GeneralConfig> _generalConfig = Substitute.For<IOptions<GeneralConfig>>();
    private readonly AasRepositoryService _sut;
    private const string AasIdentifier = "test-id";

    public AasRepositoryServiceTests()
    {
        _templateManagementConfig.Value.Returns(new TemplateManagementConfig
        {
            AasTemplateRepository = new ServiceInstance
            {
                ConcurrentOperationsLimit = 10
            }
        });
        _generalConfig.Value.Returns(new GeneralConfig
        {
            MaxFileAttachmentSizeBytes = 1000000
        });

        _sut = new AasRepositoryService(
            _logger,
            _templateService,
            _pluginDataHandler,
            _pluginManifestConflictHandler,
            _fileContentProvider,
            _templateManagementConfig,
            _submodelRepositoryService,
            _generalConfig);
    }

    [Fact]
    public async Task ValidateSubmodelBelongsToAasAsync_WithMatchingSubmodelId_DoesNotThrow()
    {
        _templateService.GetSubmodelRefByIdAsync(AasIdentifier, Arg.Any<CancellationToken>())
            .Returns(
            [
                new Reference(
                    ReferenceTypes.ModelReference,
                    [new Key(KeyTypes.Submodel, "submodel-1")],
                    null)
            ]);

        await _sut.ValidateSubmodelBelongsToAasAsync(AasIdentifier, "submodel-1", CancellationToken.None);
    }

    [Fact]
    public async Task ValidateSubmodelBelongsToAasAsync_WithoutMatchingSubmodelId_ThrowsSubmodelNotFoundException()
    {
        _templateService.GetSubmodelRefByIdAsync(AasIdentifier, Arg.Any<CancellationToken>())
            .Returns(
            [
                new Reference(
                    ReferenceTypes.ModelReference,
                    [new Key(KeyTypes.Submodel, "other-submodel")],
                    null)
            ]);

        await Assert.ThrowsAsync<SubmodelNotFoundException>(() =>
            _sut.ValidateSubmodelBelongsToAasAsync(AasIdentifier, "submodel-1", CancellationToken.None));
    }

    [Fact]
    public async Task GetSubmodelByAasIdAsync_WhenSubmodelBelongsToAas_ForwardsLevelAndExtentToSubmodelRepository()
    {
        const string submodelId = "submodel-1";
        _templateService.GetSubmodelRefByIdAsync(AasIdentifier, Arg.Any<CancellationToken>())
            .Returns(
            [
                new Reference(
                    ReferenceTypes.ModelReference,
                    [new Key(KeyTypes.Submodel, submodelId)],
                    null)
            ]);

        var expectedSubmodel = new Submodel(submodelId);
        _submodelRepositoryService.GetSubmodelAsync(
                submodelId,
                Arg.Is<SubmodelQueryOptions>(o => o.Level == Level.core.ToString() && o.Extent == Extent.withBlobValue.ToString()),
                Arg.Any<CancellationToken>())
            .Returns(expectedSubmodel);

        var result = await _sut.GetSubmodelByAasIdAsync(
            AasIdentifier,
            submodelId,
            new SubmodelQueryOptions(Level.core.ToString(), Extent.withBlobValue.ToString()),
            CancellationToken.None);

        Assert.Same(expectedSubmodel, result);
        await _submodelRepositoryService.Received(1)
            .GetSubmodelAsync(
                submodelId,
                Arg.Is<SubmodelQueryOptions>(o => o.Level == Level.core.ToString() && o.Extent == Extent.withBlobValue.ToString()),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetShellByIdAsync_ShouldReturnShellWithAssetInformation()
    {
        var cancellationToken = CancellationToken.None;

        var shellTemplate = CreateShellTemplate();
        var assetInfoTemplate = CreateAssetInformationTemplate();
        var pluginData = CreateAssetData();
        var manifests = new List<PluginManifest>
       {
           new()
           {
               PluginName = "PluginA",
               PluginUrl = new Uri("http://plugin-a"),
               SupportedSemanticIds = ["id-1"],
               Capabilities = new Capabilities { HasAssetInformation = true }
           }
       };

        _templateService.GetShellTemplateAsync(AasIdentifier, cancellationToken).Returns(shellTemplate);
        _templateService.GetAssetInformationTemplateAsync(AasIdentifier, cancellationToken).Returns(assetInfoTemplate);
        _pluginManifestConflictHandler.Manifests.Returns(manifests);
        _pluginDataHandler
            .GetDataForAssetInformationByIdAsync(manifests, AasIdentifier, cancellationToken)
            .Returns(pluginData);

        var result = await _sut.GetShellByIdAsync(AasIdentifier, cancellationToken);

        Assert.NotNull(result);
        Assert.NotNull(result.AssetInformation);
        Assert.Equal(pluginData.GlobalAssetId, result.AssetInformation.GlobalAssetId);
    }

    [Fact]
    public async Task GetAssetInformationByIdAsync_ShouldReturnMergedAssetInformation()
    {
        var cancellationToken = CancellationToken.None;

        var template = CreateAssetInformationTemplate();
        var pluginData = CreateAssetData();
        var manifests = new List<PluginManifest>
       {
           new()
           {
               PluginName = "PluginB",
               PluginUrl = new Uri("http://plugin-b"),
               SupportedSemanticIds = ["id-2"],
               Capabilities = new Capabilities { HasAssetInformation = true }
           }
       };

        _templateService.GetAssetInformationTemplateAsync(AasIdentifier, cancellationToken)
            .Returns(template);
        _pluginManifestConflictHandler.Manifests.Returns(manifests);
        _pluginDataHandler
            .GetDataForAssetInformationByIdAsync(manifests, AasIdentifier, cancellationToken)
            .Returns(pluginData);

        var result = await _sut.GetAssetInformationByIdAsync(AasIdentifier, cancellationToken);

        Assert.NotNull(result);
        Assert.Equal(pluginData.GlobalAssetId, result.GlobalAssetId);
        Assert.NotNull(result.DefaultThumbnail);
        Assert.Equal(pluginData.DefaultThumbnail?.Path, result.DefaultThumbnail?.Path);
        Assert.Equal(pluginData.DefaultThumbnail?.ContentType, result.DefaultThumbnail?.ContentType);
    }

    [Fact]
    public async Task GetSubmodelRefByIdAsync_ShouldReturnAllRefs_WhenLimitIsNull()
    {
        var cancellationToken = CancellationToken.None;
        var expectedRefs = CreateSubmodelRefs(3);
        _templateService.GetSubmodelRefByIdAsync(AasIdentifier, cancellationToken).Returns(expectedRefs);

        var result = await _sut.GetSubmodelRefByIdAsync(AasIdentifier, 100, null, cancellationToken);

        Assert.Equal(3, result.Result!.Count);
        await _templateService.Received(1).GetSubmodelRefByIdAsync(AasIdentifier, cancellationToken);
    }

    [Fact]
    public async Task GetSubmodelRefByIdAsync_ShouldReturnLimitedRefs_WhenLimitIsLessThanTotal()
    {
        var cancellationToken = CancellationToken.None;
        var expectedRefs = CreateSubmodelRefs(5);
        _templateService.GetSubmodelRefByIdAsync(AasIdentifier, cancellationToken).Returns(expectedRefs);

        var result = await _sut.GetSubmodelRefByIdAsync(AasIdentifier, 2, null, cancellationToken);

        Assert.Equal(2, result.Result!.Count);
    }

    [Fact]
    public async Task GetSubmodelRefByIdAsync_ShouldReturnAllRefs_WhenLimitIsGreaterThanTotal()
    {
        var cancellationToken = CancellationToken.None;
        var expectedRefs = CreateSubmodelRefs(2);
        _templateService.GetSubmodelRefByIdAsync(AasIdentifier, cancellationToken).Returns(expectedRefs);

        var result = await _sut.GetSubmodelRefByIdAsync(AasIdentifier, 10, null, cancellationToken);

        Assert.Equal(2, result.Result!.Count);
    }

    [Fact]
    public async Task GetSubmodelRefByIdAsync_ReturnsEmptyList_WhenNoRefsExist()
    {
        var cancellationToken = CancellationToken.None;
        _templateService.GetSubmodelRefByIdAsync(AasIdentifier, cancellationToken)!.Returns([]);

        var result = await _sut.GetSubmodelRefByIdAsync(AasIdentifier, 10, null, cancellationToken);

        Assert.NotNull(result);
        Assert.Empty(result.Result!);
    }

    [Fact]
    public async Task GetSubmodelRefByIdAsync_WithCursor_ReturnsItemsAfterCursor()
    {
        var cancellationToken = CancellationToken.None;
        var expectedRefs = CreateSubmodelRefs(5);
        _templateService.GetSubmodelRefByIdAsync(AasIdentifier, cancellationToken).Returns(expectedRefs);
        var cursor = "urn:uuid:submodel-1".EncodeBase64Url();

        var result = await _sut.GetSubmodelRefByIdAsync(AasIdentifier, 2, cursor, cancellationToken);

        Assert.NotNull(result);
        Assert.NotNull(result.Result);
        Assert.Equal(2, result.Result.Count);
        var firstValue = result.Result[0].Keys.FirstOrDefault()?.Value;
        var secondValue = result.Result[1].Keys.FirstOrDefault()?.Value;
        Assert.Equal("urn:uuid:submodel-2", firstValue);
        Assert.Equal("urn:uuid:submodel-3", secondValue);
        Assert.False(string.IsNullOrWhiteSpace(result.PagingMetaData?.Cursor));
        var decodedNext = result.PagingMetaData!.Cursor!.DecodeBase64Url();
        Assert.Equal("urn:uuid:submodel-3", decodedNext);
    }

    [Fact]
    public async Task GetSubmodelRefByIdAsync_WithInvalidCursor_ReturnsBadRequest()
    {
        var cancellationToken = CancellationToken.None;
        var expectedRefs = CreateSubmodelRefs(4);

        _templateService.GetSubmodelRefByIdAsync(AasIdentifier, cancellationToken).Returns(expectedRefs);

        var invalidCursor = "nonExistingId".EncodeBase64Url();
        await Assert.ThrowsAsync<BadRequestException>(() => _sut.GetSubmodelRefByIdAsync(AasIdentifier, 2, invalidCursor, cancellationToken));
    }

    [Fact]
    public async Task GetAssetInformationByIdAsync_ShouldThrowTemplateNotFound_WhenResourceNotFound()
    {
        _templateService.GetAssetInformationTemplateAsync(AasIdentifier, Arg.Any<CancellationToken>())
            .Throws(new ResourceNotFoundException());

        await Assert.ThrowsAsync<AssetInformationNotFoundException>(() => _sut.GetAssetInformationByIdAsync(AasIdentifier, CancellationToken.None));
    }

    [Fact]
    public async Task GetAssetInformationByIdAsync_ShouldThrowInternalDataProcessing_WhenResponseParsingFails()
    {
        _templateService.GetAssetInformationTemplateAsync(AasIdentifier, Arg.Any<CancellationToken>())
            .Throws(new ResponseParsingException());

        await Assert.ThrowsAsync<InternalDataProcessingException>(
            () => _sut.GetAssetInformationByIdAsync(AasIdentifier, CancellationToken.None));
    }

    [Fact]
    public async Task GetAssetInformationByIdAsync_ShouldThrowRepositoryNotAvailable_WhenTimeoutOccurs()
    {
        _templateService.GetAssetInformationTemplateAsync(AasIdentifier, Arg.Any<CancellationToken>())
            .Throws(new RequestTimeoutException());

        await Assert.ThrowsAsync<PluginNotAvailableException>(() => _sut.GetAssetInformationByIdAsync(AasIdentifier, CancellationToken.None));
    }

    [Fact]
    public async Task GetAssetInformationByIdAsync_ShouldThrowException_WhenManifestConflict()
    {
        _pluginDataHandler.GetDataForAssetInformationByIdAsync(Arg.Any<IReadOnlyList<PluginManifest>>(), "aasId", Arg.Any<CancellationToken>())
                          .Throws(new MultiPluginConflictException());

        await Assert.ThrowsAsync<InternalDataProcessingException>(() =>
                                                                      _sut.GetAssetInformationByIdAsync("aasId", CancellationToken.None));
    }

    [Fact]
    public async Task GetAssetInformationByIdAsync_ShouldThrowException_WhenInvalidRequest()
    {
        _pluginDataHandler.GetDataForAssetInformationByIdAsync(Arg.Any<IReadOnlyList<PluginManifest>>(), "aasId", Arg.Any<CancellationToken>())
                          .Throws(new PluginMetaDataInvalidRequestException());

        await Assert.ThrowsAsync<InvalidUserInputException>(() =>
                                                                _sut.GetAssetInformationByIdAsync("aasId", CancellationToken.None));
    }

    [Fact]
    public async Task GetShellsByFiltersAsync_ShouldKeepAllTemplateSpecificAssetIds_WhenMetadataContainsSubset()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;

        var shellTemplate = new AssetAdministrationShell(
            id: "aas-1",
            assetInformation: new AssetInformation(
                assetKind: AssetKind.Instance,
                specificAssetIds:
                [
                    new SpecificAssetId("ManufacturerId", "OldManufacturer"),
                   new SpecificAssetId("SerialNumber", "OldSerial"),
                   new SpecificAssetId("AssetTag", "Asset-001")
                ]),
            submodels: []);

        var metadata = new ShellDescriptorMetaData
        {
            Id = "aas-1",
            SpecificAssetIds =
            [
                new SpecificAssetId("ManufacturerId", "NewManufacturer"),
               new SpecificAssetId("SerialNumber", "NewSerial")
            ]
        };

        var manifests = new List<PluginManifest>();

        _pluginManifestConflictHandler.Manifests.Returns(manifests);

        _templateService.GetShellTemplateAsync("aas-1", cancellationToken).Returns(shellTemplate);

        _pluginDataHandler
            .GetDataForAllShellDescriptorsAsync(
                Arg.Any<int>(),
                null,
                null,
                null,
                manifests,
                cancellationToken)
            .Returns(new ShellDescriptorsMetaData
            {
                ShellDescriptors = [metadata],
                PagingMetaData = new PagingMetaData()
            });

        // Act
        var result = await _sut.GetShellsByFiltersAsync(filter: null, limit: 100, cursor: null, cancellationToken);

        // Assert
        var shell = Assert.Single(result.Result);

        Assert.NotNull(shell.AssetInformation);
        Assert.NotNull(shell.AssetInformation.SpecificAssetIds);

        Assert.Equal(3, shell.AssetInformation.SpecificAssetIds.Count);

        Assert.Equal("NewManufacturer", shell.AssetInformation.SpecificAssetIds.Single(x => x.Name == "ManufacturerId").Value);

        Assert.Equal("NewSerial", shell.AssetInformation.SpecificAssetIds.Single(x => x.Name == "SerialNumber").Value);

        Assert.Equal("Asset-001", shell.AssetInformation.SpecificAssetIds.Single(x => x.Name == "AssetTag").Value);
    }

    [Fact]
    public async Task GetShellsByFiltersAsync_WithIdShort_QueriesFilteredShellMetadata()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var manifests = new List<PluginManifest>();
        _pluginManifestConflictHandler.Manifests.Returns(manifests);

        const string targetIdShort = "test-idshort-value";

        _pluginDataHandler
            .GetDataForShellsByAssetIdsAsync(
                manifests,
                Arg.Is<ShellSearchFilter>(f => f != null && f.IdShort == targetIdShort),
                Arg.Any<int>(),
                Arg.Any<string?>(),
                cancellationToken)
            .Returns(new ShellDescriptorsMetaData
            {
                ShellDescriptors = [new ShellDescriptorMetaData { Id = "aas-1", SpecificAssetIds = [] }],
                PagingMetaData = new PagingMetaData()
            });

        _templateService.GetShellTemplateAsync("aas-1", cancellationToken)
            .Returns(new AssetAdministrationShell("aas-1", new AssetInformation(AssetKind.Instance)));

        // Act
        var filter = new ShellSearchFilter { IdShort = targetIdShort };
        var result = await _sut.GetShellsByFiltersAsync(filter, limit: 100, cursor: null, cancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Result);
        Assert.Equal("aas-1", result.Result[0].Id);
        await _pluginDataHandler.Received(1)
            .GetDataForShellsByAssetIdsAsync(manifests, Arg.Is<ShellSearchFilter>(f => f != null && f.IdShort == targetIdShort), Arg.Any<int>(), Arg.Any<string?>(), cancellationToken);
    }

    [Fact]
    public async Task GetShellsByFiltersAsync_ShouldBuildShellsInParallelAndSkipFailures()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var manifests = new List<PluginManifest>();
        _pluginManifestConflictHandler.Manifests.Returns(manifests);

        var metadataItems = new List<ShellDescriptorMetaData>
        {
            new() { Id = "aas-1", SpecificAssetIds = [] },
            new() { Id = "aas-2", SpecificAssetIds = [] },
            new() { Id = "aas-3", SpecificAssetIds = [] }
        };

        _pluginDataHandler
            .GetDataForAllShellDescriptorsAsync(Arg.Any<int>(), null, null, null, manifests, cancellationToken)
            .Returns(new ShellDescriptorsMetaData
            {
                ShellDescriptors = metadataItems,
                PagingMetaData = new PagingMetaData()
            });

        _templateService.GetShellTemplateAsync("aas-1", cancellationToken)
            .Returns(new AssetAdministrationShell("aas-1", new AssetInformation(AssetKind.Instance)));

        _templateService.GetShellTemplateAsync("aas-2", cancellationToken)
            .Throws(new Exception("Template loading failed"));

        _templateService.GetShellTemplateAsync("aas-3", cancellationToken)
            .Returns(new AssetAdministrationShell("aas-3", new AssetInformation(AssetKind.Instance)));

        // Act
        var result = await _sut.GetShellsByFiltersAsync(filter: null, limit: 100, cursor: null, cancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Result.Count);
        Assert.Contains(result.Result, s => s.Id == "aas-1");
        Assert.Contains(result.Result, s => s.Id == "aas-3");

        await _templateService.Received(1).GetShellTemplateAsync("aas-1", cancellationToken);
        await _templateService.Received(1).GetShellTemplateAsync("aas-2", cancellationToken);
        await _templateService.Received(1).GetShellTemplateAsync("aas-3", cancellationToken);
    }

    [Fact]
    public async Task GetShellsByFiltersAsync_WhenMetadataProvidesAssetKindAndType_OverridesTemplateValues()
    {
        var cancellationToken = CancellationToken.None;
        var manifests = new List<PluginManifest>();
        _pluginManifestConflictHandler.Manifests.Returns(manifests);

        var metadata = new ShellDescriptorMetaData
        {
            Id = "aas-1",
            AssetKind = "Instance",
            AssetType = "Attribute",
            SpecificAssetIds = []
        };

        _pluginDataHandler
            .GetDataForAllShellDescriptorsAsync(100, null, null, null, manifests, cancellationToken)
            .Returns(new ShellDescriptorsMetaData
            {
                ShellDescriptors = [metadata],
                PagingMetaData = new PagingMetaData()
            });

        var templateShell = new AssetAdministrationShell(
            "aas-1",
            new AssetInformation(AssetKind.Type, assetType: "Template", specificAssetIds: []));

        _templateService.GetShellTemplateAsync("aas-1", cancellationToken).Returns(templateShell);

        var result = await _sut.GetShellsByFiltersAsync(null, 100, null, cancellationToken);

        var shell = Assert.Single(result.Result);
        Assert.Equal(AssetKind.Instance, shell.AssetInformation?.AssetKind);
        Assert.Equal("Attribute", shell.AssetInformation?.AssetType);
    }

    [Fact]
    public async Task GetShellsByFiltersAsync_WhenMetadataOmitsAssetKindAndType_KeepsTemplateValues()
    {
        var cancellationToken = CancellationToken.None;
        var manifests = new List<PluginManifest>();
        _pluginManifestConflictHandler.Manifests.Returns(manifests);

        var metadata = new ShellDescriptorMetaData
        {
            Id = "aas-1",
            AssetKind = null,
            AssetType = null,
            SpecificAssetIds = []
        };

        _pluginDataHandler
            .GetDataForAllShellDescriptorsAsync(100, null, null, null, manifests, cancellationToken)
            .Returns(new ShellDescriptorsMetaData
            {
                ShellDescriptors = [metadata],
                PagingMetaData = new PagingMetaData()
            });

        var templateShell = new AssetAdministrationShell(
            "aas-1",
            new AssetInformation(AssetKind.Type, assetType: "Template", specificAssetIds: []));

        _templateService.GetShellTemplateAsync("aas-1", cancellationToken).Returns(templateShell);

        var result = await _sut.GetShellsByFiltersAsync(null, 100, null, cancellationToken);

        var shell = Assert.Single(result.Result);
        Assert.Equal(AssetKind.Type, shell.AssetInformation?.AssetKind);
        Assert.Equal("Template", shell.AssetInformation?.AssetType);
    }

    private static AssetAdministrationShell CreateShellTemplate()
        => new(
            id: "urn:uuid:123e4567-e89b-12d3-a456-426614174000",
            assetInformation: null!,
            idShort: "exampleAAS",
            category: "exampleCategory",
            displayName: [new LangStringNameType("en", "Example AAS")],
            description: [new LangStringTextType("en", "Description")],
            submodels: []
        );

    [Fact]
    public async Task GetThumbnailAsync_ShouldReturnStream_WhenUrlIsHttp()
    {
        var cancellationToken = CancellationToken.None;
        var assetInfoTemplate = CreateAssetInformationTemplate();
        var pluginData = new AssetData
        {
            DefaultThumbnail = new DefaultThumbnailData
            {
                Path = "https://example.com/logo.png",
                ContentType = "image/png"
            }
        };
        var manifests = new List<PluginManifest>
       {
           new()
           {
               SupportedSemanticIds = ["id-1"],
               Capabilities = new Capabilities { HasAssetInformation = true }
           }
       };

        _templateService.GetAssetInformationTemplateAsync(AasIdentifier, cancellationToken)
            .Returns(assetInfoTemplate);
        _pluginManifestConflictHandler.Manifests.Returns(manifests);
        _pluginDataHandler.GetDataForAssetInformationByIdAsync(manifests, AasIdentifier, cancellationToken)
            .Returns(pluginData);

        var stream = new MemoryStream("test-content"u8.ToArray());
        var fileContentResponse = new FileContentResponse(stream);
        _fileContentProvider.GetFileContentAsync("https://example.com/logo.png", cancellationToken)
            .Returns(fileContentResponse);

        var result = await _sut.GetThumbnailAsync(AasIdentifier, cancellationToken);

        Assert.NotNull(result);
        Assert.Equal(pluginData.DefaultThumbnail.ContentType, result.ContentType);
        Assert.Equal("logo.png", result.FileName);

        using var reader = new StreamReader(result.Content);
        var body = await reader.ReadToEndAsync();
        Assert.Equal("test-content", body);
    }

    [Fact]
    public async Task GetThumbnailAsync_ShouldThrowAssetInformationNotFoundException_WhenThumbnailIsMissing()
    {
        var cancellationToken = CancellationToken.None;
        var assetInfoTemplate = new AssetInformation(AssetKind.Instance, "globalAssetId", [], defaultThumbnail: null);
        var pluginData = new AssetData
        {
            DefaultThumbnail = null
        };
        var manifests = new List<PluginManifest>
       {
           new()
           {
               SupportedSemanticIds = ["id-1"],
               Capabilities = new Capabilities { HasAssetInformation = true }
           }
       };

        _templateService.GetAssetInformationTemplateAsync(AasIdentifier, cancellationToken)
            .Returns(assetInfoTemplate);
        _pluginManifestConflictHandler.Manifests.Returns(manifests);
        _pluginDataHandler.GetDataForAssetInformationByIdAsync(manifests, AasIdentifier, cancellationToken)
            .Returns(pluginData);

        await Assert.ThrowsAsync<AssetInformationNotFoundException>(() =>
            _sut.GetThumbnailAsync(AasIdentifier, cancellationToken));
    }

    [Fact]
    public async Task GetThumbnailAsync_ShouldThrowInternalDataProcessingException_WhenUrlIsNotHttp()
    {
        var cancellationToken = CancellationToken.None;
        var assetInfoTemplate = CreateAssetInformationTemplate();
        var pluginData = CreateAssetData();
        var manifests = new List<PluginManifest>
       {
           new()
           {
               SupportedSemanticIds = ["id-1"],
               Capabilities = new Capabilities { HasAssetInformation = true }
           }
       };

        _templateService.GetAssetInformationTemplateAsync(AasIdentifier, cancellationToken)
            .Returns(assetInfoTemplate);
        _pluginManifestConflictHandler.Manifests.Returns(manifests);
        _pluginDataHandler.GetDataForAssetInformationByIdAsync(manifests, AasIdentifier, cancellationToken)
            .Returns(pluginData);

        await Assert.ThrowsAsync<InternalDataProcessingException>(() =>
            _sut.GetThumbnailAsync(AasIdentifier, cancellationToken));
    }

    private static AssetInformation CreateAssetInformationTemplate()
    {
        var defaultThumbnail = Substitute.For<IResource>();
        defaultThumbnail.Path.Returns("sample/path");
        defaultThumbnail.ContentType.Returns("image/png");

        return new AssetInformation(
            assetKind: AssetKind.Instance,
            globalAssetId: "globalAssetId",
            specificAssetIds: [new SpecificAssetId(name: "TemplateId", value: "TemplateValue")],
            assetType: "http://example.com/type",
            defaultThumbnail: defaultThumbnail
        );
    }

    private static AssetData CreateAssetData()
        => new()
        {
            GlobalAssetId = "urn:my-company:asset:9999",
            SpecificAssetIds = new List<SpecificAssetIdsData>
            {
               new() { Name = "ManufacturerId", Value = "12345" },
               new() { Name = "SerialNumber", Value = "SN-0001" }
            },
            DefaultThumbnail = new DefaultThumbnailData
            {
                Path = "logo.svg",
                ContentType = "image/svg+xml"
            }
        };

    private static List<IReference> CreateSubmodelRefs(int count)
    {
        var refs = new List<IReference>();
        for (var i = 0; i < count; i++)
        {
            refs.Add(new Reference
            (
                ReferenceTypes.ModelReference,
                [
                     new Key(
                            KeyTypes.Submodel,
                            $"urn:uuid:submodel-{i}"
                            )
                      ],
                 null
            ));
        }

        return refs;
    }
}
