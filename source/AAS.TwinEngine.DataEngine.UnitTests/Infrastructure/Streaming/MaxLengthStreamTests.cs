using System.Text;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.Infrastructure.Streaming;

namespace AAS.TwinEngine.DataEngine.UnitTests.Infrastructure.Streaming;

public class MaxLengthStreamTests
{
    [Fact]
    public void Read_WhenTotalBytesWithinLimit_ReturnsData()
    {
        var payload = Encoding.UTF8.GetBytes("hello");
        using var inner = new MemoryStream(payload);
        using var sut = new MaxLengthStream(inner, maxBytes: payload.Length);

        var buffer = new byte[payload.Length];
        var read = sut.Read(buffer, 0, buffer.Length);

        Assert.Equal(payload.Length, read);
        Assert.Equal(payload, buffer);
    }

    [Fact]
    public void Read_WhenTotalBytesExceedsLimit_ThrowsFileSizeExceededException()
    {
        var payload = Encoding.UTF8.GetBytes("hello");
        using var inner = new MemoryStream(payload);
        using var sut = new MaxLengthStream(inner, maxBytes: 3);

        var buffer = new byte[payload.Length];

        _ = Assert.Throws<FileSizeExceededException>(() => sut.Read(buffer, 0, buffer.Length));
    }

    [Fact]
    public async Task ReadAsyncMemory_WhenTotalBytesExceedsLimit_ThrowsFileSizeExceededException()
    {
        var payload = Encoding.UTF8.GetBytes("hello");
        await using var inner = new MemoryStream(payload);
        await using var sut = new MaxLengthStream(inner, maxBytes: 3);

        var buffer = new byte[payload.Length];

        _ = await Assert.ThrowsAsync<FileSizeExceededException>(async () =>
            await sut.ReadAsync(buffer.AsMemory(), CancellationToken.None));
    }
}
