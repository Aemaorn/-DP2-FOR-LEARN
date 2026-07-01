namespace GHB.DP2.Application.Features.Procurement.Invite;

using System.IdentityModel.Tokens.Jwt;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Features.Procurement.Invite.Abstract;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PInvite;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record NotInviteRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid ProcurementId,
    Guid InviteId);

public class NotInviteEndpoint : InviteEndpointBase<NotInviteRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public NotInviteEndpoint(
        Dp2DbContext dbContext,
        ILogger<NotInviteEndpoint> logger)
        : base(dbContext, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("Procurement/Invite")
             .WithName("NotInvite")
             .Produces<Ok>()
             .ProducesProblem(StatusCodes.Status400BadRequest)
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status500InternalServerError));
        this.Put("procurement/{ProcurementId:guid}/invite/{InviteId:guid}/not-invite");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(NotInviteRequest req, CancellationToken ct)
    {
        var invite = await this.dbContext.PInvites
                               .Include(p => p.Procurement)
                               .ThenInclude(procurement => procurement.SupplyMethod)
                               .Include(x => x.Acceptors)
                               .ThenInclude(x => x.User)
                               .ThenInclude(x => x.Employee).Include(auditableEntity => auditableEntity.AuditInfo)
                               .SingleOrDefaultAsync(
                                   x => x.Id == PInviteId.From(req.InviteId) && x.ProcurementId == ProcurementId.From(req.ProcurementId),
                                   ct);

        if (invite == null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูล");
        }

        invite.SetNotInvited();

        _ = SendNotificationAsync(invite, UserId.From(invite.AuditInfo.CreatedBy));

        this.dbContext.PInvites.Update(invite);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private static async Task SendNotificationAsync(PInvite invite, UserId userId)
    {
        await Notification
              .Crate(
                  userId,
                  "ดำเนินการต่อ",
                  string.Format(
                      "ไม่ได้เชิญผู้ประกอบการสำหรับ {0} เลขที่ {1} กรุณาดำเนินการขั้นตอนถัดไป",
                      ProgramConstant.PreProcurementInvite.Name,
                      invite.Procurement.ProcurementNumber),
                  NotificationProgram.Procurement)
              .SetReferenceId(invite.Id.Value)
              .SetLinkUrl(string.Format(ProgramConstant.Procurement.Url, invite.Procurement.Id), "ดูรายละเอียด")
              .PublishAsync(CancellationToken.None);
    }
}