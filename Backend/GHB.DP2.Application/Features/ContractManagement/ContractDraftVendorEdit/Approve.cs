namespace GHB.DP2.Application.Features.ContractManagement.ContractDraftVendorEdit;

using System.IdentityModel.Tokens.Jwt;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.ContractManagement.ContractDraftVendorEdit.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractManagement.CaContractDraftVendorEdit;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record ApproveContractDraftVendorEditRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid Id,
    string? Remark);

public class ApproveContractDraftVendorEditEndpoint
    : ContractDraftVendorEditEndpoint<ApproveContractDraftVendorEditRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public ApproveContractDraftVendorEditEndpoint(
        ILogger<ApproveContractDraftVendorEditEndpoint> logger,
        Dp2DbContext dbContext)
        : base(logger, dbContext)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("ContractManagement/ContractDraftVendorEdit")
             .WithName("ApproveContractDraftVendorEdit")
             .Accepts<ApproveContractDraftVendorEditRequest>("application/json"));
        this.Post("contract/contract-draft-vendor-edit/{Id:guid}/approve");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>>
        HandleRequestAsync(ApproveContractDraftVendorEditRequest req, CancellationToken ct)
    {
        var entity = await this.GetEditByIdAsync(ContractDraftVendorEditId.From(req.Id), ct);

        switch (entity.Status)
        {
            case ContractDraftVendorEditStatus.WaitingCommitteeApproval:
                {
                    var committeeAcceptors = entity.Acceptors
                                                   .Where(a => a is { Type: AcceptorType.AcceptanceCommittee, IsActive: true })
                                                   .OrderBy(a => a.Sequence)
                                                   .ToList();

                    var current = committeeAcceptors.FirstOrDefault(a => a.UserId == UserId.From(req.UserId));

                    if (current is null)
                    {
                        return TypedResults.BadRequest("ไม่พบผู้เห็นชอบในกลุ่มคณะกรรมการ");
                    }

                    if (current.Status != AcceptorStatus.Pending)
                    {
                        return TypedResults.BadRequest("ผู้เห็นชอบดำเนินการแล้ว");
                    }

                    current.Approve(remark: req.Remark);

                    // Check if all committee approved
                    var allApproved = committeeAcceptors
                                      .Where(a => a.Status != AcceptorStatus.Draft && !a.IsUnableToPerformDuties)
                                      .All(a => a.Status == AcceptorStatus.Approved);

                    // Check board chairman approval
                    var isBoardChairman = current.CommitteePositionsCode?.Value == "PCommitteePosition001";

                    if (allApproved || isBoardChairman)
                    {
                        entity.SetWaitingAssignment();

                        // Update documents with committee info
                        var supplyMethodCode = await this.GetSupplyMethodCodeAsync(entity, ct);
                        await this.UpdateDocumentAsync(
                            entity,
                            supplyMethodCode,
                            new ContractDraftVendorEditDocumentOptions(false, false, IsMarkReplaced: true),
                            ct,
                            hasCommittee: true);

                        // Notify assignees
                        foreach (var targetUserId in entity.Assignees.SelectMany(a => a.GetAssigneeNotificationTargets()))
                        {
                            _ = SendNotificationAsync(
                                entity,
                                targetUserId,
                                NotificationConstant.WaitForAssignment.Title,
                                string.Format(
                                    NotificationConstant.WaitForAssignment.Message,
                                    ProgramConstant.ContractDraftVendorEdit.Name,
                                    entity.ContractNumber));
                        }
                    }

                    break;
                }

            case ContractDraftVendorEditStatus.WaitingApproval:
                {
                    var approvers = entity.Acceptors
                                          .Where(a => a is { Type: AcceptorType.Approver, IsActive: true, Status: AcceptorStatus.Pending })
                                          .OrderBy(a => a.Sequence)
                                          .ToList();

                    var current = approvers
                                  .Select(DelegatorExtensions.DelegatorToAcceptor)
                                  .FirstOrDefault(a => a.Delegatee?.SuUserId == null
                                      ? a.UserId == UserId.From(req.UserId)
                                      : a.Delegatee?.SuUserId == UserId.From(req.UserId));

                    if (current is null)
                    {
                        return TypedResults.BadRequest("ไม่พบผู้อนุมัติในลำดับนี้");
                    }

                    // Use domain method for approval
                    entity.SetApproved(current.Id, req.Remark, current.DelegateeId);

                    // Update documents
                    var approveSupplyMethodCode = await this.GetSupplyMethodCodeAsync(entity, ct);

                    if (entity.Status == ContractDraftVendorEditStatus.Approved)
                    {
                        // Final approval — update documents with all info, mark replaced
                        await this.UpdateDocumentAsync(
                            entity,
                            approveSupplyMethodCode,
                            new ContractDraftVendorEditDocumentOptions(false, false, IsMarkReplaced: true),
                            ct,
                            hasAcceptor: true,
                            hasCommittee: true,
                            hasComment: true);

                        // Apply edited data back to source ContractDraftVendor
                        await this.ApplyChangesToSourceAsync(entity, ct);

                        _ = SendNotificationAsync(
                            entity,
                            UserId.From(entity.AuditInfo.CreatedBy),
                            NotificationConstant.InformCommittee.Title,
                            string.Format(
                                NotificationConstant.InformCommittee.Message,
                                ProgramConstant.ContractDraftVendorEdit.Name,
                                entity.ContractNumber));

                        entity.SetWaitingAddendumAssignment();

                        // Notify existing assignees to assign AddendumDrafter
                        foreach (var targetUserId in entity.Assignees.SelectMany(a => a.GetAssigneeNotificationTargets()))
                        {
                            _ = SendNotificationAsync(
                                entity,
                                targetUserId,
                                NotificationConstant.WaitForAssignment.Title,
                                string.Format(
                                    NotificationConstant.WaitForAssignment.Message,
                                    ProgramConstant.ContractDraftVendorEdit.Name,
                                    entity.ContractNumber));
                        }
                    }
                    else
                    {
                        // Intermediate approval — update documents with acceptor info
                        await this.UpdateDocumentAsync(
                            entity,
                            approveSupplyMethodCode,
                            new ContractDraftVendorEditDocumentOptions(false, false, IsMarkReplaced: false),
                            ct,
                            hasAcceptor: true,
                            hasCommittee: true,
                            hasComment: true);

                        // Notify next approver
                        var nextApprover = entity.Acceptors
                                                 .Where(a => a is { Type: AcceptorType.Approver, IsActive: true, IsCurrent: true, Status: AcceptorStatus.Pending })
                                                 .Select(DelegatorExtensions.DelegatorToAcceptor)
                                                 .FirstOrDefault();

                        if (nextApprover != null)
                        {
                            foreach (var targetUserId in nextApprover.GetNotificationTargets())
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
                    }

                    break;
                }

            default:
                return TypedResults.BadRequest($"ไม่สามารถอนุมัติในสถานะ {entity.Status} ได้");
        }

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private async Task ApplyChangesToSourceAsync(CaContractDraftVendorEdit entity, CancellationToken ct)
    {
        var source = await this.dbContext.CaContractDraftVendors
                               .Include(v => v.PaymentTerms)
                               .Include(v => v.DraftTermsConditions)
                               .Include(v => v.DraftEquipmentRental)
                               .Include(v => v.Attachments)
                               .ThenInclude(a => a.Files)
                               .SingleOrDefaultAsync(v => v.Id == entity.ContractDraftVendorId, ct);

        if (source is null)
        {
            return;
        }

        var editedCodes = entity.Components
                                .Where(c => c.IsEdited)
                                .Select(c => c.ComponentCode)
                                .ToHashSet();

        foreach (var code in editedCodes)
        {
            switch (code)
            {
                case "SalesAgreement":
                    if (entity.Agreement is not null)
                    {
                        source.SetAgreement(entity.Agreement);
                    }

                    break;

                case "PartOfContract":
                    SyncAttachments(entity, source);

                    break;

                case "Delivery":
                    source.SetDelivery(entity.Delivery);

                    break;

                case "Payment":
                    source.SetPayment(entity.Payment);
                    SyncPaymentTerms(entity, source);

                    break;

                case "Warranty":
                    source.SetWarranty(entity.DraftTermsConditions.Warranty);

                    break;

                case "ContractPerformance":
                    source.SetGuarantee(entity.DraftTermsConditions.Guarantee);

                    break;

                case "Mulct":
                    source.SetPenalty(entity.DraftTermsConditions.Penalty);

                    break;

                case "DefectWarranty":
                    source.SetDefectWarrantyTypeCode(entity.DraftTermsConditions.DefectWarrantyTypeCode);

                    break;

                case "Redelivery":
                    if (entity.DraftTermsConditions.RedeliveryCorrection is not null)
                    {
                        source.SetRedeliveryCorrection(entity.DraftTermsConditions.RedeliveryCorrection);
                    }

                    break;

                case "AdvancePayment":
                    source.SetAdvancePayment(entity.DraftTermsConditions.AdvancePayment);

                    break;

                case "RetentionPayment":
                    source.SetRetentionPayment(entity.DraftTermsConditions.RetentionPayment);

                    break;

                case "TerminationInfoDuration":
                case "TerminationInfoDate":
                    source.SetTermination(entity.Termination);

                    break;

                case "CopierLeaseInfo":
                    source.SetCopierLease(entity.DraftEquipmentRental.CopierLease);

                    break;

                case "ComputerLeaseInfo":
                    source.SetComputerLease(entity.DraftEquipmentRental.LeaseDuration);

                    break;

                case "CarLeaseInfo":
                    source.SetCarLease(entity.DraftEquipmentRental.CarLease);

                    break;

                case "Period":
                    source.SetPayment(entity.Payment);
                    SyncPaymentTerms(entity, source);

                    break;

                case "RentalFee":
                    source.SetCopierLease(entity.DraftEquipmentRental.CopierLease);
                    source.SetComputerLease(entity.DraftEquipmentRental.LeaseDuration);
                    source.SetCarLease(entity.DraftEquipmentRental.CarLease);

                    break;
            }
        }
    }

    private static void SyncPaymentTerms(CaContractDraftVendorEdit entity, CaContractDraftVendor source)
    {
        var newTerms = entity.PaymentTerms.Select(pt =>
        {
            var term = CaContractDraftPaymentTerm.Create();
            term.SetPaymentTermNo(pt.PaymentTermNo)
                .SetLeadTime(pt.LeadTime)
                .SetInstallmentPercentage(pt.InstallmentPercentage)
                .SetAmount(pt.Amount)
                .SetAdvanceDeductionAmount(pt.AdvanceDeductionAmount)
                .SetPerformanceDeductionAmount(pt.PerformanceDeductionAmount)
                .SetDescription(pt.Description)
                .SetSequence(pt.Sequence);

            if (pt.PeriodTypeCode.HasValue)
            {
                term.SetPeriodType(pt.PeriodTypeCode.Value);
            }

            return term;
        }).ToList();

        source.SetPaymentTerm(newTerms);
    }

    private static void SyncAttachments(CaContractDraftVendorEdit entity, CaContractDraftVendor source)
    {
        // Remove existing attachments from source
        foreach (var existing in source.Attachments.ToList())
        {
            source.RemoveAttachment(existing);
        }

        // Add new attachments from edit entity
        foreach (var editAttachment in entity.Attachments)
        {
            var newAttachment = CaContractDraftVendorsAttachment.Create(
                editAttachment.TypeCode,
                editAttachment.Description,
                editAttachment.PageNumber,
                editAttachment.Sequence,
                editAttachment.FormatOtherName);

            foreach (var editFile in editAttachment.Files)
            {
                var newFile = CaContractDraftVendorsAttachmentFile.Create(
                    editFile.Id.Value,
                    editFile.FileName,
                    editFile.FileType,
                    editFile.Sequence);

                newAttachment.AddFile(newFile);
            }

            source.SetAttachments(newAttachment);
        }
    }

    private static async Task SendNotificationAsync(CaContractDraftVendorEdit entity, UserId userId, string title, string message)
    {
        await Notification
              .Crate(userId, title, message, NotificationProgram.ContractManagement)
              .SetReferenceId(entity.Id.Value)
              .SetLinkUrl(string.Format(ProgramConstant.ContractDraftVendorEdit.Url, entity.Id.Value), "ดูรายละเอียด")
              .PublishAsync(CancellationToken.None);
    }
}