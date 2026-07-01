namespace GHB.DP2.Application.Features.Procurement.PurchaseRequisition.Abstract;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.Procurement.Procurement;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PpMedianPrice;
using GHB.DP2.Domain.Procurement.PpPurchaseRequisition;
using GHB.DP2.Domain.Procurement.PpTorDraft;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using GHB.DP2.Application.Services.Document;
using GreatFriends.ThaiBahtText;
using LanguageExt;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System;

public record PurchaseRequisitionAcceptorDto(
    AcceptorType Type,
    Guid UserId,
    string EmployeeCode,
    string FullName,
    string PositionName,
    string BusinessUnitName,
    int Sequence,
    Guid? DelegateeId,
    AcceptorStatus Status,
    DateTimeOffset? ActionAt,
    string? Remark
);

public record PurchaseRequisition(
    string? PurchaseRequisitionNumber,
    string? EgpNumber,
    string? PrNumber,
    string? Description,
    string? PriceReasonablenessInfo,
    string? Telephone,
    decimal? MedianPriceAmount,
    string? EvaluationCriteriaCode,
    int? DeliveryPeriod,
    string? DeliveryPeriodTypeCode,
    string? DeliveryConditionCode,
    DateTimeOffset? DeliveryDate,
    bool HasFineRate,
    bool HasWarranty,
    int? WarrantyPeriod,
    string? WarrantyPeriodCode,
    string? WarrantyConditionCode,
    bool HasContractGuarantee,
    bool HasInspectionCommittee,
    bool HasConstructionSupervisor,
    Guid? PurchaseRequisitionDocumentId,
    bool? IsPurchaseRequisitionDocumentIdReplaced,
    PurchaseRequisitionStatus Status,
    DateTimeOffset? DocumentDate);

public record PurchaseRequisitionComponents(
    BasicInfo BasicInfo,
    PriceInfo PriceInfo,
    DeliveryInfo DeliveryInfo,
    WarrantyInfo WarrantyInfo,
    ContractOptions ContractOptions);

public abstract partial class PurchaseRequisitionEndpointBase<TRequest, TResponse> : EndpointBase<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : IResult
{
    private readonly Dp2DbContext dbContext;

    protected PurchaseRequisitionEndpointBase(
        ILogger logger,
        Dp2DbContext dbContext)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    protected void ValidateUsers(SuUser[] users, UserId[] userIds)
    {
        var foundUserIds = users.Select(u => u.Id).ToArray();

        var missingUserIds = userIds.Except(foundUserIds).ToArray();

        if (missingUserIds.Length > 0)
        {
            this.ThrowError(
                $"Users with IDs {string.Join(", ", missingUserIds)} not found.",
                StatusCodes.Status404NotFound);
        }
    }

    protected void AddAssignee(PpPurchaseRequisition purchaseRequisition, IEnumerable<AssigneeRequest> assignees, SuUser[] users, UserId? sendToAcceptorId = null)
    {
        var lastAssigneeUserId = assignees
            .Where(a => a.AssigneeType == AssigneeType.Assignee)
            .OrderByDescending(a => a.Sequence)
            .Select(a => (UserId?)UserId.From(a.UserId))
            .FirstOrDefault();

        var resolvedSendToAcceptorId = lastAssigneeUserId ?? sendToAcceptorId;

        assignees
            .Join(
                users,
                a => UserId.From(a.UserId),
                u => u.Id,
                (a, u) =>
                {
                    var assignee = PpPurchaseRequisitionAssignee.Create(a.AssigneeType, u, a.Sequence);
                    assignee.SetSendToAcceptorId(resolvedSendToAcceptorId);
                    return assignee;
                })
            .Iter(pa => purchaseRequisition.AddPpPurchaseRequisitionAssignees(pa));
    }

