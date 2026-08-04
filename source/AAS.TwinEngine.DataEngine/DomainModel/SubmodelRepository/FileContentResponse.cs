namespace AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

public sealed class FileContentResponse(Stream content, long? contentLength, string? contentType) : IAsyncDisposable
{
    public Stream Content { get; } = content;
    public long? ContentLength { get; } = contentLength;
    public string? ContentType { get; } = contentType;

    internal Action? OnDispose { get; init; }

    public async ValueTask DisposeAsync()
    {
        await Content.DisposeAsync().ConfigureAwait(false);
        OnDispose?.Invoke();
    }
}
