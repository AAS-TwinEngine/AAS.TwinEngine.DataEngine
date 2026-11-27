using AAS.TwinEngine.DataEngine.Api.Shared.ContentModifiers;
using AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Requests;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;

using Microsoft.Extensions.Logging;

using NSubstitute;

namespace AAS.TwinEngine.DataEngine.UnitTests.Api.Shared.ContentModifiers;

public class ContentModifierValidatorTests
{
    private readonly ILogger _logger = Substitute.For<ILogger>();

    [Theory]
    [InlineData(Content.Metadata, Level.deep, null)]
    [InlineData(Content.Metadata, Level.core, null)]
    [InlineData(Content.Metadata, null, Extent.withBlobValue)]
    [InlineData(Content.Reference, Level.deep, null)]
    public void Validate_WithForbiddenCombination_ThrowsInvalidUserInputException(Content content, Level? level, Extent? extent)
        => Assert.Throws<InvalidUserInputException>(() => ContentModifierValidator.Validate(content, level, extent, _logger));

    [Theory]
    [InlineData(Content.Metadata, null, null)]
    [InlineData(Content.Metadata, null, Extent.withoutBlobValue)]
    [InlineData(Content.Reference, null, null)]
    [InlineData(Content.Reference, Level.core, null)]
    [InlineData(Content.Value, Level.core, Extent.withBlobValue)]
    [InlineData(Content.Path, Level.deep, null)]
    public void Validate_WithAllowedCombination_DoesNotThrow(Content content, Level? level, Extent? extent)
        => ContentModifierValidator.Validate(content, level, extent, _logger);

    [Theory]
    [InlineData(Content.Value, Level.core, Level.core)]
    [InlineData(Content.Path, null, Level.deep)]
    [InlineData(Content.Reference, Level.core, Level.deep)]
    [InlineData(Content.Metadata, null, Level.deep)]
    public void ResolveLevel_ReturnsLevelUsedToLoadData(Content content, Level? requested, Level expected)
        => Assert.Equal(expected, ContentModifierValidator.ResolveLevel(content, requested));
}
