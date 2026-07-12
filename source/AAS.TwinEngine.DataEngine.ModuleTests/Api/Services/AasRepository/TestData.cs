using System.Text.Json.Nodes;

using AAS.TwinEngine.DataEngine.DomainModel.Plugin;

using AasCore.Aas3_1;

namespace AAS.TwinEngine.DataEngine.ModuleTests.Api.Services.AasRepository;

internal static class TestData
{
    public static AssetAdministrationShell CreateShellTemplate()
    {
        var shellJson = """
            {
              "id": "https://example.com/aas/aasTemplate",
              "assetInformation": {
                "assetKind": "Instance",
                "specificAssetIds": [
              {
                "name": "LotNumber",
                "value": "Test"
              },
              {
                "name": "BatchId",
                "value": "Test"
              },
              {
                "name": "SerialNumber",
                "value": "Test"
              }
            ]
              },
              "submodels": [
                {
                  "type": "ModelReference",
                  "keys": [
                    {
                      "type": "Submodel",
                      "value": "Nameplate"
                    }
                  ]
                },
                {
                  "type": "ModelReference",
                  "keys": [
                    {
                      "type": "Submodel",
                      "value": "ContactInformation"
                    }
                  ]
                },
                {
                  "type": "ModelReference",
                  "keys": [
                    {
                      "type": "Submodel",
                      "value": "Reliability"
                    }
                  ]
                }
              ],
              "modelType": "AssetAdministrationShell"
            }
            """;

        var jsonNode = JsonNode.Parse(shellJson);
        return Jsonization.Deserialize.AssetAdministrationShellFrom(jsonNode!);
    }

    public static AssetInformation CreateAssetInformationTemplate()
    {
        var assetJson = """
            {
                "assetKind": "Type",
                "globalAssetId": "",
                "specificAssetIds": [],
                "defaultThumbnail": {
                    "path": "",
                    "contentType": ""
                }
            }
            """;

        var jsonNode = JsonNode.Parse(assetJson);
        return Jsonization.Deserialize.AssetInformationFrom(jsonNode!);
    }

    public static string CreatePluginResponseForAssetinformation()
               => """
                   {
                     "assetKind": "Type",
                     "globalAssetId": "https://example.com/ids/F/5350_5407_2522_6562",
                     "specificAssetIds": [],
                     "defaultThumbnail": {
                       "path": "https://example.com/share/img/10080308_DE.jpg",
                       "contentType": "image/svg\u002Bxml"
                     }
                   }
                   """;

    public static IReadOnlyList<PluginManifest> CreatePluginManifests()
    {
        return new List<PluginManifest>
        {
            new()
            {
            PluginName = "TestPlugin1",
            PluginUrl = new Uri("https://example.com/plugin"),
            SupportedSemanticIds =
            [
                "http://example.com/idta/digital-nameplate/thumbnail",
                "http://example.com/idta/digital-nameplate/contact-name",
                "http://example.com/idta/digital-nameplate/email"
            ],
            Capabilities = new Capabilities
            {
                HasShellDescriptor = true,
                HasAssetInformation = true,
                HasAssetIdSearch = true
            }
            }
        };
    }

    public static string CreateShellResponse() => """
                   {
                     "id": "https://example.com/ids/aas/1170_1160_3052_6568/test/aas",
                     "assetInformation": {
                       "assetKind": "Type",
                       "globalAssetId": "https://example.com/ids/F/5350_5407_2522_6562",
                       "specificAssetIds": [],
                       "defaultThumbnail": {
                         "path": "https://example.com/share/img/10080308_DE.jpg",
                         "contentType": "image/svg\u002Bxml"
                       }
                     },
                     "submodels": [
                       {
                         "type": "ModelReference",
                         "keys": [
                           {
                             "type": "Submodel",
                             "value": "https://mm-software.com/submodel/1170_1160_3052_6568/Nameplate"
                           }
                         ]
                       },
                       {
                         "type": "ModelReference",
                         "keys": [
                           {
                             "type": "Submodel",
                             "value": "https://mm-software.com/submodel/1170_1160_3052_6568/ContactInformation"
                           }
                         ]
                       },
                       {
                         "type": "ModelReference",
                         "keys": [
                           {
                             "type": "Submodel",
                             "value": "https://mm-software.com/submodel/1170_1160_3052_6568/Reliability"
                           }
                         ]
                       }
                     ],
                     "modelType": "AssetAdministrationShell"
                   }
                   """;

    public static string CreateAssetInformationResponse() => """
                   {
                     "assetKind": "Type",
                     "globalAssetId": "https://example.com/ids/F/5350_5407_2522_6562",
                     "specificAssetIds": [],
                     "defaultThumbnail": {
                       "path": "https://example.com/share/img/10080308_DE.jpg",
                       "contentType": "image/svg\u002Bxml"
                     }
                   }
                   """;

    public static List<IReference> CreateSubmodelRefs()
    {
        var refs = new List<IReference>();
        for (var i = 0; i < 10; i++)
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

    public static string? GetProductIdFromRule(string aasIdentifier, int index)
    {
        var parts = aasIdentifier?.Split("/");
        if (parts is null || parts.Length < index || index < 1)
        {
            return null;
        }

        return parts[index - 1];
    }

    public static string CreatePluginResponseForShellDescriptors()
        => """
           {
             "paging_metadata": {
               "cursor": null
             },
             "result": [
               {
                 "globalAssetId": "https://mm-software.com/ids/assets/000-001",
                 "idShort": "Product1",
                 "id": "https://mm-software.com/ids/aas/000-001",
                 "specificAssetIds": [
                   { "name": "SerialNumber", "value": "SN-4711" }
                 ]
               },
               {
                 "globalAssetId": "https://mm-software.com/ids/assets/000-002",
                 "idShort": "Product2",
                 "id": "https://mm-software.com/ids/aas/000-002",
                 "specificAssetIds": [
                   { "name": "SerialNumber", "value": "SN-4712" }
                 ]
               }
             ]
           }
           """;

    public static string CreatePluginResponseForShellDescriptorsFilterByIdShort()
    => """
           {
             "paging_metadata": {
               "cursor": null
             },
             "result": [
               {
                 "globalAssetId": "https://mm-software.com/ids/assets/000-001",
                 "idShort": "Product1",
                 "id": "https://mm-software.com/ids/aas/000-001",
                 "specificAssetIds": [
                   { "name": "SerialNumber", "value": "SN-4711" }
                 ]
               }
             ]
           }
           """;

    public static string CreatePluginResponseForShellDescriptorsEmpty()
        => """
           {
             "paging_metadata": {
               "cursor": null
             },
             "result": []
           }
           """;
}
