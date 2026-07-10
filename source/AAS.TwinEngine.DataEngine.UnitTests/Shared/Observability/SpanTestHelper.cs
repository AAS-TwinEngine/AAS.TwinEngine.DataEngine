using System.Diagnostics;

using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

namespace AAS.TwinEngine.DataEngine.UnitTests.Shared.Observability;

/// <summary>
/// Helper class for verifying OpenTelemetry spans in unit tests.
/// Provides reusable functionality for ActivityListener setup and span assertion.
/// </summary>
public class SpanTestHelper : IDisposable
{
    private readonly List<Activity> _activities = [];
    private readonly ActivityListener _listener;

    public SpanTestHelper()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == DataEngineDiagnostics.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = _activities.Add
        };
        ActivitySource.AddActivityListener(_listener);
    }

    /// <summary>
    /// Gets all activities (spans) captured so far.
    /// </summary>
    public IReadOnlyList<Activity> Activities => _activities.AsReadOnly();

    /// <summary>
    /// Asserts that exactly one activity with the specified operation name was captured.
    /// </summary>
    public Activity AssertSingleSpan(string operationName)
    {
        var span = Assert.Single(_activities);
        Assert.Equal(operationName, span.OperationName);
        return span;
    }

    /// <summary>
    /// Asserts that exactly one activity with the specified operation name and tag was captured.
    /// </summary>
    public Activity AssertSingleSpanWithTag(string operationName, string tagKey, object? expectedTagValue)
    {
        var span = AssertSingleSpan(operationName);
        var tagValue = span.GetTagItem(tagKey);
        Assert.Equal(expectedTagValue, tagValue);
        return span;
    }

    /// <summary>
    /// Finds the first activity with the specified operation name.
    /// </summary>
    public Activity? FindSpan(string operationName)
    {
        return _activities.FirstOrDefault(a => a.OperationName == operationName);
    }

    /// <summary>
    /// Finds all activities with the specified operation name.
    /// </summary>
    public IEnumerable<Activity> FindAllSpans(string operationName)
    {
        return _activities.Where(a => a.OperationName == operationName);
    }

    /// <summary>
    /// Asserts that a span with the specified operation name and tag exists.
    /// </summary>
    public Activity AssertSpanWithTag(string operationName, string tagKey, object? expectedTagValue)
    {
        var span = FindSpan(operationName);
        Assert.NotNull(span);
        var tagValue = span!.GetTagItem(tagKey);
        Assert.Equal(expectedTagValue, tagValue);
        return span;
    }

    /// <summary>
    /// Clears all captured activities. Useful for multi-part tests.
    /// </summary>
    public void ClearActivities()
    {
        _activities.Clear();
    }

    /// <summary>
    /// Asserts that the activity has the expected number of tags.
    /// </summary>
    public void AssertActivityTagCount(Activity activity, int expectedCount)
    {
        var tags = activity.Tags.ToList();
        Assert.Equal(expectedCount, tags.Count);
    }

    /// <summary>
    /// Asserts that the activity has a tag with the specified key.
    /// </summary>
    public void AssertActivityHasTag(Activity activity, string tagKey)
    {
        var hasTag = activity.Tags.Any(t => t.Key == tagKey);
        Assert.True(hasTag, $"Activity does not have tag '{tagKey}'");
    }

    public void Dispose()
    {
        _listener?.Dispose();
        _activities.Clear();
    }
}
