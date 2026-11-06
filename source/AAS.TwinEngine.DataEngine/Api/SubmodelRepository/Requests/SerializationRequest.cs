namespace AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Requests;

public record SerializationRequest(IList<string> AasIdentifier,
                                   IList<string> SubmodelIdentifier,
                                   bool IncludeConceptDescriptions,
                                   string? AcceptHeader);
