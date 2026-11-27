namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository;

public interface ISerializationService
{
    Task<Stream> GetSerializationStreamAsync(IList<string> aasIds,
                                             IList<string> submodelIds,
                                             bool includeConceptDescriptions,
                                             SerializationFormat format,
                                             CancellationToken cancellationToken);
}
