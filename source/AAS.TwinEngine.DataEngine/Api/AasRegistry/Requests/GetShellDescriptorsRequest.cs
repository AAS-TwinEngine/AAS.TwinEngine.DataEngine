using AasCore.Aas3_1;

namespace AAS.TwinEngine.DataEngine.Api.AasRegistry.Requests;

public record GetShellDescriptorsRequest(int Limit, string? Cursor, AssetKind? AssetKind, string? AssetType);
