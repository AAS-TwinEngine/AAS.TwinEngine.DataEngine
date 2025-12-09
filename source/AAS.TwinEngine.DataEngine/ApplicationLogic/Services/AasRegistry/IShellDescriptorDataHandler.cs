using AAS.TwinEngine.DataEngine.DomainModel.AasRegistry;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasRegistry;

public interface IShellDescriptorDataHandler
{
    IList<ShellDescriptor> FillOut(ShellDescriptor template, IList<ShellDescriptorMetaData> metaData);

    ShellDescriptor FillOut(ShellDescriptor template, ShellDescriptorMetaData metaData);
}
