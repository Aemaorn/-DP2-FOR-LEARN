namespace GHB.DP2.Application.Features.ContractManagement.ContractGuaranteeReturn;

using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.ContractManagement.ContractGuaranteeReturn.Abstract;
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

public record RejectContractGuaranteeReturnRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)] Guid UserId,
    Guid ContractDraftVendorId,
    Guid Id,
    string Remark);

public class RejectContractGuaranteeReturnEndpoint : ContractGuaranteeReturnEndpoint<RejectContractGuaranteeReturnRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public RejectContractGuaranteeReturnEndpoint(
        Dp2DbContext dbContext,
        IFileServiceClient fileServiceClient,
        ILogger<RejectContractGuaranteeReturnEndpoint> logger,
        IOperationService operationService)
        : base(logger, dbContext, fileServiceClient, operationService) => this.dbContext = dbContext;

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("ContractManagement/ContractGuaranteeReturn")
             .WithName("RejectContractGuaranteeReturn")
             .AllowAnonymous()
             .Accepts<RejectContractGuaranteeReturnEndpoint>("application/json"));

        this.Post("contract/{ContractDraftVendorId:guid}/contract-guarantee-return/{Id:guid}/reject");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(
        RejectContractGuaranteeReturnRequest req, CancellationToken ct)
    {
        var entity = await this.GetByIdAsync(ContractDraftVendorId.From(req.ContractDraftVendorId), ct);

        var invitationVendor = entity.ContractInvitationVendors;
        var suVendor = entity.ContractDraft.Procurement.Type is ProcurementType.Procurement
            ? invitationVendor?.PurchaseOrderApprovalContract?.Entrepreneur?.SuVendor
            : invitationVendor?.PurchaseOrderApprovalContract?.PrincipleApprovalRentalEntrepreneurs?.Vendor;

        if (suVendor is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลผู้ประกอบการ");
        }

        var guarantee = entity.CmContractGuaranteeReturns.FirstOrDefault(t => t.Id.Value == req.Id);

        if (guarantee is null)
        {
            this.ThrowError($"ไม่พบสัญญาที่มีรหัส {req.ContractDraftVendorId}", StatusCodes.Status404NotFound);
        }

        switch (guarantee.Status)
        {
            case CmContractGuaranteeReturnStatus.WaitingCommitteeApproval:
                this.TryUpdateCommitteeStatusAfterReject(guarantee, UserId.From(req.UserId), req.Remark);

                if (guarantee.Status == CmContractGuaranteeReturnStatus.Rejected)
                {
                    _ = SendNotificationAsync(
                        guarantee,
                        UserId.From(guarantee.AuditInfo.CreatedBy),
                        NotificationConstant.ReturnToCreator.Title,
                        string.Format(
                            NotificationConstant.ReturnToCreator.Message,
                            ProgramConstant.ContractGuaranteeReturn.Name,
                            string.Empty));
                }

                break;

            case CmContractGuaranteeReturnStatus.WaitingAssigned:
                this.AssigneeRejected(guarantee, UserId.From(req.UserId), req.Remark);

                _ = SendNotificationAsync(
                    guarantee,
                    UserId.From(guarantee.AuditInfo.CreatedBy),
                    NotificationConstant.ReturnToCreator.Title,
                    string.Format(
                        NotificationConstant.ReturnToCreator.Message,
                        ProgramConstant.ContractGuaranteeReturn.Name,
                        string.Empty));

                break;

            case CmContractGuaranteeReturnStatus.WaitingAcceptance:
                this.ApproverRejected(guarantee, UserId.From(req.UserId), req.Remark);

                foreach (var targetUserId in guarantee.Assignees.SelectMany(a => a.GetAssigneeNotificationTargets()))
                {
                    _ = SendNotificationAsync(
                        guarantee,
                        targetUserId,
                        NotificationConstant.ReturnToCreator.Title,
                        string.Format(
                            NotificationConstant.ReturnToCreator.Message,
                            ProgramConstant.ContractGuaranteeReturn.Name,
                            string.Empty));
                }

                break;
        }

        this.dbContext.CmContractGuaranteeReturns.Update(guarantee);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
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
              .SetLinkUrl(string.Format(ProgramConstant.ContractGuaranteeReturn.Url, entity.ContractDraftVendorId, entity.Id), "ดูรายละเอียด")
              .PublishAsync(CancellationToken.None);
    }

    private void TryUpdateCommitteeStatusAfterReject(CmContractGuaranteeReturn entity, UserId userId, string? remark)
    {
        var committeeAcceptors = entity.Acceptors
                                       .Where(a => a is
                                       {
                                           Type: AcceptorType.AcceptanceCommittee,
                                           Status: AcceptorStatus.Pending,
                                           IsActive: true,
                                           IsUnableToPerformDuties: false,
                                       })
                                       .ToList();

        var acceptor =
            committeeAcceptors
                .FirstOrDefault(a => a.UserId == userId);

        if (acceptor is null)
        {
            this.ThrowError(
                "ไม่พบผู้อนุมัติคณะกรรมการที่ใช้งานได้",
                StatusCodes.Status400BadRequest);
        }

        if (committeeAcceptors.Any(c => !c.IsBoardChairman()) &&
            acceptor.IsBoardChairman())
        {
            this.ThrowError(
                "ผู้อนุมัติคณะกรรมการไม่ตรงกับตำแหน่งที่กำหนด",
                StatusCodes.Status400BadRequest);
        }

        acceptor.Reject(remark);
        entity.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.CommitteeReject,
            ActivityLogActionTypeConstant.CommitteeReject,
            entity.Status.ToString(),
            remark));

        if (entity.HasMajorityRejection() || acceptor.IsBoardChairman())
        {
            entity.SetStatusRejected();
        }
    }

    private void AssigneeRejected(CmContractGuaranteeReturn entity, UserId userId, string? remark)
    {
        var assignee = entity.Assignees
                             .Select(DelegatorExtensions.DelegatorToAssignee)
                             .Where(a => a.Delegatee?.SuUserId == null
                                                            ? a.UserId == userId
                                                            : a.Delegatee?.SuUserId == userId)
                             .OrderBy(o => o.Sequence)
                             .SingleOrDefault();

        if (assignee is null)
        {
            this.ThrowError("ไม่พบผู้รับผิดชอบ", StatusCodes.Status404NotFound);
        }

        var currentAcceptorUser = entity.Assignees.FirstOrDefault(a => a.Id == assignee.Id);

        if (currentAcceptorUser is null)
        {
            this.ThrowError(
                "ไม่พบผู้อนุมัติที่ใช้งานได้",
                StatusCodes.Status400BadRequest);
        }

        currentAcceptorUser
            .SetDelegatee(assignee.DelegateeId)
            .Reject(remark: remark);

        entity.SetStatusRejected()
              .AddActivity(new ActivityInfo(
                  ActivityLogActionTypeConstant.AssigneeReject,
                  ActivityLogActionTypeConstant.AssigneeReject,
                  entity.Status.ToString(),
                  remark));
    }

    private void ApproverRejected(CmContractGuaranteeReturn entity, UserId userId, string? remark)
    {
        var acceptors = entity.Acceptors
                              .Where(a => a is { Type: AcceptorType.Approver, IsActive: true, Status: AcceptorStatus.Pending })
                              .OrderBy(a => a.Sequence)
                              .ToArray();

        var currentAcceptor = acceptors.FirstOrDefault();

        var current = acceptors.Select(DelegatorExtensions.DelegatorToAcceptor)
                               .SingleOrDefault(a => a.Delegatee?.SuUserId == null
                                                            ? a.UserId == userId
                                                            : a.Delegatee?.SuUserId == userId);

        if (current is null || currentAcceptor is null)
        {
            this.ThrowError("ไม่พบผู้อนุมัติในกลุ่มหรือขั้นตอนนี้", StatusCodes.Status404NotFound);
        }

        if (current.Sequence != currentAcceptor.Sequence)
        {
            this.ThrowError("ลำดับอนุมัติไม่ถูกต้อง", StatusCodes.Status404NotFound);
        }

        var currentAcceptorUser = entity.Acceptors.FirstOrDefault(a => a.Id == current.Id);

        if (currentAcceptorUser is null)
        {
            this.ThrowError(
                "ไม่พบผู้อนุมัติที่ใช้งานได้",
                StatusCodes.Status400BadRequest);
        }

        currentAcceptorUser
            .SetDelegatee(current.DelegateeId)
            .Reject(remark: remark);

        entity.SetStatusWaitingAssigned()
              .AddActivity(new ActivityInfo(
                  ActivityLogActionTypeConstant.Reject,
                  ActivityLogActionTypeConstant.Reject,
                  entity.Status.ToString(),
                  remark));
    }
}