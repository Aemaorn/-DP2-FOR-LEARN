namespace GHB.DP2.Application.Features.Procurement.TorDraft;

using Codehard.FileService.Contracts.ValueObjects;
using FluentValidation;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.TorDraft.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PpTorDraft;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;

public record UpdateTorDraftRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid ProcurementId,
    Guid Id,
    string ReferenceNumber,
    string? TelephoneNumber,
    bool? BidGuarantee,
    bool? IsStock,
    bool? IsMA,
    bool? IsSaveDraft,
    string? Reason,
    string? EvaluationCriteria,
    string? TorDocumentTemplateCode,
    TorDraftStatus Status,
    Guid? TorDraftDocumentId,
    bool? IsTorDraftDocumentIdReplaced,
    Guid? TorDraftApprovalDocumentId,
    bool? IsTorDraftApprovalDocumentIdReplaced,
    string? ChangeReason,
    string? CancelReason,
    DateTimeOffset? DocumentDate,
    TorDraftObjectResponse[]? Objects,
    TorDraftQualificationResponse[]? Qualifications,
    TorDraftTechnicalSpecificationResponse[]? TechnicalSpecifications,
    TorDraftTechnicalPeriodResponse[]? TechnicalPeriods,
    TorDraftBudgetResponse[]? Budgets,
    TorDraftPaymentTermResponse[]? PaymentTerms,
    TorDraftWarrantyResponse[]? Warranties,
    TorDraftFineRateResponse[]? FineRates,
    TorDraftAcceptorResponse[]? Acceptors,
    AssigneeRequest[]? Assignees,
    bool? IsContractGuarantee,
    decimal? PercentageContract,
    string? DocumentDescription,
    string? ManuelDescription,
    TorPreventiveMaintenanceResponse? PreventiveMaintenance,
    TorCorrectiveMaintenanceResponse? CorrectiveMaintenance,
    TorTrainingResponse? Training,
    TorTrainingItemResponse[]? TrainingItems,
    TorImpedimentResponse[]? Impediments,
    TorDraftPaymentTermPeriodsResponse[]? PaymentTermPeriods,
    bool? IsCM,
    bool? IsPM,
    bool? IsTraining,
    bool? IsImpediment
);

public record UpdateTorDraftResponse(Guid? NewTorDocumentFileId, Guid? NewApprovalDocumentFileId);

public class UpdateTorDraftRequestValidator : AbstractValidator<UpdateTorDraftRequest>
{
    public UpdateTorDraftRequestValidator()
    {
        this.RuleFor(x => x.ReferenceNumber)
            .NotEmpty()
            .WithMessage("กรุณาระบุเลขที่อ้างอิง");
        this.RuleFor(x => x.TelephoneNumber)
            .MaximumLength(20)
            .WithMessage("เบอร์โทรศัพท์ต้องไม่เกิน 20 ตัวอักษร");
        this.RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("กรุณาระบุเหตุผล");
        this.RuleForEach(x => x.Objects)
            .ChildRules(obj =>
            {
                obj.RuleFor(o => o.Description)
                   .NotEmpty()
                   .WithMessage("กรุณาระบุวัตถุประสงค์");
            });
        this.RuleForEach(x => x.Qualifications)
            .ChildRules(q =>
            {
                q.RuleFor(o => o.Description)
                 .NotEmpty()
                 .WithMessage("กรุณาระบุคุณสมบัติผู้ประสงค์จะเสนอราคา");
            });
        this.RuleForEach(x => x.Budgets)
            .ChildRules(b =>
            {
                b.RuleFor(o => o.Description)
                 .NotEmpty()
                 .WithMessage("กรุณาระบุรายละเอียดงบประมาณ");
                b.RuleFor(o => o.BudgetAmount)
                 .GreaterThan(0)
                 .WithMessage("กรุณาระบุจำนวนเงินงบประมาณมากกว่าศูนย์");
            });
        this.RuleForEach(x => x.PaymentTerms)
            .ChildRules(p =>
            {
                p.RuleFor(o => o.PaymentPercent)
                 .GreaterThan(0)
                 .WithMessage("กรุณาระบุเปอร์เซ็นต์การชำระเงินมากกว่าศูนย์");
            });
        this.RuleForEach(x => x.Warranties)
            .ChildRules(w =>
            {
                w.RuleFor(o => o.Period)
                 .GreaterThanOrEqualTo(0)
                 .WithMessage("กรุณาระบุระยะเวลาการรับประกัน")
                 .When(x => x.HasWarranty == true);

                w.RuleFor(r => r.PeriodTypeCode)
                 .NotEmpty()
                 .WithMessage("กรุณาระบุช่วงเวลา")
                 .When(x => x.HasWarranty == true);

                w.RuleFor(r => r.ConditionOther)
                 .NotEmpty()
                 .WithMessage("กรุณาระบุเงื่อนไขการรับประกัน")
                 .When(x => x.HasWarranty == true);
            });
        this.RuleForEach(x => x.FineRates)
            .ChildRules(f =>
            {
                f.RuleFor(o => o.Rate)
                 .GreaterThan(0)
                 .WithMessage("กรุณาระบุอัตราค่าปรับมากกว่าศูนย์");
            });
    }
}

public class UpdateTorDraftEndpoint : TorDraftEndpointBase<UpdateTorDraftRequest, Results<Ok<UpdateTorDraftResponse>, NotFound<string>, BadRequest<string>>>
{
    private const string EntityNotFoundMessage = "ไม่พบข้อมูล";
    private const string InvalidStatusChangeMessage = "ไม่สามารถเปลี่ยนสถานะเป็น เรียกคืนแก้ไข ได้ เนื่องจากสถานะปัจจุบันไม่ใช่ รอการอนุมัติคณะกรรมการ/ผู้มีอำนาจ";

    private readonly Dp2DbContext dbContext;

