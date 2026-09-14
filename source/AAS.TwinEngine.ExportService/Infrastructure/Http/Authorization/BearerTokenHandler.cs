using System.Net.Http.Headers;

namespace AAS.TwinEngine.ExportService.Infrastructure.Http.Authorization;

/// <summary>
/// DelegatingHandler that attaches a bearer token acquired from <see cref="ITokenProvider"/>
/// to every request of a named HttpClient. Skips attaching when the endpoint requires no auth.
/// </summary>
public sealed class BearerTokenHandler : DelegatingHandler
{
    private readonly ITokenProvider _tokenProvider;
    private readonly string _endpointName;

    public BearerTokenHandler(ITokenProvider tokenProvider, string endpointName)
    {
        _tokenProvider = tokenProvider;
        _endpointName = endpointName;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _tokenProvider.GetTokenAsync(_endpointName, cancellationToken).ConfigureAwait(false);

        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
