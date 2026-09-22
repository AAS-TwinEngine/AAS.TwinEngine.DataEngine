namespace AAS.TwinEngine.DataEngine.DomainModel.Shared;

public sealed class FileAttachmentResult(Stream content, string contentType, string? fileName, long maxAllowedBytes) : IAsyncDisposable
{
    public Stream Content { get; } = content;
    public string ContentType { get; } = contentType;
    public string? FileName { get; } = fileName;
    public long MaxAllowedBytes { get; } = maxAllowedBytes;

    internal IAsyncDisposable? Upstream { get; init; }

    public async ValueTask DisposeAsync()
    {
        await Content.DisposeAsync().ConfigureAwait(false);
        if (Upstream is not null)
        {
            await Upstream.DisposeAsync().ConfigureAwait(false);
        }
    }
}
