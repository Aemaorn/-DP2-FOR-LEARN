namespace GHB.DP2.Application.Features.Procurement.Jp005;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.EventHandlers.AnnouncementInfo;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.Jp005.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PJp005;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public class ApproveJp005Request
{
    public Guid ProcurementId { get; init; }

    public Guid Id { get; init; }

    [FromClaim(JwtRegisteredClaimNames.Sub)]
    public Guid UserId { get; init; }

    public string? Remark { get; init; }
}

public class ApproveJp005Endpoint
    : Jp005EndpointBase<ApproveJp005Request, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;
    private readonly IFileServiceClient fileServiceClient;

    public ApproveJp005Endpoint(
        Dp2DbContext dbContext,
        IOperationService operationService,
        ICommandTextService commandTextService,
        IFileServiceClient fileServiceClient,
        ILogger<ApproveJp005Endpoint> logger)
        : base(dbContext, operationService, commandTextService, logger)
    {
        this.dbContext = dbContext;
        this.fileServiceClient = fileServiceClient;
    }

    public override void Configure()
    {
        this.Put("procurement/{ProcurementId:guid}/jp005/{Id:guid}/approve");
        this.Options(b =>
            b.WithTags("Procurement/JorPor005")
             .WithName("Approve")
             .Produces<Ok>()
             .ProducesProblem(StatusCodes.Status400BadRequest)
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status500InternalServerError));
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(
        ApproveJp005Request req,
        CancellationToken ct)
    {
        var procurement = await this.GetProcurementById(
            ProcurementId.From(req.ProcurementId),
            ct);

        var jp005Existing = this.GetJp005ById(
            procurement.Jp005,
            PJp005Id.From(req.Id),
            ProcurementId.From(req.ProcurementId));

        var canApprove =
            jp005Existing.Status is PJp005Status.WaitingApproval;

        if (!canApprove)
        {
            this.ThrowError(
                r =>
                    req.Id,
                $"จพ.005 ที่ระบุไม่อยู่ในสถานะที่สามารถอนุมัติได้ (สถานะปัจจุบัน: {jp005Existing.Status})",
                StatusCodes.Status404NotFound);
        }

        this.ApproverApprove(
            jp005Existing,
            req);

        UpdateSequentialCurrents(jp005Existing);

        jp005Existing.EvaluateAcceptorApproval();

        if (jp005Existing.Status == PJp005Status.Approved)
        {
            await SendNotificationCommitteeAsync(jp005Existing, ct);
        }

        var isSixTyOverPrice = jp005Existing.Procurement.SupplyMethodCode.Value == "SMethod002" && jp005Existing.Procurement.Budget > 100000;

        jp005Existing.AddActivity(new ActivityInfo(
            isSixTyOverPrice ? ActivityLogActionTypeConstant.ApprovedSegment : ActivityLogActionTypeConstant.Approved,
            $"เห็นชอบ/อนุมัติ",
            jp005Existing.Status.ToString(),
            req.Remark));

        var isAllPendingOrDraft = jp005Existing.Acceptors
                                               .Where(a =>
                                                   a is
                                                   {
                                                       Type: AcceptorType.Approver or AcceptorType.DepartmentDirectorAgree,
                                                       Status: AcceptorStatus.Pending,
                                                       IsActive: true
                                                   })
                                               .All(a => a.Status is AcceptorStatus.Pending or AcceptorStatus.Draft);

        if (isAllPendingOrDraft && jp005Existing.Status != PJp005Status.Approved)
        {
            await this.UpdateDocumentAsync(jp005Existing, req.UserId, procurement, true, hasCreator: false, hasAcceptor: true, hasPublisher: false, cancellationToken: ct);
        }

        if (jp005Existing.Status == PJp005Status.Approved && !isSixTyOverPrice && jp005Existing.LastedWaitingApprovalIsReplaceApprovalDocument != null)
        {
            await this.UpdateDocumentAsync(jp005Existing, req.UserId, procurement, true, hasCreator: false, hasAcceptor: true, hasPublisher: true, cancellationToken: ct);

            var planType = jp005Existing.Procurement.SupplyMethod.Code == SupplyMethodConstant.Sixty ? Section60.ReportPlan : Section80.ReportPlan;

            var file = await this.fileServiceClient.DownloadAsStreamAsync(jp005Existing.LastedWaitingApprovalIsReplaceApprovalDocument.FileId, cancellationToken: ct);

            await AnnouncementData.Create(
                                      jp005Existing.Procurement.Name,
                                      DateTimeOffset.UtcNow,
                                      jp005Existing.Procurement.Budget ?? decimal.Zero,
                                      string.Empty,
                                      planType,
                                      file?.Stream)
                                  .PublishEvent(ct);
        }

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private void ApproverApprove(
        PJp005 jp005Existing,
        ApproveJp005Request req)
    {
        var draftAcceptors =
            jp005Existing.Acceptors
                         .Where(a =>
                             a is
                             {
                                 Type: AcceptorType.Approver or AcceptorType.DepartmentDirectorAgree,
                                 Status: AcceptorStatus.Pending,
                                 IsActive: true
                             })
                         .Map(DelegatorExtensions.DelegatorToAcceptor)
                         .OrderBy(a => a.Sequence)
                         .ToList();

        var currentAcceptor =
                draftAcceptors.FirstOrDefault(a => a.Delegatee?.SuUserId == null
                            ? a.UserId == req.UserId
                            : a.Delegatee?.SuUserId == UserId.From(req.UserId)
                              && a.Status == AcceptorStatus.Pending);

        if (currentAcceptor is null)
        {
            this.ThrowError(
                "ไม่พบผู้อนุมัติที่ใช้งานได้",
                StatusCodes.Status400BadRequest);
        }

        var currentAcceptorUser =
            jp005Existing.Acceptors
                         .FirstOrDefault(a => a.Id == currentAcceptor.Id);

        if (currentAcceptorUser is null)
        {
            this.ThrowError(
                "ไม่พบผู้อนุมัติที่ใช้งานได้",
                StatusCodes.Status400BadRequest);
        }

        if (!currentAcceptorUser.ArePreviousAcceptorsApproved(jp005Existing.Acceptors))
        {
            this.ThrowError(
                "ต้องได้รับการอนุมัติจากลำดับก่อนหน้าก่อน",
                StatusCodes.Status400BadRequest);
        }

        currentAcceptorUser
            .SetDelegatee(currentAcceptor.DelegateeId)
            .Approve(remark: req.Remark);
    }

    private static void UpdateSequentialCurrents(PJp005 jp005)
    {
        var approvers = jp005.Acceptors
                             .Where(a => a.IsActive)
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

        var next = approvers.Select(DelegatorExtensions.DelegatorToAcceptor)
                            .FirstOrDefault(a => a.Status == AcceptorStatus.Pending);

        if (next is null)
        {
            return;
        }

        var pendingOfType = approvers.Where(a => a.Status == AcceptorStatus.Pending).ToList();
        var isLastPending = pendingOfType.Count == 1;

        if (jp005.Status == PJp005Status.WaitingApproval && next.Status == AcceptorStatus.Pending)
        {
            next.SetCurrent();
        }

        if ((next.Type == AcceptorType.Approver && !isLastPending) || next.Type == AcceptorType.DepartmentDirectorAgree)
        {
            foreach (var targetUserId in next.GetNotificationTargets())
            {
                _ = SendNotificationAsync(jp005, targetUserId, NotificationConstant.WaitForLike.Title, string.Format(NotificationConstant.WaitForLike.Message, ProgramConstant.PreProcurementJorPor05.Name, jp005.PJp005Number));
            }
        }
        else if (next.Type == AcceptorType.Approver && isLastPending)
        {
            foreach (var targetUserId in next.GetNotificationTargets())
            {
                _ = SendNotificationAsync(jp005, targetUserId, NotificationConstant.WaitForApprove.Title, string.Format(NotificationConstant.WaitForApprove.Message, ProgramConstant.PreProcurementJorPor05.Name, jp005.PJp005Number));
            }
        }
    }

    private static async Task SendNotificationAsync(PJp005 jp005, UserId userId, string title, string message)
    {
        await Notification
              .Crate(
                  userId,
                  title,
                  message,
                  NotificationProgram.Procurement)
              .SetReferenceId(jp005.Id.Value)
              .SetLinkUrl(string.Format(ProgramConstant.Procurement.Url, jp005.Procurement.Id), "ดูรายละเอียด")
              .PublishAsync(CancellationToken.None);
    }

    private static async Task SendNotificationCommitteeAsync(PJp005 jp005, CancellationToken ct)
    {
        _ = await jp005.Committees.Where(w => w.GroupType == PJp005CommitteeGroupType.InspectionCommittee).Map(pa =>
                           Notification
                               .Crate(
                                   pa.SuUserId,
                                   NotificationConstant.InformInspector.Title,
                                   string.Format(NotificationConstant.InformInspector.Message, ProgramConstant.PreProcurementJorPor05.Name, jp005.PJp005Number),
                                   NotificationProgram.Procurement)
                               .SetReferenceId(jp005.Id.Value)
                               .SetLinkUrl(
                                   string.Format(ProgramConstant.Procurement.Url, jp005.Procurement.Id),
                                   "ดูรายละเอียด"))
                       .Map(n => n.PublishAsync(ct).ToUnit())
                       .SequenceSerial();

        _ = await jp005.Committees.Where(w => w.GroupType == PJp005CommitteeGroupType.ProcurementCommittee).Map(pa =>
                           Notification
                               .Crate(
                                   pa.SuUserId,
                                   NotificationConstant.InformPurchaseCommittee.Title,
                                   string.Format(NotificationConstant.InformPurchaseCommittee.Message, ProgramConstant.PreProcurementJorPor05.Name, jp005.PJp005Number),
                                   NotificationProgram.Procurement)
                               .SetReferenceId(jp005.Id.Value)
                               .SetLinkUrl(
                                   string.Format(ProgramConstant.Procurement.Url, jp005.Procurement.Id),
                                   "ดูรายละเอียด"))
                       .Map(n => n.PublishAsync(ct).ToUnit())
                       .SequenceSerial();
    }
}