namespace GHB.DP2.Application.Features.Procurement.PurchaseOrderApproval.Abstract;

using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.Procurement.PurchaseOrderApproval.Dto;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PpPurchaseRequisition;
using GHB.DP2.Domain.Procurement.PPrincipleApprovalRental;
using GHB.DP2.Domain.Procurement.PpTorDraft;
using GHB.DP2.Domain.Procurement.PPurchaseOrder;
using GHB.DP2.Domain.Procurement.PPurchaseOrderApproval;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;

public abstract class PurchaseOrderApprovalEndpointBase<TRequest, TResponse> : EndpointBase<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : IResult
{
    private readonly Dp2DbContext dbContext;

    protected PurchaseOrderApprovalEndpointBase(
        ILogger logger,
        Dp2DbContext dbContext)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    protected async Task<PurchaseOrderApprovalResponseDto> InitialData(Domain.Procurement.Procurement procurement, UserId userId, CancellationToken ct)
    {
        var contractManager = await this.dbContext.RawEmployeePositions
                                        .Include(p => p.Employee)
                                        .ThenInclude(e => e.View)
                                        .Where(p =>
                                            p.BusinessUnitId == BusinessUnitId.From(JorPor.DefaultSectionHead.BusinessUnitId) &&
                                            p.Position.Name == JorPor.DefaultSectionHead.PositionName)
                                        .SelectMany(p => p.Employee.Users)
                                        .FirstOrDefaultAsync(ct);

        if (contractManager?.Employee?.View is null)
        {
            this.ThrowError(
                $"ไม่พบข้อมูลผู้รับผิดชอบสัญญา",
                StatusCodes.Status404NotFound);
        }

        List<AssigneeResponse> assigneeResponses = new List<AssigneeResponse>
        {
            new(
                null,
                AssigneeGroup.Contract,
                AssigneeType.Director,
                contractManager.Id.Value,
                1,
                contractManager.Employee.View.FullName,
                contractManager.Employee.View.FullPositionName,
                contractManager.Employee.View.BusinessUnitName,
                AssigneeStatus.Draft),
        };

        if (procurement.Type is ProcurementType.Rent)
        {
            var principleApprovalRental = await this.dbContext.PPrincipleApprovalRentals
                                                    .Include(x => x.Budgets)
                                                    .Include(pPrincipleApprovalRental => pPrincipleApprovalRental.Assignees)
                                                    .ThenInclude(a => a.User)
                                                    .ThenInclude(a => a.Employee)
                                                    .FirstOrDefaultAsync(x => x.ProcurementId == procurement.Id, ct);

            if (principleApprovalRental is null && !principleApprovalRental!.Budgets.Any())
            {
                this.ThrowError("ไม่พบข้อมูลขออนุมัติเช่าที่เกี่ยวข้องกับจัดซื้อจัดจ้างนี้", StatusCodes.Status404NotFound);
            }

            var hasPermission = principleApprovalRental.Assignees
                                                       .Select(DelegatorExtensions.DelegatorToAssignee)
                                                       .Any(x => !x.IsDeleted && x.Delegatee?.SuUserId == null
                                                                                    ? x.UserId == userId
                                                                                    : x.Delegatee?.SuUserId == userId && x.Group == AssigneeGroup.Contract);

            return new PurchaseOrderApprovalResponseDto(
                null,
                procurement.Id.Value,
                string.Empty,
                PurchaseOrderApprovalStatus.Draft,
                hasPermission,
                [],
                assigneeResponses,
                principleApprovalRental
                    .Budgets
                    .OrderBy(o => o.Sequence)
                    .Select(b => new PurchaseOrderApprovalContractGroupResponseDto(
                        b.Id.Value,
                        b.Sequence,
                        b.Description,
                        b.BudgetAmount,
                        []))
                    .ToList(),
                null,
                null,
                null,
                null,
                null);
        }

        var budgetDataTor = await this.dbContext.PpTorDrafts
                                      .Include(x => x.PpTorDraftBudgets)
                                      .Where(x => x.ProcurementId == procurement.Id && x.IsActive)
                                      .SelectMany(s => s.PpTorDraftBudgets)
                                      .ToListAsync(ct);

        var budgetDataPurchaseRequisition = await this.dbContext.PpPurchaseRequisitions
                                                      .Include(x => x.Budgets)
                                                      .Where(x => x.ProcurementId == procurement.Id)
                                                      .SelectMany(s => s.Budgets)
                                                      .ToListAsync(ct);

        var jp04 = await this.dbContext.PpPurchaseRequisitions
                                .Include(x => x.Assignees)
                                .ThenInclude(a => a.User)
                                .ThenInclude(u => u.Employee)
                                .Where(x => x.ProcurementId == procurement.Id)
                                .FirstOrDefaultAsync(ct);

        var jp04Assignees = jp04?.Assignees
                                 .OrderBy(o => o.Sequence)
                                 .Select(data => new AssigneeResponse(
                                     data.Id.Value,
                                     data.Group,
                                     data.Type,
                                     data.UserId.Value,
                                     data.Sequence,
                                     data.FullName,
                                     data.PositionName,
                                     data.BusinessUnitName,
                                     data.Status,
                                     data.Remark,
                                     data.ActionAt,
                                     data.DelegateeId?.Value));

        if (budgetDataTor.Any())
        {
            return new PurchaseOrderApprovalResponseDto(
                null,
                procurement.Id.Value,
                string.Empty,
                PurchaseOrderApprovalStatus.Draft,
                true,
                [],
                assigneeResponses,
                budgetDataTor
                    .OrderBy(o => o.Sequence)
                    .Select(b => new PurchaseOrderApprovalContractGroupResponseDto(
                        b.Id.Value,
                        b.Sequence ?? 0,
                        b.Description ?? string.Empty,
                        b.BudgetAmount,
                        []))
                    .ToList(),
                jp04 is not null ? (Guid)jp04.Id : null,
                null,
                null,
                null,
                jp04Assignees);
        }

        if (budgetDataPurchaseRequisition.Any())
        {
            return new PurchaseOrderApprovalResponseDto(
            null,
            procurement.Id.Value,
            string.Empty,
            PurchaseOrderApprovalStatus.Draft,
            true,
            [],
            assigneeResponses,
            budgetDataPurchaseRequisition
                .OrderBy(o => o.Sequence)
                .Select(b => new PurchaseOrderApprovalContractGroupResponseDto(
                    b.Id.Value,
                    b.Sequence,
                    b.Description,
                    b.BudgetAmount,
                    []))
                .ToList(),
            jp04 is not null ? (Guid)jp04.Id : null,
            null,
            null,
            null,
            jp04Assignees);
        }

        return new PurchaseOrderApprovalResponseDto(
            null,
            procurement.Id.Value,
            string.Empty,
            PurchaseOrderApprovalStatus.Draft,
            true,
            [],
            assigneeResponses,
            null,
            null,
            null,
            null,
            null,
            jp04Assignees);
    }

