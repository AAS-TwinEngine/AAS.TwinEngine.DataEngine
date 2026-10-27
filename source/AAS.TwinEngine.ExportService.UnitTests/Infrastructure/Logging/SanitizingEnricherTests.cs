using AAS.TwinEngine.ExportService.Infrastructure.Logging;

using NSubstitute;

using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;

namespace AAS.TwinEngine.ExportService.UnitTests.Infrastructure.Logging;

public class SanitizingEnricherTests
{
    private readonly SanitizingEnricher _sut = new();
    private readonly ILogEventPropertyFactory _propertyFactory = Substitute.For<ILogEventPropertyFactory>();

    private static LogEvent CreateLogEvent(params LogEventProperty[] properties)
    {
        return new LogEvent(
            DateTimeOffset.UtcNow,
            LogEventLevel.Information,
            null,
            new MessageTemplate("Test message", Array.Empty<MessageTemplateToken>()),
            properties);
    }

    [Fact]
    public void Enrich_WhenLogEventIsNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _sut.Enrich(null!, _propertyFactory));
    }

    [Fact]
    public void Enrich_WhenScalarStringPropertyContainsControlChars_SanitizesValue()
    {
        // Arrange
        var prop = new LogEventProperty("User", new ScalarValue("admin\r\nrole:root"));
        var logEvent = CreateLogEvent(prop);

        // Act
        _sut.Enrich(logEvent, _propertyFactory);

        // Assert
        Assert.True(logEvent.Properties.TryGetValue("User", out var value));
        var scalar = Assert.IsType<ScalarValue>(value);
        Assert.Equal("admin\\r\\nrole:root", scalar.Value);
    }

    [Fact]
    public void Enrich_WhenScalarNonStringProperty_LeavesValueUntouched()
    {
        // Arrange
        var prop = new LogEventProperty("Count", new ScalarValue(42));
        var logEvent = CreateLogEvent(prop);

        // Act
        _sut.Enrich(logEvent, _propertyFactory);

        // Assert
        Assert.True(logEvent.Properties.TryGetValue("Count", out var value));
        var scalar = Assert.IsType<ScalarValue>(value);
        Assert.Equal(42, scalar.Value);
    }

    [Fact]
    public void Enrich_WhenSequencePropertyContainsStrings_SanitizesElements()
    {
        // Arrange
        var seq = new SequenceValue(new[]
        {
            new ScalarValue("item1\n"),
            new ScalarValue("item2")
        });
        var prop = new LogEventProperty("Items", seq);
        var logEvent = CreateLogEvent(prop);

        // Act
        _sut.Enrich(logEvent, _propertyFactory);

        // Assert
        Assert.True(logEvent.Properties.TryGetValue("Items", out var value));
        var updatedSeq = Assert.IsType<SequenceValue>(value);
        var elem0 = Assert.IsType<ScalarValue>(updatedSeq.Elements[0]);
        var elem1 = Assert.IsType<ScalarValue>(updatedSeq.Elements[1]);
        Assert.Equal("item1\\n", elem0.Value);
        Assert.Equal("item2", elem1.Value);
    }

    [Fact]
    public void Enrich_WhenStructurePropertyContainsStrings_SanitizesProperties()
    {
        // Arrange
        var structure = new StructureValue(new[]
        {
            new LogEventProperty("Field1", new ScalarValue("val\t1")),
            new LogEventProperty("Field2", new ScalarValue("clean"))
        });
        var prop = new LogEventProperty("Complex", structure);
        var logEvent = CreateLogEvent(prop);

        // Act
        _sut.Enrich(logEvent, _propertyFactory);

        // Assert
        Assert.True(logEvent.Properties.TryGetValue("Complex", out var value));
        var updatedStruct = Assert.IsType<StructureValue>(value);
        var field1 = updatedStruct.Properties.Single(p => p.Name == "Field1");
        var scalar1 = Assert.IsType<ScalarValue>(field1.Value);
        Assert.Equal("val\\t1", scalar1.Value);
    }

    [Fact]
    public void Enrich_WhenDictionaryPropertyContainsStrings_SanitizesKeysAndValues()
    {
        // Arrange
        var dict = new DictionaryValue(new[]
        {
            new KeyValuePair<ScalarValue, LogEventPropertyValue>(
                new ScalarValue("key\n"),
                new ScalarValue("val\r"))
        });
        var prop = new LogEventProperty("Dict", dict);
        var logEvent = CreateLogEvent(prop);

        // Act
        _sut.Enrich(logEvent, _propertyFactory);

        // Assert
        Assert.True(logEvent.Properties.TryGetValue("Dict", out var value));
        var updatedDict = Assert.IsType<DictionaryValue>(value);
        var pair = updatedDict.Elements.Single();
        Assert.Equal("key\\n", pair.Key.Value);
        var valScalar = Assert.IsType<ScalarValue>(pair.Value);
        Assert.Equal("val\\r", valScalar.Value);
    }

    [Fact]
    public void Enrich_WhenPropertiesAlreadyClean_DoesNotModifyLogEvent()
    {
        // Arrange
        var prop = new LogEventProperty("Safe", new ScalarValue("clean-string"));
        var logEvent = CreateLogEvent(prop);

        // Act
        _sut.Enrich(logEvent, _propertyFactory);

        // Assert
        Assert.True(logEvent.Properties.TryGetValue("Safe", out var value));
        Assert.Same(prop.Value, value);
    }
}
