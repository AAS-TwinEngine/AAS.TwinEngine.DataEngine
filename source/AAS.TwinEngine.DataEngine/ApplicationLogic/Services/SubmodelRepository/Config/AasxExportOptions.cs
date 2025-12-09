using System.ComponentModel.DataAnnotations;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.Config;

public class AasxExportOptions
{
    public const string Section = "AasxExportOptions";

    public const string DefaultXmlFileName = "content.xml";

    [Required]
    public string RootFolder { get; set; }

}
