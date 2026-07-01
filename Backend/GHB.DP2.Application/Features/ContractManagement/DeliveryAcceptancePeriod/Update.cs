namespace GHB.DP2.Application.Features.ContractManagement.DeliveryAcceptancePeriod;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Contracts.ValueObjects;
using FluentValidation;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.ContractManagement.DeliveryAcceptancePeriod.Abstract;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement;
using GHB.DP2.Application.Features.Procurement.PrincipleApproval.Dto;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractManagement.CaContractDraftVendorEdit;
using GHB.DP2.Domain.ContractManagement.CmDeliveryAcceptance;
using GHB.DP2.Domain.Plan;
using GHB.DP2.Domain.Procurement.PPurchaseOrderApproval;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record UpdateDeliveryAcceptancePeriodRequest(
    Guid DeliveryAcceptanceId,
    Guid Id,
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    CmDeliveryAcceptancePeriodStatus Status,
    IEnumerable<AcceptorRequest> AcceptanceCommittees,
    IEnumerable<AssigneeRequest> Assignees,
    IEnumerable<AcceptorRequest> Acceptors,
    IEnumerable<AcceptorRequest> AcceptanceOfAccounting,
    IEnumerable<AcceptorRequest> AcceptanceConfirmers,
    Guid? DocumentId,
    bool? IsDocumentReplaced,
    BudgetDetail[] BudgetDetails,
    string? AcceptanceNumber,
    string? Description,
    string? PhoneNumber,
    decimal? ContractBudgetAmount,
    string? ObjectiveDescription,
    bool HasDeduction,
    string? DeductionDescription,
    decimal? DeductionAmount,
    bool HasInvoiceSlip,
    string? InvoiceSlipDescription,
    decimal? InvoiceSlipAmount,
    CmDeliveryAcceptancePeriodAccountStatus AccountStatus,
    DateTimeOffset? DisbursementDate,
    decimal? DisbursementAmount,
    string? DisbursementRemark,
    GetById.Cm001PaymentTermResponse[] PaymentTerms,
    GetById.InspectionCommitteeSectionResponse? InspectionCommittees,
    DateTimeOffset? DocumentDate = null);

public record UpdateDeliveryAcceptancePeriodResponse(Guid Id, Guid? NewDocumentFileId);

public class UpdateDeliveryAcceptancePeriodRequestValidator : Validator<UpdateDeliveryAcceptancePeriodRequest>
{
    public UpdateDeliveryAcceptancePeriodRequestValidator()
    {
        this.RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("ต้องมีข้อมูลการส่งมอบตรวจรับ");

        this.RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage("สถานะไม่ถูกต้อง");

        this.When(x => x.Status == CmDeliveryAcceptancePeriodStatus.WaitingCommitteeApproval, () =>
        {
            this.RuleFor(r => r.AcceptanceCommittees)
                .Must(x => x.Any(acceptorRequest => acceptorRequest.AcceptorType is AcceptorType.AcceptanceCommittee))
                .WithMessage("ต้องมีบุคคล/คณะกรรมการตรวจรับพัสดุ 1 คน");
        });

        this.When(x => x.Status == CmDeliveryAcceptancePeriodStatus.WaitingAcceptance, () =>
        {
            this.RuleFor(r => r.Acceptors)
                .Must(x => x.Any(acceptorRequest => acceptorRequest.AcceptorType is AcceptorType.Approver))
                .WithMessage("ต้องมีผู้มีอำนาจเห็นชอบ/อนุมัติอย่างน้อย 1 คน");
        });
    }
}

