using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRegistry;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRegistry.Providers;

public interface ISubmodelDescriptorProvider
{
    Task<SubmodelDescriptor> GetDataForSubmodelDescriptorByIdAsync(string id, CancellationToken cancellationToken);
}
