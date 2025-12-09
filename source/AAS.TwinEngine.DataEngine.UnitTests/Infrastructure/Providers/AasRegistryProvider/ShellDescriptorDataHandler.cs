using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.DomainModel.AasRegistry;
using AAS.TwinEngine.DataEngine.DomainModel.Shared;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.AasRegistryProvider.Services;

using AasCore.Aas3_0;

using Microsoft.Extensions.Logging;

using NSubstitute;

namespace AAS.TwinEngine.DataEngine.UnitTests.Infrastructure.Providers.AasRegistryProvider;

public class ShellDescriptorDataHandlerTests
{
    private readonly ShellDescriptorDataHandler _sut;

    public ShellDescriptorDataHandlerTests()
    {
        var logger = Substitute.For<ILogger<ShellDescriptorDataHandler>>();
        _sut = new ShellDescriptorDataHandler(logger);
    }

    [Fact]
    public void FillOut_ThrowsIfTemplateIsNull() => Assert.Throws<ArgumentNullException>(() => _sut.FillOut(null!, []));

    [Fact]
    public void FillOut_ThrowsIfValuesIsNull()
    {
        var template = CreateShellDescriptorTemplate();
        Assert.Throws<ArgumentNullException>(() => _sut.FillOut(template, (List<ShellDescriptorMetaData>)null!));
    }

    [Fact]
    public void FillOut_FillsValuesCorrectly()
    {
        var template = CreateShellDescriptorTemplate();
        var values = CreateShellDescriptorDataList();

        var result = _sut.FillOut(template, values);

        Assert.Equal(2, result.Count);
        AssertDescriptorMatch(result[0], "GlobalAssetId_SensorWeatherStation", "idShort1", "SensorWeatherStation", [], "href1");
        AssertDescriptorMatch(result[1], "GlobalAssetId_ContactInformation", "idShort2", "ContactInformation", [], "href2");
    }

    [Fact]
    public void FillOut_SingleTemplate_UpdatesFieldsCorrectly()
    {
        var descriptor = CreateEmptyShellDescriptorWithEndpoint();
        var value = new ShellDescriptorMetaData
        {
            GlobalAssetId = "testAsset",
            IdShort = "testShort",
            Id = "testId",
            SpecificAssetIds =
                [
                    new SpecificAssetId
                    (
                       "test", "test"
                    )
                ],
            Href = "http://localhost"
        };

        var result = _sut.FillOut(descriptor, value);

        AssertDescriptorMatch(
                              result,
                              expectedAssetId: "testAsset",
                              expectedIdShort: "testShort",
                              expectedId: "testId",
                              expectedSpecificAssetIds:
                              [
                                  new SpecificAssetId
                                  (
                                      "test", "test"
                                  )
                              ],
                              expectedHref: "http://localhost"
                             );
    }

    [Fact]
    public void FillOut_ThrowsIfTemplateHasNoEndpoints()
    {
        var descriptor = new ShellDescriptor { Endpoints = null };
        var metaData = new ShellDescriptorMetaData { Href = "http://localhost" };

        Assert.Throws<InternalDataProcessingException>(() => _sut.FillOut(descriptor, metaData));
    }

    [Fact]
    public void FillOut_ThrowsIfTemplateHasEmptyEndpoints()
    {
        var descriptor = new ShellDescriptor { Endpoints = [] };
        var metaData = new ShellDescriptorMetaData { Href = "http://localhost" };

        Assert.Throws<InternalDataProcessingException>(() => _sut.FillOut(descriptor, metaData));
    }

    [Fact]
    public void FillOut_ThrowsIfProtocolInformationIsNull()
    {
        var descriptor = new ShellDescriptor
        {
            Endpoints = [new EndpointData { ProtocolInformation = null }]
        };
        var metaData = new ShellDescriptorMetaData { Href = "http://localhost" };

        Assert.Throws<InternalDataProcessingException>(() => _sut.FillOut(descriptor, metaData));
    }

    [Fact]
    public void FillOut_MultipleMetaData_DoesNotModifyOriginalTemplate()
    {
        var template = CreateShellDescriptorTemplate();
        var originalHref = template.Endpoints![0].ProtocolInformation!.Href;

        var values = CreateShellDescriptorDataList();
        _sut.FillOut(template, values);

        // Ensure template is not modified
        Assert.Equal(originalHref, template.Endpoints![0].ProtocolInformation!.Href);
        Assert.Equal("templateAssetId", template.GlobalAssetId);
    }

    #region Test Data Helpers

    private static ShellDescriptor CreateShellDescriptorTemplate()
    {
        var descriptorTemplate = new ShellDescriptor
        {
            Endpoints =
            [
                new EndpointData
                {
                    Interface = "AAS-3.0",
                    ProtocolInformation = new ProtocolInformationData() { Href = "templateHref" }
                }
            ],
            GlobalAssetId = "templateAssetId",
            IdShort = "templateIdShort",
            Id = "templateId",
            SpecificAssetIds = []
        };

        return descriptorTemplate;
    }

    private static List<ShellDescriptorMetaData> CreateShellDescriptorDataList()
    {
        return
        [
            new ShellDescriptorMetaData
            {
                GlobalAssetId = "GlobalAssetId_SensorWeatherStation",
                IdShort = "idShort1",
                Id = "SensorWeatherStation",
                SpecificAssetIds = null,
                Href = "href1"
            },
            new ShellDescriptorMetaData
            {
                GlobalAssetId = "GlobalAssetId_ContactInformation",
                IdShort = "idShort2",
                Id = "ContactInformation",
                SpecificAssetIds = null,
                Href = "href2"
            }
        ];
    }

    private static ShellDescriptor CreateEmptyShellDescriptorWithEndpoint()
    {
        return new ShellDescriptor
        {
            Endpoints =
            [
                new EndpointData
                {
                    ProtocolInformation = new ProtocolInformationData()
                }
            ]
        };
    }

    private static void AssertDescriptorMatch(
        ShellDescriptor descriptor,
        string expectedAssetId,
        string expectedIdShort,
        string expectedId,
        List<ISpecificAssetId> expectedSpecificAssetIds,
        string expectedHref)
    {
        Assert.Equal(expectedAssetId, descriptor.GlobalAssetId);
        Assert.Equal(expectedIdShort, descriptor.IdShort);
        Assert.Equal(expectedId, descriptor.Id);
        Assert.Equal(expectedHref, descriptor.Endpoints?[0]?.ProtocolInformation?.Href);
        for (var i = 0; i < expectedSpecificAssetIds.Count; i++)
        {
            var expected = expectedSpecificAssetIds[i];
            var actual = descriptor.SpecificAssetIds?[i];

            Assert.Equal(expected.Name, actual?.Name);
            Assert.Equal(expected.Value, actual?.Value);
            Assert.Equal(expected.ExternalSubjectId, actual!.ExternalSubjectId);
            Assert.Equal(expected.SemanticId, actual.SemanticId);
            Assert.Equal(expected.SupplementalSemanticIds, actual.SupplementalSemanticIds);
        }
    }

    #endregion
}
