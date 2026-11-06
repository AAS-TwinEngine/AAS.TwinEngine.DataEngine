using System.Globalization;
using System.Reflection;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Formatters;

using NSwag;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;

namespace AAS.TwinEngine.DataEngine.ServiceConfiguration.OpenApi;

/// <summary>
/// Advertises the media types declared via <see cref="ProducesResponseTypeAttribute"/> in the generated
/// OpenAPI document. NSwag reduces binary (file) responses to <c>application/octet-stream</c>, which hides
/// the content types an endpoint actually negotiates.
/// </summary>
public class ResponseContentTypesOperationProcessor : IOperationProcessor
{
    public bool Process(OperationProcessorContext context)
    {
        var responses = context.OperationDescription.Operation.Responses;

        foreach (var attribute in context.MethodInfo.GetCustomAttributes<ProducesResponseTypeAttribute>())
        {
            var contentTypes = new MediaTypeCollection();
            ((IApiResponseMetadataProvider)attribute).SetContentTypes(contentTypes);

            if (contentTypes.Count == 0 ||
                !responses.TryGetValue(attribute.StatusCode.ToString(CultureInfo.InvariantCulture), out var response))
            {
                continue;
            }

            var schema = response.Content.Values.FirstOrDefault()?.Schema;

            response.Content.Clear();

            foreach (var contentType in contentTypes)
            {
                response.Content[contentType] = new OpenApiMediaType { Schema = schema };
            }
        }

        return true;
    }
}
