namespace GHB.DP2.Infrastructure.Services.Coi;

using System.Net.Http.Headers;

public class AuthCoiHandler : DelegatingHandler
{
    private readonly ITokenProvider tokenProvider;

    public AuthCoiHandler(ITokenProvider tokenProvider)
    {
        this.tokenProvider = tokenProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        await AttachAsync(request);

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            response.Dispose();
            this.tokenProvider.Invalidate();

            var requestCopy = new HttpRequestMessage(request.Method, request.RequestUri)
            {
                Content = await CloneContentAsync(request.Content),
                Version = request.Version,
            }.CopyHeadersFrom(request);

            await AttachAsync(requestCopy);

            response = await base.SendAsync(
                requestCopy,
                cancellationToken);
        }

        return response;

        async Task AttachAsync(HttpRequestMessage req)
        {
            var token = await this.tokenProvider.GetTokenAsync(cancellationToken);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }

    private static async Task<HttpContent?> CloneContentAsync(HttpContent? content)
    {
        if (content is null)
        {
            return null;
        }

        var bytes = await content.ReadAsByteArrayAsync();
        var clone = new ByteArrayContent(bytes);

        foreach (var h in content.Headers)
        {
            clone.Headers.TryAddWithoutValidation(h.Key, h.Value);
        }

        return clone;
    }
}

file static class HttpExtensions
{
    public static HttpRequestMessage CopyHeadersFrom(this HttpRequestMessage target, HttpRequestMessage source)
    {
        foreach (var h in source.Headers)
        {
            target.Headers.TryAddWithoutValidation(h.Key, h.Value);
        }

        foreach (var kv in source.Options)
        {
            target.Options.Set(new HttpRequestOptionsKey<object?>(kv.Key), kv.Value);
        }

        target.VersionPolicy = source.VersionPolicy;

        return target;
    }
}