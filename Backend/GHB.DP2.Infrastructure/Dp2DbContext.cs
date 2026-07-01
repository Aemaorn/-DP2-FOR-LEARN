namespace GHB.DP2.Infrastructure;

using Codehard.Common.DomainModel;
using Codehard.FileService.Contracts.ValueObjects;
using Codehard.Infrastructure.EntityFramework;
using FastEndpoints;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractAgreement.CaContractInvitation;
using GHB.DP2.Domain.ContractAmendment.CamCertificateRequisition;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment.CamContractAmendmentExtendChange;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment.CamContractAmendmentPoAddendum;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment.CamContractAmendmentPoSap;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment.CamContractAmendmentWaiveOrReducePenalty;
using GHB.DP2.Domain.ContractManagement.CaContractDraftVendorAmendment;
using GHB.DP2.Domain.ContractManagement.CaContractDraftVendorEdit;
using GHB.DP2.Domain.ContractManagement.CmContractGuaranteeReturn;
using GHB.DP2.Domain.ContractManagement.CmContractTermination;
using GHB.DP2.Domain.ContractManagement.CmDeliveryAcceptance;
using GHB.DP2.Domain.ContractManagement.ContractManagement;
using GHB.DP2.Domain.Procurement.PInvite;
using GHB.DP2.Domain.Procurement.PJp005;
using GHB.DP2.Domain.Procurement.PPettyCashReimbursement;
using GHB.DP2.Domain.Procurement.PPrincipleApproval;
using GHB.DP2.Domain.Procurement.PPrincipleApprovalRental;
using GHB.DP2.Domain.Procurement.PPurchaseOrder;
using GHB.DP2.Domain.Procurement.PPurchaseOrderApproval;
using GHB.DP2.Domain.Procurement.Pw119;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.AnnouncementInfo;
using GHB.DP2.Domain.Report.RpAuditAndRevenue;
using GHB.DP2.Domain.Report.RpContractCompletionByQuarter;
using GHB.DP2.Infrastructure.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class Dp2DbContext :
    DbContext,
    IDomainEventDbContext
{
    private readonly ILogger logger;

    public Dp2DbContext()
    {
    }

    public Dp2DbContext(
        DbContextOptions<Dp2DbContext> options,
        ILogger<Dp2DbContext> logger)
        : base(options)
    {
        this.logger = logger;
    }

    public DbSet<Domain.Raws.RawEmployee> RawEmployees { get; set; }

    public DbSet<Domain.Raws.RawBusinessUnit> RawBusinessUnits { get; set; }

    public DbSet<Domain.Raws.RawEmployeePosition> RawEmployeePositions { get; set; }

    public DbSet<Domain.Raws.RawPosition> RawPositions { get; set; }

    public DbSet<Domain.Raws.RawProvinces> RawProvinces { get; set; }

    public DbSet<Domain.Raws.RawDistrict> RawDistricts { get; set; }

    public DbSet<Domain.Raws.RawSubDistrict> RawSubDistricts { get; set; }

    public DbSet<Domain.SystemUtility.SuAuditLog> SuAuditLogs { get; set; }

    public DbSet<Domain.SystemUtility.SuUser> SuUsers { get; set; }

    public DbSet<Domain.SystemUtility.SuParameter> SuParameters { get; set; }

    public DbSet<Domain.SystemUtility.SuParameterGroup> SuParameterGroups { get; set; }

    public DbSet<Domain.SystemUtility.SuDocumentTemplate> SuDocumentTemplates { get; set; }

    public DbSet<Domain.SystemUtility.SuVendor> SuVendors { get; set; }

    public DbSet<Domain.SystemUtility.SuVendorAttachment> SuVendorAttachments { get; set; }

    public DbSet<Domain.SystemUtility.SuVendorShareholders> SuVendorShareholdersList { get; set; }

    public DbSet<Domain.SystemUtility.SuProgram> SuPrograms { get; set; }

    public DbSet<Domain.SystemUtility.SuVendorCheckHistory> SuVendorCheckHistories { get; set; }

    public DbSet<Domain.SystemUtility.SuRole> SuRoles { get; set; }

    public DbSet<Domain.SystemUtility.SuRoleProgram> SuRolePrograms { get; set; }

    public DbSet<Domain.SystemUtility.SuSection> SuSections { get; init; }

    public DbSet<Domain.SystemUtility.SuSectionApprover> SuSectionApprovers { get; init; }

    public DbSet<Domain.SystemUtility.SuDelegator> SuDelegators { get; set; }

    public DbSet<Domain.SystemUtility.SuDelegatee> SuDelegatees { get; set; }

    public DbSet<Domain.SystemUtility.SuDelegateeHistories> SuDelegateeHistories { get; set; }

    public DbSet<Domain.SystemUtility.SuSecretaryOwner> SuSecretaryOwners { get; set; }

    public DbSet<Domain.SystemUtility.SuSecretary> SuSecretaries { get; set; }

    public DbSet<Domain.SystemUtility.SuSecretaryAttachment> SuSecretaryAttachments { get; set; }

    public DbSet<ActivityLog> ActivityLogs { get; set; }

    public DbSet<Domain.Plan.Plan> Plans { get; set; }

    public DbSet<Domain.Plan.PlanAnnouncement> PlanAnnouncements { get; set; }

    // Dashboard
    public DbSet<Domain.Dashboard.ProcurementProgressSummary> ProcurementProgressSummaries { get; set; }

    public DbSet<Domain.Plan.PlanAnnouncementSelected> PlanAnnouncementSelecteds { get; set; }

    public DbSet<Domain.Procurement.Procurement> Procurements { get; set; }

    public DbSet<Domain.Procurement.ProcurementAttachment> ProcurementAttachments { get; set; }

    public DbSet<Domain.Procurement.ProcurementAttachmentInfo> ProcurementAttachmentInfos { get; set; }

    // Appoint section
    public DbSet<Domain.Procurement.PpAppoint.PpAppoint> PpAppoints { get; set; }

    public DbSet<Domain.Procurement.PpAppoint.PpAppointAcceptors> PpAppointAcceptors { get; set; }

    public DbSet<Domain.Procurement.PpAppoint.PpAppointTorDraftCommittee> PpAppointTorDraftCommittees { get; set; }

    public DbSet<Domain.Procurement.PpAppoint.PpAppointMedianPriceCommitteeDuties> PpAppointMedianPriceCommitteeDuties { get; set; }

    public DbSet<Domain.Procurement.PpAppoint.PpAppointMedianPriceCommittee> PpAppointMedianPriceCommittees { get; set; }

    public DbSet<Domain.Procurement.PpAppoint.PpAppointTorDraftCommitteeDuties> PpAppointTorDraftCommitteeDuties { get; set; }

    // Purchase Requisition section
    public DbSet<Domain.Procurement.PpPurchaseRequisition.PpPurchaseRequisition> PpPurchaseRequisitions { get; set; }

    public DbSet<Domain.Procurement.PpPurchaseRequisition.PpPurchaseRequisitionBudget> PpPurchaseRequisitionBudgets { get; set; }

    public DbSet<Domain.Procurement.PpPurchaseRequisition.PpPurchaseRequisitionBudgetDetail> PpPurchaseRequisitionBudgetDetails { get; set; }

    public DbSet<Domain.Procurement.PpPurchaseRequisition.PpPurchaseRequisitionWarranty> PpPurchaseRequisitionWarranties { get; set; }

    public DbSet<Domain.Procurement.PpPurchaseRequisition.PpPurchaseRequisitionPaymentTerm> PpPurchaseRequisitionPaymentTerms { get; set; }

    public DbSet<Domain.Procurement.PpPurchaseRequisition.PpPurchaseRequisitionFineRate> PpPurchaseRequisitionFineRates { get; set; }

    public DbSet<Domain.Procurement.PpPurchaseRequisition.PpPurchaseRequisitionCommittee> PpPurchaseRequisitionCommittees { get; set; }

    public DbSet<Domain.Procurement.PpPurchaseRequisition.PpPurchaseRequisitionAcceptors> PpPurchaseRequisitionAcceptors { get; set; }

    public DbSet<Domain.Procurement.PpPurchaseRequisition.PpPurchaseRequisitionTechnicalSpecifications> PpPurchaseRequisitionTechnicalSpecifications { get; set; }

    public DbSet<Domain.Procurement.PpTorDraft.PpTorDraft> PpTorDrafts { get; set; }

    public DbSet<Domain.Procurement.PpMedianPrice.PpMedianPrice> PpMedianPrices { get; set; }

    public DbSet<Domain.Procurement.PJp005.PJp005> PJp005S { get; set; }

    public DbSet<Domain.Procurement.PJp005.PJp005Committee> PJp005Committees { get; set; }

    public DbSet<Domain.Procurement.PJp005.PJp005CommitteeDuties> PJp005CommitteeDuties { get; set; }

    public DbSet<Domain.Procurement.PJp005.PJp005Acceptors> PJp005Acceptors { get; set; }

    // ChangeCommittee section
    public DbSet<Domain.Procurement.ChangeCommittee.CommitteeChanges> CommitteeChanges { get; set; }

    public DbSet<Domain.Procurement.ChangeCommittee.CommitteeChangeAcceptor> CommitteeChangeAcceptors { get; set; }

    public DbSet<Domain.Procurement.ChangeCommittee.CommitteeChangeAssignee> CommitteeChangeAssignees { get; set; }

    public DbSet<PPurchaseOrder> PJp006S { get; set; }

    public DbSet<PPurchaseOrderEntrepreneur> PJp006Entrepreneurs { get; set; }

    public DbSet<PPurchaseOrderEntrepreneurShareholders> PJp006EntrepreneurShareholders { get; set; }

    public DbSet<PPurchaseOrderPriceDetails> PJp006PriceDetails { get; set; }

    public DbSet<PInvite> PInvites { get; set; }

    public DbSet<PInvitedEntrepreneurs> PInvitedEntrepreneurs { get; set; }

    public DbSet<PPurchaseOrder> PPurchaseOrder { get; set; }

    public DbSet<PPurchaseOrderEntrepreneur> PPurchaseOrderEntrepreneurs { get; set; }

    public DbSet<PPurchaseOrderApproval> PPurchaseOrderApprovals { get; set; }

    public DbSet<PPurchaseOrderApprovalBudget> PPurchaseOrderApprovalBudgets { get; set; }

    public DbSet<PPurchaseOrderApprovalEntrepreneurs> PPurchaseOrderApprovalEntrepreneurs { get; set; }

    public DbSet<PPurchaseOrderApprovalContract> PPurchaseOrderApprovalContracts { get; set; }

    public DbSet<PPurchaseOrderApprovalCommittee> PPurchaseOrderApprovalCommittees { get; set; }

    public DbSet<CaContractDraft> CaContractDrafts { get; set; }

    public DbSet<CaContractInvitation> CaContractInvitations { get; set; }

    public DbSet<CaContractInvitationVendors> CaContractInvitationVendors { get; set; }

    public DbSet<CaContractInvitationVendorShareholders> CaContractInvitationVendorShareholders { get; set; }

    public DbSet<Domain.Procurement.Pw119.Pw119> Pw119s { get; set; }

    public DbSet<Domain.Procurement.P79Clause2.P79Clause2> P79Clause2s { get; set; }

    public DbSet<Domain.Procurement.PPettyCash.PPettyCash> PPettyCashs { get; set; }

    public DbSet<Domain.Procurement.PPettyCash.PPettyCashGLAccount> PPettyCashGLAccounts { get; set; }

    public DbSet<PPrincipleApproval> PPrincipleApprovals { get; set; }

    public DbSet<CmDeliveryAcceptance> CmDeliveryAcceptances { get; set; }

    public DbSet<CmDeliveryAcceptancePeriod> CmDeliveryAcceptancePeriods { get; set; }

    public DbSet<PPrincipleApprovalRental> PPrincipleApprovalRentals { get; set; }

    public DbSet<PPrincipleApprovalRentalEntrepreneurs> PPrincipleApprovalRentalEntrepreneurs { get; set; }

    public DbSet<PPrincipleApprovalRentalEntrepreneursShareholders> PPrincipleApprovalRentalEntrepreneursShareholders { get; set; }

    public DbSet<PPrincipleApprovalRentalEntrepreneursPriceDetails> PPrincipleApprovalRentalEntrepreneursPriceDetails { get; set; }

    public DbSet<CaContractDraftVendor> CaContractDraftVendors { get; set; }

    public DbSet<CaContractDraftVendorEdit> CaContractDraftVendorEdits { get; set; }

    public DbSet<CaContractDraftVendorAmendment> CaContractDraftVendorAmendments { get; set; }

    public DbSet<CaContractDraftVendorAmendmentAcceptor> CaContractDraftVendorAmendmentAcceptors { get; set; }

    public DbSet<ContractManagement> ContractManagements { get; set; }

    public DbSet<CmContractTermination> CmContractTerminations { get; set; }

    public DbSet<CmContractGuaranteeReturn> CmContractGuaranteeReturns { get; set; }

    public DbSet<ActivityLog> SuActivityLogs { get; set; }

    public DbSet<CamCertificateRequisition> CamCertificateRequisitions { get; set; }

    public DbSet<RpAuditAndRevenue> RpAuditAndRevenues { get; set; }

    public DbSet<RpAuditAndRevenueDetail> RpAuditAndRevenueDetails { get; set; }

    public DbSet<RpContractCompletionByQuarter> RpContractCompletionByQuarters { get; set; }

    public DbSet<RpContractCompletionByQuarterDetail> RpContractCompletionByQuarterDetails { get; set; }

    public DbSet<Domain.SystemUtility.SuNotification> SuNotifications { get; set; }

    public DbSet<Domain.Procurement.PExpenseDisbursement.PExpenseDisbursement> PExpenseDisbursements { get; set; }

    public DbSet<PPettyCashReimbursement> PPettyCashReimbursements { get; set; }

    public DbSet<PPettyCashReimbursementItems> PPettyCashReimbursementItems { get; set; }

    public DbSet<Pw119GLAccount> Pw119GlAccounts { get; set; }

    public DbSet<Domain.Procurement.Pw184.Pw184> Pw184s { get; set; }

    public DbSet<Domain.Procurement.Pw184.Pw184GLAccount> Pw184GlAccounts { get; set; }

    public DbSet<CamContractAmendmentPoAddendum> CamContractAmendmentPoAddendums { get; set; }

    public DbSet<CamContractAmendmentPoSap> CamContractAmendmentPoSaps { get; set; }

    public DbSet<CamContractAmendment> CamContractAmendments { get; set; }

    public DbSet<CamContractAmendmentWaiveOrReducePenalty> CamContractAmendmentWaiveOrReducePenalties { get; set; }

    public DbSet<CamContractAmendmentExtendChange> CamContractAmendmentExtendChanges { get; set; }

    public DbSet<AnnouncementInfo> AnnouncementInfos { get; set; }

    public DbSet<AnnouncementReport> AnnouncementReports { get; set; }

    public DbSet<AnnouncementSorKorRor> AnnouncementSorKorRors { get; set; }

    public DbSet<RawErmEmployee> RawErmEmployees { get; set; }

    public DbSet<PJp005ProcurementSuppliesDivision> ProcurementSuppliesDivision { get; set; }

    public async Task PublishDomainEventAsync(IDomainEvent domainEvent)
    {
        this.logger.LogInformation("Publishing domain event {@DomainEvent}", domainEvent);

        // Here you can implement the logic to publish the domain event, e.g., using an event bus.
        // For now, we just log the event and return a completed task.
        // If you have an event bus, you can use it to publish the event.
        if (domainEvent is IEvent @event)
        {
            this.logger.LogInformation("Event Type: {EventType}, Event Data: {@EventData}", @event.GetType().Name, @event);

            await @event.PublishAsync();
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(Dp2DbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<FileId>()
                            .HaveConversion<ValueObjectConverter<FileId>, ValueObjectComparer<FileId>>();

        base.ConfigureConventions(configurationBuilder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        optionsBuilder.AddDomainEventPublisherInterceptor();
    }
}