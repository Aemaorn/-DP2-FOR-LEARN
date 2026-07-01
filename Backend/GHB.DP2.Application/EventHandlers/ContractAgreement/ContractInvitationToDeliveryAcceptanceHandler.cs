namespace GHB.DP2.Application.EventHandlers.ContractAgreement;

using GHB.DP2.Application.Constants;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Features.ContractAgreement.ContractDraft.Dto;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Constants;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft.SectionType;
using GHB.DP2.Domain.ContractAgreement.CaContractInvitation;
using GHB.DP2.Domain.ContractAgreement.Event;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PJp005;
using GHB.DP2.Domain.Procurement.PpPurchaseRequisition;
using GHB.DP2.Domain.Procurement.PPrincipleApproval;
using GHB.DP2.Domain.Procurement.PpTorDraft;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Linq;

public record InitBypassAcceptanceCommitteeDto(
    int Sequence,
    SuUser User,
    ParameterCode? CommitteePositionsCode);

public class ContractInvitationToDeliveryAcceptanceHandler : IEventHandler<ContractInvitationToDeliveryAcceptanceEvent>
{
    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly ILogger<ContractInvitationToDeliveryAcceptanceHandler> logger;

    public ContractInvitationToDeliveryAcceptanceHandler(
        ILogger<ContractInvitationToDeliveryAcceptanceHandler> logger,
        IServiceScopeFactory serviceScopeFactory)
    {
        this.logger = logger;
        this.serviceScopeFactory = serviceScopeFactory;
    }

    public async Task HandleAsync(ContractInvitationToDeliveryAcceptanceEvent eventModel, CancellationToken ct)
    {
        this.logger.LogInformation(
            "Handling ContractInvitationToDeliveryAcceptanceEvent for ContractInvitationId: {ContractInvitationId}",
            eventModel.Id);

        await using var scope = this.serviceScopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Dp2DbContext>();

        // Step 1: Create Contract Draft (from ContractInvitationApproveHandler)
        var contractInvitation = await GetContractInvitationWithIncludesAsync(dbContext, eventModel.Id, ct);

        if (contractInvitation == null)
        {
            this.logger.LogWarning(
                "ContractInvitation with Id {ContractInvitationId} not found.",
                eventModel.Id);

            return;
        }

        var purchaseRequisition = await dbContext.PpPurchaseRequisitions.Include(ppPurchaseRequisition => ppPurchaseRequisition.PaymentTerms)
                                                 .FirstOrDefaultAsync(x => x.ProcurementId == contractInvitation.ProcurementId, ct);

        var torData = await dbContext.PpTorDrafts
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

        // Save the contract draft first to get its ID
        await dbContext.SaveChangesAsync(ct);

        this.logger.LogInformation(
            "Created new CaContractDraft for ContractInvitationId: {ContractInvitationId}",
            eventModel.Id);

        // Step 2: Create Delivery Acceptance (from ContractDraftApproveEventHandler)
        var intiAcceptanceCommittees =
            contractDraft.Procurement.Type == ProcurementType.Rent
                ? await GetAcceptanceCommitteeFromRental(dbContext, contractDraft.ProcurementId, ct)
                : await GetAcceptanceCommitteeFromJp005(dbContext, contractDraft.ProcurementId, ct);

        if (!intiAcceptanceCommittees.Any())
        {
            this.logger.LogWarning("ไม่พบข้อมูลคณะกรรมการตรวจรับพัสดุ สำหรับการจัดซื้อจัดจ้างแบบ {ProcurementType}", contractDraft.Procurement.Type);

            throw new InvalidOperationException("init Acceptance committees not found.");
        }

        await NotifyInspectionCommitteesAsync(contractInvitation, contractDraft.Id, intiAcceptanceCommittees, ct);

        this.logger.LogInformation(
            "Created DeliveryAcceptances for ContractDraft from ContractInvitationId: {ContractInvitationId}",
            eventModel.Id);
    }

