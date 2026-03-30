using System.IO.Compression;

using Microsoft.AspNetCore.ResponseCompression;

namespace AAS.TwinEngine.DataEngine.ServiceConfiguration;

public static class ResponseCompressionConfigurationExtension
{
    public static void ConfigureResponseCompression(this IServiceCollection services)
    {
        _ = services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
        });
        _ = services.Configure<BrotliCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);
        _ = services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);
    }
}
