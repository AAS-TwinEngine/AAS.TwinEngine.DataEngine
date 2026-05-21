using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AAS.TwinEngine.DataEngine.ModuleTests.Common;

/// <summary>
/// A <see cref="WebApplicationFactory{Program}"/> that boots the application using a
/// specific config directory under TestData/. Each directory contains its own
/// appsettings.json in either V1 (old) or V2 (new) format. The content root is
/// pointed to that directory so <see cref="WebApplication.CreateBuilder"/> loads
/// the correct settings file automatically.
/// </summary>
/// <param name="configDirName">
/// Subdirectory name under TestData (e.g. "v1-config" or "v2-config")
/// that contains the appsettings.json file.
/// </param>
/// <param name="configureTestServices">
/// Optional callback to register mock/test services into the DI container.
/// </param>
internal sealed class ConfigTestFactory(string configDirName, Action<IServiceCollection>? configureTestServices = null) : WebApplicationFactory<Program>
{
    private readonly string _configDir = Path.Combine(AppContext.BaseDirectory, "TestData", configDirName);
    private readonly Action<IServiceCollection>? _configureTestServices = configureTestServices;

    protected override IHost CreateHost(IHostBuilder builder)
    {
        _ = builder.UseContentRoot(_configDir);
        _ = builder.UseEnvironment("ConfigTest");

        if (_configureTestServices is not null)
        {
            _ = builder.ConfigureServices(_configureTestServices);
        }

        return base.CreateHost(builder);
    }
}
