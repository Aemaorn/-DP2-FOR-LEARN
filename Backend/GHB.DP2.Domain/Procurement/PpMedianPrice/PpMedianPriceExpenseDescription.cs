namespace GHB.DP2.Domain.Procurement.PpMedianPrice;

public class PpMedianPriceExpenseDescription
{
    public MedianPriceId Id { get; init; }

    public decimal? MaterialCost { get; private set; }

    public decimal? OverseasTravelCost { get; private set; }

    public decimal? OtherExpenses { get; private set; }

    public decimal? HardwareCost { get; private set; }

    public decimal? SoftwareCost { get; private set; }

    public decimal? SystemDevelopmentCost { get; private set; }

    public virtual PpMedianPrice MedianPrice { get; init; }

    public PpMedianPriceExpenseDescription SetMaterialCost(decimal? materialCost)
    {
        this.MaterialCost = materialCost;

        return this;
    }

    public PpMedianPriceExpenseDescription SetOverseasTravelCost(decimal? overseasTravelCost)
    {
        this.OverseasTravelCost = overseasTravelCost;

        return this;
    }

    public PpMedianPriceExpenseDescription SetOtherExpenses(decimal? otherExpenses)
    {
        this.OtherExpenses = otherExpenses;

        return this;
    }

    public PpMedianPriceExpenseDescription SetHardwareCost(decimal? hardwareCost)
    {
        this.HardwareCost = hardwareCost;

        return this;
    }

    public PpMedianPriceExpenseDescription SetSoftwareCost(decimal? softwareCost)
    {
        this.SoftwareCost = softwareCost;

        return this;
    }

    public PpMedianPriceExpenseDescription SetSystemDevelopmentCost(decimal? systemDevelopmentCost)
    {
        this.SystemDevelopmentCost = systemDevelopmentCost;

        return this;
    }

    public static PpMedianPriceExpenseDescription Create()
    {
        return new PpMedianPriceExpenseDescription
        {
            MaterialCost = null,
            OverseasTravelCost = null,
            OtherExpenses = null,
            HardwareCost = null,
            SoftwareCost = null,
            SystemDevelopmentCost = null,
        };
    }
}