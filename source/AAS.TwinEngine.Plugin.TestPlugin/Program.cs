using AAS.TwinEngine.Plugin.TestPlugin.Infrastructure.Providers;
using AAS.TwinEngine.Plugin.TestPlugin.ServiceConfiguration;

using Asp.Versioning;

namespace AAS.TwinEngine.Plugin.TestPlugin;

public static class Program
{
    private static readonly Version ApiVersion = new(1, 0);
    private const string ApiTitle = "TestPlugin API";

    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.ConfigureLogging(builder.Configuration);

        builder.Services.AddHttpContextAccessor();
        builder.Services.ConfigureInfrastructure(builder.Configuration);
        builder.Services.ConfigureApplication(builder.Configuration);
        builder.Services.AddAuthorization();

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

        using (var scope = app.Services.CreateScope())
        {
            var initializer = scope.ServiceProvider.GetRequiredService<MockDataInitializer>();
            initializer.Initialize(CancellationToken.None);
        }

        app.UseExceptionHandler();
        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.UseOpenApi(c => c.PostProcess = (d, _) => d.Servers.Clear());
        app.UseSwaggerUI(c => c.SwaggerEndpoint($"/swagger/{ApiVersion:F1}/swagger.json", ApiTitle));
        app.MapControllers();

        await app.RunAsync();
    }
}
