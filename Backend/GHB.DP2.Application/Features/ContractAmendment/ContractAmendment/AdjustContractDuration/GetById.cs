namespace GHB.DP2.Application.Features.ContractAmendment.ContractAmendment.AdjustContractDuration;

using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.ContractAmendment.ContractAmendment.AdjustContractDuration.Abstract;
using GHB.DP2.Application.Features.ContractAmendment.ContractAmendment.AdjustContractDuration.Dto;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment.CamContractAmendmentExtendChange;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PJp005;
using GHB.DP2.Domain.Procurement.PPrincipleApproval;
using GHB.DP2.Domain.Procurement.PPurchaseOrderApproval;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;

public record GetAdjustContractDurationByIdRequest(
    Guid ContractAmendmentId,
    Guid? Id);

public record AdjustContractDurationDocumentVersionResponse(
    Guid FileId,
    string Version,
    DateTimeOffset CreatedAt,
    string CreatedByName,
    bool IsCurrent);

public record GetAdjustContractDurationByIdResponse(
    Guid? Id,
    Guid ContractAmendmentId,
    Guid? ContractAddendumDocumentId,
    bool? IsContractAddendumDocumentIdReplaced,
    Guid? ContractAmendmentRequestDocumentId,
    bool? IsContractAmendmentRequestDocumentIdReplaced,
    AdjustContractDurationInfo AdjustContractDurationOld,
    AdjustContractDurationInfo AdjustContractDurationNew,
    AcceptorNoIdResponse[] Acceptors,
    AssigneeNoIdResponse[] Assignees,
    ContractAmendmentExtendChangeStatus Status,
    AdjustContractDurationDocumentVersionResponse[]? ContractAddendumDocumentVersions = null,
    AdjustContractDurationDocumentVersionResponse[]? ContractAmendmentRequestDocumentVersions = null);

