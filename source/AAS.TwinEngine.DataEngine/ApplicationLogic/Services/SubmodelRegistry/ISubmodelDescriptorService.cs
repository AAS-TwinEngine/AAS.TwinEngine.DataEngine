using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRegistry;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRegistry;

public interface ISubmodelDescriptorService
{
    Task<SubmodelDescriptor> GetSubmodelDescriptorByIdAsync(string id, CancellationToken cancellationToken);
}
