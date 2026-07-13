using System.Diagnostics;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Observability;

namespace AAS.TwinEngine.DataEngine.UnitTests.Infrastructure.Observability;

public class DataEngineDiagnosticsTests
{
    [Fact]
    public void SourceName_ReturnsExpectedValue() => Assert.Equal("DataEngine", DataEngineDiagnostics.SourceName);

    [Fact]
    public void Source_IsNotNull() => Assert.NotNull(DataEngineDiagnostics.Source);

    [Fact]
    public void Source_HasCorrectName() => Assert.Equal(DataEngineDiagnostics.SourceName, DataEngineDiagnostics.Source.Name);

    private ActivityListenerFixture CreateFixture() => new();

    #region Span Name Constants

    [Fact]
    public void SpanNames_AreCorrect()
    {
        Assert.Equal("ResolveTemplateId", DataEngineDiagnostics.Spans.ResolveTemplateId);
        Assert.Equal("FetchTemplate", DataEngineDiagnostics.Spans.FetchTemplate);
        Assert.Equal("ResolveTemplate", DataEngineDiagnostics.Spans.ResolveTemplate);
        Assert.Equal("GenerateSubmodelIds", DataEngineDiagnostics.Spans.GenerateSubmodelIds);
        Assert.Equal("GetProductId", DataEngineDiagnostics.Spans.GetProductId);
        Assert.Equal("ExtractSemanticIds", DataEngineDiagnostics.Spans.ExtractSemanticIds);
        Assert.Equal("PluginResolution", DataEngineDiagnostics.Spans.PluginResolution);
        Assert.Equal("RequestGeneration", DataEngineDiagnostics.Spans.RequestGeneration);
        Assert.Equal("FetchPluginData", DataEngineDiagnostics.Spans.FetchPluginData);
        Assert.Equal("FillDataIntoTemplate", DataEngineDiagnostics.Spans.FillDataIntoTemplate);
        Assert.Equal("FetchPluginMetadata", DataEngineDiagnostics.Spans.FetchPluginMetadata);
        Assert.Equal("FetchAssetInformation", DataEngineDiagnostics.Spans.FetchAssetInformation);
    }

    #endregion

    #region Attribute Name Constants

    [Fact]
    public void AttributeNames_AreCorrect()
    {
        Assert.Equal("aas.submodel_id", DataEngineDiagnostics.Attributes.SubmodelId);
        Assert.Equal("aas.template_id", DataEngineDiagnostics.Attributes.TemplateId);
        Assert.Equal("aas.shell_id", DataEngineDiagnostics.Attributes.ShellId);
    }

    #endregion

    #region StartResolveSubmodelTemplateId Tests

    [Fact]
    public void StartResolveSubmodelTemplateId_CreatesActivityWithCorrectName()
    {
        using var fixture = CreateFixture();
        using var activity = DataEngineDiagnostics.StartResolveSubmodelTemplateId("test-submodel");

        var capturedActivity = Assert.Single(fixture.Activities);
        Assert.Equal(DataEngineDiagnostics.Spans.ResolveTemplateId, capturedActivity.OperationName);
    }

    [Fact]
    public void StartResolveSubmodelTemplateId_SetsSubmodelIdTag()
    {
        using var fixture = CreateFixture();
        const string SubmodelId = "test-submodel-123";
        using var activity = DataEngineDiagnostics.StartResolveSubmodelTemplateId(SubmodelId);

        var capturedActivity = Assert.Single(fixture.Activities);
        Assert.Equal(SubmodelId, capturedActivity.GetTagItem(DataEngineDiagnostics.Attributes.SubmodelId));
    }

    #endregion

    #region StartResolveShellTemplateId Tests

    [Fact]
    public void StartResolveShellTemplateId_CreatesActivityWithCorrectName()
    {
        using var fixture = CreateFixture();
        using var activity = DataEngineDiagnostics.StartResolveShellTemplateId("test-shell");

        var capturedActivity = Assert.Single(fixture.Activities);
        Assert.Equal(DataEngineDiagnostics.Spans.ResolveTemplateId, capturedActivity.OperationName);
    }

    [Fact]
    public void StartResolveShellTemplateId_SetsShellIdTag()
    {
        using var fixture = CreateFixture();
        const string ShellId = "test-shell-123";
        using var activity = DataEngineDiagnostics.StartResolveShellTemplateId(ShellId);

        var capturedActivity = Assert.Single(fixture.Activities);
        Assert.Equal(ShellId, capturedActivity.GetTagItem(DataEngineDiagnostics.Attributes.ShellId));
    }

    #endregion

    #region StartFetchTemplate Tests

