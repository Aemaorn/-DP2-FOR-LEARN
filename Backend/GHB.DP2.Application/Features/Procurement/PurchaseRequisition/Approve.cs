namespace GHB.DP2.Application.Features.Procurement.PurchaseRequisition;

using System.IdentityModel.Tokens.Jwt;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.Procurement.PurchaseRequisition.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.PpPurchaseRequisition;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class ApprovePurchaseRequisitionCommand
{
    public Guid Id { get; init; }

    public Guid PurchaseRequisitionId { get; init; }

    [FromClaim(JwtRegisteredClaimNames.Sub)]
    public Guid UserId { get; init; }

    public string? Remarks { get; init; }
}

public class ApprovePurchaseRequisitionEndpoint : PurchaseRequisitionEndpointBase<ApprovePurchaseRequisitionCommand, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public ApprovePurchaseRequisitionEndpoint(
        Dp2DbContext dbContext,
        ILogger<ApprovePurchaseRequisitionEndpoint> logger)
        : base(logger, dbContext)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("Procurement/PurchaseRequisition"));
        this.Put("JorPor04/{PurchaseRequisitionId:guid}/approve");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(ApprovePurchaseRequisitionCommand req, CancellationToken ct)
    {
        var purchaseRequisition = await this.FetchPurchaseRequisitionAsync(req, ct);

        if (purchaseRequisition == null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูล การแจ้งข้อมูลเบื้องต้น");
        }

        if (purchaseRequisition.Status != PurchaseRequisitionStatus.WaitingApproval)
        {
            return TypedResults.BadRequest("ไม่สามารถอนุมัติ การแจ้งข้อมูลเบื้องต้น ในสถานะนี้ได้");
        }

        this.ApproverApproval(purchaseRequisition, req);

        UpdateSequentialCurrents(purchaseRequisition, AcceptorType.Approver);

        purchaseRequisition.EvaluateAcceptorApproval(req.Remarks);

        if (purchaseRequisition.Status == PurchaseRequisitionStatus.WaitingAssign)
        {
            var directorAssignee = purchaseRequisition.Assignees.Select(DelegatorExtensions.DelegatorToAssignee).FirstOrDefault(x => x.Type == AssigneeType.Director);

            if (directorAssignee is not null)
            {
                foreach (var targetUserId in directorAssignee.GetAssigneeNotificationTargets())
                {
                    _ = SendNotificationAsync(purchaseRequisition, targetUserId, NotificationConstant.WaitForAssignment.Title, string.Format(NotificationConstant.WaitForAssignment.Message, ProgramConstant.PreProcurementJorPor04.Name, purchaseRequisition.PurchaseRequisitionNumber));
                }
            }
        }

        purchaseRequisition.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Approved,
            "ผู้มีอำนาจเห็นชอบ/อนุมัติ",
            purchaseRequisition.Status.ToString(),
            req.Remarks));

        await this.UpdateDocumentAsync(purchaseRequisition, true, true, ct);

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private async Task<PpPurchaseRequisition?> FetchPurchaseRequisitionAsync(ApprovePurchaseRequisitionCommand req, CancellationToken ct)
    {
        return await this.dbContext.PpPurchaseRequisitions
                         .Include(mp => mp.Acceptors)
                         .ThenInclude(mp => mp.User)
                         .ThenInclude(mp => mp.Employee)
                         .Include(mp => mp.Procurement)
                         .SingleOrDefaultAsync(
                             mp =>
                                 mp.Id == PpPurchaseRequisitionId.From(req.PurchaseRequisitionId),
                             ct);
    }

    private void ApproverApproval(PpPurchaseRequisition purchaseRequisition, ApprovePurchaseRequisitionCommand req)
    {
        var approverAcceptor =
            purchaseRequisition.Acceptors
                               .Where(a =>
                                   a is
                                   {
                                       Type: AcceptorType.Approver,
                                       Status: AcceptorStatus.Pending,
                                       IsActive: true
                                   })
                               .Select(DelegatorExtensions.DelegatorToAcceptor)
                               .OrderBy(a => a.Sequence)
                               .ToArray();

        var acceptor =
            approverAcceptor
                .FirstOrDefault(a => a.Delegatee == null
                                        ? a.UserId == UserId.From(req.UserId)
                                        : a.Delegatee?.SuUserId == UserId.From(req.UserId));

        if (acceptor is null)
        {
            this.ThrowError(
                "ไม่พบผู้อนุมัติที่ใช้งานได้",
                StatusCodes.Status400BadRequest);
        }

        if (!acceptor.ArePreviousAcceptorsApproved(purchaseRequisition.Acceptors))
        {
            this.ThrowError(
                "ต้องได้รับการอนุมัติจากลำดับก่อนหน้าก่อน",
                StatusCodes.Status400BadRequest);
        }

        var currentApproverUser =
                purchaseRequisition.Acceptors
                    .FirstOrDefault(a => a.Id == acceptor.Id);

        if (currentApproverUser is null)
        {
            this.ThrowError(
                "ไม่พบผู้อนุมัติที่ใช้งานได้",
                StatusCodes.Status400BadRequest);
        }

        currentApproverUser
            .SetDelegatee(acceptor.DelegateeId)
            .Approve(req.Remarks);
    }

    private static void UpdateSequentialCurrents(PpPurchaseRequisition ppPurchaseRequisition, AcceptorType type)
    {
        var approvers = ppPurchaseRequisition.Acceptors
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

        var next = approvers.Select(DelegatorExtensions.DelegatorToAcceptor).FirstOrDefault(a =>
            a.Status == AcceptorStatus.Pending);

        if (next is null)
        {
            return;
        }

        var pendingOfType = approvers.Where(a => a.Status == AcceptorStatus.Pending).ToList();
        var isLastPending = pendingOfType.Count == 1;

        if (ppPurchaseRequisition.Status == PurchaseRequisitionStatus.WaitingApproval && next.Status == AcceptorStatus.Pending)
        {
            next.SetCurrent(true);
        }

        if (next.Type == AcceptorType.Approver && !isLastPending)
        {
            foreach (var targetUserId in next.GetNotificationTargets())
            {
                _ = SendNotificationAsync(ppPurchaseRequisition, targetUserId, NotificationConstant.WaitForLike.Title, string.Format(NotificationConstant.WaitForLike.Message, ProgramConstant.PreProcurementJorPor04.Name, ppPurchaseRequisition.PurchaseRequisitionNumber));
            }
        }
        else if (next.Type == AcceptorType.Approver && isLastPending)
        {
            foreach (var targetUserId in next.GetNotificationTargets())
            {
                _ = SendNotificationAsync(ppPurchaseRequisition, targetUserId, NotificationConstant.WaitForApprove.Title, string.Format(NotificationConstant.WaitForApprove.Message, ProgramConstant.PreProcurementJorPor04.Name, ppPurchaseRequisition.PurchaseRequisitionNumber));
            }
        }
    }

    private static async Task SendNotificationAsync(PpPurchaseRequisition ppPurchaseRequisition, UserId userId, string title, string message)
    {
        await Notification
              .Crate(
                  userId,
                  title,
                  message,
                  NotificationProgram.Procurement)
              .SetReferenceId(ppPurchaseRequisition.Id.Value)
              .SetLinkUrl(string.Format(ProgramConstant.Procurement.Url, ppPurchaseRequisition.Procurement.Id), "ดูรายละเอียด")
              .PublishAsync(CancellationToken.None);
    }
}