    protected static PurchaseRequisitionComponents MapPurchaseModelToDomain(
        PurchaseRequisition req)
    {
        var basicInfo = new BasicInfo(
            req.PurchaseRequisitionNumber,
            req.EgpNumber,
            req.PrNumber,
            req.Description,
            req.Telephone);

        var priceInfo = new PriceInfo(
            req.PriceReasonablenessInfo,
            req.MedianPriceAmount,
            string.IsNullOrWhiteSpace(req.EvaluationCriteriaCode) ? null : ParameterCode.From(req.EvaluationCriteriaCode!));

        var deliveryInfo = new DeliveryInfo(
            req.DeliveryPeriod,
            string.IsNullOrWhiteSpace(req.DeliveryPeriodTypeCode) ? null : ParameterCode.From(req.DeliveryPeriodTypeCode!),
            string.IsNullOrWhiteSpace(req.DeliveryConditionCode) ? null : ParameterCode.From(req.DeliveryConditionCode!),
            req.DeliveryDate);

        var warrantyInfo = new WarrantyInfo(
            req.HasWarranty,
            req.WarrantyPeriod,
            string.IsNullOrWhiteSpace(req.WarrantyPeriodCode) ? null : ParameterCode.From(req.WarrantyPeriodCode!),
            string.IsNullOrWhiteSpace(req.WarrantyConditionCode) ? null : ParameterCode.From(req.WarrantyConditionCode!));

        var contractOptions = new ContractOptions(
            req.HasFineRate,
            req.HasContractGuarantee,
            req.HasInspectionCommittee,
            req.HasConstructionSupervisor);

        return new PurchaseRequisitionComponents(
            basicInfo,
            priceInfo,
            deliveryInfo,
            warrantyInfo,
            contractOptions);
    }

    protected async Task<PpPurchaseRequisition> GetPurchaseRequisitionById(
        PpPurchaseRequisitionId id,
        ProcurementId procurementId,
        CancellationToken ct)
    {
        var purchaseRequisition = await this.dbContext.PpPurchaseRequisitions
                                            .Include(r => r.Budgets)
                                            .ThenInclude(b => b.PpPurchaseRequisitionBudgetDetails)
                                            .ThenInclude(bd => bd.AccountNo)
                                            .Include(r => r.Budgets)
                                            .ThenInclude(b => b.PpPurchaseRequisitionBudgetDetails)
                                            .ThenInclude(bd => bd.BudgetType)
                                            .Include(r => r.Warranties)
                                            .Include(r => r.PaymentTerms)
                                            .Include(r => r.FineRates)
                                            .Include(r => r.Committees)
                                            .ThenInclude(ppPurchaseRequisitionCommittee => ppPurchaseRequisitionCommittee.User)
                                            .ThenInclude(suUser => suUser.Employee)
                                            .ThenInclude(rawEmployee => rawEmployee.View)
                                            .Include(r => r.Acceptors)
                                            .Include(r => r.Assignees)
                                            .Include(r => r.TechnicalSpecifications)
                                            .ThenInclude(r => r.Unit)
                                            .Include(x => x.TorDraft)
                                            .ThenInclude(x => x!.PpTorDraftObjects)
                                            .Include(r => r.DocumentHistories)
                                            .Include(r => r.Procurement)
                                            .ThenInclude(procurement => procurement.Department)
                                            .Include(r => r.Procurement)
                                            .ThenInclude(procurement => procurement.SupplyMethod)
                                            .Include(r => r.Procurement)
                                            .ThenInclude(procurement => procurement.SupplyMethodType)
                                            .Include(r => r.Procurement)
                                            .ThenInclude(procurement => procurement.SupplyMethodSpecialType)
                                            .Include(r => r.Procurement)
                                            .ThenInclude(procurement => procurement.Plan)
                                            .Include(p => p.EvaluationCriteria)
                                            .Include(r => r.DeliveryPeriodType)
                                            .Include(r => r.DeliveryCondition)
                                            .AsSplitQuery()
                                            .FirstOrDefaultAsync(
                                                r =>
                                                    r.Id == id &&
                                                    r.ProcurementId == procurementId,
                                                ct);

        if (purchaseRequisition is null)
        {
            this.ThrowError($"Purchase requisition with ID {id} not found for procurement {procurementId}.", StatusCodes.Status404NotFound);
        }

        return purchaseRequisition;
    }

