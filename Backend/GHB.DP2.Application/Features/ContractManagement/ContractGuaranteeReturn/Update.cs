namespace GHB.DP2.Application.Features.ContractManagement.ContractGuaranteeReturn;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client.Abstractions;
using FluentValidation;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Features.ContractManagement.ContractGuaranteeReturn.Abstract;
using GHB.DP2.Application.Features.ContractManagement.ContractGuaranteeReturn.Dto;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractManagement.CmContractGuaranteeReturn;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using GHB.DP2.Application.Extensions;

public record UpdateContractGuaranteeReturnRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid Id,
    Guid ContractDraftVendorId,
    DateTimeOffset GuaranteeReturnDate,
    string? ContractDescription,
    string? ProofOfPaymentDescription,
    string? GuranteeDescription,
    decimal ReturnAmount,
    bool IsDeducted,
    decimal? DeductedAmount,
    decimal NetReturnAmount,
    string? AdditionalComment,
    DateTimeOffset? DisbursementDate,
    decimal? DisbursementAmount,
    string? DisbursementRemark,
    CmContractGuaranteeReturnStatus Status,
    IEnumerable<AcceptorRequest>? Acceptors,
    IEnumerable<AssigneeRequest>? Assignees,
    IEnumerable<ConditionRequest>? Conditions,
    IEnumerable<RequiredDocumentRequest>? RequiredDocuments,
    bool IsApprovalCmContractGuaranteeReturnDocumentIdReplaced,
    bool IsContractGuaranteeReturnResultDocumentIdReplaced,
    DateTimeOffset? DocumentDate = null);

public record UpdateContractGuaranteeReturnResponse(
    Guid? NewApprovalDocumentFileId,
    Guid? NewReturnDocumentFileId);

public class UpdateContractGuaranteeReturnValidator : Validator<UpdateContractGuaranteeReturnRequest>
{
    public UpdateContractGuaranteeReturnValidator()
    {
        this.RuleFor(x => x.ContractDraftVendorId)
            .NotEmpty()
            .WithMessage("กรุณาระบุรหัสสัญญา");
        this.RuleFor(x => x.GuaranteeReturnDate)
            .NotEmpty()
            .WithMessage("กรุณาระบุวันที่คืนหลักประกัน");
        this.RuleFor(x => x.ReturnAmount)
            .GreaterThan(0)
            .WithMessage("จำนวนเงินคืนหลักประกันต้องมากกว่า 0");
        this.RuleFor(x => x.NetReturnAmount)
            .GreaterThan(0)
            .WithMessage("จำนวนเงินสุทธิคืนหลักประกันต้องมากกว่า 0");
        this.RuleFor(x => x.IsDeducted)
            .NotNull()
            .WithMessage("กรุณาระบุว่ามีการหักเงินคืนหลักประกันหรือไม่");
        this.RuleFor(x => x.DeductedAmount)
            .GreaterThanOrEqualTo(0)
            .When(x => x.IsDeducted)
            .WithMessage("จำนวนเงินหักคืนหลักประกันต้องมากกว่าหรือเท่ากับ 0");

        this.When(x => x.Status == CmContractGuaranteeReturnStatus.WaitingCommitteeApproval, () =>
        {
            this.RuleFor(x => x.Acceptors)
                .NotNull().WithMessage("ต้องระบุผู้มีอำนาจเห็นชอบ/อนุมัติอย่างน้อย 1 คน")
                .Must(a => a != null && a.Any())
                .WithMessage("ต้องระบุผู้มีอำนาจเห็นชอบ/อนุมัติอย่างน้อย 1 คน");
            this.RuleFor(x => x.Assignees)
                .NotNull().WithMessage("ต้องระบุผู้รับผิดชอบอย่างน้อย 1 คน")
                .Must(a => a != null && a.Any())
                .WithMessage("ต้องระบุผู้รับผิดชอบอย่างน้อย 1 คน");
        });

        this.When(x => x.Status == CmContractGuaranteeReturnStatus.Paid, () =>
        {
            this.RuleFor(x => x.DisbursementDate)
                .NotNull()
                .WithMessage("กรุณาระบุวันที่เบิกจ่าย");
            this.RuleFor(x => x.DisbursementAmount)
                .NotNull()
                .GreaterThan(0)
                .WithMessage("จำนวนเงินเบิกจ่ายต้องมากกว่า 0");
        });
    }
}

