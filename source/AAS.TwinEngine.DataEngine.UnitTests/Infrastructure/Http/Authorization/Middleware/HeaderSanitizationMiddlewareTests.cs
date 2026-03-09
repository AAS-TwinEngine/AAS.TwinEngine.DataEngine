using AAS.TwinEngine.DataEngine.Infrastructure.Http.Authorization.Headers;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Authorization.Middleware;

using Microsoft.AspNetCore.Http;

using NSubstitute;

namespace AAS.TwinEngine.DataEngine.UnitTests.Infrastructure.Http.Authorization;

public class HeaderSanitizationMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_CallsValidateIncomingHeaders_AndNext()
    {
        var mappingService = Substitute.For<IRequestHeaderMapper>();

        var context = new DefaultHttpContext();

        var nextCalled = false;

        Task Next(HttpContext _)
        {
            nextCalled = true;
            return Task.CompletedTask;
        }

        var middleware = new HeaderSanitizationMiddleware(Next);

        await middleware.InvokeAsync(context, mappingService);

        mappingService.Received(1).ValidateIncomingHeaders(context);
        Assert.True(nextCalled);
    }
}
