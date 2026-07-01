namespace GHB.DP2.Application.Features.Procurement.Appoint;

using GHB.DP2.Application.Constants;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.Appoint.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.PpAppoint;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record RejectAppointRequest(
    Guid AppointId,
    Guid AcceptorId,
    AcceptorType AcceptorType,
    string? Remark);

public class RejectAppointEndpoint : AppointEndpointBase<RejectAppointRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public RejectAppointEndpoint(
        Dp2DbContext dbContext,
        IOperationService operationService,
        ICommandTextService commandTextService,
        ILogger<RejectAppointEndpoint> logger)
        : base(dbContext, logger, operationService, commandTextService)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("Procurement/Appoint"));
        this.Post("appointments/reject");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(RejectAppointRequest req, CancellationToken ct)
    {
        var appoint = await this.dbContext.PpAppoints
                                .Include(a => a.Acceptors)
                                .Include(auditableEntity => auditableEntity.AuditInfo)
                                .FirstOrDefaultAsync(a => a.Id == PpAppointId.From(req.AppointId), ct);

        if (appoint == null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลขอแต่งตั้ง รหัสนี้ {req.AppointId}.");
        }

        var acceptors =
            appoint.Acceptors
                   .Where(a => a.Type == req.AcceptorType && a.IsActive)
                   .Map(DelegatorExtensions.DelegatorToAcceptor)
                   .OrderBy(a => a.Sequence)
                   .ToList();

        var currentAcceptor = acceptors.FirstOrDefault(a => a.Id == AcceptorId.From(req.AcceptorId));

        if (currentAcceptor == null)
        {
            return TypedResults.NotFound("ไม่พบผู้อนุมัติในกลุ่มหรือขั้นตอนนี้");
        }

        if (!IsPreviousApproved(acceptors, currentAcceptor))
        {
            return TypedResults.BadRequest("ต้องได้รับการอนุมัติจากลำดับก่อนหน้าก่อน");
        }

        if (currentAcceptor.Status == AcceptorStatus.Rejected)
        {
            return TypedResults.BadRequest("รายการนี้ถูกปฏิเสธไปแล้ว");
        }

        var currentAcceptorUser =
            appoint.Acceptors
                   .FirstOrDefault(a => a.Id == currentAcceptor.Id);

        if (currentAcceptorUser == null)
        {
            return TypedResults.NotFound("ไม่พบผู้อนุมัติในกลุ่มหรือขั้นตอนนี้");
        }

        currentAcceptorUser
            .SetDelegatee(currentAcceptor.DelegateeId)
            .Reject(req.Remark);

        // Clear all current flags when rejected
        foreach (var a in appoint.Acceptors.Where(a => a.IsActive && a.IsCurrent))
        {
            a.SetCurrent(false);
        }

        appoint.SetRejected(req.Remark);

        _ = SendNotificationAsync(
            appoint,
            UserId.From(appoint.AuditInfo.CreatedBy),
            NotificationConstant.ReturnToCreator.Title,
            string.Format(NotificationConstant.ReturnToCreator.Message, ProgramConstant.PreProcurementAppointment.Name, appoint.AppointNumber));

        this.dbContext.PpAppoints.Update(appoint);

        var replaceTemplate = appoint.LastedNotReplacedDocument;

        if (replaceTemplate is not null)
        {
            appoint.AddDocumentHistory(replaceTemplate.FileId, false);
        }

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private static async Task SendNotificationAsync(PpAppoint appoint, UserId userId, string title, string message)
    {
        await Notification
              .Crate(
                  userId,
                  title,
                  message,
                  NotificationProgram.Procurement)
              .SetReferenceId(appoint.Id.Value)
              .SetLinkUrl(string.Format(ProgramConstant.PreProcurementAppointment.Url, appoint.Procurement.Id), "ดูรายละเอียด")
              .PublishAsync(CancellationToken.None);
    }
}