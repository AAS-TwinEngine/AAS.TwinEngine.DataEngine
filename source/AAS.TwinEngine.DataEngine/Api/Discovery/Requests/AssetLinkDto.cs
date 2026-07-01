using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace AAS.TwinEngine.DataEngine.Api.Discovery.Requests;

public class AssetLinkDto
{
    [JsonPropertyName("name")]
    [Required]
    [DefaultValue("ud800MudbffAudbffJudbff7ud800d3MnAudbff@udbff:>Zudbffbud800ZU^=L")]
    public required string Name { get; set; }

    [JsonPropertyName("value")]
    [Required]
    [DefaultValue("8ud800Gudbffjudbff4lDVd9tud800phYud8001udbffqud800Pud800ssPWFD:hud800CAOud800MO<WT")]
    public required string Value { get; set; }
}
