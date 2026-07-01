namespace GHB.DP2.Application.Features.WorkList.Infrastructure;

/// <summary>
/// Thread-safe container for accumulating worklist results from parallel queries.
/// Uses Interlocked for atomic count updates.
/// </summary>
internal sealed class ConcurrentWorklistResults
{
    private int plans;
    private int planAnnouncements;
    private int preProcurement;
    private int procurement;
    private int contractAgreement;
    private int contractManagement;
    private int expenseDisbursement;

    private SectionResult<PlanItem>? planSection;
    private SectionResult<PlanAnnouncementItem>? announcementSection;
    private SectionResult<ProcurementItem>? preProcurementSection;
    private SectionResult<ProcurementItem>? procurementSection;
    private SectionResult<ProcurementItem>? contractAgreementSection;
    private SectionResult<ContractManagementItem>? contractManagementSection;
    private SectionResult<ContractAmendmentItem>? contractAmendmentSection;
    private SectionResult<ExpenseDisbursementItem>? expenseDisbursementSection;

    public void SetPlanResult(int count, SectionResult<PlanItem>? section)
    {
        Interlocked.Exchange(ref this.plans, count);
        Interlocked.Exchange(ref this.planSection, section);
    }

    public void SetAnnouncementResult(int count, SectionResult<PlanAnnouncementItem>? section)
    {
        Interlocked.Exchange(ref this.planAnnouncements, count);
        Interlocked.Exchange(ref this.announcementSection, section);
    }

    public void SetPreProcurementResult(int count, SectionResult<ProcurementItem>? section)
    {
        Interlocked.Exchange(ref this.preProcurement, count);
        Interlocked.Exchange(ref this.preProcurementSection, section);
    }

    public void SetProcurementResult(int count, SectionResult<ProcurementItem>? section)
    {
        Interlocked.Exchange(ref this.procurement, count);
        Interlocked.Exchange(ref this.procurementSection, section);
    }

    public void SetContractAgreementResult(int count, SectionResult<ProcurementItem>? section)
    {
        Interlocked.Exchange(ref this.contractAgreement, count);
        Interlocked.Exchange(ref this.contractAgreementSection, section);
    }

    public void SetContractManagementResult(int count, SectionResult<ContractManagementItem>? section)
    {
        Interlocked.Exchange(ref this.contractManagement, count);
        Interlocked.Exchange(ref this.contractManagementSection, section);
    }

    public void SetContractAmendmentResult(SectionResult<ContractAmendmentItem>? section)
    {
        Interlocked.Exchange(ref this.contractAmendmentSection, section);
    }

    public void SetExpenseDisbursementResult(int count, SectionResult<ExpenseDisbursementItem>? section)
    {
        Interlocked.Exchange(ref this.expenseDisbursement, count);
        Interlocked.Exchange(ref this.expenseDisbursementSection, section);
    }

    public WorklistCounts ToCounts()
    {
        var combined = Volatile.Read(ref this.plans) +
                       Volatile.Read(ref this.planAnnouncements) +
                       Volatile.Read(ref this.preProcurement) +
                       Volatile.Read(ref this.procurement) +
                       Volatile.Read(ref this.contractAgreement) +
                       Volatile.Read(ref this.contractManagement) +
                       Volatile.Read(ref this.expenseDisbursement);

        return new WorklistCounts(
            Volatile.Read(ref this.plans),
            Volatile.Read(ref this.planAnnouncements),
            Volatile.Read(ref this.preProcurement),
            Volatile.Read(ref this.procurement),
            Volatile.Read(ref this.contractAgreement),
            Volatile.Read(ref this.contractManagement),
            Volatile.Read(ref this.expenseDisbursement),
            combined);
    }

    public WorklistSections ToSections()
    {
        return new WorklistSections
        {
            Plans = Volatile.Read(ref this.planSection),
            Announcements = Volatile.Read(ref this.announcementSection),
            PreProcurement = Volatile.Read(ref this.preProcurementSection),
            Procurement = Volatile.Read(ref this.procurementSection),
            ContractAgreement = Volatile.Read(ref this.contractAgreementSection),
            ContractManagement = Volatile.Read(ref this.contractManagementSection),
            ContractAmendments = Volatile.Read(ref this.contractAmendmentSection),
            ExpenseDisbursement = Volatile.Read(ref this.expenseDisbursementSection),
        };
    }
}