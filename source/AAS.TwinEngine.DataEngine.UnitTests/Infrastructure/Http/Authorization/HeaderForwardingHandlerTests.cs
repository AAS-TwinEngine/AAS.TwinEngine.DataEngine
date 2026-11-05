using System.Net;

using AAS.TwinEngine.DataEngine.Infrastructure.Http.Authorization;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Authorization.Config;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Authorization.Headers;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

using NSubstitute;

namespace AAS.TwinEngine.DataEngine.UnitTests.Infrastructure.Http.Authorization;

public class HeaderForwardingHandlerTests
{
    private static HeaderForwardingHandler CreateHandler(HttpContext? httpContext, string clientName, TemplateManagementConfig? templateManagementConfig = null)
    {
        var accessor = new HttpContextAccessor { HttpContext = httpContext };

        var generalConfig = Options.Create(new GeneralConfig
        {
            HeaderSanitization = new HeaderSanitizationOptions()
        });

        var pluginsConfig = Options.Create(new PluginsConfig
        {
            Instances =
            [
                new ServiceInstance
                {
                    Name = "TestPlugin",
                    BaseUrl = new Uri("http://example.com"),
                    HeaderMappings =
                    [
                        new HeaderMappingRule
                        {
                            Source = "Authorization",
                            Target = "X-Auth-Token",
                            Required = true
                        }
                    ]
                }
            ]
        });

        var templateManagementOptions = Options.Create(templateManagementConfig ?? new TemplateManagementConfig());

        var headerMapper = new RequestHeaderMapper(
            new NullLogger<RequestHeaderMapper>(),
            generalConfig,
            pluginsConfig,
            templateManagementOptions);

        return new HeaderForwardingHandler(accessor, headerMapper, clientName)
        {
            InnerHandler = new TestHandler()
        };
    }

    [Fact]
    public async Task HeaderForwardingHandler_ForwardsMappedHeaderToInnerHandler()
    {
        const string pluginName = "TestPlugin";
        var clientName = HttpClientNames.PluginDataProviderPrefix + pluginName;

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Authorization = "Bearer test-token";

        using var handler = CreateHandler(httpContext, clientName);
        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://example.com")
        };

        using var response = await client.GetAsync("/test").ConfigureAwait(false);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var innerHandler = (TestHandler)handler.InnerHandler!;
        Assert.NotNull(innerHandler.LastRequest);
        Assert.True(innerHandler.LastRequest!.Headers.TryGetValues("X-Auth-Token", out var values));
        Assert.Contains("Bearer test-token", values);
    }

    [Fact]
    public async Task HeaderForwardingHandler_UsesCapturedHeadersWhenHttpContextIsUnavailable()
    {
        var templateManagementConfig = new TemplateManagementConfig
        {
            AasTemplateRepository = new ServiceInstance
            {
                HeaderMappings =
                [
                    new HeaderMappingRule
                    {
                        Source = "Authorization",
                        Target = "Authorization"
                    }
                ]
            }
        };

        using var handler = CreateHandler(null, HttpClientNames.AasTemplateRepository, templateManagementConfig);
        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://example.com")
        };
        using var request = new HttpRequestMessage(HttpMethod.Get, "/test");
        request.Options.Set(
            RequestHeaderForwardingOptions.IncomingHeaders,
            new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase)
            {
                ["Authorization"] = "Bearer captured-token"
            });

        using var response = await client.SendAsync(request).ConfigureAwait(false);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var innerHandler = (TestHandler)handler.InnerHandler!;
        Assert.Equal("Bearer", innerHandler.LastRequest!.Headers.Authorization?.Scheme);
        Assert.Equal("captured-token", innerHandler.LastRequest.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task HeaderForwardingHandler_CallsMappingServiceWithClientName()
    {
        const string clientName = "test-client";
        var httpContext = new DefaultHttpContext();

        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var mappingService = Substitute.For<IRequestHeaderMapper>();

        using var handler = new HeaderForwardingHandler(accessor, mappingService, clientName)
        {
            InnerHandler = new TestHandler()
        };

        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://example.com")
        };

        _ = await client.GetAsync("/test").ConfigureAwait(false);

        mappingService
            .Received(1)
            .ApplyMappings(httpContext, Arg.Any<HttpRequestMessage>(), clientName);
    }

    private sealed class TestHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            return Task.FromResult(response);
        }
    }
}
