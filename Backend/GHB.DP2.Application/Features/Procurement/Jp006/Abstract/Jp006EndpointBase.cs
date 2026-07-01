namespace GHB.DP2.Application.Features.Procurement.Jp006.Abstract;

using Codehard.FileService.Client.Abstractions;
using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.Invite.Abstract;
using GHB.DP2.Application.Features.Procurement.Procurement;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PInvite;
using GHB.DP2.Domain.Procurement.PJp005;
using GHB.DP2.Domain.Procurement.PpPurchaseRequisition;
using GHB.DP2.Domain.Procurement.PPurchaseOrder;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using global::GHB.DP2.Application.Features.Procurement.Jp006.Dto;
using GreatFriends.ThaiBahtText;
using LanguageExt;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public abstract partial class Jp006EndpointBase<TRequest, TResponse> : EndpointBase<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : IResult
{
    private readonly Dp2DbContext dbContext;
    private readonly IOperationService operationService;
    private readonly ICommandTextService commandTextService;
    private readonly IFileServiceClient fileServiceClient;

    protected Jp006EndpointBase(
        ILogger logger,
        IOperationService operationService,
        ICommandTextService commandTextService,
        IFileServiceClient fileServiceClient,
        Dp2DbContext dbContext)
        : base(logger)
    {
        this.dbContext = dbContext;
        this.operationService = operationService;
        this.commandTextService = commandTextService;
        this.fileServiceClient = fileServiceClient;
    }

    protected async Task<Procurement> ValidateProcurementAsync(Guid procurementId, CancellationToken ct)
    {
        var procurement = await
            this.dbContext.Procurements
                .SingleOrDefaultAsync(
                    p => p.Id == ProcurementId.From(procurementId),
                    ct);

        if (procurement is null)
        {
            this.ThrowError(
                $"Procurement with ID {procurementId} not found.",
                StatusCodes.Status404NotFound);
        }

        return procurement;
    }

    protected async Task UpsertAcceptors(PPurchaseOrder purchaseOrder, IEnumerable<Jp006AcceptorInfo> acceptors, BusinessUnitId workBusinessUnitId, UserId? sendToAcceptorId = null)
    {
        var lastAssigneeUserId = purchaseOrder.Assignees
            .Where(a => a.Type == AssigneeType.Assignee)
            .OrderByDescending(a => a.Sequence)
            .Select(a => (UserId?)a.UserId)
            .FirstOrDefault();

        var resolvedSendToAcceptorId = lastAssigneeUserId ?? sendToAcceptorId;

        _ = purchaseOrder.Acceptors
                         .Where(w => !acceptors.Select(s => s.Id).Contains(w.Id.Value))
                         .Map(purchaseOrder.RemoveAcceptor)
                         .ToHashSet();

        // Get a User list from the database
        var userIds = acceptors
                      .Map(a => a.UserId)
                      .Map(UserId.From)
                      .ToArray();

        var users = await this.dbContext.SuUsers
                              .Where(u => userIds.Contains(u.Id))
                              .ToArrayAsync(CancellationToken.None);

        var userExists = userIds
                         .Except(users.Map(u => u.Id))
                         .ToArray();

        if (userExists.Length > 0)
        {
            this.ThrowError(
                $"User with ID {string.Join(", ", userExists)} not found.",
                StatusCodes.Status404NotFound);
        }

        var committeePositionsCodes =
            acceptors
                .Where(a => !string.IsNullOrWhiteSpace(a.CommitteePositionsCode))
                .Select(a => a.CommitteePositionsCode)
                .Map(ParameterCode.From!);

        var committeePositions =
            await this.dbContext.SuParameters
                      .Where(p => committeePositionsCodes.Contains(p.Code))
                      .ToArrayAsync(CancellationToken.None);

        var committeePositionsExists =
            committeePositionsCodes.Except(committeePositions.Map(p => p.Code))
                                   .ToArray();

        if (committeePositionsExists.Length > 0)
        {
            this.ThrowError(
                $"Committee position with code {string.Join(", ", committeePositionsExists)} not found.",
                StatusCodes.Status404NotFound);
        }

        // Assuming AcceptorRequest is a DTO that maps to PpMedianPriceAcceptor
        var requestAcceptor =
            acceptors.Join(
                         users,
                         a => a.UserId,
                         u => u.Id.Value,
                         (a, u) =>
                         {
                             var acceptor = a.Id is null
                                 ? PPurchaseOrderAcceptor.Create(
                                     a.AcceptorType,
                                     u,
                                     a.Sequence,
                                     workBusinessUnitId)
                                 : PPurchaseOrderAcceptor.Create(
                                     AcceptorId.From(a.Id.Value),
                                     a.AcceptorType,
                                     u,
                                     a.Sequence,
                                     workBusinessUnitId);

                             _ = string.IsNullOrWhiteSpace(a.CommitteePositionsCode)
                                 ? acceptor.SetCommitteePositionsCode(null)
                                 : acceptor.SetCommitteePositionsCode(
                                     ParameterCode.From(a.CommitteePositionsCode));

                             acceptor.SetIsUnableToPerformDuties(a.IsUnableToPerformDuties ?? false);

                             if (a is { IsUnableToPerformDuties: true, Remark: not null })
                             {
                                 acceptor.UnableToPerformDuties(a.Remark);
                             }

                             return acceptor;
                         })
                     .ToHashSet();

        // Update existing acceptors
        _ = purchaseOrder.Acceptors
                         .Join(
                             requestAcceptor,
                             domainAcceptor => domainAcceptor.Id,
                             request => request.Id,
                             (domainAcceptor, request) =>
                             {
                                 domainAcceptor.SetType(request.Type)
                                               .SetUser(
                                                   request.UserId,
                                                   request.EmployeeCode,
                                                   request.FullName,
                                                   request.PositionName,
                                                   request.BusinessUnitName)
                                               .SetSequence(request.Sequence);

                                 domainAcceptor.SetCommitteePositionsCode(request.CommitteePositionsCode)
                                               .SetIsUnableToPerformDuties(request.IsUnableToPerformDuties);

                                 if (request.IsUnableToPerformDuties)
                                 {
                                     domainAcceptor.UnableToPerformDuties(request.Remark);
                                 }
                                 else if (domainAcceptor.Status == AcceptorStatus.UnableToPerformDuties)
                                 {
                                     if (purchaseOrder.Status == PurchaseOrderStatus.WaitingCommitteeApproval)
                                     {
                                         domainAcceptor.Pending();
                                     }
                                     else
                                     {
                                         domainAcceptor.Draft();
                                     }
                                 }

                                 domainAcceptor.SetSendToAcceptorId(resolvedSendToAcceptorId);

                                 return domainAcceptor;
                             })
                         .ToHashSet();

        // Add new acceptors
        _ = requestAcceptor
            .Except(purchaseOrder.Acceptors)
            .Map(a =>
            {
                a.SetSendToAcceptorId(resolvedSendToAcceptorId);
                return purchaseOrder.AddAcceptor(a);
            })
            .ToHashSet();
    }

    protected async Task UpsertAssignee(
        PPurchaseOrder purchaseOrder,
        IEnumerable<Jp006AssigneeInfo> assignee,
        CancellationToken cancellationToken = default,
        UserId? sendToAcceptorId = null)
    {
        var lastAssigneeUserId = assignee
            .Where(a => a.AssigneeType == AssigneeType.Assignee)
            .OrderByDescending(a => a.Sequence)
            .Select(a => (UserId?)UserId.From(a.UserId))
            .FirstOrDefault();

        var resolvedSendToAcceptorId = lastAssigneeUserId ?? sendToAcceptorId;

        _ = purchaseOrder.Assignees
                         .Where(w => !assignee.Select(s => s.Id).Contains(w.Id.Value))
                         .Map(purchaseOrder.RemoveAssignee)
                         .ToHashSet();

        // Get the user from the database
        var userIds = assignee.Map(a => a.UserId)
                              .Map(UserId.From)
                              .ToArray();

        var users = await this.dbContext.SuUsers
                              .Include(suUser => suUser.Employee)
                              .ThenInclude(rawEmployee => rawEmployee.View)
                              .Where(u => userIds.Contains(u.Id))
                              .ToArrayAsync(cancellationToken);

        var userExists
            = userIds.Except(users.Map(u => u.Id)).ToArray();

        if (userExists.Length > 0)
        {
            this.ThrowError(
                $"User with ID {string.Join(", ", userExists)} not found.",
                StatusCodes.Status404NotFound);
        }

        var requestAssignee =
            assignee.Join(
                        users,
                        a => a.UserId,
                        u => u.Id.Value,
                        (a, u) =>
                        {
                            var assigneeEntity = a.Id.IsNull()
                                ? PPurchaseOrderAssignee.Create(
                                    a.AssigneeGroup,
                                    a.AssigneeType,
                                    u,
                                    a.Sequence)
                                : PPurchaseOrderAssignee.Create(
                                    Domain.Procurement.PPurchaseOrder.PPurchaseOrderAssigneeId.From(a.Id.Value),
                                    a.AssigneeGroup,
                                    a.AssigneeType,
                                    u,
                                    a.Sequence);

                            return assigneeEntity;
                        })
                    .ToHashSet();

        // Update existing assignees
        _ = purchaseOrder.Assignees
                         .Join(
                             requestAssignee,
                             domainAssignee => domainAssignee.Id,
                             request => request.Id,
                             (domainAssignee, request) =>
                             {
                                 domainAssignee.SetType(request.Type)
                                               .SetUser(
                                                   request.UserId,
                                                   request.EmployeeCode,
                                                   request.FullName,
                                                   request.PositionName,
                                                   request.BusinessUnitName)
                                               .SetSequence(request.Sequence);

                                 domainAssignee.SetSendToAcceptorId(resolvedSendToAcceptorId);

                                 return domainAssignee;
                             })
                         .ToHashSet();

        // Add new assignees
        _ = requestAssignee
            .Except(purchaseOrder.Assignees)
            .Map(a =>
            {
                a.SetSendToAcceptorId(resolvedSendToAcceptorId);
                return purchaseOrder.AddAssignee(a);
            })
            .ToHashSet();
    }

    protected void ValidateJp006Status(PPurchaseOrder jp006)
    {
        switch (jp006.Status)
        {
            case PurchaseOrderStatus.Draft:
                this.ThrowError(
                    "ไม่สามารถอนุมัติราคากลางได้ในสถานะปัจจุบัน",
                    StatusCodes.Status400BadRequest);

                break;

            case PurchaseOrderStatus.Approved or PurchaseOrderStatus.Cancelled:
                this.ThrowError(
                    "ราคากลางนี้ถูกอนุมัติหรือยกเลิกแล้ว",
                    StatusCodes.Status400BadRequest);

                break;

            case PurchaseOrderStatus.Rejected:
                jp006.SetWaitingCommitteeApproval();

                break;
        }
    }

    protected Task<PPurchaseOrder?> GetByIdAsync(
        ProcurementId procurementId,
        PurchaseOrderId purchaseOrderId,
        CancellationToken ct)
    {
        return this.dbContext.PJp006S
                   .Include(p => p.Entrepreneurs)
                   .ThenInclude(e => e.PJp006PriceDetails)
                   .Include(p => p.Entrepreneurs)
                   .ThenInclude(e => e.SuVendor)
                   .Include(pJp006 => pJp006.Acceptors)
                   .ThenInclude(p => p.User)
                   .ThenInclude(p => p.Employee)
                   .ThenInclude(v => v.View)
                   .Include(pJp006 => pJp006.Assignees)
                   .ThenInclude(p => p.User)
                   .ThenInclude(p => p.Employee)
                   .ThenInclude(v => v.View)
                   .Include(pJp006 => pJp006.DocumentHistories)
                   .Include(pJp006 => pJp006.Procurement)
                   .ThenInclude(procurement => procurement.Department)
                   .Include(pJp006 => pJp006.Procurement)
                   .ThenInclude(procurement => procurement.SupplyMethod)
                   .Include(pJp006 => pJp006.Procurement)
                   .ThenInclude(procurement => procurement.SupplyMethodType)
                   .Include(pJp006 => pJp006.Procurement)
                   .ThenInclude(procurement => procurement.SupplyMethodSpecialType)
                   .Include(pJp006 => pJp006.Procurement)
                   .ThenInclude(procurement => procurement.Plan)
                   .Include(p => p.Procurement)
                   .ThenInclude(p => p.Jp005)
                   .ThenInclude(jp005 => jp005.ProcurementSuppliesDivisions)
                   .Include(p => p.Procurement)
                   .ThenInclude(p => p.PurchaseRequisitions)
                   .ThenInclude(a => a.Assignees)
                   .ThenInclude(p => p.User)
                   .ThenInclude(p => p.Employee)
                   .ThenInclude(v => v.View)
                   .Include(po => po.Entrepreneurs)
                   .ThenInclude(e => e.PurchaseOrderEntrepreneurChecker)
                   .Include(po => po.Entrepreneurs)
                   .ThenInclude(e => e.PurchaseOrderShareholders)
                   .ThenInclude(sh => sh.PurchaseOrderEntrepreneurShareholderCheckers)
                   .Include(p => p.Procurement)
                   .ThenInclude(p => p.PurchaseRequisitions)
                   .ThenInclude(c => c.DeliveryPeriodType)
                   .OrderByDescending(p =>
                       p.Status == PurchaseOrderStatus.Approved)
                   .ThenByDescending(p =>
                       p.Status == PurchaseOrderStatus.Cancelled ? 9 :
                       p.Status == PurchaseOrderStatus.RejectToAssignee ? 8 :
                       p.Status == PurchaseOrderStatus.WaitingApproval ? 7 :
                       p.Status == PurchaseOrderStatus.WaitingComment ? 6 :
                       p.Status == PurchaseOrderStatus.WaitingAssign ? 5 :
                       p.Status == PurchaseOrderStatus.WaitingCommitteeApproval ? 4 :
                       p.Status == PurchaseOrderStatus.Rejected ? 3 :
                       p.Status == PurchaseOrderStatus.Edit ? 2 :
                       p.Status == PurchaseOrderStatus.Draft ? 1 :
                       0)
                   .AsSplitQuery()
                   .FirstOrDefaultAsync(
                       p =>
                           p.Id == purchaseOrderId &&
                           p.ProcurementId == procurementId,
                       ct);
    }

    protected GetJp006ByIdResponse? MapPJp006(PPurchaseOrder? pPurchaseOrder)
    {
        var medianPriceData = pPurchaseOrder?.Procurement.PurchaseRequisitions.FirstOrDefault();

        var jp005 = pPurchaseOrder?.Procurement.Jp005.FirstOrDefault();

        if (pPurchaseOrder == null)
        {
            return null;
        }

        var operatorIds = pPurchaseOrder.Procurement.PurchaseRequisitions.FirstOrDefault()?
            .Assignees?.OrderBy(x => x.Sequence).Select(c => new Operators((Guid)c.UserId, c.Sequence)).ToArray();

        var jp006Doc = pPurchaseOrder.DocumentHistories
                                     .Where(w => w.DocumentType == PurchaseOrderDocumentType.Jp006)
                                     .OrderVersions()
                                     .FirstOrDefault();

        var isReplacedJp006 = pPurchaseOrder.DocumentHistories
                                            .Any(w => w.DocumentType == PurchaseOrderDocumentType.Jp006 && w.IsReplaced);

        var winnerDoc = pPurchaseOrder.DocumentHistories
                                      .Where(w => w.DocumentType == PurchaseOrderDocumentType.Winner)
                                      .OrderVersions()
                                      .FirstOrDefault();

        var isReplacedWinner = pPurchaseOrder.DocumentHistories
                                             .Any(w => w.DocumentType == PurchaseOrderDocumentType.Winner && w.IsReplaced);

        var jp006DocumentVersions = pPurchaseOrder.DocumentHistories
                                                  .Where(d => d.DocumentType == PurchaseOrderDocumentType.Jp006)
                                                  .OrderVersions()
                                                  .Select((d, index) => new Jp006DocumentVersionResponse(
                                                      d.FileId.Value,
                                                      d.Version,
                                                      d.CreatedAt,
                                                      d.CreatedByName ?? string.Empty,
                                                      index == 0))
                                                  .ToArray();

        var winnerDocumentVersions = pPurchaseOrder.DocumentHistories
                                                   .Where(d => d.DocumentType == PurchaseOrderDocumentType.Winner)
                                                   .OrderVersions()
                                                   .Select((d, index) => new Jp006DocumentVersionResponse(
                                                       d.FileId.Value,
                                                       d.Version,
                                                       d.CreatedAt,
                                                       d.CreatedByName ?? string.Empty,
                                                       index == 0))
                                                   .ToArray();

        var procurementSuppliesDivision =
            jp005?.ProcurementSuppliesDivisions
                 .Select(s => new ProcurementSuppliesDivisionDto(
                     s.Id.Value,
                     s.SuUserId.Value,
                     s.FullName,
                     s.FullPositionName,
                     s.Sequence))
                 .OrderBy(o => o.Sequence)
                 .ToList() ?? [];

        var currentCommittees = pPurchaseOrder.Acceptors
                                              .Where(x => x.Type == AcceptorType.ProcurementCommittee)
                                              .Map(MapToAcceptorResponse).OrderBy(s => s.Sequence);

        var currentAcceptors = pPurchaseOrder.Acceptors
                                             .Where(x => x.Type == AcceptorType.Approver)
                                             .Select(DelegatorExtensions.DelegatorToAcceptor)
                                             .Map(MapToAcceptorResponse).OrderBy(s => s.Sequence);

        var acceptors = currentCommittees.Concat(currentAcceptors);

        return new GetJp006ByIdResponse(
            pPurchaseOrder!.Id.Value,
            pPurchaseOrder.ProcurementId.Value,
            new ProcurementDto(
                pPurchaseOrder.Procurement.PlanId.HasValue ? (Guid)pPurchaseOrder.Procurement.PlanId : null,
                pPurchaseOrder.Procurement.ProcurementNumber,
                pPurchaseOrder.Procurement.Type,
                pPurchaseOrder.Procurement.Step,
                pPurchaseOrder.Procurement.Department.Name,
                pPurchaseOrder.Procurement.DepartmentId,
                pPurchaseOrder.Procurement.Plan?.PlanNumber.ToString() ?? string.Empty,
                pPurchaseOrder.Procurement.Name,
                pPurchaseOrder.Procurement.Budget,
                pPurchaseOrder.Procurement.Budget.ThaiBahtText(),
                pPurchaseOrder.Procurement.BudgetYear,
                pPurchaseOrder.Procurement.SupplyMethod.Label,
                pPurchaseOrder.Procurement.SupplyMethodCode,
                pPurchaseOrder.Procurement.SupplyMethodType?.Label ?? string.Empty,
                pPurchaseOrder.Procurement.SupplyMethodTypeCode,
                pPurchaseOrder.Procurement.SupplyMethodSpecialType?.Label ?? string.Empty,
                pPurchaseOrder.Procurement.SupplyMethodSpecialTypeCode,
                pPurchaseOrder.Procurement.Status,
                pPurchaseOrder.Procurement.ExpectingProcurementAt,
                pPurchaseOrder.Procurement.IsStock,
                pPurchaseOrder.Procurement.IsCommercialMaterial,
                pPurchaseOrder.Procurement.Plan?.Type,
                pPurchaseOrder.Procurement.ProcessType),
            pPurchaseOrder.Status,
            jp006Doc?.FileId.Value,
            false,
            winnerDoc?.FileId.Value,
            false,
            pPurchaseOrder.Entrepreneurs
                          .OrderBy(o => o.Sequence)
                          .Map(MapToEntrepreneurResponse),
            acceptors,
            pPurchaseOrder.Assignees
                          .Select(DelegatorExtensions.DelegatorToAssignee)
                          .Map(MapToAssigneeResponse).OrderBy(o => o.Sequence),
            medianPriceData?.MedianPriceAmount,
            pPurchaseOrder.Entrepreneurs.Where(x => x.IsWinner).SelectMany(s => s.PJp006PriceDetails).Sum(x => x.AgreedPrice * x.ParcelQuantity) > 1000000,
            operatorIds,
            procurementSuppliesDivision,
            jp006DocumentVersions,
            winnerDocumentVersions,
            (string?)pPurchaseOrder.PurchaseOrderNumber.ToString(),
            [],
            pPurchaseOrder.DocumentDate,
            pPurchaseOrder.AuditInfo.LastModifiedAt);
    }

    protected Task<GetJp006ByIdResponse?> GetByProcurementIdAsync(ProcurementId procurementId, CancellationToken ct)
    {
        return this.dbContext.PInvites
                   .Include(i => i.Procurement)
                   .ThenInclude(p => p.TorDrafts)
                   .Include(i => i.Procurement)
                   .ThenInclude(procurement => procurement.Department)
                   .Include(i => i.Procurement)
                   .ThenInclude(procurement => procurement.SupplyMethod)
                   .Include(i => i.Procurement)
                   .ThenInclude(procurement => procurement.SupplyMethodType)
                   .Include(i => i.Procurement)
                   .ThenInclude(procurement => procurement.SupplyMethodSpecialType)
                   .Include(i => i.Procurement)
                   .ThenInclude(procurement => procurement.Plan)
                   .Include(i => i.Procurement)
                   .ThenInclude(i => i.PurchaseRequisitions)
                   .ThenInclude(a => a.Assignees)
                   .ThenInclude(p => p.User)
                   .ThenInclude(p => p.Employee)
                   .Include(i => i.Procurement)
                   .ThenInclude(p => p.Jp005)
                   .ThenInclude(jp005 => jp005.ProcurementSuppliesDivisions)
                   .AsSplitQuery()
                   .FirstOrDefaultAsync(p => p.ProcurementId == procurementId, ct)
                   .Map(MapPInvite);

        static GetJp006ByIdResponse? MapPInvite(PInvite? pInvite)
        {
            if (pInvite.IsNull())
            {
                return null;
            }

            var medianPriceData = pInvite!.Procurement.PurchaseRequisitions.FirstOrDefault();

            var jp005Data = pInvite!.Procurement.Jp005.FirstOrDefault();

            var operatorIds = pInvite.Procurement.PurchaseRequisitions.FirstOrDefault()?
                .Assignees?.OrderBy(c => c.Sequence).Select(c => new Operators((Guid)c.UserId, c.Sequence)).ToArray();

            var committeeData = jp005Data?.Committees.Where(x => x.GroupType == PJp005CommitteeGroupType.ProcurementCommittee);

            var isSixtyMoreThan100k = pInvite.Procurement.SupplyMethodCode == SupplyMethodConstant.Sixty &&
                                      pInvite.Procurement.Budget > 100000;

            var procurementSuppliesDivision =
                pInvite.Procurement
                       .Jp005
                       .FirstOrDefault()?.ProcurementSuppliesDivisions
                       .Select(s => new ProcurementSuppliesDivisionDto(
                           s.Id.Value,
                           s.SuUserId.Value,
                           s.FullName,
                           s.FullPositionName,
                           s.Sequence))
                       .OrderBy(o => o.Sequence)
                       .ToList() ?? [];

            var purchaseRequisitionData = pInvite.Procurement.PurchaseRequisitions.FirstOrDefault();
            var techSpecial = purchaseRequisitionData?.TechnicalSpecifications.OrderBy(x => x.Sequence);

            return new GetJp006ByIdResponse(
                null,
                pInvite!.ProcurementId.Value,
                null,
                PurchaseOrderStatus.Draft,
                null,
                false,
                null,
                false,
                pInvite.InvitedEntrepreneurs
                       .Map(MapToEntrepreneur),
                isSixtyMoreThan100k ? [] : committeeData.Map(MapAcceptorResponse).OrderBy(x => x.Sequence),
                [],
                medianPriceData?.MedianPriceAmount,
                false,
                operatorIds,
                procurementSuppliesDivision,
                [],
                [],
                null,
                techSpecial != null ? techSpecial.Map(MapToPriceDetails) : [],
                null);
        }

        static Jp006EntrepreneurResponse MapToEntrepreneur(PInvitedEntrepreneurs pInvitedEntrepreneurs)
        {
            var purchaseRequisitionData = pInvitedEntrepreneurs.Invite.Procurement.PurchaseRequisitions.FirstOrDefault();

            var techSpecial = purchaseRequisitionData?.TechnicalSpecifications.OrderBy(x => x.Sequence);

            var shareholders =
                pInvitedEntrepreneurs
                    .InvitedEntrepreneurShareholders
                    .OrderBy(s => s.Sequence)
                    .Select(s => new PurchaseOrderEntrepreneurShareholderDto(
                        s.Id.Value,
                        s.Sequence,
                        s.TaxId,
                        s.FirstName,
                        s.LastName,
                        s.IsDirector,
                        s.IsShareholder,
                        s.IsJuristic,
                        s.CheckType,
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

            return new Jp006EntrepreneurResponse(
                null,
                pInvitedEntrepreneurs.Vendor.Id.Value,
                false,
                pInvitedEntrepreneurs.Sequence,
                new EntrepreneurCheckConditions(
                    null,
                    null,
                    null),
                new EntrepreneurCheckConditions(
                    null,
                    null,
                    null),
                new EntrepreneurCheckConditions(
                    null,
                    null,
                    null),
                pInvitedEntrepreneurs.Vendor.TaxpayerIdentificationNo,
                pInvitedEntrepreneurs.Vendor.EntrepreneurTypeInfo.Label,
                pInvitedEntrepreneurs.Vendor.EstablishmentName,
                pInvitedEntrepreneurs.Vendor.Email,
                pInvitedEntrepreneurs.Vendor.Nationality,
                pInvitedEntrepreneurs.Vendor.Type,
                pInvitedEntrepreneurs.Vendor.PlaceName,
                pInvitedEntrepreneurs.Vendor.Tel,
                false,
                null,
                null,
                techSpecial != null ? techSpecial.Map(MapToPriceDetails) : [],
                false,
                null,
                null,
                (PurchaseOrderEntrepreneurShareholderDto[]?)shareholders,
                [],
                pInvitedEntrepreneurs.Vendor.SapBranchNumber);
        }

        static Jp006PriceDetailsResponse MapToPriceDetails(PpPurchaseRequisitionTechnicalSpecifications priceDetails)
        {
            return new Jp006PriceDetailsResponse(
                priceDetails.Id.Value,
                priceDetails.Sequence,
                priceDetails.Name,
                priceDetails.Quantity,
                priceDetails.UnitCode.HasValue ? priceDetails.UnitCode.Value.ToString() : string.Empty,
                string.Empty,
                0,
                0,
                priceDetails.Description);
        }

        static Jp006AcceptorResponseInfo MapAcceptorResponse(PJp005Committee committee)
        {
            return new Jp006AcceptorResponseInfo(
                null,
                AcceptorType.ProcurementCommittee,
                committee.SuUserId.Value,
                committee.Sequence,
                committee.FullName,
                committee.FullPositionName,
                committee.User.Employee.View?.BusinessUnitName ?? string.Empty,
                AcceptorStatus.Draft,
                null,
                null,
                Optional(committee.CommitteePositionsCode)
                    .Map(v => v.Value)
                    .IfNoneUnsafe((string?)null),
                committee.CommitteePositionsName,
                false,
                false,
                committee.User.Employee.PrimaryDepartment != null ? (string)committee.User.Employee.PrimaryDepartment.Id : string.Empty);
        }
    }

    private static Jp006EntrepreneurResponse MapToEntrepreneurResponse(PPurchaseOrderEntrepreneur entrepreneur)
    {
        var shareholders =
            entrepreneur
                .PurchaseOrderShareholders
                .Where(s => !s.IsDeleted)
                .OrderBy(s => s.Sequence)
                .Select(s =>
                {
                    var coiChecker = s.PurchaseOrderEntrepreneurShareholderCheckers
                                      .OrderByDescending(c => c.ResultAt)
                                      .FirstOrDefault(c => c.CheckType == QualificationType.COI);

                    var watchlistChecker = s.PurchaseOrderEntrepreneurShareholderCheckers
                                            .OrderByDescending(c => c.ResultAt)
                                            .FirstOrDefault(c => c.CheckType == QualificationType.Watchlist);

                    var coiCheckerResult = coiChecker is null
                        ? null
                        : new QualificationResultDto(
                            coiChecker.Result,
                            coiChecker.ResultAt,
                            coiChecker.Remark);

                    var watchlistCheckerResult = watchlistChecker is null
                        ? null
                        : new QualificationResultDto(
                            watchlistChecker.Result,
                            watchlistChecker.ResultAt,
                            watchlistChecker.Remark);

                    return new PurchaseOrderEntrepreneurShareholderDto(
                        s.Id.Value,
                        s.Sequence,
                        s.TaxId,
                        s.FirstName,
                        s.LastName,
                        s.IsDirector,
                        s.IsShareholder,
                        s.IsJuristic,
                        s.CheckType,
                        s.WatchlistResult,
                        s.WatchlistResultRemark,
                        s.WatchlistResultAt,
                        s.CoiResult,
                        s.CoiResultRemark,
                        s.CoiResultAt,
                        s.EgpResult,
                        s.EgpRemark,
                        s.EgpResultAt,
                        coiCheckerResult,
                        watchlistCheckerResult);
                })
                .ToArray();

        var coiChecker =
            entrepreneur.PurchaseOrderEntrepreneurChecker
                        .OrderByDescending(c => c.ResultAt)
                        .FirstOrDefault(c => c.CheckType == QualificationType.COI);

        var watchlistChecker =
            entrepreneur.PurchaseOrderEntrepreneurChecker
                        .OrderByDescending(c => c.ResultAt)
                        .FirstOrDefault(c => c.CheckType == QualificationType.Watchlist);

        var coiCheckerResult = coiChecker is null
            ? null
            : new QualificationResultDto(
                coiChecker.Result,
                coiChecker.ResultAt,
                coiChecker.Remark);

        var watchlistCheckerResult = watchlistChecker is null
            ? null
            : new QualificationResultDto(
                watchlistChecker.Result,
                watchlistChecker.ResultAt,
                watchlistChecker.Remark);

        return new Jp006EntrepreneurResponse(
            entrepreneur.Id.Value,
            entrepreneur.SuVendorId.Value,
            entrepreneur.EmailSended,
            entrepreneur.Sequence,
            new EntrepreneurCheckConditions(entrepreneur.CoiResult, entrepreneur.CoiRemark, entrepreneur.CoiDate),
            new EntrepreneurCheckConditions(entrepreneur.WatchlistResult, entrepreneur.WatchlistRemark, entrepreneur.WatchlistDate),
            new EntrepreneurCheckConditions(entrepreneur.EgpResult, entrepreneur.EgpRemark, entrepreneur.EgpDate),
            entrepreneur.SuVendor.TaxpayerIdentificationNo,
            entrepreneur.SuVendor.EntrepreneurTypeInfo.Label,
            entrepreneur.SuVendor.EstablishmentName,
            entrepreneur.SuVendor.Email,
            entrepreneur.SuVendor.Nationality,
            entrepreneur.SuVendor.Type,
            entrepreneur.SuVendor.PlaceName,
            entrepreneur.SuVendor.Tel,
            entrepreneur.IsWinner,
            entrepreneur.SelectionReasonCode,
            entrepreneur.Remark,
            entrepreneur.PJp006PriceDetails.Select(MapToPriceDetailsResponse).OrderBy(s => s.Sequence),
            entrepreneur.PJp006PriceDetails.All(x => !string.IsNullOrWhiteSpace(x.VatTypeCode) && x.AgreedPrice > 0),
            coiCheckerResult,
            watchlistCheckerResult,
            (PurchaseOrderEntrepreneurShareholderDto[]?)shareholders,
            [
                .. entrepreneur.Attachments
                               .OrderBy(o => o.Sequence)
                               .GroupBy(
                                   a => a.DocumentTypeCode,
                                   (key, g) => new EntrepreneurResponseAttachment(
                                       key.Value,
                                       [
                                           .. g.Select(s =>
                                               new EntrepreneurFileWithId(
                                                   s.Id.Value,
                                                   s.FileId.Value,
                                                   s.FileName,
                                                   s.Sequence,
                                                   s.IsPublic,
                                                   s.AuditInfo.CreatedBy,
                                                   s.Type))
                                       ]))
            ],
            entrepreneur.SuVendor.SapBranchNumber);
    }

    private static Jp006PriceDetailsResponse MapToPriceDetailsResponse(PPurchaseOrderPriceDetails priceDetails)
    {
        return new Jp006PriceDetailsResponse(
            priceDetails.Id.Value,
            priceDetails.Sequence,
            priceDetails.ParcelName,
            priceDetails.ParcelQuantity,
            priceDetails.ParcelUnitCode,
            priceDetails.VatTypeCode,
            priceDetails.OfferedPrice,
            priceDetails.AgreedPrice,
            priceDetails.Description);
    }

    private static Jp006AcceptorResponseInfo MapToAcceptorResponse(PPurchaseOrderAcceptor acceptor)
    {
        return new Jp006AcceptorResponseInfo(
            acceptor.Id.Value,
            acceptor.Type,
            acceptor.UserId.Value,
            acceptor.Sequence,
            acceptor.FullName,
            acceptor.PositionName,
            acceptor.BusinessUnitName,
            acceptor.Status,
            acceptor.Remark,
            acceptor.ActionAt,
            Optional(acceptor.CommitteePositionsCode)
                .Map(v => v.Value)
                .IfNoneUnsafe((string?)null),
            acceptor.CommitteePosition?.Label,
            acceptor.IsUnableToPerformDuties,
            acceptor.IsCurrentApprover(),
            acceptor.User.Employee.PrimaryDepartment != null ? (string)acceptor.User.Employee.PrimaryDepartment.Id : string.Empty,
            acceptor.Delegatee?.SuUserId.Value);
    }

    private static AssigneeResponse MapToAssigneeResponse(PPurchaseOrderAssignee assignee)
    {
        return new AssigneeResponse(
            assignee.Id.Value,
            assignee.Group,
            assignee.Type,
            assignee.UserId.Value,
            assignee.Sequence,
            assignee.FullName,
            assignee.PositionName,
            assignee.BusinessUnitName,
            assignee.Status,
            assignee.Remark,
            assignee.ActionAt,
            assignee.Delegatee?.SuUserId.Value);
    }

    protected async Task UpsertAttachments(PPurchaseOrderEntrepreneur entity, EntrepreneurResponseAttachment[] attachments)
    {
        var fileList = attachments
                       .SelectMany(r => r.FileAttachments.Select(f => new
                       {
                           f.Id,
                           r.DocumentTypeCode,
                           f.FileId,
                           f.FileName,
                           f.Sequence,
                           f.IsPublic,
                           f.Type,
                       }))
                       .ToArray();

        var removeFileIds = entity.Attachments
                                  .Where(w => !fileList.Select(s => s.Id).Contains(w.Id.Value))
                                  .Map(s =>
                                  {
                                      entity.RemoveAttachment(s);

                                      return s.FileId;
                                  }).ToArray();

        foreach (var id in removeFileIds)
        {
            await this.fileServiceClient.DeleteAsync(id, CancellationToken.None);
        }

        fileList.Where(w => !w.Id.HasValue)
                .Map(f => PurchaseOrderEntrepreneurAttachments.Create(
                    ParameterCode.From(f.DocumentTypeCode),
                    FileId.From(f.FileId),
                    f.FileName,
                    f.Type,
                    f.Sequence,
                    f.IsPublic))
                .Iter(r => entity.AddAttachment(r));

        foreach (var existing in entity.Attachments)
        {
            var match = fileList
                        .Where(w => w.Id.HasValue)
                        .FirstOrDefault(e => e.Id == existing.Id.Value);

            if (match != null)
            {
                existing.SetIsPublic(match.IsPublic)
                        .SetSequence(match.Sequence)
                        .SetDocumentType(ParameterCode.From(match.DocumentTypeCode));
            }
        }
    }

    protected async Task ValidateDocumentTypeCode(EntrepreneurResponseAttachment[] attachments, CancellationToken ct)
    {
        var docTypeCodes = attachments.Select(s => s.DocumentTypeCode)
                                      .Where(w => !string.IsNullOrWhiteSpace(w))
                                      .Select(ParameterCode.From)
                                      .ToArray();

        var docType = await this.dbContext.SuParameters
                                .Where(x => docTypeCodes.Contains(x.Code))
                                .ToArrayAsync(ct);

        var missingDocumentTypes = docTypeCodes
                                   .Except(docType.Select(dt => dt.Code))
                                   .ToArray();

        if (missingDocumentTypes.Any())
        {
            this.ThrowError(
                $"ไม่พบประเภทไฟล์",
                StatusCodes.Status404NotFound);
        }
    }
}