using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Shared.Authorization;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

namespace AAS.TwinEngine.DataEngine.UnitTests.ApplicationLogic.Services.Shared.Authorization;

public class HeaderSanitizationMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_CallsValidateIncomingHeaders_AndNext()
    {
        var services = new ServiceCollection();
        var mappingService = Substitute.For<IHeaderMappingService>();
        _ = services.AddSingleton(mappingService);
        var serviceProvider = services.BuildServiceProvider();

        var context = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };

        var nextCalled = false;
        Task Next(HttpContext _) { nextCalled = true; return Task.CompletedTask; }

        var middleware = new HeaderSanitizationMiddleware(Next);

        await middleware.InvokeAsync(context).ConfigureAwait(false);

        mappingService
            .Received(1)
            .ValidateIncomingHeaders(context);

        Assert.True(nextCalled);
    }
}
