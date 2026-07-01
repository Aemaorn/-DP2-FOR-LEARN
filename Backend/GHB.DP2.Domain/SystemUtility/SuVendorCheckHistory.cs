namespace GHB.DP2.Domain.SystemUtility;

using GHB.DP2.Domain.Common;
using System.ComponentModel.DataAnnotations;
using Vogen;

public enum CheckType
{
    [Display(Name = "COI")]
    COI,

    [Display(Name = "Watchlist")]
    Watchlist,
}

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct SuVendorCheckHistoryId
{
    public static SuVendorCheckHistoryId New() => From(Guid.CreateVersion7());
}

public partial class SuVendorCheckHistory : AuditableEntity<SuVendorCheckHistoryId>
{
    public override SuVendorCheckHistoryId Id { get; init; }

    public SuVendorId VendorId { get; init; }

    public CheckType CheckType { get; init; }

    public bool Result { get; set; }

    public string? Remark { get; set; }

    public virtual SuVendor Vendor { get; private set; }

    public static SuVendorCheckHistory Create(
        SuVendor vendor,
        CheckType checkType,
        bool result,
        string remark)
    {
        return new SuVendorCheckHistory()
        {
            Id = SuVendorCheckHistoryId.New(),
            Vendor = vendor,
            VendorId = vendor.Id,
            CheckType = checkType,
            Result = result,
            Remark = remark,
        };
    }
}