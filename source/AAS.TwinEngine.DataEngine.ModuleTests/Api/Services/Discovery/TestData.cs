using System.Text.Json;

using AAS.TwinEngine.DataEngine.DomainModel.AasRegistry;
using AAS.TwinEngine.DataEngine.DomainModel.Plugin;

namespace AAS.TwinEngine.DataEngine.ModuleTests.Api.Services.Discovery;

internal static class TestData
{
    public static IReadOnlyList<PluginManifest> CreatePluginManifestsWithAssetIdSearch()
    {
        return new List<PluginManifest>
        {
            new()
            {
                PluginName = "TestPlugin1",
                PluginUrl = new Uri("https://testendpoint1.com"),
                SupportedSemanticIds = ["urn:semantic:1"],
                Capabilities = new Capabilities
                {
                    HasShellDescriptor = true,
                    HasAssetInformation = true,
                    HasAssetIdSearch = true,
                }
            }
        };
    }

    public static IReadOnlyList<PluginManifest> CreatePluginManifestsWithoutAssetIdSearch()
    {
        return new List<PluginManifest>
        {
            new()
            {
                PluginName = "TestPlugin1",
                PluginUrl = new Uri("https://testendpoint1.com"),
                SupportedSemanticIds = ["urn:semantic:1"],
                Capabilities = new Capabilities
                {
                    HasShellDescriptor = true,
                    HasAssetInformation = true,
                }
            }
        };
    }

    public static string CreatePluginResponseForAssetIdSearch()
        => """
           {
             "paging_metadata": {
               "cursor": null
             },
             "result": [
               {
                 "globalAssetId": "urn:manufacturer-x:asset:motor:001",
                 "idShort": "Motor001",
                 "id": "urn:manufacturer-x:aas:motor:001",
                 "specificAssetIds": [
                   {
                     "name": "SerialNumber",
                     "value": "SN-4711"
                   }
                 ]
               },
               {
                 "globalAssetId": "urn:manufacturer-x:asset:motor:002",
                 "idShort": "Motor002",
                 "id": "urn:manufacturer-x:aas:motor:002",
                 "specificAssetIds": [
                   {
                     "name": "SerialNumber",
                     "value": "SN-4711"
                   },
                   {
                     "name": "BatchId",
                     "value": "B-2026-03"
                   }
                 ]
               }
             ]
           }
           """;

    public static string CreatePluginResponseForAssetIdSearchEmpty()
        => """
           {
             "paging_metadata": {
               "cursor": null
             },
             "result": []
           }
           """;

    public static ShellDescriptor CreateShellTemplate()
    {
        var json = """
                   {
                       "description": null,
                       "displayName": null,
                       "extensions": null,
                       "administration": null,
                       "assetKind": "Instance",
                       "assetType": "Instance",
                       "endpoints": [
                           {
                               "interface": "AAS-3.0",
                               "protocolInformation": {
                                   "href": "",
                                   "endpointProtocol": "http",
                                   "endpointProtocolVersion": null,
                                   "subprotocol": null,
                                   "subprotocolBody": null,
                                   "subprotocolBodyEncoding": null,
                                   "securityAttributes": null
                               }
                           }
                       ],
                       "globalAssetId": "",
                       "idShort": "",
                       "id": "",
                       "specificAssetIds": [],
                       "submodelDescriptors": null
                   }
                   """;

        return JsonSerializer.Deserialize<ShellDescriptor>(json)!;
    }
}
