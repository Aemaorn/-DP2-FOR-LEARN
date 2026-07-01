namespace GHB.DP2.Application.Features.WorkList.AccessHelpers;

using GHB.DP2.Application.Extensions;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractAgreement.CaContractInvitation;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PInvite;
using GHB.DP2.Domain.Procurement.PJp005;
using GHB.DP2.Domain.Procurement.PpMedianPrice;
using GHB.DP2.Domain.Procurement.PpPurchaseRequisition;
using GHB.DP2.Domain.Procurement.PPrincipleApproval;
using GHB.DP2.Domain.Procurement.PPrincipleApprovalRental;
using GHB.DP2.Domain.Procurement.PpTorDraft;
using GHB.DP2.Domain.Procurement.PPurchaseOrder;
using GHB.DP2.Domain.Procurement.PPurchaseOrderApproval;
using System.Linq;
using System.Linq.Expressions;

internal sealed class ProcurementAccessHelper
{
    public Expression<Func<Procurement, bool>> IsPreProcurementAccessible(
        IEnumerable<EffectiveUserId> userIds,
        List<ProcurementId> noPrIds)
    {
        Expression<Func<Procurement, bool>> stepCheck = x => (x.Step == ProcurementStep.PreProcurement || x.Step == ProcurementStep.Procurement);

        return stepCheck
               .And(HasPreProcurementAcceptorRole(userIds)
                        .Or(HasPreProcurementDepartmentDirectorAgreeRole(userIds))
                        .Or(HasPreProcurementCommitteeRole(userIds))
                        .Or(HasCreatedByRole(userIds, noPrIds))
                        .Or(HasPreProcurementAssignmentRole(userIds)));
    }

    public Expression<Func<Procurement, bool>> IsProcurementAccessible(
        IEnumerable<EffectiveUserId> userIds,
        List<ProcurementId> poaIds)
    {
        Expression<Func<Procurement, bool>> stepCheck = x =>
            x.Step == ProcurementStep.PreProcurement || x.Step == ProcurementStep.Procurement;

        return stepCheck
               .And(HasProcurementAcceptorRole(userIds)
                        .Or(this.HasProcurementCommitteeRole(userIds, poaIds))
                        .Or(HasProcurementAssignmentRole(userIds))
                        .Or(HasProcurementDepartmentDirectorAgreeRole(userIds)));
    }

    public Expression<Func<Procurement, bool>> IsContractAgreementAccessible(
        IEnumerable<EffectiveUserId> userIds)
    {
        Expression<Func<Procurement, bool>> stepCheck = x => x.Step == ProcurementStep.ContractAgreement;

        return stepCheck
               .And(HasContractInvitationAcceptorRole(userIds)
                        .Or(HasContractDraftRole(userIds))
                        .Or(HasContractInvitationRole(userIds)));
    }

    // Pre-Procurement Methods
    private static Expression<Func<Procurement, bool>> HasPreProcurementAcceptorRole(IEnumerable<EffectiveUserId> effectiveUserIds)
    {
        var userIds = effectiveUserIds.Select(e => e.UserId);

        return procurement =>
            procurement.Appoints.Any(a =>
                a.Status == Domain.Procurement.PpAppoint.AppointStatus.WaitingApproval &&
                a.Acceptors.Any(ac =>
                    ac.Type == AcceptorType.Approver &&
                    ac.Status == AcceptorStatus.Pending &&
                    ac.Sequence ==
                        a.Acceptors
                            .Where(i => i.Status == AcceptorStatus.Pending)
                            .Min(i => (int?)i.Sequence) &&
                    (
                        userIds.Contains(ac.UserId) ||
                        (ac.Delegatee != null && userIds.Contains(ac.Delegatee.SuUserId))
                    ))) ||
            procurement.TorDrafts.Any(td =>
                td.Status == TorDraftStatus.WaitingApproval &&
                td.PpTorDraftAcceptors.Any(ac =>
                    ac.Type == AcceptorType.Approver &&
                    ac.Status == AcceptorStatus.Pending &&
                    !ac.IsDeleted &&
                    ac.Sequence ==
                        td.PpTorDraftAcceptors
                            .Where(i => !i.IsDeleted && i.Status == AcceptorStatus.Pending)
                            .Min(i => (int?)i.Sequence) &&
                    (
                        userIds.Contains(ac.UserId) ||
                        (ac.Delegatee != null && userIds.Contains(ac.Delegatee.SuUserId))
                    ))) ||
            procurement.MedianPrices.Any(mp =>
                mp.Status == MedianPriceStatus.WaitingApproval &&
                mp.Acceptors.Any(ac =>
                    ac.Type == AcceptorType.Approver &&
                    ac.Status == AcceptorStatus.Pending &&
                    !ac.IsDeleted &&
                    ac.Sequence ==
                        mp.Acceptors
                            .Where(i => !i.IsDeleted && i.Status == AcceptorStatus.Pending)
                            .Min(i => (int?)i.Sequence) &&
                    (
                        userIds.Contains(ac.UserId) ||
                        (ac.Delegatee != null && userIds.Contains(ac.Delegatee.SuUserId))
                    ))) ||
            procurement.PurchaseRequisitions.Any(rq =>
                rq.Status == PurchaseRequisitionStatus.WaitingApproval &&
                rq.Acceptors.Any(ac =>
                    ac.Type == AcceptorType.Approver &&
                    ac.Status == AcceptorStatus.Pending &&
                    !ac.IsDeleted &&
                    ac.Sequence ==
                        rq.Acceptors
                            .Where(i => !i.IsDeleted && i.Status == AcceptorStatus.Pending)
                            .Min(i => (int?)i.Sequence) &&
                    (
                        userIds.Contains(ac.UserId) ||
                        (ac.Delegatee != null && userIds.Contains(ac.Delegatee.SuUserId))
                    ))) ||
            procurement.PurchaseOrderApprovals.Any(poa =>
                !poa.IsDeleted &&
                poa.Status == PurchaseOrderApprovalStatus.WaitingApproval &&
                poa.Acceptors.Any(ac =>
                    !ac.IsDeleted &&
                    ac.Status == AcceptorStatus.Pending &&
                    ac.Sequence ==
                        poa.Acceptors
                            .Where(i => !i.IsDeleted && i.Status == AcceptorStatus.Pending)
                            .Min(i => (int?)i.Sequence) &&
                    (
                        userIds.Contains(ac.UserId) ||
                        (ac.Delegatee != null && userIds.Contains(ac.Delegatee.SuUserId))
                    )));
    }

