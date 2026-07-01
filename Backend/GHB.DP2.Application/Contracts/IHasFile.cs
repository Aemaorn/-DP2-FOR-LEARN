namespace GHB.DP2.Application.Contracts;

using Microsoft.AspNetCore.Http;

public interface IHasFile
{
    IFormFile? File { get; }
}