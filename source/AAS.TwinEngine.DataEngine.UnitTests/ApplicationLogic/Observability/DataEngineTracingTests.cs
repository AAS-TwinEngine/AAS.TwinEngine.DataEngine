using System.Diagnostics;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Observability;

namespace AAS.TwinEngine.DataEngine.UnitTests.ApplicationLogic.Observability;

public class DataEngineTracingTests
{
    [Fact]
    public void SourceName_ReturnsExpectedValue() => Assert.Equal("DataEngine", DataEngineTracing.SourceName);

    [Fact]
    public void Source_IsNotNull() => Assert.NotNull(DataEngineTracing.Source);

    [Fact]
    public void Source_HasCorrectName() => Assert.Equal(DataEngineTracing.SourceName, DataEngineTracing.Source.Name);

    private ActivityListenerFixture CreateFixture() => new();

    [Fact]
    public void SpanNames_AreCorrect()
    {
        Assert.Equal("Get Shell Template", DataEngineTracing.Spans.GetShellTemplate);
        Assert.Equal("Get Submodel Template", DataEngineTracing.Spans.GetSubmodelTemplate);
        Assert.Equal("Get Shell Descriptor Template", DataEngineTracing.Spans.GetShellDescriptorTemplate);
        Assert.Equal("Get Submodel Descriptor Template", DataEngineTracing.Spans.GetSubmodelDescriptorTemplate);
        Assert.Equal("Get Submodel Ref Template", DataEngineTracing.Spans.GetSubmodelRefTemplate);
        Assert.Equal("Get Concept Description", DataEngineTracing.Spans.GetConceptDescription);
        Assert.Equal("Get ProductId", DataEngineTracing.Spans.GetProductId);
        Assert.Equal("Plugin Request Generation", DataEngineTracing.Spans.PluginRequestGeneration);
        Assert.Equal("Get Plugin Data", DataEngineTracing.Spans.GetPluginData);
        Assert.Equal("Get Plugin Metadata-shells", DataEngineTracing.Spans.GetPluginMetadataShells);
        Assert.Equal("Get Plugin Metadata-assets", DataEngineTracing.Spans.GetPluginMetadataAssets);
    }

    [Fact]
    public void AttributeNames_AreCorrect()
    {
        Assert.Equal("aas.submodel_id", DataEngineTracing.Attributes.SubmodelId);
        Assert.Equal("aas.template_id", DataEngineTracing.Attributes.TemplateId);
        Assert.Equal("aas.shell_id", DataEngineTracing.Attributes.ShellId);
    }

    [Fact]
    public void StartSpan_WithoutTag_CreatesActivityWithCorrectName()
    {
        using var fixture = CreateFixture();
        const string spanName = "Custom Span";
        using var activity = DataEngineTracing.StartSpan(spanName);

        var capturedActivity = Assert.Single(fixture.Activities);
        Assert.Equal(spanName, capturedActivity.OperationName);
        Assert.Empty(capturedActivity.Tags);
    }

    [Fact]
    public void StartSpan_WithTag_CreatesActivityAndSetsTag()
    {
        using var fixture = CreateFixture();
        const string spanName = "Tagged Span";
        const string tagName = "custom.tag";
        const string tagValue = "value-001";
        using var activity = DataEngineTracing.StartSpan(spanName, tagName, tagValue);

        var capturedActivity = Assert.Single(fixture.Activities);
        Assert.Equal(spanName, capturedActivity.OperationName);
        Assert.Equal(tagValue, capturedActivity.GetTagItem(tagName));
    }

    [Fact]
    public void StartSpan_GetShellTemplate_CreatesActivityWithTemplateTag()
    {
        using var fixture = CreateFixture();
        const string templateId = "template-shell-001";
        using var activity = DataEngineTracing.StartSpan(DataEngineTracing.Spans.GetShellTemplate, DataEngineTracing.Attributes.TemplateId, templateId);

        var capturedActivity = Assert.Single(fixture.Activities);
        Assert.Equal(DataEngineTracing.Spans.GetShellTemplate, capturedActivity.OperationName);
        Assert.Equal(templateId, capturedActivity.GetTagItem(DataEngineTracing.Attributes.TemplateId));
    }

