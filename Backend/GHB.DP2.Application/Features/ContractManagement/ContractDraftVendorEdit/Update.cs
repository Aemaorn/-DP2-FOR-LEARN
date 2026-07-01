namespace GHB.DP2.Application.Features.ContractManagement.ContractDraftVendorEdit;

using System.IdentityModel.Tokens.Jwt;
using FluentValidation;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Validators;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.ContractAgreement.ContractDraft.Dto;
using GHB.DP2.Application.Features.ContractManagement.ContractDraftVendorEdit.Abstract;
using GHB.DP2.Application.Features.ContractManagement.ContractDraftVendorEdit.Dto;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractManagement.CaContractDraftVendorEdit;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record UpdateContractDraftVendorEditRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid Id,
    ContractDraftVendorEditStatus Status,
    string? Email,
    string? Title,
    string? Description,
    string? ContractName,
    decimal? Budget,
    string? PoNumber,
    DateTimeOffset? ContractSignedDate,
    DateTimeOffset? ContractEndDate,
    DateTimeOffset? StartDate,
    DateTimeOffset? EndDate,
    bool? IsWorkingDayOnly,
    string? PeriodConditionTypeCode,
    BuyerInfo? Buyer,
    VendorInfo? Vendor,
    AgreementBase? Agreement,
    PaymentBase? Payment,
    DeliveryInfo? Delivery,
    TerminationInfo? Termination,
    string? DefectWarrantyTypeCode,
    WarrantyInfo? Warranty,
    PenaltyInfo? Penalty,
    GuaranteeInfo? Guarantee,
    AdvancePayment? AdvancePayment,
    RetentionPayment? RetentionPayment,
    RedeliveryBase? Redelivery,
    CopierLeaseInfo? CopierLease,
    ComputerLeaseInfo? ComputerLease,
    CarLeaseInfo? CarLease,
    ContractDraftVendorEditComponentDto[]? Components,
    ContractDraftVendorEditAttachmentDto[]? Attachments,
    bool IsAmendmentDocumentIdReplaced,
    bool IsAmendmentApprovalRequestDocumentIdReplaced,
    IEnumerable<AcceptorRequest>? Acceptors,
    IEnumerable<AssigneeRequest>? Assignees,
    DateTimeOffset? DocumentDate = null);

public class UpdateContractDraftVendorEditValidator : Validator<UpdateContractDraftVendorEditRequest>
{
    public UpdateContractDraftVendorEditValidator()
    {
        this.RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("กรุณาระบุรหัสแก้ไขร่างสัญญา");

        this.When(x => x.Status == ContractDraftVendorEditStatus.WaitingCommitteeApproval, () =>
        {
            this.RuleFor(x => x.Acceptors)
                .NotNull().WithMessage("ต้องระบุผู้มีอำนาจเห็นชอบ/อนุมัติอย่างน้อย 1 คน")
                .Must(a => a != null && a.Any())
                .WithMessage("ต้องระบุผู้มีอำนาจเห็นชอบ/อนุมัติอย่างน้อย 1 คน");
        });

        this.RuleForEach(x => x.Attachments)
            .ChildRules(a => a.RuleForEach(x => x.Files)
                .ChildRules(f => f.RuleFor(x => x.FileName).MustBeValidFileExtension()))
            .When(x => x.Attachments is not null);
    }
}