    [Fact]
    public void StartFetchTemplate_CreatesActivityWithCorrectName()
    {
        using var fixture = CreateFixture();
        using var activity = DataEngineDiagnostics.StartFetchTemplate("template-001");

        var capturedActivity = Assert.Single(fixture.Activities);
        Assert.Equal(DataEngineDiagnostics.Spans.FetchTemplate, capturedActivity.OperationName);
    }

    [Fact]
    public void StartFetchTemplate_SetsTemplateIdTag()
    {
        using var fixture = CreateFixture();
        const string TemplateId = "template-abc";
        using var activity = DataEngineDiagnostics.StartFetchTemplate(TemplateId);

        var capturedActivity = Assert.Single(fixture.Activities);
        Assert.Equal(TemplateId, capturedActivity.GetTagItem(DataEngineDiagnostics.Attributes.TemplateId));
    }

    #endregion

    #region StartResolveTemplate Tests

    [Fact]
    public void StartResolveTemplate_CreatesActivityWithCorrectName()
    {
        using var fixture = CreateFixture();
        using var activity = DataEngineDiagnostics.StartResolveTemplate("submodel-xyz");

        var capturedActivity = Assert.Single(fixture.Activities);
        Assert.Equal(DataEngineDiagnostics.Spans.ResolveTemplate, capturedActivity.OperationName);
    }

    [Fact]
    public void StartResolveTemplate_SetsSubmodelIdTag()
    {
        using var fixture = CreateFixture();
        const string SubmodelId = "submodel-xyz";
        using var activity = DataEngineDiagnostics.StartResolveTemplate(SubmodelId);

        var capturedActivity = Assert.Single(fixture.Activities);
        Assert.Equal(SubmodelId, capturedActivity.GetTagItem(DataEngineDiagnostics.Attributes.SubmodelId));
    }

    #endregion

    #region StartGenerateSubmodelIds Tests

    [Fact]
    public void StartGenerateSubmodelIds_CreatesActivityWithCorrectName()
    {
        using var fixture = CreateFixture();
        using var activity = DataEngineDiagnostics.StartGenerateSubmodelIds("shell-id");

        var capturedActivity = Assert.Single(fixture.Activities);
        Assert.Equal(DataEngineDiagnostics.Spans.GenerateSubmodelIds, capturedActivity.OperationName);
    }

    [Fact]
    public void StartGenerateSubmodelIds_SetsShellIdTag()
    {
        using var fixture = CreateFixture();
        const string ShellId = "shell-gen-123";
        using var activity = DataEngineDiagnostics.StartGenerateSubmodelIds(ShellId);

        var capturedActivity = Assert.Single(fixture.Activities);
        Assert.Equal(ShellId, capturedActivity.GetTagItem(DataEngineDiagnostics.Attributes.ShellId));
    }

    #endregion

    #region StartGetProductId Tests

    [Fact]
    public void StartGetProductId_CreatesActivityWithCorrectName()
    {
        using var fixture = CreateFixture();
        using var activity = DataEngineDiagnostics.StartGetProductId("shell-prod");

        var capturedActivity = Assert.Single(fixture.Activities);
        Assert.Equal(DataEngineDiagnostics.Spans.GetProductId, capturedActivity.OperationName);
    }

    [Fact]
    public void StartGetProductId_SetsShellIdTag()
    {
        using var fixture = CreateFixture();
        const string ShellId = "shell-prod-456";
        using var activity = DataEngineDiagnostics.StartGetProductId(ShellId);

        var capturedActivity = Assert.Single(fixture.Activities);
        Assert.Equal(ShellId, capturedActivity.GetTagItem(DataEngineDiagnostics.Attributes.ShellId));
    }

    #endregion

    #region StartExtractSemanticIds Tests

    [Fact]
    public void StartExtractSemanticIds_CreatesActivityWithCorrectName()
    {
        using var fixture = CreateFixture();
        using var activity = DataEngineDiagnostics.StartExtractSemanticIds();

        var capturedActivity = Assert.Single(fixture.Activities);
        Assert.Equal(DataEngineDiagnostics.Spans.ExtractSemanticIds, capturedActivity.OperationName);
    }

    #endregion

    #region StartFillDataIntoTemplate Tests

    [Fact]
    public void StartFillDataIntoTemplate_CreatesActivityWithCorrectName()
    {
        using var fixture = CreateFixture();
        using var activity = DataEngineDiagnostics.StartFillDataIntoTemplate("template-fill");

        var capturedActivity = Assert.Single(fixture.Activities);
        Assert.Equal(DataEngineDiagnostics.Spans.FillDataIntoTemplate, capturedActivity.OperationName);
    }

