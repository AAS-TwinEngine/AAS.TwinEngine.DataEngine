using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;

public static class PaginationValidationExtensions
{
    public const int MaxRequestedLimit = 10_000;
    public const string MaxRequestedLimitExceededMessage = "The requested limit exceeds the maximum allowed value of 10,000.";

    public static void ValidateLimit(this int? limit, ILogger? logger = null)
    {
        if (limit is null)
        {
            return;
        }

        if (limit > MaxRequestedLimit)
        {
            logger?.LogError("Requested pagination limit exceeds maximum. Provided: {Limit}, Maximum: {Maximum}", limit, MaxRequestedLimit);
            throw new InvalidUserInputException(MaxRequestedLimitExceededMessage);
        }

        if (limit > 0)
        {
            return;
        }

        logger?.LogError("Invalid pagination limit provided: {Limit}", limit);
        throw new InvalidUserInputException();
    }

    public static void ValidateCursor(this string cursor, ILogger? logger = null)
    {
        if (!string.IsNullOrWhiteSpace(cursor))
        {
            _ = cursor.DecodeBase64Url(logger);
        }
    }
}
