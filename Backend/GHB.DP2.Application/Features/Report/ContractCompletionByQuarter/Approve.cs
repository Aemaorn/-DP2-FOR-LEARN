namespace GHB.DP2.Application.Features.Report.ContractCompletionByQuarter;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.Report.ContractCompletionByQuarter.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Report.RpContractCompletionByQuarter;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record ApproveRpContractCompletionByQuartersRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid Id,
    string? Remark);

public class ApproveRpContractCompletionByQuartersEndpoint : ContractCompletionByQuarterEndpoint<ApproveRpContractCompletionByQuartersRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public ApproveRpContractCompletionByQuartersEndpoint(ILogger<ApproveRpContractCompletionByQuartersEndpoint> logger, Dp2DbContext dbContext, IFileServiceClient fileServiceClient)
        : base(logger, dbContext, fileServiceClient)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Post("report/contract-completion-by-quarter/{id:guid}/approve");
        this.Description(b => b
                              .WithTags("Report/ContractCompletionByQuarter")
                              .WithName("ApproveRpContractCompletionByQuarters")
                              .WithSummary("อนุมัติรายงานการสรุปผลสัญญาตามไตรมาส")
                              .AllowAnonymous()
                              .Produces(StatusCodes.Status200OK)
                              .Produces<string>(StatusCodes.Status404NotFound)
                              .Produces<string>(StatusCodes.Status400BadRequest));
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(ApproveRpContractCompletionByQuartersRequest req, CancellationToken ct)
    {
        var entity = await this.dbContext.RpContractCompletionByQuarters
                               .Include(rpAuditAndRevenue => rpAuditAndRevenue.Acceptors)
                               .ThenInclude(x => x.User)
                               .ThenInclude(x => x.Employee)
                               .Include(x => x.DocumentHistories)
                               .FirstOrDefaultAsync(x => x.Id == RpContractCompletionByQuarterId.From(req.Id), ct);

        if (entity is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลรายงานรหัส {req.Id}");
        }

        if (entity.Status != RpContractCompletionByQuarterStatus.WaitingApproval)
        {
            return TypedResults.BadRequest("อนุมัติได้เฉพาะสถานะ รอผู้มีอำนาจเห็นชอบ/อนุมัติ เท่านั้น");
        }

        var acceptors = entity.Acceptors
                              .Where(a => a.IsActive && a.Status == AcceptorStatus.Pending)
                              .OrderBy(a => a.Sequence)
                              .Select(DelegatorExtensions.DelegatorToAcceptor)
                              .ToList();

        var current = acceptors.FirstOrDefault(a => a.Delegatee?.SuUserId == null
                                                            ? a.UserId == req.UserId
                                                            : a.Delegatee?.SuUserId == UserId.From(req.UserId));

        if (current == null)
        {
            return TypedResults.BadRequest("ไม่พบผู้อนุมัติในกลุ่มหรือขั้นตอนนี้");
        }

        var currentAcceptorUser = entity.Acceptors.FirstOrDefault(a => a.Id == current.Id);

        if (currentAcceptorUser is null)
        {
            this.ThrowError(
                "ไม่พบผู้อนุมัติที่ใช้งานได้",
                StatusCodes.Status400BadRequest);
        }

        currentAcceptorUser
            .SetDelegatee(current.DelegateeId)
            .Approve(remark: req.Remark);

        entity.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.Update,
                $"เห็นชอบ/อนุมัติ ข้อมูลสัญญาแล้วเสร็จตามไตรมาส",
                entity.Status.ToString(),
                req.Remark));

        var allApproved = entity.Acceptors.All(a => a.Status == AcceptorStatus.Approved);

        if (allApproved)
        {
            entity.SetStatus(RpContractCompletionByQuarterStatus.Approved);
        }

        await this.ManageDocumentForApproveAsync(
            entity,
            UserId.From(req.UserId),
            hasAcceptor: true,
            isReplace: allApproved,
            ct);

        UpdateSequentialCurrents(entity);

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private static void UpdateSequentialCurrents(RpContractCompletionByQuarter entity)
    {
        var approvers = entity.Acceptors
                              .Where(a => a.IsActive)
                              .OrderBy(a => a.Sequence)
                              .ToList();

        foreach (var a in approvers)
        {
            a.SetCurrent(false);
        }

        var next = approvers.Select(DelegatorExtensions.DelegatorToAcceptor)
                            .FirstOrDefault(a => a.Status == AcceptorStatus.Pending);

        if (next is null)
        {
            return;
        }

        next.SetCurrent(true);

        var isLastPending = approvers.Count(a => a.Status == AcceptorStatus.Pending) == 1;

        foreach (var targetUserId in next.GetNotificationTargets())
        {
            if (!isLastPending)
            {
                _ = SendNotificationAsync(
                    entity,
                    targetUserId,
                    NotificationConstant.WaitForLike.Title,
                    string.Format(NotificationConstant.WaitForLike.Message, ProgramConstant.ContractCompletionByQuarter.Name, entity.DocumentNumber));
            }
            else
            {
                _ = SendNotificationAsync(
                    entity,
                    targetUserId,
                    NotificationConstant.WaitForApprove.Title,
                    string.Format(NotificationConstant.WaitForApprove.Message, ProgramConstant.ContractCompletionByQuarter.Name, entity.DocumentNumber));
            }
        }
    }

    private static async Task SendNotificationAsync(RpContractCompletionByQuarter entity, UserId userId, string title, string message)
    {
        await Notification
              .Crate(
                  userId,
                  title,
                  message,
                  NotificationProgram.Report)
              .SetReferenceId(entity.Id.Value)
              .SetLinkUrl(string.Format(ProgramConstant.ContractCompletionByQuarter.Url, entity.Id.Value), "ดูรายละเอียด")
              .PublishAsync(CancellationToken.None);
    }
}