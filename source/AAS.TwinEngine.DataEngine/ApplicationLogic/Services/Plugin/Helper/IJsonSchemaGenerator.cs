using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

using Json.Schema;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Helper;

public interface IJsonSchemaGenerator
{
    JsonSchema Generate(SemanticTreeNode rootNode);
}
