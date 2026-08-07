using System.Diagnostics;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Observability;

public static class DataEngineTracing
{
    public const string SourceName = "DataEngine";

    public static readonly ActivitySource Source = new(SourceName);

    public static class Spans
    {
        public const string GetShellTemplate = "Get Shell Template";
        public const string GetSubmodelTemplate = "Get Submodel Template";
        public const string GetShellDescriptorTemplate = "Get Shell Descriptor Template";
        public const string GetSubmodelDescriptorTemplate = "Get Submodel Descriptor Template";
        public const string GetSubmodelRefTemplate = "Get Submodel Ref Template";
        public const string GetConceptDescription = "Get Concept Description";

        public const string GetProductId = "Get ProductId";

        public const string PluginRequestGeneration = "Plugin Request Generation";
        public const string GetPluginData = "Get Plugin Data";

        public const string GetPluginMetadataShells = "Get Plugin Metadata-shells";

        public const string GetPluginMetadataAssets = "Get Plugin Metadata-assets";

        public const string CacheFetch = "Cache Fetch";

        public const string HttpFetch = "Http Fetch";
        public const string LoadFileAttachment = "Load File Attachment";
        public const string StreamResponse = "Stream Response";
    }

    public static class Attributes
    {
        public const string SubmodelId = "aas.submodel_id";
        public const string TemplateId = "aas.template_id";
        public const string ShellId = "aas.shell_id";
    }

    public static Activity? StartSpan(string spanName)
        => Source.StartActivity(spanName);

    public static Activity? StartSpan(string spanName, ActivityContext parentContext)
        => Source.StartActivity(spanName, ActivityKind.Internal, parentContext);

    public static Activity? StartSpan(string spanName, string tagName, object? tagValue)
    {
        var activity = Source.StartActivity(spanName);
        _ = activity?.SetTag(tagName, tagValue);
        return activity;
    }

    public static void RecordError(this Activity? activity, Exception ex)
        => activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

    public static void RecordError(this Activity? activity, string description)
        => activity?.SetStatus(ActivityStatusCode.Error, description);
}
