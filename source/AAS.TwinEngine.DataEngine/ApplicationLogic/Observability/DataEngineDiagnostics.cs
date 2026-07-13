using System.Diagnostics;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Observability;

public static class DataEngineDiagnostics
{
    public const string SourceName = "DataEngine";

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
    }

    public static class Attributes
    {
        public const string SubmodelId = "aas.submodel_id";
        public const string TemplateId = "aas.template_id";
        public const string ShellId = "aas.shell_id";
    }

    public static Activity? StartResolveSubmodelTemplateId(string submodelId)
    {
        var activity = Source.StartActivity(Spans.ResolveTemplateId);
        _ = activity?.SetTag(Attributes.SubmodelId, submodelId);
        return activity;
    }

    public static Activity? StartResolveShellTemplateId(string shellId)
    {
        var activity = Source.StartActivity(Spans.ResolveTemplateId);
        _ = activity?.SetTag(Attributes.ShellId, shellId);
        return activity;
    }

    public static Activity? StartFetchTemplate(string templateId)
    {
        var activity = Source.StartActivity(Spans.FetchTemplate);
        _ = activity?.SetTag(Attributes.TemplateId, templateId);
        return activity;
    }

    public static Activity? StartResolveTemplate(string submodelId)
    {
        var activity = Source.StartActivity(Spans.ResolveTemplate);
        _ = activity?.SetTag(Attributes.SubmodelId, submodelId);
        return activity;
    }

    public static Activity? StartGenerateSubmodelIds(string shellId)
    {
        var activity = Source.StartActivity(Spans.GenerateSubmodelIds);
        _ = activity?.SetTag(Attributes.ShellId, shellId);
        return activity;
    }

    public static Activity? StartGetProductId(string shellId)
    {
        var activity = Source.StartActivity(Spans.GetProductId);
        _ = activity?.SetTag(Attributes.ShellId, shellId);
        return activity;
    }

    public static Activity? StartExtractSemanticIds() => Source.StartActivity(Spans.ExtractSemanticIds);

    public static Activity? StartFillDataIntoTemplate(string templateId)
    {
        var activity = Source.StartActivity(Spans.FillDataIntoTemplate);
        _ = activity?.SetTag(Attributes.TemplateId, templateId);
        return activity;
    }

    public static Activity? StartFillShellDataIntoTemplate(string shellId, string templateId)
    {
        var activity = Source.StartActivity(Spans.FillDataIntoTemplate);
        _ = activity?.SetTag(Attributes.ShellId, shellId);
        _ = activity?.SetTag(Attributes.TemplateId, templateId);
        return activity;
    }

    public static Activity? StartFillAssetInformationIntoTemplate(string shellId)
    {
        var activity = Source.StartActivity(Spans.FillDataIntoTemplate);
        _ = activity?.SetTag(Attributes.ShellId, shellId);
        return activity;
    }

    public static Activity? StartPluginResolution() => Source.StartActivity(Spans.PluginResolution);

    public static Activity? StartRequestGeneration() => Source.StartActivity(Spans.RequestGeneration);

    public static Activity? StartFetchPluginData(string submodelId)
    {
        var activity = Source.StartActivity(Spans.FetchPluginData);
        _ = activity?.SetTag(Attributes.SubmodelId, submodelId);
        return activity;
    }

    public static Activity? StartFetchPluginMetadata() => Source.StartActivity(Spans.FetchPluginMetadata);

    public static Activity? StartFetchShellDescriptorMetadata(string shellId)
    {
        var activity = Source.StartActivity(Spans.FetchPluginMetadata);
        _ = activity?.SetTag(Attributes.ShellId, shellId);
        return activity;
    }

    public static Activity? StartFetchSubmodelDescriptorMetadata(string submodelId)
    {
        var activity = Source.StartActivity(Spans.FetchPluginMetadata);
        _ = activity?.SetTag(Attributes.SubmodelId, submodelId);
        return activity;
    }

    public static Activity? StartFetchAssetInformation(string shellId)
    {
        var activity = Source.StartActivity(Spans.FetchAssetInformation);
        _ = activity?.SetTag(Attributes.ShellId, shellId);
        return activity;
    }

    public static void RecordError(this Activity? activity, Exception ex)
        => activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

    public static void RecordError(this Activity? activity, string description)
        => activity?.SetStatus(ActivityStatusCode.Error, description);
}
