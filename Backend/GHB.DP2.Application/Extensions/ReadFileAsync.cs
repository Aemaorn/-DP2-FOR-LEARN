namespace GHB.DP2.Application.Extensions;

using Microsoft.AspNetCore.Http;

public static class ReadFileExtension
{
    public static async Task<byte[]> ReadFileAsync(this IFormFile formFile, CancellationToken cancellationToken)
    {
        await using var memoryStream = new MemoryStream();
        await formFile.CopyToAsync(memoryStream, cancellationToken);
        return memoryStream.ToArray();
    }
}