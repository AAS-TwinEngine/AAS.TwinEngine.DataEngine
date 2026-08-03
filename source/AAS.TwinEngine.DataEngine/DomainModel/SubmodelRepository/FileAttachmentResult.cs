namespace AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

public sealed record FileAttachmentResult(Stream Content, string ContentType, string? FileName)
{
    public IReadOnlyList<IDisposable> ResponseDisposables { get; init; } = [];
}
