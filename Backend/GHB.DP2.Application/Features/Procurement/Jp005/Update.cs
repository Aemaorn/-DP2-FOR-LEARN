namespace GHB.DP2.Application.Features.Procurement.Jp005;

using Codehard.FileService.Contracts.ValueObjects;
using FluentValidation;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.Jp005.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PJp005;
using GHB.DP2.Domain.Procurement.PpPurchaseRequisition;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;

public record UpdateJp005Request(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid Id,
    Guid ProcurementId,
    Guid PurchaseRequisitionId,
    PJp005Status Status,
    EvaluationDto Evaluations,
    string? EgpProjectNumber,
    string? JorPorNumber,
    CommitteeSectionDto ProcurementCommittees,
    CommitteeSectionDto InspectionCommittees,
    CommitteeSectionDto MaintenanceInspectionCommittee,
    CommitteeSectionDto ConstructionSupervisor,
    Guid? Jp005ApprovalDocumentId,
    bool? IsJp005ApprovalDocumentIdReplaced,
    Guid? Jp005CommandDocumentId,
    bool? IsJp005CommandDocumentIdReplaced,
    IEnumerable<AcceptorRequest> Acceptors,
    IEnumerable<ProcurementSuppliesDivisionDto> ProcurementSuppliesDivision,
    DateTimeOffset? DocumentDate,
    string? PrNumber,
    string? Telephone,
    string? Description,
    string? PriceReasonablenessInfo,
    decimal? MedianPriceAmount);

public class UpdateJp005RequestValidator : Validator<UpdateJp005Request>
{
    public UpdateJp005RequestValidator()
    {
        this.RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("ต้องมีข้อมูล จพ.005 เบื้องต้น");

        this.RuleFor(x => x.ProcurementId)
            .NotEmpty()
            .WithMessage("ต้องมีข้อมูลจัดซื้อจัดจ้างเบื้องต้น");

        this.RuleFor(x => x.PurchaseRequisitionId)
            .NotEmpty()
            .WithMessage("ต้องมีข้อมูล จพ.004 เบื้องต้น");

        this.RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage("สถานะไม่ถูกต้อง");

        this.RuleFor(x => x.Evaluations)
            .NotNull()
            .WithMessage("ข้อมูลการพิจารณาต้องไม่เป็นค่าว่าง")
            .SetValidator(new EvaluationValidator());

        this.RuleFor(x => x.ProcurementCommittees)
            .NotNull()
            .WithMessage("ข้อมูลคณะกรรมการจัดซื้อจัดจ้างต้องไม่เป็นค่าว่าง")
            .NotEmpty()
            .WithMessage("ข้อมูลคณะกรรมการจัดซื้อจัดจ้างต้องไม่เป็นค่าว่าง")
            .DependentRules(() =>
            {
                this.RuleFor(x => x.ProcurementCommittees)
                    .SetValidator(new CommitteeSectionDtoValidator("จัดซื้อจัดจ้าง"));
            });

        this.RuleFor(x => x.InspectionCommittees)
            .NotNull()
            .WithMessage("ข้อมูลคณะกรรมการตรวจรับต้องไม่เป็นค่าว่าง")
            .NotEmpty()
            .WithMessage("ข้อมูลคณะกรรมการตรวจรับต้องไม่เป็นค่าว่าง")
            .DependentRules(() =>
            {
                this.RuleFor(x => x.InspectionCommittees)
                    .SetValidator(new CommitteeSectionDtoValidator("ตรวจรับ"));
            });

        this.RuleFor(x => x.Acceptors)
            .NotNull()
            .WithMessage("ข้อมูลผู้มีอำนาจต้องไม่เป็นค่าว่าง")
            .NotEmpty()
            .WithMessage("ข้อมูลผู้มีอำนาจต้องไม่เป็นค่าว่าง");

        this.RuleForEach(x => x.Acceptors)
            .SetValidator(new AcceptorRequestValidator());

        this.RuleForEach(x => x.Acceptors)
            .Must(a =>
                a.AcceptorType == AcceptorType.Approver ||
                a.AcceptorType == AcceptorType.DepartmentDirectorAgree)
            .WithMessage("ประเภทผู้อนุมัติ/เห็นชอบต้องเป็น ผู้มีอำนาจเห็นชอบ หรือ สายงานเห็นชอบ เท่านั้น");
    }
}

