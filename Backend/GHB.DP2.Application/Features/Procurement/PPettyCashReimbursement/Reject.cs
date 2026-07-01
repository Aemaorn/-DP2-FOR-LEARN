namespace GHB.DP2.Application.Features.Procurement.PPettyCashReimbursement;

using System.IdentityModel.Tokens.Jwt;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.PPettyCashReimbursement;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using GHB.DP2.Application.EventHandlers.SuNotifications;

public record RejectPettyCashReimbursementRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)] Guid UserId,
    Guid Id,
    string? Remark);

public class RejectPettyCashReimbursementEndpoint : EndpointBase<RejectPettyCashReimbursementRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public RejectPettyCashReimbursementEndpoint(ILogger<RejectPettyCashReimbursementEndpoint> logger, Dp2DbContext dbContext)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Post("petty-cash-reimbursement/{id:guid}/reject");
        this.Description(b => b
            .WithTags("Procurement/PPettyCashReimbursement")
            .WithName("RejectPettyCashReimbursement")
            .Produces(StatusCodes.Status200OK)
            .Produces<string>(StatusCodes.Status404NotFound)
            .Produces<string>(StatusCodes.Status400BadRequest));
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(RejectPettyCashReimbursementRequest req, CancellationToken ct)
    {
        var entity = await this.dbContext.PPettyCashReimbursements
            .Include(e => e.Acceptors)
            .FirstOrDefaultAsync(e => e.Id == PPettyCashReimbursementId.From(req.Id), ct);

        if (entity is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลเบิกเงินชดเชยเงินสดย่อย {req.Id}");
        }

        var acceptors = entity.Acceptors
            ?.Where(a => a.IsActive)
            .OrderBy(a => a.Sequence)
            .ToList() ?? new List<PPettyCashReimbursementAcceptor>();

        var current = acceptors.Select(DelegatorExtensions.DelegatorToAcceptor)
                               .FirstOrDefault(a => a.Delegatee?.SuUserId == null
                                                        ? a.UserId == req.UserId
                                                        : a.Delegatee?.SuUserId == UserId.From(req.UserId));
        if (current == null)
        {
            return TypedResults.BadRequest("ไม่พบผู้อนุมัติในกลุ่มหรือขั้นตอนนี้");
        }

        if (current.Status is AcceptorStatus.Rejected)
        {
            return TypedResults.BadRequest("รายการนี้ถูกปฏิเสธแล้ว");
        }

        var currentAcceptorUser = entity.Acceptors?.FirstOrDefault(a => a.Id == current.Id);

        if (currentAcceptorUser == null)
        {
            this.ThrowError(
                    "ไม่พบผู้อนุมัติที่ใช้งานได้",
                    StatusCodes.Status400BadRequest);
        }

        currentAcceptorUser
            .SetDelegatee(current.DelegateeId)
            .Reject(remark: req.Remark);

        entity.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Reject,
            string.Empty,
            entity.Status.ToString(),
            req.Remark));

        entity.SetRejected(req.Remark);

        _ = SendNotificationAsync(entity);

        this.dbContext.PPettyCashReimbursements.Update(entity);
        await this.dbContext.SaveChangesAsync(ct);
        return TypedResults.Ok();
    }

    private static async Task SendNotificationAsync(PPettyCashReimbursement entity)
    {
        await Notification
        .Crate(
               UserId.From(entity.AuditInfo.CreatedBy),
               NotificationConstant.ReturnToCreator.Title,
               string.Format(NotificationConstant.ReturnToCreator.Message, ProgramConstant.PettyCashReimbursement.Name, entity.Number),
               NotificationProgram.Procurement)
              .SetReferenceId(entity.Id.Value)
              .SetLinkUrl(string.Format(ProgramConstant.PettyCashReimbursement.Url, entity.Id), "ดูรายละเอียด")
              .PublishAsync(CancellationToken.None);
    }
}