namespace GHB.DP2.Application.Features.Report.AuditAndRevenue;

using Codehard.FileService.Client.Abstractions;
using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Application.Common;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Report.AuditAndRevenue.Abstract;
using GHB.DP2.Domain.Report.RpAuditAndRevenue;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetListAuditAndRevenueRequest(
    int PageNumber,
    int PageSize,
    string? Keyword,
    DateTimeOffset? DocumentDate,
    DateTimeOffset? SignStartDate,
    DateTimeOffset? SignEndDate,
    RpAuditAndRevenueStatus? Status,
    WorkProcess WorkProcess = WorkProcess.InProcess
);

public record GetListAuditAndRevenueResponse(
    Guid Id,
    string DocumentNumber,
    DateTimeOffset DocumentDate,
    DateTimeOffset SignStartDate,
    DateTimeOffset SignEndDate,
    DateTimeOffset DeliveryDate,
    RpAuditAndRevenueStatus Status,
    int DetailCount,
    decimal TotalAmount
);

public record GetAuditAndRevenueStatusCount(
    int All,
    int Draft,
    int Edit,
    int WaitingApproval,
    int Approved,
    int Rejected
);

public record GetAuditAndRevenueResult(
    GetAuditAndRevenueStatusCount StatusCount,
    PaginatedQueryResult<GetListAuditAndRevenueResponse> Data
);

public class GetAuditAndRevenueListEndpoint : AuditAndRevenueEndpoint<GetListAuditAndRevenueRequest, Ok<GetAuditAndRevenueResult>>
{
    private readonly Dp2DbContext dbContext;

    public GetAuditAndRevenueListEndpoint(
        ILogger<GetAuditAndRevenueListEndpoint> logger,
        Dp2DbContext dbContext,
        IFileServiceClient fileServiceClient,
        IOperationService operationService)
        : base(logger, dbContext, fileServiceClient, operationService)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Get("report/audit-revenue");
        this.Description(b => b
                              .WithTags("Report/AuditAndRevenue")
                              .WithName("GetAuditAndRevenueList")
                              .AllowAnonymous()
                              .Produces<GetAuditAndRevenueResult>(StatusCodes.Status200OK));
    }

    protected override async ValueTask<Ok<GetAuditAndRevenueResult>> HandleRequestAsync(GetListAuditAndRevenueRequest req, CancellationToken ct)
    {
        var statusMap = new Dictionary<WorkProcess, List<RpAuditAndRevenueStatus>>
        {
            { WorkProcess.InProcess, [RpAuditAndRevenueStatus.Draft, RpAuditAndRevenueStatus.Edit, RpAuditAndRevenueStatus.Rejected] },
            { WorkProcess.Related, [RpAuditAndRevenueStatus.WaitingApproval] },
            { WorkProcess.Completed, [RpAuditAndRevenueStatus.Approved] },
        };

        var workProcessStatus = statusMap.TryGetValue(req.WorkProcess, value: out var value)
            ? value
            : [];

        var query = this.dbContext.RpAuditAndRevenues
                        .Include(x => x.Details)
                        .ThenInclude(d => d.CaContractDraftVendor)
                        .WhereIfTrue(
                            workProcessStatus.Count != 0,
                            x => workProcessStatus.Contains(x.Status))
                        .WhereIfTrue(
                            !string.IsNullOrWhiteSpace(req.Keyword),
                            x => EF.Functions.ILike(x.DocumentNumber, $"%{req.Keyword}%"))
                        .WhereIfTrue(
                            req.DocumentDate.HasValue,
                            x => x.DocumentDate.Date == req.DocumentDate.Value.Date);

        if (req.SignStartDate.HasValue && req.SignEndDate.HasValue)
        {
            var start = req.SignStartDate.Value.UtcDateTime.AddDays(-1).AddTicks(1);
            var end = req.SignEndDate.Value.UtcDateTime.AddDays(1).AddTicks(-1);

            query = query.Where(x => x.SignStartDate >= start && x.SignEndDate <= end);
        }
        else if (req.SignStartDate.HasValue)
        {
            var start = req.SignStartDate.Value.UtcDateTime.AddDays(-1).AddTicks(1);
            query = query.Where(x => x.SignStartDate >= start);
        }
        else if (req.SignEndDate.HasValue)
        {
            var end = req.SignEndDate.Value.UtcDateTime.AddDays(1).AddTicks(-1);
            query = query.Where(x => x.SignEndDate <= end);
        }

        var paginatedQuery = query.WhereIfTrue(req.Status.HasValue, x => x.Status == req.Status);

        var paginated = await PaginatedList<RpAuditAndRevenue>.CreateAsync(
            paginatedQuery,
            req.PageNumber,
            req.PageSize,
            ct);

        var result = await query.ToListAsync(ct);

        var statusCount = new GetAuditAndRevenueStatusCount(
            result.Count,
            result.Count(s => s.Status == RpAuditAndRevenueStatus.Draft),
            result.Count(s => s.Status == RpAuditAndRevenueStatus.Edit),
            result.Count(s => s.Status == RpAuditAndRevenueStatus.WaitingApproval),
            result.Count(s => s.Status == RpAuditAndRevenueStatus.Approved),
            result.Count(s => s.Status == RpAuditAndRevenueStatus.Rejected));

        var data = paginated.ToResult(c =>
            new GetListAuditAndRevenueResponse(
                c.Id.Value,
                c.DocumentNumber,
                c.DocumentDate,
                c.SignStartDate,
                c.SignEndDate,
                c.DeliveryDate,
                c.Status,
                c.Details?.Count ?? 0,
                c.Details?.Sum(d => d.CaContractDraftVendor != null ? d.CaContractDraftVendor.Budget : 0m) ?? 0m));

        return TypedResults.Ok(new GetAuditAndRevenueResult(statusCount, data));
    }
}