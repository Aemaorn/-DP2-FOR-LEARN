namespace GHB.DP2.Application.Features.Procurement.TorDraft;

using System;
using System.IdentityModel.Tokens.Jwt;
using System.Threading;
using System.Threading.Tasks;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.TorDraft.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PpTorDraft;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record RejectTorDraftRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid ProcurementId,
    Guid TorDraftId,
    string Group, // "Approver" or "TorDraftCommittee"
    string? Remark
);

public class RejectTorDraftEndpoint : TorDraftEndpointBase<RejectTorDraftRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public RejectTorDraftEndpoint(
        Dp2DbContext dbContext,
        IOperationService operationService,
        ICommandTextService commandTextService,
        ILogger<RejectTorDraftEndpoint> logger)
        : base(logger, dbContext, operationService, commandTextService)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("Procurement/TorDraft")
             .WithName("RejectTorDraft")
             .AllowAnonymous()
             .Accepts<RejectTorDraftRequest>("application/json"));
        this.Post("procurement/{procurementId:guid}/tordraft/{TorDraftId:guid}/reject");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(RejectTorDraftRequest req, CancellationToken ct)
    {
        var torDraft = await this.dbContext.PpTorDrafts
                                 .Include(x => x.PpTorDraftAcceptors)
                                 .ThenInclude(x => x.CommitteePosition)
                                 .SingleOrDefaultAsync(
                                     x =>
                                         x.Id == PpTorDraftId.From(req.TorDraftId) &&
                                         x.ProcurementId == ProcurementId.From(req.ProcurementId),
                                     ct);

        if (torDraft == null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูล");
        }

        var appoint = await this.GetAppointById(ProcurementId.From(req.ProcurementId), ct);

        if (torDraft.Status is
            TorDraftStatus.Draft or
            TorDraftStatus.Rejected or
            TorDraftStatus.Approved)
        {
            return TypedResults.BadRequest("ไม่สามารถปฏิเสธเอกสารที่อยู่ในสถานะนี้ได้");
        }

        switch (torDraft.Status)
        {
            case TorDraftStatus.WaitingCommitteeApproval:
                this.CommitteeReject(torDraft, req);
                SendNotificationToCommittee(torDraft);

                break;

            case TorDraftStatus.WaitingUnitApproval:
                this.DepartmentDirectorReject(torDraft, req);
                SendNotificationToCommittee(torDraft);

                break;

            case TorDraftStatus.WaitingAssign:
            case TorDraftStatus.RejectToAssignee:
                this.AssigneeReject(torDraft, req, torDraft.Status);
                SendNotificationToCommittee(torDraft);

                break;

            case TorDraftStatus.WaitingApproval:
                this.ApproverReject(torDraft, req);

                break;

            default:
                return TypedResults.BadRequest("ไม่สามารถปฏิเสธเอกสารที่อยู่ในสถานะนี้ได้");
        }

        this.dbContext.PpTorDrafts.Update(torDraft);

        if (torDraft.Status == TorDraftStatus.WaitingCommitteeApproval || torDraft.Status == TorDraftStatus.Rejected || torDraft.Status == TorDraftStatus.RejectToAssignee)
        {
            await this.ReplaceDocumentsAsync(torDraft, appoint, ct, isReplace: true);
        }

        this.dbContext.PpTorDrafts.Update(torDraft);

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private void CommitteeReject(PpTorDraft torDraft, RejectTorDraftRequest req)
    {
        var committeeAcceptor =
            torDraft.PpTorDraftAcceptors
                    .Where(a => a is
                    {
                        Type: AcceptorType.TorDraftCommittee,
                        IsActive: true,
                        IsUnableToPerformDuties: false,
                        Status: AcceptorStatus.Pending
                    })
                    .ToArray();

        var acceptor =
            committeeAcceptor.FirstOrDefault(a => a.UserId == UserId.From(req.UserId));

        if (acceptor is null)
        {
            this.ThrowError(
                "ไม่พบผู้อนุมัติคณะกรรมการที่ใช้งานได้",
                StatusCodes.Status400BadRequest);
        }

        if (committeeAcceptor.Any(c => !c.IsBoardChairman()) &&
            acceptor.IsBoardChairman())
        {
            this.ThrowError(
                "ผู้อนุมัติคณะกรรมการไม่ตรงกับตำแหน่งที่กำหนด",
                StatusCodes.Status400BadRequest);
        }

        acceptor.Reject(req.Remark);

        if (torDraft.HasMajorityRejection() || acceptor.IsBoardChairman())
        {
            torDraft.SetRejected(req.Remark, TorDraftStatus.WaitingCommitteeApproval);
        }
    }

    private void DepartmentDirectorReject(PpTorDraft torDraft, RejectTorDraftRequest req)
    {
        var departmentDirectorAcceptor =
            torDraft.PpTorDraftAcceptors
                    .Where(a => a is
                    {
                        Type: AcceptorType.DepartmentDirectorAgree,
                        IsActive: true,
                        Status: AcceptorStatus.Pending
                    })
                    .Map(DelegatorExtensions.DelegatorToAcceptor)
                    .ToArray();

        var acceptor =
            departmentDirectorAcceptor.FirstOrDefault(a => a.Delegatee?.SuUserId == null
                                                                ? a.UserId == UserId.From(req.UserId)
                                                                : a.Delegatee?.SuUserId == UserId.From(req.UserId));

        if (acceptor is null)
        {
            this.ThrowError(
                "ไม่พบผู้อนุมัติผู้อำนวยการฝ่ายที่ใช้งานได้",
                StatusCodes.Status400BadRequest);
        }

        var currentAcceptorUser =
         torDraft.PpTorDraftAcceptors
                 .FirstOrDefault(a => a.Id == acceptor.Id);

        if (currentAcceptorUser is null)
        {
            this.ThrowError(
                "ไม่พบผู้อนุมัติที่ใช้งานได้",
                StatusCodes.Status400BadRequest);
        }

        currentAcceptorUser
            .SetDelegatee(acceptor.DelegateeId)
            .Reject(req.Remark);

        torDraft.SetRejected(req.Remark, TorDraftStatus.WaitingUnitApproval);
    }

    private void AssigneeReject(PpTorDraft torDraft, RejectTorDraftRequest req, TorDraftStatus status)
    {
        var torAssignees = torDraft.Assignees
                                   .Map(DelegatorExtensions.DelegatorToAssignee)
                                   .ToArray();

        var assignee =
            torAssignees.FirstOrDefault(a => a.Delegatee?.SuUserId == null
                                                                ? a.UserId == UserId.From(req.UserId)
                                                                : a.Delegatee?.SuUserId == UserId.From(req.UserId));

        if (assignee is null)
        {
            this.ThrowError(
                "ไม่พบผู้มอบหมาย",
                StatusCodes.Status400BadRequest);
        }

        var currentUser =
                torDraft.Assignees
                        .FirstOrDefault(a => a.Id == assignee.Id);

        if (currentUser is null)
        {
            this.ThrowError(
                "ไม่พบผู้อนุมัติที่ใช้งานได้",
                StatusCodes.Status400BadRequest);
        }

        currentUser
            .SetDelegatee(assignee.DelegateeId)
            .Reject(req.Remark);

        torDraft.SetRejected(req.Remark, status);
    }

    private void ApproverReject(PpTorDraft torDraft, RejectTorDraftRequest req)
    {
        var torDraftAcceptor =
            torDraft.PpTorDraftAcceptors
                    .Where(a => a is
                    {
                        Type: AcceptorType.Approver,
                        IsActive: true,
                        Status: AcceptorStatus.Pending
                    })
                    .Map(DelegatorExtensions.DelegatorToAcceptor)
                    .ToArray();

        var acceptor =
            torDraftAcceptor.FirstOrDefault(a => a.Delegatee?.SuUserId == null
                                                        ? a.UserId == UserId.From(req.UserId)
                                                        : a.Delegatee?.SuUserId == UserId.From(req.UserId));

        if (acceptor is null)
        {
            this.ThrowError(
                "ไม่พบผู้อนุมัติที่ใช้งานได้",
                StatusCodes.Status400BadRequest);
        }

        var currentAcceptorUser =
            torDraft.PpTorDraftAcceptors
                    .FirstOrDefault(a => a.Id == acceptor.Id);

        if (currentAcceptorUser is null)
        {
            this.ThrowError(
                "ไม่พบผู้อนุมัติที่ใช้งานได้",
                StatusCodes.Status400BadRequest);
        }

        currentAcceptorUser
            .SetDelegatee(acceptor.DelegateeId)
            .Reject(req.Remark);

        if (torDraft.Procurement.HasMd)
        {
            torDraft.SetUnitRejected(req.Remark);
            _ = SendNotificationAssigneeAsync(torDraft, CancellationToken.None);
        }
        else
        {
            torDraft.SetRejected(req.Remark);
            SendNotificationToCommittee(torDraft);
        }
    }

    private static void SendNotificationToCommittee(PpTorDraft torDraft)
    {
        var committeeMembers = torDraft.PpTorDraftAcceptors
                                       .Where(a => a.Type == AcceptorType.TorDraftCommittee && a.IsActive)
                                       .ToList();

        foreach (var member in committeeMembers)
        {
            _ = SendNotificationToUserAsync(torDraft, member.UserId);
        }
    }

    private static async Task SendNotificationToUserAsync(PpTorDraft torDraft, UserId userId)
    {
        await Notification
              .Crate(
                  userId,
                  NotificationConstant.ReturnToCreator.Title,
                  string.Format(NotificationConstant.ReturnToCreator.Message, ProgramConstant.PreProcurementTorDraft.Name, torDraft.ReferenceNumber),
                  NotificationProgram.Procurement)
              .SetReferenceId(torDraft.Id.Value)
              .SetLinkUrl(string.Format(ProgramConstant.PreProcurementAppointment.Url, torDraft.Procurement.Id), "ดูรายละเอียด")
              .PublishAsync(CancellationToken.None);
    }

    private static async Task SendNotificationAssigneeAsync(PpTorDraft torDraft, CancellationToken ct)
    {
        foreach (var targetUserId in torDraft.Assignees.SelectMany(a => a.GetAssigneeNotificationTargets()))
        {
            await Notification
                  .Crate(
                      targetUserId,
                      NotificationConstant.ReturnToCreator.Title,
                      string.Format(NotificationConstant.ReturnToCreator.Message, ProgramConstant.PreProcurementTorDraft.Name, torDraft.ReferenceNumber),
                      NotificationProgram.Procurement)
                  .SetReferenceId(torDraft.Id.Value)
                  .SetLinkUrl(
                      string.Format(ProgramConstant.Procurement.Url, torDraft.Procurement.Id),
                      "ดูรายละเอียด")
                  .PublishAsync(ct);
        }
    }
}