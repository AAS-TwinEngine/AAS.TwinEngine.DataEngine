using AAS.TwinEngine.Plugin.TestPlugin.Infrastructure.Monitoring;
using AAS.TwinEngine.Plugin.TestPlugin.Infrastructure.Providers;
using AAS.TwinEngine.Plugin.TestPlugin.ServiceConfiguration;

using Asp.Versioning;

using Microsoft.AspNetCore.ResponseCompression;

using System.IO.Compression;

namespace AAS.TwinEngine.Plugin.TestPlugin;

public static class Program
{
    private static readonly Version ApiVersion = new(1, 0);
    private const string ApiTitle = "TestPlugin API";

    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        _ = builder.Services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
        });
        _ = builder.Services.Configure<BrotliCompressionProviderOptions>(options => options.Level = CompressionLevel.Optimal);
        _ = builder.Services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Optimal);

        builder.ConfigureLogging(builder.Configuration);

        builder.Services.AddHttpContextAccessor();
        builder.Services.ConfigureInfrastructure(builder.Configuration);
        builder.Services.ConfigureApplication(builder.Configuration);
        builder.Services.AddAuthorization();

        builder.Services.AddHealthChecks().AddCheck<MockDataHealthCheck>("mock_data");

        builder.Services.AddControllers();

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddOpenApiDocument(settings =>
        {
            settings.DocumentName = ApiVersion.ToString();
            settings.Title = ApiTitle;
        });

        builder.Services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(ApiVersion.Major, ApiVersion.Minor);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = new HeaderApiVersionReader("api-version");
        })
        .AddMvc();

        var app = builder.Build();

        app.MapHealthChecks("/healthz");

        using (var scope = app.Services.CreateScope())
        {
            var initializer = scope.ServiceProvider.GetRequiredService<MockDataInitializer>();
            initializer.Initialize(CancellationToken.None);
        }

        app.UseExceptionHandler();
        app.UseHttpsRedirection();
        _ = app.UseResponseCompression();

        app.UseAuthorization();
        app.UseOpenApi(c => c.PostProcess = (d, _) => d.Servers.Clear());
        app.MapControllers();

        await app.RunAsync();
    }
}
