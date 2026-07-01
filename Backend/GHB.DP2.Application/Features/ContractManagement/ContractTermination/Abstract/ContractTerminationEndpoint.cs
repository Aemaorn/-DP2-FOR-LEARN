namespace GHB.DP2.Application.Features.ContractManagement.ContractTermination.Abstract;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractAgreement.CaContractInvitation;
using GHB.DP2.Domain.ContractManagement.CmContractTermination;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PJp005;
using GHB.DP2.Domain.Procurement.PPrincipleApproval;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public abstract partial class ContractTerminationEndpoint<TRequest, TResponse> : EndpointBase<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : IResult
{
    private readonly Dp2DbContext dbContext;

    protected ContractTerminationEndpoint(
        ILogger logger,
        Dp2DbContext dbContext)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    protected async Task<CaContractDraftVendor> GetByIdAsync(ContractDraftVendorId contractVendorId, CancellationToken ct)
    {
        var entity = await this.BuildContractDraftVendorQuery()
                               .SingleOrDefaultAsync(c => c.Id == contractVendorId, ct);

        if (entity is null)
        {
            this.ThrowError($"ไม่พบข้อมูลสัญญารหัส {contractVendorId}", StatusCodes.Status404NotFound);
        }

        return entity;
    }

    private IQueryable<CaContractDraftVendor> BuildContractDraftVendorQuery()
    {
        return this.dbContext.CaContractDraftVendors
                   .Include(c => c.ContractInvitationVendors)
                   .ThenInclude(iv => iv.PurchaseOrderApprovalContract)
                   .ThenInclude(p => p.Entrepreneur)
                   .ThenInclude(e => e!.SuVendor)
                   .Include(c => c.ContractInvitationVendors)
                   .ThenInclude(iv => iv.PurchaseOrderApprovalContract)
                   .ThenInclude(p => p.PrincipleApprovalRentalEntrepreneurs)
                   .ThenInclude(p => p!.Vendor)
                   .Include(c => c.ContractType)
                   .Include(c => c.Template)
                   .Include(c => c.ContractDraft)
                   .ThenInclude(cd => cd.Procurement)
                   .Include(c => c.CmContractTerminations)
                   .ThenInclude(ct => ct.DocumentHistories)
                   .Include(c => c.CmContractTerminations)
                   .ThenInclude(ct => ct.Assignees)
                   .ThenInclude(a => a.Delegatee)
                   .Include(c => c.CmContractTerminations)
                   .ThenInclude(ct => ct.Acceptors)
                   .ThenInclude(a => a.Delegatee)
                   .Include(c => c.CmContractTerminations)
                   .ThenInclude(ct => ct.Acceptors)
                   .ThenInclude(a => a.CommitteePosition)
                   .Include(ct => ct.CmContractTerminations)
                   .ThenInclude(r => r.TerminateTypeNavigation)
                   .Include(c => c.Delivery)
                   .ThenInclude(d => d.LeadTimeType)
                   .AsSplitQuery();
    }

    protected async Task UpsertAcceptors(CmContractTermination entity, IEnumerable<AcceptorRequest> requests, CmContractTerminationStatus status)
    {
        var requestsList = requests.ToList();
        var requestIds = requestsList.Select(s => s.Id);

        ContractTerminationEndpoint<TRequest, TResponse>.RemoveUnrequestedAcceptors(entity, requestIds);
        await this.AddNewAcceptors(entity, requestsList);
        this.UpdateExistingAcceptors(entity, requestsList, status);
    }

    private static void RemoveUnrequestedAcceptors(CmContractTermination entity, IEnumerable<Guid?> requestIds)
    {
        var acceptorsToRemove = entity.Acceptors.Where(w => !requestIds.Contains(w.Id.Value)).ToList();

        foreach (var acceptor in acceptorsToRemove)
        {
            entity.RemoveAcceptor(acceptor);
        }
    }

    private async Task AddNewAcceptors(CmContractTermination entity, List<AcceptorRequest> requests)
    {
        var newRequests = requests.Where(w => !w.Id.HasValue).ToList();

        if (!newRequests.Any())
        {
            return;
        }

        var userIds = newRequests.Select(r => r.UserId).ToList();
        var users = await this.ValidateUsersAsync(userIds, CancellationToken.None);

        var newAcceptors = newRequests.Join(
            users,
            req => UserId.From(req.UserId),
            usr => UserId.From(usr.Id.Value),
            (req, usr) => CmContractTerminationAcceptor.Create(req.AcceptorType, usr, req.Sequence, CmContractTerminationStatus.Draft));

        foreach (var acceptor in newAcceptors)
        {
            entity.AddAcceptor(acceptor);
        }
    }

    private void UpdateExistingAcceptors(CmContractTermination entity, List<AcceptorRequest> requests, CmContractTerminationStatus status)
    {
        if (entity.Acceptors == null)
        {
            return;
        }

        foreach (var existing in entity.Acceptors.ToList())
        {
            var match = requests.FirstOrDefault(e => e.UserId == existing.UserId && existing.Type == e.AcceptorType);

            if (match == null)
            {
                continue;
            }

            existing.SetIsUnableToPerformDuties(match.IsUnableToPerformDuties ?? false)
                    .SetSequence(match.Sequence);

            this.ProcessAcceptorStatusUpdate(entity, existing, status);
        }
    }

    private void ProcessAcceptorStatusUpdate(CmContractTermination entity, CmContractTerminationAcceptor existing, CmContractTerminationStatus status)
    {
        if (existing.IsUnableToPerformDuties)
        {
            existing.UnableToPerformDuties(existing.Remark);

            return;
        }

        switch (entity.Status)
        {
            case CmContractTerminationStatus.Draft or CmContractTerminationStatus.Rejected when status == CmContractTerminationStatus.WaitingCommitteeApproval:
                existing.Pending();

                break;

            case CmContractTerminationStatus.WaitingComment when status == CmContractTerminationStatus.WaitingApproval:
                ContractTerminationEndpoint<TRequest, TResponse>.HandleWaitingApprovalStatus(entity, existing);

                break;
        }
    }

    private static void HandleWaitingApprovalStatus(CmContractTermination entity, CmContractTerminationAcceptor existing)
    {
        if (existing.Type != AcceptorType.Approver)
        {
            return;
        }

        existing.SetStatus(AcceptorStatus.Pending);

        if (existing.Sequence == 1)
        {
            existing.SetCurrent();
            foreach (var targetUserId in existing.GetNotificationTargets())
            {
                _ = SendNotificationAsync(
                    entity,
                    targetUserId,
                    NotificationConstant.WaitForLike.Title,
                    string.Format(
                        NotificationConstant.WaitForLike.Message,
                        ProgramConstant.DisbursementApproval.Name,
                        entity.CaContractDraftVendor.ContractDraftNumber));
            }
        }
    }

    protected async Task UpsertAssignee(CmContractTermination entity, IEnumerable<AssigneeRequest> requests, CancellationToken cancellationToken = default)
    {
        var requestsList = requests.ToList();
        var requestIds = requestsList.Select(s => s.Id);

        // Remove unrequested assignees
        var assigneesToRemove = entity.Assignees.Where(w => !requestIds.Contains(w.Id.Value)).ToList();

        foreach (var assignee in assigneesToRemove)
        {
            entity.RemoveAssignee(assignee);
        }

        // Add new assignees
        var newRequests = requestsList.Where(w => !w.Id.HasValue).ToList();

        if (newRequests.Any())
        {
            var userIds = newRequests.Select(r => r.UserId).ToList();
            var users = await this.ValidateUsersAsync(userIds, cancellationToken);

            var newAssignees = newRequests.Join(
                users,
                r => r.UserId,
                innerKeySelector: usr => usr.Id.Value,
                (req, usr) => CmContractTerminationAssignee.Create(req.AssigneeGroup, req.AssigneeType, usr, req.Sequence));

            foreach (var assignee in newAssignees)
            {
                entity.AddAssignee(assignee);
            }
        }

        // Update existing assignees
        foreach (var existing in entity.Assignees.ToList())
        {
            var match = requestsList.FirstOrDefault(e => e.UserId == existing.UserId);

            if (match != null)
            {
                existing.SetSequence(match.Sequence);
            }
        }
    }

    private async Task<SuUser[]> ValidateUsersAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken)
    {
        var ids = userIds.Map(UserId.From).ToArray();

        var users = await this.dbContext.SuUsers
                              .Include(u => u.Employee)
                              .ThenInclude(e => e.View)
                              .Where(u => ids.Contains(u.Id))
                              .ToArrayAsync(cancellationToken);

        var missingIds = ids.Except(users.Map(u => u.Id)).ToArray();

        if (missingIds.Length > 0)
        {
            this.ThrowError($"User with ID {string.Join(", ", missingIds)} not found.", StatusCodes.Status404NotFound);
        }

        return users;
    }

    protected async Task<FileId?> UpdateDocumentHistoryAsync(
        CmContractTermination entity,
        FileId fileId,
        bool? isReplace = false,
        CancellationToken ct = default)
    {
        var latestHistory = entity.DocumentHistories
                                  .OrderVersions()
                                  .FirstOrDefault();

        if (latestHistory == null)
        {
            return null;
        }

        var newVersion = RunningDocumentVersion.IncrementDocumentVersion(
            latestHistory.Version,
            latestHistory.StatusState.ToString(),
            entity.Status.ToString());

        var documentService = this.Resolve<IDocumentService>();

        var copiedFileId = await documentService.CopyDocumentTemplateAsync(
            fileId,
            parentDirectory: $"{DocumentTemplateGroups.CMTermination}/{entity.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
            cancellationToken: ct);

        if (!copiedFileId.HasValue)
        {
            return null;
        }

        entity.AddDocumentHistory(copiedFileId.Value, isReplace ?? false);

        var newHistory = entity.DocumentHistories
                               .OrderVersions()
                               .First(h => h.FileId == copiedFileId.Value);
        newHistory.SetVersion(newVersion);

        return copiedFileId.Value;
    }

    protected SuVendor? MapSuVendorByType(CaContractInvitationVendors entity, ProcurementType type)
    {
        return type == ProcurementType.Procurement ? entity.PurchaseOrderApprovalContract.Entrepreneur?.SuVendor : entity.PurchaseOrderApprovalContract.PrincipleApprovalRentalEntrepreneurs?.Vendor;
    }

    protected async Task AddCommitteeAcceptors(ProcurementId procurementId, CmContractTermination entity, CancellationToken ct)
    {
        var procurement = await this.dbContext
                                    .Procurements
                                    .SingleOrDefaultAsync(p => p.Id == procurementId, ct);

        if (procurement is null)
        {
            return;
        }

        var defaultCommitteeQuery = procurement.Type switch
        {
            ProcurementType.Procurement => this.AddInspectionCommitteeAcceptors(procurementId, entity, ct),
            ProcurementType.Rent => this.AddRentalCommitteeAcceptors(procurementId, entity, ct),
            _ => throw new ArgumentOutOfRangeException(),
        };

        await defaultCommitteeQuery;
    }

    private async Task AddRentalCommitteeAcceptors(ProcurementId procurementId, CmContractTermination entity, CancellationToken ct)
    {
        var rentCommittees = await this.dbContext.PPrincipleApprovals
                                       .Where(w => w.ProcurementId == procurementId)
                                       .SelectMany(s => s.PrincipleApprovalCommittees)
                                       .Include(u => u.User)
                                       .Where(w => w.GroupType == CommitteeGroupType.AcceptanceCommittee)
                                       .ToArrayAsync(ct);

        rentCommittees.Iter(r =>
            entity.AddAcceptor(
                CmContractTerminationAcceptor.Create(
                                                 AcceptorType.AcceptanceCommittee,
                                                 r.User,
                                                 r.Sequence,
                                                 entity.Status)
                                             .SetCommitteePositionsCode(r.CommitteePositionsCode)));
    }

    private async Task AddInspectionCommitteeAcceptors(ProcurementId procurementId, CmContractTermination entity, CancellationToken ct)
    {
        var committees = await this.dbContext.PJp005S
                                   .Where(w => w.ProcurementId == procurementId)
                                   .SelectMany(s => s.Committees)
                                   .Where(w => w.GroupType == PJp005CommitteeGroupType.InspectionCommittee)
                                   .Include(pJp005Committee => pJp005Committee.User)
                                   .ToArrayAsync(ct);

        committees.Iter(r =>
            entity.AddAcceptor(
                CmContractTerminationAcceptor.Create(
                                                 AcceptorType.AcceptanceCommittee,
                                                 r.User,
                                                 r.Sequence,
                                                 entity.Status)
                                             .SetCommitteePositionsCode(r.CommitteePositionsCode)));
    }

    protected static async Task SendNotificationAsync(CmContractTermination entity, UserId userId, string title, string message)
    {
        await Notification
              .Crate(
                  userId,
                  title,
                  message,
                  NotificationProgram.ContractManagement)
              .SetReferenceId(entity.Id.Value)
              .SetLinkUrl(string.Format(ProgramConstant.ContractTermination.Url, entity.ContractDraftVendorId, entity.Id), "ดูรายละเอียด")
              .PublishAsync(CancellationToken.None);
    }
}