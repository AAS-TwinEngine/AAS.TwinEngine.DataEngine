namespace AAS.TwinEngine.DataEngine.Api.Discovery.Requests;

public record SearchShellsByAssetLinkRequest(AssetLinkDto[] AssetLinks, int Limit, string? Cursor);
