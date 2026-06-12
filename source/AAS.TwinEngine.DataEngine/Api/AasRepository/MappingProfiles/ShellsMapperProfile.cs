using AAS.TwinEngine.DataEngine.Api.AasRepository.Responses;
using AAS.TwinEngine.DataEngine.Api.Shared;
using AAS.TwinEngine.DataEngine.DomainModel.AasRepository;

using AasCore.Aas3_1;

namespace AAS.TwinEngine.DataEngine.Api.AasRepository.MappingProfiles;

public static class ShellsMapperProfile
{
    public static ShellsDto ToDto(this Shells shells)
    {
        return new ShellsDto
        {
            PagingMetaData = new PagingMetaDataDto
            {
                Cursor = shells.PagingMetaData?.Cursor
            },
            Result = shells.Result?.Select(Jsonization.Serialize.ToJsonObject).ToList()
        };
    }
}
