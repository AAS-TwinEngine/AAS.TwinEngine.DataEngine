using AAS.TwinEngine.Plugin.TestPlugin.Api.Submodel.Services.Legacy;
using AAS.TwinEngine.Plugin.TestPlugin.Api.Submodel.Services.Validation;

using Json.Schema;

namespace AAS.TwinEngine.Plugin.TestPlugin.UnitTests.Api.Submodel.Services.Validation;

public class JsonSchemaDraftSelectorTests
{
    [Fact]
    public void Resolve_WhenNoSchemaKeyword_DefaultsToDraft202012()
    {
        var sut = CreateSelector();

        var result = sut.Resolve(null);

        Assert.Equal(MetaSchemas.Draft202012Id.OriginalString, result.MetaSchemaId);
    }

    [Fact]
    public void Resolve_WhenDraft7SchemaKeyword_ReturnsLegacyDraft7Handler()
    {
        var sut = CreateSelector();

        var result = sut.Resolve(MetaSchemas.Draft7Id.OriginalString);

        Assert.Equal(MetaSchemas.Draft7Id.OriginalString, result.MetaSchemaId);
    }

    [Fact]
    public void GetKnownRefPrefixes_ReturnsDraft7AndDraft202012Prefixes()
    {
        var sut = CreateSelector();

        var result = sut.GetKnownRefPrefixes();

        Assert.Contains("#/definitions/", result);
        Assert.Contains("#/$defs/", result);
    }

    private static JsonSchemaDraftSelector CreateSelector()
    {
        return new JsonSchemaDraftSelector([
            new JsonSchemaDraft202012Handler(),
#pragma warning disable CS0618
            new LegacyDraft7JsonSchemaValidatorHandler()
#pragma warning restore CS0618
        ]);
    }
}
