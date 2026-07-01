namespace GHB.DP2.Application.Features.Procurement.TorDraft.Abstract;

using System.Linq;
using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.Procurement;
using GHB.DP2.Application.Features.Procurement.Procurement.Abstact;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PpAppoint;
using GHB.DP2.Domain.Procurement.PpTorDraft;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using GreatFriends.ThaiBahtText;
using LanguageExt;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public abstract class TorDraftEndpointBase<TRequest, TResponse> : EndpointBase<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : IResult
{
    private readonly Dp2DbContext dbContext;
    private readonly IOperationService operationService;
    private readonly ICommandTextService commandTextService;

    protected TorDraftEndpointBase(
        ILogger logger,
        Dp2DbContext dbContext,
        IOperationService operationService,
        ICommandTextService commandTextService)
        : base(logger)
    {
        this.dbContext = dbContext;
        this.operationService = operationService;
        this.commandTextService = commandTextService;
    }

    protected async Task<PpTorDraft> ValidateRequestAsync(ProcurementId procurementId, PpTorDraftId torId, CancellationToken ct)
    {
        var procurement = await this.dbContext.Procurements
                                    .SingleOrDefaultAsync(f => f.Id == procurementId, ct);

        if (procurement is null)
        {
            this.ThrowError("ไม่พบข้อมูลจัดซื้อจัดจ้าง", StatusCodes.Status404NotFound);
        }

        var torData = await this.dbContext.PpTorDrafts
                                .Include(a => a.PpTorDraftAcceptors)
                                .ThenInclude(a => a.User)
                                .ThenInclude(u => u.Employee)
                                .FirstOrDefaultAsync(a => a.Id == torId && a.ProcurementId == procurementId, ct);

        if (torData is null)
        {
            this.ThrowError("ไม่พบข้อมูลร่างขอบเขตงาน", StatusCodes.Status404NotFound);
        }

        return torData;
    }

    protected async Task<PpTorDraft> GetTorDraftById(
        PpTorDraftId id,
        ProcurementId procurementId,
        CancellationToken ct)
    {
        var torDraft = await this.dbContext.PpTorDrafts
                                 .Include(t => t.PpTorDraftAcceptors)
                                 .ThenInclude(a => a.User)
                                 .ThenInclude(u => u.Employee)
                                 .Include(t => t.Assignees)
                                 .Include(t => t.DocumentHistories)
                                 .Include(t => t.PpTorDraftBudgets)
                                 .ThenInclude(b => b.PpTorDraftBudgetDetails)
                                 .Include(t => t.PpTorDraftTechnicalSpecifications)
                                 .Include(t => t.PpTorDraftObjects)
                                 .Include(t => t.PpTorDraftQualifications)
                                 .Include(t => t.PpTorDraftPaymentTerms)
                                 .ThenInclude(pt => pt.PpTorDraftPaymentTermDetails)
                                 .Include(t => t.PpTorDraftWarranties)
                                 .Include(t => t.PpTorDraftFineRates)
                                 .Include(t => t.PpTorDraftTechnicalPeriods)
                                 .ThenInclude(tp => tp.PpTorDraftTechnicalPeriodDetails)
                                 .Include(t => t.PpTorDraftTechnicalPeriods)
                                 .ThenInclude(p => p.DeliveryCondition)
                                 .Include(t => t.PpTorDraftTechnicalPeriods)
                                 .ThenInclude(p => p.PeriodCondition)
                                 .Include(t => t.PpTorDraftTechnicalPeriods)
                                 .ThenInclude(p => p.PeriodType)
                                 .Include(t => t.PpTorTemplateComputer)
                                 .Include(t => t.PpTorTrainingItems)
                                 .Include(t => t.PpTorImpediments)
                                 .Include(t => t.Procurement)
                                 .ThenInclude(p => p.Department)
                                 .Include(t => t.Procurement)
                                 .ThenInclude(p => p.SupplyMethod)
                                 .Include(t => t.Procurement)
                                 .ThenInclude(p => p.SupplyMethodType)
                                 .Include(t => t.Procurement)
                                 .ThenInclude(p => p.SupplyMethodSpecialType)
                                 .Include(t => t.Procurement)
                                 .ThenInclude(p => p.Plan)
                                 .Include(t => t.Procurement)
                                 .AsSplitQuery()
                                 .FirstOrDefaultAsync(
                                     t =>
                                         t.Id == id &&
                                         t.ProcurementId == procurementId,
                                     ct);

        if (torDraft is null)
        {
            this.ThrowError($"TOR Draft with ID {id} not found for procurement {procurementId}.", StatusCodes.Status404NotFound);
        }

        return torDraft;
    }

    protected async Task<PpAppoint?> GetAppointById(
        ProcurementId procurementId,
        CancellationToken ct)
    {
        var appoint = await this.dbContext.PpAppoints
                                .Include(x => x.TorDraftCommittees)
                                .Include(x => x.TorDraftCommitteeDuties)
                                .Include(x => x.MedianPriceCommittees)
                                .ThenInclude(x => x.User)
                                .AsSplitQuery()
                                .FirstOrDefaultAsync(x => x.ProcurementId == procurementId, ct);

        return appoint;
    }

    protected async Task<FileId> GetDocumentTemplateByCode(
        string? requestTemplateCode,
        CancellationToken ct)
    {
        var documentService =
            this.Resolve<IDocumentService>();

        var fileId =
            await documentService.GetDocumentTemplateAsync(
                dt =>
                    dt.Group == DocumentTemplateGroups.Tor &&
                    dt.IsActive &&
                    dt.Code == requestTemplateCode,
                ct);

        return (FileId)fileId;
    }

    protected async Task<FileId> GetDocumentApprovalTemplateByCriteria(
        ParameterCode supplyMethodCode,
        bool isJorPorComment,
        bool isChange,
        bool isCancel,
        CancellationToken ct)
    {
        var documentService =
            this.Resolve<IDocumentService>();

        var fileId =
            await documentService.GetDocumentTemplateAsync(
                dt =>
                    dt.Group == DocumentTemplateGroups.Tor &&
                    dt.SupplyMethodCode == supplyMethodCode &&
                    dt.AdditionalInfo != null &&
                    (dt.IsCancel ?? false) == isCancel &&
                    (dt.IsChange ?? false) == isChange &&
                    dt.AdditionalInfo.RootElement
                      .GetProperty(nameof(SuDocumentTemplate.IsApproval))
                      .GetBoolean() &&
                    (
                        EF.Functions.JsonExists(dt.AdditionalInfo, nameof(SuDocumentTemplate.IsJorPorComment)) == false ||
                        dt.AdditionalInfo.RootElement
                          .GetProperty(nameof(SuDocumentTemplate.IsJorPorComment))
                          .GetBoolean() == isJorPorComment
                    ) &&
                    dt.IsActive,
                ct);

        return (FileId)fileId;
    }

    protected async ValueTask SetDefaultDocumentTemplate(
        PpTorDraft torDraftData,
        string? torDocumentTemplateCode,
        ParameterCode supplyMethodCode,
        bool hasJorporComment,
        CancellationToken ct)
    {
        var defaultDocumentTemplateId =
            await this.GetDocumentTemplateByCode(
                torDocumentTemplateCode,
                ct);

        var defaultDocumentApproveTemplateId =
            await this.GetDocumentApprovalTemplateByCriteria(
                supplyMethodCode,
                hasJorporComment,
                torDraftData.IsChange,
                torDraftData.IsCancel,
                ct);

        torDraftData.AddDocumentHistory(PpTorDraftDocumentType.Tor, defaultDocumentTemplateId);
        torDraftData.AddDocumentHistory(PpTorDraftDocumentType.Approval, defaultDocumentApproveTemplateId);
    }

    protected async Task<FileId?> UpdateDocumentHistoryAsync(
        PpTorDraft entity,
        PpTorDraftDocumentType documentType,
        FileId fileId,
        bool? isReplace = false,
        CancellationToken ct = default)
    {
        var latestHistory = entity.DocumentHistories
                                  .Where(d => d.DocumentType == documentType)
                                  .OrderVersions()
                                  .FirstOrDefault();

        if (latestHistory == null)
        {
            return null;
        }

        var newVersion = RunningDocumentVersion.IncrementDocumentVersion(
            latestHistory.Version,
            latestHistory.StatusState.ToString(),
            entity.Status.ToString());

        var documentService = this.Resolve<IDocumentService>();

        var copiedFileId = await documentService.CopyDocumentTemplateAsync(
            fileId,
            parentDirectory: $"{DocumentTemplateGroups.Tor}/{entity.Id}_{documentType}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
            cancellationToken: ct);

        if (!copiedFileId.HasValue)
        {
            return null;
        }

        entity.AddDocumentHistory(documentType, copiedFileId.Value, isReplace ?? false);

        var newHistory = entity.DocumentHistories
                               .Where(d => d.DocumentType == documentType)
                               .OrderVersions()
                               .First(h => h.FileId == copiedFileId.Value);
        newHistory.SetVersion(newVersion);

        return copiedFileId.Value;
    }

    protected async Task UpsertAssigneeAsync(
        PpTorDraft torDraft,
        AssigneeRequest[] assignee,
        UserId? sendToAcceptorId = null,
        CancellationToken cancellationToken = default)
    {
        // Get the user from the database
        var userIds = assignee.Map(a => a.UserId)
                              .Map(UserId.From)
                              .ToArray();

        var users =
            await this.dbContext.SuUsers
                      .Include(suUser => suUser.Employee)
                      .ThenInclude(rawEmployee => rawEmployee.View)
                      .Where(u => userIds.Contains(u.Id))
                      .ToArrayAsync(cancellationToken);

        var userExists = userIds.Except(users.Map(u => u.Id)).ToArray();

        if (userExists.Length != 0)
        {
            this.ThrowError(
                $"User with ID {string.Join(", ", userExists)} not found.",
                StatusCodes.Status404NotFound);
        }

        var requestAssignee =
            assignee.Join(
                        users,
                        a => UserId.From(a.UserId),
                        u => u.Id,
                        (a, u) =>
                        {
                            var assigneeEntity = a.Id.IsNull()
                                ? PpTorDraftAssignee.Create(
                                    a.AssigneeGroup,
                                    a.AssigneeType,
                                    u,
                                    a.Sequence)
                                : PpTorDraftAssignee.Create(
                                    PpTorDraftAssigneeId.From(a.Id.Value),
                                    a.AssigneeGroup,
                                    a.AssigneeType,
                                    u,
                                    a.Sequence);

                            return assigneeEntity;
                        })
                    .ToHashSet();

        if (torDraft.Assignees.IsNull())
        {
            _ = torDraft.SerAssigneesDefault();
        }

        // Remove assignees that are not in the request
        _ = torDraft.Assignees.Where(w => !requestAssignee.Select(s => s.Id).Contains(w.Id))
                    .Iter(s => torDraft.RemoveAssignee(s));

        var lastAssigneeUserId = assignee
            .Where(a => a.AssigneeType == AssigneeType.Assignee)
            .OrderByDescending(a => a.Sequence)
            .Select(a => (UserId?)UserId.From(a.UserId))
            .FirstOrDefault();

        var resolvedSendToAcceptorId = lastAssigneeUserId ?? sendToAcceptorId;

        // Update the existing assignees
        _ = torDraft.Assignees
                    .Join(
                        requestAssignee,
                        a => a.Id,
                        r => r.Id,
                        (a, r) =>
                        {
                            a.SetType(r.Type)
                             .SetUser(
                                 r.UserId,
                                 r.EmployeeCode,
                                 r.FullName,
                                 r.PositionName,
                                 r.BusinessUnitName)
                             .SetSequence(r.Sequence)
                             .SetSendToAcceptorId(resolvedSendToAcceptorId);

                            return a;
                        })
                    .ToHashSet();

        // Add new assignees
        _ = requestAssignee
            .Except(torDraft.Assignees)
            .Map(a =>
            {
                a.SetSendToAcceptorId(resolvedSendToAcceptorId);
                return torDraft.AddAssignee(a);
            })
            .ToHashSet();
    }

    protected TorDraftResponse MapToResponse(PpTorDraft entity)
    {
        var lastedApprovalHistory =
            entity.DocumentHistories
                  .Where(d => d.DocumentType == PpTorDraftDocumentType.Approval)
                  .OrderVersions()
                  .FirstOrDefault();

        var lastedTorDraftHistory =
            entity.DocumentHistories
                  .Where(d => d.DocumentType == PpTorDraftDocumentType.Tor)
                  .OrderVersions()
                  .FirstOrDefault();

        var torDocumentVersions = entity.DocumentHistories
                                        .Where(d => d.DocumentType == PpTorDraftDocumentType.Tor)
                                        .OrderVersions()
                                        .Select((d, index) => new DocumentVersionResponse(
                                            d.FileId.Value,
                                            d.Version,
                                            d.CreatedAt,
                                            d.CreatedByName ?? string.Empty,
                                            index == 0))
                                        .ToArray();

        var approvalDocumentVersions = entity.DocumentHistories
                                             .Where(d => d.DocumentType == PpTorDraftDocumentType.Approval)
                                             .OrderVersions()
                                             .Select((d, index) => new DocumentVersionResponse(
                                                 d.FileId.Value,
                                                 d.Version,
                                                 d.CreatedAt,
                                                 d.CreatedByName ?? string.Empty,
                                                 index == 0))
                                             .ToArray();

        var torDraftAcceptorApprover =
            entity.PpTorDraftAcceptors
                  .Where(a => a.Type != AcceptorType.TorDraftCommittee)
                  .Select(DelegatorExtensions.DelegatorToAcceptor)
                  .ToList();

        var torDraftAcceptorNotApprover =
            entity.PpTorDraftAcceptors
                  .Where(a => a.Type == AcceptorType.TorDraftCommittee)
                  .ToList();

        var torDraftAcceptors =
            torDraftAcceptorApprover
                .Union(torDraftAcceptorNotApprover)
                .Select(this.MapAcceptorResponse)
                .OrderBy(t => t.AcceptorType)
                .ThenBy(t => t.Sequence)
                .ToArray();

        var templateComputer = this.MapTemplateComputerResponse(entity.PpTorTemplateComputer, entity.PpTorTrainingItems, entity.PpTorImpediments);

        return new TorDraftResponse(
            entity.Id.Value,
            (string)entity.ReferenceNumber,
            entity.DocumentDate,
            entity.Telephone,
            entity.ProcurementId.Value,
            new ProcurementDto(
                entity.Procurement.PlanId.HasValue ? (Guid)entity.Procurement.PlanId : null,
                entity.Procurement.ProcurementNumber,
                entity.Procurement.Type,
                entity.Procurement.Step,
                entity.Procurement.Department.Name,
                entity.Procurement.DepartmentId,
                entity.Procurement.Plan?.PlanNumber.ToString() ?? string.Empty,
                entity.Procurement.Name,
                entity.Procurement.Budget,
                entity.Procurement.Budget.ThaiBahtText(),
                entity.Procurement.BudgetYear,
                entity.Procurement.SupplyMethod.Label,
                entity.Procurement.SupplyMethodCode,
                entity.Procurement.SupplyMethodType?.Label ?? string.Empty,
                entity.Procurement.SupplyMethodTypeCode,
                entity.Procurement.SupplyMethodSpecialType?.Label ?? string.Empty,
                entity.Procurement.SupplyMethodSpecialTypeCode,
                entity.Procurement.Status,
                entity.Procurement.ExpectingProcurementAt,
                entity.Procurement.IsStock,
                entity.Procurement.IsCommercialMaterial,
                entity.Procurement.Plan?.Type,
                entity.Procurement.ProcessType),
            entity.BidGuarantee,
            entity.IsStock,
            entity.Reason,
            entity.EvaluationCriteria,
            entity.IsChange,
            entity.IsCancel,
            entity.Status.ToString(),
            entity.IsActive,
            entity.DocumentTemplate?.Code,
            lastedTorDraftHistory?.FileId.Value,
            false,
            lastedApprovalHistory?.FileId.Value,
            false,
            torDocumentVersions,
            approvalDocumentVersions,
            [
                .. entity.PpTorDraftObjects
                         .Select(o => new TorDraftObjectResponse(
                             o.Id.Value,
                             o.Sequence,
                             o.Description))
                         .OrderBy(o => o.Sequence)
            ],
            [
                .. entity.PpTorDraftQualifications
                         .Select(q => new TorDraftQualificationResponse(
                             q.Id.Value,
                             q.Sequence,
                             q.Description))
                         .OrderBy(q => q.Sequence)
            ],
            [
                .. entity.PpTorDraftTechnicalSpecifications
                         .Select(t => new TorDraftTechnicalSpecificationResponse(
                             t.Id.Value,
                             t.Sequence,
                             t.Name,
                             t.Description,
                             t.Quantity,
                             t.UnitCode?.Value))
                         .OrderBy(o => o.Sequence)
            ],
            [
                .. entity.PpTorDraftTechnicalPeriods
                         .Select(t =>
                         {
                             if (t.PpTorDraftTechnicalPeriodDetails != null)
                             {
                                 return new TorDraftTechnicalPeriodResponse(
                                     t.Id.Value,
                                     t.Period,
                                     (string?)t.PeriodTypeCode ?? null,
                                     (string?)t.PeriodConditionCode ?? null,
                                     t.StartDate,
                                     t.EndDate,
                                     (string?)t.DeliveryConditionCode ?? null,
                                     t.DeliveryDate,
                                     [
                                         .. t.PpTorDraftTechnicalPeriodDetails
                                             .Select(d => new TorDraftTechnicalPeriodDetailResponse(
                                                 d.Id.Value,
                                                 d.Branch,
                                                 d.PersonalCount,
                                                 d.StartDate,
                                                 d.EndDate))
                                     ]);
                             }

                             return new TorDraftTechnicalPeriodResponse(
                                 t.Id.Value,
                                 t.Period,
                                 (string?)t.PeriodTypeCode ?? null,
                                 (string?)t.PeriodConditionCode ?? null,
                                 t.StartDate,
                                 t.EndDate,
                                 (string?)t.DeliveryConditionCode ?? null,
                                 t.DeliveryDate,
                                 null);
                         })
            ],
            [
                .. entity.PpTorDraftBudgets
                         .OrderBy(d => d.Sequence)
                         .Select(t =>
                         {
                             if (t.PpTorDraftBudgetDetails != null)
                             {
                                 return new TorDraftBudgetResponse(
                                     t.Id.Value,
                                     t.Sequence,
                                     t.Description,
                                     t.BudgetAmount,
                                     [
                                         .. t.PpTorDraftBudgetDetails
                                             .OrderBy(d => d.Sequence)
                                             .Select(d => new TorDraftBudgetDetailResponse(
                                                 d.Id.Value,
                                                 d.Sequence,
                                                 d.Department,
                                                 d.BudgetType,
                                                 d.ProjectCode,
                                                 d.AccountNo,
                                                 d.Budget))
                                             .OrderBy(d => d.Sequence)
                                     ]);
                             }

                             return new TorDraftBudgetResponse(
                                 t.Id.Value,
                                 t.Sequence,
                                 t.Description,
                                 t.BudgetAmount,
                                 null);
                         })
            ],
            [
                .. entity.PpTorDraftPaymentTerms
                         .Select(t =>
                         {
                             if (t.PpTorDraftPaymentTermDetails != null)
                             {
                                 return new TorDraftPaymentTermResponse(
                                     t.Id.Value,
                                     t.ProRateTypeCode?.ToString(),
                                     t.PaymentPercent,
                                     t.Description,
                                     t.Period,
                                     t.PeriodTypeCode?.ToString(),
                                     t.TotalPeriod,
                                     t.TotalPeriodTypeCode?.ToString(),
                                     t.IsMA,
                                     [
                                         .. t.PpTorDraftPaymentTermDetails
                                             .Select(d => new TorDraftPaymentTermDetailResponse(
                                                 d.Id.Value,
                                                 d.TermNumber,
                                                 d.Percent,
                                                 d.Period,
                                                 d.Description))
                                             .OrderBy(d => d.TermNumber)
                                     ]);
                             }

                             return new TorDraftPaymentTermResponse(
                                 t.Id.Value,
                                 t.ProRateTypeCode?.ToString(),
                                 t.PaymentPercent,
                                 t.Description,
                                 t.Period,
                                 t.PeriodTypeCode?.ToString(),
                                 t.TotalPeriod,
                                 t.TotalPeriodTypeCode?.ToString(),
                                 t.IsMA,
                                 null);
                         }).OrderBy(x => x.IsMA)
            ],
            [
                .. entity.PpTorDraftWarranties
                         .Select(t => new TorDraftWarrantyResponse(
                             t.Id.Value,
                             t.HasWarranty,
                             t.Period,
                             t.PeriodTypeCode?.Value ?? string.Empty,
                             t.ConditionOther))
            ],
            [
                .. entity.PpTorDraftFineRates
                         .Select(t => new TorDraftFineRateResponse(
                             t.Id.Value,
                             t.Sequence,
                             t.Description,
                             t.Rate,
                             t.PeriodTypeCode?.Value,
                             t.ConditionCode?.Value ?? null,
                             t.ConditionOther))
                         .OrderBy(t => t.Sequence)
            ],
            torDraftAcceptors,
            [
                .. entity.Assignees
                         .Select(DelegatorExtensions.DelegatorToAssignee)
                         .Select(this.MapAssignee).OrderBy(a => a.Sequence)
            ],
            entity.CancelReason,
            entity.ChangeReason,
            entity.IsContractGuarantee,
            entity.PercentageContract,
            entity.Procurement.SupplyMethodTypeCode.Value.ToString(),
            templateComputer.DocumentDescription,
            templateComputer.PreventiveMaintenance,
            templateComputer.CorrectiveMaintenance,
            templateComputer.Training,
            templateComputer.TrainingItems?.OrderBy(x => x.Sequence).ToArray(),
            templateComputer.ManuelDescription,
            templateComputer.Impediments?.OrderBy(x => x.Sequence).ToArray(),
            entity.IsMigration,
            entity.IsMA,
            entity.IsCM,
            entity.IsPM,
            entity.IsImpediment,
            entity.IsTraining,
            [
                .. entity.PpTorPaymentTermPeriods
                         .Select(p => new TorDraftPaymentTermPeriodsResponse(
                             p.Id.Value,
                             p.Sequence,
                             p.Description,
                             p.Quantity,
                             p.PeriodTypeCode?.Value?.ToString(),
                             p.TotalQuantity,
                             p.TotalPeriodTypeCode?.Value?.ToString()))
                         .OrderBy(t => t.Sequence)
            ]);
    }

    private TorTemplateComputerResponse MapTemplateComputerResponse(
        PpTorTemplateComputer? tpc,
        IEnumerable<PpTorTrainingItem>? trainingItems,
        IEnumerable<PpTorImpediment>? impediments)
    {
        var preventiveMaintenance =
            new TorPreventiveMaintenanceResponse(
                tpc?.PreventiveMaintenance?.PmProductName ?? string.Empty,
                tpc?.PreventiveMaintenance?.PmCount,
                tpc?.PreventiveMaintenance?.PmUnit != null ? (string)tpc.PreventiveMaintenance.PmUnit : null,
                tpc?.PreventiveMaintenance?.PmFinePct,
                tpc?.PreventiveMaintenance?.PmFineAmount,
                tpc?.PreventiveMaintenance?.Condition,
                tpc?.PreventiveMaintenance?.DisruptedCount,
                tpc?.PreventiveMaintenance?.PmUnit != null ? (string)tpc.PreventiveMaintenance.DisruptedCountUnit : null,
                tpc?.PreventiveMaintenance?.DisruptedPercent,
                tpc?.PreventiveMaintenance?.DisruptedFinePercent,
                tpc?.PreventiveMaintenance?.DisruptedFineAmount,
                tpc?.PreventiveMaintenance?.PmFinePctUnit != null ? (string)tpc.PreventiveMaintenance.PmFinePctUnit : null);

        var correctiveMaintenance =
            new TorCorrectiveMaintenanceResponse(
                tpc?.CorrectiveMaintenance?.CmProductName ?? string.Empty,
                tpc?.CorrectiveMaintenance?.StartDate,
                tpc?.CorrectiveMaintenance?.EndDate,
                tpc?.CorrectiveMaintenance?.CmCount,
                tpc?.CorrectiveMaintenance?.CmUnit != null ? (string)tpc.CorrectiveMaintenance.CmUnit : null,
                tpc?.CorrectiveMaintenance?.CmCompleteCount,
                tpc?.CorrectiveMaintenance?.CmCompleteUnit != null ? (string)tpc.CorrectiveMaintenance.CmCompleteUnit : null,
                tpc?.CorrectiveMaintenance?.CmFinePercent,
                tpc?.CorrectiveMaintenance?.CmDisruptedFinePercent,
                tpc?.CorrectiveMaintenance?.DayStart != null ? (string)tpc.CorrectiveMaintenance.DayStart : null,
                tpc?.CorrectiveMaintenance?.DayEnd != null ? (string)tpc.CorrectiveMaintenance.DayEnd : null,
                tpc?.CorrectiveMaintenance?.StartTime,
                tpc?.CorrectiveMaintenance?.EndTime,
                tpc?.CorrectiveMaintenance?.CmFinePercentUnit != null ? (string)tpc.CorrectiveMaintenance.CmFinePercentUnit : null);

        var rraining =
            new TorTrainingResponse(
                tpc?.Training?.TrainingCount,
                tpc?.Training?.TrainingCountUnit != null ? (string)tpc.Training.TrainingCountUnit : null,
                tpc?.Training?.TrainingUnitId != null ? (string)tpc.Training.TrainingUnitId : null);

        return new TorTemplateComputerResponse(
            tpc?.DocumentDescription,
            tpc?.ManuelDescription,
            preventiveMaintenance,
            correctiveMaintenance,
            rraining,
            trainingItems?.Select(MapTrainingItemResponse).ToArray(),
            impediments?.Select(MapImpedimentResponse).ToArray());
    }

    private static TorTrainingItemResponse MapTrainingItemResponse(PpTorTrainingItem item)
    {
        return new TorTrainingItemResponse(
            item.Id.Value,
            item.Sequence,
            item.CourseName,
            item.PeriodDay,
            item.Place,
            item.TrainingCount,
            item.TotalPersonPerTime);
    }

    private static TorImpedimentResponse MapImpedimentResponse(PpTorImpediment impediment)
    {
        return new TorImpedimentResponse(
            impediment.Id.Value,
            impediment.Sequence,
            impediment.Description,
            impediment.ImpedimentValue);
    }

    private TorDraftAcceptorResponse MapAcceptorResponse(PpTorDraftAcceptors t)
    {
        var departmentCode =
            t.User.Employee.PrimaryDepartment != null
                ? (string)t.User.Employee.PrimaryDepartment.Id
                : string.Empty;

        var committeePositionCode =
            t.CommitteePositionsCode.HasValue
                ? (string)t.CommitteePositionsCode
                : string.Empty;

        return new TorDraftAcceptorResponse(
            t.Id.Value,
            t.Type,
            t.UserId.Value,
            t.EmployeeCode.Value,
            t.FullName,
            t.PositionName,
            t.BusinessUnitName,
            t.Sequence,
            t.DelegateeId?.Value,
            t.Status,
            t.ActionAt,
            t.Remark,
            t.IsActive,
            t.IsCurrentApprover(),
            committeePositionCode,
            t.CommitteePosition?.Label,
            t.IsUnableToPerformDuties,
            departmentCode,
            t.Delegatee?.SuUserId.Value);
    }

    private AssigneeResponse MapAssignee(PpTorDraftAssignee assignee)
    {
        return new AssigneeResponse(
            assignee.Id.Value,
            assignee.Group,
            assignee.Type,
            assignee.UserId.Value,
            assignee.Sequence,
            assignee.FullName,
            assignee.PositionName,
            assignee.BusinessUnitName,
            assignee.Status,
            assignee.Remark,
            assignee.ActionAt,
            assignee.Delegatee?.SuUserId.Value);
    }

    protected async ValueTask UpdateAndReplaceDocumentAsync(
        PpTorDraft torDraft,
        TorDraftStatus oldStatus,
        PpAppoint? appoint,
        bool? isTorDraftDocumentIdReplaced,
        bool? isTorDraftApprovalDocumentIdReplaced,
        CancellationToken ct)
    {
        var documentService =
            this.Resolve<IDocumentService>();

        var lastedDraftTorDocument =
            torDraft.LastedDraftDocument(PpTorDraftDocumentType.Tor);

        var lastedDraftApprovalDocument =
            torDraft.LastedDraftDocument(PpTorDraftDocumentType.Approval);

        if (lastedDraftTorDocument is not null && lastedDraftApprovalDocument is not null)
        {
            var replaceDto =
                await this.MapToReplaceDto(torDraft, oldStatus, appoint, ct);

            if (torDraft.Status != TorDraftStatus.WaitingComment)
            {
                var torFileId =
                    await ReplaceDocument(
                        lastedDraftTorDocument.FileId,
                        isTorDraftDocumentIdReplaced ?? false,
                        PpTorDraftDocumentType.Tor);

                torDraft.AddDocumentHistory(PpTorDraftDocumentType.Tor, torFileId);
            }

            var approvalFileId =
                await ReplaceDocument(
                    lastedDraftApprovalDocument.FileId,
                    isTorDraftApprovalDocumentIdReplaced ?? false,
                    PpTorDraftDocumentType.Approval);

            torDraft.AddDocumentHistory(PpTorDraftDocumentType.Approval, approvalFileId);

            return;

            async Task<FileId> ReplaceDocument(
                FileId fileId,
                bool isReplace,
                PpTorDraftDocumentType documentType)
            {
                // Get original template (with placeholders) instead of LastedDraftDocument
                var templateFileId = documentType == PpTorDraftDocumentType.Tor
                    ? await this.GetDocumentTemplateByCode(torDraft.DocumentTemplate?.Code, ct)
                    : await this.GetDocumentApprovalTemplateByCriteria(
                        torDraft.Procurement.SupplyMethodCode,
                        torDraft.Procurement.HasMd,
                        torDraft.IsChange,
                        torDraft.IsCancel,
                        ct);

                var replaceDocumentAsync = isReplace switch
                {
                    true => documentService.CopyDocumentTemplateAsync(
                        templateFileId,
                        contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto),
                        parentDirectory: $"{DocumentTemplateGroups.Tor}/{torDraft.Id}_{documentType.ToString()}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
                        cancellationToken: ct),
                    false => documentService.CopyDocumentTemplateAsync(
                        fileId,
                        contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto),
                        parentDirectory: $"{DocumentTemplateGroups.Tor}/{torDraft.Id}_{documentType.ToString()}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
                        cancellationToken: ct),
                };

                var fileIdResult = await replaceDocumentAsync;

                if (fileIdResult is null)
                {
                    this.ThrowError("ไม่สามารถคัดลอกเอกสารร่างได้");
                }

                return (FileId)fileIdResult;
            }
        }
    }

    protected async ValueTask ReplaceDocumentsAsync(
        PpTorDraft torDraft,
        PpAppoint? appoint,
        CancellationToken ct,
        TorDraftStatus? previousStatus = null,
        bool isReplace = false)
    {
        var documentService = this.Resolve<IDocumentService>();
        var replaceDto = await this.MapToReplaceDto(torDraft, torDraft.Status, appoint, ct);
        var timeStamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var statusForReplace = previousStatus ?? torDraft.Status;

        await ReplaceDocumentAsync(PpTorDraftDocumentType.Tor);
        await ReplaceDocumentAsync(PpTorDraftDocumentType.Approval);

        async ValueTask ReplaceDocumentAsync(PpTorDraftDocumentType documentType)
        {
            var replaceTemplate = statusForReplace is TorDraftStatus.Rejected or TorDraftStatus.WaitingCommitteeApproval or TorDraftStatus.WaitingUnitApproval or TorDraftStatus.WaitingAssign
                or TorDraftStatus.WaitingComment
                ? torDraft.LastedNotReplacedDocument(documentType)
                : torDraft.LastedNotReplacedWaitingApprovalDocument(documentType);

            if (replaceTemplate is not null)
            {
                var finalFileId =
                    await documentService.CopyDocumentTemplateAsync(
                        replaceTemplate.FileId,
                        contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto),
                        parentDirectory: $"{DocumentTemplateGroups.Tor}/{torDraft.Id}_{documentType.ToString()}_{timeStamp}.odt",
                        cancellationToken: ct);

                if (finalFileId is null)
                {
                    this.ThrowError("ไม่สามารถคัดลอกเอกสารที่ส่งเห็นชอบ");
                }

                torDraft.AddDocumentHistory(documentType, finalFileId.Value, isReplace);
            }
        }
    }

    private async Task<SuUser?> GetLastActivityCreatedByAsync(
        string key,
        string type,
        CancellationToken ct)
    {
        var lastActivity =
            await this.dbContext.SuActivityLogs
                      .Where(l =>
                          l.Key == key &&
                          l.ActivityInfo.Type == type)
                      .OrderByDescending(l => l.AuditInfo.CreatedAt)
                      .FirstOrDefaultAsync(cancellationToken: ct);

        if (lastActivity is null)
        {
            return null;
        }

        var createByUser =
            await this.dbContext.SuUsers
                      .Include(u => u.Employee)
                      .ThenInclude(e => e.View)
                      .FirstOrDefaultAsync(
                          u => u.Id == UserId.From(lastActivity.AuditInfo.CreatedBy),
                          ct);

        return createByUser;
    }

    protected async Task<TorDraftReplace> MapToReplaceDto(
        PpTorDraft torDraft,
        TorDraftStatus oldStatus,
        PpAppoint? appoint,
        CancellationToken ct,
        bool isPreview = false)
    {
        var documentHistories = TorDraftEndpointBase<TRequest, TResponse>.GetDocumentHistories(torDraft);
        var creatorReplace = await this.GetCreatorReplaceAsync(torDraft, ct);
        var (committeesReplace, acceptorsReplace) = this.GetCommitteeAndAcceptorReplaces(torDraft, oldStatus, creatorReplace, isPreview);
        var parameters = await this.GetParametersAsync(ct);
        var procurementReplace = TorDraftEndpointBase<TRequest, TResponse>.MapProcurementReplace(torDraft);
        var torDraftComponents = this.MapTorDraftComponents(torDraft, parameters);
        var sectionApprove = await this.GetSectionApproveNameAsync(torDraft, ct);
        var additionalData = TorDraftEndpointBase<TRequest, TResponse>.GetAdditionalReplacementData(torDraft, parameters);

        return this.BuildTorDraftReplace(
            torDraft,
            appoint,
            documentHistories,
            creatorReplace,
            committeesReplace,
            acceptorsReplace,
            procurementReplace,
            torDraftComponents,
            sectionApprove,
            additionalData);
    }

    private static (PpTorDraftDocumentHistory? Approval, PpTorDraftDocumentHistory? TorDraft) GetDocumentHistories(PpTorDraft torDraft)
    {
        var lastedApprovalHistory = torDraft.DocumentHistories
                                            .Where(d => d.DocumentType == PpTorDraftDocumentType.Approval)
                                            .OrderVersions()
                                            .FirstOrDefault();

        var lastedTorDraftHistory = torDraft.DocumentHistories
                                            .Where(d => d.DocumentType == PpTorDraftDocumentType.Tor)
                                            .OrderVersions()
                                            .FirstOrDefault();

        return (lastedApprovalHistory, lastedTorDraftHistory);
    }

    private async Task<CreatorReplace?> GetCreatorReplaceAsync(PpTorDraft torDraft, CancellationToken ct)
    {
        var hasCreatorStatus = torDraft.Status is not (
            TorDraftStatus.Draft or
            TorDraftStatus.Edit or
            TorDraftStatus.Rejected);

        if (!hasCreatorStatus)
        {
            return null;
        }

        var sendToCommitteeApproveByUser = await this.GetLastActivityCreatedByAsync(
            torDraft.Id.ToString(),
            ActivityLogActionTypeConstant.SendCommitteeApprove,
            ct);

        if (sendToCommitteeApproveByUser is null)
        {
            return null;
        }

        return new CreatorReplace(
            sendToCommitteeApproveByUser.Id.Value,
            "ผู้จัดทำ",
            sendToCommitteeApproveByUser.FullName,
            sendToCommitteeApproveByUser.Employee.View?.FullPositionName ?? string.Empty,
            sendToCommitteeApproveByUser.Employee.View?.BusinessUnitId.Value ?? string.Empty);
    }

    private (List<TorCommitteeReplace> Committees, List<TorAcceptorReplace> Acceptors) GetCommitteeAndAcceptorReplaces(
        PpTorDraft torDraft,
        TorDraftStatus oldStatus,
        CreatorReplace? creatorReplace,
        bool isPreview)
    {
        var committeesReplace = new List<TorCommitteeReplace>();
        var acceptorsReplace = new List<TorAcceptorReplace>();

        if (isPreview)
        {
            return (committeesReplace, acceptorsReplace);
        }

        committeesReplace = oldStatus is not (TorDraftStatus.Edit or TorDraftStatus.Draft or TorDraftStatus.Rejected)
                                ? this.MapCommitteesReplace(torDraft, creatorReplace)
                                : [];

        acceptorsReplace = TorDraftEndpointBase<TRequest, TResponse>.MapAcceptorsReplace(torDraft);

        return (committeesReplace, acceptorsReplace);
    }

    private List<TorCommitteeReplace> MapCommitteesReplace(
        PpTorDraft torDraft,
        CreatorReplace? creatorReplace)
    {
        var creatorUserId = creatorReplace?.UserId;

        var committeeCount = torDraft.PpTorDraftAcceptors
                                     .Count(a => a.Type == AcceptorType.TorDraftCommittee);

        var committeesReplace = torDraft.PpTorDraftAcceptors
                                        .Where(a => a is
                                        {
                                            Type: AcceptorType.TorDraftCommittee,
                                        })
                                        .OrderBy(a => a.Sequence)
                                        .Select(MapCommitteeReplace)
                                        .ToList();

        return committeesReplace;
    }

    private static TorCommitteeReplace MapCommitteeReplace(
        PpTorDraftAcceptors acceptor)
    {
        var action =
            acceptor.Status switch
            {
                AcceptorStatus.Approved => "เห็นชอบ",
                AcceptorStatus.Rejected => "ไม่เห็นชอบ",
                AcceptorStatus.UnableToPerformDuties => acceptor.Remark ?? string.Empty,
                _ => string.Empty,
            };

        var positionOnBoardLabel = acceptor.CommitteePosition?.Label ?? string.Empty;

        return new TorCommitteeReplace(
            acceptor.UserId.Value,
            acceptor.Sequence,
            action,
            acceptor.FullName,
            acceptor.FullName,
            acceptor.User.Employee.View?.FullPositionName ?? string.Empty,
            positionOnBoardLabel,
            string.Empty);
    }

    private static List<TorAcceptorReplace> MapAcceptorsReplace(PpTorDraft torDraft)
    {
        var acceptorsReplace = new List<TorAcceptorReplace>();
        var acceptors = torDraft.PpTorDraftAcceptors
                                .Where(a => a is { Type: AcceptorType.Approver })
                                .Select(DelegatorExtensions.DelegatorToAcceptor)
                                .OrderBy(a => a.Sequence)
                                .ToList();

        for (int acceptorIndex = 0; acceptorIndex < acceptors.Count; acceptorIndex++)
        {
            var acceptor = acceptors[acceptorIndex];

            if (acceptor.Status == AcceptorStatus.Approved)
            {
                var actionLabel = acceptorIndex == acceptors.Count - 1 ? "อนุมัติ" : "เห็นชอบ";
                acceptorsReplace.Add(MapAcceptorReplace(acceptor, actionLabel));
            }
        }

        return acceptorsReplace;
    }

    private static TorAcceptorReplace MapAcceptorReplace(PpTorDraftAcceptors acceptor, string actionLabel)
    {
        return new TorAcceptorReplace(
            acceptor.UserId.Value,
            acceptor.Sequence,
            actionLabel,
            acceptor.FullName,
            acceptor.FullName,
            acceptor.PositionName ?? string.Empty,
            string.Empty,
            string.Empty);
    }

    private async Task<SuParameter[]> GetParametersAsync(CancellationToken ct)
    {
        return await this.dbContext.SuParameters
                         .AsNoTracking()
                         .Where(su =>
                             su.GroupCode == GroupCode.From(SuParameterGroupCodeConstant.UnitOfMeasures) ||
                             su.GroupCode == GroupCode.From(SuParameterGroupCodeConstant.PeriodType) ||
                             su.GroupCode == GroupCode.From(SuParameterGroupCodeConstant.PeriodCondition) ||
                             su.GroupCode == GroupCode.From(SuParameterGroupCodeConstant.FineType) ||
                             su.GroupCode == GroupCode.From(SuParameterGroupCodeConstant.MaintenancePeriodType) ||
                             su.GroupCode == GroupCode.From(SuParameterGroupCodeConstant.CriteriaCons) ||
                             su.GroupCode == GroupCode.From(SuParameterGroupCodeConstant.PTimeType) ||
                             su.GroupCode == GroupCode.From(SuParameterGroupCodeConstant.DelvCUnit) ||
                             su.GroupCode == GroupCode.From(SuParameterGroupCodeConstant.DOW) ||
                             su.GroupCode == GroupCode.From(SuParameterGroupCodeConstant.DWCUnit) ||
                             su.GroupCode == GroupCode.From(SuParameterGroupCodeConstant.CMFineType))
                         .ToArrayAsync(ct);
    }

    private static ProcurementReplaceDto MapProcurementReplace(PpTorDraft torDraft)
    {
        var procurementNumber = torDraft.Procurement.ProcurementNumber.HasValue
            ? torDraft.Procurement.ProcurementNumber.Value.ToString()
            : string.Empty;

        return new ProcurementReplaceDto(
            torDraft.Procurement.PlanId.HasValue ? (Guid)torDraft.Procurement.PlanId : null,
            procurementNumber,
            torDraft.Procurement.Type,
            torDraft.Procurement.Step,
            torDraft.Procurement.Department.Name,
            torDraft.Procurement.DepartmentId,
            torDraft.Procurement.Plan.PlanNumber.ToString(),
            torDraft.Procurement.Name,
            (torDraft.Procurement.Budget ?? 0).ToCurrencyStringWithComma(),
            torDraft.Procurement.Budget.ThaiBahtText(),
            torDraft.Procurement.BudgetYear,
            torDraft.Procurement.SupplyMethod.Label,
            torDraft.Procurement.SupplyMethodCode,
            torDraft.Procurement.SupplyMethodType?.Label ?? string.Empty,
            torDraft.Procurement.SupplyMethodTypeCode,
            torDraft.Procurement.SupplyMethodSpecialType?.Label ?? string.Empty,
            torDraft.Procurement.SupplyMethodSpecialTypeCode,
            torDraft.Procurement.Status,
            torDraft.Procurement.ExpectingProcurementAt,
            torDraft.Procurement.IsStock,
            torDraft.Procurement.IsCommercialMaterial,
            torDraft.Procurement.Plan.Type,
            torDraft.Procurement.ProcessType);
    }

    private TorDraftComponentsData MapTorDraftComponents(PpTorDraft torDraft, SuParameter[] parameters)
    {
        var torDraftObjects = TorDraftEndpointBase<TRequest, TResponse>.MapTorDraftObjects(torDraft);
        var torDraftQualifications = TorDraftEndpointBase<TRequest, TResponse>.MapTorDraftQualifications(torDraft);
        var torDraftTechnicalSpecifications = TorDraftEndpointBase<TRequest, TResponse>.MapTorDraftTechnicalSpecifications(torDraft, parameters);
        var torDraftTechnicalPeriods = this.MapTorDraftTechnicalPeriods(torDraft);
        var technicalPeriod = TorDraftEndpointBase<TRequest, TResponse>.MapTechnicalPeriodSummary(torDraftTechnicalPeriods, parameters);
        var torDraftBudgets = this.MapTorDraftBudgets(torDraft);
        var torDraftBudgetSummary = TorDraftEndpointBase<TRequest, TResponse>.MapTorDraftBudgetSummary(torDraftBudgets);
        var torDraftPaymentTerms = this.MapTorDraftPaymentTerms(torDraft, parameters);
        var torDraftWarranties = this.MapTorDraftWarranties(torDraft, parameters);
        var torDraftFineRates = this.MapTorDraftFineRates(torDraft, parameters);
        var torPreventiveMaintenance = TorDraftEndpointBase<TRequest, TResponse>.MapPreventiveMaintenanceReplace(torDraft?.PpTorTemplateComputer?.PreventiveMaintenance, parameters);
        var torCorrectiveMaintenance = TorDraftEndpointBase<TRequest, TResponse>.MapCorrectiveMaintenanceReplace(torDraft?.PpTorTemplateComputer?.CorrectiveMaintenance, parameters);
        var torTraining = TorDraftEndpointBase<TRequest, TResponse>.MapTrainingReplace(torDraft?.PpTorTemplateComputer?.Training, parameters);
        var torTrainingItem = this.MapTorTrainingItems(torDraft);
        var torImpediments = this.MapTorImpediments(torDraft);
        var paymentTerm = torDraft?.PpTorDraftPaymentTerms?
                                  .Where(x => x.ProRateTypeCode.HasValue == false || x.ProRateTypeCode.Value.ToString() != SplitPaymentConstant.SplitPayment003)
                                  .Select(x => TorDraftEndpointBase<TRequest, TResponse>.MapPaymentTerm(x, parameters))
                                  .FirstOrDefault();
        var maPaymentTerm = torDraft?.PpTorDraftPaymentTerms?
                                    .Where(x => x.ProRateTypeCode.HasValue && x.ProRateTypeCode.Value.ToString() == SplitPaymentConstant.SplitPayment003)
                                    .Select(x => TorDraftEndpointBase<TRequest, TResponse>.MapMaPaymentTerm(x, parameters))
                                    .FirstOrDefault();

        return new TorDraftComponentsData(
            torDraftObjects,
            torDraftQualifications,
            torDraftTechnicalSpecifications,
            torDraftTechnicalPeriods,
            technicalPeriod,
            torDraftBudgets,
            torDraftBudgetSummary,
            torDraftPaymentTerms,
            torDraftWarranties,
            torDraftFineRates,
            torPreventiveMaintenance,
            torCorrectiveMaintenance,
            torTraining,
            torImpediments,
            torTrainingItem,
            paymentTerm,
            maPaymentTerm);
    }

    private static TorTrainingReplace MapTrainingReplace(TorTraining? training, SuParameter[] parameters)
    {
        return new TorTrainingReplace(
            training?.TrainingCount?.ToString() ?? "-",
            training?.TrainingCountUnit?.Value != null ? parameters.FirstOrDefault(su => su.Code == training.TrainingCountUnit.Value)?.Label ?? "-" : "-",
            training?.TrainingUnitId?.Value != null ? parameters.FirstOrDefault(su => su.Code == training.TrainingUnitId.Value)?.Label ?? "-" : "-");
    }

    private static TorPreventiveMaintenanceReplace MapPreventiveMaintenanceReplace(TorPreventiveMaintenance? pm, SuParameter[] parameters)
    {
        return new TorPreventiveMaintenanceReplace(
            pm?.PmProductName ?? "-",
            pm?.PmCount?.ToString() ?? "-",
            pm?.PmCount?.ToThaiNumberText() ?? "-",
            pm?.PmUnit?.Value != null ? parameters.FirstOrDefault(su => su.Code == pm.PmUnit.Value)?.Label ?? "-" : "-",
            pm?.PmFinePct?.ToString() ?? "-",
            pm?.PmFinePctUnit?.Value != null ? parameters.FirstOrDefault(su => su.Code == pm.PmFinePctUnit.Value)?.Label ?? "-" : "-",
            pm?.PmFineAmount?.ToCurrencyStringWithComma().ToString() ?? "-",
            pm?.PmFineAmount?.ThaiBahtText() ?? "-",
            pm?.Condition ?? "-",
            pm?.DisruptedCount?.ToString() ?? "-",
            pm?.DisruptedCount?.ToThaiNumberText() ?? "-",
            pm?.DisruptedCountUnit?.Value != null ? parameters.FirstOrDefault(su => su.Code == pm.DisruptedCountUnit.Value)?.Label ?? "-" : "-",
            pm?.DisruptedPercent?.ToString() ?? "-",
            pm?.DisruptedPercent?.ToThaiNumberText() ?? "-",
            pm?.DisruptedFinePercent?.ToString() ?? "-",
            pm?.DisruptedFineAmount?.ToCurrencyStringWithComma().ToString() ?? "-",
            pm?.DisruptedFineAmount?.ThaiBahtText() ?? "-");
    }

    private static TorCorrectiveMaintenanceReplace MapCorrectiveMaintenanceReplace(TorCorrectiveMaintenance? cm, SuParameter[] parameters)
    {
        var startTime = string.Format("{0} - {1} น.", cm?.StartTime ?? "-", cm?.EndTime ?? "-");

        return new TorCorrectiveMaintenanceReplace(
            cm?.DayStart?.Value != null ? parameters.FirstOrDefault(su => su.Code == cm.DayStart.Value)?.Label ?? "-" : "-",
            cm?.DayEnd?.Value != null ? parameters.FirstOrDefault(su => su.Code == cm.DayEnd.Value)?.Label ?? "-" : "-",
            startTime,
            cm?.CmCount?.ToString() ?? "-",
            cm?.CmCount?.ToThaiNumberText() ?? "-",
            cm?.CmUnit?.Value != null ? parameters.FirstOrDefault(su => su.Code == cm.CmUnit.Value)?.Label ?? "-" : "-",
            cm?.CmCompleteCount?.ToString() ?? "-",
            cm?.CmCompleteCount?.ToThaiNumberText() ?? "-",
            cm?.CmCompleteUnit?.Value != null ? parameters.FirstOrDefault(su => su.Code == cm.CmCompleteUnit.Value)?.Label ?? "-" : "-",
            cm?.CmFinePercent?.ToString() ?? "-",
            cm?.CmFinePercent?.ToThaiNumberText() ?? "-",
            cm?.CmDisruptedFinePercent?.ToString() ?? "-",
            cm?.CmFinePercentUnit?.Value != null ? parameters.FirstOrDefault(su => su.Code == cm.CmFinePercentUnit.Value)?.Label ?? "-" : "-");
    }

    private TorDraftTrainingItemReplace[] MapTorTrainingItems(PpTorDraft? torDraft)
    {
        if (torDraft?.PpTorTrainingItems != null && torDraft.PpTorTrainingItems.Any())
        {
            return
            [
                .. torDraft.PpTorTrainingItems
                           .Select(t => TorDraftEndpointBase<TRequest, TResponse>.MapTrainingItem(t))
                           .OrderBy(t => t.Sequence)
            ];
        }

        return new[]
        {
            new TorDraftTrainingItemReplace(
                1,
                "-",
                "-",
                "-",
                "-",
                "-"),
        };
    }

    private static TorDraftTrainingItemReplace MapTrainingItem(PpTorTrainingItem t)
    {
        return new TorDraftTrainingItemReplace(
            t.Sequence ?? 1,
            t.CourseName ?? "-",
            t.PeriodDay?.ToString() ?? "-",
            t.Place ?? "-",
            t.TrainingCount?.ToString() ?? "-",
            t.TotalPersonPerTime?.ToString() ?? "-");
    }

    private TorDraftImpedimentReplace[] MapTorImpediments(PpTorDraft? torDraft)
    {
        if (torDraft?.PpTorImpediments != null && torDraft.PpTorImpediments.Any())
        {
            return
            [
                .. torDraft.PpTorImpediments
                           .Select(t => TorDraftEndpointBase<TRequest, TResponse>.MapImpediment(t))
                           .OrderBy(t => t.Sequence)
            ];
        }

        return new[]
        {
            new TorDraftImpedimentReplace(
                1,
                "-",
                "-"),
        };
    }

    private static TorDraftImpedimentReplace MapImpediment(PpTorImpediment t)
    {
        return new TorDraftImpedimentReplace(
            t.Sequence ?? 1,
            t.Description ?? "-",
            t.ImpedimentValue?.ToCurrencyStringWithComma().ToString() ?? "-");
    }

    private async Task<SectionApproverDto> GetSectionApproveNameAsync(PpTorDraft torDraft, CancellationToken ct)
    {
        var committeesInProcurementDepartment = torDraft.PpTorDraftAcceptors
                                                        .Where(a => a is { Type: AcceptorType.TorDraftCommittee, CommitteePositionsCode: not null } &&
                                                                    a.User.Employee.View?.BusinessUnitId == torDraft.Procurement.DepartmentId)
                                                        .ToArray();

        var jorPorDirector = await this.operationService.GetDefaultJorPorDirectorAsync(ct);

        var minCommitteePosition = committeesInProcurementDepartment
                                   .OrderByDescending(cd => cd.CommitteePositionsCode.Value.Value)
                                   .ThenByDescending(cd => cd.Sequence)
                                   .FirstOrDefault()?
                                   .UserId.Value
                                   ?? torDraft.PpTorDraftAcceptors
                                              .Where(a => a.Type == AcceptorType.TorDraftCommittee)?
                                              .MaxBy(a => a.Sequence)?
                                              .UserId.Value;

        if (torDraft.Procurement.HasMd && jorPorDirector is null)
        {
            this.ThrowError("ไม่พบผู้อำนวยการฝ่ายจัดหาและการพัสดุ (จพ.)", StatusCodes.Status400BadRequest);
        }

        var operationUserId = torDraft.Procurement.HasMd
            ? jorPorDirector!.UserId.Value
            : minCommitteePosition!.Value;

        var processType = torDraft.Procurement.HasMd ? SectionProcessType.TORHasMD : SectionProcessType.TOR;
        var isStock = torDraft.Procurement.IsStock;
        var isCommercialMaterial = torDraft.Procurement.IsCommercialMaterial;

        if (torDraft.Procurement.SupplyMethodCode.Value == SupplyMethodConstant.Eighty)
        {
            if (isStock)
            {
                processType = SectionProcessType.TORStock;
            }
            else if (isCommercialMaterial)
            {
                processType = torDraft.Procurement.HasMd ? SectionProcessType.TORCommercialParcelHasMD : SectionProcessType.TORCommercialParcel;
            }
        }

        var managers = await this.operationService.GetDefaultAcceptorPositionIgnorePrefixAsync(
            processType,
            operationUserId,
            torDraft.Procurement.Budget ?? 0,
            torDraft.Procurement.SupplyMethodCode.Value,
            torDraft.Procurement.SupplyMethodCode.Value is SupplyMethodConstant.Eighty ? default : (string?)torDraft.Procurement.SupplyMethodSpecialTypeCode,
            ct);

        var positionNamePrefix = this.operationService.AddPositionNamePrefix(managers);
        var result = positionNamePrefix.Select(m => new SectionApprove(m.PositionName))
                                       .DefaultIfEmpty(new SectionApprove(string.Empty));

        var commandNumber = managers.FirstOrDefault()?.CommandNumber;

        var commandText = this.commandTextService
                              .GetCommandText(
                                  CommandTextProgram.TorDraft,
                                  managers,
                                  torDraft.Procurement.SupplyMethodCode,
                                  torDraft.Procurement.Budget ?? 0,
                                  supplyMethodSpecialType: torDraft.Procurement.SupplyMethodSpecialTypeCode,
                                  supplyMethodSpecialName: torDraft.Procurement.SupplyMethodSpecialType?.Label,
                                  commandNumber: commandNumber);

        return new SectionApproverDto(result, commandText);
    }

    private static AdditionalReplacementData GetAdditionalReplacementData(PpTorDraft torDraft, SuParameter[] parameters)
    {
        var memorandumDate = torDraft.Procurement.Appoints.FirstOrDefault()!
                                     .MemorandumDate.ToThaiDateString(thaiNumber: false);

        var evaluationCriteria = torDraft.EvaluationCriteria == null
            ? string.Empty
            : parameters.FirstOrDefault(su => su.Code == ParameterCode.From(torDraft.EvaluationCriteria))?.Label ?? string.Empty;

        var torDraftDate = torDraft.Date == DateTimeOffset.MinValue ? DateTimeOffset.UtcNow : torDraft.Date;

        var torDraftCommitteeIsCreator = torDraft.Procurement.Appoints.FirstOrDefault()?
            .TorDraftCommittees.Any(c => c.CommitteePositionsCode == SuParameterCodeConstant.PosBoard006) ?? false;

        var torDraftCommitteeType = torDraftCommitteeIsCreator
            ? "ผู้จัดทำร่างขอบเขตงาน (TOR)"
            : "คณะกรรมการจัดทำร่างขอบเขตงาน (TOR)";

        return new AdditionalReplacementData(memorandumDate, evaluationCriteria, torDraftDate, torDraftCommitteeType);
    }

    private TorDraftReplace
        BuildTorDraftReplace(
            PpTorDraft torDraft,
            PpAppoint? appoint,
            (PpTorDraftDocumentHistory? Approval, PpTorDraftDocumentHistory? TorDraft) documentHistories,
            CreatorReplace? creatorReplace,
            List<TorCommitteeReplace> committeesReplace,
            List<TorAcceptorReplace> acceptorsReplace,
            ProcurementReplaceDto procurementReplace,
            TorDraftComponentsData torDraftComponents,
            SectionApproverDto sectionApprove,
            AdditionalReplacementData additionalData)
    {
        var committeeTorIsCreator =
            appoint?.TorDraftCommittees
                   ?.Any(c => c.CommitteePositionsCode == SuParameterCodeConstant.PosBoard006)
            ?? false;

        var committeeMedianPriceIsCreator =
            appoint?.MedianPriceCommittees
                   ?.Any(c => c.CommitteePositionsCode == SuParameterCodeConstant.PosBoard006)
            ?? false;

        var committeeTorSection =
            committeeTorIsCreator ? "ผู้จัดทำ" : "คณะกรรมการ";

        var committeeMedianPriceSection =
            committeeMedianPriceIsCreator ? "ผู้จัดทำ" : "คณะกรรมการ";

        var firstPeriod = torDraft.PpTorDraftTechnicalPeriods.FirstOrDefault();

        string deliveryDescription = string.Empty;

        if (firstPeriod != null)
        {
            var conditionCode = firstPeriod.DeliveryConditionCode?.ToString() ?? string.Empty;

            if (conditionCode == "DelvCUnit005")
            {
                deliveryDescription = $"วันที่ {firstPeriod.DeliveryDate?.ToThaiDateString()}";
            }
            else
            {
                deliveryDescription = $"{firstPeriod.Period} {firstPeriod.PeriodType?.Label} {firstPeriod.DeliveryCondition?.Label}";
            }
        }

        var committees = (torDraft.Status is TorDraftStatus.Draft
            or TorDraftStatus.Rejected
            or TorDraftStatus.Edit)
            ? []
            : committeesReplace;

        var acceptors = (torDraft.Status is TorDraftStatus.Draft
            or TorDraftStatus.Rejected
            or TorDraftStatus.Edit
            or TorDraftStatus.WaitingCommitteeApproval
            or TorDraftStatus.RejectToAssignee
            or TorDraftStatus.WaitingComment)
            ? []
            : acceptorsReplace;

        var lastAssignee = (torDraft.Status is TorDraftStatus.WaitingComment or TorDraftStatus.WaitingApproval or TorDraftStatus.Approved)
            ? torDraft.Assignees
                      .Where(a => a.Type == AssigneeType.Assignee)
                      .Select(DelegatorExtensions.DelegatorToAssignee)
                      .OrderBy(a => a.Sequence)
                      .LastOrDefault()
            : null;

        var jorPorCommentReplace = lastAssignee is not null
            ? new JorPorCommentReplace(
                lastAssignee.UserId.Value,
                lastAssignee.FullName,
                lastAssignee.FullName,
                lastAssignee.PositionName,
                lastAssignee.Remark,
                "ผู้จัดทำ")
            : null;

        var acceptorDate = torDraft.Status is not (
                TorDraftStatus.Draft or
                TorDraftStatus.Rejected or
                TorDraftStatus.Edit)
            ? torDraft.DocumentDate?.ToThaiDateString() ?? DateTimeOffset.UtcNow.ToThaiDateString()
            : null;

        return new TorDraftReplace(
            acceptorDate,
            (string)torDraft.ReferenceNumber,
            (string)appoint?.AppointNumber,
            sectionApprove.SectionApprove,
            additionalData.MemorandumDate,
            additionalData.TorDraftCommitteeType,
            sectionApprove.CommandText,
            torDraft.Telephone ?? string.Empty,
            torDraft.ProcurementId.Value,
            procurementReplace,
            torDraft.BidGuarantee ?? false,
            torDraft.IsStock,
            torDraft.Reason,
            additionalData.EvaluationCriteria,
            torDraft.IsChange,
            torDraft.IsCancel,
            torDraft.Status.ToString(),
            torDraft.IsActive,
            torDraft.DocumentTemplate?.Code ?? string.Empty,
            documentHistories.TorDraft?.FileId.Value,
            documentHistories.Approval?.FileId.Value,
            torDraftComponents.TorDraftObjects,
            torDraftComponents.TorDraftQualifications,
            torDraftComponents.TorDraftTechnicalSpecifications,
            torDraftComponents.TorDraftTechnicalPeriods,
            torDraftComponents.TechnicalPeriod,
            torDraftComponents.TorDraftBudgets,
            torDraftComponents.TorDraftBudgetSummary,
            torDraftComponents.TorDraftPaymentTerms,
            torDraftComponents.PaymentTerm,
            torDraftComponents.MaPaymentTerm,
            torDraftComponents.TorDraftWarranties,
            torDraftComponents.TorDraftWarranties.FirstOrDefault(),
            torDraftComponents.TorDraftFineRates.First(),
            torDraftComponents.TorDraftFineRates.Last(),
            [.. torDraft.Assignees.Select(this.MapAssignee).OrderBy(a => a.Sequence)],
            torDraft.CancelReason,
            torDraft.ChangeReason,
            creatorReplace,
            [.. committees],
            [.. acceptors],
            torDraft.PpTorTemplateComputer?.DocumentDescription ?? string.Empty,
            torDraft.PpTorTemplateComputer?.ManuelDescription ?? string.Empty,
            torDraftComponents.PreventiveMaintenance,
            torDraftComponents.CorrectiveMaintenance,
            torDraftComponents.Training,
            torDraftComponents.Impediments,
            torDraftComponents.TrainingItem,
            committeeTorSection,
            committeeMedianPriceSection,
            deliveryDescription,
            jorPorCommentReplace);
    }

    private static TorDraftObjectReplace[] MapTorDraftObjects(PpTorDraft torDraft)
    {
        return
        [
            .. torDraft.PpTorDraftObjects
                       .Select(o => new TorDraftObjectReplace(o.Sequence ?? 0, o.Description ?? string.Empty))
                       .OrderBy(o => o.Sequence)
        ];
    }

    private static TorDraftQualificationReplace[] MapTorDraftQualifications(PpTorDraft torDraft)
    {
        return
        [
            .. torDraft.PpTorDraftQualifications
                       .Select(q => new TorDraftQualificationReplace(q.Sequence ?? 0, q.Description ?? string.Empty))
                       .OrderBy(q => q.Sequence)
        ];
    }

    private static TorDraftTechnicalSpecificationReplace[] MapTorDraftTechnicalSpecifications(PpTorDraft torDraft, SuParameter[] parameters)
    {
        return
        [
            .. torDraft.PpTorDraftTechnicalSpecifications
                       .Select(t =>
                       {
                           var unitOfMeasuresLabel = parameters.FirstOrDefault(u => u.Code == t.UnitCode)?.Label ?? string.Empty;

                           var technicalSpecificationLabel = $"{t.Name} จำนวน {t.Quantity?.ToString("N0")} {unitOfMeasuresLabel}\n\t - {t.Description}".Trim();

                           return new TorDraftTechnicalSpecificationReplace(
                               t.Sequence ?? 0,
                               t.Name ?? string.Empty,
                               t.Description ?? string.Empty,
                               t.Quantity?.ToString("N0") ?? "0",
                               t.UnitCode?.Value ?? string.Empty,
                               unitOfMeasuresLabel,
                               technicalSpecificationLabel);
                       })
                       .OrderBy(o => o.Sequence)
        ];
    }

    private TorDraftTechnicalPeriodReplace[] MapTorDraftTechnicalPeriods(PpTorDraft torDraft)
    {
        return [.. torDraft.PpTorDraftTechnicalPeriods.Select(t => TorDraftEndpointBase<TRequest, TResponse>.MapTechnicalPeriod(t))];
    }

    private static TorDraftTechnicalPeriodReplace MapTechnicalPeriod(PpTorDraftTechnicalPeriod t)
    {
        if (t.PpTorDraftTechnicalPeriodDetails != null)
        {
            var details = t.PpTorDraftTechnicalPeriodDetails
                           .Select(d => new TorDraftTechnicalPeriodDetailReplace(d.Branch ?? string.Empty, d.PersonalCount ?? string.Empty, d.StartDate ?? default, d.EndDate ?? default))
                           .ToArray();

            return new TorDraftTechnicalPeriodReplace(
                t.Period, (string?)t.PeriodTypeCode, (string?)t.PeriodConditionCode, t.StartDate, t.EndDate, t.DeliveryDate, details);
        }

        return new TorDraftTechnicalPeriodReplace(
            t.Period, (string?)t.PeriodTypeCode, (string?)t.PeriodConditionCode, t.StartDate, t.EndDate, t.DeliveryDate, null);
    }

    private static TorDraftTechnicalPeriodSummaryReplace MapTechnicalPeriodSummary(TorDraftTechnicalPeriodReplace[] torDraftTechnicalPeriods, SuParameter[] parameters)
    {
        var firstTechnicalPeriod = torDraftTechnicalPeriods.FirstOrDefault();

        return new TorDraftTechnicalPeriodSummaryReplace(
            firstTechnicalPeriod?.Period?.ToString() ?? string.Empty,
            parameters.FirstOrDefault(su => su.Code == firstTechnicalPeriod?.PeriodTypeCode)?.Label ?? string.Empty,
            parameters.FirstOrDefault(su => su.Code == firstTechnicalPeriod?.PeriodConditionCode)?.Label ?? string.Empty,
            firstTechnicalPeriod?.StartDate.ToThaiDateString(includeBuddhistEra: false),
            firstTechnicalPeriod?.EndDate.ToThaiDateString(includeBuddhistEra: false),
            firstTechnicalPeriod?.DeliveryDate.ToThaiDateString(includeBuddhistEra: false));
    }

    private TorDraftBudgetReplace[] MapTorDraftBudgets(PpTorDraft torDraft)
    {
        return [.. torDraft.PpTorDraftBudgets.OrderBy(d => d.Sequence).Select(t => TorDraftEndpointBase<TRequest, TResponse>.MapTorDraftBudget(t))];
    }

    private static TorDraftBudgetReplace MapTorDraftBudget(PpTorDraftBudget t)
    {
        if (t.PpTorDraftBudgetDetails != null)
        {
            var details = t.PpTorDraftBudgetDetails
                           .OrderBy(d => d.Sequence)
                           .Select(d => new TorDraftBudgetDetailReplace(
                               d.Sequence ?? 0, d.Department ?? string.Empty, d.BudgetType ?? string.Empty, d.ProjectCode, d.AccountNo ?? string.Empty, d.Budget ?? 0m))
                           .ToArray();

            return new TorDraftBudgetReplace(t.Sequence ?? 0, t.Description ?? string.Empty, t.BudgetAmount ?? 0m, (t.BudgetAmount ?? 0m).ThaiBahtText(), details);
        }

        return new TorDraftBudgetReplace(t.Sequence ?? 0, t.Description ?? string.Empty, t.BudgetAmount ?? 0m, (t.BudgetAmount ?? 0m).ThaiBahtText(), null);
    }

    private static TorDraftBudgetSummaryReplace MapTorDraftBudgetSummary(TorDraftBudgetReplace[] torDraftBudgets)
    {
        var budgetAmount = torDraftBudgets.Sum(b => b.BudgetAmount);

        return new TorDraftBudgetSummaryReplace(budgetAmount.ToCurrencyStringWithComma(), budgetAmount.ThaiBahtText());
    }

    private TorDraftPaymentTermReplace[] MapTorDraftPaymentTerms(PpTorDraft torDraft, SuParameter[] parameters)
    {
        return
        [
            .. torDraft.PpTorDraftPaymentTerms
                       .Where(t => t.ProRateTypeCode.HasValue == false || t.ProRateTypeCode.Value.ToString() != SplitPaymentConstant.SplitPayment003)
                       .Select(t => TorDraftEndpointBase<TRequest, TResponse>.MapPaymentTerm(t, parameters))
        ];
    }

    private static TorDraftPaymentTermReplace MapPaymentTerm(PpTorDraftPaymentTerm t, SuParameter[] parameters)
    {
        var periodTypeLabel = parameters.FirstOrDefault(su => su.Code == t.PeriodTypeCode)?.Label ?? string.Empty;
        var totalPeriodTypeLabel = parameters.FirstOrDefault(su => su.Code == t.TotalPeriodTypeCode)?.Label ?? string.Empty;

        var proRateTypeCode = t.ProRateTypeCode.HasValue ? t.ProRateTypeCode.Value.ToString() : null;

        var orderedDetails = t.PpTorDraftPaymentTermDetails?
                              .OrderBy(d => d.TermNumber)
                              .ToArray();

        var details = orderedDetails?
                      .Select((d, index) => new TorDraftPaymentTermDetailReplace(
                          d.TermNumber ?? 0,
                          d.Percent ?? 0m,
                          d.Period ?? 0,
                          d.Description ?? string.Empty,
                          string.Format("งวดที่ {0} จำนวน {1}% ของมูลค่าตามสัญญา", d.TermNumber, FormatPercent(d.Percent ?? 0m)),
                          PaymentTermDetailsDescription(proRateTypeCode, d.TermNumber ?? 0, d.Percent ?? 0m, d.Description ?? string.Empty, index == orderedDetails.Length - 1)))
                      .ToArray();

        return new TorDraftPaymentTermReplace(
            t.ProRateTypeCode.HasValue ? t.ProRateTypeCode.Value.ToString() : string.Empty,
            t.PaymentPercent,
            t.Period,
            t.PeriodTypeCode.HasValue ? t.PeriodTypeCode.Value.ToString() : string.Empty,
            periodTypeLabel ?? string.Empty,
            t.TotalPeriod,
            t.TotalPeriodTypeCode.HasValue ? t.TotalPeriodTypeCode.Value.ToString() : string.Empty,
            totalPeriodTypeLabel ?? string.Empty,
            t.Description,
            details,
            []);
    }

    private static TorDraftPaymentTermReplace MapMaPaymentTerm(PpTorDraftPaymentTerm t, SuParameter[] parameters)
    {
        var periodTypeLabel = parameters.FirstOrDefault(su => su.Code == t.PeriodTypeCode)?.Label ?? string.Empty;
        var totalPeriodTypeLabel = parameters.FirstOrDefault(su => su.Code == t.TotalPeriodTypeCode)?.Label ?? string.Empty;

        return new TorDraftPaymentTermReplace(
            t.ProRateTypeCode.HasValue ? t.ProRateTypeCode.Value.ToString() : string.Empty,
            t.PaymentPercent,
            t.Period,
            t.PeriodTypeCode.HasValue ? t.PeriodTypeCode.Value.ToString() : string.Empty,
            periodTypeLabel ?? string.Empty,
            t.TotalPeriod,
            t.TotalPeriodTypeCode.HasValue ? t.TotalPeriodTypeCode.Value.ToString() : string.Empty,
            totalPeriodTypeLabel ?? string.Empty,
            t.Description,
            null,
            []);
    }

    private static string FormatPercent(decimal percent)
    {
        var formatted = percent.ToString("0.000").TrimEnd('0');

        if (formatted.EndsWith('.'))
        {
            formatted = formatted.TrimEnd('.');
        }
        else if (formatted.IndexOf('.') >= 0 && formatted.Length - formatted.IndexOf('.') - 1 < 2)
        {
            formatted = formatted.PadRight(formatted.IndexOf('.') + 3, '0');
        }

        return formatted;
    }

    private static string PaymentTermDetailsDescription(string? proRateTypeCode, int termNumber, decimal percent, string description, bool isLastItem = false)
    {
        var formattedPercent = FormatPercent(percent);
        var termLabel = isLastItem && proRateTypeCode == "SplitPayment001"
            ? $"งวดที่ {termNumber} (งวดสุดท้าย)"
            : $"งวดที่ {termNumber}";

        return proRateTypeCode switch
        {
            "SplitPayment001" =>
                $"ธนาคารจะดำเนินการชำระเงิน {termLabel} จำนวน {formattedPercent}% ของมูลค่าตามสัญญา หลังจากธนาคารได้รับมอบพัสดุ ถูกต้องครบถ้วน พร้อมทั้งธนาคารได้ตรวจรับพัสดุถูกต้องแล้ว ดังนี้\t\n{description}",
            "SplitPayment002" =>
                $"ธนาคารจะดำเนินการชำระเงิน งวดเดียวจำนวน {formattedPercent}% ของมูลค่าตามสัญญา หลังจากธนาคารได้รับมอบพัสดุ ถูกต้องครบถ้วน พร้อมทั้งธนาคารได้ตรวจรับพัสดุถูกต้องแล้ว ดังนี้\t\n{description}",
            _ => string.Empty,
        };
    }

    private TorDraftWarrantyReplace[] MapTorDraftWarranties(PpTorDraft torDraft, SuParameter[] parameters)
    {
        var warranties = torDraft.PpTorDraftWarranties
                                 .Select(t => TorDraftEndpointBase<TRequest, TResponse>.MapWarranty(t, parameters))
                                 .ToArray();

        if (!warranties.Any())
        {
            return new[]
            {
                new TorDraftWarrantyReplace(false, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty),
            };
        }

        return warranties;
    }

    private static TorDraftWarrantyReplace MapWarranty(PpTorDraftWarranty t, SuParameter[] parameters)
    {
        var periodTypeLabel = parameters.FirstOrDefault(su => su.Code == t.PeriodTypeCode)?.Label ?? string.Empty;
        var conditionTypeLabel = parameters.FirstOrDefault(su => su.Code == t.ConditionOther)?.Label ?? string.Empty;

        return new TorDraftWarrantyReplace(
            t.HasWarranty ?? false,
            t.Period.ToString(),
            t.PeriodTypeCode?.Value ?? string.Empty,
            periodTypeLabel,
            t.ConditionOther,
            conditionTypeLabel);
    }

    private TorDraftFineRateReplace[] MapTorDraftFineRates(PpTorDraft torDraft, SuParameter[] parameters)
    {
        if (torDraft.PpTorDraftFineRates.Any())
        {
            return
            [
                .. torDraft.PpTorDraftFineRates
                           .Select(t => TorDraftEndpointBase<TRequest, TResponse>.MapFineRate(t, parameters))
                           .OrderBy(t => t.Sequence)
            ];
        }

        return new[]
        {
            new TorDraftFineRateReplace(
                0,
                string.Empty,
                "0.00",
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty),
        };
    }

    private static TorDraftFineRateReplace MapFineRate(PpTorDraftFineRate t, SuParameter[] parameters)
    {
        var periodTypeLabel = parameters.FirstOrDefault(su => su.Code == t.PeriodTypeCode)?.Label ?? string.Empty;
        var conditionLabel = parameters.FirstOrDefault(su => su.Code == t.ConditionCode)?.Label ?? string.Empty;

        if (t.ConditionCode.HasValue && t.ConditionCode.Value == SuParameterCodeConstant.FineTypeOtherCondition)
        {
            conditionLabel = t.ConditionOther ?? string.Empty;
        }

        return new TorDraftFineRateReplace(
            t.Sequence ?? 0,
            t.Description ?? string.Empty,
            t.Rate.GetValueOrDefault() > 0 ? $"{t.Rate:0.00#}" : "0.00",
            t.PeriodTypeCode?.Value ?? string.Empty,
            periodTypeLabel ?? string.Empty,
            t.ConditionCode?.Value ?? string.Empty,
            conditionLabel ?? string.Empty,
            t.ConditionOther);
    }

    private record SectionApproverDto(IEnumerable<SectionApprove> SectionApprove, string CommandText);

    private record TorDraftComponentsData(
        TorDraftObjectReplace[] TorDraftObjects,
        TorDraftQualificationReplace[] TorDraftQualifications,
        TorDraftTechnicalSpecificationReplace[] TorDraftTechnicalSpecifications,
        TorDraftTechnicalPeriodReplace[] TorDraftTechnicalPeriods,
        TorDraftTechnicalPeriodSummaryReplace TechnicalPeriod,
        TorDraftBudgetReplace[] TorDraftBudgets,
        TorDraftBudgetSummaryReplace TorDraftBudgetSummary,
        TorDraftPaymentTermReplace[] TorDraftPaymentTerms,
        TorDraftWarrantyReplace[] TorDraftWarranties,
        TorDraftFineRateReplace[] TorDraftFineRates,
        TorPreventiveMaintenanceReplace? PreventiveMaintenance,
        TorCorrectiveMaintenanceReplace? CorrectiveMaintenance,
        TorTrainingReplace? Training,
        TorDraftImpedimentReplace[]? Impediments,
        TorDraftTrainingItemReplace[]? TrainingItem,
        TorDraftPaymentTermReplace? PaymentTerm,
        TorDraftPaymentTermReplace? MaPaymentTerm);

    private record AdditionalReplacementData(string MemorandumDate, string EvaluationCriteria, DateTimeOffset? TorDraftDate, string TorDraftCommitteeType);
}