public class UpdateContractGuaranteeReturnEndpoint : ContractGuaranteeReturnEndpoint<UpdateContractGuaranteeReturnRequest, Results<Ok<UpdateContractGuaranteeReturnResponse>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public UpdateContractGuaranteeReturnEndpoint(
        ILogger<UpdateContractGuaranteeReturnEndpoint> logger,
        Dp2DbContext dbContext,
        IFileServiceClient fileServiceClient,
        IOperationService operationService)
        : base(logger, dbContext, fileServiceClient, operationService)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Put("contract/{ContractDraftVendorId:guid}/contract-guarantee-return/{Id:guid}");
        this.Description(b => b
                              .WithTags("ContractManagement/ContractGuaranteeReturn")
                              .WithName("UpdateContractGuaranteeReturn")
                              .AllowAnonymous()
                              .Produces<Ok>()
                              .ProducesProblem(StatusCodes.Status400BadRequest)
                              .ProducesProblem(StatusCodes.Status500InternalServerError));
    }

    protected override async ValueTask<Results<Ok<UpdateContractGuaranteeReturnResponse>, NotFound<string>>> HandleRequestAsync(UpdateContractGuaranteeReturnRequest req, CancellationToken ct)
    {
        Guid? newApprovalDocumentFileId = null;
        Guid? newReturnDocumentFileId = null;

        var entity = await this.GetByIdAsync(ContractDraftVendorId.From(req.ContractDraftVendorId), ct);

        var invitationVendor = entity.ContractInvitationVendors;
        var suVendor = entity.ContractDraft.Procurement.Type is ProcurementType.Procurement
            ? invitationVendor?.PurchaseOrderApprovalContract?.Entrepreneur?.SuVendor
            : invitationVendor?.PurchaseOrderApprovalContract?.PrincipleApprovalRentalEntrepreneurs?.Vendor;

        if (suVendor is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลผู้ประกอบการ");
        }

        var guarantee = entity.CmContractGuaranteeReturns.SingleOrDefault(t => t.Id.Value == req.Id);

        if (guarantee is null)
        {
            this.ThrowError($"ไม่พบสัญญาที่มีรหัส {req.ContractDraftVendorId}", StatusCodes.Status404NotFound);
        }

        if (req.Acceptors != null)
        {
            await this.UpsertAcceptors(guarantee, [.. req.Acceptors], req.Status, ct, UserId.From(req.UserId));
        }

        if (req.Assignees != null)
        {
            var newAssigneeUserIds = req.Assignees.Where(x => !x.Id.HasValue)
                                                  .Select(x => UserId.From(x.UserId))
                                                  .ToHashSet();

            await this.UpsertAssignee(guarantee, [.. req.Assignees], ct, UserId.From(req.UserId));

            foreach (var targetUserId in guarantee.Assignees
                         .Where(a => newAssigneeUserIds.Contains(a.UserId))
                         .SelectMany(a => a.GetAssigneeNotificationTargets()))
            {
                _ = SendNotificationAsync(
                    guarantee,
                    targetUserId,
                    NotificationConstant.Assignment.Title,
                    string.Format(
                        NotificationConstant.Assignment.Message,
                        ProgramConstant.ContractGuaranteeReturn.Name,
                        guarantee.GuaranteeNumber.Value));
            }
        }

        if (req.Conditions != null)
        {
            this.UpsertConditions(guarantee, req.Conditions);
        }

        if (req.RequiredDocuments != null)
        {
            this.UpsertRequiredDocument(guarantee, req.RequiredDocuments);
        }

        var previousStatus = guarantee.Status;

        guarantee.SetValues(
                     req.GuaranteeReturnDate,
                     req.ReturnAmount,
                     req.IsDeducted,
                     req.DeductedAmount,
                     req.NetReturnAmount,
                     req.AdditionalComment)
                 .SetStatus(req.Status);

        guarantee.SetDescriptions(
            req.ContractDescription,
            req.ProofOfPaymentDescription,
            req.GuranteeDescription);

        guarantee.SetDisbursement(
            req.DisbursementDate,
            req.DisbursementAmount,
            req.DisbursementRemark);

        if (req.Status == CmContractGuaranteeReturnStatus.WaitingCommitteeApproval || req.DocumentDate is not null)
        {
            guarantee.SetDocumentDate(req.DocumentDate);
        }

        var isApprovalReplace = req.IsApprovalCmContractGuaranteeReturnDocumentIdReplaced;
        var isResultReplace = req.IsContractGuaranteeReturnResultDocumentIdReplaced;

        switch (req.Status)
        {
            case CmContractGuaranteeReturnStatus.WaitingCommitteeApproval:
                guarantee.Acceptors
                         .Where(w => w is { Type: AcceptorType.AcceptanceCommittee, IsUnableToPerformDuties: false, IsActive: true })
                         .Iter(r => r.Pending());

                var committeeMembers = guarantee.Acceptors
                    .Where(w => w is { Type: AcceptorType.AcceptanceCommittee, IsUnableToPerformDuties: false, IsActive: true })
                    .Where(a => a.Status == AcceptorStatus.Pending)
                    .ToList();

                foreach (var member in committeeMembers)
                {
                    _ = SendNotificationAsync(
                        guarantee,
                        member.UserId,
                        NotificationConstant.WaitForLike.Title,
                        string.Format(
                            NotificationConstant.WaitForLike.Message,
                            ProgramConstant.ContractGuaranteeReturn.Name,
                            guarantee.GuaranteeNumber.Value));
                }

                var options = new GuaranteeReturnDocumentOptions(isApprovalReplace, isResultReplace, true, false, false, false, IsMarkReplaced: true);
                var (approvalFileId, returnFileId) = await this.UpdateDocumentAsync(entity, guarantee, suVendor, options, ct);
                newApprovalDocumentFileId = approvalFileId?.Value;
                newReturnDocumentFileId = returnFileId?.Value;

                guarantee.AddActivity(new ActivityInfo(
                    ActivityLogActionTypeConstant.SendCommitteeApprove,
                    ActivityLogActionTypeConstant.SendCommitteeApprove,
                    nameof(CmContractGuaranteeReturnStatus.WaitingCommitteeApproval)));

                break;

            case CmContractGuaranteeReturnStatus.Assigned:
                var optionsAssigned = new GuaranteeReturnDocumentOptions(isApprovalReplace, isResultReplace, false, false, false, true);
                var (approvalAssignedFileId, returnAssignedFileId) = await this.UpdateDocumentAsync(entity, guarantee, suVendor, optionsAssigned, ct);
                newApprovalDocumentFileId = approvalAssignedFileId?.Value;
                newReturnDocumentFileId = returnAssignedFileId?.Value;

                guarantee.AddActivity(new ActivityInfo(
                    ActivityLogActionTypeConstant.Assigned,
                    ActivityLogActionTypeConstant.Assigned,
                    nameof(CmContractGuaranteeReturnStatus.Assigned)));

                break;

            case CmContractGuaranteeReturnStatus.WaitingAcceptance:
                guarantee.Acceptors
                         .Where(w => w is { Type: AcceptorType.Approver, IsActive: true })
                         .Iter(r => r.Pending());

                var firstApprover = guarantee.Acceptors
                    .Where(w => w is { Type: AcceptorType.Approver, IsActive: true, Status: AcceptorStatus.Pending })
                    .OrderBy(a => a.Sequence)
                    .FirstOrDefault();

                if (firstApprover is not null)
                {
                    foreach (var targetUserId in firstApprover.GetNotificationTargets())
                    {
                        _ = SendNotificationAsync(
                            guarantee,
                            targetUserId,
                            NotificationConstant.WaitForLike.Title,
                            string.Format(
                                NotificationConstant.WaitForLike.Message,
                                ProgramConstant.ContractGuaranteeReturn.Name,
                                guarantee.GuaranteeNumber.Value));
                    }
                }

                var optionsWaitingAcceptance = new GuaranteeReturnDocumentOptions(isApprovalReplace, isResultReplace, false, false, false, true, IsMarkReplaced: true);
                var (approvalWaitingAcceptanceFileId, returnWaitingAcceptanceFileId) = await this.UpdateDocumentAsync(entity, guarantee, suVendor, optionsWaitingAcceptance, ct);
                newApprovalDocumentFileId = approvalWaitingAcceptanceFileId?.Value;
                newReturnDocumentFileId = returnWaitingAcceptanceFileId?.Value;

                guarantee.AddActivity(new ActivityInfo(
                    ActivityLogActionTypeConstant.SendApprove,
                    ActivityLogActionTypeConstant.SendApprove,
                    nameof(CmContractGuaranteeReturnStatus.WaitingAcceptance)));

                break;

            case CmContractGuaranteeReturnStatus.Paid:
                guarantee.AddActivity(new ActivityInfo(
                    ActivityLogActionTypeConstant.Update,
                    "ยืนยันวันที่เบิกจ่าย",
                    nameof(CmContractGuaranteeReturnStatus.Paid)));

                break;
        }

        if (previousStatus == req.Status)
        {
            var optionsRegular = new GuaranteeReturnDocumentOptions(isApprovalReplace, isResultReplace, true, false, false, false);
            var (approvalRegularFileId, returnRegularFileId) = await this.UpdateDocumentAsync(entity, guarantee, suVendor, optionsRegular, ct);
            newApprovalDocumentFileId = approvalRegularFileId?.Value;
            newReturnDocumentFileId = returnRegularFileId?.Value;
        }

        await this.dbContext.SaveChangesAsync(CancellationToken.None);

        return TypedResults.Ok(new UpdateContractGuaranteeReturnResponse(newApprovalDocumentFileId, newReturnDocumentFileId));
    }

    private static async Task SendNotificationAsync(CmContractGuaranteeReturn entity, UserId userId, string title, string message)
    {
        await Notification
              .Crate(
                  userId,
                  title,
                  message,
                  NotificationProgram.ContractManagement)
              .SetReferenceId(entity.Id.Value)
              .SetLinkUrl(string.Format(ProgramConstant.ContractGuaranteeReturn.Url, entity.ContractDraftVendorId, entity.Id.Value), "ดูรายละเอียด")
              .PublishAsync(CancellationToken.None);
    }

    protected new async ValueTask SetDefaultDocumentTemplate(CmContractGuaranteeReturn contractGuaranteeReturn, ParameterCode supplyMethodCode, CancellationToken ct)
    {
        var approvalTemplateDocId = await this.GetDocumentTemplateByTypeAsync(
            CmContractGuaranteeReturnDocumentType.ApprovalCmContractGuaranteeReturn,
            supplyMethodCode,
            ct);

        var contractGuaranteeReturnTemplateDocId = await this.GetDocumentTemplateByTypeAsync(
            CmContractGuaranteeReturnDocumentType.CmContractGuaranteeReturnResule,
            supplyMethodCode,
            ct);

        contractGuaranteeReturn.AddDocumentHistory(CmContractGuaranteeReturnDocumentType.CmContractGuaranteeReturnResule, contractGuaranteeReturnTemplateDocId, false);
        contractGuaranteeReturn.AddDocumentHistory(CmContractGuaranteeReturnDocumentType.ApprovalCmContractGuaranteeReturn, approvalTemplateDocId, false);
    }
}