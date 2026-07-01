namespace GHB.DP2.Application.EventHandlers.ContractAgreement;

using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Features.ContractAgreement.ContractDraft.Dto;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Constants;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft.SectionType;
using GHB.DP2.Domain.ContractAgreement.CaContractInvitation;
using GHB.DP2.Domain.ContractAgreement.Event;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PpPurchaseRequisition;
using GHB.DP2.Domain.Procurement.PpTorDraft;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public class ContractInvitationApproveHandler : IEventHandler<ContractInvitationApproveEvent>
{
    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly ILogger<ContractInvitationApproveHandler> logger;

    public ContractInvitationApproveHandler(
        ILogger<ContractInvitationApproveHandler> logger,
        IServiceScopeFactory serviceScopeFactory)
    {
        this.logger = logger;
        this.serviceScopeFactory = serviceScopeFactory;
    }

    public async Task HandleAsync(ContractInvitationApproveEvent eventModel, CancellationToken ct)
    {
        this.logger.LogInformation(
            "Handling ContractInvitationApproveEvent for ContractInvitationId: {ContractInvitationId}",
            eventModel.Id);

        await using var scope = this.serviceScopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Dp2DbContext>();

        var contractInvitation = await GetContractInvitationWithIncludesAsync(dbContext, eventModel.Id, ct);

        if (contractInvitation == null)
        {
            this.logger.LogWarning(
                "ContractInvitation with Id {ContractInvitationId} not found.",
                eventModel.Id);

            return;
        }

        var purchaseRequisition = await dbContext.PpPurchaseRequisitions
                                                 .Include(x => x.PaymentTerms)
                                                 .FirstOrDefaultAsync(x => x.ProcurementId == contractInvitation.ProcurementId, ct);

        var torData = await dbContext.PpTorDrafts
                                     .Include(t => t.PpTorTemplateComputer)
                                     .Include(t => t.PpTorDraftWarranties)
                                     .Include(t => t.PpTorDraftFineRates)
                                     .Include(t => t.DocumentTemplate)
                                     .FirstOrDefaultAsync(x => x.ProcurementId == contractInvitation.ProcurementId, ct);

        var jorPorSectionHead =
            await dbContext.RawEmployeePositions
                           .Where(p =>
                               p.BusinessUnitId == BusinessUnitId.From(JorPor.DefaultSectionHead.BusinessUnitId) &&
                               p.Position.Name == JorPor.DefaultSectionHead.PositionName)
                           .Select(p => p.Employee)
                           .SelectMany(e => e.Users)
                           .FirstOrNoneAsync(ct);

        var contractDraft = CaContractDraft.Create(contractInvitation.ProcurementId);

        var createContractDraftNumber = await GenerateContractDraftNumberAsync(dbContext, contractInvitation.Procurement.BudgetYear, ct);

        var vendorList = contractInvitation.Vendors.ToList();
        var vendors = vendorList
                      .Select((v, index) => CreateContractDraftVendor(
                          v,
                          createContractDraftNumber,
                          purchaseRequisition,
                          torData,
                          jorPorSectionHead,
                          isMAVendor: vendorList.Count > 1 && index == 1))
                      .ToList();

        foreach (var vendor in vendors)
        {
            contractDraft.AddVendor(vendor);
        }

        await this.UpdateProcurementStepAsync(dbContext, contractInvitation.ProcurementId, eventModel.Id, ct);

        dbContext.CaContractDrafts.Add(contractDraft);

        await dbContext.SaveChangesAsync(ct);

        this.logger.LogInformation(
            "Created new CaContractDraft for ContractInvitationId: {ContractInvitationId}",
            eventModel.Id);
    }

    private async Task UpdateProcurementStepAsync(
        Dp2DbContext dbContext,
        ProcurementId procurementId,
        ContractInvitationId contractInvitationId,
        CancellationToken ct)
    {
        var procurement = await dbContext.Procurements
                                         .FirstOrDefaultAsync(p => p.Id == procurementId, ct);

        if (procurement == null)
        {
            this.logger.LogWarning(
                "Procurement with Id {ProcurementId} not found for ContractInvitationId: {ContractInvitationId}",
                procurementId.Value,
                contractInvitationId);

            return;
        }

        var purchaseOrderApproval = await dbContext.PPurchaseOrderApprovals
                                                   .FirstOrDefaultAsync(p => p.ProcurementId == procurementId, ct);

        if (purchaseOrderApproval?.ContractType == PurchaseOrderApprovalContractType.Contract41)
        {
            procurement.SetProcurementStep(procurement.Type, ProcurementStep.ContractAgreement)
                       .SetProcessType(ProcessType.ContractDraft);
        }

        dbContext.Procurements.Update(procurement);
    }

    private static async Task<ContractDraftNumber> GenerateContractDraftNumberAsync(Dp2DbContext dbContext, int? budgetYear, CancellationToken ct)
    {
        var lastCa = await dbContext.CaContractDrafts
                                    .Where(c => c.Procurement.BudgetYear == budgetYear)
                                    .SelectMany(c => c.Vendors)
                                    .OrderByDescending(c => c.ContractDraftNumber)
                                    .FirstOrDefaultAsync(ct);

        return lastCa.IsNull()
            ? ContractDraftNumber.New(budgetYear)
            : lastCa!.ContractDraftNumber.Next();
    }

    private static async Task<CaContractInvitation?> GetContractInvitationWithIncludesAsync(
        Dp2DbContext dbContext,
        ContractInvitationId contractInvitationId,
        CancellationToken ct)
    {
        return await dbContext
                     .CaContractInvitations
                     .Include(ci => ci.Vendors)
                     .ThenInclude(v => v.PurchaseOrderApprovalContract)
                     .ThenInclude(poac => poac.Entrepreneur)
                     .ThenInclude(e => e!.PJp006PriceDetails)
                     .Include(ci => ci.Vendors)
                     .ThenInclude(v => v.PurchaseOrderApprovalContract)
                     .ThenInclude(poac => poac.PrincipleApprovalRentalEntrepreneurs)
                     .ThenInclude(pare => pare!.Vendor)
                     .Include(ci => ci.Procurement)
                     .ThenInclude(p => p.PrincipleApprovals)
                     .ThenInclude(pa => pa.RentTypeCodeInfo)
                     .AsSplitQuery()
                     .SingleOrDefaultAsync(c => c.Id == contractInvitationId, ct);
    }

    private static CaContractDraftVendor CreateContractDraftVendor(
        CaContractInvitationVendors invitationVendor,
        ContractDraftNumber contractDraftNumber,
        PpPurchaseRequisition? purchaseRequisition,
        PpTorDraft? torData,
        Option<SuUser> jorPorSectionHead,
        bool isMAVendor = false)
    {
        var vendorId = GetVendorId(invitationVendor);
        var defaultBuyer = CreateDefaultBuyer();

        var vendor = CaContractDraftVendor
                     .Create(invitationVendor.Id, vendorId)
                     .SetEmail(invitationVendor.Email)
                     .SetContractName(invitationVendor.ContractName)
                     .SetPoNumber(invitationVendor.PoNumber)
                     .SetContractNumber(invitationVendor.ContractNumber)
                     .SetContractDraftNumber(contractDraftNumber)
                     .SetBudget(invitationVendor.AgreedPrice)
                     .SetBuyer(defaultBuyer)
                     .SetContractStatus(ContractStatus.Draft);

        if (!torData.IsNull() && torData!.PpTorDraftTechnicalPeriods.Any())
        {
            var firstPeriod = torData.PpTorDraftTechnicalPeriods.FirstOrDefault();

            if (firstPeriod != null)
            {
                var compareCode = ParameterCode.From("DelvCUnit005");

                if (firstPeriod.DeliveryConditionCode != null && firstPeriod.DeliveryConditionCode.Equals(compareCode))
                {
                    vendor.SetPeriodConditionType("CSDPCond003");

                    if (firstPeriod.DeliveryDate != default)
                    {
                        vendor.SetStartDate(firstPeriod.DeliveryDate);
                        vendor.SetEndDate(firstPeriod.DeliveryDate);
                    }
                }
                else
                {
                    vendor.SetPeriodConditionType("CSDPCond002");
                }
            }
        }

        if (invitationVendor.Shareholders.Any())
        {
            var shareholders = invitationVendor.Shareholders.Select(s =>
                CaContractDraftVendorShareholders
                    .Create(
                        s.Sequence,
                        s.TaxId,
                        s.FirstName,
                        s.LastName,
                        s.IsDirector,
                        s.IsShareholder,
                        s.IsJuristic)
                    .SetCheckType(s.CheckType)).ToList();

            vendor.AddCaContractDraftVendorShareholderList(shareholders);
        }

        SetPaymentTerms(vendor, invitationVendor, purchaseRequisition, torData, isMAVendor);
        SetTemplateForRentType(vendor, invitationVendor);
        AddAcceptor(vendor, jorPorSectionHead);
        SetWarrantyFromTor(vendor, torData);
        SetContractInfoFromPrincipleApprovalRental(vendor, invitationVendor);
        SetFineTerms(vendor, torData, isMAVendor);

        return vendor;
    }

    private static SuVendorId GetVendorId(CaContractInvitationVendors invitationVendor)
    {
        var contract = invitationVendor.PurchaseOrderApprovalContract;

        if (contract.Entrepreneur is not null)
        {
            return Optional(contract.Entrepreneur)
                   .Map(e => e.SuVendorId)
                   .IfNone(() => contract.PrincipleApprovalRentalEntrepreneurs!.Vendor.Id);
        }

        return Optional(contract.PPurchaseOrderApprovalEntrepreneurs)
               .Map(e => e.Vendor.Id)
               .IfNone(() => contract.PrincipleApprovalRentalEntrepreneurs!.Vendor.Id);
    }

    private static Buyer CreateDefaultBuyer()
    {
        return new Buyer(
            "ธนาคารอาคารสงเคราะห์",
            "ธนาคารอาคารสงเคราะห์ สำนักงานใหญ่ ตั้งอยู่เลขที่ 63 ถนนพระราม 9",
            new LocationInfo("1", "กรุงเทพมหานคร"),
            new LocationInfo("1017", "ห้วยขวาง"),
            new LocationInfo("101701", "ห้วยขวาง"));
    }

    private static void SetFineTerms(
        CaContractDraftVendor vendor,
        PpTorDraft? torData,
        bool isMaVendor)
    {
        if (torData == null)
        {
            return;
        }

        if (!torData.PpTorDraftFineRates.Any())
        {
            return;
        }

        var allowList = new List<string>()
        {
            TorDocumentTemplatesConstant.TorBuyWithHire60,
            TorDocumentTemplatesConstant.TorBuyWithHire80,
            TorDocumentTemplatesConstant.TorHireWithHire60,
            TorDocumentTemplatesConstant.TorHireWithHire80,
        };

        if (!allowList.Contains(torData.DocumentTemplate?.Code ?? string.Empty))
        {
            return;
        }

        var (first, last) = (torData.PpTorDraftFineRates.FirstOrDefault(), torData.PpTorDraftFineRates.LastOrDefault());

        if (!isMaVendor && first != null)
        {
            var amount = first.Rate > 0 ? (vendor.Budget / 100) * first.Rate : 0;

            var newPenalty = new Penalty(
                isPenalty: true,
                typeCode: first.ConditionCode,
                amount: (decimal)amount,
                rate: (decimal)first.Rate,
                rateTypeCode: first.PeriodTypeCode);

            vendor.SetPenalty(newPenalty);
        }

        if (isMaVendor && torData.PpTorDraftFineRates.Count > 1 && last != null)
        {
            var amount = last.Rate > 0 ? (vendor.Budget / 100) * last.Rate : 0;

            var newPenalty = new Penalty(
                isPenalty: true,
                typeCode: last.ConditionCode,
                amount: (decimal)amount,
                rate: (decimal)last.Rate,
                rateTypeCode: last.PeriodTypeCode);

            vendor.SetPenalty(newPenalty);
        }
    }

    private static void SetPaymentTerms(
        CaContractDraftVendor vendor,
        CaContractInvitationVendors invitationVendor,
        PpPurchaseRequisition? purchaseRequisition,
        PpTorDraft? torData,
        bool isMAVendor = false)
    {
        if (purchaseRequisition == null)
        {
            return;
        }

        var paymentBase = MapTorPaymentTerm(invitationVendor, purchaseRequisition, torData, isMAVendor);
        paymentBase?.MapToEntity(vendor);
    }

    private static void SetTemplateForRentType(
        CaContractDraftVendor vendor,
        CaContractInvitationVendors invitationVendor)
    {
        if (invitationVendor.ContractInvitation.Procurement.Type != ProcurementType.Rent)
        {
            return;
        }

        var principleApproval = invitationVendor.ContractInvitation.Procurement.PrincipleApprovals.FirstOrDefault();

        if (principleApproval is null)
        {
            return;
        }

        var templateType = principleApproval.RentTypeCodeInfo.Values.GetValueOrDefault("TemplateType")?.Value;

        if (templateType != null)
        {
            var templateCode = templateType.ToString();

            if (!string.IsNullOrWhiteSpace(templateCode))
            {
                vendor.SetTemplate(templateCode);
            }
        }
    }

    private static void AddAcceptor(CaContractDraftVendor vendor, Option<SuUser> jorPorSectionHead)
    {
        jorPorSectionHead
            .Map(user => CaContractDraftAcceptor.Create(user, AcceptorType.Approver, 1))
            .Iter(v => vendor.AddAcceptor(v));
    }

    private static void SetWarrantyFromTor(CaContractDraftVendor vendor, PpTorDraft? torData)
    {
        var warrantyPeriod = torData?.PpTorDraftWarranties;
        var templateComputer = torData?.PpTorTemplateComputer;
        var cm = templateComputer?.CorrectiveMaintenance;
        var pm = templateComputer?.PreventiveMaintenance;

        if (cm == null && pm == null && (warrantyPeriod == null || !warrantyPeriod.Any()))
        {
            return;
        }

        // Map PpTorDraftWarranties => warrantyPeriod (Year / Month / Day)
        int? warrantyPeriodYear = null;
        int? warrantyPeriodMonth = null;
        int? warrantyPeriodDay = null;
        var hasWarranty = warrantyPeriod?.Any(w => (bool)w.HasWarranty) == true;

        if (hasWarranty && warrantyPeriod != null)
        {
            foreach (var w in warrantyPeriod)
            {
                if (w.PeriodTypeCode == ParameterCode.From("PeriodType003"))
                {
                    warrantyPeriodYear = (warrantyPeriodYear ?? 0) + w.Period;
                }
                else if (w.PeriodTypeCode == ParameterCode.From("PeriodType002"))
                {
                    warrantyPeriodMonth = (warrantyPeriodMonth ?? 0) + w.Period;
                }
                else if (w.PeriodTypeCode == ParameterCode.From("PeriodType001"))
                {
                    warrantyPeriodDay = (warrantyPeriodDay ?? 0) + w.Period;
                }
            }
        }

        var warrantyPeriodInfo = (warrantyPeriodYear != null || warrantyPeriodMonth != null || warrantyPeriodDay != null)
            ? new RentalDurationInfo(warrantyPeriodYear, warrantyPeriodMonth, warrantyPeriodDay)
            : null;

        int? warrantyMaintenanceCount = null;
        ParameterCode? warrantyMaintenanceTypeCode = null;
        int? downtimeResolutionDay = null;
        int? downtimeResolutionHours = null;
        int? repairCompletionDay = null;
        int? repairCompletionHours = null;
        decimal? repairDelayPenaltyPercentPerHour = null;

        if (cm != null)
        {
            if (cm.CmUnit == ParameterCode.From("PeriodType001"))
            {
                downtimeResolutionDay = cm.CmCount;
            }
            else if (cm.CmUnit == ParameterCode.From("PeriodType005"))
            {
                downtimeResolutionHours = cm.CmCount;
            }

            if (cm.CmCompleteUnit == ParameterCode.From("PeriodType001"))
            {
                repairCompletionDay = cm.CmCompleteCount;
            }
            else if (cm.CmCompleteUnit == ParameterCode.From("PeriodType005"))
            {
                repairCompletionHours = cm.CmCompleteCount;
            }

            repairDelayPenaltyPercentPerHour = cm.CmFinePercent;
        }

        int? warrantyMonthlyAllowedDowntimeHours = null;
        decimal? warrantyDowntimePercentPerMonth = null;
        decimal? warrantyPenaltyPerHour = null;
        decimal? maxMonthlyMalfunctionPenaltyPercentageRate = null;

        if (pm != null)
        {
            warrantyMaintenanceCount = pm.PmCount;
            warrantyMaintenanceTypeCode = pm.PmUnit;

            if (pm.DisruptedCountUnit == ParameterCode.From("PTimeType001"))
            {
                warrantyMonthlyAllowedDowntimeHours = pm.DisruptedCount;
            }
            else if (pm.DisruptedCountUnit == ParameterCode.From("PTimeType002"))
            {
                warrantyMonthlyAllowedDowntimeHours = pm.DisruptedCount / 60;
            }

            warrantyDowntimePercentPerMonth = pm.DisruptedPercent;
            warrantyPenaltyPerHour = pm.DisruptedFineAmount;
            maxMonthlyMalfunctionPenaltyPercentageRate = pm.DisruptedFinePercent;
        }

        var warranty = new Warranty(
            hasWarranty: hasWarranty,
            warrantyConditionCode: null,
            warrantyPeriod: warrantyPeriodInfo,
            fixingDeadlinePeriod: null,
            warrantyMonthlyAllowedDowntimeHours: warrantyMonthlyAllowedDowntimeHours,
            warrantyDowntimePercentPerMonth: warrantyDowntimePercentPerMonth,
            warrantyPenaltyPerHour: warrantyPenaltyPerHour,
            downtimeResolutionHours: downtimeResolutionHours,
            downtimeResolutionDay: downtimeResolutionDay,
            repairCompletionHours: repairCompletionHours,
            repairCompletionDay: repairCompletionDay,
            repairDelayPenaltyPercentPerHour: repairDelayPenaltyPercentPerHour,
            maxMonthlyMalfunction: null,
            maxMonthlyMalfunctionTypeCode: null,
            maxMonthlyMalfunctionRate: null,
            maxMonthlyMalfunctionPenaltyPercentageRate: maxMonthlyMalfunctionPenaltyPercentageRate,
            maxMonthlyMalfunctionPenaltyPerHour: null,
            maxMonthlyMalfunctionPenaltyDueDays: null,
            warrantyStartDate: null,
            warrantyEndDate: null,
            warrantyMaintenanceTypeCode: warrantyMaintenanceTypeCode,
            warrantyMaintenanceCount: warrantyMaintenanceCount);

        vendor.SetWarranty(warranty);
    }

    private static PaymentBase? MapTorPaymentTerm(CaContractInvitationVendors vendor, PpPurchaseRequisition purchaseRequisition, PpTorDraft? torDraft = null, bool isMAVendor = false)
    {
        var poPrice = vendor.PurchaseOrderApprovalContract.AgreedPrice;

        var firstTerm = torDraft?.PpTorDraftPaymentTerms.FirstOrDefault();

        var filteredPaymentTerms = isMAVendor
            ? purchaseRequisition.PaymentTerms.Where(pp => pp.IsMA == true).ToList()
            : purchaseRequisition.PaymentTerms.Where(pp => pp.IsMA != true).ToList();

        if (!filteredPaymentTerms.Any())
        {
            return null;
        }

        IEnumerable<PaymentTermDetail> paymentTermDetailsTerm;

        var termWithTotalPeriod = filteredPaymentTerms
            .FirstOrDefault(pp => pp.TotalPeriod.HasValue && pp.TotalPeriod > 0 && pp.TotalPeriodTypeCode != null);

        if (termWithTotalPeriod != null)
        {
            var totalPeriod = termWithTotalPeriod.TotalPeriod!.Value;
            var basePercent = Math.Floor(10000m / totalPeriod) / 100m;
            var remainder = 100m - (basePercent * totalPeriod);

            paymentTermDetailsTerm = Enumerable.Range(1, totalPeriod).Select(i =>
            {
                var percent = i == totalPeriod ? basePercent + remainder : basePercent;

                return new PaymentTermDetail
                {
                    No = i,
                    InstallmentPercentage = percent,
                    Amount = (decimal)poPrice * (percent / 100m),
                    AdvanceDeductionAmount = 0,
                    DeliveryDate = null,
                    Description = termWithTotalPeriod.Description,
                    LeadTime = termWithTotalPeriod.Period * i,
                    PerformanceDeductionAmount = 0,
                    Sequence = i,
                    PeriodTypeCode = termWithTotalPeriod.PeriodTypeCode ?? ParameterCode.From("PeriodType001"),
                };
            });
        }
        else
        {
            paymentTermDetailsTerm = filteredPaymentTerms.Select(pp => new PaymentTermDetail
            {
                No = pp.TermNumber,
                InstallmentPercentage = pp.Percent,
                Amount = poPrice * (pp.Percent / 100m),
                AdvanceDeductionAmount = 0,
                DeliveryDate = null,
                Description = pp.Description,
                LeadTime = pp.Period,
                PerformanceDeductionAmount = 0,
                Sequence = pp.TermNumber ?? 0,
                PeriodTypeCode = pp.PeriodTypeCode ?? ParameterCode.From("PeriodType001"),
            });
        }

        int? dueDay;
        string? paymentTypeCode;

        if (isMAVendor && termWithTotalPeriod != null)
        {
            dueDay = termWithTotalPeriod.Period;
            paymentTypeCode = "PayType001";
        }
        else
        {
            dueDay = firstTerm?.Period;
            paymentTypeCode = firstTerm?.ProRateTypeCode == ParameterCode.From("SplitPayment002") ? "PayType002" : "PayType001";
        }

        var term = new Term
        {
            DueDay = dueDay,
            PaymentTypeCode = paymentTypeCode,
            RedeliveryTypeCode = null,
            Details = [.. paymentTermDetailsTerm],
        };

        return term;
    }

    private static void SetContractInfoFromPrincipleApprovalRental(
        CaContractDraftVendor vendor,
        CaContractInvitationVendors invitationVendor)
    {
        if (invitationVendor.ContractInvitation.Procurement.Type != ProcurementType.Rent)
        {
            return;
        }

        var principleApproval = invitationVendor.ContractInvitation.Procurement.PrincipleApprovals.FirstOrDefault();

        if (principleApproval is null)
        {
            return;
        }

        var rentalDuration = new RentalDurationInfo(
            principleApproval.RentalDurationYear,
            principleApproval.RentalDurationMonth,
            principleApproval.RentalDurationDay);

        var agreement = AgreementContract.Create(
            AgreementType.RentalDuration,
            vendor.Agreement?.ContractItem ?? string.Empty,
            null,
            null,
            null,
            null);

        agreement
            .SetRentalDuration(rentalDuration)
            .SetStartAndEndDate(principleApproval.RentalStartDate, principleApproval.RentalEndDate);

        vendor.SetAgreement(agreement);

        var guarantee = new Guarantee(
            isSubmitted: invitationVendor.HasContractGuarantee,
            typeCode: null,
            amount: invitationVendor.GuaranteeAmount,
            percentage: invitationVendor.ContractGuaranteePercent,
            referenceNumber: null,
            bankCode: null,
            bankBranch: null,
            bankAccountNumber: null,
            bankCollateralStartDate: null,
            bankCollateralEndDate: null,
            guaranteeDate: null,
            otherDetails: null);

        vendor.SetGuarantee(guarantee);
    }
}