    protected async Task<PurchaseOrderApprovalResponseDto?> GetDataById(PurchaseOrderApprovalId id, UserId userId, CancellationToken ct)
    {
        var approval = await this.dbContext.PPurchaseOrderApprovals
                                 .Include(x => x.Acceptors)
                                 .ThenInclude(po => po.User)
                                 .ThenInclude(po => po.Employee)
                                 .Include(x => x.Assignees)
                                 .ThenInclude(po => po.User)
                                 .ThenInclude(po => po.Employee)
                                 .Include(x => x.Committees)
                                 .ThenInclude(po => po.User)
                                 .ThenInclude(po => po.Employee).ThenInclude(rawEmployee => rawEmployee.View)
                                 .Include(x => x.Contracts)
                                 .ThenInclude(c => c.PrincipleApprovalRentalBudget)
                                 .Include(x => x.Contracts)
                                 .ThenInclude(c => c.Budget)
                                 .Include(x => x.Contracts)
                                 .ThenInclude(c => c.Entrepreneur!)
                                 .ThenInclude(e => e.SuVendor)
                                 .Include(x => x.Contracts)
                                 .ThenInclude(c => c.PrincipleApprovalRentalEntrepreneurs!)
                                 .ThenInclude(e => e.Vendor)
                                 .Include(x => x.Contracts)
                                 .ThenInclude(c => c.PPurchaseOrderApprovalEntrepreneurs!)
                                 .ThenInclude(e => e.Vendor)
                                 .Include(x => x.Contracts)
                                 .ThenInclude(c => c.PpPurchaseRequisitionBudget)
                                 .Include(x => x.Contracts)
                                 .ThenInclude(c => c.PPurchaseOrderApprovalBudget)
                                 .Include(x => x.Procurement).ThenInclude(procurement => procurement.PurchaseRequisitions).Include(pPurchaseOrderApproval => pPurchaseOrderApproval.PurchaseOrderApprovalBudget)
                                 .AsSplitQuery()
                                 .SingleOrDefaultAsync(x => x.Id == id, cancellationToken: ct);

        if (approval is null)
        {
            this.ThrowError($"ไม่พบข้อมูลอนุมัติใบสั่งซื้อที่มีรหัส {id.Value}", StatusCodes.Status404NotFound);
        }

        var hasPermission = false;

        if (approval.Procurement.Type is ProcurementType.Rent)
        {
            var assignees = await this.dbContext.PPrincipleApprovalRentals
                                        .Include(x => x.Assignees)
                                        .ThenInclude(a => a.User)
                                        .ThenInclude(a => a.Employee)
                                        .Where(x => x.ProcurementId == approval.ProcurementId)
                                        .SelectMany(s => s.Assignees)
                                        .ToListAsync(cancellationToken: ct);

            hasPermission = assignees.Select(DelegatorExtensions.DelegatorToAssignee)
                                     .Any(w => !w.IsDeleted && w.Delegatee?.SuUserId == null
                                                        ? w.UserId == userId
                                                        : w.Delegatee?.SuUserId == userId && w.Group == AssigneeGroup.Contract);
        }

        var jp04 = await this.dbContext.PpPurchaseRequisitions
                                .Include(x => x.Assignees)
                                .ThenInclude(a => a.User)
                                .ThenInclude(u => u.Employee)
                                .Where(x => x.ProcurementId == approval.ProcurementId)
                                .FirstOrDefaultAsync(ct);

        var jp04Assignees = jp04?.Assignees
                                 .OrderBy(o => o.Sequence)
                                 .Select(DelegatorExtensions.DelegatorToAssignee)
                                 .Select(data => new AssigneeResponse(
                                     data.Id.Value,
                                     data.Group,
                                     data.Type,
                                     data.UserId.Value,
                                     data.Sequence,
                                     data.FullName,
                                     data.PositionName,
                                     data.BusinessUnitName,
                                     data.Status,
                                     data.Remark,
                                     data.ActionAt,
                                     data.DelegateeId?.Value));

        var contracts = approval.Contracts
                                .GroupBy(c =>
                                {
                                    if (approval.Procurement.Type == ProcurementType.Rent && c.PrincipleApprovalRentalBudget is not null)
                                    {
                                        return new ContractGroupKey
                                        {
                                            Id = c.PrincipleApprovalRentalBudget.Id.Value,
                                            Sequence = c.PrincipleApprovalRentalBudget.Sequence,
                                            Description = c.PrincipleApprovalRentalBudget.Description,
                                            BudgetAmount = c.PrincipleApprovalRentalBudget.BudgetAmount,
                                        };
                                    }

                                    if (approval.Procurement.Type == ProcurementType.Procurement && c.Budget is not null)
                                    {
                                        return new ContractGroupKey
                                        {
                                            Id = c.Budget.Id.Value,
                                            Sequence = c.Budget.Sequence ?? 0,
                                            Description = c.Budget.Description ?? string.Empty,
                                            BudgetAmount = c.Budget.BudgetAmount ?? 0,
                                        };
                                    }

                                    if (c.PpPurchaseRequisitionBudget is not null)
                                    {
                                        return new ContractGroupKey
                                        {
                                            Id = c.PpPurchaseRequisitionBudget!.Id.Value,
                                            Sequence = c.PpPurchaseRequisitionBudget.Sequence,
                                            Description = c.PpPurchaseRequisitionBudget.Description,
                                            BudgetAmount = c.PpPurchaseRequisitionBudget.BudgetAmount,
                                        };
                                    }

                                    if (approval.PurchaseOrderApprovalBudget is not null)
                                    {
                                        return new ContractGroupKey
                                        {
                                            Id = c.PPurchaseOrderApprovalBudget!.Id.Value,
                                            Sequence = c.PPurchaseOrderApprovalBudget.Sequence,
                                            Description = c.PPurchaseOrderApprovalBudget.Description,
                                            BudgetAmount = c.PPurchaseOrderApprovalBudget.BudgetAmount,
                                        };
                                    }

                                    return new ContractGroupKey
                                    {
                                        // กรณีสร้างตรง อนุมัติใบสั่งซื้อ/จ้าง/เช่า และแจ้งทำสัญญา
                                    };
                                })
                                .OrderBy(g => g.Key.Sequence)
                                .Select(g => new PurchaseOrderApprovalContractGroupResponseDto(
                                    g.Key.Id,
                                    g.Key.Sequence,
                                    g.Key.Description,
                                    g.Key.BudgetAmount,
                                    g
                                        .OrderBy(o => o.Sequence)
                                        .Select(c =>
                                        {
                                            var isRent = approval.Procurement.Type == ProcurementType.Rent;
                                            var vendor = isRent
                                                ? c.PrincipleApprovalRentalEntrepreneurs?.Vendor
                                                : c.PurchaseOrderEntrepreneurId.HasValue
                                                    ? c.Entrepreneur?.SuVendor
                                                    : c.PPurchaseOrderApprovalEntrepreneurs?.Vendor;

                                            return new PurchaseOrderApprovalContractResponseDto(
                                                c.Id.Value,
                                                c.Sequence,
                                                c.PurchaseOrderEntrepreneurId?.Value,
                                                c.PrincipleApprovalRentalEntrepreneursId?.Value,
                                                c.PPurchaseOrderApprovalEntrepreneursId?.Value,
                                                vendor != null ? $"{vendor.TaxpayerIdentificationNo} : {vendor.EstablishmentName}" : string.Empty,
                                                vendor?.Email ?? string.Empty,
                                                c.ContractNumber,
                                                c.HasEditContractNumber,
                                                c.AgreedPrice,
                                                c.PoNumber,
                                                c.CommitteeType,
                                                vendor != null ? vendor.Id.Value : null);
                                        }).OrderBy(x => x.Sequence))).ToList();

        var approvalBudget = approval.PurchaseOrderApprovalBudget
                                .OrderBy(o => o.Sequence)
                                .Select(b => new PurchaseOrderApprovalContractGroupResponseDto(
                                    b.Id.Value,
                                    b.Sequence,
                                    b.Description,
                                    b.BudgetAmount,
                                    []))
                                .ToList();

        if (approvalBudget.Any() && contracts.Any())
        {
            foreach (var budget in approvalBudget)
            {
                bool exists = contracts.Any(c => c.BudgetId == budget.BudgetId);
                if (!exists)
                {
                    contracts.Add(budget);
                }
            }
        }

        return new PurchaseOrderApprovalResponseDto(
            approval.Id.Value,
            approval.ProcurementId.Value,
            approval.ContractType.ToString(),
            approval.Status,
            approval.Procurement.Type != ProcurementType.Rent || hasPermission,
            approval.Acceptors
                    .OrderBy(x => x.Sequence)
                    .Select(DelegatorExtensions.DelegatorToAcceptor)
                    .Select(a => new AcceptorResponse(
                        a.Id.Value,
                        a.Type,
                        a.UserId.Value,
                        a.Sequence,
                        a.FullName,
                        a.PositionName,
                        a.BusinessUnitName,
                        a.Status,
                        a.Remark,
                        a.ActionAt,
                        IsCurrent: CurrentAcceptor(approval.Acceptors, a.Id.Value, approval.Status),
                        DelegateeUserId: a.Delegatee?.SuUserId.Value)),
            approval.Assignees
                    .Select(DelegatorExtensions.DelegatorToAssignee)
                    .Select(data => new AssigneeResponse(
                        data.Id.Value,
                        data.Group,
                        data.Type,
                        data.UserId.Value,
                        data.Sequence,
                        data.FullName,
                        data.PositionName,
                        data.BusinessUnitName,
                        data.Status,
                        data.Remark,
                        data.ActionAt,
                        data.Delegatee?.SuUserId.Value))
                    .OrderBy(o => o.Sequence),
            contracts.Any() ? contracts : approvalBudget,
            jp04 is not null ? (Guid)jp04.Id : null,
            approval.Procurement.Budget,
            approval.Committees.Where(w => w.GroupType is Domain.Procurement.PPurchaseOrderApproval.GroupType.InspectionCommittee)
                                .All(a => a.IsCommittee()),
            approval.Committees
                               .OrderBy(o => o.Sequence)
                               .Select(c =>
                                   new PurchaseOrderApprovalCommittee(
                                       c.Id.Value,
                                       c.GroupType,
                                       c.SuUserId.Value,
                                       c.FullName,
                                       c.FullPositionName,
                                       c.CommitteePositionsCode.Value,
                                       c.CommitteePositionsName,
                                       c.Sequence,
                                       c.User.Employee.PrimaryDepartment?.Id.Value ?? string.Empty)),
            jp04Assignees);
    }

