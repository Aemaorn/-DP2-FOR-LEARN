namespace GHB.DP2.Application.Features.Procurement.Jp006;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.EventHandlers.AnnouncementInfo;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.Jp006.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PPurchaseOrder;
using GHB.DP2.Domain.Raws.Constants;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class ApproveJp006Request
{
    public Guid ProcurementId { get; init; }

    public Guid Jp006Id { get; init; }

    [FromClaim(JwtRegisteredClaimNames.Sub)]
    public Guid UserId { get; init; }

    public Guid OperationUserId { get; init; }

    public string? Remark { get; init; }
}

public class ApproveJp006Endpoint : Jp006EndpointBase<ApproveJp006Request, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    private readonly IFileServiceClient fileServiceClient;
    private readonly IOperationService operationService;

    public ApproveJp006Endpoint(
        ILogger<AssigneePurchaseOrderEndpoint> logger,
        IOperationService operationService,
        ICommandTextService commandTextService,
        IFileServiceClient fileServiceClient,
        Dp2DbContext dbContext)
        : base(logger, operationService, commandTextService, fileServiceClient, dbContext)
    {
        this.dbContext = dbContext;

        this.fileServiceClient = fileServiceClient;
        this.operationService = operationService;
    }

    public override void Configure()
    {
        this.Put("procurement/{ProcurementId:guid}/jp006/{Jp006Id:guid}/approve");
        this.Options(b =>
            b.WithTags(nameof(Jp006))
             .WithName("ApproveJp006")
             .Produces<Ok>()
             .ProducesProblem(StatusCodes.Status400BadRequest)
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status500InternalServerError));
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(ApproveJp006Request req, CancellationToken ct)
    {
        var procurementExisting = await this.ValidateProcurementAsync(req.ProcurementId, ct);

        var jp006 = await this.dbContext.PJp006S
                              .Include(p => p.Acceptors)
                              .ThenInclude(acceptorInfoEntity => acceptorInfoEntity.Delegatee)
                              .Include(p => p.Procurement)
                              .ThenInclude(procurement => procurement.SupplyMethod)
                              .Include(p => p.Assignees)
                              .ThenInclude(p => p.User)
                              .ThenInclude(p => p.Employee)
                              .Include(pPurchaseOrder => pPurchaseOrder.Entrepreneurs)
                              .ThenInclude(pPurchaseOrderEntrepreneur => pPurchaseOrderEntrepreneur.PJp006PriceDetails)
                              .Include(p => p.DocumentHistories)
                              .Include(auditableEntity => auditableEntity.AuditInfo)
                              .SingleOrDefaultAsync(
                                  p =>
                                      p.Id == Domain.Procurement.PPurchaseOrder.PurchaseOrderId.From(req.Jp006Id) &&
                                      p.ProcurementId == ProcurementId.From(req.ProcurementId),
                                  ct);

        if (jp006 is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลการจัดซื้อจัดจ้าง");
        }

        var acceptorType = jp006.Status switch
        {
            PurchaseOrderStatus.WaitingCommitteeApproval => AcceptorType.ProcurementCommittee,
            PurchaseOrderStatus.WaitingApproval => AcceptorType.Approver,
            _ => throw new InvalidOperationException("สถานะการอนุมัติไม่รองรับ"),
        };

        var acceptors = jp006.Acceptors
                             .Where(a =>
                                 a.Type == acceptorType &&
                                 a is { IsActive: true, IsUnableToPerformDuties: false, Status: AcceptorStatus.Pending })
                             .OrderBy(a => a.Sequence)
                             .ToList();

        if (acceptorType == AcceptorType.Approver)
        {
            acceptors = [.. acceptors.Map(DelegatorExtensions.DelegatorToAcceptor).OrderBy(a => a.Sequence)];
        }

        var current = acceptors.FirstOrDefault(a => a.Delegatee?.SuUserId == null
            ? a.UserId == req.UserId
            : a.Delegatee?.SuUserId == UserId.From(req.UserId)
              && a.Type == acceptorType
              && a.Status == AcceptorStatus.Pending);

        if (current == null)
        {
            return TypedResults.BadRequest("ไม่พบผู้อนุมัติในกลุ่มหรือขั้นตอนนี้");
        }

        var currentAcceptorUser =
            jp006.Acceptors
                 .FirstOrDefault(a => a.Id == current.Id);

        if (currentAcceptorUser == null)
        {
            return TypedResults.BadRequest("ไม่พบผู้อนุมัติในกลุ่มหรือขั้นตอนนี้");
        }

        currentAcceptorUser
            .SetDelegatee(current.DelegateeId)
            .Approve(remark: req.Remark);

        var shouldUpdateStatus = ShouldUpdateStatus(acceptorType, [.. jp006.Acceptors], current);

        if (!current.ArePreviousAcceptorsApproved(jp006.Acceptors) && acceptorType == AcceptorType.Approver)
        {
            this.ThrowError(
                "ต้องได้รับการอนุมัติจากลำดับก่อนหน้าก่อน",
                StatusCodes.Status400BadRequest);
        }

        if (acceptors.Any(c => !c.IsBoardChairman()) &&
            current.IsBoardChairman() && acceptorType == AcceptorType.ProcurementCommittee)
        {
            this.ThrowError(
                "ผู้อนุมัติคณะกรรมการไม่ตรงกับตำแหน่งที่กำหนด",
                StatusCodes.Status400BadRequest);
        }

        this.ValidateJp006Status(jp006);

        var processType = SectionProcessType.PurchaseOrder;
        var isCommercialMaterial = jp006.Procurement.IsCommercialMaterial;

        if (isCommercialMaterial)
        {
            processType = SectionProcessType.PurchaseOrderCommercialParcel;
        }

        var budget = jp006.Entrepreneurs.Where(e => e.IsWinner).SelectMany(s => s.PJp006PriceDetails).Sum(s => s.AgreedPrice * s.ParcelQuantity);

        var managers = await this.operationService.GetDefaultAcceptorAsync(
            processType,
            req.OperationUserId,
            budget,
            jp006.Procurement.SupplyMethodCode.Value,
            jp006.Procurement.SupplyMethodCode.Value is SupplyMethodConstant.Eighty ? default : (string?)jp006.Procurement.SupplyMethodSpecialTypeCode,
            ct,
            false);

        var isDirectorLine = managers.Any(m => m.OrganizationLevel.ToString() == EmployeeConstant.OrganizationLevel.Line);

        switch (jp006.Status)
        {
            case PurchaseOrderStatus.WaitingCommitteeApproval:
                await this.ReplaceDocumentCommitteeApproved(jp006.Procurement, jp006, ct);

                if (!shouldUpdateStatus)
                {
                    jp006.AddActivity(new ActivityInfo(
                        ActivityLogActionTypeConstant.CommitteeApproved,
                        $"ผู้มีอำนาจเห็นชอบ/อนุมัติ",
                        jp006.Status.ToString(),
                        req.Remark));
                }

                break;

            case PurchaseOrderStatus.WaitingApproval:
                await this.ReplaceDocumentApproverApproved(jp006.Procurement, jp006, ct);

                if (!shouldUpdateStatus)
                {
                    if (isDirectorLine)
                    {
                        jp006.AddActivity(new ActivityInfo(
                            ActivityLogActionTypeConstant.Approved,
                            $"ผู้มีอำนาจเห็นชอบ/อนุมัติ",
                            jp006.Status.ToString(),
                            req.Remark));
                    }
                    else
                    {
                        jp006.AddActivity(new ActivityInfo(
                            "สายงานเห็นชอบ/อนุมัตื",
                            $"ผู้มีอำนาจเห็นชอบ/อนุมัติ",
                            jp006.Status.ToString(),
                            req.Remark));
                    }
                }

                break;

            default:
                this.ThrowError(
                    "กลุ่มการอนุมัติไม่รองรับ",
                    StatusCodes.Status400BadRequest);

                break;
        }

        if (shouldUpdateStatus)
        {
            await this.UpdateJp006Status(jp006, isDirectorLine, req.Remark, ct);
        }

        switch (jp006.Status)
        {
            case PurchaseOrderStatus.WaitingApproval:
                UpdateSequentialCurrents(jp006, AcceptorType.Approver);

                break;

            case PurchaseOrderStatus.WaitingCommitteeApproval:
                UpdateCommitteeCurrents(jp006);

                break;

            case PurchaseOrderStatus.Approved:
                var jp004 = await this.dbContext.PpPurchaseRequisitions
                                      .Include(p => p.Assignees)
                                      .FirstOrDefaultAsync(w => w.ProcurementId == ProcurementId.From(req.ProcurementId), ct);

                UserId[] assigneeUserIds = jp004?.LastedAssignee is { } lastedAssignee ? [lastedAssignee.UserId] : [];

                var recipients = new[] { UserId.From(jp006.AuditInfo.CreatedBy) }
                    .Concat(assigneeUserIds)
                    .Distinct();

                foreach (var userId in recipients)
                {
                    _ = SendNotificationAsync(
                        jp006,
                        userId,
                        NotificationConstant.InformCommittee.Title,
                        string.Format(NotificationConstant.InformCommittee.Message, ProgramConstant.PreProcurementJorPor06.Name, jp006.PurchaseOrderNumber));
                }

                var planType = jp006.Procurement.SupplyMethod.Code == SupplyMethodConstant.Sixty ? Section60.Winner : Section80.Winner;

                if (!procurementExisting.IsSixtyAndMoreThanOneHundredThousand)
                {
                    var file = await this.fileServiceClient.DownloadAsStreamAsync(jp006.GetLatestDocumentHistory(PurchaseOrderDocumentType.Jp006)!.FileId, cancellationToken: ct);

                    await AnnouncementData.Create(
                                              jp006.Procurement.Name,
                                              DateTimeOffset.UtcNow,
                                              jp006.Procurement.Budget ?? decimal.Zero,
                                              string.Empty,
                                              planType,
                                              file?.Stream)
                                          .PublishEvent(ct);
                }

                await this.ReplaceDocumentPublisherApproved(jp006.Procurement, jp006, req.UserId, ct);

                break;
        }

        // Save changes
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private void CommitteeApproval(PPurchaseOrder purchaseOrder, ApproveJp006Request req)
    {
        var committeeAcceptors =
            purchaseOrder.Acceptors
                         .Where(a =>
                             a is
                             {
                                 Type: AcceptorType.ProcurementCommittee,
                                 Status: AcceptorStatus.Pending,
                                 IsUnableToPerformDuties: false,
                                 IsActive: true,
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

        // Approve the committee acceptor
        acceptor.SetIsCurrent(false);
        acceptor.Approve(req.Remark);
    }

    private void ApproverApproval(PPurchaseOrder purchaseOrder, ApproveJp006Request req)
    {
        var approverAcceptors =
            purchaseOrder.Acceptors
                         .Where(a =>
                             a is
                             {
                                 Type: AcceptorType.Approver,
                                 Status: AcceptorStatus.Pending,
                                 IsActive: true,
                             })
                         .ToArray();

        var acceptor =
            approverAcceptors
                .FirstOrDefault(a => a.UserId == UserId.From(req.UserId));

        if (acceptor is null)
        {
            this.ThrowError(
                "ไม่พบผู้อนุมัติที่ใช้งานได้",
                StatusCodes.Status400BadRequest);
        }

        if (!acceptor.ArePreviousAcceptorsApproved(purchaseOrder.Acceptors))
        {
            this.ThrowError(
                "ต้องได้รับการอนุมัติจากลำดับก่อนหน้าก่อน",
                StatusCodes.Status400BadRequest);
        }

        // Approve the approver acceptor
        acceptor.SetIsCurrent(false);
        acceptor.Approve(req.Remark);
    }

    private async Task UpdateJp006Status(PPurchaseOrder purchaseOrder, bool isDirectorLine, string? remark, CancellationToken ct)
    {
        switch (purchaseOrder.Status, isDirectorLine)
        {
            case (PurchaseOrderStatus.WaitingCommitteeApproval, false):
                _ = purchaseOrder.SetWaitingAcceptor();
                await this.StampCheckPointLastedDocument(purchaseOrder.Procurement, purchaseOrder, PurchaseOrderDocumentType.Jp006, ct);
                await this.StampCheckPointLastedDocument(purchaseOrder.Procurement, purchaseOrder, PurchaseOrderDocumentType.Winner, ct);

                break;

            case (PurchaseOrderStatus.WaitingApproval, _):
                _ = purchaseOrder.SetApproved(remark);
                _ = purchaseOrder.Procurement.SetProcurementStep(purchaseOrder.Procurement.Type, ProcurementStep.Procurement);

                break;

            case (PurchaseOrderStatus.WaitingCommitteeApproval, true):
                _ = purchaseOrder.SetWaitingAssignee();

                var directorAssignee = purchaseOrder.Assignees.Select(DelegatorExtensions.DelegatorToAssignee).FirstOrDefault(x => x.Type == AssigneeType.Director);

                if (directorAssignee is not null)
                {
                    foreach (var targetUserId in directorAssignee.GetAssigneeNotificationTargets())
                    {
                        _ = SendNotificationAsync(
                            purchaseOrder,
                            targetUserId,
                            NotificationConstant.WaitForAssignment.Title,
                            string.Format(NotificationConstant.WaitForAssignment.Message, ProgramConstant.PreProcurementJorPor06.Name, purchaseOrder.PurchaseOrderNumber));
                    }
                }

                await this.StampCheckPointLastedDocument(purchaseOrder.Procurement, purchaseOrder, PurchaseOrderDocumentType.Jp006, ct);
                await this.StampCheckPointLastedDocument(purchaseOrder.Procurement, purchaseOrder, PurchaseOrderDocumentType.Winner, ct);

                break;

            default:
                this.ThrowError(
                    "สถานะการอนุมัติหรือกลุ่มไม่รองรับ",
                    StatusCodes.Status400BadRequest);

                break;
        }
    }

    private static void UpdateCommitteeCurrents(PPurchaseOrder purchase)
    {
        var committee = purchase.Acceptors
                                .Where(a => a.Type == AcceptorType.ProcurementCommittee && a.IsActive && !a.IsUnableToPerformDuties)
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
                purchase,
                chairman.UserId,
                NotificationConstant.WaitForLike.Title,
                string.Format(NotificationConstant.WaitForLike.Message, ProgramConstant.PreProcurementJorPor06.Name, purchase.PurchaseOrderNumber));
        }
    }

    private static bool IsChairman(PPurchaseOrderAcceptor a)
    {
        // Either committee position code PosBoard001 or IsBoardChairman metadata
        if (a.CommitteePositionsCode != null && a.CommitteePositionsCode == ParameterCode.From("PosBoard001"))
        {
            return true;
        }

        return a.IsBoardChairman();
    }

    private static void UpdateSequentialCurrents(PPurchaseOrder purchase, AcceptorType type)
    {
        var approvers = purchase.Acceptors
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
                    purchase,
                    targetUserId,
                    NotificationConstant.WaitForLike.Title,
                    string.Format(NotificationConstant.WaitForLike.Message, ProgramConstant.PreProcurementJorPor06.Name, purchase.PurchaseOrderNumber));
            }
        }
        else if (next.Type == AcceptorType.Approver && isLastPending)
        {
            foreach (var targetUserId in next.GetNotificationTargets())
            {
                _ = SendNotificationAsync(
                    purchase,
                    targetUserId,
                    NotificationConstant.WaitForApprove.Title,
                    string.Format(NotificationConstant.WaitForApprove.Message, ProgramConstant.PreProcurementJorPor06.Name, purchase.PurchaseOrderNumber));
            }
        }
    }

    private static async Task SendNotificationAsync(PPurchaseOrder purchase, UserId userId, string title, string message)
    {
        await Notification
              .Crate(
                  userId,
                  title,
                  message,
                  NotificationProgram.Procurement)
              .SetReferenceId(purchase.Id.Value)
              .SetLinkUrl(string.Format(ProgramConstant.Procurement.Url, purchase.Procurement.Id), "ดูรายละเอียด")
              .PublishAsync(CancellationToken.None);
    }

    private static bool ShouldUpdateStatus(
        AcceptorType type,
        List<PPurchaseOrderAcceptor> acceptors,
        PPurchaseOrderAcceptor current)
    {
        if (type is AcceptorType.ProcurementCommittee)
        {
            return current.IsBoardChairman();
        }

        return acceptors
               .Where(a =>
                   a.Type == type &&
                   !a.IsUnableToPerformDuties)
               .All(a => a.Status == AcceptorStatus.Approved);
    }
}