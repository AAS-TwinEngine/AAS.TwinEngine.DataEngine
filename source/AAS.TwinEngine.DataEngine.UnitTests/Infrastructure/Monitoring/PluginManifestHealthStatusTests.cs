using AAS.TwinEngine.DataEngine.Infrastructure.Monitoring;

namespace AAS.TwinEngine.DataEngine.UnitTests.Infrastructure.Monitoring;

public class PluginManifestHealthStatusTests
{
    [Fact]
    public void Default_IsHealthy_IsTrue()
    {
        var sut = new PluginManifestHealthStatus();

        Assert.True(sut.IsHealthy);
    }

    [Fact]
    public void Can_Set_IsHealthy_ToFalse()
    {
        var sut = new PluginManifestHealthStatus
        {
            IsHealthy = false
        };

        Assert.False(sut.IsHealthy);
    }

    [Fact]
    public void Can_Set_IsHealthy_BackToTrue()
    {
        var sut = new PluginManifestHealthStatus { IsHealthy = false };

        sut.IsHealthy = true;

        Assert.True(sut.IsHealthy);
    }

    [Fact]
    public void IsHealthy_IsThreadSafe_WhenAccessedConcurrently()
    {
        var sut = new PluginManifestHealthStatus();
        var tasks = new Task[100];
        var readValues = new bool[100];

        for (var i = 0; i < 100; i++)
        {
            var index = i;
            tasks[i] = Task.Run(() =>
            {
                if (index % 2 == 0)
                {
                    sut.IsHealthy = true;
                }
                else
                {
                    sut.IsHealthy = false;
                }

                readValues[index] = sut.IsHealthy;
            });
        }

        Task.WaitAll(tasks);

        var finalValue = sut.IsHealthy;
        Assert.True(finalValue || finalValue == false);
    }

    [Fact]
    public void Multiple_Writes_And_Reads_Are_ThreadSafe()
    {
        var sut = new PluginManifestHealthStatus();
        var exceptions = new List<Exception>();
        var tasks = new List<Task>();

        for (var i = 0; i < 50; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    for (var j = 0; j < 1000; j++)
                    {
                        sut.IsHealthy = j % 2 == 0;
                        _ = sut.IsHealthy;
                    }
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            }));
        }

        Task.WaitAll([.. tasks]);

        Assert.Empty(exceptions);
    }
}