    private static Expression<Func<Procurement, bool>> HasPreProcurementDepartmentDirectorAgreeRole(IEnumerable<EffectiveUserId> effectiveUserIds)
    {
        var userIds = effectiveUserIds.Select(e => e.UserId);

        return procurement => procurement.TorDrafts.Any(td => td.Status == TorDraftStatus.WaitingUnitApproval &&
                                  td.PpTorDraftAcceptors.Any(ac => !ac.IsDeleted && ac.Type == AcceptorType.DepartmentDirectorAgree &&
                                      ac.Status == AcceptorStatus.Pending &&
                                      (userIds.Contains(ac.UserId) || (ac.Delegatee != null && userIds.Contains(ac.Delegatee.SuUserId))) &&
                                      !td.PpTorDraftAcceptors.Any(b => !b.IsDeleted && b.Type == AcceptorType.DepartmentDirectorAgree && b.Status == AcceptorStatus.Pending && b.Sequence < ac.Sequence))) ||
                              procurement.MedianPrices.Any(mp => !mp.IsDeleted && mp.Status == MedianPriceStatus.WaitingUnitApproval &&
                                  mp.Acceptors.Any(ac => ac.Type == AcceptorType.DepartmentDirectorAgree &&
                                      ac.Status == AcceptorStatus.Pending &&
                                      (userIds.Contains(ac.UserId) || (ac.Delegatee != null && userIds.Contains(ac.Delegatee.SuUserId))) &&
                                      !mp.Acceptors.Any(b => b.Type == AcceptorType.DepartmentDirectorAgree && b.Status == AcceptorStatus.Pending && b.Sequence < ac.Sequence)));
    }

