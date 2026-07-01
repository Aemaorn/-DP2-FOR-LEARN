namespace GHB.DP2.Infrastructure.Services.ErmEmployee;

using System.Net.Http.Headers;

public class AuthErmEmployeeHandler : DelegatingHandler
{
    private readonly IErmEmployeeTokenProvider tokenProvider;

    public AuthErmEmployeeHandler(IErmEmployeeTokenProvider tokenProvider)
    {
        this.tokenProvider = tokenProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        await AttachAsync();

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            response.Dispose();
            await AttachAsync();

            var requestCopy = new HttpRequestMessage(request.Method, request.RequestUri)
            {
                Content = await CloneContentAsync(request.Content),
                Version = request.Version,
            }.CopyHeadersFrom(request);

            response = await base.SendAsync(
                requestCopy,
                cancellationToken);
        }

        return response;

        async Task AttachAsync()
        {
            var token = await this.tokenProvider.GetTokenAsync(cancellationToken);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
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