    protected async Task UpdateCommittees(PPurchaseOrderApproval requisition, IEnumerable<PurchaseOrderApprovalCommittee> purchaseOrderApprovalCommittee, CancellationToken ct)
    {
        // Delete removed committees
        var incomingIds = purchaseOrderApprovalCommittee
                          .Where(c => c.Id.HasValue)
                          .Select(c => PPurchaseOrderApprovalCommitteeId.From(c.Id.Value))
                          .ToList();

        var existingCommittees = await this.dbContext.PPurchaseOrderApprovalCommittees
                                           .Where(c => c.PurchaseOrderApprovalId == requisition.Id)
                                           .ToListAsync(ct);

        var committeesToDelete = existingCommittees
                                 .Where(existing => !incomingIds.Contains(existing.Id))
                                 .ToList();

        this.dbContext.PPurchaseOrderApprovalCommittees.RemoveRange(committeesToDelete);

        foreach (var committee in purchaseOrderApprovalCommittee)
        {
            await this.CreateOrUpdateCommittee(requisition, committee, ct);
        }
    }

    private async Task CreateOrUpdateCommittee(
        PPurchaseOrderApproval requisition,
        PurchaseOrderApprovalCommittee committeeDto,
        CancellationToken ct)
    {
        var committeePost = await this.dbContext.SuParameters
                                      .Where(w => w.Code == ParameterCode.From(committeeDto.CommitteePositionsCode))
                                      .FirstOrDefaultAsync(ct);

        if (committeePost is null)
        {
            this.ThrowError("ไม่พบตำแหน่งในคณะกรรมการในระบ", StatusCodes.Status404NotFound);
        }

        if (committeeDto.Id.HasValue)
        {
            var existingCommittee = await this.dbContext.PPurchaseOrderApprovalCommittees
                                              .FirstOrDefaultAsync(c => c.Id == PPurchaseOrderApprovalCommitteeId.From(committeeDto.Id.Value), ct);

            if (existingCommittee == null)
            {
                this.ThrowError($"Committee with ID {committeeDto.Id} not found.", StatusCodes.Status404NotFound);
            }

            existingCommittee.Update(
                committeePost.Code,
                committeePost.Label,
                committeeDto.Sequence);

            return;
        }

        var newCommittee = PPurchaseOrderApprovalCommittee.Create(
            requisition.Id,
            committeeDto.GroupType,
            UserId.From(committeeDto.SuUserId),
            committeeDto.FullName,
            committeeDto.FullPositionName,
            committeePost.Code,
            committeePost.Label,
            committeeDto.Sequence);

        this.dbContext.PPurchaseOrderApprovalCommittees.Add(newCommittee);
        requisition.AddPPurchaseOrderApprovalCommittee(newCommittee);
    }