    private static Expression<Func<Procurement, bool>> HasPreProcurementCommitteeRole(IEnumerable<EffectiveUserId> effectiveUserIds)
    {
        var userWithoutDelegateeId = effectiveUserIds
            .Where(e => !e.IsDelegateeUser)
            .Select(e => e.UserId);

        return procurement =>
            (
                procurement.ProcessType == ProcessType.TorDraft
                &&
                (
                    (procurement.Appoints.Any(a => a.TorDraftCommittees.Any(c => userWithoutDelegateeId.Contains(c.SuUserId))) &&
                        procurement.TorDrafts.Any(td =>
                            td.Status == TorDraftStatus.Draft ||
                            td.Status == TorDraftStatus.Rejected ||
                            td.Status == TorDraftStatus.Edit))
                    ||
                    (procurement.Appoints.Any(a => a.TorDraftCommittees.Any(c => userWithoutDelegateeId.Contains(c.SuUserId))) && !procurement.TorDrafts.Any())
                  ||
                  procurement.TorDrafts.Any(td =>
                            td.Status == TorDraftStatus.WaitingCommitteeApproval && td.PpTorDraftAcceptors.Any(ac => ac.Type == AcceptorType.TorDraftCommittee && userWithoutDelegateeId.Contains(ac.UserId)))
                )
            )
            ||
            (
                procurement.ProcessType == ProcessType.MedianPrice
                &&
                (
                    (procurement.Appoints.Any(a => a.MedianPriceCommittees.Any(c => userWithoutDelegateeId.Contains(c.SuUserId))) &&
                        procurement.MedianPrices.Any(td =>
                            td.Status == MedianPriceStatus.Draft ||
                            td.Status == MedianPriceStatus.Rejected ||
                            td.Status == MedianPriceStatus.Edit))
                    ||
                    (procurement.Appoints.Any(a => a.MedianPriceCommittees.Any(c => userWithoutDelegateeId.Contains(c.SuUserId))) && !procurement.MedianPrices.Any())
                  ||
                  procurement.MedianPrices.Any(td =>
                            td.Status == MedianPriceStatus.WaitingCommitteeApproval && td.Acceptors.Any(ac => ac.Type == AcceptorType.MedianPriceCommittee && userWithoutDelegateeId.Contains(ac.UserId)))
                )
            )
            ||
            (
                procurement.ProcessType == ProcessType.PrincipleApprovalRental
                &&
                (
                    (procurement.PrincipleApprovalRentals.Any(a => a.Acceptors.Any(c => userWithoutDelegateeId.Contains(c.UserId))) &&
                        procurement.PrincipleApprovalRentals.Any(td =>
                            td.Status == PPrincipleApprovalRentalStatus.Draft ||
                            td.Status == PPrincipleApprovalRentalStatus.Rejected ||
                            td.Status == PPrincipleApprovalRentalStatus.Edit))
                    ||
                    (procurement.PrincipleApprovalRentals.Any(a => a.Acceptors.Any(c => userWithoutDelegateeId.Contains(c.UserId))) && !procurement.PrincipleApprovalRentals.Any())
                    ||
                    procurement.PrincipleApprovalRentals.Any(td =>
                            td.Status == PPrincipleApprovalRentalStatus.WaitingCommitteeApproval && td.Acceptors.Any(ac => ac.Type == AcceptorType.RentCommittee && userWithoutDelegateeId.Contains(ac.UserId)))
                )
            );
    }

    private static Expression<Func<Procurement, bool>> HasCreatedByRole(IEnumerable<EffectiveUserId> effectiveUserIds, List<ProcurementId> noPrIds)
    {
        var userIds = effectiveUserIds.Select(e => e.UserId.Value);

        return procurement =>
            procurement.Appoints.Any(a =>
                        (a.Status == Domain.Procurement.PpAppoint.AppointStatus.Draft ||
                        a.Status == Domain.Procurement.PpAppoint.AppointStatus.Edit ||
                        a.Status == Domain.Procurement.PpAppoint.AppointStatus.Rejected) &&
                        userIds.Contains(a.AuditInfo.CreatedBy))
            ||
            (noPrIds.Count > 0 && noPrIds.Contains(procurement.Id))
            ||
            procurement.PurchaseRequisitions.Any(pr =>
                (pr.Status == PurchaseRequisitionStatus.Draft ||
                 pr.Status == PurchaseRequisitionStatus.Rejected ||
                 pr.Status == PurchaseRequisitionStatus.Edit) &&
                 userIds.Contains(pr.AuditInfo.CreatedBy))
            ||
            procurement.PurchaseOrderApprovals.Any(a =>
                        (a.Status == PurchaseOrderApprovalStatus.Draft ||
                        a.Status == PurchaseOrderApprovalStatus.Edit ||
                        a.Status == PurchaseOrderApprovalStatus.Rejected) &&
                        userIds.Contains(a.AuditInfo.CreatedBy));
    }

