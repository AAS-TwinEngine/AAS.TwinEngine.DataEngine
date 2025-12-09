using System.Text.Json.Nodes;

using AAS.TwinEngine.DataEngine.DomainModel.Plugin;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.TemplateProvider.Config;

using AasCore.Aas3_0;
namespace AAS.TwinEngine.DataEngine.ModuleTests.ApplicationLogic.Services.AasRepository;

internal static class TestData
{
    public static AssetAdministrationShell CreateShellTemplate()
    {
        var shellJson = """
            {
              "id": "https://example.com/aas/aasTemplate",
              "assetInformation": {
                "assetKind": "Instance"
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
                HasAssetInformation = true
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
        var aasIdExtractionRules = new List<AasIdExtractionRules>
        {
            new() {
                Pattern = "Regex",
                Index = index,
                Separator = "/"
            }
        };

        var productId = aasIdExtractionRules
            .Select(rule => new
            {
                Rule = rule,
                Parts = aasIdentifier?.Split(rule.Separator)
            })
            .Where(x => x.Parts is { Length: >= 1 } && x.Rule.Index > 0 && x.Parts.Length >= x.Rule.Index)
            .Select(x => x.Parts![x.Rule.Index - 1])
            .FirstOrDefault();

        return productId;
    }
}
