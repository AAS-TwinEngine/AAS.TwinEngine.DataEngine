using AAS.TwinEngine.DataEngine.Api.Discovery.Responses;
using AAS.TwinEngine.DataEngine.Api.Shared;
using AAS.TwinEngine.DataEngine.DomainModel.Discovery;

namespace AAS.TwinEngine.DataEngine.Api.Discovery.MappingProfiles;

public static class ShellsByAssetLinkMapperProfile
{
    public static ShellsByAssetLinkResponseDto ToDto(this ShellsByAssetLink shellsByAssetLink)
    {
        return new ShellsByAssetLinkResponseDto
        {
            PagingMetaData = new PagingMetaDataDto
            {
                Cursor = shellsByAssetLink.PagingMetaData?.Cursor
            },
            Result = shellsByAssetLink.Result?.ToList()
        };
    }
}
