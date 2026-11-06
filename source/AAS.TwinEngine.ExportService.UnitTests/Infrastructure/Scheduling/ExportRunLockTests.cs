using AAS.TwinEngine.ExportService.Infrastructure.Scheduling;

namespace AAS.TwinEngine.ExportService.UnitTests.Infrastructure.Scheduling;

public class ExportRunLockTests
{
    private readonly ExportRunLock _sut = new();

    [Fact]
    public void TryAcquire_WhenNotBusy_ReturnsDisposableReleaser()
    {
        // Act
        using var releaser = _sut.TryAcquire();

        // Assert
        Assert.NotNull(releaser);
    }

    [Fact]
    public void TryAcquire_WhenAlreadyAcquired_ReturnsNull()
    {
        // Arrange
        using var releaser1 = _sut.TryAcquire();

        // Act
        using var releaser2 = _sut.TryAcquire();

        // Assert
        Assert.NotNull(releaser1);
        Assert.Null(releaser2);
    }

    [Fact]
    public void Dispose_ReleasesLock_AllowingSubsequentAcquisition()
    {
        // Arrange
        var releaser1 = _sut.TryAcquire();
        Assert.NotNull(releaser1);

        // Act
        releaser1.Dispose();
        var releaser2 = _sut.TryAcquire();

        // Assert
        Assert.NotNull(releaser2);
        releaser2.Dispose();
    }

    [Fact]
    public void Dispose_WhenCalledMultipleTimes_DoesNotCauseExtraReleases()
    {
        // Arrange
        var releaser1 = _sut.TryAcquire();
        Assert.NotNull(releaser1);

        // Act
        releaser1.Dispose();
        releaser1.Dispose(); // second call should be no-op

        // Assert
        var releaser2 = _sut.TryAcquire();
        Assert.NotNull(releaser2);

        var releaser3 = _sut.TryAcquire();
        Assert.Null(releaser3);

        releaser2.Dispose();
    }
}