    private static async Task NotifyInspectionCommitteesAsync(
        CaContractInvitation contractInvitation,
        ContractDraftId contractDraftId,
        InitBypassAcceptanceCommitteeDto[] committees,
        CancellationToken ct)
    {
        var procurementNumber = contractInvitation.Procurement.ProcurementNumber?.ToString() ?? string.Empty;
        var programName = ProgramConstant.ProcurementPurchaseOrderApproval.Name;
        var linkUrl = string.Format(ProgramConstant.ContractAcceptance.Url, string.Empty);
        var linkButton = ProgramConstant.ProcurementPurchaseOrderApproval.Button;

        foreach (var committee in committees)
        {
            await Notification
                  .Crate(
                      committee.User.Id,
                      NotificationConstant.InformInspectionCommitteeReadyForAcceptance.Title,
                      string.Format(
                          NotificationConstant.InformInspectionCommitteeReadyForAcceptance.Message,
                          programName,
                          procurementNumber),
                      NotificationProgram.ContractAgreement)
                  .SetReferenceId(contractDraftId.Value)
                  .SetLinkUrl(linkUrl, linkButton)
                  .PublishAsync(ct);
        }
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
                     .SetBuyer(defaultBuyer);

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

        var paymentBase = MapTorPaymenTerm(invitationVendor, purchaseRequisition, torData, isMAVendor);
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

    private static PaymentBase? MapTorPaymenTerm(CaContractInvitationVendors vendor, PpPurchaseRequisition purchaseRequisition, PpTorDraft? torDraft = null, bool isMAVendor = false)
    {
        var poPrice = vendor.PurchaseOrderApprovalContract
                            .AgreedPrice;

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
                    DeliveryDate = DateTimeOffset.UtcNow,
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
                Amount = (decimal)poPrice * (pp.Percent / 100m),
                AdvanceDeductionAmount = 0,
                DeliveryDate = DateTimeOffset.UtcNow,
                Description = pp.Description,
                LeadTime = pp.Period,
                PerformanceDeductionAmount = 0,
                Sequence = (int)pp.TermNumber,
                PeriodTypeCode = pp.PeriodTypeCode ?? ParameterCode.From("PeriodType001"),
            });
        }

        int? dueDay;
        string? paymentTypeCode;

        if (isMAVendor && termWithTotalPeriod != null)
        {
            dueDay = termWithTotalPeriod.Period;
            paymentTypeCode = termWithTotalPeriod.PaymentTypeCode?.ToString();
        }
        else
        {
            dueDay = firstTerm?.Period;
            paymentTypeCode = filteredPaymentTerms.FirstOrDefault()?.PaymentTypeCode.ToString();
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

    private static async Task<InitBypassAcceptanceCommitteeDto[]> GetAcceptanceCommitteeFromJp005(
        Dp2DbContext dbContext,
        ProcurementId procurementId,
        CancellationToken ct)
    {
        var jp005 = await dbContext.PJp005S
                                   .FirstOrDefaultAsync(w => w.ProcurementId == procurementId, ct);

        if (jp005 is null)
        {
            var orderApprovaCommittees = await dbContext.PPurchaseOrderApprovals
                                                        .Where(w => w.ProcurementId == procurementId)
                                                        .SelectMany(s => s.Committees)
                                                        .Include(u => u.User)
                                                        .Where(w => w.GroupType == Domain.Procurement.PPurchaseOrderApproval.GroupType.InspectionCommittee)
                                                        .OrderBy(o => o.Sequence)
                                                        .ToListAsync(ct);

            if (!orderApprovaCommittees.Any())
            {
                return [];
            }

            return
            [
                .. orderApprovaCommittees
                    .Select(a =>
                        new InitBypassAcceptanceCommitteeDto(
                            a.Sequence,
                            a.User,
                            a.CommitteePositionsCode))
            ];
        }

        var committees =
            await dbContext.PJp005S
                           .Include(f => f.Committees)
                           .ThenInclude(c => c.User)
                           .Where(w => w.ProcurementId == procurementId)
                           .SelectMany(s => s.Committees)
                           .Where(w => w.GroupType == PJp005CommitteeGroupType.InspectionCommittee)
                           .OrderBy(o => o.Sequence)
                           .ToListAsync(ct);

        if (!committees.Any())
        {
            return [];
        }

        return
        [
            .. committees
                .Select(a =>
                    new InitBypassAcceptanceCommitteeDto(
                        a.Sequence,
                        a.User,
                        a.CommitteePositionsCode))
        ];
    }

    private static async Task<InitBypassAcceptanceCommitteeDto[]> GetAcceptanceCommitteeFromRental(
        Dp2DbContext dbContext,
        ProcurementId procurementId,
        CancellationToken ct)
    {
        var committees =
            await dbContext.PPrincipleApprovals
                           .Include(c => c.PrincipleApprovalCommittees)
                           .ThenInclude(c => c.User)
                           .Where(w => w.ProcurementId == procurementId)
                           .SelectMany(s => s.PrincipleApprovalCommittees)
                           .Where(w => w.GroupType == CommitteeGroupType.AcceptanceCommittee)
                           .OrderBy(o => o.Sequence)
                           .ToArrayAsync(ct);

        if (!committees.Any())
        {
            return [];
        }

        return
        [
            .. committees
                .Select(a =>
                    new InitBypassAcceptanceCommitteeDto(
                        a.Sequence,
                        a.User,
                        a.CommitteePositionsCode))
        ];
    }
}