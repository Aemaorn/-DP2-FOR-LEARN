namespace GHB.DP2.Application.Features.Procurement.ChangeCommittee;

using GHB.DP2.Application.Constants;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.Procurement.ChangeCommittee.Abstract;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractManagement.CmDeliveryAcceptance;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.ChangeCommittee;
using GHB.DP2.Domain.Procurement.PInvite;
using GHB.DP2.Domain.Procurement.PJp005;
using GHB.DP2.Domain.Procurement.PpAppoint;
using GHB.DP2.Domain.Procurement.PpMedianPrice;
using GHB.DP2.Domain.Procurement.PpPurchaseRequisition;
using GHB.DP2.Domain.Procurement.PPrincipleApproval;
using GHB.DP2.Domain.Procurement.PPrincipleApprovalRental;
using GHB.DP2.Domain.Procurement.PpTorDraft;
using GHB.DP2.Domain.Procurement.PPurchaseOrder;
using GHB.DP2.Domain.Procurement.PPurchaseOrderApproval;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record ApproveChangeCommitteeRequest(
    Guid ChangeCommitteeId,
    Guid AcceptorId,
    string? Remark);

public class ApproveChangeCommitteeEndpoint : ChangeCommitteeEndpointBase<ApproveChangeCommitteeRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public ApproveChangeCommitteeEndpoint(
        Dp2DbContext dbContext,
        ILogger<ApproveChangeCommitteeEndpoint> logger)
        : base(dbContext, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("ChangeCommittee"));
        this.Post("change-committee/{changeCommitteeId:guid}/approve");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(ApproveChangeCommitteeRequest req, CancellationToken ct)
    {
        var changeCommitteeId = CommitteeChangeId.From(req.ChangeCommitteeId);

        var changeCommittee = await this.dbContext.CommitteeChanges
                                        .Include(c => c.Acceptors)
                                        .ThenInclude(a => a.User)
                                        .Include(c => c.Acceptors)
                                        .ThenInclude(a => a.CommitteePosition)
                                        .Include(c => c.Assignees)
                                        .ThenInclude(a => a.User)
                                        .Include(c => c.Procurement)
                                        .Include(committeeChanges => committeeChanges.NewCommittees)
                                        .FirstOrDefaultAsync(x => x.Id == changeCommitteeId, ct);

        if (changeCommittee == null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลการเปลี่ยนแปลงคณะกรรมการ");
        }

        switch (changeCommittee.Status)
        {
            case CommitteeChangeStatus.WaitingCommitteeApproval:
                {
                    var committeeApproval = this.CommitteeApproval(changeCommittee, req);

                    if (committeeApproval is not null)
                    {
                        return TypedResults.NotFound("ไม่พบผู้อนุมัติในกลุ่มหรือขั้นตอนนี้");
                    }

                    break;
                }

            case CommitteeChangeStatus.WaitingApproval:
                {
                    var approverApproval = this.ApproverApproval(changeCommittee, req);

                    if (approverApproval is not null)
                    {
                        return TypedResults.NotFound("ไม่พบผู้อนุมัติในกลุ่มหรือขั้นตอนนี้");
                    }

                    break;
                }

            default:
                return TypedResults.BadRequest("สถานะไม่รองรับการอนุมัติ");
        }

        var allAcceptors = changeCommittee.Acceptors
                                          .Where(a => a.IsActive)
                                          .OrderBy(a => a.Sequence)
                                          .ToList();

        switch (changeCommittee.Status)
        {
            case CommitteeChangeStatus.WaitingCommitteeApproval:
                {
                    var acceptor = allAcceptors.FirstOrDefault(a => a.Type != AcceptorType.Approver && a.UserId == UserId.From(req.AcceptorId));

                    if (acceptor is null)
                    {
                        break;
                    }

                    if (ShouldUpdateStatus([.. allAcceptors], acceptor))
                    {
                        if (changeCommittee.IsJorPorComment)
                        {
                            changeCommittee.SetStatus(CommitteeChangeStatus.WaitingAssign);

                            var directorAssignee = changeCommittee.Assignees
                                                                   .Select(DelegatorExtensions.DelegatorToAssignee)
                                                                   .FirstOrDefault(x => x.Type == AssigneeType.Director);

                            if (directorAssignee is not null)
                            {
                                foreach (var targetUserId in directorAssignee.GetAssigneeNotificationTargets())
                                {
                                    _ = SendNotificationAsync(
                                        changeCommittee,
                                        targetUserId,
                                        NotificationConstant.WaitForAssignment.Title,
                                        string.Format(NotificationConstant.WaitForAssignment.Message, ProgramConstant.CommitteeChange.Name, string.Empty));
                                }
                            }
                        }
                        else
                        {
                            var approvers = allAcceptors
                                .Where(a => a.Type == AcceptorType.Approver)
                                .OrderBy(a => a.Sequence)
                                .ToList();

                            var firstApprover = approvers.FirstOrDefault();

                            if (firstApprover != null)
                            {
                                changeCommittee.SetStatus(CommitteeChangeStatus.WaitingApproval);
                                firstApprover.Pending();
                                firstApprover.SetCurrent();

                                var isLastApprover = approvers.Count == 1;

                                foreach (var targetUserId in firstApprover.GetNotificationTargets())
                                {
                                    _ = SendNotificationAsync(
                                        changeCommittee,
                                        targetUserId,
                                        isLastApprover ? NotificationConstant.WaitForApprove.Title : NotificationConstant.WaitForLike.Title,
                                        isLastApprover ? string.Format(NotificationConstant.WaitForApprove.Message, ProgramConstant.CommitteeChange.Name, string.Empty) : string.Format(NotificationConstant.WaitForLike.Message, ProgramConstant.CommitteeChange.Name, string.Empty));
                                }
                            }
                        }
                    }
                    else
                    {
                        var nextNonApprover = allAcceptors
                            .Where(a => a.Type != AcceptorType.Approver)
                            .FirstOrDefault(a => a.Status == AcceptorStatus.Pending);

                        nextNonApprover?.SetCurrent();

                        if (nextNonApprover != null)
                        {
                            _ = SendNotificationAsync(
                                changeCommittee,
                                nextNonApprover.UserId,
                                NotificationConstant.WaitForLike.Title,
                                string.Format(NotificationConstant.WaitForLike.Message, ProgramConstant.CommitteeChange.Name, string.Empty));
                        }
                    }

                    break;
                }

            case CommitteeChangeStatus.WaitingApproval:
                {
                    var allApproved = allAcceptors.Where(a => a.Type == AcceptorType.Approver)
                                                  .All(a => a.Status == AcceptorStatus.Approved);

                    if (allApproved)
                    {
                        changeCommittee.SetApproved(req.Remark);

                        _ = SendNotificationAsync(
                            changeCommittee,
                            UserId.From(changeCommittee.AuditInfo.CreatedBy),
                            NotificationConstant.InformCommittee.Title,
                            string.Format(NotificationConstant.InformCommittee.Message, ProgramConstant.CommitteeChange.Name, string.Empty));
                    }
                    else
                    {
                        var nextApprover = allAcceptors
                            .Where(a => a.Type == AcceptorType.Approver)
                            .FirstOrDefault(a => a.Status == AcceptorStatus.Pending);

                        if (nextApprover != null)
                        {
                            nextApprover.SetCurrent();
                            changeCommittee.SetStatus(CommitteeChangeStatus.WaitingApproval);

                            var remainingPending = allAcceptors
                                .Count(a => a.Type == AcceptorType.Approver && a.Status == AcceptorStatus.Pending);
                            var isLastApprover = remainingPending == 1;

                            foreach (var targetUserId in nextApprover.GetNotificationTargets())
                            {
                                _ = SendNotificationAsync(
                                    changeCommittee,
                                    targetUserId,
                                    isLastApprover ? NotificationConstant.WaitForApprove.Title : NotificationConstant.WaitForLike.Title,
                                    isLastApprover ? string.Format(NotificationConstant.WaitForApprove.Message, ProgramConstant.CommitteeChange.Name, string.Empty) : string.Format(NotificationConstant.WaitForLike.Message, ProgramConstant.CommitteeChange.Name, string.Empty));
                            }
                        }
                    }

                    break;
                }
        }

        if (changeCommittee.Status == CommitteeChangeStatus.Approved)
        {
            await this.UpdateOriginalDataBySourceAndTypeCommitteeAsync(
                changeCommittee.ProcurementId,
                changeCommittee.CommitteeType,
                changeCommittee.NewCommittees,
                ct);
        }

        this.dbContext.CommitteeChanges.Update(changeCommittee);

        await this.dbContext.SaveChangesAsync(ct);

        if (changeCommittee.Status == CommitteeChangeStatus.Approved)
        {
            await this.UpdateDeliveryAcceptanceOriginalDataBySourceAndTypeCommitteeAsync(
                changeCommittee.ProcurementId,
                changeCommittee.CommitteeType,
                changeCommittee.NewCommittees,
                ct);

            await this.dbContext.SaveChangesAsync(ct);
        }

        await this.ReplaceDocumentAsync(changeCommittee, ct);

        return TypedResults.Ok();
    }

    private static bool ShouldUpdateStatus(
        List<CommitteeChangeAcceptor> acceptors,
        CommitteeChangeAcceptor current)
    {
        if (current.IsBoardChairman())
        {
            return true;
        }

        return acceptors
               .Where(a =>
                   a.Type == current.Type &&
                   !a.IsUnableToPerformDuties)
               .All(a => a.Status == AcceptorStatus.Approved);
    }

    private static async Task SendNotificationAsync(CommitteeChanges changeCommittee, UserId userId, string title, string message)
    {
        await Notification
              .Crate(
                  userId,
                  title,
                  message,
                  NotificationProgram.Procurement)
              .SetReferenceId(changeCommittee.Id.Value)
              .SetLinkUrl(string.Format(ProgramConstant.CommitteeChange.Url, changeCommittee.Id.Value), ProgramConstant.CommitteeChange.Button)
              .PublishAsync(CancellationToken.None);
    }

    private BadRequest<string>? CommitteeApproval(CommitteeChanges changeCommittee, ApproveChangeCommitteeRequest req)
    {
        var acceptor = changeCommittee.Acceptors
            .Where(a => a.Type != AcceptorType.Approver && a.Status == AcceptorStatus.Pending && a.IsActive && !a.IsUnableToPerformDuties)
            .FirstOrDefault(a => a.UserId == UserId.From(req.AcceptorId));

        if (acceptor is null)
        {
            return TypedResults.BadRequest("ไม่พบข้อมูลผู้อนุมัติคณะกรรมการ");
        }

        acceptor.Approve(req.Remark);
        acceptor.SetCurrent(false);

        return null;
    }

    private BadRequest<string>? ApproverApproval(CommitteeChanges changeCommittee, ApproveChangeCommitteeRequest req)
    {
        var currentAcceptor = changeCommittee.Acceptors
            .Where(a => a.Type == AcceptorType.Approver && a.Status == AcceptorStatus.Pending && a.IsActive)
            .Select(DelegatorExtensions.DelegatorToAcceptor)
            .OrderBy(a => a.Sequence)
            .FirstOrDefault(a => a.Delegatee?.SuUserId == null
                ? a.UserId == UserId.From(req.AcceptorId)
                : a.Delegatee?.SuUserId == UserId.From(req.AcceptorId));

        if (currentAcceptor is null)
        {
            return TypedResults.BadRequest("ไม่พบข้อมูลผู้อนุมัติ");
        }

        var currentAcceptorUser = changeCommittee.Acceptors.First(a => a.Id == currentAcceptor.Id);

        currentAcceptorUser
            .SetDelegatee(currentAcceptor.DelegateeId)
            .Approve(remark: req.Remark);

        currentAcceptorUser.SetCurrent(false);

        return null;
    }

    private async ValueTask ReplaceDocumentAsync(
        CommitteeChanges changeCommittee,
        CancellationToken ct)
    {
        var changeCommitteeWithIncludes = await this.GetChangeCommitteeWithIncludesAsync(changeCommittee.Id, ct);

        if (changeCommitteeWithIncludes is null)
        {
            return;
        }

        var lastedFile = changeCommitteeWithIncludes.LastedNotReplacedCommitteeDocument;

        if (lastedFile is null)
        {
            return;
        }

        var hasAcceptor = changeCommittee.Status is CommitteeChangeStatus.WaitingApproval or CommitteeChangeStatus.Approved;

        var documentService = this.Resolve<IDocumentService>();
        var replaceDto = await this.MapToReplaceDto(changeCommitteeWithIncludes, hasAcceptor, ct);
        var fontName = GetFontName(changeCommitteeWithIncludes);
        var copiedFileId = await documentService.CopyDocumentTemplateAsync(
            lastedFile.FileId,
            contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto, fontName),
            parentDirectory: $"{DocumentTemplateGroups.CommitteeChange}/{changeCommitteeWithIncludes.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
            cancellationToken: ct);

        if (copiedFileId.HasValue)
        {
            changeCommitteeWithIncludes.AddDocumentHistory(copiedFileId.Value, true);
            await this.dbContext.SaveChangesAsync(ct);
        }
    }

    private async ValueTask UpdateOriginalDataBySourceAndTypeCommitteeAsync(
        ProcurementId procurementId,
        CommitteeType committeeType,
        IEnumerable<CommitteeMember> committeeMembers,
        CancellationToken ct)
    {
        switch (committeeType)
        {
            case CommitteeType.TOR:
                await this.UpdateTorCommitteeAsync(procurementId, committeeMembers, ct);

                break;

            case CommitteeType.MedianPrice:
                await this.UpdateMedianPriceCommitteeAsync(procurementId, committeeMembers, ct);

                break;

            case CommitteeType.ProcurementCommittee:
                await this.UpdatePurchaseRequisitionCommitteeAsync(procurementId, Domain.Procurement.PpPurchaseRequisition.GroupType.ProcurementCommittee, committeeMembers, ct);
                await this.UpdateJp005CommitteeAsync(procurementId, PJp005CommitteeGroupType.ProcurementCommittee, committeeMembers, ct);
                await this.UpdateInviteCommitteeAsync(procurementId, committeeMembers, ct);
                await this.UpdateJp006CommitteeAsync(procurementId, committeeMembers, ct);

                break;

            case CommitteeType.InspectionCommittee:
                await this.UpdatePurchaseRequisitionCommitteeAsync(procurementId, Domain.Procurement.PpPurchaseRequisition.GroupType.InspectionCommittee, committeeMembers, ct);
                await this.UpdatePurchaseOrderApprovalCommitteeAsync(procurementId, Domain.Procurement.PPurchaseOrderApproval.GroupType.InspectionCommittee, committeeMembers, ct);
                await this.UpdateJp005CommitteeAsync(procurementId, PJp005CommitteeGroupType.InspectionCommittee, committeeMembers, ct);

                break;

            case CommitteeType.MaintenanceInspectionCommittee:
                await this.UpdatePurchaseRequisitionCommitteeAsync(procurementId, Domain.Procurement.PpPurchaseRequisition.GroupType.MaintenanceInspectionCommittee, committeeMembers, ct);

                break;

            case CommitteeType.ConstructionSupervisor:
                await this.UpdatePurchaseRequisitionCommitteeAsync(procurementId, Domain.Procurement.PpPurchaseRequisition.GroupType.ConstructionSupervisor, committeeMembers, ct);

                break;

            case CommitteeType.RentCommittee:
                await this.UpdatePrincipleApprovalCommitteeAsync(procurementId, CommitteeGroupType.RentCommittee, committeeMembers, ct);
                await this.UpdatePrincipleApprovalRentalCommitteeAsync(procurementId, committeeMembers, ct);

                break;

            case CommitteeType.AcceptanceCommittee:
                await this.UpdatePrincipleApprovalCommitteeAsync(procurementId, CommitteeGroupType.AcceptanceCommittee, committeeMembers, ct);

                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(committeeType),
                    $"Unsupported committeeType: {committeeType}");
        }
    }

    private async ValueTask UpdateDeliveryAcceptanceOriginalDataBySourceAndTypeCommitteeAsync(
       ProcurementId procurementId,
       CommitteeType committeeType,
       IEnumerable<CommitteeMember> committeeMembers,
       CancellationToken ct)
    {
        switch (committeeType)
        {
            case CommitteeType.AcceptanceCommittee:
            case CommitteeType.InspectionCommittee:
                await this.UpdateDeliveryAcceptancePeriodCommitteeAsync(procurementId, committeeMembers, ct);

                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(committeeType),
                    $"Unsupported committeeType: {committeeType}");
        }
    }

    private async ValueTask UpdatePurchaseOrderApprovalCommitteeAsync(
         ProcurementId procurementId,
         Domain.Procurement.PPurchaseOrderApproval.GroupType groupType,
         IEnumerable<CommitteeMember> committeeMembers,
         CancellationToken ct)
    {
        var purchaseOrder = await this.dbContext.PPurchaseOrderApprovals
                              .Where(p => p.ProcurementId == procurementId)
                              .FirstOrDefaultAsync(ct);

        if (purchaseOrder is null)
        {
            return;
        }

        var committees = await this.dbContext.PPurchaseOrderApprovalCommittees
                                   .Where(p => p.PurchaseOrderApprovalId == purchaseOrder.Id && p.GroupType == groupType)
                                   .ToListAsync(ct);

        this.dbContext.PPurchaseOrderApprovalCommittees.RemoveRange(committees);

        var newCommittees = committeeMembers.Select(a =>
                    PPurchaseOrderApprovalCommittee.Create(
           purchaseOrder.Id,
           groupType,
           UserId.From(a.SuUserId),
           a.FullName,
           a.FullPositionName ?? string.Empty,
           ParameterCode.From(a.CommitteePositionsCode),
           a.CommitteePositionsName ?? string.Empty,
           a.Sequence)).ToList();

        await this.dbContext.PPurchaseOrderApprovalCommittees.AddRangeAsync(newCommittees, ct);
    }

    private async ValueTask UpdateTorCommitteeAsync(
        ProcurementId procurementId,
        IEnumerable<CommitteeMember> committeeMembers,
        CancellationToken ct)
    {
        var appoints = await this.dbContext
                                 .PpAppoints
                                 .Include(c => c.TorDraftCommittees)
                                 .Include(p => p.Procurement)
                                 .ThenInclude(r => r.Department)
                                 .Where(r => r.ProcurementId == procurementId && r.IsActive)
                                 .FirstOrDefaultAsync(ct);

        if (appoints == null)
        {
            return;
        }

        var appointTorDraftCommittees = appoints.TorDraftCommittees.ToList();

        this.dbContext.PpAppointTorDraftCommittees.RemoveRange(appointTorDraftCommittees);

        var newAppointTor = committeeMembers.Select(a =>
            PpAppointTorDraftCommittee.Create(
                appoints.Id,
                UserId.From(a.SuUserId),
                a.FullName,
                a.FullPositionName ?? string.Empty,
                ParameterCode.From(a.CommitteePositionsCode),
                a.CommitteePositionsName ?? string.Empty,
                a.Sequence)).ToList();

        await this.dbContext.PpAppointTorDraftCommittees.AddRangeAsync(newAppointTor, ct);

        var allowStatus = new List<TorDraftStatus>
        {
            TorDraftStatus.Draft,
            TorDraftStatus.Edit,
            TorDraftStatus.Rejected,
        };

        var torData = await this.dbContext.PpTorDrafts
                                .Include(r => r.PpTorDraftAcceptors)
                                .Where(r => r.IsActive && r.ProcurementId == procurementId && allowStatus.Contains(r.Status))
                                .FirstOrDefaultAsync(ct);

        if (torData is null)
        {
            return;
        }

        _ = torData.PpTorDraftAcceptors
                   .Where(r => r is { IsActive: true, Type: AcceptorType.TorDraftCommittee })
                   .Iter(r => torData.RemoveAcceptor(r));

        var userIds = committeeMembers.Select(a => UserId.From(a.SuUserId)).ToList();
        var users = await this.dbContext.SuUsers
                              .Include(u => u.Employee)
                              .ThenInclude(e => e.View)
                              .Where(u => userIds.Contains(u.Id))
                              .ToArrayAsync(ct);

        _ =
            committeeMembers.Join(
                                users,
                                a => UserId.From(a.SuUserId),
                                u => u.Id,
                                (a, u) => new { AcceptorDto = a, User = u })
                            .Select(a =>
                                PpTorDraftAcceptors
                                    .Create(
                                        new PpTorDraftAcceptors.AcceptorInfoData(
                                            AcceptorType.TorDraftCommittee,
                                            a.User.Id,
                                            a.User.EmployeeCode,
                                            a.User.FullName,
                                            a.AcceptorDto.CommitteePositionsName,
                                            appoints.Procurement.Department.Name,
                                            a.AcceptorDto.Sequence),
                                        torData.Status)
                                    .SetCommitteePositionsCode(
                                        string.IsNullOrWhiteSpace(a.AcceptorDto.CommitteePositionsCode)
                                            ? null
                                            : ParameterCode.From(a.AcceptorDto.CommitteePositionsCode!)).SetIsUnableToPerformDuties(false))
                            .Iter(r => torData.AddAcceptor(r));
    }

    private async ValueTask UpdateMedianPriceCommitteeAsync(
        ProcurementId procurementId,
        IEnumerable<CommitteeMember> committeeMembers,
        CancellationToken ct)
    {
        var appoints = await this.dbContext
                                 .PpAppoints
                                 .Include(c => c.MedianPriceCommittees)
                                 .Include(p => p.Procurement)
                                 .Where(r => r.ProcurementId == procurementId && r.IsActive)
                                 .FirstOrDefaultAsync(ct);

        if (appoints == null)
        {
            return;
        }

        var appointMdp = appoints.MedianPriceCommittees.ToList();

        this.dbContext.PpAppointMedianPriceCommittees.RemoveRange(appointMdp);

        var newAppointMedianPrice = committeeMembers.Select(a =>
            PpAppointMedianPriceCommittee.Create(
                appoints.Id,
                UserId.From(a.SuUserId),
                a.FullName,
                a.FullPositionName ?? string.Empty,
                ParameterCode.From(a.CommitteePositionsCode),
                a.CommitteePositionsName ?? string.Empty,
                a.Sequence)).ToList();

        await this.dbContext.PpAppointMedianPriceCommittees.AddRangeAsync(newAppointMedianPrice, ct);

        var allowStatus = new List<MedianPriceStatus>
        {
            MedianPriceStatus.Draft,
            MedianPriceStatus.Edit,
            MedianPriceStatus.Rejected,
        };

        var medianPriceData = await this.dbContext.PpMedianPrices
                                        .Include(r => r.Acceptors)
                                        .Include(p => p.Procurement)
                                        .Where(r => r.IsActive && r.ProcurementId == procurementId && allowStatus.Contains(r.Status))
                                        .FirstOrDefaultAsync(ct);

        if (medianPriceData is null)
        {
            return;
        }

        _ = medianPriceData.Acceptors
                           .Where(r => r is { IsActive: true, Type: AcceptorType.MedianPriceCommittee })
                           .Iter(r => medianPriceData.RemoveAcceptor(r));

        var userIds = committeeMembers.Select(a => UserId.From(a.SuUserId)).ToList();
        var users = await this.dbContext.SuUsers
                              .Include(u => u.Employee)
                              .ThenInclude(e => e.View)
                              .Where(u => userIds.Contains(u.Id))
                              .ToArrayAsync(ct);

        _ = committeeMembers.Join(
                                users,
                                a => UserId.From(a.SuUserId),
                                u => u.Id,
                                (a, u) => new { AcceptorDto = a, User = u })
                            .Select(a =>
                                PpMedianPriceAcceptor
                                    .Create(
                                        AcceptorType.MedianPriceCommittee,
                                        a.User,
                                        a.AcceptorDto.Sequence,
                                        appoints.Procurement.DepartmentId)
                                    .SetCommitteePositionsCode(
                                        string.IsNullOrWhiteSpace(a.AcceptorDto.CommitteePositionsCode)
                                            ? null
                                            : ParameterCode.From(a.AcceptorDto.CommitteePositionsCode!))
                                    .SetIsUnableToPerformDuties(false))
                            .Iter(r => medianPriceData.AddAcceptor(r));
    }

    private async ValueTask UpdatePurchaseRequisitionCommitteeAsync(
        ProcurementId procurementId,
        Domain.Procurement.PpPurchaseRequisition.GroupType groupType,
        IEnumerable<CommitteeMember> committeeMembers,
        CancellationToken ct)
    {
        var purchaseRequisition = await this.dbContext.PpPurchaseRequisitions
                                            .Include(r => r.Committees)
                                            .Where(p => p.ProcurementId == procurementId)
                                            .FirstOrDefaultAsync(ct);

        if (purchaseRequisition is null)
        {
            return;
        }

        var committees = await this.dbContext.PpPurchaseRequisitionCommittees
                                   .Where(p => p.PpPurchaseRequisitionId == purchaseRequisition.Id && p.GroupType == groupType)
                                   .ToListAsync(ct);

        this.dbContext.PpPurchaseRequisitionCommittees.RemoveRange(committees);

        var newCommittees = committeeMembers.Select(a =>
            PpPurchaseRequisitionCommittee.Create(
                purchaseRequisition.Id,
                groupType,
                UserId.From(a.SuUserId),
                a.FullName,
                a.FullPositionName ?? string.Empty,
                ParameterCode.From(a.CommitteePositionsCode),
                a.CommitteePositionsName ?? string.Empty,
                a.Sequence)).ToList();

        await this.dbContext.PpPurchaseRequisitionCommittees.AddRangeAsync(newCommittees, ct);
    }

    private async ValueTask UpdateJp005CommitteeAsync(
        ProcurementId procurementId,
        PJp005CommitteeGroupType groupType,
        IEnumerable<CommitteeMember> committeeMembers,
        CancellationToken ct)
    {
        var jp005 = await this.dbContext.PJp005S
                              .Where(p => p.ProcurementId == procurementId && p.IsActive)
                              .FirstOrDefaultAsync(ct);

        if (jp005 is null)
        {
            return;
        }

        var committees = await this.dbContext.PJp005Committees
                                   .Where(p => p.PJp005Id == jp005.Id && p.GroupType == groupType)
                                   .ToListAsync(ct);

        this.dbContext.PJp005Committees.RemoveRange(committees);

        var newCommittees = committeeMembers.Select(a =>
            groupType == PJp005CommitteeGroupType.ProcurementCommittee
                ? PJp005Committee.CreateProcurementCommittee(
                    jp005.Id,
                    UserId.From(a.SuUserId),
                    a.FullName,
                    a.FullPositionName ?? string.Empty,
                    ParameterCode.From(a.CommitteePositionsCode),
                    a.CommitteePositionsName ?? string.Empty,
                    a.Sequence)
                : PJp005Committee.CreateInspectionCommittee(
                    jp005.Id,
                    UserId.From(a.SuUserId),
                    a.FullName,
                    a.FullPositionName ?? string.Empty,
                    ParameterCode.From(a.CommitteePositionsCode),
                    a.CommitteePositionsName ?? string.Empty,
                    a.Sequence)).ToList();

        await this.dbContext.PJp005Committees.AddRangeAsync(newCommittees, ct);
    }

    private async ValueTask UpdateJp006CommitteeAsync(
        ProcurementId procurementId,
        IEnumerable<CommitteeMember> committeeMembers,
        CancellationToken ct)
    {
        var allowStatus = new List<PurchaseOrderStatus>
        {
            PurchaseOrderStatus.Draft,
            PurchaseOrderStatus.Edit,
            PurchaseOrderStatus.Rejected,
        };

        var purchaseOrder = await this.dbContext.PJp006S
                                      .Include(r => r.Acceptors)
                                      .Include(r => r.Procurement)
                                      .Where(p => p.ProcurementId == procurementId && allowStatus.Contains(p.Status))
                                      .FirstOrDefaultAsync(ct);

        if (purchaseOrder is null)
        {
            return;
        }

        _ = purchaseOrder.Acceptors
                         .Where(r => r is { IsActive: true, Type: AcceptorType.ProcurementCommittee })
                         .Iter(r => purchaseOrder.RemoveAcceptor(r));

        var userIds = committeeMembers.Select(a => UserId.From(a.SuUserId)).ToList();
        var users = await this.dbContext.SuUsers
                              .Include(u => u.Employee)
                              .ThenInclude(e => e.View)
                              .Where(u => userIds.Contains(u.Id))
                              .ToArrayAsync(ct);

        _ = committeeMembers.Join(
                                users,
                                a => UserId.From(a.SuUserId),
                                u => u.Id,
                                (a, u) => new { AcceptorDto = a, User = u })
                            .Select(a =>
                                PPurchaseOrderAcceptor
                                    .Create(
                                        AcceptorType.ProcurementCommittee,
                                        a.User,
                                        a.AcceptorDto.Sequence,
                                        purchaseOrder.Procurement.DepartmentId)
                                    .SetCommitteePositionsCode(
                                        string.IsNullOrWhiteSpace(a.AcceptorDto.CommitteePositionsCode)
                                            ? null
                                            : ParameterCode.From(a.AcceptorDto.CommitteePositionsCode!))
                                    .SetIsUnableToPerformDuties(false))
                            .Iter(r => purchaseOrder.AddAcceptor(r));
    }

    private async ValueTask UpdateInviteCommitteeAsync(
        ProcurementId procurementId,
        IEnumerable<CommitteeMember> committeeMembers,
        CancellationToken ct)
    {
        var allowStatus = new List<PInviteStatus>
        {
            PInviteStatus.Draft,
            PInviteStatus.Edit,
            PInviteStatus.Rejected,
        };

        var invite = await this.dbContext.PInvites
                               .Include(r => r.Acceptors)
                               .Include(r => r.Procurement)
                               .ThenInclude(p => p.Department)
                               .Where(p => p.ProcurementId == procurementId && allowStatus.Contains(p.Status))
                               .FirstOrDefaultAsync(ct);

        if (invite is null)
        {
            return;
        }

        _ = invite.Acceptors
                  .Where(r => r is { IsActive: true, Type: AcceptorType.ProcurementCommittee })
                  .Iter(r => invite.RemovePInviteAcceptor(r));

        var userIds = committeeMembers.Select(a => UserId.From(a.SuUserId)).ToList();
        var users = await this.dbContext.SuUsers
                              .Include(u => u.Employee)
                              .ThenInclude(e => e.View)
                              .Where(u => userIds.Contains(u.Id))
                              .ToArrayAsync(ct);

        _ = committeeMembers.Join(
                                users,
                                a => UserId.From(a.SuUserId),
                                u => u.Id,
                                (a, u) => new { AcceptorDto = a, User = u })
                            .Select(a =>
                                PInviteAcceptors
                                    .Create(
                                        new PInviteAcceptors.AcceptorInfoData(
                                            AcceptorType.ProcurementCommittee,
                                            a.User.Id,
                                            a.User.EmployeeCode,
                                            a.User.FullName,
                                            a.AcceptorDto.CommitteePositionsName,
                                            invite.Procurement.Department.Name,
                                            a.AcceptorDto.Sequence),
                                        invite.Status)
                                    .SetCommitteePositionsCode(
                                        string.IsNullOrWhiteSpace(a.AcceptorDto.CommitteePositionsCode)
                                            ? null
                                            : ParameterCode.From(a.AcceptorDto.CommitteePositionsCode!))
                                    .SetIsUnableToPerformDuties(false))
                            .Iter(r => invite.AddPInviteAcceptor(r));
    }

    private async ValueTask UpdatePrincipleApprovalCommitteeAsync(
        ProcurementId procurementId,
        CommitteeGroupType groupType,
        IEnumerable<CommitteeMember> committeeMembers,
        CancellationToken ct)
    {
        var principleApproval = await this.dbContext.PPrincipleApprovals
                                          .Include(r => r.PrincipleApprovalCommittees)
                                          .Where(p => p.ProcurementId == procurementId)
                                          .FirstOrDefaultAsync(ct);

        if (principleApproval is null)
        {
            return;
        }

        _ = principleApproval.PrincipleApprovalCommittees
                             .Where(c => c.GroupType == groupType)
                             .Iter(c => principleApproval.RemoveCommittee(c));

        var userIds = committeeMembers.Select(a => UserId.From(a.SuUserId)).ToList();
        var users = await this.dbContext.SuUsers
                              .Include(u => u.Employee)
                              .ThenInclude(e => e.View)
                              .Where(u => userIds.Contains(u.Id))
                              .ToArrayAsync(ct);

        _ = committeeMembers.Join(
                                users,
                                a => UserId.From(a.SuUserId),
                                u => u.Id,
                                (a, u) => new { AcceptorDto = a, User = u })
                            .Select(a =>
                            {
                                var positionsCode = string.IsNullOrWhiteSpace(a.AcceptorDto.CommitteePositionsCode)
                                    ? ParameterCode.From(string.Empty)
                                    : ParameterCode.From(a.AcceptorDto.CommitteePositionsCode!);

                                return PPrincipleApprovalCommittee.Create(
                                    groupType,
                                    a.User,
                                    positionsCode,
                                    a.AcceptorDto.CommitteePositionsName ?? string.Empty,
                                    a.AcceptorDto.Sequence);
                            })
                            .Iter(c => principleApproval.AddCommittee(c));
    }

    private async ValueTask UpdateDeliveryAcceptancePeriodCommitteeAsync(
       ProcurementId procurementId,
       IEnumerable<CommitteeMember> committeeMembers,
       CancellationToken ct)
    {
        var allowStatus = new List<CmDeliveryAcceptancePeriodStatus>
        {
            CmDeliveryAcceptancePeriodStatus.Draft,
            CmDeliveryAcceptancePeriodStatus.Edit,
            CmDeliveryAcceptancePeriodStatus.Rejected,
        };

        var procurementIdGuid = procurementId.Value;

        var periods = await this.dbContext.CmDeliveryAcceptancePeriods
                                   .Include(r => r.Acceptors)
                                   .Include(r => r.CmDeliveryAcceptance)
                                   .Where(r =>
                                       allowStatus.Contains(r.Status) &&
                                       (
                                           this.dbContext.PPurchaseOrderApprovals.Any(poa =>
                                               r.CmDeliveryAcceptance.RefId == (Guid)poa.Id &&
                                               poa.ProcurementId == procurementId) ||
                                           this.dbContext.CaContractDraftVendors.Any(v =>
                                               r.CmDeliveryAcceptance.RefId == (Guid)v.Id &&
                                               v.ContractDraft.ProcurementId == procurementId) ||
                                           this.dbContext.CaContractDraftVendorEdits.Any(ve =>
                                               r.CmDeliveryAcceptance.RefId == (Guid)ve.Id &&
                                               ve.ProcurementId == procurementIdGuid)
                                       ))
                                   .ToListAsync(ct);

        if (periods.Count == 0)
        {
            return;
        }

        var userIds = committeeMembers.Select(a => UserId.From(a.SuUserId)).ToList();
        var users = await this.dbContext.SuUsers
                              .Include(u => u.Employee)
                              .ThenInclude(e => e.View)
                              .Where(u => userIds.Contains(u.Id))
                              .ToArrayAsync(ct);

        foreach (var period in periods)
        {
            _ = period.Acceptors
                      .Where(r => r is { IsActive: true, Type: AcceptorType.AcceptanceCommittee })
                      .Iter(r => period.RemoveAcceptor(r));

            _ = committeeMembers.Join(
                                      users,
                                      a => UserId.From(a.SuUserId),
                                      u => u.Id,
                                      (a, u) => new { AcceptorDto = a, User = u })
                                      .Select(a =>
                                          CmDeliveryAcceptancePeriodAcceptor
                                              .Create(
                                                  period.Id,
                                                  AcceptorType.AcceptanceCommittee,
                                                  a.User,
                                                  a.AcceptorDto.Sequence,
                                                  period.Status)
                                              .SetCommitteePositionsCode(
                                                  string.IsNullOrWhiteSpace(a.AcceptorDto.CommitteePositionsCode)
                                                      ? null
                                                      : ParameterCode.From(a.AcceptorDto.CommitteePositionsCode!))
                                              .SetIsUnableToPerformDuties(false))
                                      .Iter(r => period.AddAcceptor(r));
        }
    }

    private async ValueTask UpdatePrincipleApprovalRentalCommitteeAsync(
        ProcurementId procurementId,
        IEnumerable<CommitteeMember> committeeMembers,
        CancellationToken ct)
    {
        var allowStatus = new List<PPrincipleApprovalRentalStatus>
        {
            PPrincipleApprovalRentalStatus.Draft,
            PPrincipleApprovalRentalStatus.Edit,
            PPrincipleApprovalRentalStatus.Rejected,
        };

        var principleApprovalRental = await this.dbContext.PPrincipleApprovalRentals
                                                .Include(r => r.Acceptors)
                                                .Where(p => p.ProcurementId == procurementId && allowStatus.Contains(p.Status))
                                                .FirstOrDefaultAsync(ct);

        if (principleApprovalRental is null)
        {
            return;
        }

        _ = principleApprovalRental.Acceptors
                                   .Where(r => r is { IsActive: true, Type: AcceptorType.RentCommittee })
                                   .Iter(r => principleApprovalRental.RemoveAcceptor(r));

        var userIds = committeeMembers.Select(a => UserId.From(a.SuUserId)).ToList();
        var users = await this.dbContext.SuUsers
                              .Include(u => u.Employee)
                              .ThenInclude(e => e.View)
                              .Where(u => userIds.Contains(u.Id))
                              .ToArrayAsync(ct);

        _ = committeeMembers.Join(
                                users,
                                a => UserId.From(a.SuUserId),
                                u => u.Id,
                                (a, u) => new { AcceptorDto = a, User = u })
                            .Select(a =>
                                PPrincipleApprovalRentalAcceptor
                                    .Create(
                                        AcceptorType.RentCommittee,
                                        a.User,
                                        a.AcceptorDto.Sequence,
                                        principleApprovalRental.Status)
                                    .SetCommitteePositionsCode(a.AcceptorDto.CommitteePositionsCode)
                                    .SetIsUnableToPerformDuties(false))
                            .Iter(r => principleApprovalRental.AddAcceptor(r));
    }
}