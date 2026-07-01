namespace GHB.DP2.Application.Features.ContractAmendment.ContractAmendment.WaiveOrReducePenalty;

using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.ContractAmendment.ContractAmendment.WaiveOrReducePenalty.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment.CamContractAmendmentWaiveOrReducePenalty;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PJp005;
using GHB.DP2.Domain.Procurement.PPrincipleApproval;
using GHB.DP2.Domain.Procurement.PPurchaseOrderApproval;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetWaiveOrReducePenaltyByIdRequest(
    Guid CamContractAmendmentId,
    Guid? Id);

public record WaiveOrReducePenaltyDocumentVersionResponse(
    Guid FileId,
    string Version,
    DateTimeOffset CreatedAt,
    string CreatedByName,
    bool IsCurrent);

public record WaiveOrReducePenaltyResponse(
    Guid? Id,
    Guid CamContractAmendmentId,
    bool WaiveAll,
    Guid? ContractAddendumDocumentId,
    bool? IsContractAddendumDocumentIdReplaced,
    Guid? ContractAmendmentRequestDocumentId,
    bool? IsContractAmendmentRequestDocumentIdReplaced,
    PenaltyInfo PenaltyOld,
    PenaltyInfo PenaltyNew,
    IEnumerable<AcceptorNoIdResponse> Acceptors,
    IEnumerable<AssigneeNoIdResponse> Assignees,
    CamContractAmendmentWaiveOrReducePenaltyStatus Status,
    WaiveOrReducePenaltyDocumentVersionResponse[]? ContractAddendumDocumentVersions = null,
    WaiveOrReducePenaltyDocumentVersionResponse[]? ContractAmendmentRequestDocumentVersions = null)
{
    public static WaiveOrReducePenaltyResponse? MapToResponse(
        Guid camContractAmendmentId,
        CaContractDraftVendor? vendor,
        IEnumerable<AcceptorNoIdResponse> acceptors,
        IEnumerable<AssigneeNoIdResponse> assignees)
    {
        if (vendor is null)
        {
            return null;
        }

        var penalty = vendor.DraftTermsConditions.Penalty;

        return new WaiveOrReducePenaltyResponse(
            default,
            camContractAmendmentId,
            false,
            null,
            null,
            null,
            null,
            new PenaltyInfo(
                penalty.TypeCode?.Value,
                penalty.Rate,
                penalty.Amount,
                penalty.RateTypeCode?.Value),
            PenaltyInfo.Default,
            acceptors,
            assignees,
            CamContractAmendmentWaiveOrReducePenaltyStatus.Draft);
    }

    public static WaiveOrReducePenaltyResponse? MapToResponse(
        CamContractAmendmentWaiveOrReducePenalty? entity)
    {
        if (entity is null)
        {
            return null;
        }

        var contractPenalty =
            entity.CamContractAmendment
                  .ContractDraftVendor
                  .DraftTermsConditions
                  .Penalty;

        var penaltyOld = new PenaltyInfo(
            contractPenalty.TypeCode?.Value,
            contractPenalty.Rate,
            contractPenalty.Amount,
            contractPenalty.RateTypeCode?.Value);

        var penaltyNew = new PenaltyInfo(
            entity.PenaltyTypeCode?.Value,
            entity.Rate,
            entity.Amount,
            entity.RateTypeCode?.Value);

        var waiveOrReducePenaltyVersions = entity.DocumentHistories
            .Where(d => d.DocumentType == WaiveOrReducePenaltyDocumentType.WaiveOrReducePenalty)
            .OrderVersions()
            .Select(d => new WaiveOrReducePenaltyDocumentVersionResponse(
                d.FileId.Value,
                d.Version,
                d.CreatedAt,
                d.CreatedByName ?? string.Empty,
                d.FileId == entity.LastedWaiveOrReducePenaltyDocument?.FileId))
            .ToArray();

        var approvedVersions = entity.DocumentHistories
            .Where(d => d.DocumentType == WaiveOrReducePenaltyDocumentType.Approved)
            .OrderVersions()
            .Select(d => new WaiveOrReducePenaltyDocumentVersionResponse(
                d.FileId.Value,
                d.Version,
                d.CreatedAt,
                d.CreatedByName ?? string.Empty,
                d.FileId == entity.LastedApprovedRequestDocument?.FileId))
            .ToArray();

        return new WaiveOrReducePenaltyResponse(
            entity.Id.Value,
            entity.CamContractAmendment.Id.Value,
            entity.WaiveAll,
            entity.LastedWaiveOrReducePenaltyDocument?.FileId.Value,
            entity.LastedWaiveOrReducePenaltyDocument?.IsReplaced,
            entity.LastedApprovedRequestDocument?.FileId.Value,
            entity.LastedApprovedRequestDocument?.IsReplaced,
            penaltyOld,
            penaltyNew,
            entity.Acceptors
                  .Where(x => DelegatorExtensions.IsDelegatableType(x.Type))
                  .Select(DelegatorExtensions.DelegatorToAcceptor)
                  .ToList()
                  .Union(entity.Acceptors.Where(x => !DelegatorExtensions.IsDelegatableType(x.Type)))
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
                          s.IsCurrentApprover())),
            entity.Assignees
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
                      a.ActionAt)),
            entity.Status,
            waiveOrReducePenaltyVersions,
            approvedVersions);
    }
}

