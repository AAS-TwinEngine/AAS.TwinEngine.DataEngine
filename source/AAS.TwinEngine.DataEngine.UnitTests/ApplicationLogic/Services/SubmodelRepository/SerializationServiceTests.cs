using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasRepository;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.Config;

using AasCore.Aas3_0;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

namespace AAS.TwinEngine.DataEngine.UnitTests.ApplicationLogic.Services.SubmodelRepository;

public class SerializationServiceTests
{
    private readonly ISubmodelRepositoryService _submodelRepositoryService;
    private readonly IAasRepositoryService _shellService;
    private readonly IConceptDescriptionService _conceptDescriptionService;
    private readonly SerializationService _sut;

    public SerializationServiceTests()
    {
        _submodelRepositoryService = Substitute.For<ISubmodelRepositoryService>();
        _shellService = Substitute.For<IAasRepositoryService>();
        _conceptDescriptionService = Substitute.For<IConceptDescriptionService>();

        var options = Options.Create(new AasxExportOptions
        {
            RootFolder = "aas"
        });
        var logger = Substitute.For<ILogger<SerializationService>>();
        _sut = new SerializationService(_submodelRepositoryService, _shellService, _conceptDescriptionService, options, logger);
    }

    [Fact]
    public async Task GetAasxFileStreamAsync_ReturnsStream_WhenValidIdsAndConceptDescriptionIsFalse()
    {
        const string AasId = "aas-id";
        const string SubmodelId = "submodel-id";
        const bool IncludeConceptDescriptions = false;
        var shell = CreateShell("validIdShort");
        var submodel = Substitute.For<ISubmodel>();
        _shellService.GetShellByIdAsync(AasId, Arg.Any<CancellationToken>()).Returns(shell);
        _submodelRepositoryService.GetSubmodelAsync(SubmodelId, Arg.Any<CancellationToken>()).Returns(submodel);

        var result = await _sut.GetAasxFileStreamAsync([AasId], [SubmodelId], IncludeConceptDescriptions, CancellationToken.None);

        Assert.NotNull(result);
        Assert.IsType<FileStream>(result);
    }

    [Fact]
    public async Task GetAasxFileStreamAsync_CallsShellService_ForEachAasId()
    {
        var aasIds = new[] { "aas1", "aas2" };
        var submodelIds = new[] { "sub1" };
        const bool IncludeConceptDescriptions = false;
        foreach (var id in aasIds)
        {
            _shellService.GetShellByIdAsync(id, Arg.Any<CancellationToken>())
                         .Returns(Substitute.For<IAssetAdministrationShell>());
        }

        _submodelRepositoryService.GetSubmodelAsync("sub1", Arg.Any<CancellationToken>())
                                  .Returns(Substitute.For<ISubmodel>());

        await _sut.GetAasxFileStreamAsync(aasIds, submodelIds, IncludeConceptDescriptions, CancellationToken.None);

        foreach (var id in aasIds)
        {
            await _shellService.Received(1).GetShellByIdAsync(id, Arg.Any<CancellationToken>());
        }
    }

    [Fact]
    public async Task GetAasxFileStreamAsync_CallsSubmodelRepo_ForEachSubmodelId()
    {
        var aasIds = new[] { "aas1" };
        var submodelIds = new[] { "sub1", "sub2" };
        const bool IncludeConceptDescriptions = false;

        var shell = CreateShell("valididShort");
        _shellService.GetShellByIdAsync("aas1", Arg.Any<CancellationToken>())
                     .Returns(shell);

        foreach (var id in submodelIds)
        {
            _submodelRepositoryService.GetSubmodelAsync(id, Arg.Any<CancellationToken>())
                                      .Returns(Substitute.For<ISubmodel>());
        }

        await _sut.GetAasxFileStreamAsync(aasIds, submodelIds, IncludeConceptDescriptions, CancellationToken.None);

        foreach (var id in submodelIds)
        {
            await _submodelRepositoryService.Received(1).GetSubmodelAsync(id, Arg.Any<CancellationToken>());
        }
    }

