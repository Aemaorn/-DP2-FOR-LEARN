namespace GHB.DP2.Application.Features.Report.ContractCompletionByQuarter;

using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Features.Report.AuditAndRevenue;
using GHB.DP2.Application.Features.Report.ContractCompletionByQuarter.Abstract;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetListContractDraftVendorCompletionResponse(
    Guid Id,
    string ContractTypeCode,
    string ContractTypeName,
    string ContractNumber,
    string ContractName,
    decimal Budget,
    string EntrepreneurName,
    DateTimeOffset? ContractSignedDate
);

public record GetListContractDraftVendorCompletionRequest(
    DateTimeOffset? ContractSignedStartDate,
    DateTimeOffset? ContractSignedEndDate);

public class GetListContractDraftVendorCompletionEndpoint : ContractCompletionByQuarterEndpoint<GetListContractDraftVendorCompletionRequest, Ok<IEnumerable<GetListContractDraftVendorCompletionResponse>>>
{
    private readonly Dp2DbContext dbContext;

    public GetListContractDraftVendorCompletionEndpoint(ILogger<GetListContractDraftVendorCompletionEndpoint> logger, Dp2DbContext dbContext, IFileServiceClient fileServiceClient)
        : base(logger, dbContext, fileServiceClient)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Get("report/contract-completion-by-quarter/contract-draft-vendor-completion");
        this.Description(b => b
                              .WithTags("Report/ContractCompletionByQuarter")
                              .WithName("GetListContractDraftVendorCompletion")
                              .AllowAnonymous()
                              .Produces<IEnumerable<GetListContractDraftVendorOver1MResponse>>(StatusCodes.Status200OK));
    }

    protected override async ValueTask<Ok<IEnumerable<GetListContractDraftVendorCompletionResponse>>>
        HandleRequestAsync(
            GetListContractDraftVendorCompletionRequest req,
            CancellationToken ct)
    {
        var usedContractIds =
            await this.dbContext.RpContractCompletionByQuarterDetails
                      .Where(r => !r.RpContractCompletionByQuarter.IsDeleted)
                      .Select(d => d.CaContractDraftVendor.Id)
                      .ToListAsync(ct);

        var contracts =
            await this.dbContext.CaContractDraftVendors
                      .Include(c => c.ContractType)
                      .Include(c => c.Vendor)
                      .ThenInclude(v => v.VendorInfo)
                      .Where(c =>
                          !usedContractIds.Contains(c.Id) &&
                          c.Status == ContractDraftVendorStatus.Approved)
                      .WhereIfTrue(
                          req.ContractSignedEndDate.HasValue,
                          c => req.ContractSignedEndDate.Value >= c.ContractSignedDate)
                      .Select(c => new GetListContractDraftVendorCompletionResponse(
                          c.Id.Value,
                          c.ContractTypeCode.Value.ToString(),
                          c.ContractType != null ? c.ContractType.Label : string.Empty,
                          c.ContractNumber,
                          c.ContractName,
                          c.Budget,
                          c.Vendor.VendorInfo.EstablishmentName,
                          c.ContractSignedDate))
                      .ToListAsync(ct);

        return TypedResults.Ok(contracts.AsEnumerable());
    }
}