    public UpdateTorDraftEndpoint(
        ILogger<UpdateTorDraftEndpoint> logger,
        IOperationService operationService,
        ICommandTextService commandTextService,
        Dp2DbContext dbContext)
        : base(logger, dbContext, operationService, commandTextService)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("Procurement/TorDraft")
             .WithName("UpdateTorDraft")
             .Produces<Ok>()
             .Produces<NotFound>()
             .Accepts<UpdateTorDraftRequest>("application/json"));
        this.Put("procurement/{ProcurementId:guid}/tordraft/{Id:guid}");
    }

    protected override async ValueTask<Results<Ok<UpdateTorDraftResponse>, NotFound<string>, BadRequest<string>>> HandleRequestAsync(
        UpdateTorDraftRequest req,
        CancellationToken ct)
    {
        if (!req.IsSaveDraft.GetValueOrDefault(false))
        {
            var validationResult = await new UpdateTorDraftRequestValidator().ValidateAsync(req, ct);

            if (!validationResult.IsValid)
            {
                return TypedResults.BadRequest(string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
            }
        }

        var torDraft = await this.LoadEntityWithIncludes(req.Id, req.ProcurementId, ct);

        var appoint = await this.GetAppointById(ProcurementId.From(req.ProcurementId), ct);

        this.ValidateDocument(req, torDraft);

        if (torDraft is null)
        {
            return TypedResults.NotFound(EntityNotFoundMessage);
        }

        var oldStatus = torDraft.Status;

        var statusValidationResult = ValidateStatusChange(torDraft, req.Status);

        if (statusValidationResult != null)
        {
            return statusValidationResult;
        }

        var isTorDraftDocumentIdReplaced = req.IsTorDraftDocumentIdReplaced ?? false;
        var isTorDraftApprovalDocumentIdReplaced = req.IsTorDraftApprovalDocumentIdReplaced ?? false;

        var mustChangeTorTemplate =
            !string.IsNullOrWhiteSpace(req.TorDocumentTemplateCode) &&
            req.TorDocumentTemplateCode != torDraft.DocumentTemplate?.Code;

        if (!string.IsNullOrWhiteSpace(req.TorDocumentTemplateCode))
        {
            await this.UpdateDocumentTemplate(torDraft, req.TorDocumentTemplateCode!, ct);
        }

        if ((mustChangeTorTemplate && torDraft.DocumentHistories.Any()) || (!req.IsSaveDraft.GetValueOrDefault(false) && !torDraft.DocumentHistories.Any()))
        {
            await this.SetDefaultDocumentTemplate(
                   torDraft,
                   req.TorDocumentTemplateCode,
                   torDraft.Procurement.SupplyMethodCode,
                   torDraft.Procurement.HasMd,
                   ct);
        }

        FileId? newTorDocumentFileId = null;
        FileId? newApprovalDocumentFileId = null;

        var mustSaveTorDocument =
            req.TorDraftDocumentId.HasValue &&
            isTorDraftDocumentIdReplaced &&
            req.TorDocumentTemplateCode == torDraft.DocumentTemplate?.Code &&
            torDraft.Status != TorDraftStatus.WaitingCommitteeApproval;

        var mustSaveTorApprovalDocument =
            req.TorDraftApprovalDocumentId.HasValue &&
            isTorDraftApprovalDocumentIdReplaced &&
            req.TorDocumentTemplateCode == torDraft.DocumentTemplate?.Code &&
            torDraft.Status != TorDraftStatus.WaitingCommitteeApproval;

        if (mustSaveTorDocument)
        {
            newTorDocumentFileId = await this.UpdateDocumentHistoryAsync(
                torDraft,
                PpTorDraftDocumentType.Tor,
                FileId.From(req.TorDraftDocumentId!.Value),
                isTorDraftDocumentIdReplaced,
                ct);
        }

        if (mustSaveTorApprovalDocument)
        {
            newApprovalDocumentFileId = await this.UpdateDocumentHistoryAsync(
                torDraft,
                PpTorDraftDocumentType.Approval,
                FileId.From(req.TorDraftApprovalDocumentId!.Value),
                isTorDraftApprovalDocumentIdReplaced,
                ct);
        }

        await this.UpdateEntityData(torDraft, req, torDraft.Procurement.DepartmentId);

        if (req.Assignees is not null)
        {
            var newAssignees = req.Assignees.Where(x => x is { AssigneeType: AssigneeType.Assignee, Id: null });

            foreach (var inComing in newAssignees)
            {
                await SendNotificationAsync(
                    torDraft,
                    UserId.From(inComing.UserId),
                    NotificationConstant.Assignment.Title,
                    string.Format(NotificationConstant.Assignment.Message, ProgramConstant.PreProcurementTorDraft.Name, torDraft.ReferenceNumber));
            }

            await this.UpsertAssigneeAsync(torDraft, req.Assignees, UserId.From(req.UserId), ct);
        }

        if (req.ChangeReason is not null)
        {
            torDraft.SetChangeReason(req.ChangeReason);
        }

        if (req.CancelReason is not null)
        {
            torDraft.SetCancelReason(req.CancelReason);
        }

        if (req.Status == TorDraftStatus.WaitingCommitteeApproval
            || req.DocumentDate is not null)
        {
            torDraft.SetDocumentDate(req.DocumentDate);
        }

        if (torDraft.DocumentHistories.Any())
        {
            await this.UpdateAndReplaceDocumentAsync(torDraft, oldStatus, appoint, isTorDraftDocumentIdReplaced, isTorDraftApprovalDocumentIdReplaced, ct);
        }

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok(new UpdateTorDraftResponse(newTorDocumentFileId?.Value, newApprovalDocumentFileId?.Value));
    }

    private void ValidateDocument(UpdateTorDraftRequest req, PpTorDraft? torDraft)
    {
        if (req.Status == TorDraftStatus.WaitingCommitteeApproval &&
            (req.TorDraftDocumentId is not null || req.TorDraftApprovalDocumentId is not null) &&
            (torDraft != null && !torDraft.IsMigration.GetValueOrDefault(false) && !torDraft.DocumentHistories.Any()))
        {
            this.ThrowError("กรุณาจัดทำเอกสาร", StatusCodes.Status400BadRequest);
        }
    }

    private async Task UpdateDocumentTemplate(PpTorDraft torDraft, string torDocumentTemplateCode, CancellationToken ct)
    {
        var documentTemplate =
            await this.dbContext.SuDocumentTemplates
                      .FirstOrDefaultAsync(x => x.Code == torDocumentTemplateCode, ct);

        if (documentTemplate is null)
        {
            this.ThrowError("ไม่พบเอกสาร TOR Template ที่ระบุ", StatusCodes.Status404NotFound);
        }

        if (documentTemplate.Id == torDraft.DocumentTemplateId)
        {
            return;
        }

        torDraft.SetDocumentTemplate(documentTemplate.Id);
    }

    private async Task<PpTorDraft?> LoadEntityWithIncludes(Guid id, Guid procurementId, CancellationToken ct)
    {
        return await this.dbContext.PpTorDrafts
                         .Include(x => x.PpTorDraftObjects)
                         .Include(x => x.PpTorDraftQualifications)
                         .Include(x => x.PpTorDraftTechnicalSpecifications)
                         .Include(x => x.PpTorDraftTechnicalPeriods)
                         .ThenInclude(ppTorDraftTechnicalPeriod => ppTorDraftTechnicalPeriod.PpTorDraftTechnicalPeriodDetails)
                         .Include(x => x.PpTorDraftTechnicalPeriods)
                         .ThenInclude(x => x.DeliveryCondition)
                         .Include(x => x.PpTorDraftTechnicalPeriods)
                         .ThenInclude(x => x.PeriodType)
                         .Include(x => x.PpTorDraftTechnicalPeriods)
                         .ThenInclude(x => x.PeriodCondition)
                         .Include(x => x.PpTorDraftBudgets)
                         .ThenInclude(b => b.PpTorDraftBudgetDetails)
                         .Include(x => x.PpTorDraftPaymentTerms)
                         .ThenInclude(pt => pt.PpTorDraftPaymentTermDetails)
                         .Include(x => x.PpTorDraftWarranties)
                         .Include(x => x.PpTorDraftFineRates)
                         .Include(x => x.PpTorDraftAcceptors)
                         .ThenInclude(u => u.User)
                         .ThenInclude(e => e.Employee)
                         .ThenInclude(v => v.View)
                         .Include(p => p.Procurement)
                         .ThenInclude(pl => pl.Plan)
                         .AsSplitQuery()
                         .SingleOrDefaultAsync(x => x.Id == PpTorDraftId.From(id) && x.ProcurementId == Domain.Procurement.ProcurementId.From(procurementId), ct);
    }

    private static BadRequest<string>? ValidateStatusChange(PpTorDraft entity, TorDraftStatus requestedStatus)
    {
        if (requestedStatus == TorDraftStatus.Edit && entity.Status is not (TorDraftStatus.WaitingCommitteeApproval or TorDraftStatus.Edit or TorDraftStatus.WaitingApproval))
        {
            return TypedResults.BadRequest(InvalidStatusChangeMessage);
        }

        return null;
    }

    private async Task UpdateEntityData(PpTorDraft entity, UpdateTorDraftRequest req, BusinessUnitId workBusinessUnitId)
    {
        UpdateMainFields(entity, req);
        await this.UpdateCollections(entity, req, workBusinessUnitId, UserId.From(req.UserId));

        if (entity.Status == req.Status)
        {
            entity.AddActivity(new ActivityInfo(
                ActivityLogActionTypeConstant.Update,
                $"อัปเดตข้อมูล",
                entity.Status.ToString()));
        }
        else
        {
            UpdateStatus(entity, req);
            await EnsureInitialCommitteeCurrents(entity);
        }
    }

    private static Task EnsureInitialCommitteeCurrents(PpTorDraft entity)
    {
        if (entity.Status != TorDraftStatus.WaitingCommitteeApproval)
        {
            return Task.CompletedTask;
        }

        var committee = entity.PpTorDraftAcceptors?
                              .Where(a => a.Type == AcceptorType.TorDraftCommittee && a.IsActive && !a.IsUnableToPerformDuties && a.Status == AcceptorStatus.Pending)
                              .ToList();

        if (committee == null || committee.Count == 0)
        {
            return Task.CompletedTask;
        }

        // if any committee (non-chair) already approved do not reset currents
        if (entity.PpTorDraftAcceptors!.Any(a => a.Type == AcceptorType.TorDraftCommittee && a.Status == AcceptorStatus.Approved))
        {
            return Task.CompletedTask;
        }

        var chairman = committee.FirstOrDefault(a => a.CommitteePositionsCode != null && a.CommitteePositionsCode == ParameterCode.From("PosBoard001"))
                       ?? committee.FirstOrDefault(a => a.IsBoardChairman());

        foreach (var a in committee)
        {
            a.SetCurrent(false);
        }

        var nonChair = committee.Where(a => chairman == null || a.Id != chairman.Id).ToList();

        if (nonChair.Count == 0 && chairman != null)
        {
            chairman.SetCurrent(true); // only chairman

            _ = SendNotificationAsync(
                entity,
                chairman.UserId,
                NotificationConstant.WaitForLike.Title,
                string.Format(NotificationConstant.WaitForLike.Message, ProgramConstant.PreProcurementTorDraft.Name, entity.ReferenceNumber));

            return Task.CompletedTask;
        }

        // set current for all non-chair and notify them
        foreach (var a in nonChair)
        {
            _ = SendNotificationAsync(
                entity,
                a.UserId,
                NotificationConstant.WaitForLike.Title,
                string.Format(NotificationConstant.WaitForLike.Message, ProgramConstant.PreProcurementTorDraft.Name, entity.ReferenceNumber));
            a.SetCurrent(true);
        }

        if (chairman != null)
        {
            chairman.SetCurrent(false);
        }

        return Task.CompletedTask;
    }

    private async Task UpdateCollections(PpTorDraft entity, UpdateTorDraftRequest req, BusinessUnitId workBusinessUnitId, UserId userId)
    {
        var lastAssigneeUserId = req.Assignees?
            .Where(a => a.AssigneeType == AssigneeType.Assignee)
            .OrderByDescending(a => a.Sequence)
            .Select(a => (UserId?)UserId.From(a.UserId))
            .FirstOrDefault();

        await this.UpdateAcceptors(entity, req.Acceptors, req.Status, workBusinessUnitId, lastAssigneeUserId ?? userId);
        UpdateObjects(entity, req.Objects);
        UpdateQualifications(entity, req.Qualifications);
        UpdateTechnicalPeriods(entity, req.TechnicalPeriods);
        UpdateTechnicalSpecifications(entity, req.TechnicalSpecifications);
        UpdateBudgets(entity, req.Budgets);
        UpdateWarranties(entity, req.Warranties);
        UpdatePaymentTerms(entity, req.PaymentTerms);
        UpdateFineRates(entity, req.FineRates);
        UpdateTorTemplateComputer(
            entity,
            req.DocumentDescription,
            req.ManuelDescription,
            req.PreventiveMaintenance,
            req.CorrectiveMaintenance,
            req.Training);
        UpdateTrainingItems(entity, req.TrainingItems);
        UpdateImpediments(entity, req.Impediments);
        UpdatePaymentTermPeriods(entity, req.PaymentTermPeriods);
    }

    private static void UpdateMainFields(PpTorDraft entity, UpdateTorDraftRequest req)
    {
        entity.ReferenceNumber = (TorDraftNumber)req.ReferenceNumber;
        entity.Telephone = req.TelephoneNumber;
        entity.BidGuarantee = req.BidGuarantee;
        entity.IsStock = req.IsStock ?? false;
        entity.Reason = req.Reason;
        entity.IsContractGuarantee = req.IsContractGuarantee;
        entity.PercentageContract = req.IsContractGuarantee == true ? req.PercentageContract : null;
        entity.EvaluationCriteria = req.EvaluationCriteria;
        entity.IsMA = req.IsMA;
        entity.IsCM = req.IsCM;
        entity.IsPM = req.IsPM;
        entity.IsTraining = req.IsTraining;
        entity.IsImpediment = req.IsImpediment;
    }

    private static void UpdateStatus(PpTorDraft entity, UpdateTorDraftRequest req)
    {
        if (entity.Status == req.Status)
        {
            return; // No status change needed
        }

        if (entity.HasMajorityRejection())
        {
            entity.SetRejected(null);

            return;
        }

        switch (req.Status)
        {
            case TorDraftStatus.Edit:
                entity.SetEdit();

                break;

            case TorDraftStatus.WaitingCommitteeApproval:
                entity.SetWaitingCommitteeApproval();

                break;

            case TorDraftStatus.WaitingAssign:
                entity.SetAssigned();

                break;

            case TorDraftStatus.WaitingComment:
                entity.SetWaitingComment();
                _ = SendNotificationAssigneeAsync(entity, CancellationToken.None);

                break;

            case TorDraftStatus.WaitingApproval:
                entity.SetWaitingApproval();

                var approvers = entity.PpTorDraftAcceptors
                                      .Where(p => p.Type == AcceptorType.Approver)
                                      .OrderBy(a => a.Sequence)
                                      .ToList();

                var firstPending = approvers.Select(DelegatorExtensions.DelegatorToAcceptor)
                                            .FirstOrDefault(a => a.Status == AcceptorStatus.Pending && a.IsCurrent);

                if (firstPending != null)
                {
                    foreach (var targetUserId in firstPending.GetNotificationTargets())
                    {
                        _ = SendNotificationAsync(
                            entity,
                            targetUserId,
                            NotificationConstant.WaitForLike.Title,
                            string.Format(NotificationConstant.WaitForLike.Message, ProgramConstant.PreProcurementTorDraft.Name, entity.ReferenceNumber));
                    }
                }

                break;
        }
    }

    private static void UpdateTorTemplateComputer(
        PpTorDraft entity,
        string? documentDescription,
        string? manuelDescription,
        TorPreventiveMaintenanceResponse? pm,
        TorCorrectiveMaintenanceResponse? cm,
        TorTrainingResponse? training)
    {
        if (entity.PpTorTemplateComputer == null)
        {
            entity.PpTorTemplateComputer = new PpTorTemplateComputer
            {
                Id = PpPpTorTemplateComputerId.New(),
                DocumentDescription = documentDescription,
                ManuelDescription = manuelDescription,
                PreventiveMaintenance = new TorPreventiveMaintenance(),
                CorrectiveMaintenance = new TorCorrectiveMaintenance(),
                Training = new TorTraining(),
            };
        }

        var oldData = entity.PpTorTemplateComputer;

        oldData.DocumentDescription = documentDescription;
        oldData.ManuelDescription = manuelDescription;

        oldData.PreventiveMaintenance ??= new TorPreventiveMaintenance();
        oldData.CorrectiveMaintenance ??= new TorCorrectiveMaintenance();
        oldData.Training ??= new TorTraining();

        UpdatePreventiveMaintenance(oldData.PreventiveMaintenance!, pm);
        UpdateCorrectiveMaintenance(oldData.CorrectiveMaintenance!, cm);
        UpdateTraining(oldData.Training!, training);
    }

    private static void UpdatePreventiveMaintenance(TorPreventiveMaintenance dest, TorPreventiveMaintenanceResponse? src)
    {
        if (src == null)
        {
            return;
        }

        dest.PmProductName = src?.PmProductName ?? string.Empty;
        dest.PmCount = src?.PmCount;
        dest.PmUnit = !string.IsNullOrWhiteSpace(src?.PmUnit) ? ParameterCode.From(src.PmUnit) : null;
        dest.PmFinePct = src?.PmFinePct;
        dest.PmFineAmount = src?.PmFineAmount;
        dest.PmFinePctUnit = !string.IsNullOrWhiteSpace(src?.PmFinePctUnit) ? ParameterCode.From(src.PmFinePctUnit) : null;
        dest.Condition = src?.Condition;
        dest.DisruptedCount = src?.DisruptedCount;
        dest.DisruptedCountUnit = !string.IsNullOrWhiteSpace(src?.DisruptedCountUnit) ? ParameterCode.From(src.DisruptedCountUnit) : null;
        dest.DisruptedPercent = src?.DisruptedPercent;
        dest.DisruptedFinePercent = src?.DisruptedFinePercent;
        dest.DisruptedFineAmount = src?.DisruptedFineAmount;
    }

    private static void UpdateCorrectiveMaintenance(TorCorrectiveMaintenance dest, TorCorrectiveMaintenanceResponse? src)
    {
        if (src == null)
        {
            return;
        }

        dest.CmProductName = src?.CmProductName ?? string.Empty;
        dest.StartDate = src?.StartDate ?? null;
        dest.EndDate = src?.EndDate ?? null;
        dest.CmCount = src?.CmCount ?? null;
        dest.CmUnit = !string.IsNullOrWhiteSpace(src?.CmUnit) ? ParameterCode.From(src.CmUnit) : null;
        dest.CmCompleteCount = src?.CmCompleteCount ?? null;
        dest.CmCompleteUnit = !string.IsNullOrWhiteSpace(src?.CmCompleteUnit) ? ParameterCode.From(src.CmCompleteUnit) : null;
        dest.CmFinePercent = src?.CmFinePercent ?? null;
        dest.CmDisruptedFinePercent = src?.CmDisruptedFinePercent ?? null;
        dest.CmFinePercentUnit = !string.IsNullOrWhiteSpace(src?.CmFinePercentUnit) ? ParameterCode.From(src.CmFinePercentUnit) : null;
        dest.DayStart = !string.IsNullOrWhiteSpace(src?.DayStart) ? ParameterCode.From(src.DayStart) : null;
        dest.DayEnd = !string.IsNullOrWhiteSpace(src?.DayEnd) ? ParameterCode.From(src.DayEnd) : null;
        dest.StartTime = src?.StartTime ?? null;
        dest.EndTime = src?.EndTime ?? null;
    }

    private static void UpdateTraining(TorTraining dest, TorTrainingResponse? src)
    {
        if (src == null)
        {
            return;
        }

        dest.TrainingCount = src?.TrainingCount;
        dest.TrainingCountUnit = !string.IsNullOrWhiteSpace(src?.TrainingCountUnit) ? ParameterCode.From(src.TrainingCountUnit) : null;
        dest.TrainingUnitId = !string.IsNullOrWhiteSpace(src?.TrainingUnitId) ? ParameterCode.From(src.TrainingUnitId) : null;
    }

    private static void UpdateImpediments(PpTorDraft entity, TorImpedimentResponse[]? req)
    {
        RemoveUnmatched(entity.PpTorImpediments, req, (x, r)
            => x.Id == PpTorImpedimentId.From(r.Id));

        AddOrUpdate(
            entity.PpTorImpediments,
            req,
            objReq => new PpTorImpediment
            {
                Id = PpTorImpedimentId.New(),
                Sequence = objReq.Sequence ?? 1,
                Description = objReq.Description ?? string.Empty,
                ImpedimentValue = objReq.ImpedimentValue,
            },
            (obj, objReq) =>
            {
                obj.Sequence = objReq.Sequence ?? entity.PpTorImpediments.Count + 1;
                obj.Description = objReq.Description ?? string.Empty;
                obj.ImpedimentValue = objReq.ImpedimentValue;
            },
            (x, r) => x.Id.Value == r.Id);
    }

    private static void UpdateTrainingItems(PpTorDraft entity, TorTrainingItemResponse[]? req)
    {
        RemoveUnmatched(entity.PpTorTrainingItems, req, (x, r)
            => x.Id == PpTorTrainingItemId.From(r.Id));

        AddOrUpdate(
            entity.PpTorTrainingItems,
            req,
            objReq => new PpTorTrainingItem
            {
                Id = PpTorTrainingItemId.New(),
                Sequence = objReq.Sequence ?? 1,
                CourseName = objReq.CourseName ?? string.Empty,
                PeriodDay = objReq.PeriodDay,
                Place = objReq.Place ?? string.Empty,
                TrainingCount = objReq.TrainingCount,
                TotalPersonPerTime = objReq.TotalPersonPerTime,
            },
            (obj, objReq) =>
            {
                obj.Sequence = objReq.Sequence ?? entity.PpTorTrainingItems.Count + 1;
                obj.CourseName = objReq.CourseName ?? string.Empty;
                obj.PeriodDay = objReq.PeriodDay;
                obj.Place = objReq.Place ?? string.Empty;
                obj.TrainingCount = objReq.TrainingCount;
                obj.TotalPersonPerTime = objReq.TotalPersonPerTime;
            },
            (x, r) => x.Id.Value == r.Id);
    }

    private static void UpdateObjects(PpTorDraft entity, TorDraftObjectResponse[]? objects)
    {
        if (objects == null)
        {
            return;
        }

        RemoveUnmatched(entity.PpTorDraftObjects, objects, (x, r)
            => x.Id == PpTorDraftObjectId.From(r.Id));

        AddOrUpdate(
            entity.PpTorDraftObjects,
            objects,
            objReq => new PpTorDraftObject
            {
                Id = PpTorDraftObjectId.New(),
                Sequence = objReq.Sequence,
                Description = objReq.Description,
            },
            (obj, objReq) =>
            {
                obj.Sequence = objReq.Sequence;
                obj.Description = objReq.Description;
            },
            (x, r) => x.Id.Value == r.Id);
    }

    private static void UpdateQualifications(PpTorDraft entity, TorDraftQualificationResponse[]? qualifications)
    {
        if (qualifications == null)
        {
            return;
        }

        RemoveUnmatched(entity.PpTorDraftQualifications, qualifications, (x, r)
            => x.Id == PpTorDraftQualificationsId.From(r.Id));

        AddOrUpdate(
            entity.PpTorDraftQualifications,
            qualifications,
            qualReq => new PpTorDraftQualifications
            {
                Id = PpTorDraftQualificationsId.New(),
                Sequence = qualReq.Sequence,
                Description = qualReq.Description,
            },
            (qual, qualReq) =>
            {
                qual.Sequence = qualReq.Sequence;
                qual.Description = qualReq.Description;
            },
            (x, r) => x.Id.Value == r.Id);
    }

    private static void UpdateTechnicalSpecifications(PpTorDraft entity, TorDraftTechnicalSpecificationResponse[]? technicalSpecifications)
    {
        if (technicalSpecifications == null)
        {
            return;
        }

        RemoveUnmatched(entity.PpTorDraftTechnicalSpecifications, technicalSpecifications, (x, r)
            => x.Id == PpTorDraftTechnicalSpecificationsId.From(r.Id));

        AddOrUpdate(
            entity.PpTorDraftTechnicalSpecifications,
            technicalSpecifications,
            specReq => new PpTorDraftTechnicalSpecifications
            {
                Id = PpTorDraftTechnicalSpecificationsId.New(),
                Sequence = specReq.Sequence,
                Name = specReq.Name,
                Description = specReq.Description,
                Quantity = specReq.Quantity,
                UnitCode = !string.IsNullOrWhiteSpace(specReq.UnitCode) ? ParameterCode.From(specReq.UnitCode) : null,
            },
            (spec, specReq) =>
            {
                spec.Sequence = specReq.Sequence;
                spec.Name = specReq.Name;
                spec.Description = specReq.Description;
                spec.Quantity = specReq.Quantity;
                spec.UnitCode = !string.IsNullOrWhiteSpace(specReq.UnitCode) ? ParameterCode.From(specReq.UnitCode) : null;
            },
            (x, r) => x.Id.Value == r.Id);
    }

    private static void UpdateTechnicalPeriods(PpTorDraft entity, TorDraftTechnicalPeriodResponse[]? technicalPeriods)
    {
        if (technicalPeriods == null)
        {
            return;
        }

        RemoveUnmatched(entity.PpTorDraftTechnicalPeriods, technicalPeriods, (x, r)
            => x.Id == PpTorDraftTechnicalPeriodId.From(r.Id));

        foreach (var periodReq in technicalPeriods)
        {
            var period = GetOrCreateTechnicalPeriod(entity, periodReq);
            UpdateTechnicalPeriodData(period, periodReq);
            UpdateTechnicalPeriodDetails(period, periodReq.Details);
        }
    }

    private static PpTorDraftTechnicalPeriod GetOrCreateTechnicalPeriod(PpTorDraft entity, TorDraftTechnicalPeriodResponse periodReq)
    {
        var period = entity.PpTorDraftTechnicalPeriods.FirstOrDefault(x => x.Id == PpTorDraftTechnicalPeriodId.From(periodReq.Id));

        if (period != null)
        {
            return period;
        }

        period = new PpTorDraftTechnicalPeriod
        {
            Id = PpTorDraftTechnicalPeriodId.New(),
            PpTorDraftTechnicalPeriodDetails = [],
        };
        entity.PpTorDraftTechnicalPeriods.Add(period);

        return period;
    }

    private static void UpdateTechnicalPeriodData(PpTorDraftTechnicalPeriod period, TorDraftTechnicalPeriodResponse periodReq)
    {
        period.Period = periodReq.Period;
        period.PeriodTypeCode = !string.IsNullOrWhiteSpace(periodReq.PeriodTypeCode) ? ParameterCode.From(periodReq.PeriodTypeCode) : null;
        period.PeriodConditionCode = !string.IsNullOrWhiteSpace(periodReq.PeriodConditionCode) ? ParameterCode.From(periodReq.PeriodConditionCode) : null;
        period.StartDate = periodReq.StartDate;
        period.EndDate = periodReq.EndDate;
        period.DeliveryConditionCode = !string.IsNullOrWhiteSpace(periodReq.DeliveryConditionCode) ? ParameterCode.From(periodReq.DeliveryConditionCode) : null;
        period.DeliveryDate = periodReq.DeliveryDate;
    }

    private static void UpdateTechnicalPeriodDetails(PpTorDraftTechnicalPeriod period, TorDraftTechnicalPeriodDetailResponse[]? details)
    {
        if (details == null)
        {
            return;
        }

        RemoveUnmatched(period.PpTorDraftTechnicalPeriodDetails, details, (x, r)
            => x.Id == PpTorDraftTechnicalPeriodDetailId.From(r.Id));

        AddOrUpdate(
            period.PpTorDraftTechnicalPeriodDetails,
            details,
            detailReq => new PpTorDraftTechnicalPeriodDetail
            {
                Id = PpTorDraftTechnicalPeriodDetailId.New(),
                Branch = detailReq.Branch,
                PersonalCount = detailReq.PersonalCount,
                StartDate = detailReq.StartDate,
                EndDate = detailReq.EndDate,
            },
            (detail, detailReq) =>
            {
                detail.Branch = detailReq.Branch;
                detail.PersonalCount = detailReq.PersonalCount;
                detail.StartDate = detailReq.StartDate;
                detail.EndDate = detailReq.EndDate;
            },
            (x, r) => x.Id.Value == r.Id);
    }

    private static void UpdateBudgets(PpTorDraft entity, TorDraftBudgetResponse[]? budgets)
    {
        if (budgets == null)
        {
            return;
        }

        RemoveUnmatched(entity.PpTorDraftBudgets, budgets, (x, r)
            => x.Id == PpTorDraftBudgetId.From(r.Id));

        foreach (var budgetReq in budgets)
        {
            var budget = GetOrCreateBudget(entity, budgetReq);
            UpdateBudgetData(budget, budgetReq);
            UpdateBudgetDetails(budget, budgetReq.Details);
        }
    }

    private static PpTorDraftBudget GetOrCreateBudget(PpTorDraft entity, TorDraftBudgetResponse budgetReq)
    {
        var budget = entity.PpTorDraftBudgets.FirstOrDefault(x => x.Id == PpTorDraftBudgetId.From(budgetReq.Id));

        if (budget != null)
        {
            return budget;
        }

        budget = new PpTorDraftBudget
        {
            Id = PpTorDraftBudgetId.New(),
            PpTorDraftBudgetDetails = [],
        };
        entity.PpTorDraftBudgets.Add(budget);

        return budget;
    }

    private static void UpdateBudgetData(PpTorDraftBudget budget, TorDraftBudgetResponse budgetReq)
    {
        budget.Sequence = budgetReq.Sequence;
        budget.Description = budgetReq.Description;
        budget.BudgetAmount = budgetReq.BudgetAmount;
    }

    private static void UpdateBudgetDetails(PpTorDraftBudget budget, TorDraftBudgetDetailResponse[]? details)
    {
        if (details == null)
        {
            return;
        }

        RemoveUnmatched(budget.PpTorDraftBudgetDetails, details, (x, r)
            => x.Id == PpTorDraftBudgetDetailId.From(r.Id));

        AddOrUpdate(
            budget.PpTorDraftBudgetDetails,
            details,
            detailReq => new PpTorDraftBudgetDetail
            {
                Id = PpTorDraftBudgetDetailId.New(),
                Sequence = detailReq.Sequence,
                Department = detailReq.Department,
                BudgetType = detailReq.BudgetType,
                ProjectCode = detailReq.ProjectCode,
                AccountNo = detailReq.AccountNo,
                Budget = detailReq.Budget,
            },
            (detail, detailReq) =>
            {
                detail.Sequence = detailReq.Sequence;
                detail.Department = detailReq.Department;
                detail.BudgetType = detailReq.BudgetType;
                detail.ProjectCode = detailReq.ProjectCode;
                detail.AccountNo = detailReq.AccountNo;
                detail.Budget = detailReq.Budget;
            },
            (x, r) => x.Id.Value == r.Id);
    }

    private static void UpdateWarranties(PpTorDraft entity, TorDraftWarrantyResponse[]? warranties)
    {
        if (warranties == null)
        {
            return;
        }

        RemoveUnmatched(entity.PpTorDraftWarranties, warranties, (x, r)
            => x.Id == PpTorDraftWarrantyId.From(r.Id));

        AddOrUpdate(
            entity.PpTorDraftWarranties,
            warranties,
            wReq => new PpTorDraftWarranty
            {
                Id = PpTorDraftWarrantyId.New(),
                HasWarranty = wReq.HasWarranty,
                Period = wReq.Period,
                PeriodTypeCode = !string.IsNullOrWhiteSpace(wReq.PeriodTypeCode) ? ParameterCode.From(wReq.PeriodTypeCode) : null,
                ConditionOther = wReq.ConditionOther,
            },
            (w, wReq) =>
            {
                w.HasWarranty = wReq.HasWarranty;
                w.Period = wReq.Period;
                w.PeriodTypeCode = !string.IsNullOrWhiteSpace(wReq.PeriodTypeCode) ? ParameterCode.From(wReq.PeriodTypeCode) : null;
                w.ConditionOther = wReq.ConditionOther;
            },
            (x, r) => x.Id.Value == r.Id);
    }

    private static void UpdatePaymentTerms(PpTorDraft entity, TorDraftPaymentTermResponse[]? paymentTerms)
    {
        if (paymentTerms == null)
        {
            return;
        }

        RemoveUnmatched(entity.PpTorDraftPaymentTerms, paymentTerms, (x, r)
            => x.Id == PpTorDraftPaymentTermId.From(r.Id));

        foreach (var ptReq in paymentTerms)
        {
            var pt = GetOrCreatePaymentTerm(entity, ptReq);
            UpdatePaymentTermData(pt, ptReq);
            UpdatePaymentTermDetails(pt, ptReq.Details);
        }
    }

    private static PpTorDraftPaymentTerm GetOrCreatePaymentTerm(PpTorDraft entity, TorDraftPaymentTermResponse ptReq)
    {
        var pt = entity.PpTorDraftPaymentTerms.FirstOrDefault(x => x.Id == PpTorDraftPaymentTermId.From(ptReq.Id));

        if (pt != null)
        {
            return pt;
        }

        pt = new PpTorDraftPaymentTerm
        {
            Id = PpTorDraftPaymentTermId.New(),
            PpTorDraftPaymentTermDetails = [],
        };
        entity.PpTorDraftPaymentTerms.Add(pt);

        return pt;
    }

    private static void UpdatePaymentTermData(PpTorDraftPaymentTerm pt, TorDraftPaymentTermResponse ptReq)
    {
        pt.ProRateTypeCode = !string.IsNullOrWhiteSpace(ptReq.ProRateTypeCode) ? ParameterCode.From(ptReq.ProRateTypeCode) : null;
        pt.PaymentPercent = ptReq.PaymentPercent;
        pt.Description = ptReq.Description ?? string.Empty;
        pt.Period = ptReq.Period;
        pt.PeriodTypeCode = !string.IsNullOrWhiteSpace(ptReq.PeriodTypeCode) ? ParameterCode.From(ptReq.PeriodTypeCode) : null;
        pt.TotalPeriod = ptReq.TotalPeriod;
        pt.TotalPeriodTypeCode = !string.IsNullOrWhiteSpace(ptReq.TotalPeriodTypeCode) ? ParameterCode.From(ptReq.TotalPeriodTypeCode) : null;
        pt.IsMA = ptReq.IsMA;
    }

    private static void UpdatePaymentTermDetails(PpTorDraftPaymentTerm pt, TorDraftPaymentTermDetailResponse[]? details)
    {
        if (details == null || pt.PpTorDraftPaymentTermDetails == null)
        {
            return;
        }

        RemoveUnmatched(pt.PpTorDraftPaymentTermDetails, details, (x, r)
            => x.Id == PpTorDraftPaymentTermDetailId.From(r.Id));

        AddOrUpdate(
            pt.PpTorDraftPaymentTermDetails,
            details,
            d => new PpTorDraftPaymentTermDetail
            {
                Id = PpTorDraftPaymentTermDetailId.New(),
                TermNumber = d.TermNumber,
                Percent = d.Percent,
                Period = d.Period,
                Description = d.Description ?? string.Empty,
            },
            (detail, d) =>
            {
                detail.TermNumber = d.TermNumber;
                detail.Percent = d.Percent;
                detail.Period = d.Period;
                detail.Description = d.Description ?? string.Empty;
            },
            (x, r) => x.Id.Value == r.Id);
    }

    private static void UpdatePaymentTermPeriods(PpTorDraft entity, TorDraftPaymentTermPeriodsResponse[]? paymentTermPeriods)
    {
        if (paymentTermPeriods == null)
        {
            return;
        }

        RemoveUnmatched(entity.PpTorPaymentTermPeriods, paymentTermPeriods, (x, r)
            => x.Id == PpTorDraftPaymentTermPeriodId.From(r.Id));

        foreach (var ptReq in paymentTermPeriods)
        {
            var pt = GetOrCreatePaymentTermPeriods(entity, ptReq);
            UpdatePaymentTermPeriodsData(pt, ptReq);
        }
    }

    private static PpTorPaymentTermPeriod GetOrCreatePaymentTermPeriods(PpTorDraft entity, TorDraftPaymentTermPeriodsResponse ptReq)
    {
        var pt = entity.PpTorPaymentTermPeriods.FirstOrDefault(x => x.Id == PpTorDraftPaymentTermPeriodId.From(ptReq.Id));

        if (pt != null)
        {
            return pt;
        }

        pt = new PpTorPaymentTermPeriod
        {
            Id = PpTorDraftPaymentTermPeriodId.New(),
        };

        entity.PpTorPaymentTermPeriods.Add(pt);

        return pt;
    }

    private static void UpdatePaymentTermPeriodsData(PpTorPaymentTermPeriod pt, TorDraftPaymentTermPeriodsResponse ptReq)
    {
        pt.Sequence = ptReq.Sequence;
        pt.Description = ptReq.Description;
        pt.Quantity = ptReq.Quantity;
        pt.PeriodTypeCode = !string.IsNullOrWhiteSpace(ptReq.PeriodTypeCode) ? ParameterCode.From(ptReq.PeriodTypeCode) : null;
        pt.TotalQuantity = ptReq.TotalQuantity;
        pt.TotalPeriodTypeCode = !string.IsNullOrWhiteSpace(ptReq.TotalPeriodTypeCode) ? ParameterCode.From(ptReq.TotalPeriodTypeCode) : null;
    }

    private static void UpdateFineRates(PpTorDraft entity, TorDraftFineRateResponse[]? fineRates)
    {
        if (fineRates == null)
        {
            return;
        }

        RemoveUnmatched(entity.PpTorDraftFineRates, fineRates, (x, r)
            => x.Id == PpTorDraftFineRateId.From(r.Id));

        AddOrUpdate(
            entity.PpTorDraftFineRates,
            fineRates,
            fReq => new PpTorDraftFineRate
            {
                Id = PpTorDraftFineRateId.New(),
                Sequence = fReq.Sequence,
                Description = fReq.Description,
                Rate = fReq.Rate,
                PeriodTypeCode = !string.IsNullOrWhiteSpace(fReq.PeriodTypeCode) ? ParameterCode.From(fReq.PeriodTypeCode) : null,
                ConditionCode = !string.IsNullOrWhiteSpace(fReq.ConditionCode) ? ParameterCode.From(fReq.ConditionCode) : null,
                ConditionOther = fReq.ConditionOther,
            },
            (f, fReq) =>
            {
                f.Sequence = fReq.Sequence;
                f.Description = fReq.Description;
                f.Rate = fReq.Rate;
                f.PeriodTypeCode = !string.IsNullOrWhiteSpace(fReq.PeriodTypeCode) ? ParameterCode.From(fReq.PeriodTypeCode) : null;
                f.ConditionCode = !string.IsNullOrWhiteSpace(fReq.ConditionCode) ? ParameterCode.From(fReq.ConditionCode) : null;
                f.ConditionOther = fReq.ConditionOther;
            },
            (x, r) => x.Id.Value == r.Id);
    }

    private async Task UpdateAcceptors(PpTorDraft entity, TorDraftAcceptorResponse[]? acceptors, TorDraftStatus status, BusinessUnitId workBusinessUnitId, UserId sendToAcceptorId)
    {
        if (acceptors == null)
        {
            return;
        }

        if (ShouldResetAcceptorStatusToDraft(entity, status))
        {
            entity.PpTorDraftAcceptors.Iter(p => p.Draft());

            return;
        }

        RemoveUnmatched(entity.PpTorDraftAcceptors, acceptors, (x, r) =>
            x.Id == AcceptorId.From((Guid)r.Id));

        var userIds = acceptors
                      .Select(a => UserId.From(a.UserId))
                      .ToArr();

        var users = await this.dbContext.SuUsers
                              .Include(u => u.Employee)
                              .ThenInclude(e => e.View)
                              .Where(u => userIds.Contains(u.Id))
                              .ToListAsync();

        AddOrUpdate(
            entity.PpTorDraftAcceptors,
            acceptors,
            aReq => CreateAcceptor(aReq, status, workBusinessUnitId, users, sendToAcceptorId),
            (x, r) => UpdateAcceptor(x, r, workBusinessUnitId, sendToAcceptorId),
            (x, r) => x.Id == AcceptorId.From(r.Id));
    }

    private static bool ShouldResetAcceptorStatusToDraft(PpTorDraft entity, TorDraftStatus status)
    {
        return status == TorDraftStatus.Edit && entity.Status == TorDraftStatus.WaitingCommitteeApproval;
    }

    private static PpTorDraftAcceptors CreateAcceptor(
        TorDraftAcceptorResponse aReq,
        TorDraftStatus status,
        BusinessUnitId workBusinessUnitId,
        List<SuUser> users,
        UserId sendToAcceptorId)
    {
        var user = users.FirstOrDefault(u => u.Id == UserId.From(aReq.UserId));

        var acceptorData = new PpTorDraftAcceptors.AcceptorInfoData(
            aReq.AcceptorType,
            UserId.From(aReq.UserId),
            EmployeeCode.From(aReq.EmployeeCode),
            user?.Employee.FullName ?? aReq.FullName,
            user?.Employee.ConvertPositionName(workBusinessUnitId) ?? aReq.PositionName,
            aReq.DepartmentName,
            aReq.Sequence);

        var createData = PpTorDraftAcceptors.Create(acceptorData, status);
        createData.SetSendToAcceptorId(sendToAcceptorId);
        SetAcceptorAdditionalData(createData, aReq);

        return createData;
    }

    private static void UpdateAcceptor(
        PpTorDraftAcceptors acceptor,
        TorDraftAcceptorResponse aReq,
        BusinessUnitId workBusinessUnitId,
        UserId sendToAcceptorId)
    {
        var positionName =
            !acceptor.User.IsNull()
                ? acceptor.User.Employee.ConvertPositionName(workBusinessUnitId)
                : aReq.PositionName;

        var fullName =
            !acceptor.User.IsNull()
                ? acceptor.User.Employee.FullName
                : aReq.FullName;

        var acceptorData = new PpTorDraftAcceptors.AcceptorInfoData(
            aReq.AcceptorType,
            UserId.From(aReq.UserId),
            EmployeeCode.From(aReq.EmployeeCode),
            fullName,
            positionName,
            aReq.DepartmentName,
            aReq.Sequence);

        acceptor.Update(acceptorData);
        acceptor.SetSendToAcceptorId(sendToAcceptorId);
        SetAcceptorAdditionalData(acceptor, aReq);
    }

    private static void SetAcceptorAdditionalData(PpTorDraftAcceptors acceptor, TorDraftAcceptorResponse aReq)
    {
        acceptor.SetIsUnableToPerformDuties(aReq.IsUnableToPerformDuties);

        if (aReq is { IsUnableToPerformDuties: true, Remark: not null })
        {
            acceptor.UnableToPerformDuties(aReq.Remark);
        }

        if (aReq.AcceptorType == AcceptorType.TorDraftCommittee)
        {
            acceptor.SetCommitteePositionsCode(
                aReq.CommitteePositionsCode.IsNullOrEmpty()
                    ? null
                    : ParameterCode.From(aReq.CommitteePositionsCode!));
        }
    }

    private static void RemoveUnmatched<T, TReq>(
        ICollection<T>? entityCollection,
        IEnumerable<TReq>? reqCollection,
        Func<T, TReq, bool> match)
    {
        if (entityCollection == null || reqCollection == null)
        {
            return;
        }

        var toRemove = entityCollection.Where(x => !reqCollection.Any(r => match(x, r))).ToList();

        foreach (var item in toRemove)
        {
            entityCollection.Remove(item);
        }
    }

    private static void AddOrUpdate<T, TReq>(
        ICollection<T>? entityCollection,
        IEnumerable<TReq>? reqCollection,
        Func<TReq, T> create,
        Action<T, TReq> update,
        Func<T, TReq, bool> match)
        where T : class
    {
        if (entityCollection == null || reqCollection == null)
        {
            return;
        }

        foreach (var req in reqCollection)
        {
            var entity = entityCollection.FirstOrDefault(x => match(x, req));

            if (entity == null)
            {
                entityCollection.Add(create(req));

                continue;
            }

            update(entity!, req);
        }
    }

    private static async Task SendNotificationAsync(PpTorDraft torDraft, UserId userId, string title, string message)
    {
        await Notification
              .Crate(
                  userId,
                  title,
                  message,
                  NotificationProgram.Procurement)
              .SetReferenceId(torDraft.Id.Value)
              .SetLinkUrl(string.Format(ProgramConstant.Procurement.Url, torDraft.Procurement.Id), "ดูรายละเอียด")
              .PublishAsync(CancellationToken.None);
    }

    private static async Task SendNotificationAssigneeAsync(PpTorDraft torDraft, CancellationToken ct)
    {
        foreach (var targetUserId in torDraft.Assignees.Where(x => x.Type != AssigneeType.Director).SelectMany(a => a.GetAssigneeNotificationTargets()))
        {
            await Notification
                  .Crate(
                      targetUserId,
                      NotificationConstant.Assignment.Title,
                      string.Format(NotificationConstant.Assignment.Message, ProgramConstant.PreProcurementTorDraft.Name, torDraft.ReferenceNumber),
                      NotificationProgram.Procurement)
                  .SetReferenceId(torDraft.Id.Value)
                  .SetLinkUrl(
                      string.Format(ProgramConstant.Procurement.Url, torDraft.Procurement.Id),
                      "ดูรายละเอียด")
                  .PublishAsync(ct);
        }
    }
}