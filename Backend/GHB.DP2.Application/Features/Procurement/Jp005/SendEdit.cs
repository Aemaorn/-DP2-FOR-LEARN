namespace GHB.DP2.Application.Features.Procurement.Jp005;

using GHB.DP2.Application.Constants;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.PJp005;
using GHB.DP2.Domain.Procurement.PpPurchaseRequisition;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record SendEditToPurchaseRequisitionRequest(
    Guid ProcurementId,
    string? Remark);

public class SendEditToPurchaseRequisitionEndpoint : EndpointBase<SendEditToPurchaseRequisitionRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dp2DbContext;

    public SendEditToPurchaseRequisitionEndpoint(
        Dp2DbContext dp2DbContext,
        ILogger<SendEditToPurchaseRequisitionEndpoint> logger)
        : base(logger)
    {
        this.dp2DbContext = dp2DbContext;
    }

    public override void Configure()
    {
        this.Put("procurement/{ProcurementId:guid}/jp005/send-edit-to-purchase-requisition");
        this.Options(b =>
            b.WithTags("Procurement/JorPor005")
             .WithName("Jp005SendEditToPurchaseRequisition")
             .Produces<Ok>()
             .ProducesProblem(StatusCodes.Status400BadRequest)
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status500InternalServerError));
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(SendEditToPurchaseRequisitionRequest req, CancellationToken ct)
    {
        var purchaseRequisition = await this.dp2DbContext
                                            .PpPurchaseRequisitions
                                            .Include(c => c.Procurement)
                                            .FirstOrDefaultAsync(
                                             x => x.ProcurementId == Domain.Procurement.ProcurementId.From(req.ProcurementId),
                                             ct);

        if (purchaseRequisition == null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลการแจ้งข้อมูลเบื้องต้น (จพ.004)");
        }

        purchaseRequisition.SetSendEditRemark(req.Remark);

        var jp005 = await this.dp2DbContext.PJp005S
                           .FirstOrDefaultAsync(
                            x => x.ProcurementId == Domain.Procurement.ProcurementId.From(req.ProcurementId) && x.IsActive,
                            ct);

        if (jp005 is not null)
        {
            var acceptStatus = new List<PJp005Status>()
            {
                PJp005Status.Draft,
                PJp005Status.Edit,
                PJp005Status.Rejected,
            };

            if (!acceptStatus.Contains(jp005.Status))
            {
                return TypedResults.BadRequest("ไม่สามารถส่งแก้ไขได้ในสถานะนี้");
            }

            jp005.AddActivity(new ActivityInfo(
                ActivityLogActionTypeConstant.SendEdit,
                "ส่งกลับแก้ไข จพ.004",
                jp005.Status.ToString(),
                req.Remark));

            jp005.Deactivate();
            this.dp2DbContext.PJp005S.Update(jp005);
            this.dp2DbContext.PJp005S.Remove(jp005);
        }

        _ = SendNotificationAsync(purchaseRequisition);

        this.dp2DbContext.PpPurchaseRequisitions.Update(purchaseRequisition);
        await this.dp2DbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
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