public class GetAdjustContractDurationByIdEndpoint : AdjustContractDurationEndpointBase<GetAdjustContractDurationByIdRequest, Results<Ok<GetAdjustContractDurationByIdResponse>, NotFound<string>>>
{
    public GetAdjustContractDurationByIdEndpoint(ILogger<GetAdjustContractDurationByIdEndpoint> logger, Dp2DbContext dbContext)
        : base(logger, dbContext)
    {
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("ContractAmendment/AdjustContractDuration")
             .Produces<Ok<GetAdjustContractDurationByIdResponse>>());
        this.Get(
            "contract-amendment/{ContractAmendmentId:guid}/adjust-contract-duration/{Id:guid}",
            "contract-amendment/{ContractAmendmentId:guid}/adjust-contract-duration");
    }

    protected override async ValueTask<Results<Ok<GetAdjustContractDurationByIdResponse>, NotFound<string>>> HandleRequestAsync(GetAdjustContractDurationByIdRequest req, CancellationToken ct)
    {
        var resultAsync =
            req.Id.HasValue
                ? this.GetAdjustContractDurationById(req, ct)
                : this.GetAdjustContractDurationNoId(req, ct);

        var result = await resultAsync;

        if (result is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลการแก้ไขสัญญาที่ระบุ");
        }

        return TypedResults.Ok(result);
    }

    private async Task<GetAdjustContractDurationByIdResponse?> GetAdjustContractDurationNoId(GetAdjustContractDurationByIdRequest req, CancellationToken ct)
    {
        var entity = await this.GetContractDraftVendorAsync(req.ContractAmendmentId, ct);
        if (entity is null)
        {
            return null;
        }

        var result = AdjustContractDurationInfo.Map(entity);
        var defaultCommittee = await this.GetDefaultCommitteeAsync((CaContractDraftVendor)entity, ct);
        var defaultAssignees = await this.GetDefaultAssigneesAsync(ct);

        return new GetAdjustContractDurationByIdResponse(
            default,
            req.ContractAmendmentId,
            null,
            null,
            null,
            null,
            result,
            result,
            [.. defaultCommittee.OrderBy(d => d.Sequence)],
            [.. defaultAssignees.OrderBy(d => d.Sequence)],
            ContractAmendmentExtendChangeStatus.Draft);
    }

    private async Task<CaContractDraftVendor?> GetContractDraftVendorAsync(Guid contractAmendmentId, CancellationToken ct)
    {
        return await this.DbContext.CamContractAmendments
                         .Include(c => c.ContractDraftVendor)
                         .ThenInclude(c => c.PaymentTerms)
                         .ThenInclude(p => p.DeliveryAcceptancePeriods)
                         .Where(c => c.Id == CamContractAmendmentId.From(contractAmendmentId))
                         .Select(c => c.ContractDraftVendor)
                         .FirstOrDefaultAsync(ct);
    }

    private async Task<List<AcceptorNoIdResponse>> GetDefaultCommitteeAsync(CaContractDraftVendor entity, CancellationToken ct)
    {
        var inspectCommittee = entity.ContractDraft.Procurement.Type is ProcurementType.Procurement
            ? await this.GetProcurementCommitteeAsync(entity.ContractDraft.Procurement.Id, ct)
            : await this.GetPrincipleApprovalCommitteeAsync(entity.ContractDraft.Procurement.Id, ct);

        return inspectCommittee;
    }

    private async Task<List<AcceptorNoIdResponse>> GetProcurementCommitteeAsync(ProcurementId procurementId, CancellationToken ct)
    {
        var jp005 = await this.DbContext.PJp005S
                          .FirstOrDefaultAsync(w => w.ProcurementId == procurementId, ct);

        if (jp005 is null)
        {
            return await this.DbContext.PPurchaseOrderApprovals
                .Where(w => w.ProcurementId == procurementId)
                .SelectMany(s => s.Committees)
                .Include(u => u.User)
                .ThenInclude(e => e.Employee)
                .ThenInclude(v => v.View)
                .Where(w => w.GroupType == GroupType.InspectionCommittee)
                .OrderBy(o => o.Sequence)
                .AsAsyncEnumerable()
                .Select(
                    s => CreateAcceptorResponse(
                        new AcceptorInfo(
                            s.SuUserId.Value,
                            s.Sequence,
                            s.FullName,
                            s.FullPositionName,
                            s.User.Employee.View != null ? s.User.Employee.View.BusinessUnitName : string.Empty,
                            s.CommitteePositionsCode.Value,
                            s.CommitteePositionsName,
                            s.User.Employee.View != null ? (string)s.User.Employee.View.BusinessUnitId : string.Empty)))
                .ToListAsync(ct);
        }

        return await this.DbContext.PJp005S
            .Where(w => w.ProcurementId == procurementId)
            .SelectMany(s => s.Committees)
            .Where(w => w.GroupType == PJp005CommitteeGroupType.InspectionCommittee)
            .Select(s => new AcceptorInfo(
                s.SuUserId.Value,
                s.Sequence,
                s.FullName,
                s.FullPositionName,
                s.User.Employee.View != null ? s.User.Employee.View.BusinessUnitName : string.Empty,
                s.CommitteePositionsCode.Value,
                s.CommitteePositionsName,
                s.User.Employee.View != null ? (string)s.User.Employee.View.BusinessUnitId : string.Empty))
            .AsAsyncEnumerable()
            .Select(CreateAcceptorResponse)
            .ToListAsync(ct);
    }

    private async Task<List<AcceptorNoIdResponse>> GetPrincipleApprovalCommitteeAsync(ProcurementId procurementId, CancellationToken ct)
    {
        return await this.DbContext.PPrincipleApprovals
            .Where(w => w.ProcurementId == procurementId)
            .SelectMany(s => s.PrincipleApprovalCommittees)
            .Where(w => w.GroupType == CommitteeGroupType.AcceptanceCommittee)
            .Select(s => new AcceptorInfo(
                s.SuUserId.Value,
                s.Sequence,
                s.FullName,
                s.FullPositionName,
                s.User.Employee.View != null ? s.User.Employee.View.BusinessUnitName : string.Empty,
                s.CommitteePositionsCode.Value,
                s.CommitteePositionsName,
                s.User.Employee.View != null ? (string)s.User.Employee.View.BusinessUnitId : string.Empty))
            .AsAsyncEnumerable()
            .Select(CreateAcceptorResponse)
            .ToListAsync(ct);
    }

    private record AcceptorInfo(
        Guid UserId,
        int Sequence,
        string FullName,
        string FullPositionName,
        string BusinessUnitName,
        string CommitteePositionsCode,
        string CommitteePositionsName,
        string BusinessUnitId);

    private static AcceptorNoIdResponse CreateAcceptorResponse(AcceptorInfo acceptorInfo)
    {
        return new AcceptorNoIdResponse(
            null,
            AcceptorType.AcceptanceCommittee,
            acceptorInfo.UserId,
            acceptorInfo.Sequence,
            acceptorInfo.FullName,
            acceptorInfo.FullPositionName,
            acceptorInfo.BusinessUnitName,
            AcceptorStatus.Draft,
            null,
            null,
            acceptorInfo.CommitteePositionsCode,
            acceptorInfo.CommitteePositionsName,
            false,
            acceptorInfo.BusinessUnitId,
            null,
            false);
    }

    private async Task<List<AssigneeNoIdResponse>> GetDefaultAssigneesAsync(CancellationToken ct)
    {
        var defaultAssignees = new List<AssigneeNoIdResponse>();
        var jorPorSectionHead = await this.GetJorPorSectionHeadAsync(ct);

        if (jorPorSectionHead != null)
        {
            var director = CreateDirectorAssignee((SuUser)jorPorSectionHead);
            defaultAssignees.Add(director);
        }

        return defaultAssignees;
    }

    private async Task<SuUser?> GetJorPorSectionHeadAsync(CancellationToken ct)
    {
        return await this.DbContext.RawEmployeePositions
                         .Include(r => r.Employee)
                         .ThenInclude(r => r.View)
                         .Where(p =>
                             p.BusinessUnitId == BusinessUnitId.From(JorPor.DefaultSectionHead.BusinessUnitId) &&
                             p.Position.Name == JorPor.DefaultSectionHead.PositionName)
                         .Select(p => p.Employee)
                         .SelectMany(e => e.Users)
                         .FirstOrDefaultAsync(ct);
    }

    private static AssigneeNoIdResponse CreateDirectorAssignee(SuUser jorPorSectionHead)
    {
        return new AssigneeNoIdResponse(
            null,
            AssigneeGroup.Contract,
            AssigneeType.Director,
            jorPorSectionHead.Id.Value,
            1,
            jorPorSectionHead.Employee.View?.FullName ?? string.Empty,
            jorPorSectionHead.Employee.View?.FullPositionName ?? string.Empty,
            jorPorSectionHead.Employee.View?.BusinessUnitName ?? string.Empty,
            AssigneeStatus.Draft);
    }

    private async Task<GetAdjustContractDurationByIdResponse?> GetAdjustContractDurationById(GetAdjustContractDurationByIdRequest req, CancellationToken ct)
    {
        var extendChange =
            await this.DbContext.CamContractAmendmentExtendChanges
                      .Include(c => c.PaymentTerms)
                      .Include(e => e.Acceptors)
                      .Include(c => c.Assignees)
                      .Include(c => c.CamContractAmendment)
                      .ThenInclude(c => c.ContractDraftVendor)
                      .ThenInclude(c => c.ContractDraft)
                      .Include(c => c.DocumentHistories)
                      .Where(c =>
                          c.Id == ContractAmendmentExtendChangeId.From(req.Id!.Value) &&
                          c.CamContractAmendmentId == CamContractAmendmentId.From(req.ContractAmendmentId))
                      .FirstOrDefaultAsync(ct);

        if (extendChange is null)
        {
            return null;
        }

        var adjustContractDurationOld
            = AdjustContractDurationInfo.Map(extendChange.CamContractAmendment.ContractDraftVendor);
        var adjustContractDurationNew
            = AdjustContractDurationInfo.Map(extendChange);

        var currentAcceptors = extendChange.Acceptors
                                .Where(x => x.Type != AcceptorType.AcceptanceCommittee)
                                .Select(DelegatorExtensions.DelegatorToAcceptor)
                                .ToList();

        var currentCommittees = extendChange.Acceptors
                                   .Where(x => x.Type == AcceptorType.AcceptanceCommittee)
                                   .ToList();

        var acceptors = currentAcceptors.Union(currentCommittees)
                                        .OrderBy(o => o.Sequence)
                                        .Select(s =>
                                            new AcceptorNoIdResponse(
                                                s.Id.Value,
                                                s.Type,
                                                s.UserId.Value,
                                                s.Sequence,
                                                s.FullName,
                                                s.PositionName,
                                                s.User.Employee.View != null ? s.User.Employee.View.BusinessUnitName : string.Empty,
                                                s.Status,
                                                s.Remark,
                                                s.ActionAt,
                                                (string?)s.CommitteePositionsCode,
                                                s.CommitteePosition?.Label ?? string.Empty,
                                                s.IsUnableToPerformDuties,
                                                s.User.Employee.View != null ? (string)s.User.Employee.View.BusinessUnitId : string.Empty,
                                                (Guid?)s.DelegateeId,
                                                s.IsCurrentApprover(),
                                                s.Delegatee?.SuUserId.Value));

        var assignees =
            extendChange.Assignees
                        .OrderBy(o => o.Sequence)
                        .Select(DelegatorExtensions.DelegatorToAssignee)
                        .Select(a => new AssigneeNoIdResponse(
                            a.Id.Value,
                            a.Group,
                            a.Type,
                            a.UserId.Value,
                            a.Sequence,
                            a.User.FullName,
                            a.PositionName,
                            a.BusinessUnitName,
                            a.Status,
                            a.Remark,
                            a.ActionAt,
                            a.Delegatee?.SuUserId.Value)).ToArray();

        var extendChangeVersions = extendChange.DocumentHistories
            .Where(d => d.DocumentType == ExtendChangeAcceptorDocumentType.ExtendChange)
            .OrderVersions()
            .Select(d => new AdjustContractDurationDocumentVersionResponse(
                d.FileId.Value,
                d.Version,
                d.CreatedAt,
                d.CreatedByName ?? string.Empty,
                d.FileId == extendChange.LastedExtendChangeDocument?.FileId))
            .ToArray();

        var approvedVersions = extendChange.DocumentHistories
            .Where(d => d.DocumentType == ExtendChangeAcceptorDocumentType.Approved)
            .OrderVersions()
            .Select(d => new AdjustContractDurationDocumentVersionResponse(
                d.FileId.Value,
                d.Version,
                d.CreatedAt,
                d.CreatedByName ?? string.Empty,
                d.FileId == extendChange.LastedApprovedDocument?.FileId))
            .ToArray();

        return new GetAdjustContractDurationByIdResponse(
            extendChange.Id.Value,
            extendChange.CamContractAmendmentId.Value,
            extendChange.LastedExtendChangeDocument?.FileId.Value,
            extendChange.LastedExtendChangeDocument?.IsReplaced,
            extendChange.LastedApprovedDocument?.FileId.Value,
            extendChange.LastedApprovedDocument?.IsReplaced,
            adjustContractDurationOld,
            adjustContractDurationNew,
            [.. acceptors],
            assignees,
            extendChange.Status,
            extendChangeVersions,
            approvedVersions);
    }
}