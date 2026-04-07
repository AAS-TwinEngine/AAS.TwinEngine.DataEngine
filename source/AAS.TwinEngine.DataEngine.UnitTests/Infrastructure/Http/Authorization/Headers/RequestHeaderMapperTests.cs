using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Config;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Authorization.Config;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Authorization.Headers;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Config;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.DataEngine.UnitTests.Infrastructure.Http.Authorization.Headers;

public class RequestHeaderMapperTests
{
    private static RequestHeaderMapper CreateService(
        GeneralConfig generalConfig,
        PluginsConfig? pluginsConfig = null,
        TemplateManagementConfig? templateManagementConfig = null)
    {
        var logger = new NullLogger<RequestHeaderMapper>();
        return new RequestHeaderMapper(
            logger,
            Options.Create(generalConfig),
            Options.Create(pluginsConfig ?? new PluginsConfig()),
            Options.Create(templateManagementConfig ?? new TemplateManagementConfig()));
    }

    [Fact]
    public void ValidateIncomingHeaders_RequiredHeaderMissing_ThrowsInvalidRequestHeaderException()
    {
        var generalConfig = new GeneralConfig { HeaderSanitization = new HeaderSanitizationOptions() };
        var templateManagementConfig = new TemplateManagementConfig
        {
            AasTemplateRepository = new ServiceEndpoint
            {
                HeaderMappings =
                [
                    new HeaderMappingRule { Source = "Authorization", Target = "Authorization", Required = true }
                ]
            }
        };

        var service = CreateService(generalConfig, templateManagementConfig: templateManagementConfig);
        var context = new DefaultHttpContext();

        Assert.Throws<InvalidRequestHeaderException>(() => service.ValidateIncomingHeaders(context));
    }

    [Fact]
    public void ApplyMappings_OptionalHeaderMissing_DoesNotThrow()
    {
        var generalConfig = new GeneralConfig { HeaderSanitization = new HeaderSanitizationOptions() };
        var templateManagementConfig = new TemplateManagementConfig
        {
            AasTemplateRepository = new ServiceEndpoint
            {
                HeaderMappings =
                [
                    new HeaderMappingRule { Source = "X-Optional", Target = "X-Optional", Required = false }
                ]
            }
        };

        var service = CreateService(generalConfig, templateManagementConfig: templateManagementConfig);
        var context = new DefaultHttpContext();
        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, "http://example.com");

        service.ApplyMappings(context, requestMessage, AasEnvironmentConfig.AasEnvironmentRepoHttpClientName);

        Assert.False(requestMessage.Headers.Contains("X-Optional"));
    }

    [Fact]
    public void ApplyMappings_MapsAuthorizationHeader()
    {
        var generalConfig = new GeneralConfig { HeaderSanitization = new HeaderSanitizationOptions() };
        var templateManagementConfig = new TemplateManagementConfig
        {
            AasTemplateRepository = new ServiceEndpoint
            {
                HeaderMappings =
                [
                    new HeaderMappingRule { Source = "Authorization", Target = "Authorization", Required = true }
                ]
            }
        };

        var service = CreateService(generalConfig, templateManagementConfig: templateManagementConfig);
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

        var generalConfig = new GeneralConfig { HeaderSanitization = new HeaderSanitizationOptions() };
        var pluginsConfig = new PluginsConfig
        {
            Instances =
            [
                new PluginInstance
                {
                    Name = PluginName,
                    BaseUrl = new Uri("http://example.com"),
                    HeaderMappings =
                    [
                        new HeaderMappingRule { Source = "X-Source", Target = "X-Target", Required = true }
                    ]
                }
            ]
        };

        var service = CreateService(generalConfig, pluginsConfig);
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
        var generalConfig = new GeneralConfig { HeaderSanitization = new HeaderSanitizationOptions() };
        var templateManagementConfig = new TemplateManagementConfig
        {
            AasTemplateRepository = new ServiceEndpoint
            {
                HeaderMappings =
                [
                    new HeaderMappingRule { Source = "X-Test", Target = "X-Test", Required = false }
                ]
            }
        };

        var service = CreateService(generalConfig, templateManagementConfig: templateManagementConfig);
        var context = new DefaultHttpContext();

        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, "http://example.com");

        service.ApplyMappings(context, requestMessage, AasEnvironmentConfig.AasEnvironmentRepoHttpClientName);

        Assert.False(requestMessage.Headers.Contains("X-Test"));
    }

    [Fact]
    public void ValidateIncomingHeaders_InvalidMappedHeader_ThrowsBadRequest()
    {
        var generalConfig = new GeneralConfig
        {
            // Default BlockedPatterns already includes "<script"
            HeaderSanitization = new HeaderSanitizationOptions()
        };
        var templateManagementConfig = new TemplateManagementConfig
        {
            AasTemplateRepository = new ServiceEndpoint
            {
                HeaderMappings =
                [
                    new HeaderMappingRule { Source = "X-Test", Target = "X-Test", Required = false }
                ]
            }
        };

        var service = CreateService(generalConfig, templateManagementConfig: templateManagementConfig);
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Test"] = "ok<script";

        Assert.Throws<InvalidRequestHeaderException>(() => service.ValidateIncomingHeaders(context));
    }

    [Fact]
    public void ValidateIncomingHeaders_AllHeadersValid_DoesNotThrow()
    {
        var generalConfig = new GeneralConfig { HeaderSanitization = new HeaderSanitizationOptions() };

        var service = CreateService(generalConfig);
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Valid"] = "simple-value";
        context.Request.Headers.Authorization = "Bearer token";

        service.ValidateIncomingHeaders(context);
    }

    [Fact]
    public void ApplyMappings_TemplateRegistryClient_UsesTemplateRegistryMappings()
    {
        var generalConfig = new GeneralConfig { HeaderSanitization = new HeaderSanitizationOptions() };
        var templateManagementConfig = new TemplateManagementConfig
        {
            AasTemplateRegistry = new ServiceEndpoint
            {
                HeaderMappings =
                [
                    new HeaderMappingRule { Source = "X-Source", Target = "X-Registry", Required = true }
                ]
            }
        };

        var service = CreateService(generalConfig, templateManagementConfig: templateManagementConfig);
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Source"] = "value";

        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, "http://example.com");

        service.ApplyMappings(context, requestMessage, AasEnvironmentConfig.AasRegistryHttpClientName);

        Assert.True(requestMessage.Headers.TryGetValues("X-Registry", out var values));
        Assert.Contains("value", values);
    }
}
