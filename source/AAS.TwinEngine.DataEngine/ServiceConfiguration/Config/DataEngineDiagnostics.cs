using System.Diagnostics;

namespace AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

/// <summary>
/// Central ActivitySource and span/attribute name constants for DataEngine business-level tracing.
/// Follow the same static-constants pattern as <see cref="HttpClientNames"/> and <see cref="ApiPaths"/>.
/// </summary>
public static class DataEngineDiagnostics
{
    public const string SourceName = "TwinEngine.DataEngine";

    public static readonly ActivitySource Source = new(SourceName);

    public static class Spans
    {
        public const string ResolveTemplateId = "ResolveTemplateId";
        public const string FetchTemplate = "FetchTemplate";
        public const string ResolveTemplate = "ResolveTemplate";
        public const string GenerateSubmodelIds = "GenerateSubmodelIds";
        public const string GetProductId = "GetProductId";
        public const string ExtractSemanticIds = "ExtractSemanticIds";

        public const string PluginResolution = "PluginResolution";
        public const string RequestGeneration = "RequestGeneration";
        public const string FetchPluginData = "FetchPluginData";
        public const string FillDataIntoTemplate = "FillDataIntoTemplate";

        public const string FetchPluginMetadata = "FetchPluginMetadata";
        public const string FetchAssetInformation = "FetchAssetInformation";
        public const string SearchAssetsByAssetIds = "SearchAssetsByAssetIds";
    }

    public static class Attributes
    {
        // Entity identifiers
        public const string SubmodelId = "aas.submodel_id";
        public const string TemplateId = "aas.template_id";
        public const string ShellId = "aas.shell_id";
    }

    // ── Template resolution ───────────────────────────────────────────────────

    /// <summary>Starts a <see cref="Spans.ResolveTemplateId"/> span for a submodel.</summary>
    public static Activity? StartResolveSubmodelTemplateId(string submodelId)
    {
        var activity = Source.StartActivity(Spans.ResolveTemplateId);
        activity?.SetTag(Attributes.SubmodelId, submodelId);
        return activity;
    }

    /// <summary>Starts a <see cref="Spans.ResolveTemplateId"/> span for a shell.</summary>
    public static Activity? StartResolveShellTemplateId(string shellId)
    {
        var activity = Source.StartActivity(Spans.ResolveTemplateId);
        activity?.SetTag(Attributes.ShellId, shellId);
        return activity;
    }

    /// <summary>Starts a <see cref="Spans.FetchTemplate"/> span.</summary>
    public static Activity? StartFetchTemplate(string templateId)
    {
        var activity = Source.StartActivity(Spans.FetchTemplate);
        activity?.SetTag(Attributes.TemplateId, templateId);
        return activity;
    }

    /// <summary>Starts a <see cref="Spans.ResolveTemplate"/> span for a submodel.</summary>
    public static Activity? StartResolveTemplate(string submodelId)
    {
        var activity = Source.StartActivity(Spans.ResolveTemplate);
        activity?.SetTag(Attributes.SubmodelId, submodelId);
        return activity;
    }

    /// <summary>Starts a <see cref="Spans.GenerateSubmodelIds"/> span.</summary>
    public static Activity? StartGenerateSubmodelIds(string shellId)
    {
        var activity = Source.StartActivity(Spans.GenerateSubmodelIds);
        activity?.SetTag(Attributes.ShellId, shellId);
        return activity;
    }

    /// <summary>Starts a <see cref="Spans.GetProductId"/> span.</summary>
    public static Activity? StartGetProductId(string shellId)
    {
        var activity = Source.StartActivity(Spans.GetProductId);
        activity?.SetTag(Attributes.ShellId, shellId);
        return activity;
    }

    /// <summary>Starts an <see cref="Spans.ExtractSemanticIds"/> span.</summary>
    public static Activity? StartExtractSemanticIds() => Source.StartActivity(Spans.ExtractSemanticIds);

    // ── Fill data into template ───────────────────────────────────────────────

