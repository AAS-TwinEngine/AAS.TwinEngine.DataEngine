using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Base;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Config;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Authorization.Config;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Authorization.Headers;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Config;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.DataEngine.UnitTests.Infrastructure.Http.Authorization.Headers;

public class RequestHeaderMapperTests
{
    private static RequestHeaderMapper CreateService(HeaderForwardingOptions options)
    {
        var logger = new NullLogger<RequestHeaderMapper>();
        var opts = Options.Create(options);
        return new RequestHeaderMapper(logger, opts);
    }

    [Fact]
    public void ValidateIncomingHeaders_RequiredHeaderMissing_ThrowsInvalidRequestHeaderException()
    {
        var options = new HeaderForwardingOptions
        {
            HeaderSanitization = new HeaderSanitizationOptions(),
            HeaderMappings = new HeaderMappings
            {
                TemplateRepository =
                [
                    new HeaderMappingRule { Source = "Authorization", Target = "Authorization", Required = true }
                ]
            }
        };

        var service = CreateService(options);
        var context = new DefaultHttpContext();

        Assert.Throws<InvalidRequestHeaderException>(() => service.ValidateIncomingHeaders(context));
    }

    [Fact]
    public void ApplyMappings_OptionalHeaderMissing_DoesNotThrow()
    {
        var options = new HeaderForwardingOptions
        {
            HeaderSanitization = new HeaderSanitizationOptions(),
            HeaderMappings = new HeaderMappings
            {
                TemplateRepository =
                [
                    new HeaderMappingRule { Source = "X-Optional", Target = "X-Optional", Required = false }
                ]
            }
        };

        var service = CreateService(options);
        var context = new DefaultHttpContext();
        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, "http://example.com");

        service.ApplyMappings(context, requestMessage, AasEnvironmentConfig.AasEnvironmentRepoHttpClientName);

        Assert.False(requestMessage.Headers.Contains("X-Optional"));
    }

    [Fact]
    public void ApplyMappings_MapsAuthorizationHeader()
    {
        var options = new HeaderForwardingOptions
        {
            HeaderSanitization = new HeaderSanitizationOptions(),
            HeaderMappings = new HeaderMappings
            {
                TemplateRepository =
                [
                    new HeaderMappingRule { Source = "Authorization", Target = "Authorization", Required = true }
                ]
            }
        };

        var service = CreateService(options);
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "Bearer token";

        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, "http://example.com");

        service.ApplyMappings(context, requestMessage, AasEnvironmentConfig.AasEnvironmentRepoHttpClientName);

        Assert.Equal("Bearer", requestMessage.Headers.Authorization?.Scheme);
        Assert.Equal("token", requestMessage.Headers.Authorization?.Parameter);
    }

    [Fact]
    public void ApplyMappings_PluginSpecificMapping_RenamesHeader()
    {
        const string PluginName = "MyPlugin";
        var clientName = PluginConfig.HttpClientNamePrefix + PluginName;

        var options = new HeaderForwardingOptions
        {
            HeaderSanitization = new HeaderSanitizationOptions(),
            HeaderMappings = new HeaderMappings
            {
                Plugins =
                {
                    [PluginName] =
                    [
                        new HeaderMappingRule { Source = "X-Source", Target = "X-Target", Required = true }
                    ]
                }
            }
        };

        var service = CreateService(options);
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Source"] = "value";

        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, "http://example.com");

        service.ApplyMappings(context, requestMessage, clientName);

        Assert.True(requestMessage.Headers.TryGetValues("X-Target", out var values));
        Assert.Contains("value", values);
    }

    [Fact]
    public void ApplyMappings_MissingOptionalHeader_SkipsHeader()
    {
        var options = new HeaderForwardingOptions
        {
            HeaderSanitization = new HeaderSanitizationOptions(),
            HeaderMappings = new HeaderMappings
            {
                TemplateRepository =
                [
                    new HeaderMappingRule { Source = "X-Test", Target = "X-Test", Required = false }
                ]
            }
        };

        var service = CreateService(options);
        var context = new DefaultHttpContext();

        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, "http://example.com");

        service.ApplyMappings(context, requestMessage, AasEnvironmentConfig.AasEnvironmentRepoHttpClientName);

        Assert.False(requestMessage.Headers.Contains("X-Test"));
    }

    [Fact]
    public void ValidateIncomingHeaders_InvalidMappedHeader_ThrowsBadRequest()
    {
        var options = new HeaderForwardingOptions
        {
            HeaderSanitization = new HeaderSanitizationOptions
            {
                BlockedPatterns = ["<script"]
            },
            HeaderMappings = new HeaderMappings
            {
                TemplateRepository =
                [
                    new HeaderMappingRule { Source = "X-Test", Target = "X-Test", Required = false }
                ]
            }
        };

        var service = CreateService(options);
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Test"] = "ok<script";

        Assert.Throws<InvalidRequestHeaderException>(() => service.ValidateIncomingHeaders(context));
    }

    [Fact]
    public void ValidateIncomingHeaders_AllHeadersValid_DoesNotThrow()
    {
        var options = new HeaderForwardingOptions
        {
            HeaderSanitization = new HeaderSanitizationOptions(),
            HeaderMappings = new HeaderMappings()
        };

        var service = CreateService(options);
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Valid"] = "simple-value";
        context.Request.Headers.Authorization = "Bearer token";

        service.ValidateIncomingHeaders(context);
    }

    [Fact]
    public void ApplyMappings_TemplateRegistryClient_UsesTemplateRegistryMappings()
    {
        var options = new HeaderForwardingOptions
        {
            HeaderSanitization = new HeaderSanitizationOptions(),
            HeaderMappings = new HeaderMappings
            {
                TemplateRegistry =
                [
                    new HeaderMappingRule { Source = "X-Source", Target = "X-Registry", Required = true }
                ]
            }
        };

        var service = CreateService(options);
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Source"] = "value";

        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, "http://example.com");

        service.ApplyMappings(context, requestMessage, AasEnvironmentConfig.AasRegistryHttpClientName);

        Assert.True(requestMessage.Headers.TryGetValues("X-Registry", out var values));
        Assert.Contains("value", values);
    }
}
