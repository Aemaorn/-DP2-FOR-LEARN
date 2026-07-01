namespace GHB.DP2.Application.Features.Procurement.Jp006;

using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.Jp006.Abstract;
using GHB.DP2.Application.Features.Procurement.Jp006.Dto;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.PPurchaseOrder;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public class UpdateJp006Endpoint : Jp006EndpointBase<UpdateJp006Request, Results<Ok<Guid>, NotFound<string>, Conflict<string>>>
{
    private readonly Dp2DbContext dbContext;

    public UpdateJp006Endpoint(
        ILogger<AssigneePurchaseOrderEndpoint> logger,
        IOperationService operationService,
        ICommandTextService commandTextService,
        IFileServiceClient fileServiceClient,
        Dp2DbContext dbContext)
        : base(logger, operationService, commandTextService, fileServiceClient, dbContext)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Put("procurement/{ProcurementId:guid}/jp006/{Jp006Id:guid}");
        this.Description(b => b
                              .WithTags(nameof(Jp006))
                              .WithName("UpdatePJp006")
                              .Produces<Created<Guid>>(StatusCodes.Status201Created)
                              .ProducesProblem(StatusCodes.Status400BadRequest)
                              .ProducesProblem(StatusCodes.Status500InternalServerError));
    }

    protected override async ValueTask<Results<Ok<Guid>, NotFound<string>, Conflict<string>>> HandleRequestAsync(UpdateJp006Request req, CancellationToken ct)
    {
        var procurementExisting = await this.ValidateProcurementAsync(req.ProcurementId.Value, ct);

        var jp006 = await this.GetByIdAsync(req.ProcurementId, req.Jp006Id, ct);

        if (jp006 is null)
        {
            return TypedResults.NotFound($"ไม่พบรายการจัดซื้อจัดจ้างที่มีรหัส {req.Jp006Id}");
        }

        this.ValidateDocument(procurementExisting, req, jp006);

        // Update entrepreneurs
        await UpsertEntrepreneursAsync(jp006, req.Entrepreneurs);

        // Update acceptor implementation here
        if (req.Acceptors is not null)
        {
            await this.UpsertAcceptors(jp006, req.Acceptors, procurementExisting.DepartmentId, UserId.From(req.UserId));
        }

        if (req.Assignees is not null)
        {
            var newAssignees = req.Assignees.Where(x => x is { AssigneeType: AssigneeType.Assignee, Id: null });

            foreach (var inComing in newAssignees)
            {
                await SendNotificationAsync(
                    jp006,
                    UserId.From(inComing.UserId),
                    NotificationConstant.Assignment.Title,
                    string.Format(NotificationConstant.Assignment.Message, ProgramConstant.PreProcurementJorPor06.Name, jp006.PurchaseOrderNumber));
            }

            await this.UpsertAssignee(jp006, req.Assignees, CancellationToken.None, UserId.From(req.UserId));
        }

        if (req.Status == PurchaseOrderStatus.WaitingApproval
            || req.DocumentDate is not null)
        {
            jp006.SetDocumentDate(req.DocumentDate);
        }

        // Case 1: การกดบันทึกข้อมูล (Save) โดยไม่เปลี่ยนสถานะ
        if (jp006.Status == req.Status)
        {
            if (!jp006.DocumentHistories.Any())
            {
                var sumAgreePrice = jp006.Entrepreneurs.Where(e => e.IsWinner)
                                        .Sum(e => e.PJp006PriceDetails.Sum(pd => pd.AgreedPrice * pd.ParcelQuantity));

                if (sumAgreePrice > 0)
                {
                    await this.CreateDocumentAsync(procurementExisting, jp006, req.UserId, sumAgreePrice, ct);
                }
            }
            else
            {
                await this.UpdateOrResetDocumentAsync(
                    procurementExisting,
                    jp006,
                    PurchaseOrderDocumentType.Jp006,
                    req.IsJp006DocumentIdReplaced ?? false,
                    req.UserId,
                    ct);

                await this.UpdateOrResetDocumentAsync(
                    procurementExisting,
                    jp006,
                    PurchaseOrderDocumentType.Winner,
                    req.IsWinnerDocumentIdReplaced ?? false,
                    req.UserId,
                    ct);
            }

            jp006.AddActivity(new ActivityInfo(
                ActivityLogActionTypeConstant.Update,
                $"อัปเดตข้อมูลการแจ้งข้อมูลขออนุุมัติสั่งซื้อ/สั่งจ้าง(จพ.006)",
                jp006.Status.ToString()));
        }

        // Case 2: การเปลี่ยนสถานะ
        else
        {
            await this.UpdateStatus(procurementExisting, jp006, req.Status, req.UserId, ct);
        }

        if (req.LastModifiedAt.HasValue && jp006.AuditInfo.LastModifiedAt != req.LastModifiedAt.Value)
        {
            return TypedResults.Conflict("ข้อมูลถูกแก้ไขโดยผู้อื่นแล้ว กรุณาโหลดหน้าใหม่อีกครั้ง");
        }

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok(jp006.Id.Value);
    }

    private void ValidateDocument(Domain.Procurement.Procurement procurement, UpdateJp006Request req, PPurchaseOrder? purchaseOrder)
    {
        if (procurement.IsSixtyAndMoreThanOneHundredThousand)
        {
            return;
        }

        if (req.Status == PurchaseOrderStatus.WaitingCommitteeApproval &&
            (req.Jp006DocumentId is not null || req.WinnerDocumentId is not null) &&
            purchaseOrder != null && !purchaseOrder.IsMigration.GetValueOrDefault(false) && !purchaseOrder.DocumentHistories.Any())
        {
            this.ThrowError("กรุณาจัดทำเอกสาร", StatusCodes.Status400BadRequest);
        }
    }

    internal static Task UpsertEntrepreneursAsync(PPurchaseOrder purchaseOrder, IEnumerable<UpdateJp006Entrepreneur> entrepreneurs)
    {
        var updateJp006Entrepreneurs = entrepreneurs.ToList();

        var requestEntrepreneurs = updateJp006Entrepreneurs
                                   .Select(e => e.MapToEntity(purchaseOrder.Id))
                                   .ToHashSet();

        // Update existing entrepreneurs
        _ = purchaseOrder.Entrepreneurs
                         .Join(
                             requestEntrepreneurs,
                             domainEntrepreneur => domainEntrepreneur.Id,
                             request => request.Id,
                             (domainEntrepreneur, request) =>
                             {
                                 domainEntrepreneur.SetEmailSended(request.EmailSended)
                                                   .SetSequence(request.Sequence)
                                                   .SetCoiResult(request.CoiResult, request.CoiRemark, request.CoiDate)
                                                   .SetWatchlistResult(request.WatchlistResult, request.WatchlistRemark, request.WatchlistDate)
                                                   .SetEgpResult(request.EgpResult, request.EgpRemark, request.EgpDate)
                                                   .SetIsWinner(request.IsWinner)
                                                   .SetSelectionReasonCode(request.SelectionReasonCode)
                                                   .SetRemark(request.Remark);

                                 return domainEntrepreneur;
                             })
                         .ToHashSet();

        foreach (var x in updateJp006Entrepreneurs)
        {
            var entrepreneurData = purchaseOrder.Entrepreneurs.FirstOrDefault(p => p.Id == x.EntrepreneurId);

            if (entrepreneurData is null)
            {
                continue;
            }

            UpdateShareholderList(entrepreneurData, x);

            if (x.CoiCheckerResult is not null)
            {
                entrepreneurData.AddChecker(
                    QualificationType.COI,
                    x.CoiCheckerResult.Result,
                    x.CoiCheckerResult.ResultAt,
                    x.CoiCheckerResult.Remark);
            }

            if (x.WatchlistCheckerResult is not null)
            {
                entrepreneurData.AddChecker(
                    QualificationType.Watchlist,
                    x.WatchlistCheckerResult.Result,
                    x.WatchlistCheckerResult.ResultAt,
                    x.WatchlistCheckerResult.Remark);
            }
        }

        // Add new entrepreneurs (those not found in existing)
        var newEntrepreneurs = requestEntrepreneurs
                               .Where(rpd => purchaseOrder.Entrepreneurs.All(epd => epd.Id != rpd.Id))
                               .ToHashSet();

        foreach (var newEntrepreneur in newEntrepreneurs)
        {
            purchaseOrder.AddEntrepreneur(newEntrepreneur);
        }

        // Handle shareholders and checker results for newly added entrepreneurs (EntrepreneurId was null)
        foreach (var x in updateJp006Entrepreneurs.Where(e => !e.EntrepreneurId.HasValue))
        {
            var entrepreneurData = purchaseOrder.Entrepreneurs.FirstOrDefault(p => p.SuVendorId == x.VendorId);

            if (entrepreneurData is null)
            {
                continue;
            }

            UpdateShareholderList(entrepreneurData, x);

            if (x.CoiCheckerResult is not null)
            {
                entrepreneurData.AddChecker(
                    QualificationType.COI,
                    x.CoiCheckerResult.Result,
                    x.CoiCheckerResult.ResultAt,
                    x.CoiCheckerResult.Remark);
            }

            if (x.WatchlistCheckerResult is not null)
            {
                entrepreneurData.AddChecker(
                    QualificationType.Watchlist,
                    x.WatchlistCheckerResult.Result,
                    x.WatchlistCheckerResult.ResultAt,
                    x.WatchlistCheckerResult.Remark);
            }
        }

        // Remove entrepreneurs that are not in the request
        var entrepreneursToRemove = purchaseOrder.Entrepreneurs
                                                 .Where(epd => requestEntrepreneurs.All(rpd => rpd.Id != epd.Id))
                                                 .ToHashSet();

        foreach (var entrepreneurToRemove in entrepreneursToRemove)
        {
            purchaseOrder.RemoveEntrepreneur(entrepreneurToRemove);
        }

        // Update price details for each entrepreneur
        foreach (var entrepreneurRequest in updateJp006Entrepreneurs)
        {
            var entrepreneur = purchaseOrder.Entrepreneurs
                                            .FirstOrDefault(e => e.SuVendorId == entrepreneurRequest.VendorId);

            if (entrepreneur is not null)
            {
                UpdateJp006PriceDetails(entrepreneur, entrepreneurRequest.PriceDetails);
            }
        }

        return Task.CompletedTask;
    }

    private static void UpdateJp006PriceDetails(PPurchaseOrderEntrepreneur entrepreneur, IEnumerable<UpdateJp006PriceDetails> priceDetailsRequests)
    {
        var requestPriceDetails = priceDetailsRequests
                                  .Select(pd => pd.MapToEntity(entrepreneur.Id))
                                  .ToHashSet();

        // Update existing price details by matching sequence
        _ = entrepreneur.PJp006PriceDetails
                        .Join(
                            requestPriceDetails,
                            existingPd => existingPd.Sequence,
                            requestPd => requestPd.Sequence,
                            (existingPd, requestPd) =>
                            {
                                var detailsInfo = new PPurchaseOrderPriceDetails.PriceDetailsInfo(
                                    requestPd.Sequence,
                                    requestPd.ParcelName,
                                    requestPd.ParcelQuantity,
                                    requestPd.ParcelUnitCode,
                                    requestPd.VatTypeCode,
                                    requestPd.OfferedPrice,
                                    requestPd.AgreedPrice,
                                    requestPd.Description);
                                existingPd.SetDetails(detailsInfo);

                                return existingPd;
                            })
                        .ToHashSet();

        // Add new price details (those with sequences not found in existing)
        var existingSequences = entrepreneur.PJp006PriceDetails
                                            .Select(epd => epd.Sequence)
                                            .ToHashSet();
        var newPriceDetails = requestPriceDetails
                              .Where(rpd => !existingSequences.Contains(rpd.Sequence))
                              .ToHashSet();

        foreach (var newPriceDetail in newPriceDetails)
        {
            entrepreneur.AddPriceDetails(newPriceDetail);
        }

        // Remove price details that are not in the request
        var requestSequences = requestPriceDetails
                               .Select(rpd => rpd.Sequence)
                               .ToHashSet();
        var priceDetailsToRemove = entrepreneur.PJp006PriceDetails
                                               .Where(epd => !requestSequences.Contains(epd.Sequence))
                                               .ToHashSet();

        foreach (var priceDetailToRemove in priceDetailsToRemove)
        {
            entrepreneur.RemovePriceDetails(priceDetailToRemove);
        }
    }

    private async Task UpdateStatus(
        Domain.Procurement.Procurement procurement,
        PPurchaseOrder purchaseOrder,
        PurchaseOrderStatus status,
        Guid userId,
        CancellationToken ct)
    {
        if (purchaseOrder.Status == status)
        {
            return;
        }

        switch (status)
        {
            case PurchaseOrderStatus.WaitingCommitteeApproval:
                purchaseOrder.SetWaitingCommitteeApproval();
                await this.StampWaitingCommitteeApprovalDocument(procurement, purchaseOrder, PurchaseOrderDocumentType.Jp006, userId, ct);
                await this.StampWaitingCommitteeApprovalDocument(procurement, purchaseOrder, PurchaseOrderDocumentType.Winner, userId, ct);

                break;

            case PurchaseOrderStatus.Edit:
                purchaseOrder.SetEdit();
                await this.StampCommitteeAndAssigneeRecallOrReject(procurement, purchaseOrder, ct);

                break;

            case PurchaseOrderStatus.WaitingComment:
                {
                    // Clear assignee comments from previous round
                    foreach (var assignee in purchaseOrder.Assignees.Where(a => a.Type is AssigneeType.Assignee or AssigneeType.Director))
                    {
                        assignee.ResetAction();

                        if (assignee.Type == AssigneeType.Assignee)
                        {
                            assignee.Pending();
                        }

                        if (assignee.Type == AssigneeType.Director)
                        {
                            assignee.ResetAction()
                                    .Draft();
                        }
                    }

                    purchaseOrder.SetAssigned();

                    _ = SendNotificationAssigneeAsync(purchaseOrder, CancellationToken.None);

                    await this.StampCheckPointLastedDocument(procurement, purchaseOrder, PurchaseOrderDocumentType.Jp006, ct);
                    await this.StampCheckPointLastedDocument(procurement, purchaseOrder, PurchaseOrderDocumentType.Winner, ct);

                    break;
                }

            case PurchaseOrderStatus.WaitingApproval:
                purchaseOrder.SetWaitingAcceptor();

                break;
        }

        if (purchaseOrder.Status == PurchaseOrderStatus.WaitingCommitteeApproval)
        {
            EnsureInitialCommitteeCurrents(purchaseOrder);
        }
    }

    private static void EnsureInitialCommitteeCurrents(PPurchaseOrder entity)
    {
        if (entity.Status != PurchaseOrderStatus.WaitingCommitteeApproval)
        {
            return;
        }

        var committee = entity.Acceptors?
                              .Where(a => a.Type == AcceptorType.ProcurementCommittee && a.IsActive && !a.IsUnableToPerformDuties && a.Status == AcceptorStatus.Pending)
                              .ToList();

        if (committee == null || committee.Count == 0)
        {
            return;
        }

        // if any committee (non-chair) already approved do not reset currents
        if (entity.Acceptors!.Any(a => a.Type == AcceptorType.ProcurementCommittee && a.Status == AcceptorStatus.Approved))
        {
            return; // progress already started
        }

        var chairman = committee.FirstOrDefault(a => a.CommitteePositionsCode != null && a.CommitteePositionsCode == ParameterCode.From("PosBoard001"))
                       ?? committee.FirstOrDefault(a => a.IsBoardChairman());

        foreach (var a in committee)
        {
            a.SetCurrent(false);
        }

        var nonChair = committee.Where(a => chairman == null || a.Id != chairman.Id).ToList();

        if (nonChair.Count == 0 && chairman != null)
        {
            _ = SendNotificationAsync(
                entity,
                chairman.UserId,
                NotificationConstant.WaitForLike.Title,
                string.Format(NotificationConstant.WaitForLike.Message, ProgramConstant.PreProcurementJorPor06.Name, entity.PurchaseOrderNumber));
            chairman.SetCurrent(); // only chairman

            return;
        }

        foreach (var a in nonChair)
        {
            _ = SendNotificationAsync(
                entity,
                a.UserId,
                NotificationConstant.WaitForLike.Title,
                string.Format(NotificationConstant.WaitForLike.Message, ProgramConstant.PreProcurementJorPor06.Name, entity.PurchaseOrderNumber));
            a.SetCurrent();
        }

        if (chairman != null)
        {
            chairman.SetCurrent(false);
        }
    }

    private static async Task SendNotificationAsync(PPurchaseOrder purchaseOrder, UserId userId, string title, string message)
    {
        await Notification
              .Crate(
                  userId,
                  title,
                  message,
                  NotificationProgram.Procurement)
              .SetReferenceId(purchaseOrder.Id.Value)
              .SetLinkUrl(string.Format(ProgramConstant.Procurement.Url, purchaseOrder.Procurement.Id), "ดูรายละเอียด")
              .PublishAsync(CancellationToken.None);
    }

    private static async Task SendNotificationAssigneeAsync(PPurchaseOrder jp06, CancellationToken ct)
    {
        foreach (var targetUserId in jp06.Assignees.SelectMany(a => a.GetAssigneeNotificationTargets()))
        {
            await Notification
                  .Crate(
                      targetUserId,
                      NotificationConstant.Comment.Title,
                      string.Format(NotificationConstant.Comment.Message, ProgramConstant.PreProcurementJorPor06.Name, jp06.PurchaseOrderNumber),
                      NotificationProgram.Procurement)
                  .SetReferenceId(jp06.Id.Value)
                  .SetLinkUrl(
                      string.Format(ProgramConstant.Procurement.Url, jp06.Procurement.Id),
                      "ดูรายละเอียด")
                  .PublishAsync(ct);
        }
    }

    private static void UpdateShareholderList(PPurchaseOrderEntrepreneur purchaseEntrepreneurs, UpdateJp006Entrepreneur req)
    {
        if (req.Shareholder == null || req.Shareholder.Length == 0)
        {
            var all = purchaseEntrepreneurs.PurchaseOrderShareholders.ToList();

            foreach (var shareholder in all)
            {
                purchaseEntrepreneurs.RemovePurchaseOrderEntrepreneursShareholder(shareholder.Id);
            }

            return;
        }

        var allKnownIds = req.Shareholder
            .SelectMany(s => new[] { s.CoiId, s.WatchlistId })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToHashSet();

        var toRemove = purchaseEntrepreneurs.PurchaseOrderShareholders
            .Where(a => !allKnownIds.Contains(a.Id.Value))
            .ToList();

        foreach (var shareholder in toRemove)
        {
            purchaseEntrepreneurs.RemovePurchaseOrderEntrepreneursShareholder(shareholder.Id);
        }

        foreach (var s in req.Shareholder)
        {
            var processTypes = s.CheckType != null
                ? new[] { s.CheckType }
                : new[] { "COI", "Watchlist" };

            foreach (var checkType in processTypes)
            {
                var id = checkType == "COI" ? s.CoiId : s.WatchlistId;
                var existing = id.HasValue
                    ? purchaseEntrepreneurs.PurchaseOrderShareholders
                        .FirstOrDefault(a => a.Id == PPurchaseOrderEntrepreneurShareholdersId.From(id.Value))
                    : null;

                if (existing == null)
                {
                    CreateNewShareholder(purchaseEntrepreneurs, s, checkType);
                }
                else
                {
                    UpdateExistingShareholder(purchaseEntrepreneurs, existing, s);
                }
            }
        }
    }

    private static void UpdateExistingShareholder(PPurchaseOrderEntrepreneur purchaseEntrepreneurs, PPurchaseOrderEntrepreneurShareholders existing, UpdateShareholderDto s)
    {
        existing.Update(
                    s.Sequence,
                    s.TaxId,
                    s.FirstName,
                    s.LastName,
                    s.IsDirector,
                    s.IsShareholder,
                    s.IsJuristic)
                .SetWatchlist(s.WatchlistResult, s.WatchlistResultRemark, s.WatchlistResultAt)
                .SetCoi(s.CoiResult, s.CoiResultRemark, s.CoiResultAt)
                .SetEgp(s.EgpResult, s.EgpRemark, s.EgpResultAt);

        if (s.CoiCheckerResult is not null)
        {
            existing.AddChecker(
                QualificationType.COI,
                s.CoiCheckerResult.Result,
                s.CoiCheckerResult.ResultAt,
                s.CoiCheckerResult.Remark);
        }

        if (s.WatchlistCheckerResult is not null)
        {
            existing.AddChecker(
                QualificationType.Watchlist,
                s.WatchlistCheckerResult.Result,
                s.WatchlistCheckerResult.ResultAt,
                s.WatchlistCheckerResult.Remark);
        }

        purchaseEntrepreneurs.UpdatePurchaseOrderEntrepreneursShareholder(existing);
    }

    private static void CreateNewShareholder(PPurchaseOrderEntrepreneur purchaseEntrepreneurs, UpdateShareholderDto s, string checkType)
    {
        var newShareholder =
            PPurchaseOrderEntrepreneurShareholders
                .Create(
                    s.Sequence,
                    s.TaxId,
                    s.FirstName,
                    s.LastName,
                    s.IsDirector,
                    s.IsShareholder,
                    s.IsJuristic)
                .SetCheckType(checkType)
                .SetWatchlist(s.WatchlistResult, s.WatchlistResultRemark, s.WatchlistResultAt)
                .SetCoi(s.CoiResult, s.CoiResultRemark, s.CoiResultAt)
                .SetEgp(s.EgpResult, s.EgpRemark, s.EgpResultAt);

        if (s.CoiCheckerResult is not null)
        {
            newShareholder.AddChecker(
                QualificationType.COI,
                s.CoiCheckerResult.Result,
                s.CoiCheckerResult.ResultAt,
                s.CoiCheckerResult.Remark);
        }

        if (s.WatchlistCheckerResult is not null)
        {
            newShareholder.AddChecker(
                QualificationType.Watchlist,
                s.WatchlistCheckerResult.Result,
                s.WatchlistCheckerResult.ResultAt,
                s.WatchlistCheckerResult.Remark);
        }

        purchaseEntrepreneurs.AddPurchaseOrderEntrepreneurShareholder(newShareholder);
    }
}