namespace AAS.TwinEngine.ExportService.DomainModel;

/// <summary>
/// One entity as read from the source system. <see cref="RawJson"/> is the payload that
/// will be forwarded to the target (verbatim).
/// </summary>
public sealed record SourceEntity(string Identifier, string RawJson);
