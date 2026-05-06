using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Helper.Validation;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Helper.Validation;

public sealed class JsonSchemaDraftSelector
{
    private readonly IReadOnlyList<IJsonSchemaDraftHandler> _draftHandlers;

    public JsonSchemaDraftSelector(IEnumerable<IJsonSchemaDraftHandler> draftHandlers)
    {
        _draftHandlers = draftHandlers.ToList();

        if (_draftHandlers.Count == 0)
        {
            throw new InvalidOperationException("At least one JSON Schema draft handler must be registered.");
        }
    }

    public IJsonSchemaDraftHandler Resolve(string? declaredSchema)
        => _draftHandlers.FirstOrDefault(handler => handler.CanHandle(declaredSchema))
           ?? _draftHandlers[0];

    public IReadOnlyList<string> GetKnownRefPrefixes()
        => _draftHandlers
            .SelectMany(handler => handler.SupportedRefPrefixes)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
}
