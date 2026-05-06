using AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Helper.LegacyV1;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Helper.Validation;

namespace AAS.TwinEngine.DataEngine.UnitTests.Infrastructure.Providers.PluginDataProvider.Helper.Validation;

public class JsonSchemaDraftSelectorTests
{
    [Fact]
    public void Constructor_WhenNoHandlersRegistered_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() => new JsonSchemaDraftSelector([]));
    }

    [Fact]
    public void Resolve_WhenSchemaIsMissing_DefaultsToDraft202012()
    {
#pragma warning disable CS0618
        var sut = new JsonSchemaDraftSelector([
            new JsonSchemaDraft202012Handler(),
            new LegacyDraft7JsonSchemaValidatorHandler()
        ]);
#pragma warning restore CS0618

        var resolved = sut.Resolve(null);

        Assert.IsType<JsonSchemaDraft202012Handler>(resolved);
    }

    [Fact]
    public void Resolve_WhenSchemaIsDraft7_ResolvesLegacyDraft7Handler()
    {
#pragma warning disable CS0618
        var sut = new JsonSchemaDraftSelector([
            new JsonSchemaDraft202012Handler(),
            new LegacyDraft7JsonSchemaValidatorHandler()
        ]);
#pragma warning restore CS0618

        var resolved = sut.Resolve("http://json-schema.org/draft-07/schema#");

#pragma warning disable CS0618
        Assert.IsType<LegacyDraft7JsonSchemaValidatorHandler>(resolved);
#pragma warning restore CS0618
    }

    [Fact]
    public void GetKnownRefPrefixes_ReturnsBothDraftPrefixes()
    {
#pragma warning disable CS0618
        var sut = new JsonSchemaDraftSelector([
            new JsonSchemaDraft202012Handler(),
            new LegacyDraft7JsonSchemaValidatorHandler()
        ]);
#pragma warning restore CS0618

        var prefixes = sut.GetKnownRefPrefixes();

        Assert.Contains("#/$defs/", prefixes);
        Assert.Contains("#/definitions/", prefixes);
    }
}
