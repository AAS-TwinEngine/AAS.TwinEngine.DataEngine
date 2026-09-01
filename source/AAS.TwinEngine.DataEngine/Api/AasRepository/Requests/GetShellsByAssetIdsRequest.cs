namespace AAS.TwinEngine.DataEngine.Api.AasRepository.Requests;

public record GetShellsByAssetIdsRequest(string[]? AssetIds, string? IdShort, int Limit, string? Cursor);
