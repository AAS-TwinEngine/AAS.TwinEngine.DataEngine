namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.Config;

public class AasxExportOptions
{
    public const string Section = "AasxExportOptions";

    public const string DefaultXmlFileName = "content.xml";

    public string RootFolder { get; set; } = "aasx";
}