    private static Expression<Func<Procurement, bool>> HasPreProcurementAssignmentRole(IEnumerable<EffectiveUserId> effectiveUserIds)
    {
        var userWithDelegateeId = effectiveUserIds.Select(e => e.UserId);

        return procurement =>
            procurement.TorDrafts.Any(td =>
                    ((td.Status == TorDraftStatus.WaitingAssign) &&
                      !td.Assignees.Any(a => a.Type == AssigneeType.Assignee) &&
                      userWithDelegateeId.Contains(td.Assignees.OrderBy(a => a.Sequence).Last().UserId)
                      )
                    ||
                    ((td.Status == TorDraftStatus.WaitingAssign || td.Status == TorDraftStatus.WaitingComment) &&
                      td.Assignees.Any(a => a.Type == AssigneeType.Assignee) &&
                      userWithDelegateeId.Contains(td.Assignees.OrderBy(a => a.Sequence).Last().UserId))
                    ||
                    (td.Status == TorDraftStatus.RejectToAssignee &&
                      td.Assignees.Any()))
            ||
            procurement.MedianPrices.Any(td =>
                    (
                        td.Status == MedianPriceStatus.WaitingAssign &&
                        !td.Assignees.Any(a => a.Type == AssigneeType.Assignee) &&
                        userWithDelegateeId.Contains(td.Assignees.Where(a => a.Type == AssigneeType.Director).OrderBy(a => a.Sequence).Last().UserId)
                    )
                    ||
                    (
                    (td.Status == MedianPriceStatus.WaitingAssign || td.Status == MedianPriceStatus.WaitingComment) &&
                        td.Assignees.Any(a => a.Type == AssigneeType.Assignee) &&
                        userWithDelegateeId.Contains(td.Assignees.OrderBy(a => a.Sequence).Last().UserId))
                    ||
                    (td.Status == MedianPriceStatus.RejectToAssignee &&
                        td.Assignees.Any()))
            ||
            procurement.PurchaseRequisitions.Any(td =>
                        td.Status == PurchaseRequisitionStatus.WaitingAssign
                        && !td.Assignees.Any(a => a.Type == AssigneeType.Assignee)
                        && td.Assignees.Any(a => userWithDelegateeId.Contains(a.UserId) && a.Type == AssigneeType.Director))
            ||
            procurement.PurchaseRequisitions.Any(td =>
                        td.Status == PurchaseRequisitionStatus.WaitingAssign
                        && td.Assignees.Any(a => a.Type == AssigneeType.Assignee)
                        && td.Assignees.Any(a => userWithDelegateeId.Contains(a.UserId) && a.Type == AssigneeType.Assignee))
            ||
            procurement.PrincipleApprovalRentals.Any(td =>
                    (
                        td.Status == PPrincipleApprovalRentalStatus.WaitingAssign &&
                        !td.Assignees.Any(a => a.Type == AssigneeType.Assignee) &&
                        userWithDelegateeId.Contains(td.Assignees.Where(a => a.Group == AssigneeGroup.JorPor).OrderBy(a => a.Sequence).Last().UserId)
                    )
                    ||
                    (
                        (td.Status == PPrincipleApprovalRentalStatus.WaitingAssign || td.Status == PPrincipleApprovalRentalStatus.WaitingComment) &&
                        td.Assignees.Any(a => a.Type == AssigneeType.Assignee) &&
                        userWithDelegateeId.Contains(td.Assignees.Where(a => a.Type == AssigneeType.Assignee && a.Group == AssigneeGroup.JorPor).OrderBy(a => a.Sequence).Last().UserId))
                    ||
                    (td.Status == PPrincipleApprovalRentalStatus.RejectToAssignee &&
                        td.Assignees.Any())
                    ||
                     (
                        td.Status == PPrincipleApprovalRentalStatus.ContractAssigned &&
                        !td.Assignees.Any(a => a.Type == AssigneeType.Assignee) &&
                        userWithDelegateeId.Contains(td.Assignees.Where(a => a.Group == AssigneeGroup.Contract).OrderBy(a => a.Sequence).Last().UserId)));
    }

