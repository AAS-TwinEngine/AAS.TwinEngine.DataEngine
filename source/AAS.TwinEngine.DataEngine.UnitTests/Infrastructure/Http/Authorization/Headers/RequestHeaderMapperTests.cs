using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Authorization.Config;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Authorization.Headers;
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
        return new RequestHeaderMapper(
            new NullLogger<RequestHeaderMapper>(),
            Options.Create(generalConfig),
            Options.Create(pluginsConfig ?? new PluginsConfig()),
            Options.Create(templateManagementConfig ?? new TemplateManagementConfig()));
    }

    private static GeneralConfig DefaultConfig() => new()
    {
        HeaderSanitization = new HeaderSanitizationOptions()
    };

    [Fact]
    public void ValidateIncomingHeaders_NullContext_DoesNothing()
    {
        var service = CreateService(DefaultConfig());
        service.ValidateIncomingHeaders(null);
    }

    [Fact]
    public void ValidateIncomingHeaders_ValidHeaders_DoesNotThrow()
    {
        var service = CreateService(DefaultConfig());
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Test"] = "value";

        service.ValidateIncomingHeaders(context);
    }

    [Fact]
    public void ValidateIncomingHeaders_EmptyHeaderValue_Throws()
    {
        var service = CreateService(DefaultConfig());
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Test"] = "";

        Assert.Throws<InvalidRequestHeaderException>(() => service.ValidateIncomingHeaders(context));
    }

    [Fact]
    public void ValidateIncomingHeaders_HeaderTooLarge_Throws()
    {
        var config = DefaultConfig();
        config.HeaderSanitization.MaxHeaderSize = 5;

        var service = CreateService(config);
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Test"] = "123456";

        Assert.Throws<InvalidRequestHeaderException>(() => service.ValidateIncomingHeaders(context));
    }

    [Fact]
    public void ValidateIncomingHeaders_RequiredHeaderMissing_Throws()
    {
        var config = DefaultConfig();
        var templateConfig = new TemplateManagementConfig
        {
            AasTemplateRepository = new ServiceInstance
            {
                HeaderMappings =
                [
                    new HeaderMappingRule { Source = "Authorization", Target = "Authorization", Required = true }
                ]
            }
        };

        var service = CreateService(config, templateManagementConfig: templateConfig);
        var context = new DefaultHttpContext();

        Assert.Throws<InvalidRequestHeaderException>(() => service.ValidateIncomingHeaders(context));
    }

    [Fact]
    public void ApplyMappings_NullRequest_Throws()
    {
        var service = CreateService(DefaultConfig());

        Assert.Throws<InvalidDependencyException>(() =>
            service.ApplyMappings(new DefaultHttpContext(), null!, "client"));
    }

    [Fact]
    public void ApplyMappings_NullClientName_Throws()
    {
        var service = CreateService(DefaultConfig());
        using var request = new HttpRequestMessage();

        Assert.Throws<InvalidDependencyException>(() =>
            service.ApplyMappings(new DefaultHttpContext(), request, null!));
    }

    [Fact]
    public void ApplyMappings_NoMappings_DoesNothing()
    {
        var service = CreateService(DefaultConfig());
        var context = new DefaultHttpContext();

        using var request = new HttpRequestMessage();

        service.ApplyMappings(context, request, "unknown-client");

        Assert.Empty(request.Headers);
    }

    [Fact]
    public void ApplyMappings_MapsAuthorizationHeader()
    {
        var config = DefaultConfig();
        var templateConfig = new TemplateManagementConfig
        {
            AasTemplateRepository = new ServiceInstance
            {
                HeaderMappings =
                [
                    new HeaderMappingRule { Source = "Authorization", Target = "Authorization" }
                ]
            }
        };

        var service = CreateService(config, templateManagementConfig: templateConfig);

        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "Bearer token";

        using var request = new HttpRequestMessage();

        service.ApplyMappings(context, request, HttpClientNames.AasTemplateRepository);

        Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
        Assert.Equal("token", request.Headers.Authorization?.Parameter);
    }

    [Fact]
    public void ApplyMappings_RenamesHeader()
    {
        var config = DefaultConfig();
        var templateConfig = new TemplateManagementConfig
        {
            AasTemplateRepository = new ServiceInstance
            {
                HeaderMappings =
                [
                    new HeaderMappingRule { Source = "X-Source", Target = "X-Target" }
                ]
            }
        };

        var service = CreateService(config, templateManagementConfig: templateConfig);

        var context = new DefaultHttpContext();
        context.Request.Headers["X-Source"] = "value";

        using var request = new HttpRequestMessage();

        service.ApplyMappings(context, request, HttpClientNames.AasTemplateRepository);

        Assert.True(request.Headers.Contains("X-Target"));
    }

    [Fact]
    public void ApplyMappings_OverwriteExistingHeader()
    {
        var config = DefaultConfig();
        var templateConfig = new TemplateManagementConfig
        {
            AasTemplateRepository = new ServiceInstance
            {
                HeaderMappings =
                [
                    new HeaderMappingRule { Source = "X-Source", Target = "X-Test" }
                ]
            }
        };

        var service = CreateService(config, templateManagementConfig: templateConfig);

        var context = new DefaultHttpContext();
        context.Request.Headers["X-Source"] = "new";

        using var request = new HttpRequestMessage();
        request.Headers.TryAddWithoutValidation("X-Test", "old");

        service.ApplyMappings(context, request, HttpClientNames.AasTemplateRepository);

        var value = request.Headers.GetValues("X-Test").First();
        Assert.Equal("new", value);
    }

    [Fact]
    public void ApplyMappings_MultipleValues_CombinedCorrectly()
    {
        var config = DefaultConfig();
        var templateConfig = new TemplateManagementConfig
        {
            AasTemplateRepository = new ServiceInstance
            {
                HeaderMappings =
                [
                    new HeaderMappingRule { Source = "X-Source", Target = "X-Target" }
                ]
            }
        };

        var service = CreateService(config, templateManagementConfig: templateConfig);

        var context = new DefaultHttpContext();
        context.Request.Headers["X-Source"] = new[] { "a", "b" };

        using var request = new HttpRequestMessage();

        service.ApplyMappings(context, request, HttpClientNames.AasTemplateRepository);

        var value = request.Headers.GetValues("X-Target").First();
        Assert.Equal("a,b", value);
    }

    [Fact]
    public void ApplyMappings_PluginMapping_Works()
    {
        var config = DefaultConfig();
        var pluginName = "TestPlugin";

        var pluginsConfig = new PluginsConfig
        {
            Instances =
            [
                new ServiceInstance
                {
                    Name = pluginName,
                    BaseUrl = new Uri("http://test"),
                    HeaderMappings =
                    [
                        new HeaderMappingRule { Source = "X-A", Target = "X-B" }
                    ]
                }
            ]
        };

        var service = CreateService(config, pluginsConfig);

        var context = new DefaultHttpContext();
        context.Request.Headers["X-A"] = "value";

        using var request = new HttpRequestMessage();

        var clientName = HttpClientNames.PluginDataProviderPrefix + pluginName;

        service.ApplyMappings(context, request, clientName);

        Assert.True(request.Headers.Contains("X-B"));
    }
}
