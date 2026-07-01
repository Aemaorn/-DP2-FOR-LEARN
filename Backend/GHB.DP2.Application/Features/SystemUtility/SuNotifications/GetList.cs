namespace GHB.DP2.Application.Features.SystemUtility.SuNotifications;

using System.IdentityModel.Tokens.Jwt;
using Codehard.Infrastructure.EntityFramework;
using FluentValidation;
using GHB.DP2.Application.Common;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class GetListNotificationRequest
{
    [FromClaim(JwtRegisteredClaimNames.Sub)]
    public Guid UserId { get; init; }

    public int PageNumber { get; init; }

    public int PageSize { get; init; }

    public bool? IsMarkRead { get; init; }

    public string? Keywords { get; init; }

    public class Validation : Validator<GetListNotificationRequest>
    {
        public Validation()
        {
            this.RuleFor(x => x.PageNumber)
                .GreaterThanOrEqualTo(1)
                .WithMessage("Page must be greater than or equal to 1.");

            this.RuleFor(x => x.PageSize)
                .GreaterThan(0)
                .WithMessage("Page size must be greater than 0.");
        }
    }
}

public record NotificationInfo(
    Guid Id,
    string User,
    string Title,
    string Message,
    DateTimeOffset CreatedAt,
    NotificationProgram Program,
    string LinkUrl,
    bool IsRead);

public record GetListNotificationResponse(
    int Count,
    PaginatedQueryResult<NotificationInfo> Notifications);

public class GetListNotificationEndpoint
    : EndpointBase<GetListNotificationRequest, Results<Ok<GetListNotificationResponse>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public GetListNotificationEndpoint(ILogger<GetListNotificationEndpoint> logger, Dp2DbContext dbContext)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags(nameof(SuNotifications))
             .WithName("GetNotificationsList")
             .WithSummary("Get a list of notifications for the user.")
             .WithDescription("This endpoint retrieves a paginated list of notifications for the specified user."));
        this.Get("/su/notifications");
    }

    protected override async ValueTask<Results<Ok<GetListNotificationResponse>, NotFound<string>>> HandleRequestAsync(GetListNotificationRequest req, CancellationToken ct)
    {
        var userId = UserId.From(req.UserId);

        var anyUser =
            await this.dbContext.SuUsers
                      .AnyAsync(
                          u =>
                              u.Id == userId,
                          cancellationToken: ct);

        if (!anyUser)
        {
            return TypedResults.NotFound($"User with ID {req.UserId} not found.");
        }

        var query =
            this.dbContext.SuNotifications
                .Include(n => n.User)
                .Where(n => n.UserId == userId)
                .WhereIfTrue(
                    !req.Keywords.IsNullOrEmpty(),
                    n => n.Title.Contains(req.Keywords!) || n.Message.Contains(req.Keywords!))
                .OrderByDescending(n => n.CreatedAt)
                .AsQueryable();

        var totalCount =
            await query
                  .Where(n => n.ReadAt == null)
                  .CountAsync(ct);

        query = query.WhereIfTrue(
            req.IsMarkRead.HasValue,
            n =>
                req.IsMarkRead.Value ? n.ReadAt != null : n.ReadAt == null);

        var paginated =
            await PaginatedList<SuNotification>
                .CreateAsync(
                    query,
                    req.PageNumber,
                    req.PageSize,
                    ct);

        var result =
            paginated.ToResult(n =>
                new NotificationInfo(
                    n.Id.Value,
                    n.User.FullName,
                    n.Title,
                    n.Message,
                    n.CreatedAt,
                    n.Program,
                    n.LinkUrl ?? string.Empty,
                    !n.ReadAt.IsNull()));

        return TypedResults.Ok(
            new GetListNotificationResponse(
                totalCount,
                result));
    }
}