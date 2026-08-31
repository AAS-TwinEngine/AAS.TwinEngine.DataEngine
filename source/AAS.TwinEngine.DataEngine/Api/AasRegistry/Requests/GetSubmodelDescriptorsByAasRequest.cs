namespace AAS.TwinEngine.DataEngine.Api.AasRegistry.Requests;

public record GetSubmodelDescriptorsByAasRequest(string AasIdentifier, int Limit, string? Cursor);