    // Procurement Methods
    private static Expression<Func<Procurement, bool>> HasProcurementAcceptorRole(IEnumerable<EffectiveUserId> effectiveUserIds)
    {
        var userIds = effectiveUserIds.Select(e => e.UserId);

        return procurement =>
            procurement.Jp005.Any(mp =>
                mp.Status == PJp005Status.WaitingApproval &&
                mp.Acceptors.Any(ac =>
                    !ac.IsDeleted &&
                    ac.Type == AcceptorType.Approver &&
                    ac.Status == AcceptorStatus.Pending &&
                    ac.Sequence ==
                        mp.Acceptors
                            .Where(i => !i.IsDeleted && i.Status == AcceptorStatus.Pending)
                            .Min(i => (int?)i.Sequence) &&
                    (
                        userIds.Contains(ac.UserId) ||
                        (ac.Delegatee != null && userIds.Contains(ac.Delegatee.SuUserId))
                    )))
            ||
            procurement.Invites.Any(inv =>
                inv.Status == PInviteStatus.WaitingApproval &&
                inv.Acceptors.Any(ac =>
                    !ac.IsDeleted &&
                    ac.Type == AcceptorType.Approver &&
                    ac.Status == AcceptorStatus.Pending &&
                    ac.Sequence ==
                        inv.Acceptors
                            .Where(i => !i.IsDeleted && i.Status == AcceptorStatus.Pending)
                            .Min(i => (int?)i.Sequence) &&
                    (
                        userIds.Contains(ac.UserId) ||
                        (ac.Delegatee != null && userIds.Contains(ac.Delegatee.SuUserId))
                    )))
            ||
            procurement.Invites.Any(inv =>
                inv.Status == PInviteStatus.WaitingApproval &&
                inv.Acceptors.Any(ac =>
                    !ac.IsDeleted &&
                    ac.Type == AcceptorType.ProcurementCommittee &&
                    ac.IsCurrent &&
                    ac.Status == AcceptorStatus.Pending &&
                    (
                        userIds.Contains(ac.UserId) ||
                        (ac.Delegatee != null && userIds.Contains(ac.Delegatee.SuUserId))
                    )))
            ||
            procurement.PurchaseOrder.Any(po =>
                po.Status == PurchaseOrderStatus.WaitingCommitteeApproval &&
                po.Acceptors.Any(ac =>
                    !ac.IsDeleted &&
                    ac.Type == AcceptorType.ProcurementCommittee &&
                    ac.Status == AcceptorStatus.Pending &&
                    (
                        userIds.Contains(ac.UserId) ||
                        (ac.Delegatee != null && userIds.Contains(ac.Delegatee.SuUserId))
                    )))
            ||
            procurement.PurchaseOrder.Any(po =>
                po.Status == PurchaseOrderStatus.WaitingApproval &&
                po.Acceptors.Any(ac =>
                    !ac.IsDeleted &&
                    ac.Type == AcceptorType.Approver &&
                    ac.Status == AcceptorStatus.Pending &&
                    ac.Sequence ==
                        po.Acceptors
                            .Where(i => !i.IsDeleted && i.Status == AcceptorStatus.Pending)
                            .Min(i => (int?)i.Sequence) &&
                    (
                        userIds.Contains(ac.UserId) ||
                        (ac.Delegatee != null && userIds.Contains(ac.Delegatee.SuUserId))
                    )))
            ||
            procurement.PurchaseOrderApprovals.Any(poa =>
                !poa.IsDeleted &&
                poa.Status == PurchaseOrderApprovalStatus.WaitingApproval &&
                poa.Acceptors.Any(ac =>
                    !ac.IsDeleted &&
                    ac.Status == AcceptorStatus.Pending &&
                    ac.Sequence ==
                        poa.Acceptors
                            .Where(i => !i.IsDeleted && i.Status == AcceptorStatus.Pending)
                            .Min(i => (int?)i.Sequence) &&
                    (
                        userIds.Contains(ac.UserId) ||
                        (ac.Delegatee != null && userIds.Contains(ac.Delegatee.SuUserId))
                    )))
            ||
            procurement.PrincipleApprovals.Any(pa =>
                pa.Status == PPrincipleApprovalStatus.WaitingAcceptance &&
                pa.PrincipleApprovalAcceptors.Any(ac =>
                    !ac.IsDeleted &&
                    ac.Type == AcceptorType.Approver &&
                    ac.Status == AcceptorStatus.Pending &&
                    ac.Sequence ==
                        pa.PrincipleApprovalAcceptors
                            .Where(i => !i.IsDeleted && i.Status == AcceptorStatus.Pending)
                            .Min(i => (int?)i.Sequence) &&
                    (
                        userIds.Contains(ac.UserId) ||
                        (ac.Delegatee != null && userIds.Contains(ac.Delegatee.SuUserId))
                    )))
            ||
            procurement.PrincipleApprovalRentals.Any(par =>
                !par.IsDeleted &&
                par.Status == PPrincipleApprovalRentalStatus.WaitingAcceptance &&
                par.Acceptors.Any(ac =>
                    !ac.IsDeleted &&
                    ac.Status == AcceptorStatus.Pending &&
                    ac.Sequence ==
                        par.Acceptors
                            .Where(i => !i.IsDeleted && i.Status == AcceptorStatus.Pending)
                            .Min(i => (int?)i.Sequence) &&
                    (
                        userIds.Contains(ac.UserId) ||
                        (ac.Delegatee != null && userIds.Contains(ac.Delegatee.SuUserId))
                    )));
    }

    private static Expression<Func<Procurement, bool>> HasProcurementDepartmentDirectorAgreeRole(IEnumerable<EffectiveUserId> effectiveUserIds)
    {
        var userWithDelegateeId = effectiveUserIds.Select(e => e.UserId);

        return procurement =>
            procurement.PrincipleApprovals.Any(pa => pa.Status == PPrincipleApprovalStatus.WaitingUnitApproval &&
                pa.PrincipleApprovalAcceptors.Any(ac => !ac.IsDeleted && ac.Type == AcceptorType.DepartmentDirectorAgree &&
                    ac.Status == AcceptorStatus.Pending &&
                    (userWithDelegateeId.Contains(ac.UserId) || (ac.Delegatee != null && userWithDelegateeId.Contains(ac.Delegatee.SuUserId))) &&
                    !pa.PrincipleApprovalAcceptors.Any(b => !b.IsDeleted && b.Type == AcceptorType.DepartmentDirectorAgree && b.Status == AcceptorStatus.Pending && b.Sequence < ac.Sequence))) ||
            procurement.Jp005.Any(j => j.Status == PJp005Status.WaitingApproval &&
                j.Acceptors.Any(ac => !ac.IsDeleted && ac.Type == AcceptorType.DepartmentDirectorAgree &&
                    ac.Status == AcceptorStatus.Pending &&
                    (userWithDelegateeId.Contains(ac.UserId) || (ac.Delegatee != null && userWithDelegateeId.Contains(ac.Delegatee.SuUserId))) &&
                    !j.Acceptors.Any(b => !b.IsDeleted && b.Type == AcceptorType.DepartmentDirectorAgree && b.Status == AcceptorStatus.Pending && b.Sequence < ac.Sequence)));
    }

