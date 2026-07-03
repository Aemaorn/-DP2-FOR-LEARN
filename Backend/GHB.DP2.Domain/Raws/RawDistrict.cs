namespace GHB.DP2.Domain.Raws;

using Codehard.Common.DomainModel;
using LanguageExt;
using Vogen;

[ValueObject<string>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct DistrictId
{
    public static DistrictId New() => From(Guid.CreateVersion7().ToString());
}

public class RawDistrict : Entity<DistrictId>
{
    public override DistrictId Id { get; init; }

    public string? ProvinceCode { get; private init; }

    public string Code { get; private set; }

    public string NameTh { get; private set; }

    public string? NameEn { get; private set; }

    public int Sequence { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public static RawDistrict Create(
        string provinceCode,
        string code,
        string nameTh,
        string? nameEn,
        int sequence)
    {
        return new RawDistrict
        {
            Id = DistrictId.New(),
            ProvinceCode = provinceCode,
            Code = code,
            NameTh = nameTh,
            NameEn = nameEn,
            Sequence = sequence,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public Unit Update(
        string nameTh,
        string? nameEn)
    {
        this.NameTh = nameTh;
        this.NameEn = nameEn;
        this.UpdatedAt = DateTimeOffset.UtcNow;

        return unit;
    }
}