    protected Task<GetPurchaseRequisitionResponse?> GetByIdAsync(
        ProcurementId procurementId, PpPurchaseRequisitionId purchaseRequisitionId, CancellationToken ct, Guid userId)
    {
        return this.dbContext.PpPurchaseRequisitions
                   .Include(r => r.Budgets)
                   .ThenInclude(b => b.PpPurchaseRequisitionBudgetDetails)
                   .ThenInclude(bd => bd.AccountNo)
                   .Include(r => r.Budgets)
                   .ThenInclude(b => b.PpPurchaseRequisitionBudgetDetails)
                   .ThenInclude(bd => bd.BudgetType)
                   .Include(r => r.Warranties)
                   .Include(r => r.PaymentTerms)
                   .Include(r => r.FineRates)
                   .Include(r => r.Committees)
                   .ThenInclude(ppPurchaseRequisitionCommittee => ppPurchaseRequisitionCommittee.User)
                   .ThenInclude(suUser => suUser.Employee)
                   .ThenInclude(rawEmployee => rawEmployee.View)
                   .Include(r => r.Acceptors)
                   .ThenInclude(a => a.User)
                   .ThenInclude(u => u.Employee)
                   .Include(r => r.Assignees)
                   .ThenInclude(p => p.User)
                   .ThenInclude(p => p.Employee)
                   .Include(r => r.TechnicalSpecifications)
                   .ThenInclude(r => r.Unit)
                   .Include(x => x.TorDraft)
                   .ThenInclude(x => x!.PpTorDraftObjects)
                   .Include(r => r.DocumentHistories)
                   .Include(r => r.Procurement)
                   .ThenInclude(procurement => procurement.Department)
                   .Include(r => r.Procurement)
                   .ThenInclude(procurement => procurement.SupplyMethod)
                   .Include(r => r.Procurement)
                   .ThenInclude(procurement => procurement.SupplyMethodType)
                   .Include(r => r.Procurement)
                   .ThenInclude(procurement => procurement.SupplyMethodSpecialType)
                   .Include(r => r.Procurement)
                   .ThenInclude(procurement => procurement.Plan)
                   .Include(p => p.EvaluationCriteria)
                   .Include(r => r.DeliveryPeriodType)
                   .Include(r => r.DeliveryCondition)
                   .AsNoTracking()
                   .AsSplitQuery()
                   .FirstOrDefaultAsync(
                       r =>
                           r.Id == purchaseRequisitionId &&
                           r.ProcurementId == procurementId,
                       ct)
                   .Map(x => this.MapJp004(x, userId));
    }

