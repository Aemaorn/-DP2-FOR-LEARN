namespace GHB.DP2.Application.Features.Procurement.MedianPrice.Dto;

using System.ComponentModel;
using GHB.DP2.Domain.Procurement.PpMedianPrice;

public record MedianPriceExpenseDescriptionInfo(
    [property: Description("ค่าวัสดุ")]
    decimal? MaterialCost,
    [property: Description("ค่าเดินทางต่างประเทศ")]
    decimal? OverseasTravelCost,
    [property: Description("ค่าใช้จ่ายอื่นๆ")]
    decimal? OtherExpenses,
    [property: Description("ค่าฮาร์ดแวร์")]
    decimal? HardwareCost,
    [property: Description("ค่าซอฟต์แวร์")]
    decimal? SoftwareCost,
    [property: Description("ค่าพัฒนาระบบ")]
    decimal? SystemDevelopmentCost)
{
    public PpMedianPriceExpenseDescription MapToEntity()
    {
        return PpMedianPriceExpenseDescription
               .Create()
               .SetMaterialCost(this.MaterialCost)
               .SetOverseasTravelCost(this.OverseasTravelCost)
               .SetOtherExpenses(this.OtherExpenses)
               .SetHardwareCost(this.HardwareCost)
               .SetSoftwareCost(this.SoftwareCost)
               .SetSystemDevelopmentCost(this.SystemDevelopmentCost);
    }

    public static MedianPriceExpenseDescriptionInfo Default()
    {
        return new MedianPriceExpenseDescriptionInfo(
            null,
            null,
            null,
            null,
            null,
            null);
    }

    public static MedianPriceExpenseDescriptionInfo FromEntity(PpMedianPriceExpenseDescription? entity)
    {
        if (entity is null)
        {
            return Default();
        }

        return new MedianPriceExpenseDescriptionInfo(
            entity.MaterialCost,
            entity.OverseasTravelCost,
            entity.OtherExpenses,
            entity.HardwareCost,
            entity.SoftwareCost,
            entity.SystemDevelopmentCost);
    }
}