    private Expression<Func<Procurement, bool>> HasProcurementCommitteeRole(IEnumerable<EffectiveUserId> userIds, List<ProcurementId> poaIds)
    {
        return HasJp005CommitteeRole(userIds)
                   .Or(HasInviteCommitteeOrAssigneeRole(userIds))
                   .Or(HasPurchaseOrderCommitteeOrAssigneeRole(userIds))
                   .Or(HasPurchaseOrderApprovalRole(userIds, poaIds))
                   .Or(HasPrincipleApprovalByCreatorRole(userIds))
                   .Or(HasPrincipleApprovalRentalAssigneeRole(userIds));
    }

    private static Expression<Func<Procurement, bool>> HasJp005CommitteeRole(IEnumerable<EffectiveUserId> effectiveUserIds)
    {
        var userWithDelegateeId = effectiveUserIds.Select(e => e.UserId);

        return p =>
            p.ProcessType == ProcessType.Jp005 &&
               (p.Jp005.Any(j =>
                    j.ProcurementSuppliesDivisions.Any(ps => userWithDelegateeId.Contains(ps.SuUserId)))
               ||
               p.PurchaseRequisitions.Any(pr =>
                    userWithDelegateeId.Contains(pr.Assignees.Where(a => a.Type == AssigneeType.Assignee).OrderBy(a => a.Sequence).Last().UserId)))
                &&
                (
                    !p.Jp005.Any()
                    ||
                    p.Jp005.Any(j => j.Status == PJp005Status.Draft ||
                                    j.Status == PJp005Status.Edit ||
                                    j.Status == PJp005Status.Rejected)
                );
    }

    private static Expression<Func<Procurement, bool>> HasInviteCommitteeOrAssigneeRole(IEnumerable<EffectiveUserId> effectiveUserIds)
    {
        var userWithOutDelegateeId = effectiveUserIds.Where(e => !e.IsDelegateeUser).Select(e => e.UserId);
        var userWithDelegateeId = effectiveUserIds.Select(e => e.UserId);

        return p =>
            p.ProcessType == ProcessType.Invite &&
            (
                p.Jp005.Any(j =>
                    j.Committees.Any(c => userWithOutDelegateeId.Contains(c.SuUserId)))
                ||
                   (p.Jp005.Any(j =>
                        j.ProcurementSuppliesDivisions.Any(ps => userWithDelegateeId.Contains(ps.SuUserId)))
                   || p.PurchaseRequisitions.Any(pr =>
                        userWithDelegateeId.Contains(pr.Assignees.Where(a => a.Type == AssigneeType.Assignee).OrderBy(a => a.Sequence).Last().UserId)))
            )
            &&
            (
                !p.Invites.Any() ||
                p.Invites.Any(i => i.Status == PInviteStatus.Draft ||
                                i.Status == PInviteStatus.Edit ||
                                i.Status == PInviteStatus.Rejected)
            );
    }

    private static Expression<Func<Procurement, bool>> HasPurchaseOrderCommitteeOrAssigneeRole(IEnumerable<EffectiveUserId> effectiveUserIds)
    {
        var userWithOutDelegateeId = effectiveUserIds.Where(e => !e.IsDelegateeUser).Select(e => e.UserId);
        var userWithDelegateeId = effectiveUserIds.Select(e => e.UserId);

        return p =>
            p.ProcessType == ProcessType.PurchaseOrder &&
            (
                p.Jp005.Any(j =>
                    j.Committees.Any(c => userWithOutDelegateeId.Contains(c.SuUserId)))
                ||
                   (p.Jp005.Any(j =>
                        j.ProcurementSuppliesDivisions.Any(ps => userWithDelegateeId.Contains(ps.SuUserId)))
                   || p.PurchaseRequisitions.Any(pr =>
                        userWithDelegateeId.Contains(pr.Assignees.Where(a => a.Type == AssigneeType.Assignee).OrderBy(a => a.Sequence).Last().UserId)))
            )
            &&
            (
                !p.PurchaseOrder.Any() ||
                p.PurchaseOrder.Any(po =>
                    po.Status == PurchaseOrderStatus.Draft ||
                    po.Status == PurchaseOrderStatus.Rejected ||
                    po.Status == PurchaseOrderStatus.Edit)
            );
    }

    private static Expression<Func<Procurement, bool>> HasPurchaseOrderApprovalRole(IEnumerable<EffectiveUserId> effectiveUserIds, List<ProcurementId> poaIds)
    {
        var userWithDelegateeId = effectiveUserIds.Select(e => e.UserId.Value);

        return p =>
            p.ProcessType == ProcessType.PurchaseOrderApproval &&
            (
                (poaIds.Count > 0 && poaIds.Contains(p.Id))
                ||
                (
                 p.PurchaseOrderApprovals.Any(pr => userWithDelegateeId.Contains(pr.AuditInfo.CreatedBy)) &&
                    p.PurchaseOrderApprovals.Any(poa =>
                        poa.Status == PurchaseOrderApprovalStatus.Draft ||
                        poa.Status == PurchaseOrderApprovalStatus.Edit ||
                        poa.Status == PurchaseOrderApprovalStatus.Rejected)
                )
            );
    }

