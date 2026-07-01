namespace GHB.DP2.Domain.ContractAgreement.CaContractDraft.SectionType;

public class Buyer
{
    public string? Name { get; init; }

    public string? Address { get; init; }

    public LocationInfo? Province { get; init; }

    public LocationInfo? District { get; init; }

    public LocationInfo? SubDistrict { get; init; }

    public Buyer()
    {
        // Parameterless constructor for EF Core
    }

    public Buyer(
        string? name,
        string? address,
        LocationInfo province,
        LocationInfo district,
        LocationInfo subDistrict)
    {
        this.Name = name;
        this.Address = address;
        this.Province = province;
        this.District = district;
        this.SubDistrict = subDistrict;
    }
}