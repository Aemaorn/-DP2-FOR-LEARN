namespace GHB.DP2.Domain.Raws;

using System;
using Codehard.Common.DomainModel;
using Vogen;

[ValueObject<string>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct ProvinceId;

public class RawProvinces : Entity<ProvinceId>
{
    public override ProvinceId Id { get; init; }

    public string Code { get; private set; }

    public string NameTh { get; private set; }

    public string? NameEn { get; private set; }

    public int Sequence { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? UpdatedAt { get; private set; }
}
