using AAS.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Constants;
using AAS.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Exceptions;
using AAS.TwinEngine.Plugin.TestPlugin.Common.Extensions;

using Microsoft.Extensions.Logging;

using NSubstitute;

namespace AAS.TwinEngine.Plugin.TestPlugin.UnitTests.Common.Extensions;

public class PaginationValidationExtensionsTests
{
    private readonly ILogger _logger = Substitute.For<ILogger>();

    [Fact]
    public void ValidateLimit_WhenLimitExceedsMaximum_ThrowsBadRequestExceptionWithExpectedMessage()
    {
        var exception = Assert.Throws<BadRequestException>(() => ((int?)10_001).ValidateLimit(_logger));

        Assert.Equal(ExceptionMessages.RequestedLimitExceedsMaximum, exception.Message);
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