    [Fact]
    public async Task GetAasxFileStreamAsync_SkipsMiddleFolder_WhenIdShortHasInvalidChars()
    {
        const string AasId = "aas1";
        const string SubmodelId = "sub1";
        const bool IncludeConceptDescriptions = false;

        var shell = CreateShell("invalid/id:short");
        var submodel = Substitute.For<ISubmodel>();

        _shellService.GetShellByIdAsync(AasId, Arg.Any<CancellationToken>()).Returns(shell);
        _submodelRepositoryService.GetSubmodelAsync(SubmodelId, Arg.Any<CancellationToken>()).Returns(submodel);

        var result = await _sut.GetAasxFileStreamAsync([AasId], [SubmodelId], IncludeConceptDescriptions, CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetAasxFileStreamAsync_ExtractsSemanticIdsFromNestedSubmodelElements_WhenFlagTrue()
    {
        const string AasId = "aas1";
        const string SubmodelId = "sub1";
        const bool IncludeConceptDescriptions = true;
        var shell = CreateShell("validIdShort");
        _shellService.GetShellByIdAsync(AasId, Arg.Any<CancellationToken>()).Returns(shell);
        var submodel = CreateSubmodelWithNestedElements(
            collectionSemanticIds: ["inner-semantic-1"],
            listSemanticIds: ["inner-semantic-2"]);

        _submodelRepositoryService.GetSubmodelAsync(SubmodelId, Arg.Any<CancellationToken>())
                                  .Returns(submodel);
        var cd1 = Substitute.For<IConceptDescription>();
        var cd2 = Substitute.For<IConceptDescription>();
        _conceptDescriptionService.GetConceptDescriptionById("inner-semantic-1", Arg.Any<CancellationToken>()).Returns(cd1);
        _conceptDescriptionService.GetConceptDescriptionById("inner-semantic-2", Arg.Any<CancellationToken>()).Returns(cd2);

        var result = await _sut.GetAasxFileStreamAsync([AasId], [SubmodelId], IncludeConceptDescriptions, CancellationToken.None);

        Assert.NotNull(result);
        Assert.IsType<FileStream>(result);
        await _conceptDescriptionService.Received(1).GetConceptDescriptionById("inner-semantic-1", Arg.Any<CancellationToken>());
        await _conceptDescriptionService.Received(1).GetConceptDescriptionById("inner-semantic-2", Arg.Any<CancellationToken>());
    }

    private static IAssetAdministrationShell CreateShell(string idShort)
    {
        var shell = Substitute.For<IAssetAdministrationShell>();
        shell.IdShort.Returns(idShort);
        return shell;
    }

    private static IKey CreateKey(string value)
    {
        var key = Substitute.For<IKey>();
        key.Value.Returns(value);
        return key;
    }

    private static IReference CreateReference(params string[] semanticIds)
    {
        var keys = semanticIds.Select(CreateKey).Cast<IKey>().ToList();
        var reference = Substitute.For<IReference>();
        reference.Keys.Returns(keys);
        return reference;
    }

    private static ISubmodelElement CreateSubmodelElementWithSemanticId(string semanticId)
    {
        var elementObj = Substitute.For([typeof(ISubmodelElement), typeof(IHasSemantics)], null);
        var element = (ISubmodelElement)elementObj;

        var reference = CreateReference(semanticId);
        element.SemanticId.Returns(reference);

        return element;
    }

    private static ISubmodelElementCollection CreateCollection(params ISubmodelElement[] children)
    {
        var collection = Substitute.For<ISubmodelElementCollection>();
        collection.Value.Returns(children.ToList());
        return collection;
    }

    private static ISubmodelElementList CreateList(params ISubmodelElement[] children)
    {
        var list = Substitute.For<ISubmodelElementList>();
        list.Value.Returns(children.ToList());
        return list;
    }

    private static ISubmodel CreateSubmodelWithNestedElements(IEnumerable<string> collectionSemanticIds, IEnumerable<string> listSemanticIds)
    {
        var innerCollectionElements = collectionSemanticIds.Select(CreateSubmodelElementWithSemanticId).ToArray();
        var innerListElements = listSemanticIds.Select(CreateSubmodelElementWithSemanticId).ToArray();

        var collectionElement = CreateCollection(innerCollectionElements);
        var listElement = CreateList(innerListElements);

        var submodel = Substitute.For<ISubmodel>();
        submodel.SubmodelElements.Returns([collectionElement, listElement]);
        return submodel;
    }

    private ISubmodel CreateSubmodelWithSemanticId(string semanticId)
    {
        var submodel = Substitute.For<ISubmodel>();
        submodel.SemanticId.Returns(CreateReference(semanticId));
        return submodel;
    }
}
