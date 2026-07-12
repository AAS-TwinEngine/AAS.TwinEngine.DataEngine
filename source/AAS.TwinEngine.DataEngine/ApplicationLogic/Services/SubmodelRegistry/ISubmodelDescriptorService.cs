using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRegistry;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRegistry;

public interface ISubmodelDescriptorService
{
    Task<SubmodelDescriptors> GetAllSubmodelDescriptorsAsync(int? limit, string? cursor, CancellationToken cancellationToken);

    Task<SubmodelDescriptor> GetSubmodelDescriptorByIdAsync(string id, CancellationToken cancellationToken);
}
