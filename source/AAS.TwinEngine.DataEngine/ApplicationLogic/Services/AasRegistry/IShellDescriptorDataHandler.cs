using AAS.TwinEngine.DataEngine.DomainModel.AasRegistry;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasRegistry;

public interface IShellDescriptorDataHandler
{
    ShellDescriptor FillOut(ShellDescriptor template, ShellDescriptorMetaData metaData);
}