    protected async Task UpsertAcceptors(PPurchaseOrderApproval entity, AcceptorRequest[] requests, BusinessUnitId workBusinessUnitId, UserId? sendToAcceptorId = null)
    {
        if (entity.Status == PurchaseOrderApprovalStatus.WaitingAssign)
        {
            return;
        }

        var lastAssigneeUserId = entity.Assignees
            .Where(a => a.Type == AssigneeType.Assignee)
            .OrderByDescending(a => a.Sequence)
            .Select(a => (UserId?)a.UserId)
            .FirstOrDefault();

        var resolvedSendToAcceptorId = lastAssigneeUserId ?? sendToAcceptorId;

        _ = entity.Acceptors.Where(w => !requests.Select(s => s.Id).Contains(w.Id.Value))
                  .Iter(s => entity.RemoveAcceptor(s));

        var userIds = requests.Select(r => r.UserId).ToList();
        var users = await this.ValidateUsersAsync(userIds, CancellationToken.None);

        _ = requests.Where(w => !w.Id.HasValue).Join(
                        users,
                        req => UserId.From(req.UserId),
                        usr => UserId.From(usr.Id.Value),
                        (req, usr) => PPurchaseOrderApprovalAcceptor.Create(req.AcceptorType, usr, req.Sequence, workBusinessUnitId))
                    .Iter(r =>
                    {
                        r.SetSendToAcceptorId(resolvedSendToAcceptorId);
                        entity.AddAcceptor(r);
                    });

        foreach (var existing in entity.Acceptors.ToList())
        {
            var match = requests.FirstOrDefault(e => e.UserId == existing.UserId && existing.Type == e.AcceptorType);

            if (match != null)
            {
                existing.SetSequence(match.Sequence);
                existing.SetSendToAcceptorId(resolvedSendToAcceptorId);
            }
        }
    }

