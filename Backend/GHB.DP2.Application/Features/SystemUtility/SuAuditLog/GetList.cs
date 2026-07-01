namespace GHB.DP2.Application.Features.SystemUtility.SuAuditLog;

using Codehard.Infrastructure.EntityFramework;
using FluentValidation;
using GHB.DP2.Application.Common;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Services;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetListAuditLogRequest(
    int PageNumber,
    int PageSize,
    string? Keyword,
    DateTimeOffset? StartDate,
    DateTimeOffset? EndDate)
{
    public class Validation : Validator<GetListAuditLogRequest>
    {
        public Validation()
        {
            this.RuleFor(x => x.PageNumber)
                .GreaterThan(0)
                .WithMessage("Page number must be greater than 0.");

            this.RuleFor(x => x.PageSize)
                .GreaterThan(0)
                .WithMessage("Page size must be greater than 0.")
                .LessThanOrEqualTo(100)
                .WithMessage("Page size must not exceed 100.");
        }
    }
}

public record AuditLogInfo(
    DateTimeOffset TimeStamp,
    string? UserName,
    string ProgramName,
    string Message,
    string? IpAddress,
    Guid? UserId);

public class GetAuditLogList
    : SecureEndpointBase<GetListAuditLogRequest, Ok<PaginatedQueryResult<AuditLogInfo>>>
{
    private readonly Dp2DbContext dbContext;

    public GetAuditLogList(
        Dp2DbContext dbContext,
        IPermissionValidationService permissionValidationService,
        ILogger<GetAuditLogList> logger)
        : base(permissionValidationService, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags(nameof(SuAuditLog)));
        this.Get("/st/st008");
    }

    protected override async ValueTask<Ok<PaginatedQueryResult<AuditLogInfo>>> HandleRequestAsync(
        GetListAuditLogRequest req,
        CancellationToken ct)
    {
        var query =
            this.dbContext.SuAuditLogs
                .WhereIfTrue(
                    !req.Keyword.IsNullOrEmpty(),
                    a =>
                        a.Message.Contains(req.Keyword!) ||
                        a.Program.Contains(req.Keyword!) ||
                        a.User!.Contains(req.Keyword!))
                .WhereIfTrue(
                    req is { StartDate: not null },
                    a =>
                        a.CreatedAt >= req.StartDate)
                .WhereIfTrue(
                    req is { EndDate: not null },
                    a => a.CreatedAt <= req.EndDate)
                .AsNoTracking()
                .OrderByDescending(o => o.CreatedAt)
                .AsQueryable();

        var paginated =
            await PaginatedList<Domain.SystemUtility.SuAuditLog>
                .CreateAsync(
                    query,
                    req.PageNumber,
                    req.PageSize,
                    ct);

        var result =
            paginated.ToResult(a => new AuditLogInfo(
                a.CreatedAt,
                a.User,
                a.Program,
                a.Message,
                a.IpAddress,
                a.UserId));

        return TypedResults.Ok(result);
    }
}