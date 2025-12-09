namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository;

public interface ISerializationService
{
    Task<Stream> GetAasxFileStreamAsync(IList<string> aasIds, IList<string> submodelIds, bool includeConceptDescriptions, CancellationToken cancellationToken);
}
