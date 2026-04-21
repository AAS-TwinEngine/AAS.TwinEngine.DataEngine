using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.Infrastructure.Logging;

using Serilog.Events;
using Serilog.Parsing;

namespace AAS.TwinEngine.DataEngine.UnitTests.Infrastructure.Logging;

public class SanitizingEnricherTests
{
    private readonly SanitizingEnricher _sut = new();
    private static readonly MessageTemplateParser Parser = new();

    private static LogEvent CreateLogEvent(params LogEventProperty[] properties)
    {
        return new LogEvent(
            DateTimeOffset.UtcNow,
            LogEventLevel.Information,
            null,
            Parser.Parse("Test {Prop}"),
            properties);
    }

    [Fact]
    public void Enrich_SanitizesStringWithNewlines()
    {
        var prop = new LogEventProperty("Prop", new ScalarValue("line1\nline2"));
        var logEvent = CreateLogEvent(prop);

        _sut.Enrich(logEvent, null!);

        var result = (ScalarValue)logEvent.Properties["Prop"];
        Assert.Equal("line1\\nline2", result.Value);
    }

    [Fact]
    public void Enrich_SanitizesCarriageReturn()
    {
        var prop = new LogEventProperty("Prop", new ScalarValue("value\rwith\rCR"));
        var logEvent = CreateLogEvent(prop);

        _sut.Enrich(logEvent, null!);

        var result = (ScalarValue)logEvent.Properties["Prop"];
        Assert.Equal("value\\rwith\\rCR", result.Value);
    }

    [Fact]
    public void Enrich_SanitizesTabAndNullCharacters()
    {
        var prop = new LogEventProperty("Prop", new ScalarValue("before\t\0after"));
        var logEvent = CreateLogEvent(prop);

        _sut.Enrich(logEvent, null!);

        var result = (ScalarValue)logEvent.Properties["Prop"];
        Assert.Equal("before\\t\\0after", result.Value);
    }

    [Fact]
    public void Enrich_SanitizesAnsiEscapeSequence()
    {
        var prop = new LogEventProperty("Prop", new ScalarValue("prefix\x1B[31mred\x1B[0m"));
        var logEvent = CreateLogEvent(prop);

        _sut.Enrich(logEvent, null!);

        var result = (ScalarValue)logEvent.Properties["Prop"];
        Assert.Equal("prefix\\x1B[31mred\\x1B[0m", (string?)result.Value);
    }

    [Fact]
    public void Enrich_LeavesCleanStringsUnchanged()
    {
        var prop = new LogEventProperty("Prop", new ScalarValue("clean-value-123"));
        var logEvent = CreateLogEvent(prop);

        _sut.Enrich(logEvent, null!);

        var result = (ScalarValue)logEvent.Properties["Prop"];
        Assert.Equal("clean-value-123", result.Value);
    }

    [Fact]
    public void Enrich_LeavesNonStringScalarsUnchanged()
    {
        var prop = new LogEventProperty("Prop", new ScalarValue(42));
        var logEvent = CreateLogEvent(prop);

        _sut.Enrich(logEvent, null!);

        var result = (ScalarValue)logEvent.Properties["Prop"];
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Enrich_LeavesNullScalarUnchanged()
    {
        var prop = new LogEventProperty("Prop", new ScalarValue(null));
        var logEvent = CreateLogEvent(prop);

        _sut.Enrich(logEvent, null!);

        var result = (ScalarValue)logEvent.Properties["Prop"];
        Assert.Null(result.Value);
    }

    [Fact]
    public void Enrich_SanitizesStringsInSequence()
    {
        var seq = new SequenceValue([new ScalarValue("a\nb"), new ScalarValue("clean")]);
        var prop = new LogEventProperty("Prop", seq);
        var logEvent = CreateLogEvent(prop);

        _sut.Enrich(logEvent, null!);

        var result = (SequenceValue)logEvent.Properties["Prop"];
        Assert.Equal("a\\nb", ((ScalarValue)result.Elements[0]).Value);
        Assert.Equal("clean", ((ScalarValue)result.Elements[1]).Value);
    }

    [Fact]
    public void Enrich_SanitizesStringsInStructure()
    {
        var structure = new StructureValue(
        [
            new LogEventProperty("Name", new ScalarValue("test\ninjection")),
            new LogEventProperty("Count", new ScalarValue(5))
        ]);
        var prop = new LogEventProperty("Prop", structure);
        var logEvent = CreateLogEvent(prop);

        _sut.Enrich(logEvent, null!);

        var result = (StructureValue)logEvent.Properties["Prop"];
        var nameValue = (ScalarValue)result.Properties.First(p => p.Name == "Name").Value;
        var countValue = (ScalarValue)result.Properties.First(p => p.Name == "Count").Value;
        Assert.Equal("test\\ninjection", nameValue.Value);
        Assert.Equal(5, countValue.Value);
    }

    [Fact]
    public void Enrich_SanitizesStringsInDictionary()
    {
        var dict = new DictionaryValue(
        [
            new KeyValuePair<ScalarValue, LogEventPropertyValue>(
                new ScalarValue("key\n1"), new ScalarValue("val\nue"))
        ]);
        var prop = new LogEventProperty("Prop", dict);
        var logEvent = CreateLogEvent(prop);

        _sut.Enrich(logEvent, null!);

        var result = (DictionaryValue)logEvent.Properties["Prop"];
        var entry = result.Elements.First();
        Assert.Equal("key\\n1", entry.Key.Value);
        Assert.Equal("val\\nue", ((ScalarValue)entry.Value).Value);
    }

    [Fact]
    public void Enrich_MultipleProperties_SanitizesAll()
    {
        var logEvent = CreateLogEvent(
            new LogEventProperty("Clean", new ScalarValue("ok")),
            new LogEventProperty("Dirty", new ScalarValue("bad\nvalue")),
            new LogEventProperty("Number", new ScalarValue(99)));

        _sut.Enrich(logEvent, null!);

        Assert.Equal("ok", ((ScalarValue)logEvent.Properties["Clean"]).Value);
        Assert.Equal("bad\\nvalue", ((ScalarValue)logEvent.Properties["Dirty"]).Value);
        Assert.Equal(99, ((ScalarValue)logEvent.Properties["Number"]).Value);
    }

    [Fact]
    public void Enrich_ThrowsOnNullLogEvent() => Assert.Throws<InvalidDependencyException>(() => _sut.Enrich(null!, null!));
}
