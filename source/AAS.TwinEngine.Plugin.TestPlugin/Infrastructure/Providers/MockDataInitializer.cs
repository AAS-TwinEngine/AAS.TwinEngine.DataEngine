using System.Text.Json;

using AAS.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Constants;
using AAS.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Exceptions;

namespace AAS.TwinEngine.Plugin.TestPlugin.Infrastructure.Providers;

public class MockDataInitializer(IHostEnvironment env, ILogger<MockDataInitializer> logger)
{
    public void Initialize(CancellationToken cancellationToken)
    {
        var dataPath = Path.Combine(env.ContentRootPath, "Data");

        MockData.MetaData = LoadData(Path.Combine(dataPath, "mock-metadata.json"));

        MockData.SubmodelData = LoadData(Path.Combine(dataPath, "mock-submodel-data.json"));
    }

    private JsonDocument LoadData(string filePath)
    {
        if (!File.Exists(filePath))
        {
            logger.LogCritical("data file not found at {FilePath}", filePath);
            throw new FileNotFoundException("JSON data file not found.", filePath);
        }

        try
        {
            using var fileStream = File.OpenRead(filePath);
            using var streamReader = new StreamReader(fileStream);
            var jsonContent = streamReader.ReadToEnd();
            return JsonDocument.Parse(jsonContent);
        }
        catch (JsonException jex)
        {
            logger.LogError(jex, "Invalid JSON in file {FilePath}", filePath);
            throw new InternalServerException(ExceptionMessages.ResourceNotValid);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error reading data file {FilePath}", filePath);
            throw new InternalServerException(ExceptionMessages.ResourceNotValid);
        }
    }
}
