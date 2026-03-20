using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

using AasCore.Aas3_0;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Helpers.Interfaces;

public interface ISemanticIdResolver
{
    string MlpPostFixSeparator { get; }

    string InternalSemanticIdType { get; }

    string GetSemanticId(IHasSemantics hasSemantics);

    string ExtractSemanticId(ISubmodelElement element);

    string ResolveSemanticId(IHasSemantics hasSemantics, string idShort);

    string ResolveElementSemanticId(ISubmodelElement element, string idShort);

    Cardinality GetCardinality(ISubmodelElement element);

    DataType GetValueType(ISubmodelElement element);

    string BuildReferenceKeySemanticId(string baseSemanticId, KeyTypes keyType, int index, int totalCount);
}
