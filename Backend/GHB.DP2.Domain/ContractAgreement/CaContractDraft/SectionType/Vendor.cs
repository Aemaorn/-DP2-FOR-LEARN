namespace GHB.DP2.Domain.ContractAgreement.CaContractDraft.SectionType;

using GHB.DP2.Domain.SystemUtility;

public record Vendor
{
    public SuVendorId VendorId { get; init; }

    public DateTimeOffset? StartDate { get; private set; }

    public DateTimeOffset? EndDate { get; private set; }

    public string? EstablishmentName { get; private set; }

    public string? VendorRegistrationPlace { get; private set; }

    public string? Address { get; private set; }

    public string? Road { get; private set; }

    public string? RawProvinceCode { get; private set; }

    public string? RawDistrictCode { get; private set; }

    public string? RawSubDistrictCode { get; private set; }

    public virtual SuVendor VendorInfo { get; init; }

    public Vendor SetDateDuration(
        DateTimeOffset? startDate,
        DateTimeOffset? endDate)
    {
        this.StartDate = startDate;
        this.EndDate = endDate;

        return this;
    }

    public Vendor SetDetailAddress(
        string? establishmentName,
        string? vendorRegistrationPlace,
        string? address,
        string? road,
        string? rawProvinceCode,
        string? rawDistrictCode,
        string? rawSubDistrictCode)
    {
        this.EstablishmentName = establishmentName;
        this.VendorRegistrationPlace = vendorRegistrationPlace;
        this.Address = address;
        this.Road = road;
        this.RawProvinceCode = rawProvinceCode;
        this.RawDistrictCode = rawDistrictCode;
        this.RawSubDistrictCode = rawSubDistrictCode;

        return this;
    }

    public static Vendor Create(SuVendorId vendorId)
    {
        return new Vendor
        {
            VendorId = vendorId,
        };
    }
}