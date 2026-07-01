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

public class RejectPurchaseRequisitionCommand
{
    public Guid Id { get; init; }

    public Guid PurchaseRequisitionId { get; init; }

    [FromClaim(JwtRegisteredClaimNames.Sub)]
    public Guid UserId { get; init; }

    public string? Remarks { get; init; }
}

public class RejectPurchaseRequisitionEndpoint : PurchaseRequisitionEndpointBase<RejectPurchaseRequisitionCommand, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public RejectPurchaseRequisitionEndpoint(Dp2DbContext dbContext, ILogger<RejectPurchaseRequisitionEndpoint> logger)
        : base(logger, dbContext)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("Procurement/PurchaseRequisition"));
        this.Put("JorPor04/{PurchaseRequisitionId:guid}/reject");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(RejectPurchaseRequisitionCommand req, CancellationToken ct)
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

        this.RejectApproval(purchaseRequisition, req);
        _ = SendNotificationAsync(purchaseRequisition);

        purchaseRequisition.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Reject,
            "ตีกลับแก้ไข",
            purchaseRequisition.Status.ToString(),
            req.Remarks));

        this.dbContext.PpPurchaseRequisitions.Update(purchaseRequisition);

        await this.UpdateDocumentAsync(purchaseRequisition, false, true, ct);

        await this.dbContext.SaveChangesAsync(CancellationToken.None);

        return TypedResults.Ok();
    }

    private async Task<PpPurchaseRequisition?> FetchPurchaseRequisitionAsync(RejectPurchaseRequisitionCommand req, CancellationToken ct)
    {
        return await this.dbContext.PpPurchaseRequisitions
                         .Include(mp => mp.Acceptors)
                         .Include(mp => mp.Procurement)
                         .SingleOrDefaultAsync(
                             mp =>
                                 mp.Id == PpPurchaseRequisitionId.From(req.PurchaseRequisitionId),
                             ct);
    }

    private void RejectApproval(PpPurchaseRequisition purchaseRequisition, RejectPurchaseRequisitionCommand req)
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
            .Reject(req.Remarks);

        purchaseRequisition.SetStatus(PurchaseRequisitionStatus.Rejected, req.Remarks);
    }

    private static async Task SendNotificationAsync(PpPurchaseRequisition purchaseRequisition)
    {
        await Notification
              .Crate(
                  UserId.From(purchaseRequisition.AuditInfo.CreatedBy),
                  NotificationConstant.ReturnToCreator.Title,
                  string.Format(NotificationConstant.ReturnToCreator.Message, ProgramConstant.PreProcurementJorPor04.Name, purchaseRequisition.PurchaseRequisitionNumber),
                  NotificationProgram.Procurement)
              .SetReferenceId(purchaseRequisition.Id.Value)
              .SetLinkUrl(string.Format(ProgramConstant.PreProcurementAppointment.Url, purchaseRequisition.Procurement.Id), "ดูรายละเอียด")
              .PublishAsync(CancellationToken.None);
    }
}