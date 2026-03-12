using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Helpers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Helpers.Interfaces;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

using AasCore.Aas3_0;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.ElementHandlers;

public class MultiLanguagePropertyHandler(
    ISemanticIdResolver semanticIdResolver,
    ISubmodelElementHelper elementHelper,
    ILogger<MultiLanguagePropertyHandler> logger) : ISubmodelElementTypeHandler
{
    public bool CanHandle(ISubmodelElement element) => element is MultiLanguageProperty;

    public SemanticTreeNode? Extract(ISubmodelElement element, Func<ISubmodelElement, SemanticTreeNode?> extractChild)
    {
        var mlp = (MultiLanguageProperty)element;
        var semanticId = semanticIdResolver.ExtractSemanticId(mlp);
        var node = new SemanticBranchNode(semanticId, semanticIdResolver.GetCardinality(mlp));

        var languages = elementHelper.ResolveLanguages(mlp);

        if (mlp.Value is not { Count: > 0 })
        {
            logger.LogInformation("No languages defined in template for MultiLanguageProperty {MlpIdShort}", mlp.IdShort);
        }

        var mlpSeparator = semanticIdResolver.MlpPostFixSeparator;
        foreach (var langSemanticId in languages.Select(language => string.Concat(semanticId, mlpSeparator, language)))
        {
            node.AddChild(new SemanticLeafNode(langSemanticId, string.Empty, DataType.String, Cardinality.ZeroToOne));
        }

        return node;
    }

    public void FillOut(ISubmodelElement element, SemanticTreeNode values, Action<List<ISubmodelElement>, SemanticTreeNode, bool> fillOutChildren)
    {
        var mlp = (MultiLanguageProperty)element;
        var semanticId = semanticIdResolver.ExtractSemanticId(mlp);

        if (SemanticTreeNavigator.FindNodeBySemanticId(values, semanticId).FirstOrDefault() is not SemanticBranchNode valueNode)
        {
            logger.LogInformation("No value node found for MultiLanguageProperty {MlpIdShort}", mlp.IdShort);
            return;
        }

        mlp.Value ??= [];

        var languageValueMap = new Dictionary<string, LangStringTextType>(StringComparer.OrdinalIgnoreCase);
        foreach (var langValue in mlp.Value)
        {
            languageValueMap[langValue.Language] = (LangStringTextType)langValue;
        }

        var languages = elementHelper.ResolveLanguages(mlp);

        var mlpSeparator = semanticIdResolver.MlpPostFixSeparator;
        foreach (var language in languages)
        {
            if (!languageValueMap.TryGetValue(language, out var languageValue))
            {
                languageValue = new LangStringTextType(language, string.Empty);
                mlp.Value.Add(languageValue);
                languageValueMap[language] = languageValue;

                logger.LogInformation("Added language '{Language}' to MultiLanguageProperty {MlpIdShort}", language, mlp.IdShort);
            }

            var languageSemanticId = semanticId + mlpSeparator + language;

            var leafNode = valueNode.Children
                                    .OfType<SemanticLeafNode>()
                                    .FirstOrDefault(child => child.SemanticId.Equals(languageSemanticId, StringComparison.Ordinal));

            if (leafNode != null)
            {
                languageValue.Text = leafNode.Value;
            }
        }
    }
}
