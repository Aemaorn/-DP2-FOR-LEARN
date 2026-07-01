namespace GHB.DP2.Application.Features.ContractAmendment.CertificateRequisition;

using System.IdentityModel.Tokens.Jwt;
using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Application.Common;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.ContractAmendment.CertificateRequisition.Abstract;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractAmendment.CamCertificateRequisition;
using GHB.DP2.Domain.Procurement.PJp005;
using GHB.DP2.Domain.Procurement.PPrincipleApproval;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetCertificateRequisitionListRequest(
    int PageNumber,
    int PageSize,
    string? Keyword,
    CamCertificateRequisitionStatus? Status,
    [property: FromClaim(JwtRegisteredClaimNames.Sub)] Guid UserId,
    WorkProcess WorkProcess);

public record GetCertificateRequisitionListResponse(
    CamCertificateRequisitionId Id,
    ContractDraftVendorId? ContractDraftVendorId,
    string CertificateNo,
    string PoNumber,
    string ContractName,
    decimal Budget,
    DateTimeOffset? ContractSignedDate,
    string VendorCode,
    string VendorName,
    CamCertificateRequisitionStatus Status);

public record GetStatusCount(
    int All,
    int Draft,
    int WaitingForCommitteeApproval,
    int Approved,
    int Rejected,
    int Edit,
    int Cancelled);

public record GetCertificateRequisitionListResult(
    GetStatusCount StatusCount,
    PaginatedQueryResult<GetCertificateRequisitionListResponse> Data
);

public class GetCertificateRequisitionList
    : CertificateRequisitionEndpointBase<
        GetCertificateRequisitionListRequest,
        Ok<GetCertificateRequisitionListResult>>
{
    private readonly Dp2DbContext dbContext;

    public GetCertificateRequisitionList(
        Dp2DbContext dbContext,
        ILogger<GetCertificateRequisitionList> logger)
        : base(dbContext, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("ContractAmendment/CertificateRequisition")
             .WithName("GetCertificateRequisitionList")
             .Produces<Ok>());
        this.Get("certificate-requisition");
    }

    protected override async ValueTask<Ok<GetCertificateRequisitionListResult>> HandleRequestAsync(
        GetCertificateRequisitionListRequest req,
        CancellationToken ct)
    {
        var userId = UserId.From(req.UserId);

        var keyword = $"%{req.Keyword}%";

        var query =
            this.dbContext.CamCertificateRequisitions
                .Include(c => c.ContractDraftVendor!)
                .ThenInclude(v => v.Vendor)
                .ThenInclude(info => info.VendorInfo)
                .WhereIfTrue(
                    !string.IsNullOrWhiteSpace(req.Keyword),
                    c =>
                        (c.ContractDraftVendor != null && EF.Functions.ILike(c.ContractDraftVendor.PoNumber, keyword)) ||
                        (c.ContractDraftVendor != null && EF.Functions.ILike(c.ContractDraftVendor.ContractName, keyword)) ||
                        (c.ContractDraftVendor != null && EF.Functions.ILike(c.ContractDraftVendor.Vendor.VendorInfo.SapVendorNumber, keyword)) ||
                        (c.ContractDraftVendor != null && EF.Functions.ILike(c.ContractDraftVendor.Vendor.VendorInfo.EstablishmentName, keyword)) ||
                        (c.ContractDraftVendor == null && c.ContractName != null && EF.Functions.ILike(c.ContractName, keyword)) ||
                        (c.ContractDraftVendor == null && c.EntrepreneurName != null && EF.Functions.ILike(c.EntrepreneurName, keyword)) ||
                        (c.ContractDraftVendor == null && c.PoNumber != null && EF.Functions.ILike(c.PoNumber, keyword)) ||
                        EF.Functions.ILike((string)c.CertificateNo, keyword));

        if (req.WorkProcess == WorkProcess.InProcess)
        {
            query = query.Where(x =>
                (x.ContractDraftVendor != null && x.ContractDraftVendor.ContractDraft.Procurement.Jp005.Any(j =>
                     j.Committees.Any(c => c.SuUserId == userId && c.GroupType == PJp005CommitteeGroupType.InspectionCommittee))) ||
                (x.ContractDraftVendor != null && x.ContractDraftVendor.ContractDraft.Procurement.PrincipleApprovals.Any(j =>
                     j.PrincipleApprovalCommittees.Any(c => c.SuUserId == userId && c.GroupType == CommitteeGroupType.AcceptanceCommittee))) ||
                 x.Acceptors.Any(ac => ac.UserId == userId));
        }

        query = query.OrderByDescending(o => o.AuditInfo.LastModifiedAt != null ? o.AuditInfo.LastModifiedAt : o.AuditInfo.CreatedAt);

        var paginatedQuery =
            query.WhereIfTrue(
                !req.Status.IsNull(),
                p => p.Status == req.Status);

        var paginated =
            await PaginatedList<CamCertificateRequisition>
                .CreateAsync(
                    paginatedQuery,
                    req.PageNumber,
                    req.PageSize,
                    ct);

        var result = await query.ToListAsync(ct);

        var statusCount =
            new GetStatusCount(
                result.Count,
                result.Count(s => s.Status == CamCertificateRequisitionStatus.Draft),
                result.Count(s => s.Status == CamCertificateRequisitionStatus.WaitingForCommitteeApproval),
                result.Count(s => s.Status == CamCertificateRequisitionStatus.Approved),
                result.Count(s => s.Status == CamCertificateRequisitionStatus.Rejected),
                result.Count(s => s.Status == CamCertificateRequisitionStatus.Edit),
                result.Count(s => s.Status == CamCertificateRequisitionStatus.Cancelled));

        var data =
            paginated.ToResult(
                c =>
                    new GetCertificateRequisitionListResponse(
                        c.Id,
                        c.ContractDraftVendorId,
                        c.CertificateNo.Value,
                        c.ContractDraftVendor?.PoNumber ?? c.PoNumber ?? string.Empty,
                        c.ContractDraftVendor?.ContractName ?? c.ContractName ?? string.Empty,
                        c.ContractDraftVendor?.Budget ?? c.Budget ?? 0m,
                        c.ContractDraftVendor?.ContractSignedDate ?? c.ContractSignedDate,
                        c.ContractDraftVendor?.Vendor.VendorInfo.SapVendorNumber ?? string.Empty,
                        c.ContractDraftVendor?.Vendor.VendorInfo.EstablishmentName ?? c.EntrepreneurName ?? string.Empty,
                        c.Status));

        return
            TypedResults.Ok(
                new GetCertificateRequisitionListResult(
                    statusCount,
                    data));
    }
}