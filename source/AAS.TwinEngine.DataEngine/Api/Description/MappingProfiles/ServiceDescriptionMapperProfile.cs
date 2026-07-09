using AAS.TwinEngine.DataEngine.Api.Description.Responses;
using AAS.TwinEngine.DataEngine.DomainModel.Description;

namespace AAS.TwinEngine.DataEngine.Api.Description.MappingProfiles;

public static class ServiceDescriptionMapperProfile
{
    public static ServiceDescriptionDto ToDto(this ServiceDescription serviceDescription)
    {
        return new ServiceDescriptionDto
        {
            Profiles = serviceDescription.Profiles ?? []
        };
    }
}
