using System.Diagnostics;

namespace AAS.TwinEngine.DataEngine.UnitTests.ApplicationLogic.Observability;

/// <summary>
/// Fixture to properly manage ActivityListener lifecycle for test isolation.
/// Ensures listeners are disposed and activities are cleared between tests.
/// </summary>
public sealed class ActivityListenerFixture : IDisposable
{
    private readonly ActivityListener _listener;

    public ActivityListenerFixture()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == "DataEngine",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = Activities.Add
        };
        ActivitySource.AddActivityListener(_listener);
    }

    public List<Activity> Activities { get; } = [];

    public void Dispose()
    {
        Activities.Clear();
        _listener?.Dispose();
    }
}
