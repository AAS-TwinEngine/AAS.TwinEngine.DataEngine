namespace AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

/// <summary>
/// Carries the binary stream and metadata of a File SubmodelElement attachment.
/// The caller is responsible for disposing <see cref="Content"/>.
/// </summary>
public sealed record FileAttachmentResult(Stream Content, string ContentType, string? FileName);
