using AAS.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Constants;
using AAS.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Exceptions;

namespace AAS.TwinEngine.Plugin.TestPlugin.Common.Extensions;

public static class PaginationValidationExtensions
{
    public const int MaxRequestedLimit = 10_000;

    public static void ValidateLimit(this int? limit, ILogger? logger = null)
    {
        if (limit is null)
        {
            return;
        }

        if (limit > MaxRequestedLimit)
        {
            logger?.LogError("Requested pagination limit exceeds maximum. Provided: {Limit}, Maximum: {Maximum}", limit, MaxRequestedLimit);
            throw new BadRequestException(ExceptionMessages.RequestedLimitExceedsMaximum);
        }

        if (limit > 0)
        {
            return;
        }

        logger?.LogError("Invalid pagination limit provided: {Limit}", limit);
        throw new BadRequestException(ExceptionMessages.InvalidRequestedLimit);
    }
}