    private GetPurchaseRequisitionResponse? MapJp004(PpPurchaseRequisition? purchaseRequisition, Guid userId)
    {
        if (purchaseRequisition == null)
        {
            return null;
        }

        var torDraft = purchaseRequisition.TorDraft;

        var lastedDocument = purchaseRequisition.LastedDocument;

        var documentVersions =
            purchaseRequisition.DocumentHistories
                               .OrderVersions()
                               .Select((d, index) => new PurchaseRequisitionDocumentVersionResponse(
                                   d.FileId.Value,
                                   d.Version,
                                   d.CreatedAt,
                                   d.CreatedByName ?? string.Empty,
                                   index == 0))
                               .ToArray();

        return new GetPurchaseRequisitionResponse(
            purchaseRequisition.Id,
            purchaseRequisition.ProcurementId,
            new ProcurementDto(
                purchaseRequisition.Procurement.PlanId.Map(p => p.Value),
                purchaseRequisition.Procurement.ProcurementNumber,
                purchaseRequisition.Procurement.Type,
                purchaseRequisition.Procurement.Step,
                purchaseRequisition.Procurement.Department.Name,
                purchaseRequisition.Procurement.DepartmentId,
                purchaseRequisition.Procurement.Plan?.PlanNumber.ToString() ?? string.Empty,
                purchaseRequisition.Procurement.Name,
                purchaseRequisition.Procurement.Budget,
                purchaseRequisition.Procurement.Budget.ThaiBahtText(),
                purchaseRequisition.Procurement.BudgetYear,
                purchaseRequisition.Procurement.SupplyMethod.Label,
                purchaseRequisition.Procurement.SupplyMethodCode,
                purchaseRequisition.Procurement.SupplyMethodType?.Label ?? string.Empty,
                purchaseRequisition.Procurement.SupplyMethodTypeCode,
                purchaseRequisition.Procurement.SupplyMethodSpecialType?.Label ?? string.Empty,
                purchaseRequisition.Procurement.SupplyMethodSpecialTypeCode,
                purchaseRequisition.Procurement.Status,
                purchaseRequisition.Procurement.ExpectingProcurementAt,
                purchaseRequisition.Procurement.IsStock,
                purchaseRequisition.Procurement.IsCommercialMaterial,
                purchaseRequisition.Procurement.Plan?.Type,
                purchaseRequisition.Procurement.ProcessType),
            purchaseRequisition.TorDraftId,
            purchaseRequisition.LastedDocument?.FileId.Value,
            purchaseRequisition.IsReplacedDocument,
            new GetPurchaseRequisition(
                purchaseRequisition.PurchaseRequisitionNumber.Value,
                purchaseRequisition.EgpNumber,
                purchaseRequisition.PrNumber,
                purchaseRequisition.Description,
                purchaseRequisition.PriceReasonablenessInfo,
                purchaseRequisition.Telephone,
                purchaseRequisition.MedianPriceAmount,
                purchaseRequisition.EvaluationCriteriaCode.ToString(),
                purchaseRequisition.DeliveryPeriod,
                purchaseRequisition.DeliveryPeriodTypeCode.ToString(),
                purchaseRequisition.DeliveryConditionCode.ToString(),
                purchaseRequisition.HasFineRate,
                purchaseRequisition.HasWarranty,
                purchaseRequisition.WarrantyPeriod,
                purchaseRequisition.WarrantyPeriodCode.ToString(),
                purchaseRequisition.WarrantyConditionCode.ToString(),
                purchaseRequisition.HasContractGuarantee,
                purchaseRequisition.HasInspectionCommittee,
                purchaseRequisition.HasConstructionSupervisor,
                lastedDocument?.FileId.Value,
                purchaseRequisition.IsReplacedDocument,
                purchaseRequisition.Status,
                purchaseRequisition.DeliveryDate,
                purchaseRequisition.DocumentDate),
            purchaseRequisition.Budgets.Select(b =>
                new GetPurchaseRequisitionBudget(
                    b.Id.Value,
                    b.Description,
                    b.BudgetAmount,
                    b.PpPurchaseRequisitionBudgetDetails
                     .OrderBy(x => x.Sequence)
                     .Select(d =>
                         new GetPurchaseRequisitionBudgetDetail(
                             d.Id.Value,
                             d.Sequence,
                             d.Department,
                             d.BudgetTypeCode.Value,
                             d.ProjectCode?.ToString(),
                             d.AccountNoCode.Value,
                             d.Budget)),
                    b.Sequence)),
            purchaseRequisition.Warranties.Select(w =>
                new GetPurchaseRequisitionWarranty(
                    w.Id.Value,
                    w.HasWarranty,
                    w.Period,
                    w.PeriodTypeCode?.ToString(),
                    w.ConditionOther)),
            purchaseRequisition.PaymentTerms.Select(pt =>
                new GetPurchaseRequisitionPaymentTerm(
                    pt.Id.Value,
                    pt.TermNumber,
                    pt.Percent,
                    pt.Period,
                    pt.Description ?? string.Empty,
                    pt.PaymentTypeCode != null ? pt.PaymentTypeCode.ToString() : null,
                    pt.TotalPeriodTypeCode != null ? pt.TotalPeriodTypeCode.ToString() : null,
                    pt.TotalPeriod ?? null,
                    pt.PeriodTypeCode != null ? pt.PeriodTypeCode.ToString() : null,
                    pt.IsMA)).OrderBy(x => x.TermNumber).ToArray(),
            purchaseRequisition.FineRates.OrderBy(x => x.Sequence).Select(fr =>
                new GetPurchaseRequisitionFineRate(
                    fr.Id.Value,
                    fr.Sequence,
                    fr.Rate,
                    fr.PeriodTypeCode.ToString(),
                    fr.ConditionCode.ToString(),
                    fr.ConditionOther)),
            purchaseRequisition.Committees
                               .OrderBy(o => o.Sequence)
                               .Select(c =>
                                   new GetPurchaseRequisitionCommittee(
                                       c.Id.Value,
                                       c.GroupType,
                                       c.SuUserId.Value,
                                       c.FullName,
                                       c.FullPositionName ?? string.Empty,
                                       c.CommitteePositionsCode.Value,
                                       c.CommitteePositionsName,
                                       c.Sequence,
                                       c.User.Employee.PrimaryDepartment?.Id.Value ?? string.Empty)),
            purchaseRequisition.Acceptors
                               .OrderBy(x => x.Sequence)
                               .Select(DelegatorExtensions.DelegatorToAcceptor)
                               .Select(x =>
                                   new PpPurchaseRequisitionAcceptorResponse(
                                       x.Id.Value,
                                       x.Type,
                                       x.UserId.Value,
                                       x.EmployeeCode.Value,
                                       x.FullName,
                                       x.PositionName,
                                       x.BusinessUnitName,
                                       x.Sequence,
                                       x.DelegateeId?.Value,
                                       x.Status,
                                       x.ActionAt,
                                       x.Remark,
                                       x.IsActive,
                                       PurchaseRequisitionEndpointBase<TRequest, TResponse>.CurrentAcceptor(purchaseRequisition.Acceptors, x.Id.Value, purchaseRequisition.Status),
                                       x.User?.Employee.PrimaryBusinessUnit?.Id.Value,
                                       x.Delegatee?.SuUserId.Value)),
            purchaseRequisition.Assignees
                               .OrderBy(x => x.Sequence)
                               .Select(DelegatorExtensions.DelegatorToAssignee)
                               .Select(x =>
                                   new AssigneeResponse(
                                       x.Id.Value,
                                       x.Group,
                                       x.Type,
                                       x.UserId.Value,
                                       x.Sequence,
                                       x.FullName,
                                       x.PositionName,
                                       x.BusinessUnitName,
                                       x.Status,
                                       x.Remark,
                                       x.ActionAt,
                                       x.Delegatee?.SuUserId.Value)),
            purchaseRequisition.TechnicalSpecifications
                               .OrderBy(x => x.Sequence)
                               .Select(x =>
                                   new GetTechnicalSpecification(
                                       x.Id.Value,
                                       x.Sequence,
                                       x.Name,
                                       x.Description,
                                       x.Quantity,
                                       x.UnitCode)),
            torDraft?.Reason,
            torDraft?.PpTorDraftObjects.OrderBy(x => x.Sequence).Select(s => new GetTorObjectResponse((int)s.Sequence, s.Description ?? string.Empty)) ?? [],
            purchaseRequisition.Committees
                               .Where(w => w.GroupType is GroupType.ProcurementCommittee)
                               .All(a => a.IsCommittee()),
            purchaseRequisition.Committees
                               .Where(w => w.GroupType is GroupType.InspectionCommittee)
                               .All(a => a.IsCommittee()),
            purchaseRequisition.Committees
                               .Where(w => w.GroupType is GroupType.MaintenanceInspectionCommittee)
                               .All(a => a.IsCommittee()),
            purchaseRequisition.Committees
                               .Where(w => w.GroupType is GroupType.ConstructionSupervisor)
                               .All(a => a.IsCommittee()),
            purchaseRequisition.AuditInfo.CreatedBy == userId,
            purchaseRequisition.SendEditRemark,
            purchaseRequisition.Procurement.Budget,
            purchaseRequisition.Procurement.IsCommercialMaterial,
            purchaseRequisition.Procurement.DepartmentId,
            purchaseRequisition.Procurement.SupplyMethodCode,
            documentVersions,
            torDraft?.DocumentTemplate?.Code,
            purchaseRequisition.PaymentTypeCode != null ? purchaseRequisition.PaymentTypeCode.Value.ToString() : null);
    }

