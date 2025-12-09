using System.Diagnostics.CodeAnalysis;

namespace AAS.TwinEngine.DataEngine.ServiceConfiguration;

[ExcludeFromCodeCoverage]
internal static class CorsConfigurationExtension
{
    public static void ConfigureCorsServices(this WebApplicationBuilder builder)
    {
        _ = builder.Services.AddCors(options =>
        {
            options.AddPolicy("CorsPolicy", corsPolicyBuilder => corsPolicyBuilder
                                                                 .WithOrigins("http://localhost:4200", "http://localhost:4280")
                                                                 .AllowAnyHeader()
                                                                 .AllowAnyMethod()
                                                                 .AllowCredentials()
                                                                 .SetIsOriginAllowed((host) => true));
        });
    }

    public static void UseCorsServices(this WebApplication app) => app.UseCors("CorsPolicy");
}
