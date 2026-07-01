namespace GHB.DP2.Domain.Procurement.PPrincipleApproval;

using GHB.DP2.Domain.Common;
using Vogen;

public enum PerformanceResultGroup
{
    /// <summary>
    /// ผลการดำเนินการ - เงินฝากคงเหลือ
    /// </summary>
    DepositRemaining,

    /// <summary>
    /// ผลการดำเนินการ - สินเชื่อคงเหลือ
    /// </summary>
    LoanExisting,

    /// <summary>
    /// ผลการดำเนินการ - สินเชื่อปล่อยใหม่
    /// </summary>
    LoanNew,
}

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PPrincipleApprovalRoiPerfResultId
{
    public static PPrincipleApprovalRoiPerfResultId New() => From(Guid.CreateVersion7());
}

public class PPrincipleApprovalRoiPerfResult : AuditableEntity<PPrincipleApprovalRoiPerfResultId>
{
    public override PPrincipleApprovalRoiPerfResultId Id { get; init; }

    public int Sequence { get; private set; }

    public PerformanceResultGroup PerformanceResultGroup { get; private set; }

    public int Year { get; private set; }

    public decimal AccountActual { get; private set; }

    public decimal AccountGrowth { get; private set; }

    public decimal AmountTarget { get; private set; }

    public decimal AmountActual { get; private set; }

    public decimal AmountRate { get; set; }

    public decimal AmountGrowth { get; private set; }

    public virtual PPrincipleApproval PPrincipleApproval { get; init; }

    public PPrincipleApprovalRoiPerfResult SetValues(
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

    public static PPrincipleApprovalRoiPerfResult Create(
        int sequence,
        PerformanceResultGroup group,
        int year)
    {
        return new PPrincipleApprovalRoiPerfResult
        {
            Id = PPrincipleApprovalRoiPerfResultId.New(),
            Sequence = sequence,
            PerformanceResultGroup = group,
            Year = year,
        };
    }

    public PPrincipleApprovalRoiPerfResult Update(
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