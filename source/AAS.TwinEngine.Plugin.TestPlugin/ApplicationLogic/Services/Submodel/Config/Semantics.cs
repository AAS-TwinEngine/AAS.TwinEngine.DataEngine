using System.ComponentModel.DataAnnotations;

namespace AAS.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Services.Submodel.Config;

public class Semantics
{
    public const string Section = "Semantics";

    [Required]
    public string IndexContextPrefix { get; set; }
}

