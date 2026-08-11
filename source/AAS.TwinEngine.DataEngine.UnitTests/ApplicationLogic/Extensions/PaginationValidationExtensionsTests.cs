using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;

using Microsoft.Extensions.Logging;

using NSubstitute;

namespace AAS.TwinEngine.DataEngine.UnitTests.ApplicationLogic.Extensions;

public class PaginationValidationExtensionsTests
{
    private readonly ILogger _logger = Substitute.For<ILogger>();

    [Fact]
    public void ValidateLimit_WhenLimitExceedsMaximum_ThrowsInvalidUserInputExceptionWithExpectedMessage()
    {
        var exception = Assert.Throws<InvalidUserInputException>(() => ((int?)10_001).ValidateLimit(_logger));

        Assert.Equal(PaginationValidationExtensions.MaxRequestedLimitExceededMessage, exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(1)]
    [InlineData(10_000)]
    public void ValidateLimit_WhenLimitWithinAllowedRange_DoesNotThrow(int? limit)
    {
        limit.ValidateLimit(_logger);
    }
}
