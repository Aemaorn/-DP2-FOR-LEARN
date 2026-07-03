namespace GHB.DP2.Domain.Raws;

using Codehard.Common.DomainModel;
using LanguageExt;
using Vogen;

[ValueObject<string>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct SubDistrictId
{
    public static SubDistrictId New() => From(Guid.CreateVersion7().ToString());
}

public class RawSubDistrict : Entity<SubDistrictId>
{
    public override SubDistrictId Id { get; init; }

    public string? DistrictCode { get; private init; }

    public string Code { get; private set; }

    public string NameTh { get; private set; }

    public string? NameEn { get; private set; }

    public string? ZipCode { get; private set; }

    public int Sequence { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public static RawSubDistrict Create(
        string districtCode,
        string code,
        string nameTh,
        string? nameEn,
        string? zipCode,
        int sequence)
    {
        return new RawSubDistrict
        {
            Id = SubDistrictId.New(),
            DistrictCode = districtCode,
            Code = code,
            NameTh = nameTh,
            NameEn = nameEn,
            ZipCode = zipCode,
            Sequence = sequence,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public Unit Update(
        string nameTh,
        string? nameEn,
        string? zipCode)
    {
        this.NameTh = nameTh;
        this.NameEn = nameEn;
        this.ZipCode = zipCode;
        this.UpdatedAt = DateTimeOffset.UtcNow;

        return unit;
    }
}
