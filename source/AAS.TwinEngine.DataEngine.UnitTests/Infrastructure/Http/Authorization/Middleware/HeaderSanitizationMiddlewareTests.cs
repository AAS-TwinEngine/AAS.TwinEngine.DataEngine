using AAS.TwinEngine.DataEngine.Infrastructure.Http.Authorization.Headers;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Authorization.Middleware;

using Microsoft.AspNetCore.Http;

using NSubstitute;

namespace AAS.TwinEngine.DataEngine.UnitTests.Infrastructure.Http.Authorization;

public class HeaderSanitizationMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WhenNormalRequest_ShouldValidateHeaders_AndCallNext()
    {
        var mapper = Substitute.For<IRequestHeaderMapper>();
        var context = new DefaultHttpContext();

        var nextCalled = false;

        Task Next(HttpContext _)
        {
            nextCalled = true;
            return Task.CompletedTask;
        }

        var middleware = new HeaderSanitizationMiddleware(Next);

        await middleware.InvokeAsync(context, mapper);

        mapper.Received(1).ValidateIncomingHeaders(context);
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_WhenHealthCheckRequest_ShouldSkipValidation_AndCallNext()
    {
        var mapper = Substitute.For<IRequestHeaderMapper>();

        var context = new DefaultHttpContext();
        context.Request.Path = "/healthz";

        var nextCalled = false;

        Task Next(HttpContext _)
        {
            nextCalled = true;
            return Task.CompletedTask;
        }

        var middleware = new HeaderSanitizationMiddleware(Next);

        await middleware.InvokeAsync(context, mapper);

        mapper.DidNotReceive().ValidateIncomingHeaders(Arg.Any<HttpContext>());
        Assert.True(nextCalled);
    }
}
