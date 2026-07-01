namespace GHB.DP2.Application.Features.Report.ContractCompletionByQuarter;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Codehard.FileService.Client.Abstractions;
using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Application.Common;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.Report.ContractCompletionByQuarter.Abstract;
using GHB.DP2.Domain.Report.RpContractCompletionByQuarter;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetListContractCompletionByQuarterRequest(
    int PageNumber,
    int PageSize,
    string? Keyword,
    int? Year,
    int? Quarter,
    RpContractCompletionByQuarterStatus? Status
);

public record GetListContractCompletionByQuarterResponse(
    Guid Id,
    string DocumentNumber,
    int Year,
    int Quarter,
    DateTimeOffset DocumentDate,
    DateTimeOffset SignStartDate,
    DateTimeOffset SignEndDate,
    RpContractCompletionByQuarterStatus Status,
    int DetailCount,
    decimal TotalAmount
);

public record GetContractCompletionByQuarterStatusCount(
    int All,
    int Draft,
    int Edit,
    int WaitingApproval,
    int Approved,
    int Rejected
);

public record GetContractCompletionByQuarterResult(
    GetContractCompletionByQuarterStatusCount StatusCount,
    PaginatedQueryResult<GetListContractCompletionByQuarterResponse> Data
);

public class GetContractCompletionByQuarterListEndpoint : ContractCompletionByQuarterEndpoint<GetListContractCompletionByQuarterRequest, Ok<GetContractCompletionByQuarterResult>>
{
    private readonly Dp2DbContext dbContext;

    public GetContractCompletionByQuarterListEndpoint(
        ILogger<GetContractCompletionByQuarterListEndpoint> logger,
        Dp2DbContext dbContext,
        IFileServiceClient fileServiceClient)
        : base(logger, dbContext, fileServiceClient)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Get("report/contract-completion-by-quarter");
        this.Description(b => b
            .WithTags("Report/ContractCompletionByQuarter")
            .WithName("GetContractCompletionByQuarterList")
            .AllowAnonymous()
            .Produces<GetContractCompletionByQuarterResult>(StatusCodes.Status200OK));
    }

    protected override async ValueTask<Ok<GetContractCompletionByQuarterResult>> HandleRequestAsync(GetListContractCompletionByQuarterRequest req, CancellationToken ct)
    {
        var query = this.dbContext.RpContractCompletionByQuarters
                        .Include(x => x.Details)
                        .ThenInclude(d => d.CaContractDraftVendor)
                        .WhereIfTrue(
                            !string.IsNullOrWhiteSpace(req.Keyword),
                            x => EF.Functions.ILike(x.DocumentNumber, $"%{req.Keyword}%"))
                        .WhereIfTrue(
                            req.Year.HasValue,
                            x => x.Year == req.Year)
                        .WhereIfTrue(
                            req.Quarter.HasValue,
                            x => x.Quarter == req.Quarter);

        var paginatedQuery = query.WhereIfTrue(req.Status.HasValue, x => x.Status == req.Status);

        var paginated = await PaginatedList<RpContractCompletionByQuarter>.CreateAsync(
              paginatedQuery,
              req.PageNumber,
              req.PageSize,
              ct);

        var result = await query.ToListAsync(ct);

        var statusCount = new GetContractCompletionByQuarterStatusCount(
            result.Count,
            result.Count(s => s.Status == RpContractCompletionByQuarterStatus.Draft),
            result.Count(s => s.Status == RpContractCompletionByQuarterStatus.Edit),
            result.Count(s => s.Status == RpContractCompletionByQuarterStatus.WaitingApproval),
            result.Count(s => s.Status == RpContractCompletionByQuarterStatus.Approved),
            result.Count(s => s.Status == RpContractCompletionByQuarterStatus.Rejected));

        var data = paginated.ToResult(c =>
            new GetListContractCompletionByQuarterResponse(
                c.Id.Value,
                c.DocumentNumber,
                c.Year,
                c.Quarter,
                c.DocumentDate,
                c.SignStartDate,
                c.SignEndDate,
                c.Status,
                c.Details?.Count ?? 0,
                c.Details?.Sum(d => d.CaContractDraftVendor != null ? d.CaContractDraftVendor.Budget : 0m) ?? 0m));

        return TypedResults.Ok(new GetContractCompletionByQuarterResult(statusCount, data));
    }
}