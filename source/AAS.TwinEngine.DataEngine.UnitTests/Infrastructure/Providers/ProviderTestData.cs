namespace AAS.TwinEngine.DataEngine.UnitTests.Infrastructure.Providers;

public static class ProviderTestData
{
    public const string ValidateSubmodelResponse = """
                                                               {
                                                               "modelType": "Submodel",
                                                               "semanticId": {
                                                                   "keys": [
                                                                       {
                                                                           "type": "GlobalReference",
                                                                           "value": "TestSemanticId"
                                                                       }
                                                                   ],
                                                                   "type": "ExternalReference"
                                                               },
                                                               "id": "TestId",
                                                               "idShort": "Test"
                                                           }
                                                   """;

    public const string ValidateShellDescriptorResponse = """

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
                                                       "assetKind": "Type",
                                                       "assetType": "Type",
                                                       "endpoints": [
                                                           {
                                                               "interface": "AAS-3.0",
                                                               "protocolInformation": {
                                                                   "href": "http://localhost:8081/shells/aHR0cHM6Ly9hZG1pbi1zaGVsbC5pby9pZHRhL2Fhcy9Db250YWN0SW5mb3JtYXRpb24vMS8w",
                                                                   "endpointProtocol": "http",
                                                                   "endpointProtocolVersion": null,
                                                                   "subprotocol": null,
                                                                   "subprotocolBody": null,
                                                                   "subprotocolBodyEncoding": null,
                                                                   "securityAttributes": null
                                                               }
                                                           }
                                                       ],
                                                       "globalAssetId": "https://admin-shell.io/idta/asset/ContactInformation/1/0",
                                                       "idShort": "ContactInformationAAS",
                                                       "id": "https://admin-shell.io/idta/aas/ContactInformation/1/0",
                                                       "specificAssetIds": null,
                                                       "submodelDescriptors": null
                                                   }
                                                ]
                                             }
                                           """;

    public const string ValidateShellResponse = """
                                                {
                                                    "modelType": "AssetAdministrationShell",
                                                    "assetInformation": {
                                                        "assetKind": "Type",
                                                        "assetType": "Type",
                                                        "globalAssetId": "https://admin-shell.io/idta/asset/ContactInformation/1/0"
                                                    },
                                                    "submodels": [
                                                        {
                                                            "keys": [
                                                                {
                                                                    "type": "Submodel",
                                                                    "value": "https://admin-shell.io/idta/SubmodelTemplate/ContactInformation/1/0"
                                                                }
                                                            ],
                                                            "type": "ModelReference"
                                                        }
                                                    ],
                                                    "id": "https://admin-shell.io/idta/aas/ContactInformation/1/0",
                                                    "idShort": "ContactInformationAAS"
                                                }
                                                """;

    public const string ValidateAssetInformationResponse = """
                                                           {
                                                               "assetKind": "Instance",
                                                               "assetType": "Type",
                                                               "defaultThumbnail": {
                                                                   "contentType": "image/jpeg",
                                                                   "path": "http://localhost/fileprovider/k.jpg"
                                                               },
                                                               "globalAssetId": "https://sew-eurodrive.de/shell/1"
                                                           }
                                                           """;

    public const string ValidateSubmodelRefResponse = """
                                                      {
                                                          "result": [
                                                              {
                                                                  "type": "ModelReference",
                                                                  "keys": [
                                                                      {
                                                                          "type": "Submodel",
                                                                          "value": "urn:uuid:submodel-123"
                                                                      }
                                                                  ]
                                                              }
                                                          ]
                                                      }
                                                      """;

    public const string ShellDescriptors = """
                                           [
                                           {
                                             "globalAssetId": "https://example.com/ids/F/5350_5407_2522_6562",
                                             "idShort": "SensorWeatherStationExample",
                                             "id": "https://example.com/ids/aas/1170_1160_3052_6568",
                                             "specificAssetIds": []
                                           },
                                           {
                                             "globalAssetId": "https://wago.com/ids/assets/2206-1631/1000-859",
                                             "idShort": "2206-1631/1000-859",
                                             "id": "https://wago.com/ids/aas/2206-1631/1000-859",
                                             "specificAssetIds": []
                                           }
                                           ]
                                           """;

    public const string ShellDescriptor = """
                                          {
                                            "globalAssetId": "https://example.com/ids/F/5350_5407_2522_6562",
                                            "idShort": "SensorWeatherStationExample",
                                            "id": "https://example.com/ids/aas/1170_1160_3052_6568",
                                            "specificAssetIds": []
                                          }
                                          """;

    public const string AssetInformation = """
                                           {
                                             "globalAssetId": "https://admin-shell.io/idta/asset/ContactInformation/1/0",
                                             "idShort": "ContactInformationAAS",
                                             "id": "https://admin-shell.io/idta/aas/ContactInformation/1/0",
                                             "specificAssetIds": [],
                                             "assetInformationData": {
                                               "assetKind": "Type",
                                               "assetType": "AssetType_3_Valid",
                                               "defaultThumbnail": {
                                                 "contentType": "image/svg+xml",
                                                 "path": "AAS_Logo.svg"
                                               }
                                             }
                                           }
                                           """;

    public const string ValidConceptDescription = """
                                                  {
                                                      "modelType": "ConceptDescription",
                                                      "embeddedDataSpecifications": [
                                                          {
                                                              "dataSpecification": {
                                                                  "keys": [
                                                                      {
                                                                          "type": "GlobalReference",
                                                                          "value": "http://admin-shell.io/DataSpecificationTemplates/DataSpecificationIEC61360/3/0"
                                                                      }
                                                                  ],
                                                                  "type": "ExternalReference"
                                                              },
                                                              "dataSpecificationContent": {
                                                                  "modelType": "DataSpecificationIec61360",
                                                                  "definition": [
                                                                      {
                                                                          "language": "en",
                                                                          "text": "General information, for example ordering and manufacturer information"
                                                                      },
                                                                      {
                                                                          "language": "de",
                                                                          "text": "Generelle Informationen, zum Beispiel Bestell- und Herstellerinformationen"
                                                                      }
                                                                  ],
                                                                  "preferredName": [
                                                                      {
                                                                          "language": "en",
                                                                          "text": "General information"
                                                                      },
                                                                      {
                                                                          "language": "de",
                                                                          "text": "Allgemeine Informationen"
                                                                      }
                                                                  ],
                                                                  "shortName": [
                                                                      {
                                                                          "language": "en",
                                                                          "text": "General"
                                                                      },
                                                                      {
                                                                          "language": "de",
                                                                          "text": "Allgemein"
                                                                      }
                                                                  ]
                                                              }
                                                          }
                                                      ],
                                                      "id": "0173-1#02-ABK161#002/0173-1#01-AHX838#002",
                                                      "idShort": "GeneralInformation"
                                                  }
                                                  """;
}
