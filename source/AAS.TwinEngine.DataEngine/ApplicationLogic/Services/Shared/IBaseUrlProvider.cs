namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Shared;

/// <summary>
/// Provides the base URL of the DataEngine's own repository.
/// V2 config: extracted from the current HTTP request.
/// V1 config: falls back to the configured <c>DataEngineRepositoryBaseUrl</c> value.
/// </summary>
public interface IBaseUrlProvider
{
    Uri GetBaseUrl();
}