    [Fact]
    public void StartSpan_GetSubmodelTemplate_CreatesActivityWithTemplateTag()
    {
        using var fixture = CreateFixture();
        const string templateId = "template-submodel-001";
        using var activity = DataEngineTracing.StartSpan(DataEngineTracing.Spans.GetSubmodelTemplate, DataEngineTracing.Attributes.TemplateId, templateId);

        var capturedActivity = Assert.Single(fixture.Activities);
        Assert.Equal(DataEngineTracing.Spans.GetSubmodelTemplate, capturedActivity.OperationName);
        Assert.Equal(templateId, capturedActivity.GetTagItem(DataEngineTracing.Attributes.TemplateId));
    }

    [Fact]
    public void StartSpan_GetShellDescriptorTemplate_CreatesActivityWithTemplateTag()
    {
        using var fixture = CreateFixture();
        const string templateId = "template-shell-descriptor-001";
        using var activity = DataEngineTracing.StartSpan(DataEngineTracing.Spans.GetShellDescriptorTemplate, DataEngineTracing.Attributes.TemplateId, templateId);

        var capturedActivity = Assert.Single(fixture.Activities);
        Assert.Equal(DataEngineTracing.Spans.GetShellDescriptorTemplate, capturedActivity.OperationName);
        Assert.Equal(templateId, capturedActivity.GetTagItem(DataEngineTracing.Attributes.TemplateId));
    }

    [Fact]
    public void StartSpan_GetSubmodelDescriptorTemplate_CreatesActivityWithTemplateTag()
    {
        using var fixture = CreateFixture();
        const string templateId = "template-submodel-descriptor-001";
        using var activity = DataEngineTracing.StartSpan(DataEngineTracing.Spans.GetSubmodelDescriptorTemplate, DataEngineTracing.Attributes.TemplateId, templateId);

        var capturedActivity = Assert.Single(fixture.Activities);
        Assert.Equal(DataEngineTracing.Spans.GetSubmodelDescriptorTemplate, capturedActivity.OperationName);
        Assert.Equal(templateId, capturedActivity.GetTagItem(DataEngineTracing.Attributes.TemplateId));
    }

    [Fact]
    public void StartSpan_GetSubmodelRefTemplate_CreatesActivityWithTemplateTag()
    {
        using var fixture = CreateFixture();
        const string templateId = "template-submodel-ref-001";
        using var activity = DataEngineTracing.StartSpan(DataEngineTracing.Spans.GetSubmodelRefTemplate, DataEngineTracing.Attributes.TemplateId, templateId);

        var capturedActivity = Assert.Single(fixture.Activities);
        Assert.Equal(DataEngineTracing.Spans.GetSubmodelRefTemplate, capturedActivity.OperationName);
        Assert.Equal(templateId, capturedActivity.GetTagItem(DataEngineTracing.Attributes.TemplateId));
    }

    [Fact]
    public void StartSpan_GetConceptDescription_CreatesActivityWithTemplateTag()
    {
        using var fixture = CreateFixture();
        const string conceptDescriptionId = "cd-001";
        using var activity = DataEngineTracing.StartSpan(DataEngineTracing.Spans.GetConceptDescription, DataEngineTracing.Attributes.TemplateId, conceptDescriptionId);

        var capturedActivity = Assert.Single(fixture.Activities);
        Assert.Equal(DataEngineTracing.Spans.GetConceptDescription, capturedActivity.OperationName);
        Assert.Equal(conceptDescriptionId, capturedActivity.GetTagItem(DataEngineTracing.Attributes.TemplateId));
    }

    [Fact]
    public void StartSpan_GetProductId_CreatesActivityWithCorrectName()
    {
        using var fixture = CreateFixture();
        using var activity = DataEngineTracing.StartSpan(DataEngineTracing.Spans.GetProductId, DataEngineTracing.Attributes.ShellId, "shell-prod");

        var capturedActivity = Assert.Single(fixture.Activities);
        Assert.Equal(DataEngineTracing.Spans.GetProductId, capturedActivity.OperationName);
    }

