namespace GHB.DP2.Domain.Raws;

using Codehard.Common.DomainModel;
using Vogen;

[ValueObject<string>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct DistrictId;

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
}
