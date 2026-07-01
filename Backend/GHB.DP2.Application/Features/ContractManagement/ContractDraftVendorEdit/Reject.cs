namespace GHB.DP2.Application.Features.ContractManagement.ContractDraftVendorEdit;

using System.IdentityModel.Tokens.Jwt;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.ContractManagement.ContractDraftVendorEdit.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractManagement.CaContractDraftVendorEdit;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record RejectContractDraftVendorEditRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid Id,
    string Remark);

public class RejectContractDraftVendorEditEndpoint
    : ContractDraftVendorEditEndpoint<RejectContractDraftVendorEditRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public RejectContractDraftVendorEditEndpoint(
        ILogger<RejectContractDraftVendorEditEndpoint> logger,
        Dp2DbContext dbContext)
        : base(logger, dbContext)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("ContractManagement/ContractDraftVendorEdit")
             .WithName("RejectContractDraftVendorEdit")
             .AllowAnonymous()
             .Accepts<RejectContractDraftVendorEditRequest>("application/json"));

        this.Post("contract/contract-draft-vendor-edit/{Id:guid}/reject");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>>
        HandleRequestAsync(RejectContractDraftVendorEditRequest req, CancellationToken ct)
    {
        var entity = await this.GetEditByIdAsync(ContractDraftVendorEditId.From(req.Id), ct);

        switch (entity.Status)
        {
            case ContractDraftVendorEditStatus.WaitingCommitteeApproval:
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

                    var current = committeeAcceptors.FirstOrDefault(a => a.UserId == UserId.From(req.UserId));

                    if (current is null)
                    {
                        return TypedResults.BadRequest("ไม่พบผู้เห็นชอบในกลุ่มคณะกรรมการ หรือดำเนินการแล้ว");
                    }

                    // Check if board chairman
                    var isBoardChairman = current.CommitteePositionsCode?.Value == "PCommitteePosition001";

                    current.Reject(req.Remark);

                    // Check if should set rejected — chairman rejects or majority
                    var totalActive = entity.Acceptors
                        .Count(a => a is { Type: AcceptorType.AcceptanceCommittee, IsActive: true, IsUnableToPerformDuties: false });
                    var totalRejected = entity.Acceptors
                        .Count(a => a is { Type: AcceptorType.AcceptanceCommittee, Status: AcceptorStatus.Rejected, IsActive: true });

                    if (isBoardChairman || totalRejected > totalActive / 2)
                    {
                        entity.SetRejected(req.Remark);

                        // Update documents with IsMarkReplaced
                        var committeeSupplyMethodCode = await this.GetSupplyMethodCodeAsync(entity, ct);
                        await this.UpdateDocumentAsync(
                            entity,
                            committeeSupplyMethodCode,
                            new ContractDraftVendorEditDocumentOptions(false, false, IsMarkReplaced: true),
                            ct,
                            hasCommittee: true);

                        _ = SendNotificationAsync(
                            entity,
                            UserId.From(entity.AuditInfo.CreatedBy),
                            NotificationConstant.ReturnToCreator.Title,
                            string.Format(
                                NotificationConstant.ReturnToCreator.Message,
                                ProgramConstant.ContractDraftVendorEdit.Name,
                                string.Empty));
                    }

                    break;
                }

            case ContractDraftVendorEditStatus.WaitingComment:
                {
                    var director = entity.Assignees
                        .Select(DelegatorExtensions.DelegatorToAssignee)
                        .FirstOrDefault(a =>
                            a.Type == AssigneeType.Director
                            && (a.UserId == UserId.From(req.UserId) ||
                                (a.Delegatee != null && a.Delegatee.SuUserId == UserId.From(req.UserId))));

                    if (director is null)
                    {
                        return TypedResults.BadRequest("ไม่พบผู้มอบหมาย หรือไม่มีสิทธิ์ส่งกลับในสถานะนี้");
                    }

                    director.SetRemark(req.Remark);
                    entity.SetRejected(req.Remark);

                    var directorSupplyMethodCode = await this.GetSupplyMethodCodeAsync(entity, ct);
                    await this.UpdateDocumentAsync(
                        entity,
                        directorSupplyMethodCode,
                        new ContractDraftVendorEditDocumentOptions(false, false, IsMarkReplaced: true),
                        ct);

                    _ = SendNotificationAsync(
                        entity,
                        UserId.From(entity.AuditInfo.CreatedBy),
                        NotificationConstant.ReturnToCreator.Title,
                        string.Format(
                            NotificationConstant.ReturnToCreator.Message,
                            ProgramConstant.ContractDraftVendorEdit.Name,
                            string.Empty));

                    break;
                }

            case ContractDraftVendorEditStatus.WaitingAssignment:
                {
                    var assignee = entity.Assignees
                        .Select(DelegatorExtensions.DelegatorToAssignee)
                        .FirstOrDefault(a =>
                            a.UserId == UserId.From(req.UserId) ||
                            (a.Delegatee != null && a.Delegatee.SuUserId == UserId.From(req.UserId)));

                    if (assignee is null)
                    {
                        return TypedResults.BadRequest("ไม่พบผู้รับผิดชอบ");
                    }

                    assignee.SetRemark(req.Remark);
                    entity.SetRejected(req.Remark);

                    // Update documents with IsMarkReplaced
                    var assigneeSupplyMethodCode = await this.GetSupplyMethodCodeAsync(entity, ct);
                    await this.UpdateDocumentAsync(
                        entity,
                        assigneeSupplyMethodCode,
                        new ContractDraftVendorEditDocumentOptions(false, false, IsMarkReplaced: true),
                        ct);

                    _ = SendNotificationAsync(
                        entity,
                        UserId.From(entity.AuditInfo.CreatedBy),
                        NotificationConstant.ReturnToCreator.Title,
                        string.Format(
                            NotificationConstant.ReturnToCreator.Message,
                            ProgramConstant.ContractDraftVendorEdit.Name,
                            string.Empty));

                    break;
                }

            case ContractDraftVendorEditStatus.WaitingApproval:
                {
                    var approvers = entity.Acceptors
                        .Where(a => a is { Type: AcceptorType.Approver, IsActive: true, Status: AcceptorStatus.Pending })
                        .Select(DelegatorExtensions.DelegatorToAcceptor)
                        .OrderBy(a => a.Sequence)
                        .ToList();

                    var current = approvers
                        .FirstOrDefault(a => a.Delegatee?.SuUserId == null
                            ? a.UserId == UserId.From(req.UserId)
                            : a.Delegatee?.SuUserId == UserId.From(req.UserId));

                    if (current is null)
                    {
                        return TypedResults.BadRequest("ไม่พบผู้อนุมัติในลำดับนี้");
                    }

                    var actualAcceptor = entity.Acceptors.FirstOrDefault(a => a.Id == current.Id);
                    actualAcceptor?.SetDelegatee(current.DelegateeId).Reject(req.Remark);

                    // Reject back to assignee
                    entity.SetRejectedToAssignee(req.Remark);

                    // Update documents with IsMarkReplaced (use WaitingComment context like DA)
                    var approverSupplyMethodCode = await this.GetSupplyMethodCodeAsync(entity, ct);
                    await this.UpdateDocumentAsync(
                        entity,
                        approverSupplyMethodCode,
                        new ContractDraftVendorEditDocumentOptions(false, false, IsMarkReplaced: true),
                        ct,
                        hasAcceptor: true,
                        hasCommittee: true);

                    foreach (var targetUserId in entity.Assignees.SelectMany(a => a.GetAssigneeNotificationTargets()))
                    {
                        _ = SendNotificationAsync(
                            entity,
                            targetUserId,
                            NotificationConstant.ReturnToCreator.Title,
                            string.Format(
                                NotificationConstant.ReturnToCreator.Message,
                                ProgramConstant.ContractDraftVendorEdit.Name,
                                string.Empty));
                    }

                    break;
                }

            default:
                return TypedResults.BadRequest($"ไม่สามารถส่งกลับในสถานะ {entity.Status} ได้");
        }

        this.dbContext.CaContractDraftVendorEdits.Update(entity);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
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