    /// <summary>Starts a <see cref="Spans.FillDataIntoTemplate"/> span for a submodel template (tags template ID).</summary>
    public static Activity? StartFillDataIntoTemplate(string templateId)
    {
        var activity = Source.StartActivity(Spans.FillDataIntoTemplate);
        activity?.SetTag(Attributes.TemplateId, templateId);
        return activity;
    }

    /// <summary>Starts a <see cref="Spans.FillDataIntoTemplate"/> span for a shell descriptor (tags both shell ID and template ID).</summary>
    public static Activity? StartFillShellDataIntoTemplate(string shellId, string templateId)
    {
        var activity = Source.StartActivity(Spans.FillDataIntoTemplate);
        activity?.SetTag(Attributes.ShellId, shellId);
        activity?.SetTag(Attributes.TemplateId, templateId);
        return activity;
    }

    /// <summary>Starts a <see cref="Spans.FillDataIntoTemplate"/> span for asset information (tags shell ID).</summary>
    public static Activity? StartFillAssetInformationIntoTemplate(string shellId)
    {
        var activity = Source.StartActivity(Spans.FillDataIntoTemplate);
        activity?.SetTag(Attributes.ShellId, shellId);
        return activity;
    }

    // ── Plugin pipeline ───────────────────────────────────────────────────────

    /// <summary>Starts a <see cref="Spans.PluginResolution"/> span.</summary>
    public static Activity? StartPluginResolution() => Source.StartActivity(Spans.PluginResolution);

    /// <summary>Starts a <see cref="Spans.RequestGeneration"/> span.</summary>
    public static Activity? StartRequestGeneration() => Source.StartActivity(Spans.RequestGeneration);

    /// <summary>Starts a <see cref="Spans.FetchPluginData"/> span for a submodel.</summary>
    public static Activity? StartFetchPluginData(string submodelId)
    {
        var activity = Source.StartActivity(Spans.FetchPluginData);
        activity?.SetTag(Attributes.SubmodelId, submodelId);
        return activity;
    }

    /// <summary>Starts a <see cref="Spans.FetchPluginMetadata"/> span (no entity tag).</summary>
    public static Activity? StartFetchPluginMetadata() => Source.StartActivity(Spans.FetchPluginMetadata);

    /// <summary>Starts a <see cref="Spans.FetchPluginMetadata"/> span for a shell.</summary>
    public static Activity? StartFetchShellDescriptorMetadata(string shellId)
    {
        var activity = Source.StartActivity(Spans.FetchPluginMetadata);
        activity?.SetTag(Attributes.ShellId, shellId);
        return activity;
    }

    /// <summary>Starts a <see cref="Spans.FetchPluginMetadata"/> span for a submodel descriptor.</summary>
    public static Activity? StartFetchSubmodelDescriptorMetadata(string submodelId)
    {
        var activity = Source.StartActivity(Spans.FetchPluginMetadata);
        activity?.SetTag(Attributes.SubmodelId, submodelId);
        return activity;
    }

    /// <summary>Starts a <see cref="Spans.FetchAssetInformation"/> span.</summary>
    public static Activity? StartFetchAssetInformation(string shellId)
    {
        var activity = Source.StartActivity(Spans.FetchAssetInformation);
        activity?.SetTag(Attributes.ShellId, shellId);
        return activity;
    }

    /// <summary>Starts a <see cref="Spans.SearchAssetsByAssetIds"/> span.</summary>
    public static Activity? StartSearchAssetsByAssetIds() => Source.StartActivity(Spans.SearchAssetsByAssetIds);

    // ── Error helpers ─────────────────────────────────────────────────────────

    /// <summary>
    /// Sets the activity status to <see cref="ActivityStatusCode.Error"/> with the exception message.
    /// Safe to call on a null activity.
    /// </summary>
    public static void RecordError(this Activity? activity, Exception ex)
        => activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

    /// <summary>
    /// Sets the activity status to <see cref="ActivityStatusCode.Error"/> with the provided description.
    /// Safe to call on a null activity.
    /// </summary>
    public static void RecordError(this Activity? activity, string description)
        => activity?.SetStatus(ActivityStatusCode.Error, description);
}
