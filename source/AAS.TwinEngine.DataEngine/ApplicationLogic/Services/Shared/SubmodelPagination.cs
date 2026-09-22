using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Shared;

internal sealed record SubmodelPageResult(List<string> SubmodelIds, string? NextCursor);

internal sealed class SubmodelPaginationState(SubmodelPaginationCursor? cursor, int capacity = 0)
{
    public List<string> CollectedIds { get; } = capacity > 0 ? new(capacity) : [];
    public string? TrackingAasId { get; set; } = cursor?.AasId;
    public string? LastCollectedSubmodelId { get; set; }
    public string? ResumeAfterSubmodelId { get; set; } = cursor?.SubmodelId;

    public bool CollectSubmodelIds(IList<string?> submodelIds, string shellId, int pageSize)
    {
        if (submodelIds.Count == 0)
        {
            TrackingAasId = shellId;
            ResumeAfterSubmodelId = null;
            return false;
        }

        var startIndex = 0;

        if (ResumeAfterSubmodelId is not null)
        {
            startIndex = submodelIds.IndexOf(ResumeAfterSubmodelId) + 1;
            ResumeAfterSubmodelId = null;
        }

        for (var i = startIndex; i < submodelIds.Count; i++)
        {
            CollectedIds.Add(submodelIds[i]);
            LastCollectedSubmodelId = submodelIds[i];

            if (CollectedIds.Count >= pageSize)
            {
                if (submodelIds[^1] == LastCollectedSubmodelId)
                {
                    TrackingAasId = shellId;
                }

                return true;
            }
        }

        TrackingAasId = shellId;
        return false;
    }

    public string? BuildNextCursor(int pageSize) =>
        CollectedIds.Count >= pageSize ? SubmodelPaginationCursor.Encode(LastCollectedSubmodelId, TrackingAasId) : null;
}
