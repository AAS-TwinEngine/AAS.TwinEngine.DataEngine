namespace AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Requests;

public record SerializeAasxRequest(
    IList<string> AasIdentifier,
    IList<string> SubmodelIdentifier,
    bool IncludeConceptDescriptions);

