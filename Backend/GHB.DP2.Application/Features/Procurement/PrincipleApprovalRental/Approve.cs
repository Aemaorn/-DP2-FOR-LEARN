namespace GHB.DP2.Application.Features.Procurement.PrincipleApprovalRental;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.Procurement.PrincipleApprovalRental.Abstact;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PPrincipleApprovalRental;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record ApprovePrincipleApprovalRentalRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid ProcurementId,
    Guid PrincipleApprovalRentalId,
    string? Remark);

public class ApprovePrincipleApprovalRentalEndpoint : PrincipleApprovalRentalEndpointBase<ApprovePrincipleApprovalRentalRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public ApprovePrincipleApprovalRentalEndpoint(
        ILogger<ApprovePrincipleApprovalRentalEndpoint> logger,
        Dp2DbContext dbContext,
        IFileServiceClient fileServiceClient)
        : base(logger, dbContext, fileServiceClient)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("Procurement/PrincipleApprovalRental")
             .WithName("ApprovePrincipleApprovalRental")
             .Accepts<ApprovePrincipleApprovalRentalEndpoint>("application/json"));
        this.Post("procurement/{procurementId:guid}/principle-approval-rental/{principleApprovalRentalId:guid}/approve");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(ApprovePrincipleApprovalRentalRequest req, CancellationToken ct)
    {
        var entity = await this.dbContext.PPrincipleApprovalRentals
                               .Include(x => x.Acceptors)
                               .Include(a => a.Assignees)
                               .Include(pp => pp.Procurement)
                               .ThenInclude(p => p.PrincipleApprovals)
                               .FirstOrDefaultAsync(x => x.Id == PPrincipleApprovalRentalId.From(req.PrincipleApprovalRentalId) && x.ProcurementId == ProcurementId.From(req.ProcurementId), ct);

        if (entity == null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลการอนุมัติหลักการเช่า");
        }

        var type = entity.Status == PPrincipleApprovalRentalStatus.WaitingCommitteeApproval
            ? AcceptorType.RentCommittee
            : AcceptorType.Approver;

        var acceptors = entity.Acceptors
                              .Where(a => a.Type == type && a.IsActive)
                              .OrderBy(a => a.Sequence)
                              .ToList();

        var current = acceptors.FirstOrDefault(a => a.UserId == UserId.From(req.UserId)
                                                    && a.Status == AcceptorStatus.Pending);

        if (type != AcceptorType.RentCommittee)
        {
            current = acceptors.Select(DelegatorExtensions.DelegatorToAcceptor)
                               .FirstOrDefault(a => (a.Delegatee?.SuUserId == null
                                    ? a.UserId == req.UserId
                                    : a.Delegatee?.SuUserId == UserId.From(req.UserId))
                                      && a.Status == AcceptorStatus.Pending);
        }

        if (current == null)
        {
            return TypedResults.BadRequest("ไม่พบผู้อนุมัติในกลุ่มหรือขั้นตอนนี้");
        }

        if (!IsGroupAllowedToApprove(entity.Status, current.Type))
        {
            return TypedResults.BadRequest(GetGroupNotAllowedMessage(entity.Status));
        }

        var currentAcceptorUser =
                entity.Acceptors
                .FirstOrDefault(a => a.Id == current.Id);

        if (currentAcceptorUser == null)
        {
            this.ThrowError(
                    "ไม่พบผู้อนุมัติที่ใช้งานได้",
                    StatusCodes.Status400BadRequest);
        }

        currentAcceptorUser
            .SetDelegatee(current.DelegateeId)
            .Approve(remark: req.Remark);

        UpdateSequentialCurrents(entity, current.Type);

        switch (entity.Status)
        {
            case PPrincipleApprovalRentalStatus.WaitingCommitteeApproval:
                TryUpdateCommitteeStatus(entity);

                entity.AddActivity(
                    new ActivityInfo(
                        ActivityLogActionTypeConstant.CommitteeApproved,
                        "คณะกรรมการเห็นชอบ/อนุมัติ",
                        entity.Status.ToString(),
                        req.Remark));

                break;

            case PPrincipleApprovalRentalStatus.WaitingAcceptance:
                ShouldUpdateApprover(entity);

                entity.AddActivity(
                    new ActivityInfo(
                        ActivityLogActionTypeConstant.Approved,
                        ActivityLogActionTypeConstant.Approved,
                        entity.Status.ToString(),
                        req.Remark));

                break;

            default:
                {
                    if (!IsPreviousApproved(acceptors, current))
                    {
                        return TypedResults.BadRequest("ต้องได้รับการอนุมัติจากลำดับก่อนหน้าก่อน");
                    }

                    if (ShouldUpdateStatus(current.Type, acceptors))
                    {
                        entity.SetStatusApproved();
                    }

                    break;
                }
        }

        this.dbContext.PPrincipleApprovalRentals.Update(entity);

        await this.ReplaceDocumentsAsync(
            entity,
            entity.Procurement.PrincipleApprovals.FirstOrDefault()!,
            ct,
            true);

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private static bool IsGroupAllowedToApprove(PPrincipleApprovalRentalStatus status, AcceptorType group)
    {
        if (status == PPrincipleApprovalRentalStatus.WaitingCommitteeApproval && group != AcceptorType.RentCommittee)
        {
            return false;
        }

        if (status == PPrincipleApprovalRentalStatus.WaitingAcceptance && group != AcceptorType.Approver)
        {
            return false;
        }

        return true;
    }

    private static string GetGroupNotAllowedMessage(PPrincipleApprovalRentalStatus status)
    {
        if (status == PPrincipleApprovalRentalStatus.WaitingCommitteeApproval)
        {
            return "อนุมัติได้เฉพาะบุคคลคณะกรรมการจัดเช่าเท่านั้น";
        }

        if (status == PPrincipleApprovalRentalStatus.WaitingAcceptance)
        {
            return "อนุมัติได้เฉพาะผู้มีอำนาจเห็นชอบ/อนุมัติเท่านั้น";
        }

        return "ไม่สามารถอนุมัติในสถานะนี้ได้";
    }

    private static bool IsPreviousApproved(List<PPrincipleApprovalRentalAcceptor> acceptors, PPrincipleApprovalRentalAcceptor current)
    {
        if (current.Sequence <= 1)
        {
            return true;
        }

        var prev = acceptors.LastOrDefault(a => a.Sequence < current.Sequence && a.IsActive);

        return prev == null || prev.Status == AcceptorStatus.Approved;
    }

    private static bool ShouldUpdateStatus(AcceptorType type, List<PPrincipleApprovalRentalAcceptor> acceptors)
    {
        return acceptors
               .Where(a =>
                   a.Type == type)
               .All(a => a.Status == AcceptorStatus.Approved);
    }

    private static void TryUpdateCommitteeStatus(PPrincipleApprovalRental approvalRental)
    {
        var committee = approvalRental.Acceptors
                                      .Where(a => a is
                                      {
                                          Type: AcceptorType.RentCommittee,
                                          IsActive: true,
                                          IsUnableToPerformDuties: false
                                      })
                                      .ToList();

        var total = committee.Count;
        var totalReject =
            committee.Count(a => a.Status == AcceptorStatus.Rejected);
        var totalApprove = committee.Count(a => a.Status == AcceptorStatus.Approved);

        if (totalReject > total / 2.0)
        {
            approvalRental.SetStatusRejected(null);
        }

        if (committee.Any(x => x.IsBoardChairman() && x.Status == AcceptorStatus.Approved))
        {
            approvalRental.Assignees.Iter(r => r.Pending());
            approvalRental.SetStatusApproved();

            var director = approvalRental.Assignees.FirstOrDefault(f => f.Type == AssigneeType.Director);

            if (director != null)
            {
                foreach (var targetUserId in director.GetAssigneeNotificationTargets())
                {
                    _ = SendNotificationAsync(
                        approvalRental,
                        targetUserId,
                        NotificationConstant.WaitForAssignment.Title,
                        string.Format(
                            NotificationConstant.WaitForAssignment.Message,
                            ProgramConstant.PrincipalApprovalRental.Name,
                            approvalRental.Procurement.ProcurementNumber));
                }
            }
        }
    }

    private static void ShouldUpdateApprover(PPrincipleApprovalRental rental)
    {
        var isAllAcceptorApproved = rental.Acceptors
                                          .Where(a => a is
                                          {
                                              Type: AcceptorType.Approver,
                                              IsActive: true,
                                          })
                                          .All(a => a is
                                          {
                                              Status: AcceptorStatus.Approved,
                                          });

        if (isAllAcceptorApproved)
        {
            rental.SetStatusWaitingContractAssign();

            var director = rental.Assignees.FirstOrDefault(f => f.Type == AssigneeType.Director && f.Group == AssigneeGroup.Contract);

            if (director != null)
            {
                foreach (var targetUserId in director.GetAssigneeNotificationTargets())
                {
                    _ = SendNotificationAsync(
                        rental,
                        targetUserId,
                        NotificationConstant.WaitForAssignment.Title,
                        string.Format(
                            NotificationConstant.WaitForAssignment.Message,
                            ProgramConstant.PrincipalApprovalRental.Name,
                            rental.Procurement.ProcurementNumber));
                }
            }
        }
    }

    private static void UpdateSequentialCurrents(PPrincipleApprovalRental rental, AcceptorType type)
    {
        var approvers = rental.Acceptors
                              .Where(a => a.Type == type && a.IsActive && !a.IsUnableToPerformDuties)
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

        var next = approvers.FirstOrDefault(a =>
            a.Status == AcceptorStatus.Pending);

        if (next is null)
        {
            return;
        }

        next.SetCurrent();

        var pendingOfType = approvers.Where(a => a.Status == AcceptorStatus.Pending).ToList();
        var isLastPending = pendingOfType.Count == 1;

        if (next.Type == AcceptorType.DepartmentDirectorAgree ||
            (next.Type == AcceptorType.Approver && !isLastPending))
        {
            foreach (var targetUserId in next.GetNotificationTargets())
            {
                _ = SendNotificationAsync(
                    rental,
                    targetUserId,
                    NotificationConstant.WaitForLike.Title,
                    string.Format(NotificationConstant.WaitForLike.Message, ProgramConstant.PrincipalApprovalRental.Name, rental.Procurement.ProcurementNumber));
            }
        }
        else if (next.Type == AcceptorType.Approver && isLastPending)
        {
            foreach (var targetUserId in next.GetNotificationTargets())
            {
                _ = SendNotificationAsync(
                    rental,
                    targetUserId,
                    NotificationConstant.WaitForApprove.Title,
                    string.Format(NotificationConstant.WaitForApprove.Message, ProgramConstant.PrincipalApprovalRental.Name, rental.Procurement.ProcurementNumber));
            }
        }
    }

    private static async Task SendNotificationAsync(PPrincipleApprovalRental rental, UserId userId, string title, string message)
    {
        await Notification
              .Crate(
                  userId,
                  title,
                  message,
                  NotificationProgram.Procurement)
              .SetReferenceId(rental.Id.Value)
              .SetLinkUrl(string.Format(ProgramConstant.BranchSpaceRent.Url, rental.Procurement.Id), "ดูรายละเอียด")
              .PublishAsync(CancellationToken.None);
    }
}