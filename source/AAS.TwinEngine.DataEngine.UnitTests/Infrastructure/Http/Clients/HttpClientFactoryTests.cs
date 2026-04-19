using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Clients;

using NSubstitute;

namespace AAS.TwinEngine.DataEngine.UnitTests.Infrastructure.Http.Clients;

public class HttpClientFactoryTests
{
    [Theory]
    [InlineData(HttpClientNames.SubmodelTemplateRepository)]
    [InlineData(HttpClientNames.PluginDataProviderPrefix + "PluginName")]
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
