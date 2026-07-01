namespace GHB.DP2.Application.Features.Watchlist;

using System.Text;
using FluentValidation;
using GHB.DP2.Domain.Common;
using GHB.DP2.Infrastructure.Services.Watchlist;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record SearchWatchlistRequest(
    bool IsJuristic,
    string FirstName,
    string LastName,
    string? IdCardNumber)
{
    public class Validator : Validator<SearchWatchlistRequest>
    {
        public Validator()
        {
            this.RuleFor(x => x.FirstName)
                .NotEmpty()
                .WithMessage("กรุณาระบุ ชื่อ");
        }
    }
}

public record SearchWatchlistResult(
    QualificationResult Result,
    string Remark);

public class SearchWatchlistEndpoint : EndpointBase<SearchWatchlistRequest, Results<Ok<SearchWatchlistResult>, ProblemHttpResult>>
{
    private readonly IWatchlistService watchlistService;

    public SearchWatchlistEndpoint(
        ILogger<SearchWatchlistEndpoint> logger,
        IWatchlistService watchlistService)
        : base(logger)
    {
        this.watchlistService = watchlistService;
    }

    public override void Configure()
    {
        this.Get("watchlist/search");
        this.Description(builder =>
            builder
                .WithTags("Watchlist")
                .AllowAnonymous()
                .Produces<SearchWatchlistRequest>()
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status500InternalServerError));
    }

    protected override async ValueTask<Results<Ok<SearchWatchlistResult>, ProblemHttpResult>>
        HandleRequestAsync(
            SearchWatchlistRequest req,
            CancellationToken ct)
    {
        try
        {
            var results =
                await this.watchlistService.SearchWatchlistAsync(
                    req.IsJuristic,
                    req.FirstName,
                    req.LastName,
                    req.IdCardNumber ?? string.Empty,
                    ct);

            var watchlistInfos = results as WatchlistInfo[] ?? [.. results];
            var watchlist = watchlistInfos.FirstOrDefault();

            if (watchlist is null)
            {
                return TypedResults.Ok(
                    new SearchWatchlistResult(
                        QualificationResult.Pass,
                        "ไม่พบข้อมูล Watchlist"));
            }

            var remarkString = new StringBuilder();

            foreach (var watchlistInfo in watchlist.Details ?? [])
            {
                remarkString.AppendLine($"{watchlistInfo.Reason}, ");
            }

            var response =
                new SearchWatchlistResult(
                    QualificationResult.Fail,
                    remarkString.ToString());

            return TypedResults.Ok(response);
        }
        catch (TimeoutException)
        {
            return TypedResults.Ok(
                new SearchWatchlistResult(
                    QualificationResult.UnKnow,
                    "ตรวจสอบไม่สำเร็จ"));
        }
        catch (WatchlistException ex)
        {
            this.Logger.LogError(ex, "Error occurred while searching watchlist");

            return TypedResults.Ok(
                new SearchWatchlistResult(
                    QualificationResult.UnKnow,
                    ex.ApiMessage ?? "ตรวจสอบไม่สำเร็จ"));
        }
    }
}