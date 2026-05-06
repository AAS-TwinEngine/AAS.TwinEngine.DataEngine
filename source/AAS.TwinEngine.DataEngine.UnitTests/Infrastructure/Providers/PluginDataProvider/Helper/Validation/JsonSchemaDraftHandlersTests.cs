using AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Helper.LegacyV1;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Helper.Validation;

using Json.Schema;

namespace AAS.TwinEngine.DataEngine.UnitTests.Infrastructure.Providers.PluginDataProvider.Helper.Validation;

public class JsonSchemaDraft202012HandlerTests
{
    private readonly JsonSchemaDraft202012Handler _sut = new();

    [Fact]
    public void CanHandle_WhenDeclaredSchemaIsMissing_ReturnsTrue()
    {
        Assert.True(_sut.CanHandle(null));
    }

    [Fact]
    public void CanHandle_WhenDeclaredSchemaIsDraft202012_ReturnsTrue()
    {
        Assert.True(_sut.CanHandle(MetaSchemas.Draft202012Id.OriginalString));
    }

    [Fact]
    public void CanHandle_WhenDeclaredSchemaIsDraft7_ReturnsFalse()
    {
        Assert.False(_sut.CanHandle(MetaSchemas.Draft7Id.OriginalString));
    }
}

#pragma warning disable CS0618
public class LegacyDraft7JsonSchemaValidatorHandlerTests
{
    private readonly LegacyDraft7JsonSchemaValidatorHandler _sut = new();

    [Fact]
    public void CanHandle_WhenDeclaredSchemaIsDraft7_ReturnsTrue()
    {
        Assert.True(_sut.CanHandle(MetaSchemas.Draft7Id.OriginalString));
    }

    [Fact]
    public void CanHandle_WhenDeclaredSchemaIsMissing_ReturnsFalse()
    {
        Assert.False(_sut.CanHandle(null));
    }

    [Fact]
    public void CanHandle_WhenDeclaredSchemaIsDraft202012_ReturnsFalse()
    {
        Assert.False(_sut.CanHandle(MetaSchemas.Draft202012Id.OriginalString));
    }
}
#pragma warning restore CS0618