    [Fact]
    public void StartFillDataIntoTemplate_SetsTemplateIdTag()
    {
        using var fixture = CreateFixture();
        const string TemplateId = "template-fill-789";
        using var activity = DataEngineDiagnostics.StartFillDataIntoTemplate(TemplateId);

        var capturedActivity = Assert.Single(fixture.Activities);
        Assert.Equal(TemplateId, capturedActivity.GetTagItem(DataEngineDiagnostics.Attributes.TemplateId));
    }

    #endregion

    #region StartFillShellDataIntoTemplate Tests

    [Fact]
    public void StartFillShellDataIntoTemplate_CreatesActivityWithCorrectName()
    {
        using var fixture = CreateFixture();
        using var activity = DataEngineDiagnostics.StartFillShellDataIntoTemplate("shell-123", "template-456");

        var capturedActivity = Assert.Single(fixture.Activities);
        Assert.Equal(DataEngineDiagnostics.Spans.FillDataIntoTemplate, capturedActivity.OperationName);
    }

    [Fact]
    public void StartFillShellDataIntoTemplate_SetsBothShellIdAndTemplateIdTags()
    {
        using var fixture = CreateFixture();
        const string ShellId = "shell-fill-001";
        const string TemplateId = "template-fill-001";
        using var activity = DataEngineDiagnostics.StartFillShellDataIntoTemplate(ShellId, TemplateId);

        var capturedActivity = Assert.Single(fixture.Activities);
        Assert.Equal(ShellId, capturedActivity.GetTagItem(DataEngineDiagnostics.Attributes.ShellId));
        Assert.Equal(TemplateId, capturedActivity.GetTagItem(DataEngineDiagnostics.Attributes.TemplateId));
    }

    #endregion

    #region StartFillAssetInformationIntoTemplate Tests

    [Fact]
    public void StartFillAssetInformationIntoTemplate_CreatesActivityWithCorrectName()
    {
        using var fixture = CreateFixture();
        using var activity = DataEngineDiagnostics.StartFillAssetInformationIntoTemplate("shell-asset");

        var capturedActivity = Assert.Single(fixture.Activities);
        Assert.Equal(DataEngineDiagnostics.Spans.FillDataIntoTemplate, capturedActivity.OperationName);
    }

    [Fact]
    public void StartFillAssetInformationIntoTemplate_SetsShellIdTag()
    {
        using var fixture = CreateFixture();
        const string ShellId = "shell-asset-999";
        using var activity = DataEngineDiagnostics.StartFillAssetInformationIntoTemplate(ShellId);

        var capturedActivity = Assert.Single(fixture.Activities);
        Assert.Equal(ShellId, capturedActivity.GetTagItem(DataEngineDiagnostics.Attributes.ShellId));
    }

    #endregion

    #region StartPluginResolution Tests

    [Fact]
    public void StartPluginResolution_CreatesActivityWithCorrectName()
    {
        using var fixture = CreateFixture();
        using var activity = DataEngineDiagnostics.StartPluginResolution();

        var capturedActivity = Assert.Single(fixture.Activities);
        Assert.Equal(DataEngineDiagnostics.Spans.PluginResolution, capturedActivity.OperationName);
    }

    #endregion

    #region StartRequestGeneration Tests

    [Fact]
    public void StartRequestGeneration_CreatesActivityWithCorrectName()
    {
        using var fixture = CreateFixture();
        using var activity = DataEngineDiagnostics.StartRequestGeneration();

        var capturedActivity = Assert.Single(fixture.Activities);
        Assert.Equal(DataEngineDiagnostics.Spans.RequestGeneration, capturedActivity.OperationName);
    }

    #endregion

    #region StartFetchPluginData Tests

    [Fact]
    public void StartFetchPluginData_CreatesActivityWithCorrectName()
    {
        using var fixture = CreateFixture();
        using var activity = DataEngineDiagnostics.StartFetchPluginData("submodel-plugin");

        var capturedActivity = Assert.Single(fixture.Activities);
        Assert.Equal(DataEngineDiagnostics.Spans.FetchPluginData, capturedActivity.OperationName);
    }

    [Fact]
    public void StartFetchPluginData_SetsSubmodelIdTag()
    {
        using var fixture = CreateFixture();
        const string SubmodelId = "submodel-plugin-555";
        using var activity = DataEngineDiagnostics.StartFetchPluginData(SubmodelId);

        var capturedActivity = Assert.Single(fixture.Activities);
        Assert.Equal(SubmodelId, capturedActivity.GetTagItem(DataEngineDiagnostics.Attributes.SubmodelId));
    }

    #endregion

    #region StartFetchPluginMetadata Tests

    [Fact]
    public void StartFetchPluginMetadata_CreatesActivityWithCorrectName()
    {
        using var fixture = CreateFixture();
        using var activity = DataEngineDiagnostics.StartFetchPluginMetadata();

        var capturedActivity = Assert.Single(fixture.Activities);
        Assert.Equal(DataEngineDiagnostics.Spans.FetchPluginMetadata, capturedActivity.OperationName);
    }

