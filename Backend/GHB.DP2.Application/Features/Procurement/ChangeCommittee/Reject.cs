namespace GHB.DP2.Application.Features.Procurement.ChangeCommittee;

using GHB.DP2.Application.Constants;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.ChangeCommittee;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record RejectChangeCommitteeRequest(
    Guid ChangeCommitteeId,
    Guid AcceptorId,
    string? Remark);

public class RejectChangeCommitteeEndpoint : EndpointBase<RejectChangeCommitteeRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public RejectChangeCommitteeEndpoint(
        Dp2DbContext dbContext,
        ILogger<RejectChangeCommitteeEndpoint> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("ChangeCommittee"));
        this.Post("change-committee/{changeCommitteeId:guid}/reject");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(RejectChangeCommitteeRequest req, CancellationToken ct)
    {
        var changeCommitteeId = CommitteeChangeId.From(req.ChangeCommitteeId);

        var changeCommittee = await this.dbContext.CommitteeChanges
                                .Include(c => c.Acceptors)
                                    .ThenInclude(a => a.User)
                                .Include(c => c.Assignees)
                                .Include(c => c.DocumentHistories)
                                .FirstOrDefaultAsync(x => x.Id == changeCommitteeId, ct);

        if (changeCommittee == null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลการเปลี่ยนแปลงคณะกรรมการ");
        }

        var replaceTemplateWaitingCommitteeApproval = changeCommittee.LastedNotReplacedCommitteeDocument;

        switch (changeCommittee.Status)
        {
            case CommitteeChangeStatus.WaitingCommitteeApproval:
                {
                    var result = this.CommitteeReject(changeCommittee, req, replaceTemplateWaitingCommitteeApproval);
                    if (result is not null)
                    {
                        return result;
                    }

                    break;
                }

            case CommitteeChangeStatus.RejectToAssignee:
            case CommitteeChangeStatus.WaitingAssign:
                {
                    var result = this.AssigneeReject(changeCommittee, req);
                    if (result is not null)
                    {
                        return result;
                    }

                    changeCommittee.SetRejected(req.Remark);
                    await SendNotificationAsync(changeCommittee);

                    if (replaceTemplateWaitingCommitteeApproval is not null)
                    {
                        changeCommittee.AddDocumentHistory(replaceTemplateWaitingCommitteeApproval.FileId, false);
                    }

                    break;
                }

            case CommitteeChangeStatus.WaitingApproval:
                {
                    var result = this.AcceptorReject(changeCommittee, req);
                    if (result is not null)
                    {
                        return result;
                    }

                    if (changeCommittee.IsJorPorComment)
                    {
                        changeCommittee.SetRejectToAssignee(req.Remark);
                        await SendNotificationAssigneeAsync(changeCommittee);
                    }
                    else
                    {
                        changeCommittee.SetRejected(req.Remark);
                        await SendNotificationAsync(changeCommittee);
                    }

                    if (replaceTemplateWaitingCommitteeApproval is not null)
                    {
                        changeCommittee.AddDocumentHistory(replaceTemplateWaitingCommitteeApproval.FileId, false);
                    }

                    break;
                }

            default:
                return TypedResults.BadRequest("ไม่สามารถปฏิเสธเอกสารที่อยู่ในสถานะนี้ได้");
        }

        this.dbContext.CommitteeChanges.Update(changeCommittee);

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private BadRequest<string>? CommitteeReject(CommitteeChanges changeCommittee, RejectChangeCommitteeRequest req, CommitteeChangeDocumentHistory? replaceTemplateWaitingCommitteeApproval)
    {
        var committeeAcceptors =
            changeCommittee.Acceptors
                       .Where(a =>
                           a is
                           {
                               Status: AcceptorStatus.Pending,
                               IsUnableToPerformDuties: false,
                               IsActive: true
                           })
                       .ToArray();

        var acceptor =
            committeeAcceptors
                .FirstOrDefault(a => a.UserId == UserId.From(req.AcceptorId));

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

        acceptor.Reject(req.Remark);

        if (changeCommittee.HasMajorityRejection() || acceptor.IsBoardChairman())
        {
            changeCommittee.SetRejected(req.Remark);

            _ = SendNotificationAsync(changeCommittee);

            if (replaceTemplateWaitingCommitteeApproval is not null)
            {
                changeCommittee.AddDocumentHistory(replaceTemplateWaitingCommitteeApproval.FileId, false);
            }
        }
        else
        {
            changeCommittee.AddActivity(
                new ActivityInfo(
                    "บุคคล/คณะกรรมการ ไม่เห็นชอบ",
                    string.Empty,
                    changeCommittee.Status.ToString(),
                    req.Remark));
        }

        return null;
    }

    private BadRequest<string>? AcceptorReject(CommitteeChanges changeCommittee, RejectChangeCommitteeRequest req)
    {
        var currentAcceptor = changeCommittee.Acceptors
                                             .Select(DelegatorExtensions.DelegatorToAcceptor)
                                             .OrderBy(x => x.Sequence)
                                             .FirstOrDefault(a => a.Status == AcceptorStatus.Pending && (a.Delegatee?.SuUserId == null
                                                                ? a.UserId == req.AcceptorId
                                                                : a.Delegatee?.SuUserId == UserId.From(req.AcceptorId)));

        if (currentAcceptor == null)
        {
            return TypedResults.BadRequest("ไม่พบข้อมูลผู้อนุมัติ");
        }

        var currentAcceptorUser =
                changeCommittee.Acceptors
                               .FirstOrDefault(a => a.Id == currentAcceptor.Id);

        if (currentAcceptorUser == null)
        {
            return TypedResults.BadRequest("ไม่พบข้อมูลผู้อนุมัติ");
        }

        currentAcceptorUser.Reject(req.Remark);

        foreach (var acceptor in changeCommittee.Acceptors.Where(a => a.IsActive && a.IsCurrent))
        {
            acceptor.SetCurrent(false);
        }

        return null;
    }

    private BadRequest<string>? AssigneeReject(CommitteeChanges changeCommittee, RejectChangeCommitteeRequest req)
    {
        var currentAcceptor = changeCommittee.Assignees
                                             .Select(DelegatorExtensions.DelegatorToAssignee)
                                             .OrderBy(x => x.Sequence)
                                             .FirstOrDefault(a => a.Delegatee?.SuUserId == null
                                                                ? a.UserId == req.AcceptorId
                                                                : a.Delegatee?.SuUserId == UserId.From(req.AcceptorId));

        if (currentAcceptor == null)
        {
            return TypedResults.BadRequest("ไม่พบข้อมูลผู้อนุมัติ");
        }

        var currentAcceptorUser =
                changeCommittee.Assignees
                               .FirstOrDefault(a => a.Id == currentAcceptor.Id);

        if (currentAcceptorUser == null)
        {
            return TypedResults.BadRequest("ไม่พบข้อมูลผู้อนุมัติ");
        }

        currentAcceptorUser.Reject(req.Remark);

        return null;
    }

    private static async Task SendNotificationAsync(CommitteeChanges changeCommittee)
    {
        await Notification
              .Crate(
                  UserId.From(changeCommittee.AuditInfo.CreatedBy),
                  NotificationConstant.ReturnToCreator.Title,
                  string.Format(NotificationConstant.ReturnToCreator.Message, ProgramConstant.CommitteeChange.Name, string.Empty),
                  NotificationProgram.Procurement)
              .SetReferenceId(changeCommittee.Id.Value)
              .SetLinkUrl(string.Format(ProgramConstant.CommitteeChange.Url, changeCommittee.Id.Value), ProgramConstant.CommitteeChange.Button)
              .PublishAsync(CancellationToken.None);
    }

    private static async Task SendNotificationAssigneeAsync(CommitteeChanges changeCommittee)
    {
        foreach (var targetUserId in changeCommittee.Assignees.SelectMany(a => a.GetAssigneeNotificationTargets()))
        {
            await Notification
                  .Crate(
                      targetUserId,
                      NotificationConstant.ReturnToCreator.Title,
                      string.Format(NotificationConstant.ReturnToCreator.Message, ProgramConstant.CommitteeChange.Name, string.Empty),
                      NotificationProgram.Procurement)
                  .SetReferenceId(changeCommittee.Id.Value)
                  .SetLinkUrl(string.Format(ProgramConstant.CommitteeChange.Url, changeCommittee.Id.Value), ProgramConstant.CommitteeChange.Button)
                  .PublishAsync(CancellationToken.None);
        }
    }
}