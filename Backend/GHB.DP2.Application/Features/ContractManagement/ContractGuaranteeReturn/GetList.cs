namespace GHB.DP2.Application.Features.ContractManagement.ContractGuaranteeReturn;

using System.IdentityModel.Tokens.Jwt;
using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Application.Common;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractManagement.CmContractGuaranteeReturn; // added
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetContractGuaranteeReturnListRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    int PageNumber,
    int PageSize,
    string? Keyword,
    string? DepartmentCode,
    DateTimeOffset? SignedDate,
    string? ContractTypeCode,
    WorkProcess WorkProcess = WorkProcess.InProcess);

public record GetContractGuaranteeReturnListItemResponse(
    ContractDraftVendorId ContractDraftVendorId,
    string ContractNumber,
    string PoNumber,
    DateTimeOffset? ContractSignedDate,
    string EntrepreneurCode,
    string EntrepreneurName,
    string ContractName,
    decimal Budget,
    ParameterCode? ContractTypeCode,
    string? ContractTypeLabel,
    CmContractGuaranteeReturnId? ContractGuaranteeReturnId,
    CmContractGuaranteeReturnStatus? ContractGuaranteeReturnStatus,
    string Department
);

public class GetContractGuaranteeReturnListEndpoint : EndpointBase<GetContractGuaranteeReturnListRequest, Ok<PaginatedQueryResult<GetContractGuaranteeReturnListItemResponse>>>
{
    private readonly Dp2DbContext dbContext;

    public GetContractGuaranteeReturnListEndpoint(
        Dp2DbContext dbContext,
        ILogger<GetContractGuaranteeReturnListEndpoint> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("ContractManagement/ContractGuaranteeReturn")
             .WithName("GetContractGuaranteeReturnContracts")
             .Produces<PaginatedQueryResult<GetContractGuaranteeReturnListItemResponse>>());

