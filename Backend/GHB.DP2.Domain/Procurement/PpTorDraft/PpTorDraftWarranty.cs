namespace GHB.DP2.Domain.Procurement.PpTorDraft;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PpTorDraftWarrantyId
{
    public static PpTorDraftWarrantyId New() => From(Guid.CreateVersion7());
}

public class PpTorDraftWarranty : AuditableEntity<PpTorDraftWarrantyId>
{
    public override PpTorDraftWarrantyId Id { get; init; }

    public bool? HasWarranty { get; set; }

    public int? Period { get; set; }

    public ParameterCode? PeriodTypeCode { get; set; }

    public string? ConditionOther { get; set; }

    public virtual PpTorDraft PpTorDraft { get; init; }

    public virtual SuParameter? PeriodType { get; init; }
}