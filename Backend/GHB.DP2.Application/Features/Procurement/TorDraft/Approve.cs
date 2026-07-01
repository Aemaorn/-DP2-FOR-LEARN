namespace GHB.DP2.Application.Features.Procurement.TorDraft;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.EventHandlers.AnnouncementInfo;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.TorDraft.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PpAppoint;
using GHB.DP2.Domain.Procurement.PpTorDraft;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record ApproveTorDraftRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid ProcurementId,
    Guid TorDraftId,
    AcceptorType Group, // "Approver" or "TorDraftCommittee"
    string? Remark);

public class ApproveEndpoint : TorDraftEndpointBase<ApproveTorDraftRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;
    private readonly IFileServiceClient fileServiceClient;

    public ApproveEndpoint(
        Dp2DbContext dbContext,
        IOperationService operationService,
        IFileServiceClient fileServiceClient,
        ICommandTextService commandTextService,
        ILogger<ApproveEndpoint> logger)
        : base(logger, dbContext, operationService, commandTextService)
    {
        this.dbContext = dbContext;
        this.fileServiceClient = fileServiceClient;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("Procurement/TorDraft")
             .WithName("ApproveTorDraft")
             .AllowAnonymous()
             .Accepts<ApproveTorDraftRequest>("application/json"));
        this.Post("procurement/{ProcurementId:guid}/tordraft/{TorDraftId:guid}/approve");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(ApproveTorDraftRequest req, CancellationToken ct)
    {
        var torDraft = await this.ValidateRequestAsync(ProcurementId.From(req.ProcurementId), PpTorDraftId.From(req.TorDraftId), ct);
        var previousStatus = torDraft.Status;

        var appoint = await this.GetAppointById(ProcurementId.From(req.ProcurementId), ct);

        var torDraftAcceptors =
            torDraft.PpTorDraftAcceptors
                    .Where(a =>
                        a.Type == req.Group &&
                        a is
                        {
                            IsActive: true,
                            IsUnableToPerformDuties: false,
                            Status: AcceptorStatus.Pending
                        })
                    .ToList();

        var acceptors =
            torDraftAcceptors
                .OrderBy(a => a.Sequence)
                .ToList();

        if (req.Group != AcceptorType.TorDraftCommittee)
        {
            acceptors =
                [.. torDraftAcceptors.Map(DelegatorExtensions.DelegatorToAcceptor)];
        }

        var currentAcceptor =
            acceptors.FirstOrDefault(a => a.Delegatee?.SuUserId == null
                ? a.UserId == req.UserId
                : a.Delegatee?.SuUserId == UserId.From(req.UserId)
                  && a.Status == AcceptorStatus.Pending);

        if (currentAcceptor == null)
        {
            return TypedResults.BadRequest("ไม่พบผู้อนุมัติในกลุ่มหรือขั้นตอนนี้");
        }

        var currentAcceptorUser =
            torDraft.PpTorDraftAcceptors
                    .First(a => a.Id == currentAcceptor.Id);

        currentAcceptorUser
            .SetDelegatee(currentAcceptor.DelegateeId)
            .Approve(remark: req.Remark);

        currentAcceptorUser.SetCurrent(false);

        switch (torDraft.Status)
        {
            case TorDraftStatus.WaitingCommitteeApproval:
                torDraft.AddActivity(new ActivityInfo(
                    "บุคคล/คณะกรรมการจัดทำร่างขอบเขตของงานเห็นชอบ",
                    $"คณะกรรมการเห็นชอบ/อนุมัติ",
                    torDraft.Status.ToString(),
                    req.Remark));

                break;

            case TorDraftStatus.WaitingUnitApproval:
                torDraft.AddActivity(new ActivityInfo(
                    "สายงานเห็นชอบ",
                    $"ลำดับเห็นชอบ/อนุมัติ",
                    torDraft.Status.ToString(),
                    req.Remark));

                break;

            case TorDraftStatus.WaitingApproval:
                torDraft.AddActivity(new ActivityInfo(
                    ActivityLogActionTypeConstant.Approved,
                    $"ผู้มีอำนาจเห็นชอบ/อนุมัติ",
                    torDraft.Status.ToString(),
                    req.Remark));

                break;
        }

        if (ShouldUpdateStatus(req.Group, [.. torDraft.PpTorDraftAcceptors], currentAcceptor))
        {
            UpdateTorDraftStatus(torDraft);
        }

        var isReplace = previousStatus == TorDraftStatus.WaitingApproval ? true : false;

        switch (torDraft.Status)
        {
            case TorDraftStatus.WaitingUnitApproval:
                UpdateSequentialCurrents(torDraft, AcceptorType.DepartmentDirectorAgree);

                break;

            case TorDraftStatus.WaitingApproval:
                UpdateSequentialCurrents(torDraft, AcceptorType.Approver);

                break;

            case TorDraftStatus.WaitingCommitteeApproval:
                UpdateCommitteeCurrents(torDraft);
                isReplace = true;

                break;

            case TorDraftStatus.WaitingAssign:
                var directorAssignee = torDraft.Assignees
                                               .Select(DelegatorExtensions.DelegatorToAssignee)
                                               .FirstOrDefault(x => x.Type == AssigneeType.Director);

                if (directorAssignee is not null)
                {
                    foreach (var targetUserId in directorAssignee.GetAssigneeNotificationTargets())
                    {
                        _ = SendNotificationAsync(
                            torDraft,
                            targetUserId,
                            NotificationConstant.WaitForAssignment.Title,
                            string.Format(
                                NotificationConstant.WaitForAssignment.Message,
                                GetTorName(torDraft),
                                torDraft.ReferenceNumber));
                    }
                }

                break;

            case TorDraftStatus.Approved:
                {
                    var torName = torDraft.IsChange
                        ? ProgramConstant.ChangeTor.Name
                        : torDraft.IsCancel
                            ? ProgramConstant.CancelTor.Name
                            : ProgramConstant.PreProcurementTorDraft.Name;

                    var approvedTitle = (torDraft.IsChange || torDraft.IsCancel)
                        ? NotificationConstant.PlanActionApproved.Title
                        : NotificationConstant.InformCommittee.Title;

                    var approvedMessage = (torDraft.IsChange || torDraft.IsCancel)
                        ? string.Format(NotificationConstant.PlanActionApproved.Message, torName, torDraft.ReferenceNumber)
                        : string.Format(NotificationConstant.InformCommittee.Message, torName, torDraft.ReferenceNumber);

                    var committeeMembers = torDraft.PpTorDraftAcceptors
                                                   .Where(a => a.Type == AcceptorType.TorDraftCommittee && a.IsActive)
                                                   .ToList();

                    foreach (var member in committeeMembers)
                    {
                        _ = SendNotificationAsync(
                            torDraft,
                            member.UserId,
                            approvedTitle,
                            approvedMessage);
                    }

                    if (torDraft.Procurement.HasMd)
                    {
                        _ = SendNotificationAssigneeAsync(torDraft, CancellationToken.None);
                    }

                    break;
                }
        }

        if (torDraft.Status == TorDraftStatus.Approved)
        {
            var planType = torDraft.Procurement.SupplyMethod.Code == SupplyMethodConstant.Sixty ? Section60.Tor : Section80.Tor;

            var file = await this.fileServiceClient.DownloadAsStreamAsync(torDraft.LastedDocument(PpTorDraftDocumentType.Tor)!.FileId, cancellationToken: ct);

            await AnnouncementData.Create(
                                      torDraft.Procurement.Name,
                                      DateTimeOffset.UtcNow,
                                      torDraft.Procurement.Budget ?? decimal.Zero,
                                      string.Empty,
                                      planType,
                                      file?.Stream)
                                  .PublishEvent(ct);

            if (appoint is not null && appoint.MedianPriceCommittees.Count > 0)
            {
                await SendNotificationMedianPrize(appoint, torDraft, ct);
            }
        }

        this.dbContext.PpTorDrafts.Update(torDraft);

        await this.ReplaceDocumentsAsync(torDraft, appoint, ct, previousStatus, isReplace);

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private static bool ShouldUpdateStatus(
        AcceptorType type,
        List<PpTorDraftAcceptors> acceptors,
        PpTorDraftAcceptors current)
    {
        if (type is AcceptorType.TorDraftCommittee)
        {
            return current.IsBoardChairman();
        }

        return acceptors
               .Where(a =>
                   a.Type == type &&
                   !a.IsUnableToPerformDuties)
               .All(a => a.Status == AcceptorStatus.Approved);
    }

    private static void UpdateTorDraftStatus(PpTorDraft torDraft)
    {
        _ = (torDraft.Status, torDraft.Procurement.HasMd, torDraft.IsCancel) switch
        {
            (TorDraftStatus.WaitingCommitteeApproval, true, _) => torDraft.SetWaitingUnitApproval(),
            (TorDraftStatus.WaitingCommitteeApproval, false, _) => torDraft.SetWaitingApproval(),
            (TorDraftStatus.WaitingUnitApproval, true, _) => torDraft.SetUnitApproved(),
            (TorDraftStatus.WaitingApproval, _, false) => torDraft.SetApproved(),
            (TorDraftStatus.WaitingApproval, _, true) => torDraft.SetCancelled(),
            _ => throw new NotSupportedException("ไม่รองรับการอัพเดตสถานะร่างขอบเขตงานในสถานะนี้"),
        };
    }

    private static void UpdateCommitteeCurrents(PpTorDraft torDraft)
    {
        var committee = torDraft.PpTorDraftAcceptors
                                .Where(a => a.Type == AcceptorType.TorDraftCommittee && a.IsActive && !a.IsUnableToPerformDuties)
                                .ToList();

        if (committee.Count == 0)
        {
            return;
        }

        var chairman = committee.FirstOrDefault(IsChairman);
        var nonChair = chairman is null ? committee : [.. committee.Where(a => a.Id != chairman.Id)];

        foreach (var a in committee)
        {
            a.SetCurrent(false);
        }

        var pendingNonChair = nonChair.Where(a => a.Status == AcceptorStatus.Pending).ToList();

        if (pendingNonChair.Count > 0)
        {
            foreach (var p in pendingNonChair)
            {
                p.SetCurrent(true);
            }

            return;
        }

        var allNonChairReady = nonChair.All(a => a.Status == AcceptorStatus.Approved || a.IsUnableToPerformDuties);

        if (chairman is not null && chairman.Status == AcceptorStatus.Pending && allNonChairReady)
        {
            chairman.SetCurrent(true);

            _ = SendNotificationAsync(
                torDraft,
                chairman.UserId,
                NotificationConstant.WaitForLike.Title,
                string.Format(
                    NotificationConstant.WaitForLike.Message,
                    GetTorName(torDraft),
                    torDraft.ReferenceNumber));
        }
    }

    private static string GetTorName(PpTorDraft tor) =>
        tor.IsChange ? ProgramConstant.ChangeTor.Name
        : tor.IsCancel ? ProgramConstant.CancelTor.Name
        : ProgramConstant.PreProcurementTorDraft.Name;

    private static bool IsChairman(PpTorDraftAcceptors a)
    {
        // Either committee position code PosBoard001 or IsBoardChairman metadata
        if (a.CommitteePositionsCode != null && a.CommitteePositionsCode == ParameterCode.From("PosBoard001"))
        {
            return true;
        }

        return a.IsBoardChairman();
    }

    private static void UpdateSequentialCurrents(PpTorDraft torDraft, AcceptorType type)
    {
        var approvers = torDraft.PpTorDraftAcceptors
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

        var next = approvers.Select(DelegatorExtensions.DelegatorToAcceptor).FirstOrDefault(a =>
            a.Status == AcceptorStatus.Pending);

        if (next is null)
        {
            return;
        }

        next.SetCurrent(true);

        var pendingOfType = approvers.Where(a => a.Status == AcceptorStatus.Pending).ToList();
        var isLastPending = pendingOfType.Count == 1;

        if (next.Type == AcceptorType.DepartmentDirectorAgree ||
            (next.Type == AcceptorType.Approver && !isLastPending))
        {
            foreach (var targetUserId in next.GetNotificationTargets())
            {
                _ = SendNotificationAsync(
                    torDraft,
                    targetUserId,
                    NotificationConstant.WaitForLike.Title,
                    string.Format(NotificationConstant.WaitForLike.Message, GetTorName(torDraft), torDraft.ReferenceNumber));
            }
        }
        else if (next.Type == AcceptorType.Approver && isLastPending)
        {
            foreach (var targetUserId in next.GetNotificationTargets())
            {
                _ = SendNotificationAsync(
                    torDraft,
                    targetUserId,
                    NotificationConstant.WaitForApprove.Title,
                    string.Format(NotificationConstant.WaitForApprove.Message, GetTorName(torDraft), torDraft.ReferenceNumber));
            }
        }
    }

    private static async Task SendNotificationAsync(PpTorDraft torDraft, UserId userId, string title, string message)
    {
        await Notification
              .Crate(
                  userId,
                  title,
                  message,
                  NotificationProgram.Procurement)
              .SetReferenceId(torDraft.Id.Value)
              .SetLinkUrl(string.Format(ProgramConstant.Procurement.Url, torDraft.Procurement.Id), "ดูรายละเอียด")
              .PublishAsync(CancellationToken.None);
    }

    private static async Task SendNotificationAssigneeAsync(PpTorDraft torDraft, CancellationToken ct)
    {
        foreach (var targetUserId in torDraft.Assignees.Where(x => x.Type != AssigneeType.Director).SelectMany(a => a.GetAssigneeNotificationTargets()))
        {
            await Notification
                  .Crate(
                      targetUserId,
                      NotificationConstant.InformCommittee.Title,
                      string.Format(NotificationConstant.InformCommittee.Message, GetTorName(torDraft), torDraft.ReferenceNumber),
                      NotificationProgram.Procurement)
                  .SetReferenceId(torDraft.Id.Value)
                  .SetLinkUrl(
                      string.Format(ProgramConstant.Procurement.Url, torDraft.Procurement.Id),
                      "ดูรายละเอียด")
                  .PublishAsync(ct);
        }
    }

    private static async Task SendNotificationMedianPrize(PpAppoint appoint, PpTorDraft torDraft, CancellationToken ct)
    {
        _ = await appoint.MedianPriceCommittees.Map(a =>
                             Notification
                                 .Crate(
                                     a.SuUserId,
                                     NotificationConstant.MedianPriceAnnouncement.Title,
                                     string.Format(NotificationConstant.MedianPriceAnnouncement.Message, GetTorName(torDraft), torDraft.ReferenceNumber),
                                     NotificationProgram.Procurement)
                                 .SetReferenceId(torDraft.Id.Value)
                                 .SetLinkUrl(
                                     string.Format(ProgramConstant.Procurement.Url, torDraft.Procurement.Id),
                                     "ดูรายละเอียด"))
                         .Map(n => n.PublishAsync(ct).ToUnit())
                         .SequenceSerial();
    }
}