    private static Expression<Func<Procurement, bool>> HasPrincipleApprovalByCreatorRole(IEnumerable<EffectiveUserId> effectiveUserIds)
    {
        var userWithDelegateeId = effectiveUserIds.Select(e => e.UserId.Value);

        return p =>
            p.ProcessType == ProcessType.PrincipleApproval &&
            p.PrincipleApprovals.Any(pa => userWithDelegateeId.Contains(pa.AuditInfo.CreatedBy) &&
                                    (pa.Status == PPrincipleApprovalStatus.Draft ||
                                    pa.Status == PPrincipleApprovalStatus.Rejected ||
                                    pa.Status == PPrincipleApprovalStatus.Edit));
    }

    private static Expression<Func<Procurement, bool>> HasPrincipleApprovalRentalAssigneeRole(IEnumerable<EffectiveUserId> effectiveUserIds)
    {
        var userWithDelegateeId = effectiveUserIds.Select(e => e.UserId);

        return p =>
            p.ProcessType == ProcessType.PrincipleApprovalRental &&
            p.PrincipleApprovals.Any(pa =>
                pa.PrincipleApprovalAssignees.Any(a =>
                    userWithDelegateeId.Contains(a.UserId) && a.Type == AssigneeType.Assignee)) &&
            (
                !p.PrincipleApprovalRentals.Any() ||
                p.PrincipleApprovalRentals.Any(par =>
                    par.Status == PPrincipleApprovalRentalStatus.Draft ||
                    par.Status == PPrincipleApprovalRentalStatus.Rejected ||
                    par.Status == PPrincipleApprovalRentalStatus.Edit)
            );
    }

    private static Expression<Func<Procurement, bool>> HasProcurementAssignmentRole(IEnumerable<EffectiveUserId> userIds)
    {
        var po = PurchaseOrdersPendingForUsers(userIds);
        var pa = PrincipleApprovalsPendingForUsers(userIds);
        var par = PrincipleApprovalRentalsPendingForUsers(userIds);
        var poa = PurchaseOrderApprovalsPendingForUsers(userIds);

        return po.Or(pa).Or(par).Or(poa);
    }

    private static Expression<Func<Procurement, bool>> PurchaseOrdersPendingForUsers(IEnumerable<EffectiveUserId> effectiveUserIds)
    {
        var userWithOutDelegateeId = effectiveUserIds.Where(e => !e.IsDelegateeUser).Select(e => e.UserId);

        return procurement =>
                     procurement.PurchaseOrder.Any(td =>
                                ((td.Status == PurchaseOrderStatus.WaitingAssign) &&
                                  userWithOutDelegateeId.Contains(td.Assignees.OrderBy(a => a.Sequence).Last().UserId))
                                ||
                                ((td.Status == PurchaseOrderStatus.WaitingComment || td.Status == PurchaseOrderStatus.RejectToAssignee) &&
                                  userWithOutDelegateeId.Contains(td.Assignees.Where(a => a.Type == AssigneeType.Assignee).OrderBy(a => a.Sequence).Last().UserId)));
    }

    private static Expression<Func<Procurement, bool>> PrincipleApprovalsPendingForUsers(IEnumerable<EffectiveUserId> effectiveUserIds)
    {
        var userWithOutDelegateeId = effectiveUserIds.Where(e => !e.IsDelegateeUser).Select(e => e.UserId);

        return procurement => procurement.PrincipleApprovals.Any(td =>
                                ((td.Status == PPrincipleApprovalStatus.WaitingAssign) &&
                                  userWithOutDelegateeId.Contains(td.PrincipleApprovalAssignees.OrderBy(a => a.Sequence).Last().UserId))
                                ||
                                ((td.Status == PPrincipleApprovalStatus.WaitingComment || td.Status == PPrincipleApprovalStatus.RejectToAssignee) &&
                                  userWithOutDelegateeId.Contains(td.PrincipleApprovalAssignees.Where(a => a.Type == AssigneeType.Assignee).OrderBy(a => a.Sequence).Last().UserId)));
    }

    private static Expression<Func<Procurement, bool>> PrincipleApprovalRentalsPendingForUsers(IEnumerable<EffectiveUserId> effectiveUserIds)
    {
        var userWithOutDelegateeId = effectiveUserIds.Where(e => !e.IsDelegateeUser).Select(e => e.UserId);

        return procurement => procurement.PrincipleApprovals.Any(td =>
                                ((td.Status == PPrincipleApprovalStatus.WaitingAssign) &&
                                  userWithOutDelegateeId.Contains(td.PrincipleApprovalAssignees.OrderBy(a => a.Sequence).Last().UserId))
                                ||
                                ((td.Status == PPrincipleApprovalStatus.WaitingComment || td.Status == PPrincipleApprovalStatus.RejectToAssignee) &&
                                  userWithOutDelegateeId.Contains(td.PrincipleApprovalAssignees.Where(a => a.Type == AssigneeType.Assignee).OrderBy(a => a.Sequence).Last().UserId)));
    }