    protected async Task UpsertAssignee(PPurchaseOrderApproval entity, IEnumerable<AssigneeRequest> requests, CancellationToken cancellationToken = default, UserId? sendToAcceptorId = null)
    {
        var lastAssigneeUserId = requests
            .Where(a => a.AssigneeType == AssigneeType.Assignee)
            .OrderByDescending(a => a.Sequence)
            .Select(a => (UserId?)UserId.From(a.UserId))
            .FirstOrDefault();

        var resolvedSendToAcceptorId = lastAssigneeUserId ?? sendToAcceptorId;

        _ = entity.Assignees.Where(w => !requests.Select(s => s.Id).Contains(w.Id.Value))
                  .Iter(s => entity.RemoveAssignee(s));

        var userIds = requests.Select(r => r.UserId).ToList();
        var users = await this.ValidateUsersAsync(userIds, cancellationToken);

        _ = requests.Where(w => !w.Id.HasValue).Join(
                        users,
                        r => r.UserId,
                        innerKeySelector: usr => usr.Id.Value,
                        (req, usr) => PPurchaseOrderApprovalAssignee.Create(req.AssigneeGroup, req.AssigneeType, usr, req.Sequence))
                    .Iter(r =>
                    {
                        r.SetSendToAcceptorId(resolvedSendToAcceptorId);
                        entity.AddAssignee(r);
                    });

        // Update existing
        foreach (var existing in entity.Assignees.ToList())
        {
            var match = requests.FirstOrDefault(e => e.UserId == existing.UserId);

            if (match != null)
            {
                existing.SetSequence(match.Sequence);
                existing.SetSendToAcceptorId(resolvedSendToAcceptorId);
            }
        }
    }

