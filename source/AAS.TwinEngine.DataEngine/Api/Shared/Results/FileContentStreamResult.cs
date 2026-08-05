using System.Buffers;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Observability;
using AAS.TwinEngine.DataEngine.DomainModel.Shared;

using Microsoft.AspNetCore.Mvc;

namespace AAS.TwinEngine.DataEngine.Api.Shared.Results;

public class FileContentStreamResult(FileAttachmentResult attachment) : IActionResult
{
    public async Task ExecuteResultAsync(ActionContext context)
    {
        var response = context.HttpContext.Response;
        response.ContentType = attachment.ContentType;
        response.Headers.ContentDisposition = $"attachment; filename=\"{attachment.FileName ?? string.Empty}\"";

        await using (attachment)
        {
            using var activity = DataEngineTracing.Source.StartActivity(DataEngineTracing.Spans.StreamResponse);
            var buffer = ArrayPool<byte>.Shared.Rent(81920);
            try
            {
                long totalRead = 0;
                int bytesRead;
                var cancellationToken = context.HttpContext.RequestAborted;

                while ((bytesRead = await attachment.Content.ReadAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false)) > 0)
                {
                    totalRead += bytesRead;
                    if (totalRead > attachment.MaxAllowedBytes)
                    {
                        throw new FileSizeExceededException();
                    }

                    await response.Body.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }
}
