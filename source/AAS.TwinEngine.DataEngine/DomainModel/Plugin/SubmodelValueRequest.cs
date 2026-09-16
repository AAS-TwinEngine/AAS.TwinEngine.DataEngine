using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

namespace AAS.TwinEngine.DataEngine.DomainModel.Plugin;

public record SubmodelValueRequest(string SubmodelId, SemanticTreeNode SemanticIds);