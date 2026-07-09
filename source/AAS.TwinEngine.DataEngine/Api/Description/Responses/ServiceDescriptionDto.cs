using System.Text.Json.Serialization;

using NJsonSchema.Annotations;

namespace AAS.TwinEngine.DataEngine.Api.Description.Responses;

public class ServiceDescriptionDto
{
    [JsonPropertyName("profiles")]
    [JsonSchemaExtensionData(
        "example",
        new[]
        {
            "https://admin-shell.io/aas/API/3/0/AssetAdministrationShellRegistryServiceSpecification/SSP-002",
            "https://admin-shell.io/aas/API/3/0/SubmodelRegistryServiceSpecification/SSP-002"
        })]
    public IList<string>? Profiles { get; init; }
}
