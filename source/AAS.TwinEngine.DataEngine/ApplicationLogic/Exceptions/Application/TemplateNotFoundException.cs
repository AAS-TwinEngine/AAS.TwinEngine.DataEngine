using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Base;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;

public class TemplateNotFoundException : NotFoundException
{
    public const string ServiceName = "template";

    public TemplateNotFoundException() { }

    public TemplateNotFoundException(Exception ex) : base(ServiceName, ex) { }
}
