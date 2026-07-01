namespace GHB.DP2.Application.Features.Procurement.PurchaseOrderApproval;

using System.IdentityModel.Tokens.Jwt;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.ContractAgreement.ContractInvitation;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Constants;
using GHB.DP2.Domain.ContractAgreement.CaContractInvitation;
using GHB.DP2.Domain.ContractAgreement.Event;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PPurchaseOrderApproval;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record ApprovePurchaseOrderApprovalRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid ProcurementId,
    Guid PurchaseOrderApprovalId,
    string? Remark);

public class ApproveEndpoint : EndpointBase<ApprovePurchaseOrderApprovalRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public ApproveEndpoint(
        Dp2DbContext dbContext,
        ILogger<ApproveEndpoint> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("Procurement/PurchaseOrderApproval")
             .WithName("ApprovePurchaseOrderApproval")
             .Accepts<ApprovePurchaseOrderApprovalRequest>("application/json"));
        this.Post("procurement/{procurementId:guid}/purchase-order-approval/{PurchaseOrderApprovalId:guid}/approve");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(ApprovePurchaseOrderApprovalRequest req, CancellationToken ct)
    {
        var entity = await this.dbContext.PPurchaseOrderApprovals
                               .Include(p => p.Procurement)
                               .Include(x => x.Acceptors)
                               .ThenInclude(x => x.User)
                               .ThenInclude(x => x.Employee)
                               .Include(a => a.Assignees)
                               .ThenInclude(x => x.User)
                               .ThenInclude(x => x.Employee)
                               .AsSplitQuery()
                               .FirstOrDefaultAsync(x => x.Id == PurchaseOrderApprovalId.From(req.PurchaseOrderApprovalId) && x.ProcurementId == ProcurementId.From(req.ProcurementId), ct);

        if (entity == null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลอนุมัติใบสั่งซื้อ/จ้าง/เช่า และแจ้งทำสัญญา");
        }

        var acceptors = entity.Acceptors
                              .Where(a => a.IsActive)
                              .Map(DelegatorExtensions.DelegatorToAcceptor)
                              .OrderBy(a => a.Sequence)
                              .ToList();

        var current = acceptors.FirstOrDefault(a => a.Status == AcceptorStatus.Pending
                                                    && a.Delegatee?.SuUserId == null
            ? a.UserId == req.UserId
            : a.Delegatee?.SuUserId == UserId.From(req.UserId));

        if (current == null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลผู้อนุมัติในรายการนี้");
        }

        var currentAcceptorUser =
            entity.Acceptors
                  .FirstOrDefault(a => a.Id == current.Id);

        if (currentAcceptorUser == null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลผู้อนุมัติในรายการนี้");
        }

        if (!IsPreviousApproved(acceptors, current))
        {
            return TypedResults.BadRequest("ต้องได้รับการอนุมัติจากลำดับก่อนหน้าก่อน");
        }

        currentAcceptorUser
            .SetDelegatee(current.DelegateeId)
            .Approve(req.Remark);
        current.SetCurrent(false);

        UpdateSequentialCurrents(entity, AcceptorType.Approver);

        if (ShouldUpdateStatus([.. entity.Acceptors]))
        {
            entity.SetApproved();

            if (entity.Status == PurchaseOrderApprovalStatus.WaitingAssign && entity.Assignees.Where(x => x.Type == AssigneeType.Director).Any())
            {
                await SendNotificationAssigneesAsync(entity, ct);
            }
        }

        entity.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Approved,
            $"ผู้มีอำนาจเห็นชอบ/อนุมัติ",
            entity.Status.ToString(),
            req.Remark));

        this.dbContext.PPurchaseOrderApprovals.Update(entity);

        await this.dbContext.SaveChangesAsync(ct);

        if (entity is { ContractType: PurchaseOrderApprovalContractType.Contract40, Status: PurchaseOrderApprovalStatus.Assigned })
        {
            await this.HandleCreateBypass(ProcurementId.From(req.ProcurementId), ct);
        }

        return TypedResults.Ok();
    }

    private static bool IsPreviousApproved(List<PPurchaseOrderApprovalAcceptor> acceptors, PPurchaseOrderApprovalAcceptor current)
    {
        if (current.Sequence <= 1)
        {
            return true;
        }

        var prev = acceptors.LastOrDefault(a => a.Sequence < current.Sequence && a.IsActive);

        return prev == null || prev.Status == AcceptorStatus.Approved;
    }

    private static bool ShouldUpdateStatus(List<PPurchaseOrderApprovalAcceptor> acceptors)
    {
        return acceptors
            .All(a => a.Status == AcceptorStatus.Approved);
    }

    private static void UpdateSequentialCurrents(PPurchaseOrderApproval purchaseOrderApproval, AcceptorType type)
    {
        var approvers = purchaseOrderApproval.Acceptors
                                             .Where(a => a.Type == type && a.IsActive)
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

        if (next != null)
        {
            var pendingOfType = approvers.Where(a => a.Status == AcceptorStatus.Pending).ToList();
            var isLastPending = pendingOfType.Count == 1;

            if (purchaseOrderApproval.Status == PurchaseOrderApprovalStatus.WaitingApproval && next.Status == AcceptorStatus.Pending)
            {
                next.SetCurrent(true);
            }

            var programName = purchaseOrderApproval.Procurement.Type == ProcurementType.Rent
                ? ProgramConstant.BranchSpaceRent.Name
                : ProgramConstant.ProcurementPurchaseOrderApproval.Name;

            if (next.Type == AcceptorType.Approver && !isLastPending)
            {
                foreach (var targetUserId in next.GetNotificationTargets())
                {
                    _ = SendNotificationAsync(
                        purchaseOrderApproval,
                        targetUserId,
                        NotificationConstant.WaitForLike.Title,
                        string.Format(NotificationConstant.WaitForLike.Message, programName, purchaseOrderApproval.Procurement.ProcurementNumber));
                }
            }
            else if (next.Type == AcceptorType.Approver && isLastPending)
            {
                foreach (var targetUserId in next.GetNotificationTargets())
                {
                    _ = SendNotificationAsync(
                        purchaseOrderApproval,
                        targetUserId,
                        NotificationConstant.WaitForApprove.Title,
                        string.Format(NotificationConstant.WaitForApprove.Message, programName, purchaseOrderApproval.Procurement.ProcurementNumber));
                }
            }
        }
    }

    private static async Task SendNotificationAsync(PPurchaseOrderApproval purchaseOrderApproval, UserId userId, string title, string message)
    {
        var notificationProgram = NotificationProgram.Procurement;

        var programUrl = purchaseOrderApproval.Procurement.Type == ProcurementType.Rent
            ? ProgramConstant.BranchSpaceRent.Url
            : ProgramConstant.Procurement.Url;

        await Notification
              .Crate(
                  userId,
                  title,
                  message,
                  notificationProgram)
              .SetReferenceId(purchaseOrderApproval.Id.Value)
              .SetLinkUrl(string.Format(programUrl, purchaseOrderApproval.Procurement.Id), "ดูรายละเอียด")
              .PublishAsync(CancellationToken.None);
    }

    private static async Task SendNotificationAssigneesAsync(PPurchaseOrderApproval purchaseOrderApproval, CancellationToken ct)
    {
        var programName = purchaseOrderApproval.Procurement.Type == ProcurementType.Rent
            ? ProgramConstant.BranchSpaceRent.Name
            : ProgramConstant.ProcurementPurchaseOrderApproval.Name;

        var notificationProgram = NotificationProgram.Procurement;

        var programUrl = purchaseOrderApproval.Procurement.Type == ProcurementType.Rent
            ? ProgramConstant.BranchSpaceRent.Url
            : ProgramConstant.Procurement.Url;

        foreach (var targetUserId in purchaseOrderApproval.Assignees.Where(x => x.Type == AssigneeType.Director).SelectMany(a => a.GetAssigneeNotificationTargets()))
        {
            await Notification
                  .Crate(
                      targetUserId,
                      NotificationConstant.Assignment.Title,
                      string.Format(NotificationConstant.Assignment.Message, programName, purchaseOrderApproval.Procurement.ProcurementNumber),
                      notificationProgram)
                  .SetReferenceId(purchaseOrderApproval.Id.Value)
                  .SetLinkUrl(
                      string.Format(programUrl, purchaseOrderApproval.Procurement.Id),
                      "ดูรายละเอียด")
                  .PublishAsync(ct);
        }
    }

    // Create bypass
    private async Task HandleCreateBypass(ProcurementId procurementId, CancellationToken ct)
    {
        var purchaseOrderApprovalExisting =
            await this.dbContext.PPurchaseOrderApprovals
                      .Include(p => p.Procurement)
                      .Include(p => p.Contracts)
                      .ThenInclude(c => c.Entrepreneur)
                      .ThenInclude(e => e!.SuVendor)
                      .Include(c => c.Contracts)
                      .ThenInclude(c => c.PrincipleApprovalRentalEntrepreneurs)
                      .ThenInclude(c => c!.Vendor)
                      .AsSplitQuery()
                      .FirstOrDefaultAsync(
                          p =>
                              p.ProcurementId == procurementId &&
                              p.Status == PurchaseOrderApprovalStatus.Assigned,
                          ct);

        if (purchaseOrderApprovalExisting is null)
        {
            this.ThrowError(
                r =>
                    procurementId,
                $"ไม่พบการอนุมัติใบสั่งซื้อ/จ้าง/เช่า ในระบบ",
                StatusCodes.Status404NotFound);
        }

        var jp006Exisiting = await this.dbContext.PPurchaseOrder
                                       .Include(x => x.Entrepreneurs)
                                       .FirstOrDefaultAsync(x => x.ProcurementId == procurementId, ct);

        var Jp005Exisiting = await this.dbContext.PpPurchaseRequisitions
                                       .Include(x => x.Budgets)
                                       .ThenInclude(x => x.PpPurchaseRequisitionBudgetDetails)
                                       .FirstOrDefaultAsync(x => x.ProcurementId == procurementId, ct);

        var initContractInvitationVendors = MapContractByProcurementType(
            purchaseOrderApprovalExisting,
            purchaseOrderApprovalExisting.Procurement.Type);

        var newContractInvitation =
            CaContractInvitation.Create(procurementId);

        _ = initContractInvitationVendors.Map(this.MapToInvitationVendors)
                                         .Map(newContractInvitation.AddVendor)
                                         .ToHashSet();

        newContractInvitation.SetApproved(raiseCreateDraftEvent: false);

        this.dbContext.CaContractInvitations.Add(newContractInvitation);

        await this.dbContext.SaveChangesAsync(ct);

        await ContractInvitationToDeliveryAcceptanceEvent.Create(newContractInvitation.Id)
                                                         .PublishAsync(cancellation: ct);
    }

    private CaContractInvitationVendors MapToInvitationVendors(GetById.ContractInvitationVendorResponse vendor)
    {
        var newVendor = CaContractInvitationVendors.Create(
            new CaContractInvitationVendors.InvitationVendorInfo(
                vendor.PurchaseOrderApprovalContractId,
                null,
                vendor.Email ?? string.Empty,
                vendor.ContractName ?? string.Empty,
                vendor.PoNumber ?? string.Empty,
                vendor.ContractNumber ?? string.Empty,
                0,
                false,
                vendor.ContractGuaranteePercent,
                vendor.GuaranteeAmount,
                vendor.ContractOfficerName ?? string.Empty,
                vendor.ContractOfficerPhone ?? string.Empty,
                vendor.ContractOfficerEmail ?? string.Empty,
                vendor.EgpResult,
                vendor.EgpRemark,
                vendor.EgpDate,
                vendor.CoiResult,
                vendor.CoiRemark,
                vendor.CoiDate,
                null,
                null,
                null,
                null));

        if (vendor.CoiCheckerResult is not null)
        {
            newVendor.AddChecker(
                QualificationType.COI,
                vendor.CoiCheckerResult.Result,
                vendor.CoiCheckerResult.ResultAt,
                vendor.CoiCheckerResult.Remark);
        }

        if (vendor.WatchlistCheckerResult is not null)
        {
            newVendor.AddChecker(
                QualificationType.Watchlist,
                vendor.WatchlistCheckerResult.Result,
                vendor.WatchlistCheckerResult.ResultAt,
                vendor.WatchlistCheckerResult.Remark);
        }

        if (vendor.Shareholder != null && vendor.Shareholder.Any())
        {
            var shareholders = vendor.Shareholder.Select(s =>
            {
                var newShareholder = CaContractInvitationVendorShareholders
                                     .Create(
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

                return newShareholder;
            }).ToList();

            newVendor.AddCaContractInvitationVendorShareholderList(shareholders);
        }

        return newVendor;
    }

    private static GetById.ContractInvitationVendorResponse[] MapContractByProcurementType(PPurchaseOrderApproval purchaseOrderApprovalExisting, ProcurementType procurementType)
    {
        if (procurementType is ProcurementType.Procurement)
        {
            return
            [
                .. purchaseOrderApprovalExisting
                   .Contracts
                   .OrderBy(o => o.TorDraftBudgetId)
                   .ThenBy(c => c.Sequence)
                   .Select(c =>
                   {
                       var shareholder =
                           c
                               .Entrepreneur?
                               .PurchaseOrderShareholders
                               .Select(s => new GetById.ContractInviteShareholderDTO(
                                   s.Id.Value,
                                   s.Sequence,
                                   s.TaxId,
                                   s.FirstName,
                                   s.LastName,
                                   s.IsDirector,
                                   s.IsShareholder,
                                   s.IsJuristic,
                                   null,
                                   null,
                                   null,
                                   null,
                                   null,
                                   null,
                                   null,
                                   null,
                                   null,
                                   null,
                                   null,
                                   null))
                               .ToArray();

                       return new GetById.ContractInvitationVendorResponse(
                           null,
                           c.Id,
                           null,
                           null,
                           [],
                           c.Entrepreneur != null ? c.Entrepreneur!.SuVendor.EstablishmentName : c.PPurchaseOrderApprovalEntrepreneurs!.Vendor.EstablishmentName,
                           c.Entrepreneur != null ? c.Entrepreneur.SuVendor.Email : c.PPurchaseOrderApprovalEntrepreneurs!.Vendor.Email,
                           purchaseOrderApprovalExisting.Procurement.Name,
                           c.PoNumber,
                           c.ContractNumber,
                           c.AgreedPrice,
                           false,
                           null,
                           null,
                           null,
                           string.Empty,
                           string.Empty,
                           string.Empty,
                           null,
                           null,
                           null,
                           null,
                           null,
                           null,
                           null,
                           null,
                           null,
                           MapToEntrepreneurResponse(c.Entrepreneur != null ? c.Entrepreneur.SuVendor : c.PPurchaseOrderApprovalEntrepreneurs!.Vendor),
                           null,
                           null,
                           shareholder,
                           [],
                           c.Entrepreneur != null ? c.Entrepreneur.SuVendor.Email : c.PPurchaseOrderApprovalEntrepreneurs!.Vendor.Email,
                           null,
                           [],
                           null);
                   })
            ];
        }

        return
        [
            .. purchaseOrderApprovalExisting
               .Contracts
               .OrderBy(o => o.PrincipleApprovalRentalBudgetId)
               .ThenBy(c => c.Sequence)
               .Select(c =>
               {
                   var shareholder =
                       c
                           .Entrepreneur?
                           .PurchaseOrderShareholders
                           .Select(s => new GetById.ContractInviteShareholderDTO(
                               s.Id.Value,
                               s.Sequence,
                               s.TaxId,
                               s.FirstName,
                               s.LastName,
                               s.IsDirector,
                               s.IsShareholder,
                               s.IsJuristic,
                               null,
                               null,
                               null,
                               null,
                               null,
                               null,
                               null,
                               null,
                               null,
                               null,
                               null,
                               null))
                           .ToArray();

                   return new GetById.ContractInvitationVendorResponse(
                       null,
                       c.Id,
                       null,
                       null,
                       [],
                       c.PrincipleApprovalRentalEntrepreneurs!.Vendor.EstablishmentName,
                       c.PrincipleApprovalRentalEntrepreneurs.Vendor.Email,
                       purchaseOrderApprovalExisting.Procurement.Name,
                       c.PoNumber,
                       c.ContractNumber,
                       c.AgreedPrice,
                       false,
                       null,
                       null,
                       null,
                       string.Empty,
                       string.Empty,
                       string.Empty,
                       null,
                       null,
                       null,
                       null,
                       null,
                       null,
                       null,
                       null,
                       null,
                       MapToEntrepreneurResponse(c.PrincipleApprovalRentalEntrepreneurs.Vendor),
                       null,
                       null,
                       shareholder,
                       [],
                       c.PrincipleApprovalRentalEntrepreneurs.Vendor.Email,
                       null,
                       [],
                       null);
               })
        ];
    }

    private static GetById.VendorInfoResponse MapToEntrepreneurResponse(SuVendor vendor)
    {
        return
            new GetById.VendorInfoResponse(
                vendor.Id,
                vendor.Nationality,
                vendor.Type,
                vendor.EntrepreneurType,
                vendor.EntrepreneurTypeInfo.Label,
                vendor.TaxpayerIdentificationNo,
                vendor.EstablishmentName,
                vendor.Tel,
                vendor.Fax,
                vendor.SapVendorNumber,
                vendor.SapBranchNumber,
                vendor.Email);
    }
}