public class UpdateDeliveryAcceptancePeriodEndpoint
    : DeliveryAcceptancePeriodEndpointBase<UpdateDeliveryAcceptancePeriodRequest, Results<Ok<UpdateDeliveryAcceptancePeriodResponse>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public UpdateDeliveryAcceptancePeriodEndpoint(
        Dp2DbContext dbContext,
        ILogger<UpdateDeliveryAcceptancePeriodEndpoint> logger,
        IOperationService operationService,
        ICommandTextService commandTextService)
        : base(dbContext, logger, operationService, commandTextService)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b => b.WithTags("ContractManagement/DeliveryAcceptance/Period"));
        this.Put("delivery-acceptance/{DeliveryAcceptanceId:guid?}/period/{Id:guid}");
    }

    protected override async ValueTask<Results<Ok<UpdateDeliveryAcceptancePeriodResponse>, NotFound<string>>> HandleRequestAsync(
        UpdateDeliveryAcceptancePeriodRequest req,
        CancellationToken ct)
    {
        var periodExisting = await this.ValidateRequestAsync(req, ct);

        FileId? newDocumentFileId = await this.ProcessDeliveryAcceptancePeriodAsync(periodExisting, req, ct);

        return TypedResults.Ok(new UpdateDeliveryAcceptancePeriodResponse(periodExisting.Id.Value, newDocumentFileId?.Value));
    }

    private async Task<FileId?> ProcessDeliveryAcceptancePeriodAsync(
        CmDeliveryAcceptancePeriod periodExisting,
        UpdateDeliveryAcceptancePeriodRequest req,
        CancellationToken ct)
    {
        var previousStatus = periodExisting.Status;
        var previousAccountStatus = periodExisting.AccountStatus;
        var incomingAssignees = req.Assignees?.ToArray() ?? [];

        var previousAcceptorIds = periodExisting.Acceptors.Select(a => a.Id.Value).ToHashSet();

        var keptAcceptorIds = req.Acceptors
            .Concat(req.AcceptanceCommittees)
            .Concat(req.AcceptanceOfAccounting)
            .Concat(req.AcceptanceConfirmers)
            .Where(a => a.Id.HasValue)
            .Select(a => a.Id!.Value)
            .ToHashSet();

        var removedAcceptors = periodExisting.Acceptors
            .Where(a => !keptAcceptorIds.Contains(a.Id.Value) &&
                        a.Type is AcceptorType.AccountingOperator or AcceptorType.Accounting)
            .ToList();

        var previousFirstPendingUserId = periodExisting.Acceptors
            .Where(a => a.Type is AcceptorType.AccountingOperator or AcceptorType.Accounting)
            .OrderBy(a => a.Type == AcceptorType.AccountingOperator ? 0 : 1)
            .ThenBy(a => a.Sequence)
            .FirstOrDefault(a => a.Status == AcceptorStatus.Pending)
            ?.UserId;

        var newConfirmerUserIds = req.AcceptanceConfirmers
            .Where(a => !a.Id.HasValue)
            .Select(a => UserId.From(a.UserId))
            .ToList();

        await this.UpdateEntityDataAsync(periodExisting, req, ct);

        await SendAccountingNotificationsAsync(periodExisting, previousFirstPendingUserId, newConfirmerUserIds);

        var addedAcceptors = periodExisting.Acceptors
            .Where(a => !previousAcceptorIds.Contains(a.Id.Value) &&
                        a.Type is AcceptorType.AccountingOperator or AcceptorType.Accounting)
            .ToList();

        HandleStatusChange(periodExisting, req);

        foreach (var removed in removedAcceptors)
        {
            periodExisting.AddActivity(new ActivityInfo(
                ActivityLogActionTypeConstant.RemoveAcceptor,
                ActivityLogActionTypeConstant.RemoveAcceptor,
                nameof(periodExisting.Acceptors),
                removed.FullName));
        }

        foreach (var added in addedAcceptors)
        {
            periodExisting.AddActivity(new ActivityInfo(
                ActivityLogActionTypeConstant.AddAcceptor,
                ActivityLogActionTypeConstant.AddAcceptor,
                nameof(periodExisting.Acceptors),
                added.FullName));
        }

        if (req.Status == CmDeliveryAcceptancePeriodStatus.WaitingCommitteeApproval || req.DocumentDate is not null)
        {
            periodExisting.SetDocumentDate(req.DocumentDate);
        }

        await this.ProcessNotificationsAsync(periodExisting, previousStatus, incomingAssignees);

        FileId? newDocumentFileId = await this.ProcessDocumentUpdatesAsync(periodExisting, req, ct);

        await this.dbContext.SaveChangesAsync(ct);

        if (req.AccountStatus == CmDeliveryAcceptancePeriodAccountStatus.Paid &&
            previousAccountStatus != CmDeliveryAcceptancePeriodAccountStatus.Paid)
        {
            await this.SendPaidNotificationsAsync(periodExisting);
        }

        return newDocumentFileId;
    }

    private static async Task SendAccountingNotificationsAsync(
        CmDeliveryAcceptancePeriod periodExisting,
        UserId? previousFirstPendingUserId,
        List<UserId> newConfirmerUserIds)
    {
        if (periodExisting.Status == CmDeliveryAcceptancePeriodStatus.Approved &&
            periodExisting.AccountStatus == CmDeliveryAcceptancePeriodAccountStatus.WaitingAccountingApproval)
        {
            var accountingAcceptors = periodExisting.Acceptors
                .Where(a => a.Type is AcceptorType.AccountingOperator or AcceptorType.Accounting)
                .OrderBy(a => a.Type == AcceptorType.AccountingOperator ? 0 : 1)
                .ThenBy(a => a.Sequence)
                .ToList();

            var firstPending = accountingAcceptors.FirstOrDefault(a => a.Status == AcceptorStatus.Pending);

            // AccountingOperator is already notified by NotifyNewAccountingOperatorsAsync (WaitForVerify).
            // Only notify Accounting (approver) type here to avoid double notification.
            if (firstPending != null &&
                firstPending.UserId != previousFirstPendingUserId &&
                firstPending.Type != AcceptorType.AccountingOperator)
            {
                var isLastPending = accountingAcceptors.Count(a => a.Status == AcceptorStatus.Pending) == 1;
                var title = isLastPending
                    ? NotificationConstant.WaitForApprove.Title
                    : NotificationConstant.WaitForLike.Title;
                var message = string.Format(
                    isLastPending
                        ? NotificationConstant.WaitForApprove.Message
                        : NotificationConstant.WaitForLike.Message,
                    ProgramConstant.ContractAcceptancePeriod.Name,
                    periodExisting.AcceptanceNumber);

                foreach (var targetUserId in firstPending.GetNotificationTargets())
                {
                    await SendNotificationAsync(periodExisting, targetUserId, title, message);
                }
            }
        }

        if (newConfirmerUserIds.Count > 0)
        {
            var confirmerTargets = periodExisting.Acceptors
                .Where(a => a.Type == AcceptorType.AccountingConfirmer &&
                            newConfirmerUserIds.Contains(a.UserId))
                .SelectMany(a => a.GetNotificationTargets());

            foreach (var targetUserId in confirmerTargets)
            {
                await SendNotificationAsync(
                    periodExisting,
                    targetUserId,
                    NotificationConstant.WaitConfirmDisbursement.Title,
                    string.Format(
                        NotificationConstant.WaitConfirmDisbursement.Message,
                        ProgramConstant.ContractAcceptancePeriod.Name,
                        periodExisting.AcceptanceNumber));
            }
        }
    }

    private async Task SendPaidNotificationsAsync(CmDeliveryAcceptancePeriod periodExisting)
    {
        periodExisting.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.DisbursementPaid,
                string.Empty,
                nameof(CmDeliveryAcceptancePeriodAccountStatus.Paid)));

        await this.dbContext.SaveChangesAsync(CancellationToken.None);

        var committeeMembers = periodExisting.Acceptors
                                             .Where(a => a.Type == AcceptorType.AcceptanceCommittee && a.IsActive)
                                             .ToList();

        var paymentTermMerge = string.Join(",", periodExisting.PaymentTerms.Select(x => x.PaymentTerm));

        foreach (var member in committeeMembers)
        {
            await SendNotificationAsync(
                periodExisting,
                member.UserId,
                NotificationConstant.DisbursementPaid.Title,
                string.Format(
                    NotificationConstant.DisbursementPaid.Message,
                    ProgramConstant.ContractAcceptancePeriod.Name,
                    $"{periodExisting.AcceptanceNumber} {paymentTermMerge}"));
        }

        if (!committeeMembers.Any(x => x.UserId == periodExisting.AuditInfo.CreatedBy))
        {
            var creatorUserId = periodExisting.AuditInfo.CreatedBy;

            await SendNotificationAsync(
                periodExisting,
                UserId.From(creatorUserId),
                NotificationConstant.DisbursementPaid.Title,
                string.Format(
                    NotificationConstant.DisbursementPaid.Message,
                    ProgramConstant.ContractAcceptancePeriod.Name,
                    $"{periodExisting.AcceptanceNumber} {paymentTermMerge}"));
        }
    }

    private async Task ProcessNotificationsAsync(CmDeliveryAcceptancePeriod periodExisting, CmDeliveryAcceptancePeriodStatus previousStatus, AssigneeRequest[] incomingAssignees)
    {
        if (periodExisting.Status == CmDeliveryAcceptancePeriodStatus.WaitingCommitteeApproval && previousStatus != CmDeliveryAcceptancePeriodStatus.WaitingCommitteeApproval)
        {
            var committeeMembers = periodExisting.Acceptors
                                                 .Where(a => a.Type == AcceptorType.AcceptanceCommittee && a.IsActive && !a.IsUnableToPerformDuties)
                                                 .OrderBy(a => a.Sequence)
                                                 .ToList();

            var chairman = committeeMembers.FirstOrDefault(a => a.CommitteePositionsCode != null && a.CommitteePositionsCode == ParameterCode.From("PosBoard001"))
                           ?? committeeMembers.FirstOrDefault(a => a.IsBoardChairman());

            var nonChair = committeeMembers.Where(a => chairman == null || a.Id != chairman.Id).ToList();

            if (nonChair.Count == 0 && chairman != null)
            {
                await SendNotificationAsync(
                    periodExisting,
                    chairman.UserId,
                    NotificationConstant.WaitForLike.Title,
                    string.Format(NotificationConstant.WaitForLike.Message, ProgramConstant.ContractAcceptancePeriod.Name, periodExisting.AcceptanceNumber));
            }
            else
            {
                foreach (var member in nonChair)
                {
                    await SendNotificationAsync(
                        periodExisting,
                        member.UserId,
                        NotificationConstant.WaitForLike.Title,
                        string.Format(NotificationConstant.WaitForLike.Message, ProgramConstant.ContractAcceptancePeriod.Name, periodExisting.AcceptanceNumber));
                }
            }
        }

        if (periodExisting.Status == CmDeliveryAcceptancePeriodStatus.WaitingAssign)
        {
            var newAssigneeUserIds = incomingAssignees
                                     .Where(a => !a.Id.HasValue)
                                     .Select(a => UserId.From(a.UserId))
                                     .ToHashSet();

            if (newAssigneeUserIds.Any())
            {
                var newAssignees = periodExisting.Assignees.Where(a => newAssigneeUserIds.Contains(a.UserId)).ToList();

                foreach (var targetUserId in newAssignees.SelectMany(a => a.GetAssigneeNotificationTargets()))
                {
                    await SendNotificationAsync(
                        periodExisting,
                        targetUserId,
                        NotificationConstant.Assignment.Title,
                        string.Format(NotificationConstant.Assignment.Message, ProgramConstant.ContractAcceptancePeriod.Name, periodExisting.AcceptanceNumber));
                }
            }
        }

        if (periodExisting.Status == CmDeliveryAcceptancePeriodStatus.WaitingAssign && previousStatus != CmDeliveryAcceptancePeriodStatus.WaitingAssign)
        {
            var directors = periodExisting.Assignees.Where(a => a.Type == AssigneeType.Director).ToList();

            foreach (var targetUserId in directors.SelectMany(d => d.GetAssigneeNotificationTargets()))
            {
                await SendNotificationAsync(
                    periodExisting,
                    targetUserId,
                    NotificationConstant.WaitForAssignment.Title,
                    string.Format(NotificationConstant.WaitForAssignment.Message, ProgramConstant.ContractAcceptancePeriod.Name, periodExisting.AcceptanceNumber));
            }
        }

        if (periodExisting.Status == CmDeliveryAcceptancePeriodStatus.WaitingAcceptance && previousStatus != CmDeliveryAcceptancePeriodStatus.WaitingAcceptance)
        {
            var acceptors = periodExisting.Acceptors
                                          .Where(a => a.Type == AcceptorType.Approver && a.IsActive && !a.IsUnableToPerformDuties)
                                          .OrderBy(a => a.Sequence)
                                          .ToList();

            foreach (var targetUserId in acceptors.SelectMany(a => a.GetNotificationTargets()))
            {
                await SendNotificationAsync(
                    periodExisting,
                    targetUserId,
                    NotificationConstant.WaitForLike.Title,
                    string.Format(NotificationConstant.WaitForLike.Message, ProgramConstant.ContractAcceptancePeriod.Name, periodExisting.AcceptanceNumber));
            }
        }

        if (ShouldSendAssigneeNotification(periodExisting.Status))
        {
            _ = SendNotificationAssigneeAsync(periodExisting, CancellationToken.None);
        }
    }

    private async Task UpdateEntityDataAsync(
        CmDeliveryAcceptancePeriod periodExisting,
        UpdateDeliveryAcceptancePeriodRequest req,
        CancellationToken ct)
    {
        this.UpsertBudgets(periodExisting, req.BudgetDetails ?? []);
        this.UpsertPaymentTerms(periodExisting, req.PaymentTerms ?? []);

        periodExisting.SetValue(
            req.HasDeduction,
            req.DeductionDescription,
            req.DeductionAmount,
            req.HasInvoiceSlip,
            req.InvoiceSlipDescription,
            req.InvoiceSlipAmount,
            req.PhoneNumber,
            req.ObjectiveDescription,
            req.ContractBudgetAmount,
            req.Description);

        periodExisting.AccountingUpdateValue(
            req.DisbursementDate,
            req.DisbursementAmount,
            req.DisbursementRemark);

        var acceptanceCommittees = req.InspectionCommittees is not null
            ? req.InspectionCommittees.Committees
                 .Select(x =>
                 {
                     var matching = req.AcceptanceCommittees
                                       .FirstOrDefault(a => a.UserId == x.UserId);

                     return new AcceptorRequest(
                         x.Id,
                         AcceptorType.AcceptanceCommittee,
                         x.UserId,
                         x.Sequence,
                         x.CommitteePositionsCode,
                         matching?.IsUnableToPerformDuties);
                 })
                 .ToArray()
            : req.AcceptanceCommittees.ToArray();

        await this.UpsertAcceptorAsync(
            periodExisting,
            [.. req.Acceptors, .. acceptanceCommittees, .. req.AcceptanceOfAccounting, .. req.AcceptanceConfirmers],
            req.Status,
            ct,
            UserId.From(req.UserId));

        await this.UpsertAssigneeAsync(
            periodExisting,
            [.. req.Assignees],
            ct,
            UserId.From(req.UserId));
    }

    private static void HandleStatusChange(
        CmDeliveryAcceptancePeriod periodExisting,
        UpdateDeliveryAcceptancePeriodRequest req)
    {
        if (periodExisting.Status == req.Status && req.AccountStatus == periodExisting.AccountStatus)
        {
            periodExisting.AddActivity(
                new ActivityInfo(
                    ActivityLogActionTypeConstant.Update,
                    string.Empty,
                    nameof(periodExisting.Status)));
        }
        else if (periodExisting.Status != req.Status && req.AccountStatus == periodExisting.AccountStatus)
        {
            periodExisting.UpdateStatus(req.Status);
        }
        else if (periodExisting.Status == req.Status && req.AccountStatus != periodExisting.AccountStatus)
        {
            periodExisting.AccountingUpdateStatus(
                req.AccountStatus);
        }
    }

    private static bool ShouldSendAssigneeNotification(CmDeliveryAcceptancePeriodStatus status)
    {
        return status == CmDeliveryAcceptancePeriodStatus.WaitingComment ||
               status == CmDeliveryAcceptancePeriodStatus.WaitingAcceptance;
    }

    private async Task<FileId?> ProcessDocumentUpdatesAsync(
        CmDeliveryAcceptancePeriod periodExisting,
        UpdateDeliveryAcceptancePeriodRequest req,
        CancellationToken ct)
    {
        Plan? planData = null;
        CaContractDraftVendor? contractDraftVendorData = null;
        Domain.Procurement.Procurement? procurementData = null;

        var hasJorPorAssign = await this.HasJorPorAssign(periodExisting, req.UserId, ct);

        if (periodExisting.CmDeliveryAcceptance.SourceType == SourceType.Plan)
        {
            planData = await this.dbContext.Plans.FirstOrDefaultAsync(a => a.Id == PlanId.From((Guid)periodExisting.CmDeliveryAcceptance.RefId), ct);

            if (planData is null)
            {
                this.ThrowError("ไม่พบแผนจัดซื้อจัดจ้าง");
            }
        }
        else if (periodExisting.CmDeliveryAcceptance.SourceType == SourceType.ContractDraftVendor)
        {
            contractDraftVendorData = await this.dbContext.CaContractDraftVendors
                                                .Include(x => x.ContractDraft)
                                                .ThenInclude(x => x.Procurement)
                                                .Include(x => x.ContractInvitationVendors)
                                                .FirstOrDefaultAsync(x => x.Id == ContractDraftVendorId.From((Guid)periodExisting.CmDeliveryAcceptance.RefId), ct);

            if (contractDraftVendorData is null)
            {
                this.ThrowError("ไม่พบร่างสัญญาของผู้ขาย");
            }

            procurementData = contractDraftVendorData.ContractDraft.Procurement;
        }
        else if (periodExisting.CmDeliveryAcceptance.SourceType == SourceType.Procurement)
        {
            var poaData = await this.dbContext.PPurchaseOrderApprovals
                                    .Include(poa => poa.Procurement)
                                    .FirstOrDefaultAsync(poa => poa.Id == PurchaseOrderApprovalId.From((Guid)periodExisting.CmDeliveryAcceptance.RefId), ct);

            if (poaData is null)
            {
                this.ThrowError("ไม่พบข้อมูลใบสั่งซื้อ/จ้าง/เช่า");
            }

            procurementData = poaData.Procurement;
        }
        else if (periodExisting.CmDeliveryAcceptance.SourceType == SourceType.ContractDraftVendorEdit)
        {
            var vendorEditUpdate = await this.dbContext.CaContractDraftVendorEdits
                .FirstOrDefaultAsync(ve => ve.Id == ContractDraftVendorEditId.From((Guid)periodExisting.CmDeliveryAcceptance.RefId), ct);

            if (vendorEditUpdate is null)
            {
                this.ThrowError("ไม่พบข้อมูลบันทึกต่อท้ายสัญญา");
            }

            contractDraftVendorData = await this.dbContext.CaContractDraftVendors
                .Include(v => v.ContractDraft).ThenInclude(cd => cd.Procurement)
                .Include(v => v.ContractInvitationVendors)
                .FirstOrDefaultAsync(v => v.Id == vendorEditUpdate.ContractDraftVendorId, ct);

            if (contractDraftVendorData is null)
            {
                this.ThrowError("ไม่พบข้อมูลสัญญาต้นฉบับ");
            }

            procurementData = contractDraftVendorData.ContractDraft.Procurement;
        }

        var rawSupplyMethodCode = periodExisting.CmDeliveryAcceptance.SourceType switch
        {
            SourceType.Plan => planData!.SupplyMethodCode,
            SourceType.ContractDraftVendor => contractDraftVendorData!.ContractDraft.Procurement.SupplyMethodCode,
            SourceType.Procurement => procurementData!.SupplyMethodCode,
            SourceType.ContractDraftVendorEdit => procurementData!.SupplyMethodCode,
            SourceType.Manual => periodExisting.CmDeliveryAcceptance.SupplyMethodCode,
            _ => throw new InvalidOperationException("SourceType ไม่ถูกต้อง"),
        };

        var supplyMethodCode = rawSupplyMethodCode ?? throw new InvalidOperationException("SupplyMethodCode ไม่ถูกต้อง");

        if (periodExisting.Document.IsNull())
        {
            var currentUserId = Guid.TryParse(this.HttpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var parsedUserId) ? parsedUserId : Guid.Empty;

            await this.SetDefaultDocumentTemplate(
                periodExisting,
                supplyMethodCode,
                currentUserId,
                req.HasDeduction || hasJorPorAssign,
                ct);
        }
        else
        {
            var isCurrentDoc = req.IsDocumentReplaced.HasValue && req.IsDocumentReplaced.Value;

            await this.UpdateDocumentHistory(
                periodExisting,
                supplyMethodCode,
                req.UserId,
                req.HasDeduction || hasJorPorAssign,
                isCurrentDoc,
                ct);
        }

        return periodExisting.Document?.FileId;
    }

    private async ValueTask UpdateAndReplaceDocumentAsync(
        CmDeliveryAcceptancePeriod entity,
        bool? isDocumentIdReplaced,
        CancellationToken ct,
        Guid currentUserId,
        UserId? creatorUserId)
    {
        var documentService = this.Resolve<IDocumentService>();

        var lastedDraftApprovalDocument = entity.LastedDraftDocument;

        if (lastedDraftApprovalDocument is null)
        {
            this.ThrowError("ไม่พบเอกสาร");
        }

        var copiedApprovalFileId = await this.CopyDocumentAsync(
            documentService,
            lastedDraftApprovalDocument.FileId,
            CmDeliveryAcceptanceDocumentType.DeliveryAcceptance,
            entity,
            ct);

        entity.AddDocumentHistory(
            CmDeliveryAcceptanceDocumentType.DeliveryAcceptance,
            copiedApprovalFileId);

        var replaceDto = await this.MapToReplaceDtoAsync(entity, ct, currentUserId, creatorUserId);

        var approvalFileId = await this.ReplaceDocumentAsync(
            documentService,
            lastedDraftApprovalDocument.FileId,
            isDocumentIdReplaced ?? false,
            CmDeliveryAcceptanceDocumentType.DeliveryAcceptance,
            entity,
            replaceDto,
            ct);

        entity.AddDocumentHistory(
            CmDeliveryAcceptanceDocumentType.DeliveryAcceptance,
            approvalFileId,
            true);
    }

    private async Task<FileId> CopyDocumentAsync(
        IDocumentService documentService,
        FileId sourceFileId,
        CmDeliveryAcceptanceDocumentType documentType,
        CmDeliveryAcceptancePeriod entity,
        CancellationToken ct)
    {
        var replaceDocumentAsync =
            documentService.CopyDocumentTemplateAsync(
                sourceFileId,
                parentDirectory: $"{DocumentTemplateGroups.DeliveryAcceptancePeriod}/{entity.Id}_{documentType.ToString()}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
                cancellationToken: ct);

        var fileIdResult = await replaceDocumentAsync;

        if (fileIdResult is null)
        {
            this.ThrowError("ไม่สามารถคัดลอกเอกสารร่างได้");
        }

        return (FileId)fileIdResult;
    }

    private async Task<FileId> ReplaceDocumentAsync(
        IDocumentService documentService,
        FileId fileId,
        bool isReplace,
        CmDeliveryAcceptanceDocumentType documentType,
        CmDeliveryAcceptancePeriod entity,
        object replaceDto,
        CancellationToken ct)
    {
        if (!isReplace)
        {
            return fileId;
        }

        var replaceDocumentAsync =
            documentService.CopyDocumentTemplateAsync(
                fileId,
                contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto),
                parentDirectory: $"{DocumentTemplateGroups.PrincipleApprovalRental}/{entity.Id}_{documentType.ToString()}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
                cancellationToken: ct);

        var fileIdResult = await replaceDocumentAsync;

        if (fileIdResult is null)
        {
            this.ThrowError("ไม่สามารถคัดลอกเอกสารร่างได้");
        }

        return (FileId)fileIdResult;
    }

    private async Task<CmDeliveryAcceptancePeriod> ValidateRequestAsync(
        UpdateDeliveryAcceptancePeriodRequest req,
        CancellationToken ct)
    {
        var periodExisting = await this.GetByIdAsync(req, ct);

        this.ValidateJorPorCommentRequirement(req, periodExisting);

        return periodExisting;
    }

    private async Task<CmDeliveryAcceptancePeriod> GetByIdAsync(
        UpdateDeliveryAcceptancePeriodRequest req,
        CancellationToken ct)
    {
        var periodExisting = await this.GetById(
            CmDeliveryAcceptanceId.From(req.DeliveryAcceptanceId),
            CmDeliveryAcceptancePeriodId.From(req.Id),
            ct);

        if (periodExisting is null)
        {
            this.ThrowError(
                r => req.Id,
                $"ไม่พบ งวดการส่งมอบ/ตรวจรับ {req.Id} ในระบบ",
                StatusCodes.Status404NotFound);
        }

        return periodExisting;
    }

    private void ValidateEditRequest(
        UpdateDeliveryAcceptancePeriodRequest req,
        CmDeliveryAcceptancePeriod periodExisting)
    {
        if (IsInvalidEditRequest(req, periodExisting))
        {
            this.ThrowError(
                r => req.Id,
                "งวดการส่งมอบ/ตรวจรับ ไม่อยู่ในสถานะที่สามารถส่งกลับแก้ไขได้ เนื่องจากมีคณะกรรมการอยู่ในสถานะที่อนุมัติแล้ว",
                StatusCodes.Status409Conflict);
        }
    }

    private static bool IsInvalidEditRequest(
        UpdateDeliveryAcceptancePeriodRequest req,
        CmDeliveryAcceptancePeriod periodExisting)
    {
        return req.Status == CmDeliveryAcceptancePeriodStatus.Edit &&
               periodExisting.Acceptors.Any(a => a is
               {
                   Type: AcceptorType.AcceptanceCommittee,
                   IsActive: true,
                   IsUnableToPerformDuties: false,
                   Status: AcceptorStatus.Approved
               });
    }

    private void ValidateAssignPermission(
        UpdateDeliveryAcceptancePeriodRequest req,
        CmDeliveryAcceptancePeriod periodExisting)
    {
        if (HasInvalidAssignPermission(req, periodExisting))
        {
            this.ThrowError(
                r => req.Id,
                "ผู้บันทึกมอบหมายต้องเป็นผู้ที่ได้รับมอบหมายเท่านั้น",
                StatusCodes.Status409Conflict);
        }
    }

    private static bool HasInvalidAssignPermission(
        UpdateDeliveryAcceptancePeriodRequest req,
        CmDeliveryAcceptancePeriod periodExisting)
    {
        return req.Status == CmDeliveryAcceptancePeriodStatus.WaitingAssign &&
               periodExisting.Assignees
                             .Select(DelegatorExtensions.DelegatorToAssignee)
                             .All(a => a.Delegatee?.SuUserId == null
                                    ? a.UserId != req.UserId
                                    : a.Delegatee?.SuUserId != UserId.From(req.UserId));
    }

    private void ValidateJorPorCommentRequirement(
        UpdateDeliveryAcceptancePeriodRequest req,
        CmDeliveryAcceptancePeriod periodExisting)
    {
        if (RequiresJorPorComment(req, periodExisting))
        {
            this.ThrowError(
                r => req.Id,
                "งวดการส่งมอบ/ตรวจรับ ต้องมี จพ. ให้ความเห็นอย่างน้อย 1 คน",
                StatusCodes.Status409Conflict);
        }
    }

    private static bool RequiresJorPorComment(
        UpdateDeliveryAcceptancePeriodRequest req,
        CmDeliveryAcceptancePeriod periodExisting)
    {
        return req.Status == CmDeliveryAcceptancePeriodStatus.WaitingAcceptance &&
               periodExisting.Status == CmDeliveryAcceptancePeriodStatus.WaitingComment &&
               periodExisting.Assignees.All(a => string.IsNullOrWhiteSpace(a.Remark));
    }

    private void UpsertBudgets(
        CmDeliveryAcceptancePeriod periodExisting,
        BudgetDetail[] budgetDetails)
    {
        var allIncomingBudgets = budgetDetails.ToList();

        var incomingIds =
            allIncomingBudgets
                .Where(b => b.Id.HasValue)
                .Select(b => CmDeliveryAcceptancePeriodBudgetId.From(b.Id.Value))
                .ToHashSet();

        var budgetsToDelete =
            periodExisting.Budgets
                          .Where(existing => !incomingIds.Contains(existing.Id))
                          .ToList();

        foreach (var budget in budgetsToDelete)
        {
            periodExisting.RemoveBudget(budget);
        }

        foreach (var budgetDto in allIncomingBudgets)
        {
            if (budgetDto.Id.HasValue)
            {
                var existingBudget =
                    periodExisting.Budgets
                                  .FirstOrDefault(b => b.Id == CmDeliveryAcceptancePeriodBudgetId.From(budgetDto.Id.Value));

                if (existingBudget is null)
                {
                    this.ThrowError(
                        r => budgetDto.Id,
                        $"ไม่พบข้อมูลงบประมาณที่ระบุในระบบ",
                        StatusCodes.Status404NotFound);
                }

                existingBudget.Update(
                    budgetDto.Sequence,
                    budgetDto.Department,
                    ParameterCode.From(budgetDto.BudgetType),
                    budgetDto.ProjectCode,
                    ParameterCode.From(budgetDto.AccountNo),
                    budgetDto.Budget);

                continue;
            }

            var newBudget =
                CmDeliveryAcceptancePeriodBudget.Create(
                    periodExisting.Id,
                    budgetDto.Sequence,
                    budgetDto.Department,
                    ParameterCode.From(budgetDto.BudgetType),
                    budgetDto.ProjectCode,
                    ParameterCode.From(budgetDto.AccountNo),
                    budgetDto.Budget);

            periodExisting.AddBudget(newBudget);
        }
    }

    private void UpsertPaymentTerms(
        CmDeliveryAcceptancePeriod periodExisting,
        GetById.Cm001PaymentTermResponse[] paymentTerms)
    {
        var allIncomingPaymentTerms = paymentTerms.ToList();

        var incomingIds =
            allIncomingPaymentTerms
                .Where(p => p.Id.HasValue)
                .Select(p => CmDeliveryAcceptancePeriodPaymentTermId.From(p.Id!.Value))
                .ToHashSet();

        var paymentTermsToDelete =
            periodExisting.PaymentTerms
                          .Where(existing => !incomingIds.Contains(existing.Id))
                          .ToList();

        foreach (var paymentTerm in paymentTermsToDelete)
        {
            periodExisting.RemovePaymentTerm(paymentTerm);
        }

        foreach (var paymentTermDto in allIncomingPaymentTerms)
        {
            if (paymentTermDto.Id.HasValue)
            {
                var existingPaymentTerm =
                    periodExisting.PaymentTerms
                                  .FirstOrDefault(p => p.Id == CmDeliveryAcceptancePeriodPaymentTermId.From(paymentTermDto.Id.Value));

                if (existingPaymentTerm is null)
                {
                    this.ThrowError(
                        r => paymentTermDto.Id,
                        $"ไม่พบข้อมูลงวดชำระเงินที่ระบุในระบบ",
                        StatusCodes.Status404NotFound);
                }

                existingPaymentTerm.Update(
                    paymentTermDto.Sequence,
                    paymentTermDto.PaymentTerm,
                    paymentTermDto.Description,
                    paymentTermDto.Amount);

                continue;
            }

            var newPaymentTerm =
                CmDeliveryAcceptancePeriodPaymentTerm.Create(
                    periodExisting.Id,
                    paymentTermDto.Sequence,
                    paymentTermDto.PaymentTerm,
                    paymentTermDto.Description,
                    paymentTermDto.Amount);

            periodExisting.AddPaymentTerm(newPaymentTerm);
        }

        var reSequenced = periodExisting.PaymentTerms
                                        .OrderBy(p => p.PaymentTerm)
                                        .ToList();

        for (var i = 0; i < reSequenced.Count; i++)
        {
            reSequenced[i].Update(
                i + 1,
                reSequenced[i].PaymentTerm,
                reSequenced[i].Description,
                reSequenced[i].Amount);
        }
    }

    private async Task UpsertAcceptorAsync(
        CmDeliveryAcceptancePeriod periodExisting,
        AcceptorRequest[] acceptorsRequest,
        CmDeliveryAcceptancePeriodStatus status,
        CancellationToken ct,
        UserId? sendToAcceptorId = null)
    {
        RemoveUnusedAcceptors(periodExisting, acceptorsRequest);

        var usersIncoming = await this.ValidateAndGetUsersAsync(acceptorsRequest, ct);

        var lastAssigneeUserId = periodExisting.Assignees
            .OrderByDescending(a => a.Sequence)
            .Select(a => (UserId?)a.UserId)
            .FirstOrDefault();

        var resolvedSendToAcceptorId = lastAssigneeUserId ?? sendToAcceptorId;

        var newAcceptors = CreateNewAcceptors(periodExisting, acceptorsRequest, usersIncoming, resolvedSendToAcceptorId);

        await this.UpdateExistingAcceptorsAsync(periodExisting, acceptorsRequest, status, resolvedSendToAcceptorId);

        await NotifyNewAccountingOperatorsAsync(periodExisting, newAcceptors);
    }

    private static async Task NotifyNewAccountingOperatorsAsync(
        CmDeliveryAcceptancePeriod periodExisting,
        List<CmDeliveryAcceptancePeriodAcceptor> newAcceptors)
    {
        var newAccountingOperators = newAcceptors
            .Where(a => a.Type == AcceptorType.AccountingOperator)
            .ToList();

        foreach (var acceptor in newAccountingOperators)
        {
            foreach (var targetUserId in acceptor.GetNotificationTargets())
            {
                await SendNotificationAsync(
                    periodExisting,
                    targetUserId,
                    NotificationConstant.WaitForVerify.Title,
                    string.Format(
                        NotificationConstant.WaitForVerify.Message,
                        ProgramConstant.ContractAcceptancePeriod.Name,
                        periodExisting.AcceptanceNumber));
            }
        }
    }

    private static void RemoveUnusedAcceptors(
        CmDeliveryAcceptancePeriod periodExisting,
        AcceptorRequest[] acceptorsRequest)
    {
        var incomingAcceptorIds = acceptorsRequest.Select(s => s.Id).ToHashSet();
        var acceptorsToRemove = periodExisting.Acceptors
                                              .Where(w => !incomingAcceptorIds.Contains(w.Id.Value))
                                              .ToList();

        foreach (var acceptor in acceptorsToRemove)
        {
            periodExisting.RemoveAcceptor(acceptor);
        }
    }

    private async Task<SuUser[]> ValidateAndGetUsersAsync(
        AcceptorRequest[] acceptorsRequest,
        CancellationToken ct)
    {
        var userIdsIncoming = acceptorsRequest
                              .Select(s => UserId.From(s.UserId))
                              .ToArray();

        var usersIncoming = await this.dbContext.SuUsers
                                      .Include(r => r.Employee)
                                      .ThenInclude(r => r.View)
                                      .Where(w => userIdsIncoming.Contains(w.Id))
                                      .ToArrayAsync(ct);

        var userNotExistsInDb = userIdsIncoming
                                .Except(usersIncoming.Select(u => u.Id))
                                .ToArray();

        if (userNotExistsInDb.Length > 0)
        {
            this.ThrowError(
                $"User with ID {string.Join(", ", userNotExistsInDb)} not found.",
                StatusCodes.Status404NotFound);
        }

        return usersIncoming;
    }

    private static List<CmDeliveryAcceptancePeriodAcceptor> CreateNewAcceptors(
        CmDeliveryAcceptancePeriod periodExisting,
        AcceptorRequest[] acceptorsRequest,
        SuUser[] usersIncoming,
        UserId? sendToAcceptorId = null)
    {
        var newAcceptorRequests = acceptorsRequest.Where(w => !w.Id.HasValue);
        var newAcceptors = new List<CmDeliveryAcceptancePeriodAcceptor>();

        foreach (var req in newAcceptorRequests)
        {
            var user = usersIncoming.First(usr => usr.Id == UserId.From(req.UserId));

            var newAcceptor = CmDeliveryAcceptancePeriodAcceptor
                              .Create(
                                  periodExisting.Id,
                                  req.AcceptorType,
                                  user,
                                  req.Sequence,
                                  periodExisting.Status)
                              .SetCommitteePositionsCode(
                                  req.CommitteePositionsCode.IsNullOrEmpty()
                                      ? null
                                      : ParameterCode.From(req.CommitteePositionsCode!))
                              .SetIsUnableToPerformDuties(req.IsUnableToPerformDuties ?? false);

            newAcceptor.SetSendToAcceptorId(sendToAcceptorId);

            if (req.AcceptorType == AcceptorType.Accounting || req.AcceptorType == AcceptorType.AccountingOperator)
            {
                newAcceptor.SetStatus(AcceptorStatus.Pending);
            }

            periodExisting.AddAcceptor(newAcceptor);
            newAcceptors.Add(newAcceptor);
        }

        return newAcceptors;
    }

    private async Task UpdateExistingAcceptorsAsync(
        CmDeliveryAcceptancePeriod periodExisting,
        AcceptorRequest[] acceptorsRequest,
        CmDeliveryAcceptancePeriodStatus status,
        UserId? sendToAcceptorId = null)
    {
        var existingAcceptors = periodExisting.Acceptors.ToList();

        foreach (var existing in existingAcceptors)
        {
            var match = acceptorsRequest.FirstOrDefault(e =>
                e.UserId == existing.UserId && existing.Type == e.AcceptorType);

            if (match == null)
            {
                continue;
            }

            existing.SetSendToAcceptorId(sendToAcceptorId);

            if (status != CmDeliveryAcceptancePeriodStatus.WaitingAssign)
            {
                existing
                    .SetIsUnableToPerformDuties(match.IsUnableToPerformDuties ?? false)
                    .SetSequence(match.Sequence);
            }

            await this.HandleAcceptorStatusUpdateAsync(periodExisting, existing, status);
        }
    }

    private static AcceptorType GetAcceptorTypeForStatus(CmDeliveryAcceptancePeriodStatus status)
    {
        return status == CmDeliveryAcceptancePeriodStatus.WaitingCommitteeApproval
            ? AcceptorType.AcceptanceCommittee
            : AcceptorType.Approver;
    }

    private async Task HandleAcceptorStatusUpdateAsync(
        CmDeliveryAcceptancePeriod periodExisting,
        CmDeliveryAcceptancePeriodAcceptor existing,
        CmDeliveryAcceptancePeriodStatus status)
    {
        if (ShouldSetPendingForCommitteeApproval(periodExisting.Status, status) && existing.Type == AcceptorType.AcceptanceCommittee && !existing.IsUnableToPerformDuties)
        {
            existing.SetStatus(AcceptorStatus.Pending);
        }

        if (ShouldSetPendingForAcceptance(periodExisting.Status, status) && existing.Type == AcceptorType.Approver)
        {
            existing.SetStatus(AcceptorStatus.Pending);

            if (ShouldSendFirstAcceptorNotification(periodExisting.Status, existing.Sequence))
            {
                await SendAcceptorNotificationAsync(periodExisting, existing);
            }
        }
    }

    private static bool ShouldSetPendingForCommitteeApproval(
        CmDeliveryAcceptancePeriodStatus currentStatus,
        CmDeliveryAcceptancePeriodStatus newStatus)
    {
        return (currentStatus == CmDeliveryAcceptancePeriodStatus.Draft ||
                currentStatus == CmDeliveryAcceptancePeriodStatus.Rejected) &&
               newStatus == CmDeliveryAcceptancePeriodStatus.WaitingCommitteeApproval;
    }

    private static bool ShouldSetPendingForAcceptance(
        CmDeliveryAcceptancePeriodStatus currentStatus,
        CmDeliveryAcceptancePeriodStatus newStatus)
    {
        return currentStatus == CmDeliveryAcceptancePeriodStatus.WaitingComment &&
               newStatus == CmDeliveryAcceptancePeriodStatus.WaitingAcceptance;
    }

    private static bool ShouldSendFirstAcceptorNotification(
        CmDeliveryAcceptancePeriodStatus currentStatus,
        int sequence)
    {
        return currentStatus == CmDeliveryAcceptancePeriodStatus.WaitingAcceptance && sequence == 1;
    }

    private static async Task SendAcceptorNotificationAsync(
        CmDeliveryAcceptancePeriod periodExisting,
        CmDeliveryAcceptancePeriodAcceptor acceptor)
    {
        foreach (var targetUserId in acceptor.GetNotificationTargets())
        {
            await SendNotificationAsync(
                periodExisting,
                targetUserId,
                NotificationConstant.WaitForLike.Title,
                string.Format(
                    NotificationConstant.WaitForLike.Message,
                    ProgramConstant.ContractAcceptancePeriod.Name,
                    periodExisting.AcceptanceNumber));
        }
    }

    private static async Task SendNotificationAsync(CmDeliveryAcceptancePeriod periodExisting, UserId userId, string title, string message)
    {
        await Notification
              .Crate(
                  userId,
                  title,
                  message,
                  NotificationProgram.ContractManagement)
              .SetReferenceId(periodExisting.Id.Value)
              .SetLinkUrl(string.Format(ProgramConstant.ContractAcceptancePeriod.Url, periodExisting.CmDeliveryAcceptanceId, periodExisting.Id), "ดูรายละเอียด")
              .PublishAsync(CancellationToken.None);
    }

    private static async Task SendNotificationAssigneeAsync(CmDeliveryAcceptancePeriod periodExisting, CancellationToken ct)
    {
        var targetUserIds = periodExisting.Assignees
                                          .Where(x => x.Type != AssigneeType.Director)
                                          .SelectMany(pa => pa.GetAssigneeNotificationTargets())
                                          .ToList();

        _ = await targetUserIds.Map(userId =>
                                    Notification
                                        .Crate(
                                            userId,
                                            NotificationConstant.Assignment.Title,
                                            string.Format(NotificationConstant.Assignment.Message, ProgramConstant.ContractAcceptancePeriod.Name, periodExisting.AcceptanceNumber),
                                            NotificationProgram.ContractManagement)
                                        .SetReferenceId(periodExisting.Id.Value)
                                        .SetLinkUrl(
                                            string.Format(ProgramConstant.ContractAcceptancePeriod.Url, periodExisting.CmDeliveryAcceptanceId, periodExisting.Id),
                                            "ดูรายละเอียด"))
                                .Map(n => n.PublishAsync(ct).ToUnit())
                                .SequenceSerial();
    }
}