    private static bool CurrentAcceptor(IEnumerable<PpPurchaseRequisitionAcceptors> acceptors, Guid acceptorId, PurchaseRequisitionStatus status)
    {
        if (status != PurchaseRequisitionStatus.WaitingApproval)
        {
            return false;
        }

        var requiredType = AcceptorType.Approver;

        var current = acceptors.FirstOrDefault(a =>
            a.Id.Value == acceptorId && a.Type == requiredType);

        if (current == null)
        {
            return false;
        }

        var prev = acceptors
                   .Where(a =>
                       a.Type == requiredType &&
                       a.Sequence < current.Sequence &&
                       a.IsActive)
                   .OrderByDescending(a => a.Sequence)
                   .FirstOrDefault();

        if (prev == null)
        {
            return current.Status != AcceptorStatus.Approved;
        }

        return prev.Status == AcceptorStatus.Approved;
    }

    protected Task<GetPurchaseRequisitionResponse?> GetByProcurementIdAsync(ProcurementId procurementId, CancellationToken ct)
    {
        return this.dbContext
                   .Procurements
                   .Include(i => i.TorDrafts)
                   .Include(m => m.MedianPrices)
                   .FirstOrDefaultAsync(p => p.Id == procurementId, ct)
                   .Map(MapJp004ById);
    }

