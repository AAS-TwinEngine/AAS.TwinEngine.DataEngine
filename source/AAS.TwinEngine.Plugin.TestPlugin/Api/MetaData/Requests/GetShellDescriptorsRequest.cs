namespace AAS.TwinEngine.Plugin.TestPlugin.Api.MetaData.Requests;

public record GetShellDescriptorsRequest(int? Limit, string? Cursor, string? AssetIdsFilter = null);
