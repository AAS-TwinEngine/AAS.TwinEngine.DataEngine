namespace AAS.TwinEngine.ExportService.Infrastructure.Scheduling;

/// <summary>
/// Non-blocking mutual exclusion for export runs. Guarantees that at most one run is
/// executing at any time; the next scheduled tick is skipped if a previous run is still
/// in progress.
/// </summary>
public interface IExportRunLock
{
    /// <summary>
    /// Attempts to acquire the run lock. Returns <c>null</c> if a run is already in progress.
    /// The caller must dispose the returned handle when the run completes.
    /// </summary>
    IDisposable? TryAcquire();
}
