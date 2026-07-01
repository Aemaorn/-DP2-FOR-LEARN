namespace GHB.DP2.Domain.Dashboard;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Plan;
using Vogen;

public enum ProcurementProgressStatus
{
    OnPlan,
    Risk,
    Delay,
}

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct ProcurementProgressSummaryId
{
    public static ProcurementProgressSummaryId New() => From(Guid.CreateVersion7());
}

public class ProcurementProgressSummary : AuditableEntity<ProcurementProgressSummaryId>
{
    public override ProcurementProgressSummaryId Id { get; init; }

    public PlanId PlanId { get; private set; }

    public DateOnly? PlanDate { get; private set; }

    public DateOnly? PurchaseOrderDate { get; private set; }

    public DateOnly? DocPrepareNotifyDate { get; private set; }

    public DateOnly? ContractDate { get; private set; }

    public ProcurementProgressStatus? Status { get; private set; }

    public virtual Plan Plan { get; private set; } = null!;

    protected ProcurementProgressSummary() { }

    public static ProcurementProgressSummary Create(
        PlanId planId,
        DateOnly? planDate,
        DateOnly? purchaseOrderDate,
        DateOnly? docPrepareNotifyDate,
        DateOnly? contractDate)
    {
        return new ProcurementProgressSummary
        {
            Id = ProcurementProgressSummaryId.New(),
            PlanId = planId,
            PlanDate = planDate,
            PurchaseOrderDate = purchaseOrderDate,
            DocPrepareNotifyDate = docPrepareNotifyDate,
            ContractDate = contractDate,
        };
    }

    public void Update(
        DateOnly? planDate,
        DateOnly? purchaseOrderDate,
        DateOnly? docPrepareNotifyDate,
        DateOnly? contractDate,
        ProcurementProgressStatus? status = null)
    {
        this.PlanDate = planDate;
        this.PurchaseOrderDate = purchaseOrderDate;
        this.DocPrepareNotifyDate = docPrepareNotifyDate;
        this.ContractDate = contractDate;
        this.Status = status;
    }
}