public class UpdateContractDraftVendorEditEndpoint
    : ContractDraftVendorEditEndpoint<UpdateContractDraftVendorEditRequest, Ok>
{
    private readonly Dp2DbContext dbContext;

    public UpdateContractDraftVendorEditEndpoint(
        ILogger<UpdateContractDraftVendorEditEndpoint> logger,
        Dp2DbContext dbContext)
        : base(logger, dbContext)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Put("contract/contract-draft-vendor-edit/{Id:guid}");
        this.Description(b => b
                              .WithTags("ContractManagement/ContractDraftVendorEdit")
                              .WithName("UpdateContractDraftVendorEdit")
                              .AllowAnonymous()
                              .Produces<Ok>()
                              .ProducesProblem(StatusCodes.Status400BadRequest)
                              .ProducesProblem(StatusCodes.Status500InternalServerError));
    }

    protected override async ValueTask<Ok> HandleRequestAsync(
        UpdateContractDraftVendorEditRequest req, CancellationToken ct)
    {
        var entity = await this.GetEditByIdAsync(ContractDraftVendorEditId.From(req.Id), ct);

        var previousStatus = entity.Status;

        if (req.PoNumber != null)
        {
            entity.SetPoNumber(req.PoNumber);
        }

        // ── Update contract info ──
        if (req.Email != null)
        {
            entity.SetEmail(req.Email);
        }

        if (req.Title != null)
        {
            entity.SetTitle(req.Title);
        }

        if (req.Description != null)
        {
            entity.SetDescription(req.Description);
        }

        if (req.ContractName != null)
        {
            entity.SetContractName(req.ContractName);
        }

        if (req.Budget.HasValue)
        {
            entity.SetBudget(req.Budget.Value);
        }

        if (req.ContractSignedDate.HasValue)
        {
            entity.SetContractSignedDate(req.ContractSignedDate.Value);
        }

        if (req.ContractEndDate.HasValue)
        {
            entity.SetContractEndDate(req.ContractEndDate.Value);
        }

        if (req.StartDate.HasValue)
        {
            entity.SetStartDate(req.StartDate.Value);
        }

        if (req.EndDate.HasValue)
        {
            entity.SetEndDate(req.EndDate.Value);
        }

        if (req.IsWorkingDayOnly.HasValue)
        {
            entity.SetIsWorkingDayOnly(req.IsWorkingDayOnly.Value);
        }

        if (req.PeriodConditionTypeCode != null)
        {
            entity.SetPeriodConditionType(req.PeriodConditionTypeCode);
        }

        // ── Update section data (mapped DTOs → domain via MapToEntity) ──
        if (req.Buyer != null)
        {
            entity.SetBuyer(req.Buyer.MapToEntity());
        }

        if (req.Vendor != null)
        {
            entity.Vendor.SetDateDuration(req.Vendor.StartDate, req.Vendor.EndDate);
            entity.Vendor.SetDetailAddress(
                req.Vendor.RegistrationPlace,
                req.Vendor.VendorRegistrationPlace,
                req.Vendor.Address,
                req.Vendor.Street,
                req.Vendor.Province,
                req.Vendor.District,
                req.Vendor.SubDistrict);
        }

        if (req.Agreement != null)
        {
            entity.SetAgreement(req.Agreement.MapToEntity());
        }

        if (req.Payment != null)
        {
            if (req.Payment is Term term)
            {
                entity.SetPayment(
                    new GHB.DP2.Domain.ContractAgreement.CaContractDraft.SectionType.Payment(
                        !string.IsNullOrWhiteSpace(term.PaymentTypeCode) ? ParameterCode.From(term.PaymentTypeCode) : null,
                        term.DueDay,
                        !string.IsNullOrWhiteSpace(term.RedeliveryTypeCode) ? ParameterCode.From(term.RedeliveryTypeCode) : null));

                if (term.Details != null)
                {
                    entity.SetPaymentTerm(term.Details.Select(MapPaymentTermDetail));
                }
            }
            else if (req.Payment is Contract contract)
            {
                entity.SetPayment(
                    new GHB.DP2.Domain.ContractAgreement.CaContractDraft.SectionType.Payment(
                        null,
                        contract.DueDay,
                        ParameterCode.From(contract.RedeliveryTypeCode)));
            }
            else if (req.Payment is TremNotType tremNotType)
            {
                entity.SetPayment(
                    new GHB.DP2.Domain.ContractAgreement.CaContractDraft.SectionType.Payment(null, null, null));

                if (tremNotType.Details != null)
                {
                    entity.SetPaymentTerm(tremNotType.Details.Select(MapPaymentTermDetail));
                }
            }
        }

        if (req.Delivery != null)
        {
            entity.SetDelivery(req.Delivery.MapToEntity());
        }

        if (req.Termination != null)
        {
            entity.SetTermination(
                new GHB.DP2.Domain.ContractAgreement.CaContractDraft.SectionType.Termination(
                    req.Termination.StartDate,
                    req.Termination.EndDate,
                    req.Termination.VendorProcessingTime));
        }

        // ── TermsConditions (flat) ──
        if (req.DefectWarrantyTypeCode != null)
        {
            entity.SetDefectWarrantyTypeCode(
                string.IsNullOrWhiteSpace(req.DefectWarrantyTypeCode)
                    ? null
                    : (ParameterCode?)ParameterCode.From(req.DefectWarrantyTypeCode));
        }

        if (req.AdvancePayment != null)
        {
            entity.SetAdvancePayment(req.AdvancePayment.MapToEntity());
        }

        if (req.Warranty != null)
        {
            entity.SetWarranty(req.Warranty.MapToEntity());
        }

        if (req.Penalty != null)
        {
            entity.SetPenalty(req.Penalty.MapToEntity());
        }

        if (req.Guarantee != null)
        {
            entity.SetGuarantee(req.Guarantee.MapToEntity());
        }

        if (req.RetentionPayment != null)
        {
            entity.SetRetentionPayment(req.RetentionPayment.MapToEntity());
        }

        if (req.Redelivery != null)
        {
            entity.SetRedeliveryCorrection(req.Redelivery.MapToEntity());
        }

        // ── EquipmentRental (flat) ──
        if (req.CopierLease != null)
        {
            entity.SetCopierLease(req.CopierLease.MapToEntity());
        }

        if (req.CarLease != null)
        {
            entity.SetCarLease(req.CarLease.MapToEntity());
        }

        if (req.ComputerLease != null)
        {
            entity.SetComputerLease(req.ComputerLease.MapToEntity());
        }

        // ── Update Components ──
        if (req.Components != null)
        {
            var existingComponents = entity.Components.ToList();

            var reqCodes = req.Components
                              .Where(c => c.ComponentCode != null)
                              .Select(c => c.ComponentCode)
                              .ToHashSet();

            foreach (var comp in existingComponents.Where(c => c.ComponentCode != null && !reqCodes.Contains(c.ComponentCode)))
            {
                entity.RemoveComponent(comp);
            }

            foreach (var comp in req.Components)
            {
                var existing = existingComponents.FirstOrDefault(c => c.ComponentCode == comp.ComponentCode);

                if (existing != null)
                {
                    existing.SetIsEdited(comp.IsEdited);
                    existing.SetComponentName(comp.ComponentName);
                }
                else
                {
                    entity.AddComponent(CaContractDraftVendorEditComponent.Create(
                        entity.Id,
                        comp.ComponentCode,
                        comp.ComponentName,
                        comp.IsEdited));
                }
            }
        }

        // ── Update Attachments ──
        if (req.Attachments != null)
        {
            var existingAttachments = entity.Attachments.ToList();

            var reqAttachmentIds = req.Attachments
                                      .Where(a => a.Id.HasValue)
                                      .Select(a => a.Id!.Value)
                                      .ToHashSet();

            foreach (var attachment in existingAttachments.Where(a => !reqAttachmentIds.Contains(a.Id.Value)))
            {
                entity.RemoveAttachment(attachment);
            }

            foreach (var att in req.Attachments)
            {
                if (string.IsNullOrWhiteSpace(att.TypeCode))
                {
                    continue;
                }

                var typeCode = ParameterCode.From(att.TypeCode);

                var attachmentEntity = att.Id.HasValue
                    ? CaContractDraftEditVendorsAttachment.Create(
                        att.Id.Value,
                        typeCode,
                        att.Description,
                        att.PageNumber,
                        att.Sequence,
                        att.FormatOtherName)
                    : CaContractDraftEditVendorsAttachment.Create(
                        typeCode,
                        att.Description,
                        att.PageNumber,
                        att.Sequence,
                        att.FormatOtherName);

                foreach (var file in att.Files)
                {
                    attachmentEntity.AddFile(CaContractDraftEditVendorsAttachmentFile.Create(
                        file.FileId,
                        file.FileName,
                        file.FileType ?? string.Empty,
                        file.Sequence));
                }

                entity.SetAttachments(attachmentEntity);
            }
        }

        // ── Upsert Acceptors/Assignees ──
        if (req.Acceptors != null)
        {
            await this.UpsertAcceptors(entity, [.. req.Acceptors], ct, UserId.From(req.UserId));
        }

        if (req.Assignees != null)
        {
            await this.UpsertAssignee(entity, [.. req.Assignees], ct);
        }

        // ── Get supplyMethodCode for document operations ──
        var isDocumentReplace = req.IsAmendmentDocumentIdReplaced || req.IsAmendmentApprovalRequestDocumentIdReplaced;
        ParameterCode? supplyMethodCode = null;

        if (isDocumentReplace || req.Status == ContractDraftVendorEditStatus.WaitingCommitteeApproval)
        {
            supplyMethodCode = await this.GetSupplyMethodCodeAsync(entity, ct);
        }

        if (entity.ContractSignedDate is not null && entity.PaymentTerms.Any())
        {
            entity.CalculateAndSetDeliveryDates();

            var lastDeliveryDate = entity.PaymentTerms
                                         .OrderBy(t => t.Sequence)
                                         .LastOrDefault()
                                         ?.DeliveryDate;

            if (lastDeliveryDate is not null)
            {
                entity.SetContractEndDate(lastDeliveryDate.Value);
            }
        }

        if (req.DocumentDate is not null)
        {
            entity.SetDocumentDate(req.DocumentDate);
        }

        switch (req.Status)
        {
            case ContractDraftVendorEditStatus.Editing
                when previousStatus == ContractDraftVendorEditStatus.Draft
                     || previousStatus == ContractDraftVendorEditStatus.WaitingCommitteeApproval:
                entity.SetEditing();

                break;

            case ContractDraftVendorEditStatus.WaitingCommitteeApproval:
                entity.Acceptors
                      .Where(a => a is { Type: AcceptorType.AcceptanceCommittee, IsActive: true } && !a.IsUnableToPerformDuties)
                      .Iter(a => a.Pending());

                entity.SetWaitingCommitteeApproval();

                // Update documents with replacement
                if (supplyMethodCode != null)
                {
                    await this.UpdateDocumentAsync(
                        entity,
                        supplyMethodCode.Value,
                        new ContractDraftVendorEditDocumentOptions(
                            req.IsAmendmentDocumentIdReplaced,
                            req.IsAmendmentApprovalRequestDocumentIdReplaced,
                            IsMarkReplaced: true),
                        ct);
                }

                // Send notifications to committee members
                foreach (var member in entity.Acceptors
                                             .Where(a => a is { Type: AcceptorType.AcceptanceCommittee, IsActive: true, Status: AcceptorStatus.Pending }))
                {
                    _ = SendNotificationAsync(
                        entity,
                        member.UserId,
                        NotificationConstant.WaitForLike.Title,
                        string.Format(
                            NotificationConstant.WaitForLike.Message,
                            ProgramConstant.ContractDraftVendorEdit.Name,
                            entity.ContractNumber));
                }

                break;

            case ContractDraftVendorEditStatus.WaitingAssignment when previousStatus == ContractDraftVendorEditStatus.Editing:
                entity.SetWaitingAssignment();

                break;

            case ContractDraftVendorEditStatus.WaitingComment
                when previousStatus == ContractDraftVendorEditStatus.WaitingAssignment
                     || previousStatus == ContractDraftVendorEditStatus.WaitingApproval:

                // Mark assignees as Pending
                entity.Assignees
                      .Where(a => a.Status == AssigneeStatus.Draft)
                      .Iter(a => a.Pending());

                // Reset approver acceptors to Pending on recall
                if (previousStatus == ContractDraftVendorEditStatus.WaitingApproval)
                {
                    entity.Acceptors
                          .Where(a => a is { Type: AcceptorType.Approver, IsActive: true } && !a.IsUnableToPerformDuties)
                          .Iter(a => a.Pending());
                }

                entity.SetWaitingComment();

                // Update documents
                supplyMethodCode ??= await this.GetSupplyMethodCodeAsync(entity, ct);

                await this.UpdateDocumentAsync(
                    entity,
                    supplyMethodCode.Value,
                    new ContractDraftVendorEditDocumentOptions(false, false, IsMarkReplaced: true),
                    ct);

                // Notify assignees
                foreach (var targetUserId in entity.Assignees
                                                   .Where(a => a.Status == AssigneeStatus.Pending)
                                                   .SelectMany(a => a.GetAssigneeNotificationTargets()))
                {
                    _ = SendNotificationAsync(
                        entity,
                        targetUserId,
                        NotificationConstant.Assignment.Title,
                        string.Format(
                            NotificationConstant.Assignment.Message,
                            ProgramConstant.ContractDraftVendorEdit.Name,
                            entity.ContractNumber));
                }

                break;

            case ContractDraftVendorEditStatus.WaitingDraftAddendum
                when previousStatus == ContractDraftVendorEditStatus.WaitingAssignment
                  || previousStatus == ContractDraftVendorEditStatus.WaitingAddendumAssignment
                  || previousStatus == ContractDraftVendorEditStatus.WaitingReview:

                entity.Assignees
                      .Where(a => a.Group == AssigneeGroup.AddendumDrafter && a.Status == AssigneeStatus.Draft)
                      .Iter(a => a.Pending());

                entity.SetWaitingDraftAddendum();

                foreach (var targetUserId in entity.Assignees
                                                   .Where(a => a.Group == AssigneeGroup.AddendumDrafter && a.Status == AssigneeStatus.Pending)
                                                   .SelectMany(a => a.GetAssigneeNotificationTargets()))
                {
                    _ = SendNotificationAsync(
                        entity,
                        targetUserId,
                        NotificationConstant.Assignment.Title,
                        string.Format(
                            NotificationConstant.Assignment.Message,
                            ProgramConstant.ContractDraftVendorEdit.Name,
                            entity.ContractNumber));
                }

                break;

            case ContractDraftVendorEditStatus.WaitingReview
                when previousStatus == ContractDraftVendorEditStatus.WaitingDraftAddendum:

                entity.Assignees
                      .Where(a => a.Group == AssigneeGroup.AddendumDrafter && a.Status == AssigneeStatus.Draft)
                      .Iter(a => a.Pending());

                entity.Acceptors
                      .Where(a => a is { Type: AcceptorType.Reviewer, IsActive: true } && !a.IsUnableToPerformDuties)
                      .Iter(a => a.Pending());

                entity.SetWaitingReview();

                foreach (var targetUserId in entity.Acceptors
                                                   .Where(a => a is { Type: AcceptorType.Reviewer, IsActive: true, Status: AcceptorStatus.Pending })
                                                   .Select(a => a.UserId))
                {
                    _ = SendNotificationAsync(
                        entity,
                        targetUserId,
                        NotificationConstant.WaitForApprove.Title,
                        string.Format(
                            NotificationConstant.WaitForApprove.Message,
                            ProgramConstant.ContractDraftVendorEdit.Name,
                            entity.ContractNumber));
                }

                break;

            case ContractDraftVendorEditStatus.Approved
                when previousStatus == ContractDraftVendorEditStatus.WaitingReview:

                entity.Acceptors
                      .Where(a => a is { Type: AcceptorType.Reviewer, IsActive: true, Status: AcceptorStatus.Pending })
                      .Iter(a => a.Approve());

                entity.SetApprovedByReview();

                _ = SendNotificationAsync(
                    entity,
                    UserId.From(entity.AuditInfo.CreatedBy),
                    NotificationConstant.InformCommittee.Title,
                    string.Format(
                        NotificationConstant.InformCommittee.Message,
                        ProgramConstant.ContractDraftVendorEdit.Name,
                        entity.ContractNumber));

                break;

            case ContractDraftVendorEditStatus.WaitingApproval
                 when previousStatus is ContractDraftVendorEditStatus.WaitingComment
                    or ContractDraftVendorEditStatus.RejectedToAssignee:

                entity.SetWaitingApproval();

                supplyMethodCode ??= await this.GetSupplyMethodCodeAsync(entity, ct);

                await this.UpdateDocumentAsync(
                    entity,
                    supplyMethodCode.Value,
                    new ContractDraftVendorEditDocumentOptions(
                        req.IsAmendmentDocumentIdReplaced,
                        req.IsAmendmentApprovalRequestDocumentIdReplaced,
                        IsMarkReplaced: true),
                    ct,
                    hasAcceptor: true,
                    hasCommittee: true,
                    hasComment: true);

                // Notify first pending approver
                var firstApprover = entity.Acceptors
                                          .FirstOrDefault(a => a is { Type: AcceptorType.Approver, IsActive: true, IsCurrent: true });

                if (firstApprover != null)
                {
                    foreach (var targetUserId in firstApprover.GetNotificationTargets())
                    {
                        _ = SendNotificationAsync(
                            entity,
                            targetUserId,
                            NotificationConstant.WaitForApprove.Title,
                            string.Format(
                                NotificationConstant.WaitForApprove.Message,
                                ProgramConstant.ContractDraftVendorEdit.Name,
                                entity.ContractNumber));
                    }
                }

                break;

            default:
                if (isDocumentReplace && supplyMethodCode != null)
                {
                    await this.UpdateDocumentAsync(
                        entity,
                        supplyMethodCode.Value,
                        new ContractDraftVendorEditDocumentOptions(
                            req.IsAmendmentDocumentIdReplaced,
                            req.IsAmendmentApprovalRequestDocumentIdReplaced),
                        ct);
                }

                break;
        }

        this.dbContext.CaContractDraftVendorEdits.Update(entity);

        await this.dbContext.SaveChangesAsync(CancellationToken.None);

        return TypedResults.Ok();
    }

    private static async Task SendNotificationAsync(CaContractDraftVendorEdit entity, UserId userId, string title, string message)
    {
        await Notification
              .Crate(
                  userId,
                  title,
                  message,
                  NotificationProgram.ContractManagement)
              .SetReferenceId(entity.Id.Value)
              .SetLinkUrl(string.Format(ProgramConstant.ContractDraftVendorEdit.Url, entity.Id.Value), "ดูรายละเอียด")
              .PublishAsync(CancellationToken.None);
    }

    private static CaContractDraftEditPaymentTerm MapPaymentTermDetail(PaymentTermDetail d)
    {
        var pt = d.Id.HasValue
            ? CaContractDraftEditPaymentTerm.Create(d.Id.Value)
            : CaContractDraftEditPaymentTerm.Create();

        pt.SetPaymentTermNo(d.No)
          .SetLeadTime(d.LeadTime)
          .SetDeliveryDate(d.DeliveryDate)
          .SetInstallmentPercentage(d.InstallmentPercentage)
          .SetAmount(d.Amount)
          .SetAdvanceDeductionAmount(d.AdvanceDeductionAmount)
          .SetPerformanceDeductionAmount(d.PerformanceDeductionAmount)
          .SetDescription(d.Description)
          .SetSequence(d.Sequence);

        if (!string.IsNullOrWhiteSpace(d.PeriodTypeCode.Value))
        {
            pt.SetPeriodType(d.PeriodTypeCode);
        }

        return pt;
    }
}