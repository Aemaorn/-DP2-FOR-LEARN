namespace GHB.DP2.Application.Features.ContractManagement.ContractTermination;

using System.IdentityModel.Tokens.Jwt;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.ContractManagement.ContractTermination.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractManagement.CmContractTermination;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record ApproveContractTerminationRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid ContractDraftVendorId,
    Guid Id,
    string? Remark);

public class ApproveContractTerminationEndpoint : ContractTerminationEndpoint<ApproveContractTerminationRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public ApproveContractTerminationEndpoint(
        Dp2DbContext dbContext,
        ILogger<ApproveContractTerminationEndpoint> logger)
        : base(logger, dbContext)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("ContractManagement/ContractTermination")
             .WithName("ApproveContractTermination")
             .Accepts<ApproveContractTerminationRequest>("application/json"));
        this.Post("contract/{ContractDraftVendorId:guid}/contract-termination/{Id:guid}/approve");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(ApproveContractTerminationRequest req, CancellationToken ct)
    {
        var entity = await this.GetByIdAsync(ContractDraftVendorId.From(req.ContractDraftVendorId), ct);

        var termination = entity.CmContractTerminations.FirstOrDefault(s => s.Id == CmContractTerminationId.From(req.Id));

        var delivery = entity.Delivery;

        var suVendor = this.MapSuVendorByType(entity.ContractInvitationVendors, entity.ContractDraft.Procurement.Type);

        if (suVendor is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลผู้ประกอบการ");
        }

        if (termination == null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลการยกเลิกสัญญา");
        }

        var type = termination.Status switch
        {
            CmContractTerminationStatus.WaitingCommitteeApproval => AcceptorType.AcceptanceCommittee,
            _ => AcceptorType.Approver,
        };

        var acceptors = termination.Acceptors
                                   .Where(a => a.Type == type && a.IsActive)
                                   .OrderBy(a => a.Sequence)
                                   .ToList();

        var current = acceptors.FirstOrDefault(a => a.UserId == UserId.From(req.UserId));

        if (type != AcceptorType.AcceptanceCommittee)
        {
            current = acceptors.Select(DelegatorExtensions.DelegatorToAcceptor)
                               .SingleOrDefault(a => a.Delegatee?.SuUserId == null
                                   ? a.UserId == UserId.From(req.UserId)
                                   : a.Delegatee?.SuUserId == UserId.From(req.UserId));
        }

        if (current == null)
        {
            return TypedResults.BadRequest("ไม่พบผู้อนุมัติในกลุ่มหรือขั้นตอนนี้");
        }

        if (!IsGroupAllowedToApprove(termination.Status, current.Type))
        {
            return TypedResults.BadRequest(GetGroupNotAllowedMessage(termination.Status));
        }

        var currentAcceptorUser = termination.Acceptors.FirstOrDefault(a => a.Id == current.Id);

        if (currentAcceptorUser is null)
        {
            this.ThrowError(
                "ไม่พบผู้อนุมัติที่ใช้งานได้",
                StatusCodes.Status400BadRequest);
        }

        currentAcceptorUser
            .SetDelegatee(current.DelegateeId)
            .Approve(remark: req.Remark);

        current.SetCurrent(false);

        UpdateSequentialCurrents(termination, type);

        switch (termination.Status)
        {
            case CmContractTerminationStatus.WaitingCommitteeApproval:
                var lastedDoc = termination.DocumentHistories
                                           .Where(d => d.DocumentType == CmContractTerminationDocumentType.ContractTermination)
                                           .OrderVersions()
                                           .FirstOrDefault();

                if (lastedDoc != null)
                {
                    await this.CopyCheckpointDocumentAsync(termination, lastedDoc, ct, isReplace: false, incrementMajor: false);
                }

                TryUpdateCommitteeStatus(termination, current);

                if (termination.Status == CmContractTerminationStatus.WaitingAssign)
                {
                    var latestDocument = termination.DocumentHistories
                                                    .Where(d => d.DocumentType == CmContractTerminationDocumentType.ContractTermination)
                                                    .OrderVersions()
                                                    .FirstOrDefault();

                    if (latestDocument != null)
                    {
                        await this.CopyCheckpointDocumentAsync(termination, latestDocument, ct, isReplace: true);
                    }
                }

                termination.AddActivity(
                    new ActivityInfo(
                        ActivityLogActionTypeConstant.CommitteeApproved,
                        string.Empty,
                        termination.Status.ToString(),
                        req.Remark));

                break;

            case CmContractTerminationStatus.WaitingApproval:
                termination.AddActivity(
                    new ActivityInfo(
                        ActivityLogActionTypeConstant.Approved,
                        string.Empty,
                        termination.Status.ToString(),
                        req.Remark));

                await this.ReplaceAcceptorsAsync(entity, termination, ct);

                if (acceptors.All(a => a.Status == AcceptorStatus.Approved))
                {
                    entity.SetContractStatus(ContractStatus.Cancel);
                    termination.SetStatus(CmContractTerminationStatus.Approved);
                    await this.StampAcceptorDateAsync(termination, ct);
                }

                break;
        }

        this.dbContext.CmContractTerminations.Update(termination);

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private static void UpdateSequentialCurrents(CmContractTermination termination, AcceptorType type)
    {
        var approvers = termination.Acceptors
                                   .Where(a => a.Type == type && a.IsActive)
                                   .OrderBy(a => a.Sequence)
                                   .ToList();

        if (approvers.Count == 0)
        {
            return;
        }

        foreach (var a in approvers)
        {
            a.SetCurrent(false);
        }

        var next = approvers.FirstOrDefault(a =>
            a.Status == AcceptorStatus.Pending);

        if (next is null)
        {
            return;
        }

        if (termination.Status == CmContractTerminationStatus.WaitingApproval && next.Status == AcceptorStatus.Pending)
        {
            next.SetCurrent(true);
        }

        var pendingOfType = approvers.Where(a => a.Status == AcceptorStatus.Pending).ToList();
        var isLastPending = pendingOfType.Count == 1;

        if (next.Type == AcceptorType.DepartmentDirectorAgree ||
            (next.Type == AcceptorType.Approver && !isLastPending))
        {
            foreach (var targetUserId in next.GetNotificationTargets())
            {
                _ = SendNotificationAsync(
                    termination,
                    targetUserId,
                    NotificationConstant.WaitForLike.Title,
                    string.Format(NotificationConstant.WaitForLike.Message, ProgramConstant.ContractTermination.Name, termination.CaContractDraftVendor.ContractDraftNumber));
            }
        }
        else if (next.Type == AcceptorType.Approver && isLastPending)
        {
            foreach (var targetUserId in next.GetNotificationTargets())
            {
                _ = SendNotificationAsync(
                    termination,
                    targetUserId,
                    NotificationConstant.WaitForApprove.Title,
                    string.Format(NotificationConstant.WaitForApprove.Message, ProgramConstant.ContractTermination.Name, termination.CaContractDraftVendor.ContractDraftNumber));
            }
        }
    }

    private static new async Task SendNotificationAsync(CmContractTermination termination, UserId userId, string title, string message)
    {
        await Notification
              .Crate(
                  userId,
                  title,
                  message,
                  NotificationProgram.Procurement)
              .SetReferenceId(termination.Id.Value)
              .SetLinkUrl(string.Format(ProgramConstant.ContractTermination.Url, termination.ContractDraftVendorId, termination.Id), "ดูรายละเอียด")
              .PublishAsync(CancellationToken.None);
    }

    private static bool IsGroupAllowedToApprove(CmContractTerminationStatus status, AcceptorType group)
    {
        if (status == CmContractTerminationStatus.WaitingCommitteeApproval && group != AcceptorType.AcceptanceCommittee)
        {
            return false;
        }

        if (status == CmContractTerminationStatus.WaitingApproval && group != AcceptorType.Approver)
        {
            return false;
        }

        return true;
    }

    private static string GetGroupNotAllowedMessage(CmContractTerminationStatus status)
    {
        if (status == CmContractTerminationStatus.WaitingCommitteeApproval)
        {
            return "อนุมัติได้เฉพาะบุคคลคณะกรรมการตรวจรับเท่านั้น";
        }

        if (status == CmContractTerminationStatus.WaitingApproval)
        {
            return "อนุมัติได้เฉพาะผู้มีอำนาจเห็นชอบ/อนุมัติเท่านั้น";
        }

        return "ไม่สามารถอนุมัติในสถานะนี้ได้";
    }

    private static void TryUpdateCommitteeStatus(CmContractTermination cmContractTermination, CmContractTerminationAcceptor current)
    {
        if (current.IsBoardChairman())
        {
            cmContractTermination.SetStatus(CmContractTerminationStatus.WaitingAssign);
        }
    }
}