    private static GetPurchaseRequisitionResponse? MapJp004ById(Procurement? procurement)
    {
        if (procurement.IsNull())
        {
            return null;
        }

        var torData = procurement!.TorDrafts.FirstOrDefault(t => t.IsActive);
        var medianPrice = procurement!.MedianPrices.FirstOrDefault(t => t.IsActive);
        var paymentTermMa = torData?.PpTorDraftPaymentTerms.FirstOrDefault(x => x.IsMA == true);

        var budgetDetails = ExtractBudgetDetails(torData);
        var paymentTermDetails = ExtractExistingPaymentTerms(torData);
        var paymentTermDetailsMa = ExtractExistingPaymentTermsMA(paymentTermMa);

        var sumPaymentTerm = paymentTermDetails?.OrderBy(x => x.TermNumber).ToArray() ?? [];

        if (paymentTermDetailsMa != null)
        {
            sumPaymentTerm = [.. sumPaymentTerm, paymentTermDetailsMa];
        }

        var technicalSpecs = ExtractTechnicalSpecifications(torData);
        var torObjects = ExtractTorObjects(torData);

        var paymentType = torData?.PpTorDraftPaymentTerms.FirstOrDefault(x => x.IsMA == false)?.ProRateTypeCode;

        return new GetPurchaseRequisitionResponse(
            null,
            procurement.Id,
            null,
            torData?.Id,
            null,
            false,
            CreatePurchaseRequisition(torData, medianPrice),
            budgetDetails ?? [],
            [],
            sumPaymentTerm,
            [],
            [],
            [],
            [],
            technicalSpecs ?? [],
            torData?.Reason,
            torObjects ?? [],
            true,
            true,
            true,
            true,
            false,
            string.Empty,
            procurement.Budget,
            procurement.IsCommercialMaterial,
            procurement.DepartmentId,
            procurement.SupplyMethodCode,
            [],
            torData?.DocumentTemplate?.Code,
            paymentType != null ? paymentType.Value.ToString() : null);
    }

    private static IEnumerable<GetPurchaseRequisitionBudget>? ExtractBudgetDetails(PpTorDraft? torData)
    {
        return torData?.PpTorDraftBudgets.OrderBy(x => x.Sequence)
                      .Select(s =>
                      {
                          var details = s.PpTorDraftBudgetDetails?
                              .Select(p =>
                                  new GetPurchaseRequisitionBudgetDetail(
                                      null,
                                      p.Sequence ?? 0,
                                      p.Department ?? string.Empty,
                                      p.BudgetType ?? string.Empty,
                                      p.ProjectCode,
                                      p.AccountNo ?? string.Empty,
                                      p.Budget ?? 0m)) ?? [];

                          return new GetPurchaseRequisitionBudget(
                              null,
                              s.Description ?? string.Empty,
                              s.BudgetAmount ?? 0m,
                              details,
                              s.Sequence ?? 0);
                      });
    }

