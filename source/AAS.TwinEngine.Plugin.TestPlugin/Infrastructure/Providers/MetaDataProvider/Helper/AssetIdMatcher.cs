using AAS.TwinEngine.Plugin.TestPlugin.DomainModel.MetaData;

namespace AAS.TwinEngine.Plugin.TestPlugin.Infrastructure.Providers.MetaDataProvider.Helper;

public static class AssetIdMatcher
{
    public static bool MatchesAllIdentifiers(ShellDescriptorData shellDescriptor, AssetIdFilterHeader filter)
    {
        if (filter == null || filter.Identifiers.Count == 0)
        {
            return true;
        }

        foreach (var identifier in filter.Identifiers)
        {
            if (!MatchesSingleIdentifier(shellDescriptor, identifier))
            {
                return false;
            }
        }

        return true;
    }

    private static bool MatchesSingleIdentifier(ShellDescriptorData shellDescriptor, SpecificAssetIdsData identifier)
    {
        if (string.Equals(identifier.Name, "globalAssetId", StringComparison.Ordinal))
        {
            return MatchesGlobalAssetId(shellDescriptor.GlobalAssetId, identifier.Value);
        }

        return MatchesSpecificAssetId(shellDescriptor.SpecificAssetIds, identifier);
    }

    private static bool MatchesGlobalAssetId(string? shellGlobalAssetId, string identifierValue)
    {
        if (string.IsNullOrEmpty(shellGlobalAssetId))
        {
            return false;
        }

        return string.Equals(shellGlobalAssetId, identifierValue, StringComparison.Ordinal);
    }

    private static bool MatchesSpecificAssetId(IList<SpecificAssetIdsData>? shellAssets, SpecificAssetIdsData identifier)
    {
        if (shellAssets == null || shellAssets.Count == 0)
        {
            return false;
        }

        foreach (var shellAsset in shellAssets)
        {
            if (MatchesNameAndValue(shellAsset, identifier))
            {
                return true;
            }
        }

        return false;
    }

    private static bool MatchesNameAndValue(SpecificAssetIdsData shellAsset, SpecificAssetIdsData identifier)
    {
        if (string.IsNullOrEmpty(shellAsset.Name) || string.IsNullOrEmpty(shellAsset.Value))
        {
            return false;
        }

        return string.Equals(shellAsset.Name, identifier.Name, StringComparison.Ordinal)
            && string.Equals(shellAsset.Value, identifier.Value, StringComparison.Ordinal);
    }
}
