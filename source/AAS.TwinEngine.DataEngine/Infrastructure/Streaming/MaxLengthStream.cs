namespace AAS.TwinEngine.DataEngine.Infrastructure.Streaming;

public sealed class MaxLengthStream : Stream
{
    private readonly Stream _inner;
    private readonly long _maxBytes;
    private readonly string _idShortPath;
    private long _totalRead;

    public MaxLengthStream(Stream inner, long maxBytes, string idShortPath)
    {
        _inner = inner;
        _maxBytes = maxBytes;
        _idShortPath = idShortPath;
    }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var read = _inner.Read(buffer, offset, count);
        CheckLimit(read);
        return read;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var read = await _inner.ReadAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
        CheckLimit(read);
        return read;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var read = await _inner.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
        CheckLimit(read);
        return read;
    }

    private void CheckLimit(int justRead)
    {
        _totalRead += justRead;
        if (_totalRead > _maxBytes)
        {
            throw new NotImplementedException(
                $"File attachment at '{_idShortPath}' exceeds the maximum allowed size of {_maxBytes} bytes.");
        }
    }

    public override void Flush() => _inner.Flush();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _inner.Dispose();
        }

        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        await _inner.DisposeAsync().ConfigureAwait(false);
        await base.DisposeAsync().ConfigureAwait(false);
    }
}