        // Route pattern aligned under contract management module
        this.Get("contract/contract-guarantee-return");
    }

    protected override async ValueTask<Ok<PaginatedQueryResult<GetContractGuaranteeReturnListItemResponse>>> HandleRequestAsync(GetContractGuaranteeReturnListRequest req, CancellationToken ct)
    {
        var keyword = $"%{req.Keyword}%";

        var userId = UserId.From(req.UserId);

        var query = this.dbContext.CmContractGuaranteeReturns
                        .Include(g => g.CaContractDraftVendor)
                        .ThenInclude(cv => cv.Vendor)
                        .Include(g => g.CaContractDraftVendor)
                        .ThenInclude(cv => cv.ContractDraft)
                        .ThenInclude(cd => cd.Procurement)
                        .ThenInclude(p => p.Department)
                        .Include(a => a.Acceptors)
                        .ThenInclude(a => a.Delegatee)
                        .Include(a => a.Assignees)
                        .ThenInclude(a => a.Delegatee)
                        .WhereIfTrue(!string.IsNullOrWhiteSpace(req.Keyword), cv =>
                            EF.Functions.ILike(cv.CaContractDraftVendor.PoNumber, keyword) ||
                            EF.Functions.ILike(cv.CaContractDraftVendor.ContractName, keyword) ||
                            EF.Functions.ILike(cv.CaContractDraftVendor.ContractNumber, keyword) ||
                            EF.Functions.ILike(cv.CaContractDraftVendor.Vendor.VendorInfo.SapVendorNumber, keyword) ||
                            EF.Functions.ILike(cv.CaContractDraftVendor.Vendor.VendorInfo.EstablishmentName, keyword))
                        .WhereIfTrue(
                            !string.IsNullOrWhiteSpace(req.DepartmentCode),
                            x =>
                                x.CaContractDraftVendor.ContractDraft
                                 .Procurement.DepartmentId == BusinessUnitId.From(req.DepartmentCode!))
                        .WhereIfTrue(req.SignedDate != null, cv => cv.CaContractDraftVendor.ContractSignedDate.Value.Date == req.SignedDate!.Value.Date)
                        .WhereIfTrue(req.ContractTypeCode != null, cv => req.ContractTypeCode != null && cv.CaContractDraftVendor.ContractTypeCode == ParameterCode.From(req.ContractTypeCode));

        if (req.WorkProcess == WorkProcess.InProcess)
        {
            query = query.Where(x =>
                ((x.Status == CmContractGuaranteeReturnStatus.Draft ||
                  x.Status == CmContractGuaranteeReturnStatus.Rejected ||
                  x.Status == CmContractGuaranteeReturnStatus.WaitingCommitteeApproval) &&
                 x.Acceptors.Any(j =>
                     j.Type == AcceptorType.AcceptanceCommittee &&
                     j.UserId == userId)) ||
                (x.Status == CmContractGuaranteeReturnStatus.WaitingAssigned &&
                 x.Assignees.Any(j =>
                     j.UserId == userId ||
                     (j.Delegatee != null &&
                      j.Delegatee.SuUserId == userId))) ||
                (x.Status == CmContractGuaranteeReturnStatus.Assigned &&
                 x.Assignees.Any(j =>
                     j.UserId == userId ||
                     (j.Delegatee != null &&
                      j.Delegatee.SuUserId == userId))) ||
                (x.Status == CmContractGuaranteeReturnStatus.WaitingAcceptance &&
                 x.Acceptors.Any(a =>
                     a.Type == AcceptorType.Approver &&
                     (a.IsCurrent || a.Status == AcceptorStatus.Pending) &&
                     (a.UserId == userId ||
                      (a.Delegatee != null &&
                       a.Delegatee.SuUserId == userId)))) ||
                (x.Status == CmContractGuaranteeReturnStatus.WaitingAccountingApproval &&
                 x.Acceptors.Any(a =>
                     a.Type == AcceptorType.Accounting &&
                     (a.IsCurrent || a.Status == AcceptorStatus.Pending) &&
                     (a.UserId == userId ||
                      (a.Delegatee != null &&
                       a.Delegatee.SuUserId == userId)))) ||
                (x.Status == CmContractGuaranteeReturnStatus.WaitingDisbursementDate &&
                 x.Acceptors.Any(a =>
                     a.Type == AcceptorType.Accounting &&
                     (a.UserId == userId ||
                      (a.Delegatee != null &&
                       a.Delegatee.SuUserId == userId)))));
        }
        else if (req.WorkProcess == WorkProcess.Related)
        {
            query = query.Where(x =>
                (x.Status == CmContractGuaranteeReturnStatus.WaitingCommitteeApproval &&
                 x.Acceptors.Any(j =>
                     j.Type == AcceptorType.AcceptanceCommittee &&
                     !j.IsCurrent &&
                     j.Status != AcceptorStatus.Pending &&
                     j.UserId == userId)) ||
                (x.Status == CmContractGuaranteeReturnStatus.WaitingAcceptance &&
                 x.Acceptors.Any(a =>
                     a.Type == AcceptorType.Approver &&
                     !a.IsCurrent &&
                     a.Status != AcceptorStatus.Pending &&
                     (a.UserId == userId ||
                      (a.Delegatee != null &&
                       a.Delegatee.SuUserId == userId)))) ||
                (x.Status == CmContractGuaranteeReturnStatus.WaitingAccountingApproval &&
                 x.Acceptors.Any(a =>
                     a.Type == AcceptorType.Accounting &&
                     !a.IsCurrent &&
                     (a.Status == AcceptorStatus.Approved || a.Status == AcceptorStatus.Rejected) &&
                     (a.UserId == userId ||
                      (a.Delegatee != null &&
                       a.Delegatee.SuUserId == userId)))));
        }
        else if (req.WorkProcess == WorkProcess.Completed)
        {
            query = query.Where(x =>
                (x.Status == CmContractGuaranteeReturnStatus.Approved &&
                 (x.Acceptors.Any(j =>
                      j.UserId == userId ||
                      (j.Delegatee != null &&
                       j.Delegatee.SuUserId == userId)) ||
                  x.Assignees.Any(j =>
                      j.UserId == userId ||
                      (j.Delegatee != null &&
                       j.Delegatee.SuUserId == userId)))) ||
                (x.Status == CmContractGuaranteeReturnStatus.Paid &&
                 x.Acceptors.Any(a =>
                     (a.Type == AcceptorType.Accounting || a.Type == AcceptorType.AccountingConfirmer) &&
                     (a.UserId == userId ||
                      (a.Delegatee != null &&
                       a.Delegatee.SuUserId == userId)))));
        }

        var orderQuery = query.OrderByDescending(o => o.AuditInfo.LastModifiedAt ?? o.AuditInfo.CreatedAt);

        var paginated = await PaginatedList<CmContractGuaranteeReturn>.CreateAsync(
            orderQuery,
            req.PageNumber,
            req.PageSize,
            ct);

        var result = paginated.ToResult(p =>
        {
            var vendor = p.CaContractDraftVendor.ContractInvitationVendors.PurchaseOrderApprovalContract.Entrepreneur?.SuVendor;

            return new GetContractGuaranteeReturnListItemResponse(
                p.ContractDraftVendorId,
                p.CaContractDraftVendor.ContractNumber,
                p.CaContractDraftVendor.PoNumber,
                p.CaContractDraftVendor.ContractSignedDate,
                vendor != null ? vendor.SapVendorNumber : string.Empty,
                vendor != null ? vendor.EstablishmentName : string.Empty,
                p.CaContractDraftVendor.ContractName,
                p.CaContractDraftVendor.Budget,
                p.CaContractDraftVendor.ContractTypeCode,
                p.CaContractDraftVendor.ContractType?.Label,
                p.Id,
                p.Status,
                p.CaContractDraftVendor.ContractDraft?.Procurement?.Department?.Name ?? string.Empty);
        });

        return TypedResults.Ok(result);
    }
}