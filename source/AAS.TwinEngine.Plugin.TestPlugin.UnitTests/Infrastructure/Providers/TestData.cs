namespace AAS.TwinEngine.Plugin.TestPlugin.UnitTests.Infrastructure.Providers;

public static class TestData
{
    public const string TestMetaData = @"[
        {
         ""globalAssetId"": ""WeatherStation"",
         ""idShort"": ""SensorWeatherStationExample"",
         ""id"": ""WeatherStation"",
         ""specificAssetIds"": [],
         ""assetInformationData"": {
         ""defaultThumbnail"": {
            ""contentType"": ""image/svg+xml"",
            ""path"": ""AAS_Logo.svg""
            }
        }
        },
        {
         ""globalAssetId"": ""mm-2206-1631"",
         ""idShort"": ""2206-1631/1000-859"",
         ""id"": ""mm-2206-1631/1000-859"",
         ""specificAssetIds"": [],
         ""assetInformationData"": {
           ""defaultThumbnail"": {
             ""contentType"": ""image/svg+xml"",
             ""path"": ""AAS_Logo.svg""
           }
         }
        },
      {
         ""globalAssetId"": ""100-859"",
         ""idShort"": """",
         ""id"": ""1000-859"",
         ""specificAssetIds"": [],
         ""assetInformationData"": {
           ""defaultThumbnail"": {
             ""contentType"": ""image/svg+xml"",
             ""path"": ""AAS_Logo.svg""
           }
         }
       },
       {
         ""globalAssetId"": ""m&m-259"",
         ""idShort"": """",
         ""id"": """",
         ""specificAssetIds"": []
       },
       {
         ""globalAssetId"": ""SoftwareNameplate"",
         ""idShort"": ""SoftwareNameplateAAS"",
         ""id"": ""SoftwareNameplate/1/0"",
         ""specificAssetIds"": []
       },
       {
         ""globalAssetId"": ""ContactInformation"",
         ""idShort"": ""ContactInformationAAS"",
         ""id"": ""ContactInformation"",
         ""specificAssetIds"": [
           {
             ""name"": ""serialNumber"",
             ""value"": ""SN-859-001""
           }
         ],
         ""assetInformationData"": {
           ""defaultThumbnail"": {
             ""contentType"": ""image/svg+xml"",
             ""path"": ""AAS_Logo.svg""
           }
         }
       },
       {
         ""globalAssetId"": ""DigitalNameplate"",
         ""idShort"": ""DigitalNameplateAAS"",
         ""id"": ""DigitalNameplate/3/0"",
         ""specificAssetIds"": null
       }
     ]";

    public const string TestSubmodelData = @"
            {
        ""test-submodelId"": {
    ""root"": {
      ""Email"": ""test@example.com""
    },
    ""Email"": ""test@example.com"",
    ""ContactInformation"": {
      ""Email"": ""contact@test.com"",
      ""Phone"": ""555-1234""
    },
    ""ContactInformations"": {
      ""ContactInformation"": [
        {
          ""Email"": ""first@test.com"",
          ""Phone"": {
            ""TelephoneNumber"": ""111-1111""
          }
        },
        {
          ""Email"": ""second@test.com"",
          ""Phone"": {
            ""TelephoneNumber"": ""222-2222"",
            ""AvailableTime"": {
              ""AvailableTime_de"": ""Montag – Freitag 08:00 bis 16:00""
            }
          }
        }
      ]
    },
    ""ManufacturerName"": {
      ""ManufacturerName_en"": ""M&M""
    },
    ""Document"": [
      {
        ""IsPrimary"": ""true"",
        ""DocumentClassification"": [
          {
            ""ClassId"": ""02-02"",
            ""ClassificationSystem"": ""VDI2770:2020""
          },
          {
            ""ClassId"": ""STEP"",
            ""ClassificationSystem"": ""IDTA-MCAD:2022""
          }
        ]
      },
      {
        ""IsPrimary"": ""false"",
        ""DocumentClassification"": {
          ""ClassId"": ""01-01"",
          ""ClassificationSystem"": ""VDI2770:2025""
        }
      }
    ],
    ""Nameplate"": {
      ""ContactInformation"": {
        ""Phone"": [
          {
            ""TelephoneNumber"": ""+49571 8870""
          },
          {
            ""TelephoneNumber"": ""+91 7845129532""
          }
        ]
      }
    },
    ""MCAD"": {
      ""Document_STEP"": {
        ""DocumentId"": {
          ""DocumentVersion"": [
            {
              ""StatusValue"": ""Released""
            },
            {
              ""StatusValue"": ""Inprogress""
            }
          ]}}}}}";
}
