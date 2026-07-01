namespace GHB.DP2.Domain.Procurement.PPrincipleApprovalRental;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.PPrincipleApproval;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PPrincipleApprovalRentalRoiPerfResultId
{
    public static PPrincipleApprovalRentalRoiPerfResultId New() => From(Guid.CreateVersion7());
}

public class PPrincipleApprovalRentalRoiPerfResult : AuditableEntity<PPrincipleApprovalRentalRoiPerfResultId>
{
    public override PPrincipleApprovalRentalRoiPerfResultId Id { get; init; }

    public int Sequence { get; private set; }

    public PerformanceResultGroup PerformanceResultGroup { get; private set; }

    public int Year { get; private set; }

    public decimal AccountActual { get; private set; }

    public decimal AccountGrowth { get; private set; }

    public decimal AmountTarget { get; private set; }

    public decimal AmountActual { get; private set; }

    public decimal AmountRate { get; set; }

    public decimal AmountGrowth { get; private set; }

    public virtual PPrincipleApprovalRental PPrincipleApprovalRental { get; init; }

    public PPrincipleApprovalRentalRoiPerfResult SetValues(
        decimal accountActual,
        decimal accountGrowth,
        decimal amountTarget,
        decimal amountActual,
        decimal amountRate,
        decimal amountGrowth)
    {
        this.AccountActual = accountActual;
        this.AccountGrowth = accountGrowth;
        this.AmountTarget = amountTarget;
        this.AmountActual = amountActual;
        this.AmountRate = amountRate;
        this.AmountGrowth = amountGrowth;

        return this;
    }

    public static PPrincipleApprovalRentalRoiPerfResult Create(
        int sequence,
        PerformanceResultGroup group,
        int year)
    {
        return new PPrincipleApprovalRentalRoiPerfResult
        {
            Id = PPrincipleApprovalRentalRoiPerfResultId.New(),
            Sequence = sequence,
            PerformanceResultGroup = group,
            Year = year,
        };
    }

    public PPrincipleApprovalRentalRoiPerfResult Update(
        int sequence,
        PerformanceResultGroup group,
        int year)
    {
        this.Sequence = sequence;
        this.PerformanceResultGroup = group;
        this.Year = year;

        return this;
    }
}