using System.Text.Json;

using AAS.TwinEngine.DataEngine.DomainModel.AasRegistry;
using AAS.TwinEngine.DataEngine.DomainModel.Plugin;

namespace AAS.TwinEngine.DataEngine.ModuleTests.ApplicationLogic.Services.AasRegistry;

internal static class TestData
{
    public static ShellDescriptor CreateShellDescriptorsTemplate()
    {
        var shellDescriptorJson = """
                   {
                       "description": null,
                       "displayName": null,
                       "extensions": null,
                       "administration": null,
                       "assetKind": "Type",
                       "assetType": "Type",
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
                       "globalAssetId": "https://example.com/ids/assets/221-422",
                       "idShort": "221-422",
                       "id": "https://example.com/ids/aas/221-422",
                       "specificAssetIds": [],
                       "submodelDescriptors": null
                   }
                   """;

        var descriptor = JsonSerializer.Deserialize<ShellDescriptor>(shellDescriptorJson);

        return descriptor;
    }

    public static List<HttpContent> CreatePluginResponseForShellDescriptors()
    {
        var json = """
                   {
                     "paging_metadata": {
                       "cursor": null
                     },
                     "result": [
                       {
                         "globalAssetId": "https://example.com/ids/F/5350_5407_2522_6562",
                         "idShort": "SensorWeatherStationExample",
                         "id": "https://example.com/ids/aas/1170_1160_3052_6568",
                         "specificAssetIds": []
                       },
                       {
                         "globalAssetId": "https://example.com/ids/assets/2206-1631/1000-859",
                         "idShort": "2206-1631/1000-859",
                         "id": "https://example.com/ids/aas/2206-1631/1000-859",
                         "specificAssetIds": []
                       }
                     ]
                   }
                   """;

        var content = new StringContent(json);
        var mockResponse = new List<HttpContent>
        {
            content
        };

        return mockResponse;
    }

    public static string CreatePlugin1ResponseForShellDescriptors()
                       => """
                   {
                     "paging_metadata": {
                       "cursor": null
                     },
                     "result": [
                        {
                        "globalAssetId": "https://example.com/ids/F/5350_5407_2522_6562",
                       "idShort": "SensorWeatherStationExample",
                       "id": "https://example.com/ids/aas/1170_1160_3052_6568",
                       "specificAssetIds": []
                   }
                     ]
                   }
                   """;

    public static string CreatePlugin2ResponseForShellDescriptors()
               => """
                   {
                     "paging_metadata": {
                       "cursor": null
                     },
                     "result": [
                        {
                       "globalAssetId": "https://example.com/ids/assets/2206-1631/1000-859",
                         "idShort": "2206-1631/1000-859",
                         "id": "https://example.com/ids/aas/2206-1631/1000-859",
                         "specificAssetIds": []
                        }
                     ]
                   }
                   """;

    public static string CreatePlugin1ResponseForShellDescriptor()
           => """
                     {
                       "globalAssetId": "https://example.com/ids/F/5350_5407_2522_6562",
                       "idShort": "SensorWeatherStationExample",
                       "id": "https://example.com/ids/aas/1170_1160_3052_6568",
                       "specificAssetIds": []
                     }
                   """;

    public static string CreateShellDescriptors() => """
                   {
                     "paging_metadata": {
                       "cursor": null
                     },
                     "result": [
                       {
                         "description": null,
                         "displayName": null,
                         "extensions": null,
                         "administration": null,
                         "assetKind": 0,
                         "assetType": 0,
                         "endpoints": [
                           {
                             "interface": "AAS-3.0",
                             "protocolInformation": {
                               "href": "https://localhost:5059/shells/aHR0cHM6Ly9leGFtcGxlLmNvbS9pZHMvYWFzLzExNzBfMTE2MF8zMDUyXzY1Njg",
                               "endpointProtocol": "http",
                               "endpointProtocolVersion": null,
                               "subprotocol": null,
                               "subprotocolBody": null,
                               "subprotocolBodyEncoding": null,
                               "securityAttributes": null
                             }
                           }
                         ],
                         "globalAssetId": "https://example.com/ids/F/5350_5407_2522_6562",
                         "idShort": "SensorWeatherStationExample",
                         "id": "https://example.com/ids/aas/1170_1160_3052_6568",
                         "specificAssetIds": [],
                         "submodelDescriptors": null
                       },
                       {
                         "description": null,
                         "displayName": null,
                         "extensions": null,
                         "administration": null,
                         "assetKind": 0,
                         "assetType": 0,
                         "endpoints": [
                           {
                             "interface": "AAS-3.0",
                             "protocolInformation": {
                               "href": "https://localhost:5059/shells/aHR0cHM6Ly9leGFtcGxlLmNvbS9pZHMvYWFzLzIyMDYtMTYzMS8xMDAwLTg1OQ",
                               "endpointProtocol": "http",
                               "endpointProtocolVersion": null,
                               "subprotocol": null,
                               "subprotocolBody": null,
                               "subprotocolBodyEncoding": null,
                               "securityAttributes": null
                             }
                           }
                         ],
                         "globalAssetId": "https://example.com/ids/assets/2206-1631/1000-859",
                         "idShort": "2206-1631/1000-859",
                         "id": "https://example.com/ids/aas/2206-1631/1000-859",
                         "specificAssetIds": [],
                         "submodelDescriptors": null
                       }
                     ]
                   }
                   """;

    public static string CreateShellDescriptor() => """
                   {
                     "description": null,
                     "displayName": null,
                     "extensions": null,
                     "administration": null,
                     "assetKind": 0,
                     "assetType": 0,
                     "endpoints": [
                       {
                         "interface": "AAS-3.0",
                         "protocolInformation": {
                           "href": "https://localhost:5059/shells/aHR0cHM6Ly9leGFtcGxlLmNvbS9pZHMvYWFzLzExNzBfMTE2MF8zMDUyXzY1Njg",
                           "endpointProtocol": "http",
                           "endpointProtocolVersion": null,
                           "subprotocol": null,
                           "subprotocolBody": null,
                           "subprotocolBodyEncoding": null,
                           "securityAttributes": null
                         }
                       }
                     ],
                     "globalAssetId": "https://example.com/ids/F/5350_5407_2522_6562",
                     "idShort": "SensorWeatherStationExample",
                     "id": "https://example.com/ids/aas/1170_1160_3052_6568",
                     "specificAssetIds": [],
                     "submodelDescriptors": null
                   }
                   """;

    public static IReadOnlyList<PluginManifest> CreatePluginManifests()
    {
        return new List<PluginManifest>
      {
        new() {
            PluginName = "TestPlugin1",
            PluginUrl = new Uri("https://example.com/plugin"),
            SupportedSemanticIds = new List<string>
            {
                "http://example.com/idta/digital-nameplate/thumbnail",
                "http://example.com/idta/digital-nameplate/contact-name",
                "http://example.com/idta/digital-nameplate/email"
            },
            Capabilities = new Capabilities
            {
                HasShellDescriptor = true,
                HasAssetInformation = false
            }
          },
        new() {
            PluginName = "TestPlugin2",
            PluginUrl = new Uri("https://example.com/plugin"),
            SupportedSemanticIds = new List<string>
            {
                "http://example.com/idta/digital-nameplate/manufacturer-name_en",
                "http://example.com/idta/digital-nameplate/manufacturer-name_de",
            },
            Capabilities = new Capabilities
            {
                HasShellDescriptor = true,
                HasAssetInformation = false
            }
          },
      };
    }
}
