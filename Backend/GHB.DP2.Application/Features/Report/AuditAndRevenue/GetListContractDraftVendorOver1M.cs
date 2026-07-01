namespace GHB.DP2.Application.Features.Report.AuditAndRevenue;

using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Report.AuditAndRevenue.Abstract;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetListContractDraftVendorOver1MResponse(
    Guid CaContractDraftVendorId,
    string ContractTypeCode,
    string ContractTypeName,
    string ContractNumber,
    string ContractName,
    decimal Budget,
    string EntrepreneurName,
    DateTimeOffset? ContractSignedDate
);

public record GetListContractDraftVendorOver1MRequest(
    DateTimeOffset? ContractSignedStartDate,
    DateTimeOffset? ContractSignedEndDate);

public class GetListContractDraftVendorOver1MEndpoint : AuditAndRevenueEndpoint<GetListContractDraftVendorOver1MRequest, Ok<IEnumerable<GetListContractDraftVendorOver1MResponse>>>
{
    private readonly Dp2DbContext dbContext;

    public GetListContractDraftVendorOver1MEndpoint(ILogger<GetListContractDraftVendorOver1MEndpoint> logger, Dp2DbContext dbContext, IFileServiceClient fileServiceClient, IOperationService operationService)
        : base(logger, dbContext, fileServiceClient, operationService)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Get("report/audit-revenue/contract-draft-vendor-over-1m");
        this.Description(b => b
                              .WithTags("Report/AuditAndRevenue")
                              .WithName("GetListContractDraftVendorOver1M")
                              .WithSummary("ดึงสัญญา DraftVendor ที่งบประมาณมากกว่า 1 ล้านบาท และยังไม่ถูกนำไปสร้าง AuditAndRevenue")
                              .AllowAnonymous()
                              .Produces<IEnumerable<GetListContractDraftVendorOver1MResponse>>(StatusCodes.Status200OK));
    }

    protected override async ValueTask<Ok<IEnumerable<GetListContractDraftVendorOver1MResponse>>> HandleRequestAsync(GetListContractDraftVendorOver1MRequest req, CancellationToken ct)
    {
        var usedContractIds = await this.dbContext.RpAuditAndRevenueDetails
                                        .Select(d => d.CaContractDraftVendor.Id)
                                        .ToListAsync(ct);

        var contracts = await this.dbContext.CaContractDraftVendors
                                  .Include(c => c.ContractType)
                                  .Include(c => c.Vendor).ThenInclude(v => v.VendorInfo)
                                  .Where(c => c.Budget > 1_000_000m && !usedContractIds.Contains(c.Id))
                                  .Where(c => c.Status == ContractDraftVendorStatus.Approved)
                                  .WhereIfTrue(
                                      req.ContractSignedStartDate.HasValue && req.ContractSignedEndDate.HasValue,
                                      c => c.ContractSignedDate >= req.ContractSignedStartDate && c.ContractSignedDate <= req.ContractSignedEndDate)
                                  .Select(c => new GetListContractDraftVendorOver1MResponse(
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