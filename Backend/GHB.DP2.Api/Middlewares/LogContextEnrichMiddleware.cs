namespace GHB.DP2.Api.Middlewares;

using System.Buffers;
using System.Net;
using System.Text;
using System.Text.Json;
using Configurations;
using FastEndpoints;
using Serilog.Context;

public class LogContextEnrichMiddleware : IMiddleware
{
    private const string ReverseProxyHeaderForOriginalIpAddress = "X-Forwarded-For";

    private readonly static IEnumerable<string> MaskedKeys =
    [
        "password",
        "email",
        "token",
        "access_token",
        "refresh_token",
        "secret",
        "api_key",
        "apikey",
        "citizen",
    ];

    private readonly LoggingOptionsConfiguration options;
    private readonly ILogger<LogContextEnrichMiddleware> logger;

    public LogContextEnrichMiddleware(
        LoggingOptionsConfiguration options,
        ILogger<LogContextEnrichMiddleware> logger)
    {
        this.options = options;
        this.logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var endpoint = context.GetEndpoint();
        var endpointDefinition = endpoint?.Metadata.OfType<EndpointDefinition>().SingleOrDefault();

        var program = endpointDefinition?.EndpointSummary?.Summary ?? context.Request.Path;
        var requestId = context.TraceIdentifier;
        var user = context.User.Identity?.Name;
        var ipAddress = TryGetIpAddress(context, this.logger);

        using (LogContext.PushProperty("RequestId", requestId))
        using (LogContext.PushProperty("Program", program))
        using (LogContext.PushProperty("User", user))
        using (LogContext.PushProperty("IpAddress", ipAddress))
        {
            var queryStringDictionary = context.Request.QueryString.Value;

            var formData =
                context.Request.HasFormContentType
                    ? context.Request.Form
                             .ToDictionary(x => x.Key, MaskText)
                    : [];

            var requestJsonString =
                this.options.EnableAdvancedRequestLogging ? await GetRequestJsonAsync(context.Request) : "{}";
            var flattenAndMaskedJsonDictionary = GetFlattenedRequestJson(requestJsonString, this.logger);

            try
            {
                await next(context);

                var response = context.Response;

                var isNoneSuccessResponse = response.StatusCode >= 400;

                if (isNoneSuccessResponse)
                {
                    var jsonResponse = await GetResponseJsonAsync(response);

                    this.logger.LogDebug(
                        "A request result in {StatusCode} with {QueryString} {FormData} {FlattenRequestJson}",
                        response.StatusCode,
                        queryStringDictionary,
                        JsonSerializer.Serialize(formData),
                        JsonSerializer.Serialize(flattenAndMaskedJsonDictionary));
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(
                    ex,
                    "An error occurred with the following parameter(s) {QueryString} {FormData} {FlattenRequestJson}",
                    queryStringDictionary,
                    JsonSerializer.Serialize(formData),
                    JsonSerializer.Serialize(flattenAndMaskedJsonDictionary));

                throw new Exception("Error during request processing in LogContextEnrichMiddleware", ex);
            }
        }
    }

    private static IPAddress? TryGetIpAddress(HttpContext context, ILogger logger)
    {
        var headers = context.Request.Headers;

        if (!headers.TryGetValue(ReverseProxyHeaderForOriginalIpAddress, out var originalIpAddresses))
        {
            logger.LogWarning("Missing header : {Header}", ReverseProxyHeaderForOriginalIpAddress);

            return context.Connection.RemoteIpAddress;
        }

        var originalIpAddr = originalIpAddresses.FirstOrDefault()?.Split(",").FirstOrDefault();

        if (IPAddress.TryParse(originalIpAddr, out var ipAddress))
        {
            return ipAddress;
        }

        logger.LogError("Cannot parse original IP address: {OriginalIpAddr}", originalIpAddr!);

        return context.Connection.RemoteIpAddress;
    }

    private static async Task<string> GetRequestJsonAsync(HttpRequest request)
    {
        const int maximumReadContentCount = 2048;

        request.EnableBuffering();

        try
        {
            switch (request.ContentType)
            {
                case var ct when string.IsNullOrWhiteSpace(ct)
                                 || ct.StartsWith("multipart/form-data")
                                 || ct.StartsWith("application/octet-stream")
                                 || ct.StartsWith("application/x-www-form-urlencoded"):
                    {
                        return "{}";
                    }

                default:
                    {
                        var contentLength = Math.Clamp((int)(request.ContentLength ?? 0), 0, maximumReadContentCount);

                        if (contentLength == 0)
                        {
                            return "{}";
                        }

                        var buffer = ArrayPool<byte>.Shared.Rent(contentLength);

                        try
                        {
                            _ = await request.Body.ReadAsync(buffer.AsMemory(0, contentLength));

                            // ArrayPool may return a larger array size than requested, so we need to slice it to exactly what we asked for
                            // to prevent over-read bytes.
                            return Encoding.UTF8.GetString(buffer[..contentLength]);
                        }
                        finally
                        {
                            ArrayPool<byte>.Shared.Return(buffer, true);
                        }
                    }
            }
        }
        catch (Exception ex)
        {
            return $"Unable to extract request body due to {ex.Message}";
        }
        finally
        {
            request.Body.Position = 0;
        }
    }

    private static async Task<string> GetResponseJsonAsync(HttpResponse response)
    {
        const int maximumReadContentCount = 2048;

        try
        {
            switch (response.ContentType)
            {
                case var ct when string.IsNullOrWhiteSpace(ct)
                                 || ct.StartsWith("multipart/form-data")
                                 || ct.StartsWith("application/octet-stream"):
                    {
                        return "{}";
                    }

                default:
                    {
                        var contentLength = Math.Clamp((int)(response.ContentLength ?? 0), 0, maximumReadContentCount);

                        if (contentLength == 0)
                        {
                            return "{}";
                        }

                        var buffer = ArrayPool<byte>.Shared.Rent(contentLength);

                        try
                        {
                            _ = await response.Body.ReadAsync(buffer.AsMemory(0, contentLength));

                            // ArrayPool may return a larger array size than requested, so we need to slice it to exactly what we asked for
                            // to prevent over-read bytes.
                            return Encoding.UTF8.GetString(buffer[..contentLength]);
                        }
                        finally
                        {
                            ArrayPool<byte>.Shared.Return(buffer, true);
                        }
                    }
            }
        }
        catch (Exception ex)
        {
            return $"Unable to extract request body due to {ex.Message}";
        }
    }

    private static Dictionary<string, string> GetFlattenedRequestJson(string requestJsonString, ILogger logger)
    {
        try
        {
            var jsonDocument = JsonSerializer.Deserialize<JsonDocument>(requestJsonString)!;
            var flattenDictionary = GetFlattenedJson(jsonDocument.RootElement, string.Empty);

            return flattenDictionary.ToDictionary(kvp => kvp.Key, MaskText);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unable to extract json body from request: {Message}", ex.Message);

            return [];
        }

        static IEnumerable<KeyValuePair<string, string>> GetFlattenedJson(JsonElement element, string key)
        {
            switch (element.ValueKind)
            {
                // If the value is a simple type (string, number, boolean), add it to the flattened dictionary with the corresponding key
                case JsonValueKind.String:
                case JsonValueKind.Number:
                case JsonValueKind.True:
                case JsonValueKind.False:
                    {
                        yield return new KeyValuePair<string, string>(key, element.ToString());

                        break;
                    }

                // If the value is an object, flatten each property of the object recursively
                case JsonValueKind.Object:
                    {
                        var iterator = element.EnumerateObject().Bind(objProperty =>
                        {
                            var objKey = GetKey(key, objProperty.Name);

                            return GetFlattenedJson(objProperty.Value, objKey);
                        });

                        foreach (var kvp in iterator)
                        {
                            yield return kvp;
                        }

                        break;
                    }

                // If the value is an array, flatten each element of the array recursively
                case JsonValueKind.Array:
                    {
                        var iterator =
                            element.EnumerateArray().Map((index, element) =>
                            {
                                var arrayKey = GetKey(key, index.ToString());

                                return GetFlattenedJson(element, arrayKey);
                            }).Flatten();

                        foreach (var kvp in iterator)
                        {
                            yield return kvp;
                        }

                        break;
                    }
            }

            string GetKey(string key, string value)
            {
                return string.IsNullOrWhiteSpace(key) ? value : $"{key}_{value}";
            }
        }
    }

    private static string MaskText<T>(KeyValuePair<string, T> kvp)
    {
        var isSensitiveData = MaskedKeys.Any(k => kvp.Key.Contains(k, StringComparison.OrdinalIgnoreCase));

        return isSensitiveData
            ? "***"
            : kvp.Value switch
            {
                string s => s,
                JsonElement json => json.ToString(),
                _ => kvp.Value?.ToString() ?? string.Empty,
            };
    }
}