    private async Task<Guid> GetVendorAsync(PurchaseOrderApprovalEntrepreneursDto dto, PPurchaseOrderApproval purchaseOrderApproval, CancellationToken ct)
    {
        var vendor = await this.dbContext.SuVendors
            .FirstOrDefaultAsync(v => v.Id == SuVendorId.From(dto.VendorId), ct);

        if (vendor is null)
        {
            throw new InvalidOperationException($"ไม่พบข้อมูลผู้ขายที่มีรหัส {dto.VendorId}");
        }

        var entity = PPurchaseOrderApprovalEntrepreneurs.Create(
               purchaseOrderApproval,
               vendor,
               dto.Sequence,
               dto.EmailSend);

        this.dbContext.PPurchaseOrderApprovalEntrepreneurs.Add(entity);
        await this.dbContext.SaveChangesAsync(ct);

        return entity.Id.Value;
    }

    protected void UpsertContract(Domain.Procurement.Procurement procurement, PPurchaseOrderApproval entity, IEnumerable<PurchaseOrderApprovalContractDto> requests)
    {
        var newEntities = requests.Select(dto =>
        {
            PPurchaseOrderApprovalContract? existing;

            if (procurement.Type == ProcurementType.Rent)
            {
                existing = entity.Contracts.FirstOrDefault(c =>
                    c.PrincipleApprovalRentalBudgetId == PPrincipleApprovalRentalBudgetId.From(dto.PrincipleApprovalRentalBudgetId.Value)
                    && c.PrincipleApprovalRentalEntrepreneursId == PPrincipleApprovalRentalEntrepreneursId.From(dto.PrincipleApprovalRentalEntrepreneursId.Value));

                if (existing != null)
                {
                    existing.Update(dto.Sequence)
                            .SetContractInformation(
                                dto.ContractNumber,
                                dto.HasEditContractNumber,
                                dto.AgreedPrice,
                                dto.PoNumber,
                                dto.CommitteeType);

                    return existing;
                }

                return PPurchaseOrderApprovalContract.Create(
                                                               dto.Sequence,
                                                               new PPurchaseOrderApprovalContract.ContractInfoData(
                                                                   dto.ContractNumber,
                                                                   dto.HasEditContractNumber,
                                                                   dto.AgreedPrice,
                                                                   dto.PoNumber,
                                                                   dto.CommitteeType))
                                                           .SetPrincipleApprovalRentalData(
                                                               PPrincipleApprovalRentalBudgetId.From(dto.PrincipleApprovalRentalBudgetId.Value),
                                                               PPrincipleApprovalRentalEntrepreneursId.From(dto.PrincipleApprovalRentalEntrepreneursId.Value));
            }
            else
            {
                existing = entity.Contracts.FirstOrDefault(c =>
                    c.TorDraftBudgetId == PpTorDraftBudgetId.From(dto.TorDraftBudgetId.Value)
                    && c.PurchaseOrderEntrepreneurId == PurchaseOrderEntrepreneurId.From(dto.PurchaseOrderEntrepreneurId.Value));

                if (existing is null && dto.TorDraftBudgetId is not null && dto.PurchaseOrderApprovalEntrepreneursId is not null)
                {
                    existing = entity.Contracts.FirstOrDefault(c =>
                      c.PPurchaseOrderApprovalBudgetId == PPurchaseOrderApprovalBudgetId.From(dto.TorDraftBudgetId.Value)
                      && c.PPurchaseOrderApprovalEntrepreneursId == PPurchaseOrderApprovalEntrepreneursId.From(dto.PurchaseOrderApprovalEntrepreneursId.Value));
                }

                if (existing != null)
                {
                    existing.Update(dto.Sequence)
                            .SetContractInformation(
                                dto.ContractNumber,
                                dto.HasEditContractNumber,
                                dto.AgreedPrice,
                                dto.PoNumber,
                                dto.CommitteeType);

                    return existing;
                }

                if (procurement.TorDrafts != null && procurement.TorDrafts.Count > 0)
                {
                    return PPurchaseOrderApprovalContract.Create(
                                                                   dto.Sequence,
                                                                   new PPurchaseOrderApprovalContract.ContractInfoData(
                                                                       dto.ContractNumber,
                                                                       dto.HasEditContractNumber,
                                                                       dto.AgreedPrice,
                                                                       dto.PoNumber,
                                                                       dto.CommitteeType))
                                                               .SetPurchaseOrderData(
                                                                   PpTorDraftBudgetId.From(dto.TorDraftBudgetId.Value),
                                                                   PurchaseOrderEntrepreneurId.From(dto.PurchaseOrderEntrepreneurId.Value));
                }

                if (procurement.Invites != null && procurement.Invites.Count > 0)
                {
                    return PPurchaseOrderApprovalContract.Create(
                                                                  dto.Sequence,
                                                                  new PPurchaseOrderApprovalContract.ContractInfoData(
                                                                      dto.ContractNumber,
                                                                      dto.HasEditContractNumber,
                                                                      dto.AgreedPrice,
                                                                      dto.PoNumber,
                                                                      dto.CommitteeType))
                                                              .SetPurchasePurchaseRequisitionData(
                                                                  PpPurchaseRequisitionBudgetId.From(dto.TorDraftBudgetId.Value),
                                                                  PurchaseOrderEntrepreneurId.From(dto.PurchaseOrderEntrepreneurId.Value));
                }

                var entrepreneursId = Guid.Empty;

                if (dto.Entrepreneurs is not null)
                {
                    entrepreneursId = this.GetVendorAsync(dto.Entrepreneurs, entity, CancellationToken.None).Result;
                }

                return PPurchaseOrderApprovalContract.Create(
                                                                  dto.Sequence,
                                                                  new PPurchaseOrderApprovalContract.ContractInfoData(
                                                                      dto.ContractNumber,
                                                                      dto.HasEditContractNumber,
                                                                      dto.AgreedPrice,
                                                                      dto.PoNumber,
                                                                      dto.CommitteeType))
                                                              .SetPPurchaseOrderApprovalData(
                                                                  PPurchaseOrderApprovalBudgetId.From(dto.PurchaseOrderApprovalBudgetId.Value),
                                                                  PPurchaseOrderApprovalEntrepreneursId.From(entrepreneursId));
            }
        }).ToList();

        // Add new
        foreach (var toAdd in newEntities.Where(e => entity.Contracts.All(a => !a.Id.Equals(e.Id))))
        {
            entity.AddContractor(toAdd);
        }

        // Remove obsolete
        foreach (var toRemove in entity.Contracts.Where(a => !newEntities.Any(e => e.Id.Equals(a.Id))).ToList())
        {
            entity.RemoveContractor(toRemove);

            if (toRemove.PPurchaseOrderApprovalEntrepreneurs != null)
            {
                entity.RemoveEntrepreneur(toRemove.PPurchaseOrderApprovalEntrepreneurs);
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

    private static bool CurrentAcceptor(IEnumerable<PPurchaseOrderApprovalAcceptor> acceptors, Guid acceptorId, PurchaseOrderApprovalStatus status)
    {
        if (status is
            PurchaseOrderApprovalStatus.Draft or
            PurchaseOrderApprovalStatus.Rejected or
            PurchaseOrderApprovalStatus.WaitingAssign)
        {
            return false;
        }

        var current = acceptors.FirstOrDefault(a => a.Id.Value == acceptorId);

        if (current == null)
        {
            return false;
        }

        var prev = acceptors
                   .Where(a => a.Sequence < current.Sequence)
                   .OrderByDescending(a => a.Sequence)
                   .FirstOrDefault();

        // If this is the first in sequence, only return true if the current is not approved
        if (prev == null)
        {
            // If current is already approved, should not be current
            return current.Status != AcceptorStatus.Approved;
        }

        return current.Status != AcceptorStatus.Approved && prev.Status == AcceptorStatus.Approved;
    }

    private sealed class ContractGroupKey
    {
        public Guid Id { get; set; }

        public int Sequence { get; set; }

        public string Description { get; set; } = string.Empty;

        public decimal BudgetAmount { get; set; }

        public override bool Equals(object? obj) =>
            obj is ContractGroupKey other &&
            this.Id == other.Id &&
            this.Sequence == other.Sequence;

        public override int GetHashCode() => HashCode.Combine(this.Id, this.Sequence);
    }
}