using System.Diagnostics;

namespace AAS.TwinEngine.ExportService.ApplicationLogic.Observability;

/// <summary>
/// OpenTelemetry activity source and span/attribute names used by the Export Service.
/// Mirrors the pattern used in <c>AAS.TwinEngine.DataEngine</c> (<c>DataEngineTracing</c>).
/// </summary>
public static class ExportServiceTracing
{
    public const string SourceName = "ExportService";

    public static readonly ActivitySource Source = new(SourceName);

    public static class Spans
    {
        public const string ExportRun = "Export Run";
        public const string ExportPhase = "Export Phase";
        public const string FetchSourceEntities = "Fetch Source Entities";
        public const string DecideCrudOperations = "Decide CRUD Operations";
        public const string WriteEntityToTarget = "Write Entity To Target";
        public const string AcquireToken = "Acquire Token";
    }

    public static class Attributes
    {
        public const string EntityKind = "export.entity_kind";
        public const string EntityIdentifier = "export.entity_identifier";
        public const string Operation = "export.operation";
        public const string Created = "export.created_count";
        public const string Updated = "export.updated_count";
        public const string Deleted = "export.deleted_count";
        public const string Skipped = "export.skipped_count";
        public const string Failed = "export.failed_count";
    }

    public static Activity? StartSpan(string spanName) => Source.StartActivity(spanName);

    public static Activity? StartSpan(string spanName, string tagName, object? tagValue)
    {
        var activity = Source.StartActivity(spanName);
        _ = activity?.SetTag(tagName, tagValue);
        return activity;
    }

    public static void RecordError(this Activity? activity, Exception ex)
        => activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
}
