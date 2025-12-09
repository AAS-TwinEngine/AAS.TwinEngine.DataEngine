using AAS.TwinEngine.DataEngine.Api.Configuration;

using Microsoft.AspNetCore.Mvc;

namespace AAS.TwinEngine.DataEngine.Api.Base;

/// <summary>
///     The class is used to do some common operations
///     and inherited by other controller.
/// </summary>
/// <typeparam name="TController">The generic controller class for logger.</typeparam>
[ApiController]
public class BaseApiController<TController> : ControllerBase
{
    private readonly ApiConfiguration _apiConfiguration = null!;

    protected ILogger<TController> Logger { get; }

    public BaseApiController(ILogger<TController> logger) => Logger = logger;

    public BaseApiController(ILogger<TController> logger, ApiConfiguration apiConfiguration)
    {
        _apiConfiguration = apiConfiguration;
        Logger = logger;
    }

    protected string FinalizeLink(string relativePath) => $"{_apiConfiguration.BasePath}{relativePath}";
}
