using System.Diagnostics;

namespace AAS.TwinEngine.DataEngine.UnitTests.Infrastructure.Observability;

/// <summary>
/// Fixture to properly manage ActivityListener lifecycle for test isolation.
/// Ensures listeners are disposed and activities are cleared between tests.
/// </summary>
public sealed class ActivityListenerFixture : IDisposable
{
    private readonly List<Activity> _activities = [];
    private readonly ActivityListener _listener;

    public ActivityListenerFixture()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == "DataEngine",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = _activities.Add
        };
        ActivitySource.AddActivityListener(_listener);
    }

    public List<Activity> Activities => _activities;

    public void Dispose()
    {
        _activities.Clear();
        _listener?.Dispose();
    }
}