    #endregion

    #region StartFetchShellDescriptorMetadata Tests

    [Fact]
    public void StartFetchShellDescriptorMetadata_CreatesActivityWithCorrectName()
    {
        using var fixture = CreateFixture();
        using var activity = DataEngineDiagnostics.StartFetchShellDescriptorMetadata("shell-meta");

        var capturedActivity = Assert.Single(fixture.Activities);
        Assert.Equal(DataEngineDiagnostics.Spans.FetchPluginMetadata, capturedActivity.OperationName);
    }

    [Fact]
    public void StartFetchShellDescriptorMetadata_SetsShellIdTag()
    {
        using var fixture = CreateFixture();
        const string ShellId = "shell-meta-111";
        using var activity = DataEngineDiagnostics.StartFetchShellDescriptorMetadata(ShellId);

        var capturedActivity = Assert.Single(fixture.Activities);
        Assert.Equal(ShellId, capturedActivity.GetTagItem(DataEngineDiagnostics.Attributes.ShellId));
    }

    #endregion

    #region StartFetchSubmodelDescriptorMetadata Tests

    [Fact]
    public void StartFetchSubmodelDescriptorMetadata_CreatesActivityWithCorrectName()
    {
        using var fixture = CreateFixture();
        using var activity = DataEngineDiagnostics.StartFetchSubmodelDescriptorMetadata("submodel-desc");

        var capturedActivity = Assert.Single(fixture.Activities);
        Assert.Equal(DataEngineDiagnostics.Spans.FetchPluginMetadata, capturedActivity.OperationName);
    }

    [Fact]
    public void StartFetchSubmodelDescriptorMetadata_SetsSubmodelIdTag()
    {
        using var fixture = CreateFixture();
        const string SubmodelId = "submodel-desc-222";
        using var activity = DataEngineDiagnostics.StartFetchSubmodelDescriptorMetadata(SubmodelId);

        var capturedActivity = Assert.Single(fixture.Activities);
        Assert.Equal(SubmodelId, capturedActivity.GetTagItem(DataEngineDiagnostics.Attributes.SubmodelId));
    }

    #endregion

    #region StartFetchAssetInformation Tests

    [Fact]
    public void StartFetchAssetInformation_CreatesActivityWithCorrectName()
    {
        using var fixture = CreateFixture();
        using var activity = DataEngineDiagnostics.StartFetchAssetInformation("shell-asset-info");

        var capturedActivity = Assert.Single(fixture.Activities);
        Assert.Equal(DataEngineDiagnostics.Spans.FetchAssetInformation, capturedActivity.OperationName);
    }

    [Fact]
    public void StartFetchAssetInformation_SetsShellIdTag()
    {
        using var fixture = CreateFixture();
        const string ShellId = "shell-asset-info-777";
        using var activity = DataEngineDiagnostics.StartFetchAssetInformation(ShellId);

        var capturedActivity = Assert.Single(fixture.Activities);
        Assert.Equal(ShellId, capturedActivity.GetTagItem(DataEngineDiagnostics.Attributes.ShellId));
    }

    #endregion

    #region RecordError Extension Method Tests

    [Fact]
    public void RecordError_WithException_SetsErrorStatusWithExceptionMessage()
    {
        using var fixture = CreateFixture();
        using var activity = DataEngineDiagnostics.Source.StartActivity("test-error");
        var ex = new ArgumentException("Invalid argument provided");

        activity.RecordError(ex);

        Assert.Equal(ActivityStatusCode.Error, activity.Status);
        Assert.Equal("Invalid argument provided", activity.StatusDescription);
    }

    [Fact]
    public void RecordError_WithException_WhenActivityIsNull_DoesNotThrow()
    {
        Activity? activity = null;
        var ex = new InvalidOperationException("Operation failed");

        var result = Record.NoException(() => activity.RecordError(ex));
        Assert.Null(result);
    }

    [Fact]
    public void RecordError_WithDescription_SetsErrorStatusWithDescription()
    {
        using var fixture = CreateFixture();
        using var activity = DataEngineDiagnostics.Source.StartActivity("test-error-desc");
        const string ErrorDescription = "Custom error occurred";

        activity.RecordError(ErrorDescription);

        Assert.Equal(ActivityStatusCode.Error, activity.Status);
        Assert.Equal(ErrorDescription, activity.StatusDescription);
    }

    [Fact]
    public void RecordError_WithDescription_WhenActivityIsNull_DoesNotThrow()
    {
        Activity? activity = null;
        const string ErrorDescription = "Custom error occurred";

        var result = Record.NoException(() => activity.RecordError(ErrorDescription));
        Assert.Null(result);
    }

    #endregion
}