    [Fact]
    public void StartSpan_GetProductId_SetsShellIdTag()
    {
        using var fixture = CreateFixture();
        const string ShellId = "shell-prod-456";
        using var activity = DataEngineTracing.StartSpan(DataEngineTracing.Spans.GetProductId, DataEngineTracing.Attributes.ShellId, ShellId);

        var capturedActivity = Assert.Single(fixture.Activities);
        Assert.Equal(ShellId, capturedActivity.GetTagItem(DataEngineTracing.Attributes.ShellId));
    }

    [Fact]
    public void StartSpan_PluginRequestGeneration_CreatesActivityWithCorrectName()
    {
        using var fixture = CreateFixture();
        using var activity = DataEngineTracing.StartSpan(DataEngineTracing.Spans.PluginRequestGeneration);

        var capturedActivity = Assert.Single(fixture.Activities);
        Assert.Equal(DataEngineTracing.Spans.PluginRequestGeneration, capturedActivity.OperationName);
    }

    [Fact]
    public void StartSpan_GetPluginData_CreatesActivityWithSubmodelTag()
    {
        using var fixture = CreateFixture();
        const string submodelId = "submodel-plugin-001";
        using var activity = DataEngineTracing.StartSpan(DataEngineTracing.Spans.GetPluginData, DataEngineTracing.Attributes.SubmodelId, submodelId);

        var capturedActivity = Assert.Single(fixture.Activities);
        Assert.Equal(DataEngineTracing.Spans.GetPluginData, capturedActivity.OperationName);
        Assert.Equal(submodelId, capturedActivity.GetTagItem(DataEngineTracing.Attributes.SubmodelId));
    }

    [Fact]
    public void StartSpan_GetPluginMetadataShells_WithShellId_SetsTag()
    {
        using var fixture = CreateFixture();
        const string shellId = "shell-123";
        using var activity = DataEngineTracing.StartSpan(DataEngineTracing.Spans.GetPluginMetadataShells, DataEngineTracing.Attributes.ShellId, shellId);

        var capturedActivity = Assert.Single(fixture.Activities);
        Assert.Equal(DataEngineTracing.Spans.GetPluginMetadataShells, capturedActivity.OperationName);
        Assert.Equal(shellId, capturedActivity.GetTagItem(DataEngineTracing.Attributes.ShellId));
    }

    [Fact]
    public void StartSpan_GetPluginMetadataShells_WithoutTag_CreatesActivity()
    {
        using var fixture = CreateFixture();
        using var activity = DataEngineTracing.StartSpan(DataEngineTracing.Spans.GetPluginMetadataShells);

        var capturedActivity = Assert.Single(fixture.Activities);
        Assert.Equal(DataEngineTracing.Spans.GetPluginMetadataShells, capturedActivity.OperationName);
        Assert.Null(capturedActivity.GetTagItem(DataEngineTracing.Attributes.ShellId));
    }

    [Fact]
    public void StartSpan_GetPluginMetadataAssets_SetsAssetTag()
    {
        using var fixture = CreateFixture();
        const string assetId = "asset-001";
        using var activity = DataEngineTracing.StartSpan(DataEngineTracing.Spans.GetPluginMetadataAssets, DataEngineTracing.Attributes.ShellId, assetId);

        var capturedActivity = Assert.Single(fixture.Activities);
        Assert.Equal(DataEngineTracing.Spans.GetPluginMetadataAssets, capturedActivity.OperationName);
        Assert.Equal(assetId, capturedActivity.GetTagItem(DataEngineTracing.Attributes.ShellId));
    }

    [Fact]
    public void RecordError_WithException_SetsErrorStatusWithExceptionMessage()
    {
        using var fixture = CreateFixture();
        using var activity = DataEngineTracing.Source.StartActivity("test-error");
        var ex = new ArgumentException("Invalid argument provided");

        activity.RecordError(ex);

        Assert.Equal(ActivityStatusCode.Error, activity.Status);
        Assert.Equal("Invalid argument provided", activity.StatusDescription);
    }

    [Fact]
    public void RecordError_WithDescription_SetsErrorStatusWithDescription()
    {
        using var fixture = CreateFixture();
        using var activity = DataEngineTracing.Source.StartActivity("test-error-desc");
        const string ErrorDescription = "Custom error occurred";

        activity.RecordError(ErrorDescription);

        Assert.Equal(ActivityStatusCode.Error, activity.Status);
        Assert.Equal(ErrorDescription, activity.StatusDescription);
    }
}
