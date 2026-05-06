namespace AAS.TwinEngine.Plugin.TestPlugin.Api.Submodel.Services.Validation;

public class JsonSchemaDraftSelector(IEnumerable<IJsonSchemaDraftHandler> handlers)
{
    private readonly IReadOnlyList<IJsonSchemaDraftHandler> _handlers = handlers?.ToList()
        ?? throw new ArgumentNullException(nameof(handlers));

    public IJsonSchemaDraftHandler Resolve(string? declaredSchema)
    {
        if (_handlers.Count == 0)
        {
            throw new InvalidOperationException("No JSON Schema draft handlers are registered.");
        }

        return _handlers.FirstOrDefault(handler => handler.CanHandle(declaredSchema)) ?? _handlers[0];
    }

    public IReadOnlyCollection<string> GetKnownRefPrefixes()
    {
        return _handlers
            .SelectMany(handler => handler.SupportedRefPrefixes)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
