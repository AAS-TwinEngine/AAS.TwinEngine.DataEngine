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

    #region Span Name Constants

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

    #endregion

    #region Attribute Name Constants

    [Fact]
    public void AttributeNames_AreCorrect()
    {
        Assert.Equal("aas.submodel_id", DataEngineTracing.Attributes.SubmodelId);
        Assert.Equal("aas.template_id", DataEngineTracing.Attributes.TemplateId);
        Assert.Equal("aas.shell_id", DataEngineTracing.Attributes.ShellId);
    }

    #endregion

    #region Template Span Tests

    [Fact]
    public void StartGetShellTemplate_CreatesActivityWithTemplateTag()
    {
        using var fixture = CreateFixture();
        const string templateId = "template-shell-001";
        using var activity = DataEngineTracing.StartGetShellTemplate(templateId);

        var capturedActivity = Assert.Single(fixture.Activities);
        Assert.Equal(DataEngineTracing.Spans.GetShellTemplate, capturedActivity.OperationName);
        Assert.Equal(templateId, capturedActivity.GetTagItem(DataEngineTracing.Attributes.TemplateId));
    }

    [Fact]
    public void StartGetSubmodelTemplate_CreatesActivityWithTemplateTag()
    {
        using var fixture = CreateFixture();
        const string templateId = "template-submodel-001";
        using var activity = DataEngineTracing.StartGetSubmodelTemplate(templateId);

        var capturedActivity = Assert.Single(fixture.Activities);
        Assert.Equal(DataEngineTracing.Spans.GetSubmodelTemplate, capturedActivity.OperationName);
        Assert.Equal(templateId, capturedActivity.GetTagItem(DataEngineTracing.Attributes.TemplateId));
    }

    [Fact]
    public void StartGetShellDescriptorTemplate_CreatesActivityWithTemplateTag()
    {
        using var fixture = CreateFixture();
        const string templateId = "template-shell-descriptor-001";
        using var activity = DataEngineTracing.StartGetShellDescriptorTemplate(templateId);

        var capturedActivity = Assert.Single(fixture.Activities);
        Assert.Equal(DataEngineTracing.Spans.GetShellDescriptorTemplate, capturedActivity.OperationName);
        Assert.Equal(templateId, capturedActivity.GetTagItem(DataEngineTracing.Attributes.TemplateId));
    }

    [Fact]
    public void StartGetSubmodelDescriptorTemplate_CreatesActivityWithTemplateTag()
    {
        using var fixture = CreateFixture();
        const string templateId = "template-submodel-descriptor-001";
        using var activity = DataEngineTracing.StartGetSubmodelDescriptorTemplate(templateId);

        var capturedActivity = Assert.Single(fixture.Activities);
        Assert.Equal(DataEngineTracing.Spans.GetSubmodelDescriptorTemplate, capturedActivity.OperationName);
        Assert.Equal(templateId, capturedActivity.GetTagItem(DataEngineTracing.Attributes.TemplateId));
    }

    [Fact]
    public void StartGetSubmodelRefTemplate_CreatesActivityWithTemplateTag()
    {
        using var fixture = CreateFixture();
        const string templateId = "template-submodel-ref-001";
        using var activity = DataEngineTracing.StartGetSubmodelRefTemplate(templateId);

        var capturedActivity = Assert.Single(fixture.Activities);
        Assert.Equal(DataEngineTracing.Spans.GetSubmodelRefTemplate, capturedActivity.OperationName);
        Assert.Equal(templateId, capturedActivity.GetTagItem(DataEngineTracing.Attributes.TemplateId));
    }

    [Fact]
    public void StartGetConceptDescription_CreatesActivityWithTemplateTag()
    {
        using var fixture = CreateFixture();
        const string conceptDescriptionId = "cd-001";
        using var activity = DataEngineTracing.StartGetConceptDescription(conceptDescriptionId);

        var capturedActivity = Assert.Single(fixture.Activities);
        Assert.Equal(DataEngineTracing.Spans.GetConceptDescription, capturedActivity.OperationName);
        Assert.Equal(conceptDescriptionId, capturedActivity.GetTagItem(DataEngineTracing.Attributes.TemplateId));
    }

    #endregion

    #region StartGetProductId Tests

    [Fact]
    public void StartGetProductId_CreatesActivityWithCorrectName()
    {
        using var fixture = CreateFixture();
        using var activity = DataEngineTracing.StartGetProductId("shell-prod");

        var capturedActivity = Assert.Single(fixture.Activities);
        Assert.Equal(DataEngineTracing.Spans.GetProductId, capturedActivity.OperationName);
    }

    [Fact]
    public void StartGetProductId_SetsShellIdTag()
    {
        using var fixture = CreateFixture();
        const string ShellId = "shell-prod-456";
        using var activity = DataEngineTracing.StartGetProductId(ShellId);

        var capturedActivity = Assert.Single(fixture.Activities);
        Assert.Equal(ShellId, capturedActivity.GetTagItem(DataEngineTracing.Attributes.ShellId));
    }

    #endregion

    #region Plugin Span Tests

    [Fact]
    public void StartPluginRequestGeneration_CreatesActivityWithCorrectName()
    {
        using var fixture = CreateFixture();
        using var activity = DataEngineTracing.StartPluginRequestGeneration();

        var capturedActivity = Assert.Single(fixture.Activities);
        Assert.Equal(DataEngineTracing.Spans.PluginRequestGeneration, capturedActivity.OperationName);
    }

    [Fact]
    public void StartGetPluginData_CreatesActivityWithSubmodelTag()
    {
        using var fixture = CreateFixture();
        const string submodelId = "submodel-plugin-001";
        using var activity = DataEngineTracing.StartGetPluginData(submodelId);

        var capturedActivity = Assert.Single(fixture.Activities);
        Assert.Equal(DataEngineTracing.Spans.GetPluginData, capturedActivity.OperationName);
        Assert.Equal(submodelId, capturedActivity.GetTagItem(DataEngineTracing.Attributes.SubmodelId));
    }

    [Fact]
    public void StartGetPluginMetadataShells_WithShellId_SetsTag()
    {
        using var fixture = CreateFixture();
        const string shellId = "shell-123";
        using var activity = DataEngineTracing.StartGetPluginMetadataShells(shellId);

        var capturedActivity = Assert.Single(fixture.Activities);
        Assert.Equal(DataEngineTracing.Spans.GetPluginMetadataShells, capturedActivity.OperationName);
        Assert.Equal(shellId, capturedActivity.GetTagItem(DataEngineTracing.Attributes.ShellId));
    }

    [Fact]
    public void StartGetPluginMetadataShells_WithoutArgument_CreatesActivity()
    {
        using var fixture = CreateFixture();
        using var activity = DataEngineTracing.StartGetPluginMetadataShells();

        var capturedActivity = Assert.Single(fixture.Activities);
        Assert.Equal(DataEngineTracing.Spans.GetPluginMetadataShells, capturedActivity.OperationName);
        Assert.Null(capturedActivity.GetTagItem(DataEngineTracing.Attributes.ShellId));
    }

    [Fact]
    public void StartGetPluginMetadataAssets_SetsAssetTag()
    {
        using var fixture = CreateFixture();
        const string assetId = "asset-001";
        using var activity = DataEngineTracing.StartGetPluginMetadataAssets(assetId);

        var capturedActivity = Assert.Single(fixture.Activities);
        Assert.Equal(DataEngineTracing.Spans.GetPluginMetadataAssets, capturedActivity.OperationName);
        Assert.Equal(assetId, capturedActivity.GetTagItem(DataEngineTracing.Attributes.ShellId));
    }

    #endregion

    #region RecordError Extension Method Tests

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

    #endregion
}
