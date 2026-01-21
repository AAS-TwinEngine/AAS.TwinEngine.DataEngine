using System.Collections.ObjectModel;
using System.Net;
using System.Text.Json.Serialization;

using Microsoft.AspNetCore.Mvc;

namespace AAS.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Exceptions.Responses;

public class ServiceErrorResponse : ProblemDetails
{
    [JsonPropertyName("errors")]
    public ReadOnlyCollection<ApiError>? Errors { get; set; }

    [JsonPropertyName("traceId")]
    public string? TraceId { get; set; }

    public ServiceErrorResponse Create(HttpStatusCode status, string title, string? traceId = null)
    {
        var problem = new ServiceErrorResponse
        {
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
            Title = "An error occurred.",
            Status = (int)status,
            TraceId = traceId,
            Errors = new ReadOnlyCollection<ApiError>(new List<ApiError>
            {
                new() { Description = title }
            })
        };

        return problem;
    }
}

public class ApiError
{
    [JsonPropertyName("description")]
    public string Description { get; set; } = null!;
}

