namespace GHB.DP2.Application.Features.Procurement.Jp006;

using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.Jp006.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PPurchaseOrder;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class RejectJp006Request
{
    public Guid ProcurementId { get; init; }

    public Guid Jp006Id { get; init; }

    [FromClaim(JwtRegisteredClaimNames.Sub)]
    public Guid UserId { get; init; }

    public string? Remark { get; init; }
}

public class RejectJp006Endpoint : Jp006EndpointBase<RejectJp006Request, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public RejectJp006Endpoint(
        ILogger<AssigneePurchaseOrderEndpoint> logger,
        IOperationService operationService,
        ICommandTextService commandTextService,
        IFileServiceClient fileServiceClient,
        Dp2DbContext dbContext)
        : base(logger, operationService, commandTextService, fileServiceClient, dbContext)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Put("procurement/{ProcurementId:guid}/jp006/{Jp006Id:guid}/reject");
        this.Options(b =>
            b.WithTags(nameof(Jp006))
             .WithName("RejectJp006")
             .Produces<Ok>()
             .ProducesProblem(StatusCodes.Status400BadRequest)
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status500InternalServerError));
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(RejectJp006Request req, CancellationToken ct)
    {
        var jp006 = await this.dbContext.PJp006S
                              .Include(p => p.Acceptors)
                              .Include(p => p.Procurement)
                              .Include(pPurchaseOrder => pPurchaseOrder.Assignees)
                              .Include(p => p.DocumentHistories)
                              .SingleOrDefaultAsync(
                                  p =>
                                      p.Id == PurchaseOrderId.From(req.Jp006Id) &&
                                      p.ProcurementId == ProcurementId.From(req.ProcurementId),
                                  ct);

        if (jp006 is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลการจัดซื้อจัดจ้าง");
        }

        this.ValidateJp006Status(jp006);

        var jp004 = await this.dbContext.PpPurchaseRequisitions
                              .Include(p => p.Assignees)
                              .FirstOrDefaultAsync(w => w.ProcurementId == ProcurementId.From(req.ProcurementId), ct);

        UserId[] assigneeUserIds = jp004?.LastedAssignee is { } lastedAssignee ? [lastedAssignee.UserId] : [];

        switch (jp006.Status, jp006.Assignees.Any())
        {
            case (PurchaseOrderStatus.WaitingCommitteeApproval, _):
                await this.CommitteeReject(jp006, req);
                _ = SendNotificationCommitteeAsync(jp006);

                break;

            case (PurchaseOrderStatus.WaitingAssign, _):
                await this.AssigneeReject(jp006, req);
                _ = SendNotificationAsync(jp006, assigneeUserIds);

                break;

            case (PurchaseOrderStatus.RejectToAssignee, _):
                await this.AssigneeReject(jp006, req);
                _ = SendNotificationAsync(jp006, assigneeUserIds);

                break;

            case (PurchaseOrderStatus.WaitingApproval, true):
                await this.ApproverRejectToAssignee(jp006, req);
                _ = SendNotificationAssigneeAsync(jp006, ct);

                break;

            case (PurchaseOrderStatus.WaitingApproval, false):
                await this.ApproverReject(jp006, req);
                _ = SendNotificationAsync(jp006, assigneeUserIds);

                break;

            default:
                this.ThrowError(
                    "กลุ่มการอนุมัติไม่รองรับ",
                    StatusCodes.Status400BadRequest);

                break;
        }

        jp006.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Reject,
            $"ตีกลับแก้ไข",
            jp006.Status.ToString(),
            req.Remark));

        // Save changes
        this.dbContext.PJp006S.Update(jp006);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private async Task CommitteeReject(PPurchaseOrder jp006, RejectJp006Request req)
    {
        var committeeAcceptors =
            jp006.Acceptors
                 .Where(a =>
                     a is
                     {
                         Type: AcceptorType.ProcurementCommittee,
                         Status: AcceptorStatus.Pending,
                         IsUnableToPerformDuties: false,
                         IsActive: true
                     })
                 .ToArray();

        var acceptor =
            committeeAcceptors
                .FirstOrDefault(a => a.UserId == UserId.From(req.UserId));

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

        await this.IsRejectedJp06(jp006);
    }

    private async Task IsRejectedJp06(PPurchaseOrder jp006)
    {
        var committeeAcceptors =
            jp006.Acceptors
                 .Where(a =>
                     a is
                     {
                         Type: AcceptorType.ProcurementCommittee,
                         IsActive: true
                     })
                 .ToArray();

        if (committeeAcceptors.Length == 0)
        {
            return;
        }

        var rejectedCount = committeeAcceptors.Count(a => a.Status == AcceptorStatus.Rejected);
        var approvedCount = committeeAcceptors.Count(a => a.Status == AcceptorStatus.Approved);
        var totalCount = committeeAcceptors.Length;

        var haft = totalCount / 2;

        // ถ้า rejected เกินครึ่งหนึ่ง
        if (rejectedCount > haft)
        {
            jp006.SetRejected();
            await this.StampCommitteeAndAssigneeRecallOrReject(jp006.Procurement, jp006, CancellationToken.None);

            return;
        }

        var chairmanAcceptor = committeeAcceptors.FirstOrDefault(a => a.IsBoardChairman());

        // ถ้าจำนวน rejected และ approved เท่ากัน ให้ดูที่ chairman
        if (rejectedCount == approvedCount && chairmanAcceptor?.Status == AcceptorStatus.Rejected)
        {
            jp006.SetRejected();
            await this.StampCommitteeAndAssigneeRecallOrReject(jp006.Procurement, jp006, CancellationToken.None);

            return;
        }

        if (rejectedCount == haft && chairmanAcceptor?.Status == AcceptorStatus.Rejected)
        {
            jp006.SetRejected();
            await this.StampCommitteeAndAssigneeRecallOrReject(jp006.Procurement, jp006, CancellationToken.None);
        }
    }

    private async Task ApproverReject(PPurchaseOrder jp006, RejectJp006Request req)
    {
        var approvers =
            jp006.Acceptors
                 .Where(a =>
                     a.Type == AcceptorType.Approver &&
                     a is { Status: AcceptorStatus.Pending, IsActive: true })
                 .Map(DelegatorExtensions.DelegatorToAcceptor)
                 .ToArray();

        var acceptor =
            approvers
                .FirstOrDefault(a => a.Delegatee == null
                    ? a.UserId == req.UserId
                    : a.Delegatee?.SuUserId == UserId.From(req.UserId));

        if (acceptor is null)
        {
            this.ThrowError(
                "ไม่พบผู้อนุมัติที่ใช้งานได้",
                StatusCodes.Status400BadRequest);
        }

        var currentAcceptorUser =
            jp006.Acceptors
                 .FirstOrDefault(a => a.Id == acceptor.Id);

        if (currentAcceptorUser is null)
        {
            this.ThrowError(
                "ไม่พบผู้อนุมัติที่ใช้งานได้",
                StatusCodes.Status400BadRequest);
        }

        if (!currentAcceptorUser.ArePreviousAcceptorsApproved(jp006.Acceptors))
        {
            this.ThrowError(
                "ต้องได้รับการอนุมัติจากลำดับก่อนหน้าก่อน",
                StatusCodes.Status400BadRequest);
        }

        // Reject the approver acceptor
        currentAcceptorUser
            .SetDelegatee(acceptor.DelegateeId)
            .Reject(remark: req.Remark);

        jp006.SetRejected();
        await this.StampCommitteeAndAssigneeRecallOrReject(jp006.Procurement, jp006, CancellationToken.None);
    }

    private async Task ApproverRejectToAssignee(PPurchaseOrder jp006, RejectJp006Request req)
    {
        var approvers =
            jp006.Acceptors
                 .Where(a =>
                     a.Type == AcceptorType.Approver &&
                     a is { Status: AcceptorStatus.Pending, IsActive: true })
                 .Map(DelegatorExtensions.DelegatorToAcceptor)
                 .ToArray();

        var acceptor =
            approvers
                .FirstOrDefault(a => a.Delegatee == null
                    ? a.UserId == req.UserId
                    : a.Delegatee?.SuUserId == UserId.From(req.UserId));

        if (acceptor is null)
        {
            this.ThrowError(
                "ไม่พบผู้อนุมัติที่ใช้งานได้",
                StatusCodes.Status400BadRequest);
        }

        var currentAcceptorUser =
            jp006.Acceptors
                 .FirstOrDefault(a => a.Id == acceptor.Id);

        if (currentAcceptorUser is null)
        {
            this.ThrowError(
                "ไม่พบผู้อนุมัติที่ใช้งานได้",
                StatusCodes.Status400BadRequest);
        }

        if (!acceptor.ArePreviousAcceptorsApproved(jp006.Acceptors))
        {
            this.ThrowError(
                "ต้องได้รับการอนุมัติจากลำดับก่อนหน้าก่อน",
                StatusCodes.Status400BadRequest);
        }

        // Reject the approver acceptor
        currentAcceptorUser
            .SetDelegatee(acceptor.DelegateeId)
            .Reject(remark: req.Remark);

        jp006.SetRejectToAssignee();
        await this.RestoreJorPorCommentDocument(jp006.Procurement, jp006, PurchaseOrderDocumentType.Jp006, CancellationToken.None);
    }

    private async Task AssigneeReject(PPurchaseOrder jp006, RejectJp006Request req)
    {
        var approvers =
            jp006.Assignees
                 .Map(DelegatorExtensions.DelegatorToAssignee)
                 .ToArray();

        var acceptor =
            approvers
                .FirstOrDefault(a => a.Delegatee == null
                    ? a.UserId == req.UserId
                    : a.Delegatee?.SuUserId == UserId.From(req.UserId));

        if (acceptor is null)
        {
            this.ThrowError(
                "ไม่พบผู้อนุมัติที่ใช้งานได้",
                StatusCodes.Status400BadRequest);
        }

        var currentUser =
            jp006.Assignees
                 .FirstOrDefault(a => a.Id == acceptor.Id);

        if (currentUser is null)
        {
            this.ThrowError(
                "ไม่พบผู้อนุมัติที่ใช้งานได้",
                StatusCodes.Status400BadRequest);
        }

        // Reject the approver acceptor
        currentUser
            .SetDelegatee(acceptor.DelegateeId)
            .Reject(remark: req.Remark);

        jp006.SetRejected();
        await this.RestoreJorPorCommentDocument(jp006.Procurement, jp006, PurchaseOrderDocumentType.Jp006, CancellationToken.None);
    }

    private static async Task SendNotificationCommitteeAsync(PPurchaseOrder jp06)
    {
        var targets = jp06.Acceptors
                          .Where(a => a is
                          {
                              Type: AcceptorType.ProcurementCommittee,
                              IsActive: true,
                              IsUnableToPerformDuties: false
                          })
                          .SelectMany(a => a.GetNotificationTargets())
                          .Distinct();

        foreach (var userId in targets)
        {
            await Notification
                  .Crate(
                      userId,
                      NotificationConstant.ReturnToCreator.Title,
                      string.Format(NotificationConstant.ReturnToCreator.Message, ProgramConstant.PreProcurementJorPor06.Name, jp06.PurchaseOrderNumber),
                      NotificationProgram.Procurement)
                  .SetReferenceId(jp06.Id.Value)
                  .SetLinkUrl(string.Format(ProgramConstant.PreProcurementAppointment.Url, jp06.Procurement.Id), "ดูรายละเอียด")
                  .PublishAsync(CancellationToken.None);
        }
    }

    private static async Task SendNotificationAsync(PPurchaseOrder jp06, IEnumerable<UserId> assigneeUserIds)
    {
        var recipients = new[] { UserId.From(jp06.AuditInfo.CreatedBy) }
            .Concat(assigneeUserIds)
            .Distinct();

        foreach (var userId in recipients)
        {
            await Notification
                  .Crate(
                      userId,
                      NotificationConstant.ReturnToCreator.Title,
                      string.Format(NotificationConstant.ReturnToCreator.Message, ProgramConstant.PreProcurementJorPor06.Name, jp06.PurchaseOrderNumber),
                      NotificationProgram.Procurement)
                  .SetReferenceId(jp06.Id.Value)
                  .SetLinkUrl(string.Format(ProgramConstant.PreProcurementAppointment.Url, jp06.Procurement.Id), "ดูรายละเอียด")
                  .PublishAsync(CancellationToken.None);
        }
    }

    private static async Task SendNotificationAssigneeAsync(PPurchaseOrder jp06, CancellationToken ct)
    {
        foreach (var targetUserId in jp06.Assignees.SelectMany(a => a.GetAssigneeNotificationTargets()))
        {
            await Notification
                  .Crate(
                      targetUserId,
                      NotificationConstant.ReturnToCreator.Title,
                      string.Format(NotificationConstant.ReturnToCreator.Message, ProgramConstant.PreProcurementJorPor06.Name, jp06.PurchaseOrderNumber),
                      NotificationProgram.Procurement)
                  .SetReferenceId(jp06.Id.Value)
                  .SetLinkUrl(
                      string.Format(ProgramConstant.Procurement.Url, jp06.Procurement.Id),
                      "ดูรายละเอียด")
                  .PublishAsync(ct);
        }
    }
}