namespace GHB.DP2.Application.Features.ContractManagement.ContractDraftVendorEdit;

using System.IdentityModel.Tokens.Jwt;
using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Application.Common;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.ContractManagement.ContractDraftVendorEdit.Dto;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractManagement.CaContractDraftVendorEdit;
using GHB.DP2.Domain.Procurement.PJp005;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetContractDraftVendorEditListRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    int PageNumber,
    int PageSize,
    string? Keyword,
    string? DepartmentCode,
    DateTimeOffset? SignedDate,
    string? ContractTypeCode,
    WorkProcess WorkProcess = WorkProcess.InProcess);

public class GetContractDraftVendorEditListEndpoint
    : EndpointBase<GetContractDraftVendorEditListRequest, Ok<PaginatedQueryResult<ContractDraftVendorEditListItemResponse>>>
{
    private readonly Dp2DbContext dbContext;

    public GetContractDraftVendorEditListEndpoint(
        Dp2DbContext dbContext,
        ILogger<GetContractDraftVendorEditListEndpoint> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("ContractManagement/ContractDraftVendorEdit")
             .WithName("GetContractDraftVendorEditList")
             .Produces<PaginatedQueryResult<ContractDraftVendorEditListItemResponse>>());

        this.Get("contract/contract-draft-vendor-edit");
    }

    protected override async ValueTask<Ok<PaginatedQueryResult<ContractDraftVendorEditListItemResponse>>>
        HandleRequestAsync(GetContractDraftVendorEditListRequest req, CancellationToken ct)
    {
        var keyword = $"%{req.Keyword}%";
        var userId = UserId.From(req.UserId);

        var query = this.dbContext.CaContractDraftVendorEdits
                        .Include(e => e.ContractType)
                        .Include(e => e.Acceptors)
                        .Include(e => e.Assignees)
                        .WhereIfTrue(!string.IsNullOrWhiteSpace(req.Keyword), e =>
                            EF.Functions.ILike(e.PoNumber, keyword) ||
                            EF.Functions.ILike(e.ContractName, keyword) ||
                            EF.Functions.ILike(e.ContractNumber, keyword))
                        .WhereIfTrue(
                            !string.IsNullOrWhiteSpace(req.DepartmentCode),
                            _ => false)
                        .WhereIfTrue(
                            req.SignedDate != null,
                            e => e.ContractSignedDate!.Value.Date == req.SignedDate!.Value.Date)
                        .WhereIfTrue(
                            !string.IsNullOrWhiteSpace(req.ContractTypeCode),
                            e => e.ContractTypeCode == ParameterCode.From(req.ContractTypeCode!));

        // WorkProcess-based filtering
        if (req.WorkProcess == WorkProcess.InProcess)
        {
            query = query.Where(x =>

                ((x.Status == ContractDraftVendorEditStatus.Draft ||
                  x.Status == ContractDraftVendorEditStatus.Editing ||
                  x.Status == ContractDraftVendorEditStatus.Rejected) &&
                 (x.Acceptors.Any(a =>
                     a.Type == AcceptorType.AcceptanceCommittee &&
                     a.UserId == userId) ||
                  x.Assignees.Any(a =>
                     a.Type == AssigneeType.Assignee &&
                     a.UserId == userId) ||
                  this.dbContext.PJp005S
                      .Any(jp005 => jp005.ProcurementId == x.ContractDraftVendor.ContractDraft.ProcurementId
                          && !jp005.IsDeleted
                          && jp005.Committees.Any(committee =>
                              committee.GroupType == PJp005CommitteeGroupType.ProcurementCommittee
                              && committee.SuUserId == userId)) ||
                  this.dbContext.PPurchaseOrderApprovals
                      .Any(poa => poa.ProcurementId == x.ContractDraftVendor.ContractDraft.ProcurementId
                          && !poa.IsDeleted
                          && (poa.Committees.Any(committee =>
                                  committee.SuUserId == userId) ||
                              poa.Assignees.Any(assignee =>
                                  assignee.Type == AssigneeType.Assignee
                                  && assignee.UserId == userId
                                  && !assignee.IsDeleted))))) ||
                (x.Status == ContractDraftVendorEditStatus.WaitingCommitteeApproval &&
                 x.Acceptors.Any(a =>
                     a.Type == AcceptorType.AcceptanceCommittee &&
                     (a.IsCurrent || a.Status == AcceptorStatus.Pending) &&
                     a.UserId == userId)) ||
                (x.Status == ContractDraftVendorEditStatus.WaitingAssignment &&
                 x.Assignees.Any(a =>
                     a.UserId == userId ||
                     (a.Delegatee != null && a.Delegatee.SuUserId == userId))) ||
                (x.Status == ContractDraftVendorEditStatus.WaitingComment &&
                 x.Assignees.Any(a =>
                     a.UserId == userId ||
                     (a.Delegatee != null && a.Delegatee.SuUserId == userId))) ||
                (x.Status == ContractDraftVendorEditStatus.RejectedToAssignee &&
                 x.Assignees.Any(a =>
                     a.UserId == userId ||
                     (a.Delegatee != null && a.Delegatee.SuUserId == userId))) ||
                (x.Status == ContractDraftVendorEditStatus.WaitingApproval &&
                 x.Acceptors.Any(a =>
                     a.Type == AcceptorType.Approver &&
                     (a.IsCurrent || a.Status == AcceptorStatus.Pending) &&
                     (a.UserId == userId ||
                      (a.Delegatee != null && a.Delegatee.SuUserId == userId)))));
        }
        else if (req.WorkProcess == WorkProcess.Related)
        {
            query = query.Where(x =>
                (x.Status == ContractDraftVendorEditStatus.WaitingCommitteeApproval &&
                 x.Acceptors.Any(a =>
                     a.Type == AcceptorType.AcceptanceCommittee &&
                     !a.IsCurrent &&
                     a.Status != AcceptorStatus.Pending &&
                     a.UserId == userId)) ||
                (x.Status == ContractDraftVendorEditStatus.WaitingApproval &&
                 x.Acceptors.Any(a =>
                     a.Type == AcceptorType.Approver &&
                     !a.IsCurrent &&
                     a.Status != AcceptorStatus.Pending &&
                     (a.UserId == userId ||
                      (a.Delegatee != null && a.Delegatee.SuUserId == userId)))));
        }
        else if (req.WorkProcess == WorkProcess.Completed)
        {
            query = query.Where(x =>
                x.Status == ContractDraftVendorEditStatus.Approved &&
                (x.Acceptors.Any(a =>
                     a.UserId == userId ||
                     (a.Delegatee != null && a.Delegatee.SuUserId == userId)) ||
                 x.Assignees.Any(a =>
                     a.UserId == userId ||
                     (a.Delegatee != null && a.Delegatee.SuUserId == userId))));
        }

        var orderQuery = query.OrderByDescending(o => o.AuditInfo.LastModifiedAt ?? o.AuditInfo.CreatedAt);

        var paginated = await PaginatedList<CaContractDraftVendorEdit>.CreateAsync(
            orderQuery,
            req.PageNumber,
            req.PageSize,
            ct);

        var contractDraftVendorIds = paginated.Select(e => e.ContractDraftVendorId).Distinct().ToList();

        var procurementInfoMap = await this.dbContext.CaContractDraftVendors
                                           .Include(v => v.ContractDraft)
                                           .ThenInclude(cd => cd.Procurement)
                                           .ThenInclude(p => p.SupplyMethod)
                                           .Include(v => v.ContractDraft)
                                           .ThenInclude(cd => cd.Procurement)
                                           .ThenInclude(p => p.Department)
                                           .Where(v => contractDraftVendorIds.Contains(v.Id))
                                           .ToDictionaryAsync(
                                               v => v.Id,
                                               v => new
                                               {
                                                   SupplyMethodName = v.ContractDraft?.Procurement?.SupplyMethod?.Label,
                                                   DepartmentName = v.ContractDraft?.Procurement?.Department?.Name,
                                               },
                                               ct);

        var result = paginated.ToResult(e =>
        {
            procurementInfoMap.TryGetValue(e.ContractDraftVendorId, out var info);

            return new ContractDraftVendorEditListItemResponse(
                e.Id,
                e.ContractDraftVendorId,
                e.ContractNumber,
                e.PoNumber,
                e.ContractSignedDate,
                e.ContractName,
                e.Budget,
                e.ContractType?.Label,
                e.Status,
                info?.DepartmentName ?? string.Empty,
                info?.SupplyMethodName);
        });

        return TypedResults.Ok(result);
    }
}