    private static IEnumerable<GetPurchaseRequisitionPaymentTerm>? ExtractPaymentTermDetails(PpTorDraft? torData)
    {
        var firstTerm = torData?.PpTorDraftPaymentTerms.FirstOrDefault();

        if (HasNoPaymentTermDetails(firstTerm))
        {
            return CreateDefaultPaymentTerms(firstTerm);
        }

        return ExtractExistingPaymentTerms(torData);
    }

    private static bool HasNoPaymentTermDetails(PpTorDraftPaymentTerm? firstTerm)
    {
        return firstTerm?.PpTorDraftPaymentTermDetails == null ||
               !firstTerm.PpTorDraftPaymentTermDetails.Any();
    }

    private static IEnumerable<GetPurchaseRequisitionPaymentTerm> CreateDefaultPaymentTerms(PpTorDraftPaymentTerm? firstTerm)
    {
        if (firstTerm?.TotalPeriod > 0 && firstTerm.PaymentPercent > 0)
        {
            var totalPeriod = firstTerm.TotalPeriod.Value;
            var paymentPercent = firstTerm.PaymentPercent.Value;
            var periodDays = CalculatePeriodDays(firstTerm);

            return Enumerable.Range(1, totalPeriod)
                             .Select(i => new GetPurchaseRequisitionPaymentTerm(null, i, paymentPercent, periodDays, string.Empty, null, null, null, null, null));
        }

        return [];
    }

    private static int CalculatePeriodDays(PpTorDraftPaymentTerm? firstTerm)
    {
        var period = firstTerm?.Period ?? 0;
        var periodTypeMultiplier = GetPeriodTypeMultiplier(firstTerm?.PeriodTypeCode);

        return period * periodTypeMultiplier;
    }

    private static int GetPeriodTypeMultiplier(ParameterCode? periodTypeCode)
    {
        if (periodTypeCode is null)
        {
            return 0;
        }

        return periodTypeCode?.Value switch
        {
            PeriodTypeConstant.PeriodType001 => 1,
            PeriodTypeConstant.PeriodType002 => 30,
            _ => 365,
        };
    }

    private static IEnumerable<GetPurchaseRequisitionPaymentTerm>? ExtractExistingPaymentTerms(PpTorDraft? torData)
    {
        return torData?
               .PpTorDraftPaymentTerms
               .Where(p => p.IsMA == null || p.IsMA == false)
               .SelectMany(p =>
                   p.PpTorDraftPaymentTermDetails?
                       .Select(pp =>
                           new GetPurchaseRequisitionPaymentTerm(
                               null,
                               pp.TermNumber,
                               pp.Percent,
                               pp.Period > 0 ? pp.Period : p.Period,
                               pp.Description,
                               p.ProRateTypeCode != null ? p.ProRateTypeCode.ToString() : null,
                               p.TotalPeriodTypeCode != null ? p.TotalPeriodTypeCode.ToString() : null,
                               p.TotalPeriod ?? null,
                               p.PeriodTypeCode != null ? p.PeriodTypeCode.ToString() : null,
                               p.IsMA)) ?? []);
    }

    private static GetPurchaseRequisitionPaymentTerm? ExtractExistingPaymentTermsMA(PpTorDraftPaymentTerm? paymentTermMa)
    {
        if (paymentTermMa is null)
        {
            return null;
        }

        return new GetPurchaseRequisitionPaymentTerm(
            null,
            null,
            null,
            paymentTermMa?.Period,
            null,
            SplitPaymentConstant.SplitPayment002,
            paymentTermMa?.TotalPeriodTypeCode != null ? paymentTermMa.TotalPeriodTypeCode.ToString() : null,
            paymentTermMa?.TotalPeriod,
            paymentTermMa?.PeriodTypeCode != null ? paymentTermMa.PeriodTypeCode.ToString() : null,
            paymentTermMa?.IsMA);
    }

