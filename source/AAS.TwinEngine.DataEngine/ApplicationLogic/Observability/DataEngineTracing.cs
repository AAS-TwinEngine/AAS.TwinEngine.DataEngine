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
    }

    public static class Attributes
    {
        public const string SubmodelId = "aas.submodel_id";
        public const string TemplateId = "aas.template_id";
        public const string ShellId = "aas.shell_id";
    }

    public static Activity? StartGetShellTemplate(string templateId)
    {
        var activity = Source.StartActivity(Spans.GetShellTemplate);
        _ = activity?.SetTag(Attributes.TemplateId, templateId);
        return activity;
    }

    public static Activity? StartGetSubmodelTemplate(string templateId)
    {
        var activity = Source.StartActivity(Spans.GetSubmodelTemplate);
        _ = activity?.SetTag(Attributes.TemplateId, templateId);
        return activity;
    }

    public static Activity? StartGetShellDescriptorTemplate(string templateId)
    {
        var activity = Source.StartActivity(Spans.GetShellDescriptorTemplate);
        _ = activity?.SetTag(Attributes.TemplateId, templateId);
        return activity;
    }

    public static Activity? StartGetSubmodelDescriptorTemplate(string templateId)
    {
        var activity = Source.StartActivity(Spans.GetSubmodelDescriptorTemplate);
        _ = activity?.SetTag(Attributes.TemplateId, templateId);
        return activity;
    }
    public static Activity? StartGetSubmodelRefTemplate(string templateId)
    {
        var activity = Source.StartActivity(Spans.GetSubmodelRefTemplate);
        _ = activity?.SetTag(Attributes.TemplateId, templateId);
        return activity;
    }

    public static Activity? StartGetConceptDescription(string cdIdentifier)
    {
        var activity = Source.StartActivity(Spans.GetConceptDescription);
        _ = activity?.SetTag(Attributes.TemplateId, cdIdentifier);
        return activity;
    }

    public static Activity? StartGetProductId(string shellId)
    {
        var activity = Source.StartActivity(Spans.GetProductId);
        _ = activity?.SetTag(Attributes.ShellId, shellId);
        return activity;
    }

    public static Activity? StartPluginRequestGeneration() => Source.StartActivity(Spans.PluginRequestGeneration);

    public static Activity? StartGetPluginData(string submodelId)
    {
        var activity = Source.StartActivity(Spans.GetPluginData);
        _ = activity?.SetTag(Attributes.SubmodelId, submodelId);
        return activity;
    }

    public static Activity? StartGetPluginMetadataShells() => Source.StartActivity(Spans.GetPluginMetadataShells);

    public static Activity? StartGetPluginMetadataShells(string shellId)
    {
        var activity = Source.StartActivity(Spans.GetPluginMetadataShells);
        _ = activity?.SetTag(Attributes.ShellId, shellId);
        return activity;
    }

    public static Activity? StartGetPluginMetadataAssets(string assetId)
    {
        var activity = Source.StartActivity(Spans.GetPluginMetadataAssets);
        _ = activity?.SetTag(Attributes.ShellId, assetId);
        return activity;
    }

    public static void RecordError(this Activity? activity, Exception ex)
        => activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

    public static void RecordError(this Activity? activity, string description)
        => activity?.SetStatus(ActivityStatusCode.Error, description);
}
