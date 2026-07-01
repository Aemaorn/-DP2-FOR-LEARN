namespace GHB.DP2.Application.Features.Procurement.MedianPrice;

using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.MedianPrice.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PpMedianPrice;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class RejectMedianPriceRequest
{
    public Guid ProcurementId { get; init; }

    public Guid MedianPriceId { get; init; }

    [FromClaim(JwtRegisteredClaimNames.Sub)]
    public Guid UserId { get; init; }

    public string? Remark { get; init; }
}

public class RejectMedianPriceEndpoint : MedianPriceEndpointBase<RejectMedianPriceRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public RejectMedianPriceEndpoint(
        ILogger<RejectMedianPriceEndpoint> logger,
        IOperationService operationService,
        ICommandTextService commandTextService,
        Dp2DbContext dbContext)
        : base(logger, dbContext, operationService, commandTextService)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Put("procurement/{ProcurementId:guid}/median-price/{MedianPriceId:guid}/reject");
        this.Options(b =>
            b.WithTags(nameof(MedianPrice))
             .WithName("RejectMedianPrice")
             .Produces<Ok>()
             .ProducesProblem(StatusCodes.Status400BadRequest)
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status500InternalServerError));
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(RejectMedianPriceRequest req, CancellationToken ct)
    {
        // Fetch median price data
        var medianPrice = await this.FetchMedianPriceAsync(req, ct);

        if (medianPrice == null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลราคากลาง");
        }

        // Validate current status
        this.ValidateMedianPriceStatus(medianPrice);

        // Reject the median price
        switch (medianPrice.Status)
        {
            case MedianPriceStatus.WaitingCommitteeApproval:
                this.CommitteeReject(medianPrice, req);
                SendNotificationToCommittee(medianPrice);

                break;

            case MedianPriceStatus.WaitingUnitApproval:
                this.DepartmentDirectorReject(medianPrice, req);
                SendNotificationToCommittee(medianPrice);

                break;

            case MedianPriceStatus.WaitingApproval:
                this.ApproverReject(medianPrice, req);

                break;

            case MedianPriceStatus.WaitingAssign:
            case MedianPriceStatus.RejectToAssignee:
                this.AssigneeRejected(medianPrice, req);
                SendNotificationToCommittee(medianPrice);

                break;

            default:
                this.ThrowError(
                    "ไม่สามารถปฏิเสธราคากลางในสถานะปัจจุบันได้",
                    StatusCodes.Status400BadRequest);

                break;
        }

        this.dbContext.PpMedianPrices.Update(medianPrice);

        if (medianPrice.Status == MedianPriceStatus.WaitingCommitteeApproval || medianPrice.Status == MedianPriceStatus.Rejected || medianPrice.Status == MedianPriceStatus.RejectToAssignee)
        {
            await this.ReplaceDocumentsAsync(medianPrice, false, ct);
        }

        this.dbContext.PpMedianPrices.Update(medianPrice);

        await this.dbContext.SaveChangesAsync(CancellationToken.None);

        return TypedResults.Ok();
    }

    private async Task<PpMedianPrice?> FetchMedianPriceAsync(RejectMedianPriceRequest req, CancellationToken ct)
    {
        return await this.dbContext.PpMedianPrices
                         .Include(m => m.Acceptors)
                         .ThenInclude(a => a.User)
                         .ThenInclude(u => u.Employee)
                         .Include(a => a.Assignees)
                         .SingleOrDefaultAsync(
                             m => m.ProcurementId == ProcurementId.From(req.ProcurementId) &&
                                  m.Id == MedianPriceId.From(req.MedianPriceId),
                             ct);
    }

    private void CommitteeReject(PpMedianPrice medianPrice, RejectMedianPriceRequest req)
    {
        var committeeAcceptors =
            medianPrice.Acceptors
                       .Where(a =>
                           a is
                           {
                               Type: AcceptorType.MedianPriceCommittee,
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

        if (medianPrice.HasMajorityRejection() || acceptor.IsBoardChairman())
        {
            medianPrice.SetRejected(req.Remark, MedianPriceStatus.WaitingCommitteeApproval);
        }
        else
        {
            medianPrice.AddActivity(
                new ActivityInfo(
                    "บุคคล/คณะกรรมการกำหนดราคากลาง ไม่เห็นชอบ",
                    string.Empty,
                    medianPrice.Status.ToString(),
                    req.Remark));
        }
    }

    private void DepartmentDirectorReject(PpMedianPrice medianPrice, RejectMedianPriceRequest req)
    {
        var departmentDirectorAcceptors =
            medianPrice.Acceptors
                       .Where(a =>
                           a.Type == AcceptorType.DepartmentDirectorAgree &&
                           a is { Status: AcceptorStatus.Pending, IsActive: true })
                       .Select(DelegatorExtensions.DelegatorToAcceptor)
                       .ToArray();

        var acceptor =
            departmentDirectorAcceptors
                .FirstOrDefault(a => a.Delegatee?.SuUserId == null
                                                            ? a.UserId == UserId.From(req.UserId)
                                                            : a.Delegatee?.SuUserId == UserId.From(req.UserId));

        if (acceptor is null)
        {
            this.ThrowError(
                "ไม่พบผู้อนุมัติส่วนสายงานที่ใช้งานได้",
                StatusCodes.Status400BadRequest);
        }

        if (!acceptor.ArePreviousAcceptorsApproved(medianPrice.Acceptors))
        {
            this.ThrowError(
                "ต้องได้รับการอนุมัติจากลำดับก่อนหน้าก่อน",
                StatusCodes.Status400BadRequest);
        }

        // Approve the department director acceptor
        var currentApproverUser =
            medianPrice.Acceptors
                .First(a => a.Id == acceptor.Id);

        currentApproverUser
            .SetDelegatee(acceptor.DelegateeId)
            .Reject(req.Remark);

        medianPrice.SetRejected(req.Remark, MedianPriceStatus.WaitingUnitApproval);
    }

    private void ApproverReject(PpMedianPrice medianPrice, RejectMedianPriceRequest req)
    {
        var medianPriceApprover =
            medianPrice.Acceptors
                       .Where(a =>
                           a is
                           {
                               Type: AcceptorType.Approver,
                               Status: AcceptorStatus.Pending,
                               IsActive: true
                           })
                       .ToArray();

        var medianPriceApproverDelegated =
            medianPriceApprover
                .Select(DelegatorExtensions.DelegatorToAcceptor)
                .OrderBy(a => a.Sequence)
                .ToArray();

        var currentApprover =
            medianPriceApproverDelegated
                .FirstOrDefault(a =>
                    a.Delegatee == null
                        ? a.UserId == UserId.From(req.UserId)
                        : a.Delegatee?.SuUserId == UserId.From(req.UserId));

        if (currentApprover is null)
        {
            this.ThrowError(
                "ไม่พบผู้อนุมัติที่ใช้งานได้",
                StatusCodes.Status400BadRequest);
        }

        if (!currentApprover.ArePreviousAcceptorsApproved(medianPrice.Acceptors))
        {
            this.ThrowError(
                "ต้องได้รับการอนุมัติจากลำดับก่อนหน้าก่อน",
                StatusCodes.Status400BadRequest);
        }

        var currentApproverUser =
                medianPrice.Acceptors
                    .First(a => a.Id == currentApprover.Id);

        currentApproverUser
            .SetDelegatee(currentApprover.DelegateeId)
            .Reject(req.Remark);

        if (medianPrice.Procurement.HasMd)
        {
            medianPrice.SetRejectToAssignee(req.Remark);
            _ = SendNotificationAssigneeAsync(medianPrice, CancellationToken.None);
        }
        else
        {
            medianPrice.SetRejected(req.Remark);
            SendNotificationToCommittee(medianPrice);
        }
    }

    private void AssigneeRejected(PpMedianPrice mdp, RejectMedianPriceRequest req)
    {
        var assignee = mdp.Assignees
                          .Select(DelegatorExtensions.DelegatorToAssignee)
                          .FirstOrDefault(w => w.Delegatee == null
                                                ? w.UserId == UserId.From(req.UserId)
                                                : w.Delegatee?.SuUserId == UserId.From(req.UserId));

        if (assignee is null)
        {
            this.ThrowError("ไม่พบเจ้าหน้าที่พัสดุให้ความเห็น", StatusCodes.Status404NotFound);
        }

        var currentUser =
                mdp.Assignees
                    .First(a => a.Id == assignee.Id);

        currentUser
         .SetDelegatee(assignee.DelegateeId)
         .Reject(req.Remark);

        mdp.SetRejected(req.Remark, MedianPriceStatus.WaitingAssign);
    }

    private static void SendNotificationToCommittee(PpMedianPrice mdp)
    {
        var committeeMembers = mdp.Acceptors
                                  .Where(a => a.Type == AcceptorType.MedianPriceCommittee && a.IsActive)
                                  .ToList();

        foreach (var member in committeeMembers)
        {
            _ = SendNotificationToUserAsync(mdp, member.UserId);
        }
    }

    private static async Task SendNotificationToUserAsync(PpMedianPrice mdp, UserId userId)
    {
        await Notification
              .Crate(
                  userId,
                  NotificationConstant.ReturnToCreator.Title,
                  string.Format(NotificationConstant.ReturnToCreator.Message, ProgramConstant.PreProcurementMedianPrice.Name, mdp.ReferenceNumber),
                  NotificationProgram.Procurement)
              .SetReferenceId(mdp.Id.Value)
              .SetLinkUrl(string.Format(ProgramConstant.PreProcurementAppointment.Url, mdp.Procurement.Id), "ดูรายละเอียด")
              .PublishAsync(CancellationToken.None);
    }

    private static async Task SendNotificationAssigneeAsync(PpMedianPrice mdp, CancellationToken ct)
    {
        foreach (var targetUserId in mdp.Assignees.SelectMany(a => a.GetAssigneeNotificationTargets()))
        {
            await Notification
                  .Crate(
                      targetUserId,
                      NotificationConstant.ReturnToCreator.Title,
                      string.Format(NotificationConstant.ReturnToCreator.Message, ProgramConstant.PreProcurementMedianPrice.Name, mdp.ReferenceNumber),
                      NotificationProgram.Procurement)
                  .SetReferenceId(mdp.Id.Value)
                  .SetLinkUrl(
                      string.Format(ProgramConstant.Procurement.Url, mdp.Procurement.Id),
                      "ดูรายละเอียด")
                  .PublishAsync(ct);
        }
    }
}