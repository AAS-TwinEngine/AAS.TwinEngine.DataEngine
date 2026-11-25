using AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Requests;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;

namespace AAS.TwinEngine.DataEngine.Api.Shared.ContentModifiers;

/// <summary>
/// Enforces the IDTA-01002 modifier constraints and resolves the level/extent used to load the data
/// that is afterwards serialized with the requested <see cref="Content"/>.
/// </summary>
public static class ContentModifierValidator
{
    public static void Validate(Content content, Level? level, Extent? extent, ILogger logger)
    {
        switch (content)
        {
            case Content.Metadata when level is not null:
                logger.LogWarning("Level parameter is not allowed for Content=Metadata.");
                throw new InvalidUserInputException("The parameter 'level' shall not be used with Content=Metadata.");
            case Content.Metadata when extent == Extent.withBlobValue:
                logger.LogWarning("Extent=withBlobValue is not allowed for Content=Metadata.");
                throw new InvalidUserInputException("The combination of Content=Metadata and Extent=withBlobValue is not allowed.");
            case Content.Reference when level == Level.deep:
                logger.LogWarning("Level=deep is not allowed for Content=Reference.");
                throw new InvalidUserInputException("The combination of Content=Reference and Level=deep is not allowed.");
        }
    }

    // Metadata and Reference never expose child values, so the data is always loaded with the default level.
    public static Level ResolveLevel(Content content, Level? level)
        => content is Content.Value or Content.Path ? level ?? Level.deep : Level.deep;

    public static Extent ResolveExtent(Content content, Extent? extent)
        => content == Content.Value ? extent ?? Extent.withoutBlobValue : Extent.withoutBlobValue;
}
