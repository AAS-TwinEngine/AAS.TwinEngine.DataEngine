using System.Text.Json.Nodes;

using AAS.TwinEngine.DataEngine.DomainModel.Plugin;

using AasCore.Aas3_0;

using File = AasCore.Aas3_0.File;

namespace AAS.TwinEngine.DataEngine.ModuleTests.Api.Services.SubmodelRepository;

internal static class TestData
{
    public static MultiLanguageProperty CreateManufacturerName()
    {
        return new MultiLanguageProperty(
          idShort: "ManufacturerName",
          value: [
            new LangStringTextType("en", ""),
            new LangStringTextType("de", "")
          ],
          semanticId: new Reference(
            ReferenceTypes.ExternalReference,
            [
              new Key(KeyTypes.MultiLanguageProperty, "http://example.com/idta/digital-nameplate/manufacturer-name")
            ]
          ),
          qualifiers:
          [
              new Qualifier(
                            type: "ExternalReference",
                            valueType: DataTypeDefXsd.String,
                            value: "One")
          ]);
    }

    public static File CreateThumbnail()
    {
        return new File(
          contentType: "image/png",
          idShort: "Thumbnail",
          value: "https://localhost/Thumbnail",
          semanticId: new Reference(
            ReferenceTypes.ExternalReference,
            [
              new Key(KeyTypes.SubmodelElementCollection, "http://example.com/idta/digital-nameplate/thumbnail")
            ]
          )
        );
    }

    public static Property CreateContactName()
    {
        return new Property(
          idShort: "ContactName",
          valueType: DataTypeDefXsd.String,
          value: "",
          semanticId: new Reference(
            ReferenceTypes.ExternalReference,
            [
              new Key(KeyTypes.Property, "http://example.com/idta/digital-nameplate/contact-name")
            ]
          ),
          qualifiers:
          [
              new Qualifier(
                            type: "ExternalReference",
                            valueType: DataTypeDefXsd.String,
                            value: "One")
          ]);
    }

    public static Property CreateEmail()
    {
        return new Property(
          idShort: "Email",
          valueType: DataTypeDefXsd.String,
          value: "",
          semanticId: new Reference(
            ReferenceTypes.ExternalReference,
            [
              new Key(KeyTypes.Property, "http://example.com/idta/digital-nameplate/email")
            ]
          ),
          qualifiers:
          [
              new Qualifier(
                            type: "ExternalReference",
                            valueType: DataTypeDefXsd.String,
                            value: "One")
          ]);
    }

    public static Submodel CreateSubmodel()
    {
        return new Submodel(
          id: "http://example.com/idta/digital-nameplate",
          idShort: "DigitalNameplate",
          semanticId: new Reference(
            ReferenceTypes.ExternalReference,
            [
              new Key(KeyTypes.Submodel, "http://example.com/idta/digital-nameplate/semantic-id")
            ]
          ),
          submodelElements: [
            CreateManufacturerName(),
            CreateContactName(),
            CreateEmail(),
            CreateThumbnail()
            ]);
    }

    public static ConceptDescription CreateConceptDescription()
    {
        return new ConceptDescription(
                                      id: "http://example.com/idta/digital-nameplate/semantic-id",
                                      idShort: "DigitalNameplateSemanticId",
                                      description:
                                      [
                                          new LangStringTextType("en", "ConceptDescription for Digital Nameplate"),
                                          new LangStringTextType("de", "ConceptDescription für Digitales Typenschild")
                                      ],
                                      isCaseOf:
                                      [
                                          new Reference(
                                                        ReferenceTypes.ExternalReference,
                                                        [new Key(KeyTypes.GlobalReference, "http://example.com/idta/digital-nameplate/semantic-id")]
                                                       )
                                      ]
                                     );
    }

    public static string CreatePluginResponseForSubmodelElement() => """
        {
        "http://example.com/idta/digital-nameplate/semantic-id": {
        "http://example.com/idta/digital-nameplate/contact-name": "John Doe"
      }
    }
    """;

    public static string CreatePlugin1ResponseForSubmodel() => """
        {
        "http://example.com/idta/digital-nameplate/semantic-id": {
        "http://example.com/idta/digital-nameplate/contact-name": "John Doe",
        "http://example.com/idta/digital-nameplate/email": "john.doe@example.com",
        "http://example.com/idta/digital-nameplate/thumbnail": "https://example.com/logo.png"
      }
    }
    """;