public record UpdateJp005Response(Guid? NewApprovalDocumentFileId, Guid? NewCommandDocumentFileId);

public class UpdateJp005Endpoint
    : Jp005EndpointBase<UpdateJp005Request, Results<Ok<UpdateJp005Response>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public UpdateJp005Endpoint(
        Dp2DbContext dbContext,
        IOperationService operationService,
        ICommandTextService commandTextService,
        ILogger<UpdateJp005Endpoint> logger)
        : base(dbContext, operationService, commandTextService, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b => b.WithTags("Procurement/JorPor005"));
        this.Put("procurement/{ProcurementId:guid}/jp005/{Id:guid}");
    }

    protected override async ValueTask<Results<Ok<UpdateJp005Response>, NotFound<string>>> HandleRequestAsync(
        UpdateJp005Request req,
        CancellationToken ct)
    {
        var procurement = await this.GetProcurementById(
            ProcurementId.From(req.ProcurementId),
            ct);

        var jp005Existing = this.GetJp005ById(
            procurement.Jp005,
            PJp005Id.From(req.Id),
            ProcurementId.From(req.ProcurementId));

        this.ValidateDocument(req, procurement, jp005Existing);

        var canEdit =
            jp005Existing.Status is
                PJp005Status.Draft or
                PJp005Status.Edit or
                PJp005Status.Rejected or
                PJp005Status.WaitingApproval;

        if (jp005Existing.Status == PJp005Status.Approved)
        {
            jp005Existing.SetJorPorNumber(req.JorPorNumber);
        }
        else
        {
            var isApproved = jp005Existing.Status == PJp005Status.WaitingApproval && jp005Existing.Acceptors.Any(x => x.Status != AcceptorStatus.Pending);

            if (!canEdit || isApproved)
            {
                this.ThrowError(
                    r =>
                        req.Id,
                    $"จพ.005 ที่ระบุไม่อยู่ในสถานะที่สามารถแก้ไขได้ (สถานะปัจจุบัน: {jp005Existing.Status})",
                    StatusCodes.Status409Conflict);
            }

            jp005Existing.SetEvaluationData(
                             req.Evaluations.EvaluationDueDate,
                             req.Evaluations.EvaluationPeriodTypeCode,
                             req.Evaluations.EvaluationPeriodConditionCode)
                         .SetEgpProjectNumber(req.EgpProjectNumber);

            await this.UpsertCommitteesAsync(
                jp005Existing,
                req.ProcurementCommittees,
                req.InspectionCommittees,
                req.MaintenanceInspectionCommittee,
                req.ConstructionSupervisor,
                ct);

            this.UpsertProcurementSuppliesDivisionAsync(
                jp005Existing,
                req.ProcurementSuppliesDivision);

            this.UpsertCommitteeDuties(
                jp005Existing,
                req.ProcurementCommittees,
                req.InspectionCommittees,
                req.MaintenanceInspectionCommittee,
                req.ConstructionSupervisor);

            await this.UpsertAcceptorAsync(
                jp005Existing,
                [.. req.Acceptors],
                procurement.DepartmentId,
                UserId.From(req.UserId),
                ct);

            var purchaseRequisition = await this.dbContext.PpPurchaseRequisitions
                                                .FirstOrDefaultAsync(
                                                    x => x.Id == PpPurchaseRequisitionId.From(req.PurchaseRequisitionId),
                                                    ct);

            purchaseRequisition?.UpdatePriceConsiderationInfo(
                req.PrNumber,
                req.Description,
                req.Telephone,
                req.PriceReasonablenessInfo,
                req.MedianPriceAmount);
        }

        if (req.Status == PJp005Status.WaitingApproval
            || req.DocumentDate is not null)
        {
            jp005Existing.SetDocumentDate(req.DocumentDate);
        }

        var isReplaceApproval = req.IsJp005ApprovalDocumentIdReplaced ?? false;
        var isReplaceCommand = req.IsJp005CommandDocumentIdReplaced ?? false;

        if (jp005Existing.DocumentHistories == null || !jp005Existing.DocumentHistories.Any())
        {
            await this.SetDefaultDocumentTemplate(
                jp005Existing,
                jp005Existing.Procurement.SupplyMethodCode,
                ct);
        }

        FileId? newApprovalDocumentFileId = null;
        FileId? newCommandDocumentFileId = null;

        if (req.Jp005ApprovalDocumentId.HasValue && isReplaceApproval && jp005Existing.Status != PJp005Status.WaitingApproval)
        {
            newApprovalDocumentFileId = await this.UpdateDocumentHistoryAsync(
                jp005Existing,
                PJp005DocumentType.Approval,
                FileId.From(req.Jp005ApprovalDocumentId.Value),
                isReplaceApproval,
                ct);
        }

        if (req.Jp005CommandDocumentId.HasValue && isReplaceCommand && jp005Existing.Status != PJp005Status.WaitingApproval)
        {
            newCommandDocumentFileId = await this.UpdateDocumentHistoryAsync(
                jp005Existing,
                PJp005DocumentType.Command,
                FileId.From(req.Jp005CommandDocumentId.Value),
                isReplaceCommand,
                ct);
        }

        if (jp005Existing.Status == req.Status)
        {
            jp005Existing.AddActivity(new ActivityInfo(
                ActivityLogActionTypeConstant.Update,
                $"อัปเดตข้อมูล",
                jp005Existing.Status.ToString()));
        }
        else
        {
            jp005Existing.UpdateStatus(req.Status);
        }

        await this.dbContext.SaveChangesAsync(ct);

        if (jp005Existing.Status == PJp005Status.Approved)
        {
            await this.UpdateDocumentCommandAsync(
                jp005Existing,
                req.UserId,
                procurement,
                hasCreator: true,
                hasAcceptor: true,
                hasPublisher: true,
                cancellationToken: ct);
        }
        else
        {
            await this.UpdateDocumentAsync(
                jp005Existing,
                req.UserId,
                procurement,
                isReplace: (req.IsJp005ApprovalDocumentIdReplaced ?? false) || (req.IsJp005CommandDocumentIdReplaced ?? false),
                hasCreator: jp005Existing.Status == PJp005Status.WaitingApproval,
                hasAcceptor: false,
                hasPublisher: false,
                cancellationToken: ct);
        }

        if (jp005Existing.Status == PJp005Status.WaitingApproval)
        {
            var approver = jp005Existing.Acceptors
                                        .Map(DelegatorExtensions.DelegatorToAcceptor)
                                        .OrderBy(o => o.Sequence)
                                        .FirstOrDefault(p => p.Status is AcceptorStatus.Pending);

            if (approver is null)
            {
                this.ThrowError($"ไม่พบข้อมูลผู้อนมัติ", StatusCodes.Status404NotFound);
            }

            foreach (var targetUserId in approver.GetNotificationTargets())
            {
                _ = SendNotificationAsync(
                    jp005Existing,
                    targetUserId,
                    NotificationConstant.WaitForLike.Title,
                    string.Format(NotificationConstant.WaitForLike.Message, ProgramConstant.PreProcurementJorPor05.Name, jp005Existing.PJp005Number));
            }
        }

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok(new UpdateJp005Response(newApprovalDocumentFileId?.Value, newCommandDocumentFileId?.Value));
    }

    private void ValidateDocument(UpdateJp005Request req, Procurement procurement, PJp005? jp005)
    {
        if (procurement.SupplyMethodCode == SupplyMethodConstant.Sixty && procurement.Budget > 100000)
        {
            return;
        }

        if (req is { Jp005ApprovalDocumentId: not null, Status: PJp005Status.WaitingApproval } &&
            (jp005 != null && !jp005.IsMigration.GetValueOrDefault(false) && !jp005.DocumentHistories.Any()))
        {
            this.ThrowError("กรุณาจัดทำเอกสาร", StatusCodes.Status400BadRequest);
        }

        if (procurement.Budget >= 1000000 && procurement.SupplyMethodCode == SupplyMethodConstant.Eighty &&
            req is { Jp005CommandDocumentId: not null, Status: PJp005Status.WaitingApproval } &&
            (jp005 != null && !jp005.IsMigration.GetValueOrDefault(false) && !jp005.DocumentHistories.Any()))
        {
            this.ThrowError("กรุณาจัดทำเอกสาร", StatusCodes.Status400BadRequest);
        }
    }

    private async ValueTask UpsertCommitteesAsync(
        PJp005 jp005Existing,
        CommitteeSectionDto procurementSection,
        CommitteeSectionDto inspectionSection,
        CommitteeSectionDto maintenanceInspectionCommittee,
        CommitteeSectionDto constructionSupervisor,
        CancellationToken ct)
    {
        var committeeIdsIncoming =
            procurementSection.Committees
                              .Select(c => c.Id)
                              .Concat(
                                  inspectionSection.Committees
                                                   .Select(c => c.Id))
                              .Concat(maintenanceInspectionCommittee.Committees
                                                                    .Select(m => m.Id))
                              .Concat(constructionSupervisor.Committees
                                                            .Select(c => c.Id))
                              .Where(id => id.HasValue)
                              .Select(id => PJp005CommitteeId.From(id.Value))
                              .ToList();

        var committeesToDelete =
            jp005Existing.Committees
                         .Where(c => !committeeIdsIncoming.Contains(c.Id))
                         .ToList();

        this.dbContext.PJp005Committees.RemoveRange(committeesToDelete);

        var allCommittees = new[]
        {
            (procurementSection.Committees, PJp005CommitteeGroupType.ProcurementCommittee),
            (inspectionSection.Committees, PJp005CommitteeGroupType.InspectionCommittee),
            (maintenanceInspectionCommittee.Committees, PJp005CommitteeGroupType.MaintenanceInspectionCommittee),
            (constructionSupervisor.Committees, PJp005CommitteeGroupType.ConstructionSupervisor),
        };

        foreach (var (committees, committeeGroupType) in allCommittees)
        {
            foreach (var committee in committees)
            {
                await this.CreateOrUpdateCommitteeAsync(
                    jp005Existing,
                    committee,
                    committeeGroupType,
                    ct);
            }
        }
    }

    private void UpsertProcurementSuppliesDivisionAsync(
        PJp005 jp005Existing,
        IEnumerable<ProcurementSuppliesDivisionDto> procurementSuppliesDivisions)
    {
        var existingIds =
            procurementSuppliesDivisions
                .Where(p => p.Id.HasValue)
                .Select(p => p.Id!.Value)
                .Map(PJp005ProcurementSuppliesDivisionId.From)
                .ToArray();

        _ = jp005Existing.ProcurementSuppliesDivisions
                         .ExceptBy(
                             existingIds,
                             d => d.Id)
                         .Iter(p => jp005Existing.RemoveProcurementSuppliesDivision(p));

        _ = jp005Existing
            .ProcurementSuppliesDivisions
            .Join(
                procurementSuppliesDivisions.Where(w => w.Id.HasValue),
                db => db.Id,
                payload => PJp005ProcurementSuppliesDivisionId.From(payload.Id!.Value),
                (db, payload) =>
                {
                    db.Update(payload.Sequence);

                    return db;
                }).ToHashSet();

        _ = procurementSuppliesDivisions
            .Where(p => !p.Id.HasValue)
            .Select(payload => PJp005ProcurementSuppliesDivision.CreatePJp005ProcurementSuppliesDivision(
                jp005Existing.Id,
                UserId.From(payload.UserId),
                payload.FullName,
                payload.FullPositionName,
                payload.Sequence))
            .Map(jp005Existing.AddProcurementSuppliesDivision)
            .ToHashSet();
    }

    private async Task CreateOrUpdateCommitteeAsync(
        PJp005 jp005Existing,
        CommitteeDto committeeReq,
        PJp005CommitteeGroupType groupType,
        CancellationToken ct)
    {
        var pobParameters = await this.dbContext
                                      .SuParameters
                                      .Where(w => w.GroupCode == GroupCode.From(ParameterGroupConstant.PositionOnBoard) ||
                                                  w.GroupCode == GroupCode.From(ParameterGroupConstant.PositionOnBoardProcurement) ||
                                                  w.GroupCode == GroupCode.From(ParameterGroupConstant.PositionOnBoardMA) ||
                                                  w.GroupCode == GroupCode.From(ParameterGroupConstant.PositionOnBoardSupervisor) ||
                                                  (w.GroupCode == GroupCode.From(ParameterGroupConstant.PositionOnBoardInSpection) &&
                                                   w.ParentId == null))
                                      .ToArrayAsync(ct);

        if (committeeReq.Id.HasValue)
        {
            var committeeExisting =
                await this.dbContext.PJp005Committees
                          .FirstOrDefaultAsync(c => c.Id == PJp005CommitteeId.From(committeeReq.Id.Value), ct);

            if (committeeExisting is null)
            {
                this.ThrowError(
                    r =>
                        committeeReq.Id.Value,
                    $"ไม่พบคณะกรรมการที่ระบุในระบบ",
                    StatusCodes.Status404NotFound);
            }

            committeeExisting.Update(
                ParameterCode.From(committeeReq.CommitteePositionsCode),
                this.FindCommitteeNameByCode(pobParameters, committeeReq.CommitteePositionsCode),
                committeeReq.Sequence);

            return;
        }

        PJp005Committee newCommittee = groupType switch
        {
            PJp005CommitteeGroupType.ProcurementCommittee => PJp005Committee.CreateProcurementCommittee(
                jp005Existing.Id,
                UserId.From(committeeReq.UserId),
                committeeReq.FullName,
                committeeReq.FullPositionName,
                ParameterCode.From(committeeReq.CommitteePositionsCode),
                this.FindCommitteeNameByCode(pobParameters, committeeReq.CommitteePositionsCode),
                committeeReq.Sequence),
            PJp005CommitteeGroupType.InspectionCommittee => PJp005Committee.CreateInspectionCommittee(
                jp005Existing.Id,
                UserId.From(committeeReq.UserId),
                committeeReq.FullName,
                committeeReq.FullPositionName,
                ParameterCode.From(committeeReq.CommitteePositionsCode),
                this.FindCommitteeNameByCode(pobParameters, committeeReq.CommitteePositionsCode),
                committeeReq.Sequence),
            PJp005CommitteeGroupType.MaintenanceInspectionCommittee => PJp005Committee.CreateMaintenanceInspectionCommittee(
                jp005Existing.Id,
                UserId.From(committeeReq.UserId),
                committeeReq.FullName,
                committeeReq.FullPositionName,
                ParameterCode.From(committeeReq.CommitteePositionsCode),
                this.FindCommitteeNameByCode(pobParameters, committeeReq.CommitteePositionsCode),
                committeeReq.Sequence),
            PJp005CommitteeGroupType.ConstructionSupervisor => PJp005Committee.CreateConstructionSupervisor(
                jp005Existing.Id,
                UserId.From(committeeReq.UserId),
                committeeReq.FullName,
                committeeReq.FullPositionName,
                ParameterCode.From(committeeReq.CommitteePositionsCode),
                this.FindCommitteeNameByCode(pobParameters, committeeReq.CommitteePositionsCode),
                committeeReq.Sequence),
            _ => throw new InvalidOperationException($"Unsupported committee group type: {groupType}"),
        };

        this.dbContext.PJp005Committees.Add(newCommittee);
    }

    private void UpsertCommitteeDuties(
        PJp005 jp005Existing,
        CommitteeSectionDto procurementSection,
        CommitteeSectionDto inspectionSection,
        CommitteeSectionDto maintenanceInspectionSection,
        CommitteeSectionDto constructionSupervisorSection)
    {
        var allIncomingDuties =
            procurementSection.Duties
                              .Select(d => (d, PJp005CommitteeGroupType.ProcurementCommittee))
                              .Concat(
                                  inspectionSection.Duties
                                                   .Select(d => (d, PJp005CommitteeGroupType.InspectionCommittee)))
                              .Concat(
                                  maintenanceInspectionSection.Duties
                                                              .Select(d => (d, PJp005CommitteeGroupType.MaintenanceInspectionCommittee)))
                              .Concat(
                                  constructionSupervisorSection.Duties
                                                               .Select(d => (d, PJp005CommitteeGroupType.ConstructionSupervisor)))
                              .ToList();

        var incomingIds =
            allIncomingDuties
                .Where(tuple => tuple.d.Id.HasValue)
                .Select(tuple => PJp005CommitteeDutiesId.From(tuple.d.Id.Value))
                .ToHashSet();

        var dutiesToDelete =
            jp005Existing.CommitteeDuties
                         .Where(existing => !incomingIds.Contains(existing.Id))
                         .ToList();

        this.dbContext.PJp005CommitteeDuties.RemoveRange(dutiesToDelete);

        foreach (var (dutyDto, groupType) in allIncomingDuties)
        {
            if (dutyDto.Id.HasValue)
            {
                var committeeDutyExisting =
                    jp005Existing.CommitteeDuties
                                 .FirstOrDefault(d => d.Id == PJp005CommitteeDutiesId.From(dutyDto.Id.Value));

                if (committeeDutyExisting is null)
                {
                    this.ThrowError(
                        r => dutyDto.Id,
                        $"ไม่พบหน้าที่คณะกรรมการที่ระบุในระบบ",
                        StatusCodes.Status404NotFound);
                }

                committeeDutyExisting.Update(dutyDto.Description, dutyDto.Sequence);

                continue;
            }

            var newDuty =
                PJp005CommitteeDuties.CreateCommitteeDuty(
                    jp005Existing.Id,
                    dutyDto.Description,
                    dutyDto.Sequence,
                    groupType);

            this.dbContext.PJp005CommitteeDuties.Add(newDuty);
            jp005Existing.AddCommitteeDuties(newDuty);
        }
    }

    private async Task UpsertAcceptorAsync(
        PJp005 jp005Existing,
        AcceptorRequest[] acceptorsRequest,
        BusinessUnitId workBusinessUnitId,
        UserId sendToAcceptorId,
        CancellationToken ct)
    {
        var acceptorNotNew = acceptorsRequest.Where(x => x.Id.HasValue);

        var toRemove = jp005Existing.Acceptors.Where(x => acceptorNotNew.All(r => x.Id != AcceptorId.From(r.Id.Value))).ToList();

        foreach (var item in toRemove)
        {
            jp005Existing.RemoveAcceptor(item);
        }

        var userIdsIncoming =
            acceptorsRequest.Map(s => s.UserId)
                            .Map(UserId.From)
                            .ToArray();

        var usersIncoming =
            await this.dbContext.SuUsers
                      .Include(r => r.Employee)
                      .ThenInclude(r => r.View)
                      .Where(w => userIdsIncoming.Contains(w.Id))
                      .ToArrayAsync(ct);

        var userNotExistsInDb
            = userIdsIncoming
              .Except(usersIncoming.Map(u => u.Id))
              .ToArray();

        if (userNotExistsInDb.Length > 0)
        {
            this.ThrowError(
                $"User with ID {string.Join(", ", userNotExistsInDb)} not found.",
                StatusCodes.Status404NotFound);
        }

        var newAcceptors =
            acceptorsRequest.Where(ar => !ar.Id.HasValue)
                            .Join(
                                usersIncoming,
                                a => a.UserId,
                                u => u.Id.Value,
                                (a, u) => PJp005Acceptors.Create(
                                    jp005Existing.Id,
                                    a.AcceptorType,
                                    u,
                                    a.Sequence,
                                    jp005Existing.Status,
                                    workBusinessUnitId))
                            .ToHashSet();

        _ = jp005Existing.Acceptors
                         .Join(
                             acceptorsRequest.Where(w => w.Id.HasValue),
                             db => db.Id.Value,
                             payload => payload.Id,
                             (db, payload) =>
                             {
                                 db.SetSequence(payload.Sequence)
                                   .SetStatus(
                                       jp005Existing.Status is PJp005Status.WaitingApproval
                                           ? AcceptorStatus.Pending
                                           : AcceptorStatus.Draft);

                                 db.SetSendToAcceptorId(sendToAcceptorId);

                                 return db;
                             }).ToHashSet();

        newAcceptors.Iter(a =>
        {
            a.SetSendToAcceptorId(sendToAcceptorId);
            jp005Existing.AddAcceptor(a);
        });
    }

    private static async Task SendNotificationAsync(PJp005 jp005Existing, UserId userId, string title, string message)
    {
        await Notification
              .Crate(
                  userId,
                  title,
                  message,
                  NotificationProgram.Procurement)
              .SetReferenceId(jp005Existing.Id.Value)
              .SetLinkUrl(string.Format(ProgramConstant.Procurement.Url, jp005Existing.Procurement.Id), "ดูรายละเอียด")
              .PublishAsync(CancellationToken.None);
    }
}