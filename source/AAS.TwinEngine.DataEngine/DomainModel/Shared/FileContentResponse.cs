namespace AAS.TwinEngine.DataEngine.DomainModel.Shared;

public sealed class FileContentResponse(Stream content, long? contentLength, string? contentType) : IAsyncDisposable
{
    public Stream Content { get; } = content;

    internal Action? OnDispose { get; init; }

    public async ValueTask DisposeAsync()
    {
        await Content.DisposeAsync().ConfigureAwait(false);
        OnDispose?.Invoke();
    }
}