public record PenaltyInfo(
    string? PenaltyTypeCode,
    decimal? Rate,
    decimal? Amount,
    string? RateTypeCode)
{
    public static PenaltyInfo Default => new(
        null,
        null,
        null,
        null);
}

public class GetWaiveOrReducePenaltyByIdEndpoint : WaiveOrReducePenaltyEndpointBase<GetWaiveOrReducePenaltyByIdRequest, Results<Ok<WaiveOrReducePenaltyResponse>, NotFound<string>>>
{
    public GetWaiveOrReducePenaltyByIdEndpoint(ILogger<GetWaiveOrReducePenaltyByIdEndpoint> logger, Dp2DbContext dbContext)
        : base(logger, dbContext)
    {
    }

    public override void Configure()
    {
        this.Get(
            "contract-amendments/{CamContractAmendmentId:guid}/waive-or-reduce-penalty",
            "contract-amendments/{CamContractAmendmentId:guid}/waive-or-reduce-penalty/{Id:guid?}");
        this.Description(b =>
            b.WithTags("ContractAmendment/WaiveOrReducePenalty")
             .Produces<WaiveOrReducePenaltyResponse>()
             .Produces<string>(StatusCodes.Status404NotFound));
    }

    protected override async ValueTask<Results<Ok<WaiveOrReducePenaltyResponse>, NotFound<string>>> HandleRequestAsync(GetWaiveOrReducePenaltyByIdRequest req, CancellationToken ct)
    {
        var resultAsync =
            req.Id is null
                ? this.GetWaiveOrReducePenaltyNoId(req, ct)
                : this.DbContext.CamContractAmendmentWaiveOrReducePenalties
                      .Where(w => w.Id == WaiveOrReducePenaltyId.From(req.Id.Value)
                                  && w.CamContractAmendmentId == CamContractAmendmentId.From(req.CamContractAmendmentId))
                      .Include(w => w.CamContractAmendment)
                      .ThenInclude(c => c.ContractDraftVendor)
                      .ThenInclude(v => v.DraftTermsConditions)
                      .Include(w => w.DocumentHistories)
                      .Select(w => w)
                      .FirstOrDefaultAsync(ct)
                      .Map(WaiveOrReducePenaltyResponse.MapToResponse);

        var result = await resultAsync;

        if (result is null && req.Id is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลการแก้ไขสัญญาหรือคู่ค้าสัญญาที่เกี่ยวข้อง");
        }

        if (result is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลการยกเว้นหรือลดค่าปรับ");
        }

        return TypedResults.Ok(result);
    }

    private async Task<WaiveOrReducePenaltyResponse?> GetWaiveOrReducePenaltyNoId(GetWaiveOrReducePenaltyByIdRequest req, CancellationToken ct)
    {
        var conDraft = await this.GetContractDraftVendorForWaiveOrReduceAsync(req.CamContractAmendmentId, ct);
        if (conDraft is null)
        {
            this.ThrowError("ไม่พบข้อมูล", StatusCodes.Status404NotFound);
        }

        var defaultCommittee = await this.GetDefaultCommitteeForWaiveOrReduceAsync(conDraft, ct);
        var defaultAssignees = await this.GetDefaultAssigneesForWaiveOrReduceAsync(ct);

        return WaiveOrReducePenaltyResponse.MapToResponse(
            req.CamContractAmendmentId,
            conDraft,
            defaultCommittee.OrderBy(d => d.Sequence).ToArray(),
            defaultAssignees.OrderBy(d => d.Sequence).ToArray());
    }

    private async Task<CaContractDraftVendor?> GetContractDraftVendorForWaiveOrReduceAsync(Guid camContractAmendmentId, CancellationToken ct)
    {
        return await this.DbContext.CamContractAmendments
                         .Where(c => c.Id == CamContractAmendmentId.From(camContractAmendmentId))
                         .Include(c => c.ContractDraftVendor)
                         .ThenInclude(v => v.DraftTermsConditions)
                         .Select(c => c.ContractDraftVendor)
                         .FirstOrDefaultAsync(ct);
    }

    private async Task<List<AcceptorNoIdResponse>> GetDefaultCommitteeForWaiveOrReduceAsync(CaContractDraftVendor conDraft, CancellationToken ct)
    {
        var inspectCommittee = conDraft.ContractDraft.Procurement.Type is ProcurementType.Procurement
            ? await this.GetProcurementCommitteeForWaiveOrReduceAsync(conDraft.ContractDraft.Procurement.Id, ct)
            : await this.GetPrincipleApprovalCommitteeForWaiveOrReduceAsync(conDraft.ContractDraft.Procurement.Id, ct);

        return inspectCommittee;
    }