    public static string CreatePlugin2ResponseForSubmodel() => """
        {
      "http://example.com/idta/digital-nameplate/semantic-id": {
        "http://example.com/idta/digital-nameplate/manufacturer-name": {
          "http://example.com/idta/digital-nameplate/manufacturer-name_en": "Example Manufacturer",
          "http://example.com/idta/digital-nameplate/manufacturer-name_de": "Beispielhersteller"
        }
      }
    }
    """;

    public static string CreateSubmodelWithValues() => """
                                               {
                                                 "idShort": "DigitalNameplate",
                                                 "id": "ContactInformation",
                                                 "semanticId": {
                                                   "type": "ExternalReference",
                                                   "keys": [
                                                     {
                                                       "type": "Submodel",
                                                       "value": "http://example.com/idta/digital-nameplate/semantic-id"
                                                     }
                                                   ]
                                                 },
                                                 "submodelElements": [
                                                   {
                                                     "idShort": "ManufacturerName",
                                                     "semanticId": {
                                                       "type": "ExternalReference",
                                                       "keys": [
                                                         {
                                                           "type": "MultiLanguageProperty",
                                                           "value": "http://example.com/idta/digital-nameplate/manufacturer-name"
                                                         }
                                                       ]
                                                     },
                                                     "qualifiers": [
                                                       {
                                                         "type": "ExternalReference",
                                                         "valueType": "xs:string",
                                                         "value": "One"
                                                       }
                                                     ],
                                                     "value": [
                                                       {
                                                         "language": "en",
                                                         "text": "Example Manufacturer"
                                                       },
                                                       {
                                                         "language": "de",
                                                         "text": "Beispielhersteller"
                                                       }
                                                     ],
                                                     "modelType": "MultiLanguageProperty"
                                                   },
                                                   {
                                                     "idShort": "ContactName",
                                                     "semanticId": {
                                                       "type": "ExternalReference",
                                                       "keys": [
                                                         {
                                                           "type": "Property",
                                                           "value": "http://example.com/idta/digital-nameplate/contact-name"
                                                         }
                                                       ]
                                                     },
                                                     "qualifiers": [
                                                       {
                                                         "type": "ExternalReference",
                                                         "valueType": "xs:string",
                                                         "value": "One"
                                                       }
                                                     ],
                                                     "valueType": "xs:string",
                                                     "value": "John Doe",
                                                     "modelType": "Property"
                                                   },
                                                   {
                                                     "idShort": "Email",
                                                     "semanticId": {
                                                       "type": "ExternalReference",
                                                       "keys": [
                                                         {
                                                           "type": "Property",
                                                           "value": "http://example.com/idta/digital-nameplate/email"
                                                         }
                                                       ]
                                                     },
                                                     "qualifiers": [
                                                       {
                                                         "type": "ExternalReference",
                                                         "valueType": "xs:string",
                                                         "value": "One"
                                                       }
                                                     ],
                                                     "valueType": "xs:string",
                                                     "value": "john.doe@example.com",
                                                     "modelType": "Property"
                                                   },
                                                   {
                                                     "idShort": "Thumbnail",
                                                     "semanticId": {
                                                       "type": "ExternalReference",
                                                       "keys": [
                                                         {
                                                           "type": "SubmodelElementCollection",
                                                           "value": "http://example.com/idta/digital-nameplate/thumbnail"
                                                         }
                                                       ]
                                                     },
                                                     "value": "https://example.com/logo.png",
                                                     "contentType": "image/png",
                                                     "modelType": "File"
                                                   }
                                                 ],
                                                 "modelType": "Submodel"
                                               }
                                               """;

    public static string CreateSubmodelElementWithValues() => """
                                               {
                                                 "idShort": "ContactName",
                                                 "semanticId": {
                                                   "type": "ExternalReference",
                                                   "keys": [
                                                     {
                                                       "type": "Property",
                                                       "value": "http://example.com/idta/digital-nameplate/contact-name"
                                                     }
                                                   ]
                                                 },
                                                 "qualifiers": [
                                                   {
                                                     "type": "ExternalReference",
                                                     "valueType": "xs:string",
                                                     "value": "One"
                                                   }
                                                 ],
                                                 "valueType": "xs:string",
                                                 "value": "John Doe",
                                                 "modelType": "Property"
                                               }
                                               """;

    public static AssetAdministrationShell CreateShellTemplate()
    {
        var shellJson = """
            {
              "id": "https://m-m.softoware.example.com/aas/aasTemplate",
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
}
