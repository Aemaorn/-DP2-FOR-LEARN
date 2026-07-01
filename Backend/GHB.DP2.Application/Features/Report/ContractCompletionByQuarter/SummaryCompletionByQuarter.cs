namespace GHB.DP2.Application.Features.Report.ContractCompletionByQuarter;

using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Features.Report.ContractCompletionByQuarter.Abstract;
using GHB.DP2.Domain.Report.RpContractCompletionByQuarter;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetSummaryCompletionByQuarterResponse(
    int Quarter,
    string ContractTypeCode,
    string ContractTypeName,
    int ContractCount,
    decimal PercentComplete);

public record GetSummaryCompletionByQuarterRequest(Guid Id);

public class GetSummaryCompletionByQuarterEndpoint
    : ContractCompletionByQuarterEndpoint<GetSummaryCompletionByQuarterRequest, Ok<IEnumerable<GetSummaryCompletionByQuarterResponse>>>
{
    private readonly Dp2DbContext dbContext;

    public GetSummaryCompletionByQuarterEndpoint(
        ILogger<GetSummaryCompletionByQuarterEndpoint> logger,
        Dp2DbContext dbContext,
        IFileServiceClient fileServiceClient)
        : base(logger, dbContext, fileServiceClient)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Get("report/contract-completion-by-quarter/summary-completion-by-quarter/{id:guid}");
        this.Description(b => b
            .WithTags("Report/ContractCompletionByQuarter")
            .WithName("GetSummaryCompletionByQuarter")
            .AllowAnonymous()
            .Produces<IEnumerable<GetSummaryCompletionByQuarterResponse>>(StatusCodes.Status200OK));
    }

    protected override async ValueTask<Ok<IEnumerable<GetSummaryCompletionByQuarterResponse>>> HandleRequestAsync(
        GetSummaryCompletionByQuarterRequest req, CancellationToken ct)
    {
        var currentDoc = await this.dbContext.RpContractCompletionByQuarters
            .Where(x => x.Id == RpContractCompletionByQuarterId.From(req.Id))
            .Select(x => new { x.Year, x.Quarter })
            .FirstOrDefaultAsync(ct);

        if (currentDoc is null)
        {
            return TypedResults.Ok(Enumerable.Empty<GetSummaryCompletionByQuarterResponse>());
        }

        var rawDetails = await this.dbContext.RpContractCompletionByQuarterDetails
            .AsNoTracking()
            .Where(d =>
                d.RpContractCompletionByQuarter.Year == currentDoc.Year &&
                d.RpContractCompletionByQuarter.Quarter <= currentDoc.Quarter &&
                !d.RpContractCompletionByQuarter.IsDeleted)
            .Select(d => new
            {
                Quarter = d.RpContractCompletionByQuarter.Quarter,
                ContractTypeCode = d.CaContractDraftVendor.ContractTypeCode.Value.ToString(),
                ContractTypeName = d.CaContractDraftVendor.ContractType != null
                    ? d.CaContractDraftVendor.ContractType.Label
                    : string.Empty,
            })
            .ToListAsync(ct);

        var result = rawDetails
            .Select(d =>
            {
                var code = d.ContractTypeCode;
                var name = d.ContractTypeName;
                if (code.Equals(ContractRentalTypeConstant.Rent, StringComparison.Ordinal))
                {
                    code = ContractTypeConstant.Rent;
                    name = "สัญญาเช่า";
                }

                return (d.Quarter, Code: code, ContractTypeName: name);
            })
            .GroupBy(d => d.Quarter)
            .SelectMany(quarterGroup =>
            {
                var total = quarterGroup.Count();
                var typeRows = quarterGroup
                    .GroupBy(d => d.Code)
                    .Select(g => new GetSummaryCompletionByQuarterResponse(
                        Quarter: quarterGroup.Key,
                        ContractTypeCode: g.Key,
                        ContractTypeName: string.IsNullOrWhiteSpace(g.First().ContractTypeName) ? g.Key : g.First().ContractTypeName,
                        ContractCount: g.Count(),
                        PercentComplete: total > 0 ? Math.Round((decimal)g.Count() * 100m / total, 2) : 0m))
                    .OrderByDescending(x => x.ContractCount)
                    .ToList();

                typeRows.Add(new GetSummaryCompletionByQuarterResponse(
                    Quarter: quarterGroup.Key,
                    ContractTypeCode: "ALL",
                    ContractTypeName: "รวมสัญญาทั้งสิ้น",
                    ContractCount: total,
                    PercentComplete: total > 0 ? 100m : 0m));

                return typeRows;
            })
            .OrderBy(x => x.Quarter)
            .ToList();

        return TypedResults.Ok(result.AsEnumerable());
    }
}