    private async Task<List<AcceptorNoIdResponse>> GetProcurementCommitteeForWaiveOrReduceAsync(
        ProcurementId procurementId, CancellationToken ct)
    {
        var jp005 = await this.DbContext.PJp005S
                          .FirstOrDefaultAsync(w => w.ProcurementId == procurementId, ct);

        if (jp005 is null)
        {
            var purchaseOrderApproval = await this.DbContext.PPurchaseOrderApprovals
                   .Where(w => w.ProcurementId == procurementId)
                   .SelectMany(s => s.Committees)
                   .Where(w => w.GroupType == GroupType.InspectionCommittee)
                   .Include(pJp005Committee => pJp005Committee.User)
                   .ThenInclude(suUser => suUser.Employee)
                   .ThenInclude(rawEmployee => rawEmployee.View)
                   .AsAsyncEnumerable()
                   .ToListAsync(ct);

            return [.. purchaseOrderApproval.Map(s => CreateAcceptorResponseForWaiveOrReduce(
                s.SuUserId.Value,
                s.Sequence,
                s.FullName,
                s.FullPositionName,
                s.User.Employee.View != null ? s.User.Employee.View.BusinessUnitName : string.Empty,
                s.CommitteePositionsCode.Value,
                s.CommitteePositionsName,
                s.User.Employee.View != null ? (string)s.User.Employee.View.BusinessUnitId : string.Empty))];
        }

        var res = await this.DbContext.PJp005S
                         .Where(w => w.ProcurementId == procurementId)
                         .SelectMany(s => s.Committees)
                         .Where(w => w.GroupType == PJp005CommitteeGroupType.InspectionCommittee)
                         .Include(pJp005Committee => pJp005Committee.User)
                         .ThenInclude(suUser => suUser.Employee)
                         .ThenInclude(rawEmployee => rawEmployee.View)
                         .AsAsyncEnumerable()
                         .ToListAsync(ct);

        return [.. res.Map(s => CreateAcceptorResponseForWaiveOrReduce(
            s.SuUserId.Value,
            s.Sequence,
            s.FullName,
            s.FullPositionName,
            s.User.Employee.View != null ? s.User.Employee.View.BusinessUnitName : string.Empty,
            s.CommitteePositionsCode.Value,
            s.CommitteePositionsName,
            s.User.Employee.View != null ? (string)s.User.Employee.View.BusinessUnitId : string.Empty))];
    }

    private async Task<List<AcceptorNoIdResponse>> GetPrincipleApprovalCommitteeForWaiveOrReduceAsync(
        ProcurementId procurementId, CancellationToken ct)
    {
        var res = await this.DbContext.PPrincipleApprovals
                         .Where(w => w.ProcurementId == procurementId)
                         .SelectMany(s => s.PrincipleApprovalCommittees)
                         .Where(w => w.GroupType == CommitteeGroupType.AcceptanceCommittee)
                         .Include(pPrincipleApprovalCommittee => pPrincipleApprovalCommittee.User)
                         .ThenInclude(suUser => suUser.Employee)
                         .ThenInclude(rawEmployee => rawEmployee.View)
                         .AsAsyncEnumerable()
                         .ToListAsync(ct);

        return [.. res.Map(s => CreateAcceptorResponseForWaiveOrReduce(
            s.SuUserId.Value,
            s.Sequence,
            s.FullName,
            s.FullPositionName,
            s.User.Employee.View != null ? s.User.Employee.View.BusinessUnitName : string.Empty,
            s.CommitteePositionsCode.Value,
            s.CommitteePositionsName,
            s.User.Employee.View != null ? (string)s.User.Employee.View.BusinessUnitId : string.Empty))];
    }

    private static AcceptorNoIdResponse CreateAcceptorResponseForWaiveOrReduce(
        Guid userId,
        int sequence,
        string fullName,
        string fullPositionName,
        string businessUnitName,
        string committeePositionsCode,
        string committeePositionsName,
        string businessUnitId)
    {
        return new AcceptorNoIdResponse(
            null,
            AcceptorType.AcceptanceCommittee,
            userId,
            sequence,
            fullName,
            fullPositionName,
            businessUnitName,
            AcceptorStatus.Draft,
            null,
            null,
            committeePositionsCode,
            committeePositionsName,
            false,
            businessUnitId,
            null,
            false);
    }

    private async Task<List<AssigneeNoIdResponse>> GetDefaultAssigneesForWaiveOrReduceAsync(CancellationToken ct)
    {
        var defaultAssignees = new List<AssigneeNoIdResponse>();
        var jorPorSectionHead = await this.GetJorPorSectionHeadForWaiveOrReduceAsync(ct);

        if (jorPorSectionHead != null)
        {
            var director = CreateDirectorAssigneeForWaiveOrReduce(jorPorSectionHead);
            defaultAssignees.Add(director);
        }

        return defaultAssignees;
    }

    private async Task<SuUser?> GetJorPorSectionHeadForWaiveOrReduceAsync(CancellationToken ct)
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

    private static AssigneeNoIdResponse CreateDirectorAssigneeForWaiveOrReduce(SuUser jorPorSectionHead)
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
}