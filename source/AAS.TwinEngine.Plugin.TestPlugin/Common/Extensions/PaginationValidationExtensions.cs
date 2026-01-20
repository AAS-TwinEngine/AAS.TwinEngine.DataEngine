using Aas.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Constants;
using Aas.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Exceptions;

namespace Aas.TwinEngine.Plugin.TestPlugin.Common.Extensions;

public static class PaginationValidationExtensions
{
    public static void ValidateLimit(this int? limit, ILogger? logger = null)
    {
        if (limit is null or > 0)
        {
            return;
        }

        logger?.LogError("Invalid pagination limit provided: {Limit}", limit);
        throw new BadRequestException(ExceptionMessages.InvalidRequestedLimit);
    }
}