    private static Expression<Func<Procurement, bool>> PurchaseOrderApprovalsPendingForUsers(IEnumerable<EffectiveUserId> effectiveUserIds)
    {
        var userWithDelegateeId = effectiveUserIds.Select(e => e.UserId);

        return procurement => procurement.PurchaseOrderApprovals.Any(td =>
                    ((td.Status == PurchaseOrderApprovalStatus.WaitingAssign) &&
                        !td.Assignees.Any(a => a.Type == AssigneeType.Assignee) &&
                        userWithDelegateeId.Contains(td.Assignees.OrderBy(a => a.Sequence).Last().UserId))
                    ||
                    ((td.Status == PurchaseOrderApprovalStatus.WaitingAssign || td.Status == PurchaseOrderApprovalStatus.Assigned) &&
                        !procurement.ContractInvitations.Any() &&
                        userWithDelegateeId.Contains(td.Assignees.Where(a => a.Type == AssigneeType.Assignee).OrderBy(a => a.Sequence).Last().UserId)));
    }

    // Contract Agreement Methods
    private static Expression<Func<Procurement, bool>> HasContractInvitationAcceptorRole(IEnumerable<EffectiveUserId> effectiveUserIds)
    {
        var userWithDelegateeId = effectiveUserIds.Select(e => e.UserId);

        return procurement => procurement.ContractInvitations.Any(ci => ci.Status == ContractInvitationStatus.WaitingApproval &&
            ci.Acceptors.Any(ac => !ac.IsDeleted &&
                ac.Type == AcceptorType.Approver &&
                ac.Status == AcceptorStatus.Pending &&
                ac.Sequence ==
                    ci.Acceptors
                        .Where(i => !i.IsDeleted && i.Status == AcceptorStatus.Pending)
                        .Min(i => (int?)i.Sequence) &&
                (
                    userWithDelegateeId.Contains(ac.UserId) ||
                    (
                        ac.Delegatee != null &&
                        userWithDelegateeId.Contains(ac.Delegatee.SuUserId)
                    )
                )));
    }

    private static Expression<Func<Procurement, bool>> HasContractDraftRole(IEnumerable<EffectiveUserId> effectiveUserIds)
    {
        var userWithDelegateeId = effectiveUserIds.Select(e => e.UserId);

        return procurement =>
            procurement.ProcessType == ProcessType.ContractDraft &&
            (
                (
                    procurement.PurchaseOrderApprovals.Any(a =>
                        a.Assignees.Any(c =>
                            userWithDelegateeId.Contains(c.UserId) && c.Type == AssigneeType.Assignee)) &&
                    procurement.ContractDrafts.Any(cd =>
                        cd.Vendors.Any(v =>
                            v.Status == ContractDraftVendorStatus.Draft ||
                            v.Status == ContractDraftVendorStatus.Edit ||
                            v.Status == ContractDraftVendorStatus.Rejected ||
                            (v.Status == ContractDraftVendorStatus.Approved && v.ContractSignedDate == null)))
                )
                ||
                procurement.ContractDrafts.Any(cd =>
                        cd.Vendors.Any(v =>
                            v.Status == ContractDraftVendorStatus.Pending &&
                            v.Acceptors.Any(ac => !ac.IsDeleted &&
                                ac.Type == AcceptorType.Approver &&
                                ac.Status == AcceptorStatus.Pending &&
                                ac.Sequence ==
                                    v.Acceptors
                                        .Where(i => !i.IsDeleted && i.Status == AcceptorStatus.Pending)
                                        .Min(i => (int?)i.Sequence) &&
                                (
                                    userWithDelegateeId.Contains(ac.UserId) ||
                                    (ac.Delegatee != null && userWithDelegateeId.Contains(ac.Delegatee.SuUserId))
                                ))))
            );
    }

    private static Expression<Func<Procurement, bool>> HasContractInvitationRole(IEnumerable<EffectiveUserId> effectiveUserIds)
    {
        var userWithDelegateeId = effectiveUserIds.Select(e => e.UserId);

        return procurement =>
            procurement.PurchaseOrderApprovals.Any(a =>
                a.Assignees.Any(c =>
                    userWithDelegateeId.Contains(c.UserId) && c.Type == AssigneeType.Assignee)) &&
                procurement.ProcessType == ProcessType.ContractInvitation &&
                (
                    !procurement.ContractInvitations.Any() ||
                    procurement.ContractInvitations.Any(mp =>
                        mp.Status == ContractInvitationStatus.Draft ||
                        mp.Status == ContractInvitationStatus.Edit ||
                        mp.Status == ContractInvitationStatus.Rejected)
                );
    }
}