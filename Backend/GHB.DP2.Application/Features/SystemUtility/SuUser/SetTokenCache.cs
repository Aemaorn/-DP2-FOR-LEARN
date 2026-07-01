namespace GHB.DP2.Application.Features.SystemUtility.SuUser;

using GHB.DP2.Application.Services.Token;
using Microsoft.AspNetCore.Http;
using FluentValidation;
using System.Text.Json;

public record SetTokenCacheCommand(string UserName, string AccessToken, string RefreshToken);

public class SetTokenCacheCommandValidator : Validator<SetTokenCacheCommand>
{
    public SetTokenCacheCommandValidator()
    {
        this.RuleFor(x => x.UserName)
            .NotNull()
            .NotEmpty()
            .WithMessage("UserName is required");

        this.RuleFor(x => x.AccessToken)
            .NotNull()
            .NotEmpty()
            .WithMessage("AccessToken is required");

        this.RuleFor(x => x.RefreshToken)
            .NotNull()
            .NotEmpty()
            .WithMessage("RefreshToken is required");
    }
}

public class SetTokenCache : Endpoint<SetTokenCacheCommand>
{
    private readonly ITokenCacheService tokenCacheService;

    public SetTokenCache(ITokenCacheService tokenCacheService)
    {
        this.tokenCacheService = tokenCacheService;
    }

    public override void Configure()
    {
        this.Options(b => b.WithTags("SuUser"));
        this.Post("/user/token-cache");
        this.AllowAnonymous();
    }

    public override async Task HandleAsync(SetTokenCacheCommand req, CancellationToken ct)
    {
        var expiry = DateTimeOffset.UtcNow.AddSeconds(30);

        var tokenObj = new { access_token = req.AccessToken, refresh_token = req.RefreshToken };
        var tokenJson = JsonSerializer.Serialize(tokenObj);

        await this.tokenCacheService.SetTokenAsync(req.UserName, tokenJson, expiry, ct);

        await this.SendOkAsync(ct);
    }
}