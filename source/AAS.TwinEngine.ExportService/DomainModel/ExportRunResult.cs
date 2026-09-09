namespace AAS.TwinEngine.ExportService.DomainModel;

public enum RunStatus
{
    Success = 0,
    PartialFailure = 1,
    Aborted = 2
}

public sealed record ExportRunResult(
    DateTimeOffset StartedAt,
    DateTimeOffset FinishedAt,
    RunStatus Status,
    IReadOnlyList<PhaseResult> Phases);
