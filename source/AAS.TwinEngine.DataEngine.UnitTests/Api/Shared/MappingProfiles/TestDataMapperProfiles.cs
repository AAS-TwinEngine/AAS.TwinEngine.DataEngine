using AAS.TwinEngine.DataEngine.DomainModel.AasRegistry;
using AAS.TwinEngine.DataEngine.DomainModel.Shared;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRegistry;

using AasCore.Aas3_0;

namespace AAS.TwinEngine.DataEngine.UnitTests.Api.Shared.MappingProfiles;

public static class TestDataMapperProfiles
{
    public static Key CreateKey() => new(KeyTypes.Identifiable, "https://example.com/key");

    public static Reference CreateReference()
        => new(
            ReferenceTypes.ExternalReference,
            [CreateKey()]);

    public static LangStringTextType CreateLangStringData() => new("en", "Sample Text");

    public static LangStringNameType CreateLangStringNameType() => new("en", "Sample Name");

    public static ValueReferencePair CreateValueReferencePair() => new("Option1", CreateReference());

    public static Extension CreateExtension()
        => new(
                      "Sample Extension",
                      CreateReference(),
                      null,
                      DataTypeDefXsd.String);

    public static SpecificAssetId CreateSpecificAssetId() => new("SpecificAssetIds", "Sample SpecificAssetIds");

    public static ValueList CreateValueList()
        => new([
            CreateValueReferencePair(),
            CreateValueReferencePair()
        ]);

    public static LevelType CreateLevelType() => new(min: true, nom: false, typ: true, max: false);

    public static DataSpecificationIec61360 CreateDataSpecificationContent()
        => new(
            preferredName:
            [
                new LangStringPreferredNameTypeIec61360 ("en", "Sample Text")
            ],
            unit: "mm",
            unitId: CreateReference(),
            sourceOfDefinition: "ISO 1234",
            symbol: "mm",
            dataType: DataTypeIec61360.String,
            valueFormat: "string",
            value: "SampleValue",
            valueList: CreateValueList(),
            levelType: CreateLevelType()
           );

    public static EmbeddedDataSpecification CreateEmbeddedDataSpecification()
        => new(
            dataSpecification: CreateReference(),
            dataSpecificationContent: CreateDataSpecificationContent()
           );

    public static AdministrativeInformation CreateAdministration()
        => new(
            version: "1.0",
            revision: "A",
            creator: CreateReference(),
            templateId: "template-123",
            embeddedDataSpecifications:
            [
                CreateEmbeddedDataSpecification(),
                CreateEmbeddedDataSpecification()
            ]
           );

    public static SecurityAttributesData CreateSecurityAttributesData() => new()
    {
        Type = "OAuth2",
        Key = "access_token",
        Value = "xyz123"
    };

    public static ProtocolInformationData CreateProtocolInformationData() => new()
    {
        Href = "https://api.example.com",
        EndpointProtocol = "HTTPS",
        EndpointProtocolVersion = "1.1",
        SubProtocol = "REST",
        SubProtocolBody = "JSON",
        SubProtocolBodyEncoding = "UTF-8",
        SecurityAttributes = new List<SecurityAttributesData> { CreateSecurityAttributesData() }
    };

    public static EndpointData CreateEndpointData() => new()
    {
        Interface = "RESTful",
        ProtocolInformation = CreateProtocolInformationData()
    };

    public static SubmodelDescriptor CreateSubmodelDescriptor() => new()
    {
        Id = "submodel-001",
        IdShort = "SubShort",
        Description = new List<LangStringTextType> { CreateLangStringData() },
        DisplayName = new List<LangStringNameType> { CreateLangStringNameType() },
        Extensions = new List<Extension> { CreateExtension() },
        Administration = CreateAdministration(),
        SemanticId = CreateReference(),
        SupplementalSemanticId = new List<Reference> { CreateReference() },
        Endpoints = new List<EndpointData> { CreateEndpointData() }
    };

    public static ShellDescriptor CreateShellDescriptor() => new()
    {
        Id = "shell-001",
        IdShort = "ShellShort",
        AssetKind = AssetKind.Type,
        AssetType = AssetKind.Type,
        GlobalAssetId = "global-asset-id",
        Description = new List<LangStringTextType> { CreateLangStringData() },
        DisplayName = new List<LangStringNameType> { CreateLangStringNameType() },
        Extensions = new List<Extension> { CreateExtension() },
        Administration = CreateAdministration(),
        SpecificAssetIds = new List<SpecificAssetId> { CreateSpecificAssetId() },
        SubmodelDescriptors = new List<SubmodelDescriptor> { CreateSubmodelDescriptor() },
        Endpoints = new List<EndpointData> { CreateEndpointData() }
    };

    public static ShellDescriptors CreateShellDescriptors() => new()
    {
        Result = new List<ShellDescriptor> { CreateShellDescriptor(), CreateShellDescriptor() },
        PagingMetaData = new PagingMetaData
        {
            Cursor = "shell-001-encodedValue"
        }
    };
}
