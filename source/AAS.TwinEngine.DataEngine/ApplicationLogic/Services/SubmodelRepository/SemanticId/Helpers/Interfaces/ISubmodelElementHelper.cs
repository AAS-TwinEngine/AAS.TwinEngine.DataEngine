using AasCore.Aas3_0;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Helpers.Interfaces;

public interface ISubmodelElementHelper
{
    ISubmodelElement CloneElement(ISubmodelElement element);

    ISubmodelElement? GetElementByIdShort(IEnumerable<ISubmodelElement>? submodelElements, string idShort);

    ISubmodelElement GetElementFromListByIndex(IEnumerable<ISubmodelElement>? elements, string idShortWithoutIndex, int index);

    IList<ISubmodelElement>? GetChildElements(ISubmodelElement submodelElement);

    HashSet<string> ResolveLanguages(MultiLanguageProperty mlp);
}
