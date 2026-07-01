namespace GHB.DP2.Application.Features.Procurement.PurchaseOrderApproval;

using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PPurchaseOrderApproval;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record RejectPurchaseOrderApprovalRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid ProcurementId,
    Guid PurchaseOrderApprovalId,
    bool IsAssignee,
    string? Remark
);

public class RejectPurchaseOrderApprovalEndpoint : EndpointBase<RejectPurchaseOrderApprovalRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public RejectPurchaseOrderApprovalEndpoint(Dp2DbContext dbContext, ILogger<RejectPurchaseOrderApprovalEndpoint> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("Procurement/PurchaseOrderApproval")
             .WithName("RejectPurchaseOrderApproval")
             .AllowAnonymous()
             .Accepts<RejectPurchaseOrderApprovalRequest>("application/json"));
        this.Post("procurement/{ProcurementId:guid}/purchase-order-approval/{PurchaseOrderApprovalId:guid}/reject");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(RejectPurchaseOrderApprovalRequest req, CancellationToken ct)
    {
        var entity = await this.dbContext.PPurchaseOrderApprovals
                               .Include(x => x.Acceptors)
                               .Include(x => x.Assignees)
                               .FirstOrDefaultAsync(x => x.Id == PurchaseOrderApprovalId.From(req.PurchaseOrderApprovalId) && x.ProcurementId == ProcurementId.From(req.ProcurementId), ct);

        if (entity == null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูล");
        }

        if (entity.Status is PurchaseOrderApprovalStatus.Draft)
        {
            return TypedResults.BadRequest("ไม่สามารถปฏิเสธเอกสารที่อยู่ในสถานะนี้ได้");
        }

        if (entity.Status is PurchaseOrderApprovalStatus.WaitingAssign)
        {
            var assignee =
                entity.Assignees
                    .Select(DelegatorExtensions.DelegatorToAssignee)
                    .FirstOrDefault(a => a.Delegatee == null
                                           ? a.UserId == req.UserId
                                           : a.Delegatee?.SuUserId == UserId.From(req.UserId));

            if (assignee is null)
            {
                this.ThrowError(
                    "ไม่พบข้อมูลผู้รับผิดชอบในรายการนี้",
                    StatusCodes.Status400BadRequest);
            }

            var currentUser =
                    entity.Assignees
                       .FirstOrDefault(a => a.Id == assignee.Id);

            if (currentUser is null)
            {
                this.ThrowError(
                    "ไม่พบข้อมูลผู้รับผิดชอบในรายการนี้",
                    StatusCodes.Status400BadRequest);
            }

            currentUser
                .SetDelegatee(assignee.DelegateeId)
                .Reject(remark: req.Remark);
        }
        else if (!req.IsAssignee && entity.Status is PurchaseOrderApprovalStatus.WaitingApproval)
        {
            var acceptor =
                entity.Acceptors
                    .Select(DelegatorExtensions.DelegatorToAcceptor)
                    .FirstOrDefault(a => a.Delegatee == null
                                           ? a.UserId == req.UserId
                                           : a.Delegatee?.SuUserId == UserId.From(req.UserId));

            if (acceptor is null)
            {
                this.ThrowError(
                    "ไม่พบข้อมูลผู้อนุมัติในรายการนี้",
                    StatusCodes.Status400BadRequest);
            }

            var currentAcceptorUser =
                    entity.Acceptors
                       .FirstOrDefault(a => a.Id == acceptor.Id);

            if (currentAcceptorUser is null)
            {
                this.ThrowError(
                    "ไม่พบข้อมูลผู้รับผิดชอบในรายการนี้",
                    StatusCodes.Status400BadRequest);
            }

            currentAcceptorUser
                .SetDelegatee(acceptor.DelegateeId)
                .Reject(remark: req.Remark);
        }
        else
        {
            return TypedResults.BadRequest("ไม่สามารถปฏิเสธเอกสารที่อยู่ในสถานะนี้ได้");
        }

        entity.SetRejected(req.Remark);
        _ = SendNotificationAsync(entity);

        this.dbContext.PPurchaseOrderApprovals.Update(entity);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private static async Task SendNotificationAsync(PPurchaseOrderApproval purchaseOrderApproval)
    {
        var programName = purchaseOrderApproval.Procurement.Type == ProcurementType.Rent
            ? ProgramConstant.BranchSpaceRent.Name
            : ProgramConstant.ProcurementPurchaseOrderApproval.Name;

        var notificationProgram = NotificationProgram.Procurement;

        var programUrl = purchaseOrderApproval.Procurement.Type == ProcurementType.Rent
            ? ProgramConstant.BranchSpaceRent.Url
            : ProgramConstant.PreProcurementAppointment.Url;

        await Notification
              .Crate(
                  UserId.From(purchaseOrderApproval.AuditInfo.CreatedBy),
                  NotificationConstant.ReturnToCreator.Title,
                  string.Format(NotificationConstant.ReturnToCreator.Message, programName, purchaseOrderApproval.Procurement.ProcurementNumber),
                  notificationProgram)
              .SetReferenceId(purchaseOrderApproval.Id.Value)
              .SetLinkUrl(string.Format(programUrl, purchaseOrderApproval.Procurement.Id), "ดูรายละเอียด")
              .PublishAsync(CancellationToken.None);
    }
}