    private static IEnumerable<GetTechnicalSpecification>? ExtractTechnicalSpecifications(PpTorDraft? torData)
    {
        return torData?.PpTorDraftTechnicalSpecifications
                      .OrderBy(x => x.Sequence)
                      .Select(x => new GetTechnicalSpecification(
                          x.Id.Value,
                          (int)x.Sequence,
                          x.Name ?? string.Empty,
                          x.Description ?? string.Empty,
                          (int)x.Quantity,
                          x.UnitCode));
    }

    private static IEnumerable<GetTorObjectResponse>? ExtractTorObjects(PpTorDraft? torData)
    {
        return torData?.PpTorDraftObjects
                      .OrderBy(x => x.Sequence)
                      .Select(s => new GetTorObjectResponse((int)s.Sequence, s.Description ?? string.Empty));
    }

    private static GetPurchaseRequisition CreatePurchaseRequisition(PpTorDraft? torData, PpMedianPrice? medianPrice)
    {
        return new GetPurchaseRequisition(
            null,
            null,
            null,
            null,
            medianPrice?.PriceReasonablenessInfo,
            null,
            medianPrice?.BudgetAllocation.ReferenceMedianPrice,
            torData?.EvaluationCriteria,
            torData?.PpTorDraftTechnicalPeriods.FirstOrDefault()?.Period,
            (string?)torData?.PpTorDraftTechnicalPeriods.FirstOrDefault()?.PeriodTypeCode ?? null,
            (string?)torData?.PpTorDraftTechnicalPeriods.FirstOrDefault()?.PeriodConditionCode ?? null,
            false,
            false,
            0,
            null,
            null,
            false,
            false,
            false,
            null,
            null,
            PurchaseRequisitionStatus.Draft,
            torData?.PpTorDraftTechnicalPeriods.FirstOrDefault()?.DeliveryDate,
            null);
    }

    protected async Task<FileId?> UpdateDocumentHistoryAsync(
        PpPurchaseRequisition purchaseRequisition,
        FileId fileId,
        bool? isReplace = false,
        CancellationToken ct = default)
    {
        var latestHistory = purchaseRequisition.DocumentHistories
                                               .OrderVersions()
                                               .FirstOrDefault();

        if (latestHistory == null)
        {
            return null;
        }

        var newVersion = RunningDocumentVersion.IncrementDocumentVersion(
            latestHistory.Version,
            latestHistory.StatusState.ToString(),
            purchaseRequisition.Status.ToString());

        var documentService = this.Resolve<IDocumentService>();

        var copiedFileId = await documentService.CopyDocumentTemplateAsync(
            fileId,
            parentDirectory: $"{DocumentTemplateGroups.Jp04}/{purchaseRequisition.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
            cancellationToken: ct);

        if (!copiedFileId.HasValue)
        {
            return null;
        }

        purchaseRequisition.AddDocumentHistory(copiedFileId.Value, isReplace ?? false);

        var newHistory = purchaseRequisition.DocumentHistories
                                            .OrderVersions()
                                            .First(h => h.FileId == copiedFileId.Value);
        newHistory.SetVersion(newVersion);

        return copiedFileId.Value;
    }

    protected async Task<FileId> GetDocumentTemplateAsync(
        PpPurchaseRequisition purchaseRequisition,
        ParameterCode supplyMethodCode,
        CancellationToken ct)
    {
        var documentService = this.Resolve<IDocumentService>();

        var fileId = await documentService.GetDocumentTemplateAsync(
            dt =>
                dt.Group == DocumentTemplateGroups.Jp04 &&
                dt.SupplyMethodCode == supplyMethodCode,
            parentDirectory: $"{DocumentTemplateGroups.Jp04}/{purchaseRequisition.Id}_Reset_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
            cancellationToken: ct);

        return (FileId)fileId;
    }
}