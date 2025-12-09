using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;

public static class PaginationValidationExtensions
{
    public static void ValidateLimit(this int? limit, ILogger? logger = null)
    {
        if (limit is null or > 0)
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
