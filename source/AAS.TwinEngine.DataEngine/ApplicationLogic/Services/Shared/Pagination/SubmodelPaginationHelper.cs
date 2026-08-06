using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Shared.Pagination;

public sealed record SubmodelPaginationPageResult(List<string> SubmodelIds, string? NextCursor);

public sealed class SubmodelPaginationState(SubmodelPaginationCursor? cursor)
{
    public List<string> CollectedIds { get; } = [];
    public string? TrackingAasId { get; set; } = cursor?.AasId;
    public string? LastCollectedSubmodelId { get; set; }
    public string? SkipToSubmodelId { get; set; } = cursor?.SubmodelId;
    public bool IsFirstAasInResume { get; set; } = cursor is not null;
}

public static class SubmodelPaginationHelper
{
    public static async Task<SubmodelPaginationPageResult> CollectSubmodelPageAsync<TShell>(
        int pageSize,
        string? encodedCursor,
        Func<int, string?, CancellationToken, Task<(List<TShell> Shells, string? NextShellCursor)>> fetchShellBatchAsync,
        Func<TShell, string?> getShellId,
        Func<TShell, CancellationToken, Task<List<string>>> getSubmodelIdsAsync,
        CancellationToken cancellationToken)
    {
        var incomingCursor = SubmodelPaginationCursor.Decode(encodedCursor);
        var state = new SubmodelPaginationState(incomingCursor);
        var pluginCursor = state.TrackingAasId;

        while (state.CollectedIds.Count < pageSize)
        {
            var (shellBatch, nextShellCursor) = await fetchShellBatchAsync(pageSize, pluginCursor, cancellationToken).ConfigureAwait(false);

            var validShells = shellBatch.Where(s => !string.IsNullOrWhiteSpace(getShellId(s))).ToList();

            if (validShells.Count == 0)
            {
                break;
            }

            var limitReached = await ProcessShellBatchAsync(validShells, pageSize, state, getShellId, getSubmodelIdsAsync, cancellationToken).ConfigureAwait(false);

            if (limitReached || nextShellCursor is null)
            {
                break;
            }

            pluginCursor = state.TrackingAasId;
        }

        var nextCursor = state.CollectedIds.Count >= pageSize
            ? SubmodelPaginationCursor.Encode(state.LastCollectedSubmodelId, state.TrackingAasId)
            : null;

        return new SubmodelPaginationPageResult(state.CollectedIds, nextCursor);
    }

    private static async Task<bool> ProcessShellBatchAsync<TShell>(
        List<TShell> shellList,
        int pageSize,
        SubmodelPaginationState state,
        Func<TShell, string?> getShellId,
        Func<TShell, CancellationToken, Task<List<string>>> getSubmodelIdsAsync,
        CancellationToken cancellationToken)
    {
        foreach (var shell in shellList)
        {
            var shellId = getShellId(shell);
            var submodelIds = await getSubmodelIdsAsync(shell, cancellationToken).ConfigureAwait(false);

            if (submodelIds.Count == 0)
            {
                state.TrackingAasId = shellId;
                state.IsFirstAasInResume = false;
                continue;
            }

            var startIndex = ResolveStartIndex(state, submodelIds);

            if (CollectSubmodelIdsForShell(submodelIds, startIndex, pageSize, shellId, state))
            {
                return true;
            }

            state.TrackingAasId = shellId;
        }

        return false;
    }

    private static int ResolveStartIndex(SubmodelPaginationState state, List<string> submodelIds)
    {
        state.IsFirstAasInResume = false;

        if (state.SkipToSubmodelId is null)
        {
            return 0;
        }

        var index = submodelIds.IndexOf(state.SkipToSubmodelId) + 1;
        state.SkipToSubmodelId = null;
        return index;
    }

    private static bool CollectSubmodelIdsForShell(
        List<string> submodelIds,
        int startIndex,
        int pageSize,
        string? shellId,
        SubmodelPaginationState state)
    {
        for (var i = startIndex; i < submodelIds.Count; i++)
        {
            var submodelId = submodelIds[i];
            if (!state.CollectedIds.Contains(submodelId, StringComparer.OrdinalIgnoreCase))
            {
                state.CollectedIds.Add(submodelId);
            }

            state.LastCollectedSubmodelId = submodelId;

            if (state.CollectedIds.Count < pageSize)
            {
                continue;
            }

            if (state.CollectedIds.Contains(submodelIds[^1], StringComparer.OrdinalIgnoreCase))
            {
                state.TrackingAasId = shellId;
            }

            return true;
        }

        return false;
    }
}
