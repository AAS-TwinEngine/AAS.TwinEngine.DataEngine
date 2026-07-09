using AAS.TwinEngine.DataEngine.DomainModel.Description;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Description;

public interface IDescriptionService
{
    ServiceDescription GetDescriptor(CancellationToken cancellationToken);
}
