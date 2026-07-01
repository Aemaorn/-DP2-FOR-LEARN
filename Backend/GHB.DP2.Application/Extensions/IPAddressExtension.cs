namespace GHB.DP2.Application.Extensions;

using System.Net;
using Microsoft.AspNetCore.Http;

public static class IpAddressExtension
{
    public static IPAddress? TryGetIpAddress(this HttpContext context)
    {
        const string reverseProxyHeaderForOriginalIpAddress = "X-Forwarded-For";

        var headers = context.Request.Headers;

        if (!headers.TryGetValue(reverseProxyHeaderForOriginalIpAddress, out var originalIpAddresses))
        {
            return context.Connection.RemoteIpAddress;
        }

        var originalIpAddr = originalIpAddresses.FirstOrDefault()?.Split(",").FirstOrDefault();

        if (IPAddress.TryParse(originalIpAddr, out var ipAddress))
        {
            return ipAddress;
        }

        return context.Connection.RemoteIpAddress;
    }
}