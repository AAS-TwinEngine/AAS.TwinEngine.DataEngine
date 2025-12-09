using System.Text;
using System.Xml;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasRepository;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.Config;

using AasCore.Aas3.Package;
using AasCore.Aas3_0;

using Microsoft.Extensions.Options;

using Environment = AasCore.Aas3_0.Environment;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository;

public class SerializationService(
    ISubmodelRepositoryService submodelRepositoryService,
    IAasRepositoryService shellService,
    IConceptDescriptionService conceptDescriptionService,
    IOptions<AasxExportOptions> options,
    ILogger<SerializationService> logger) : ISerializationService
{
    private readonly AasxExportOptions _exportOptions = options.Value;

    public async Task<Stream> GetAasxFileStreamAsync(IList<string> aasIds,
                                                     IList<string> submodelIds,
                                                     bool includeConceptDescriptions,
                                                     CancellationToken cancellationToken)
    {
        var shells = await FetchShellsByIdsAsync(aasIds, cancellationToken).ConfigureAwait(false);

        if (shells.Count == 0)
        {
            logger.LogError("Cannot generate AASX package without at least one shell.");
            throw new InternalDataProcessingException();
        }

        var submodels = await FetchSubmodelsByIdsAsync(submodelIds, cancellationToken).ConfigureAwait(false);

        var conceptDescriptions = new List<IConceptDescription>();

        if (includeConceptDescriptions)
        {
            conceptDescriptions = await FetchConceptDescriptionsAsync(submodels, cancellationToken).ConfigureAwait(false);
        }

        return BuildAasxPackageStream(shells, submodels, conceptDescriptions);
    }

    private async Task<List<ISubmodel>> FetchSubmodelsByIdsAsync(IList<string> submodelIds, CancellationToken cancellationToken)
    {
        logger.LogInformation("Fetching submodels by IDs.");

        var submodels = submodelIds
                         .Select(id => submodelRepositoryService.GetSubmodelAsync(id, cancellationToken))
                         .ToList();

        return [.. (await Task.WhenAll(submodels).ConfigureAwait(false))];
    }

    private async Task<List<IAssetAdministrationShell>> FetchShellsByIdsAsync(IList<string> shellIds, CancellationToken cancellationToken)
    {
        logger.LogInformation("Fetching shells by IDs.");

        var shells = shellIds
                         .Select(id => shellService.GetShellByIdAsync(id, cancellationToken))
                         .ToList();

        return [.. (await Task.WhenAll(shells).ConfigureAwait(false))];
    }

    private async Task<List<IConceptDescription>> FetchConceptDescriptionsAsync(List<ISubmodel> submodels, CancellationToken cancellationToken)
    {
        if (submodels.Count == 0)
        {
            logger.LogInformation("No submodels provided; skipping concept description fetch.");
            return [];
        }

        logger.LogInformation("Fetching concept descriptions for submodel element(s).");

        var semanticIds = ExtractSemanticIds(submodels);

        var conceptDescriptions = (await Task.WhenAll(semanticIds
                                                      .Select(id => conceptDescriptionService.GetConceptDescriptionById(id, cancellationToken))
                                                      .ToArray())
                                                      .ConfigureAwait(false))
                                                      .Where(cd => cd != null).Select(cd => cd!)
                                                      .ToList();

        logger.LogInformation("Fetched {Count} concept descriptions", conceptDescriptions.Count);
        return conceptDescriptions;
    }

    private static List<string> ExtractSemanticIds(List<ISubmodel> submodels)
    {
        var semanticIds = new List<string>();
        foreach (var submodel in submodels)
        {
            var submodelSemanticId = GetSemanticId(submodel);

            if (!string.IsNullOrEmpty(submodelSemanticId))
            {
                semanticIds.Add(submodelSemanticId);
            }

            if (submodel.SubmodelElements == null)
            {
                continue;
            }

            submodel.SubmodelElements
                    .ToList()
                    .ForEach(element => ExtractSemanticIdsFromElement(element, semanticIds));
        }

        return semanticIds.Distinct().ToList();
    }

    private static void ExtractSemanticIdsFromElement(ISubmodelElement submodelElement, IList<string> semanticIds)
    {
        var elementSemanticId = GetSemanticId(submodelElement);
        if (!string.IsNullOrEmpty(elementSemanticId))
        {
            semanticIds.Add(elementSemanticId);
        }

        switch (submodelElement)
        {
            case ISubmodelElementCollection collection:
                if (collection.Value is { Count: > 0 })
                {
                    collection.Value
                              .ToList()
                              .ForEach(child => ExtractSemanticIdsFromElement(child, semanticIds));
                }

                break;

            case ISubmodelElementList list:
                if (list.Value is { Count: > 0 })
                {
                    list.Value
                        .ToList()
                        .ForEach(child => ExtractSemanticIdsFromElement(child, semanticIds));
                }

                break;
        }
    }

    private static string GetSemanticId(IHasSemantics hasSemantics) => hasSemantics.SemanticId?.Keys?.FirstOrDefault()?.Value ?? string.Empty;

    private FileStream BuildAasxPackageStream(List<IAssetAdministrationShell> shells, List<ISubmodel> submodels, List<IConceptDescription>? conceptDescriptions)
    {
        var aasEnvironment = new Environment
        {
            AssetAdministrationShells = shells,
            Submodels = submodels,
            ConceptDescriptions = conceptDescriptions
        };

        var relativeFilePathOfXml = BuildRelativeXmlPath(shells.FirstOrDefault()?.IdShort);

        var packageFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.aasx");

        var aasxPackager = new Packaging();

        using (var aasxPackage = aasxPackager.Create(packageFilePath))
        {
            using var xmlMemoryStream = new MemoryStream();

            using (var environmentXmlWriter = XmlWriter.Create(xmlMemoryStream, new XmlWriterSettings { Indent = true, Encoding = Encoding.UTF8 }))
            {
                Xmlization.Serialize.To(aasEnvironment, environmentXmlWriter);
            }

            xmlMemoryStream.Position = 0;

            var specificationPart = aasxPackage.PutPart(
                                                        new Uri(relativeFilePathOfXml, UriKind.Relative),
                                                        "text/xml",
                                                        xmlMemoryStream);

            aasxPackage.MakeSpec(specificationPart);
            aasxPackage.Flush();
        }

        logger.LogInformation("AASX package created at {Path}", packageFilePath);
        return new FileStream(packageFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.DeleteOnClose);
    }

    /// <summary>
    /// Builds a relative XML file path using the provided IdShort.
    /// If the IdShort contains invalid path characters, it will be excluded from the path,
    /// and the file will be placed directly under the root folder.
    /// Otherwise, a subfolder with the IdShort as its name will be included in the path.
    /// </summary>
    private string BuildRelativeXmlPath(string? idShort)
    {
        var validName = GetValidFolderName(idShort ?? string.Empty);
        var includeFolder = validName == idShort;
        return includeFolder
                   ? $"/{_exportOptions.RootFolder}/{validName}/{validName}.xml"
                   : $"/{_exportOptions.RootFolder}/{AasxExportOptions.DefaultXmlFileName}";
    }

    /// <summary>
    /// Remove invalid file-name characters to produce a safe folder name as idShort can contain invalid characters.
    /// </summary>
    private static string GetValidFolderName(string idShort)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return new string([.. idShort.Where(c => !invalidChars.Contains(c))]);
    }
}
