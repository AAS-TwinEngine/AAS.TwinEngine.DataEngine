using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Config;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Clients;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Config;

using NSubstitute;

namespace AAS.TwinEngine.DataEngine.UnitTests.Infrastructure.Http.Clients;

public class HttpClientFactoryTests
{
    [Theory]
    [InlineData(AasEnvironmentConfig.AasEnvironmentRepoHttpClientName)]
    [InlineData(PluginConfig.HttpClientNamePrefix + "PluginName")]
    public void CreateClient_Returns_HttpClient(string clientName)
    {
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        var httpClient = Substitute.For<HttpClient>();
        httpClientFactory.CreateClient(clientName).Returns(httpClient);
        var pluginDataProviderHttpClientFactory = new HttpClientFactory(httpClientFactory);

        var result = pluginDataProviderHttpClientFactory.CreateClient(clientName);

        Assert.Equal(httpClient, result);
    }
}
