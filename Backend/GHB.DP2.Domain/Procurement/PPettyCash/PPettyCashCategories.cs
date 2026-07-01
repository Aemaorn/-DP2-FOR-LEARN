namespace GHB.DP2.Domain.Procurement.PPettyCash;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PettyCashCategoriesId
{
    public static PettyCashCategoriesId New() => From(Guid.CreateVersion7());
}

public class PPettyCashCategories : AuditableEntity<PettyCashCategoriesId>
{
    public override PettyCashCategoriesId Id { get; init; }

    public PettyCashId PettyCashId { get; set; }

    public virtual ParameterCode CategoryTypeCode { get; private set; }

    public virtual PPettyCash PettyCash { get; init; }

    public virtual SuParameter CategoryType { get; init; }

    public static PPettyCashCategories Create(
        PettyCashId pettyCashId)
    {
        var PettyCashCategories = new PPettyCashCategories
        {
            Id = PettyCashCategoriesId.New(),
            PettyCashId = pettyCashId,
        };

        return PettyCashCategories;
    }

    public PPettyCashCategories SetCategoryType(
        ParameterCode categoryTypeCode)
    {
        this.CategoryTypeCode = categoryTypeCode;

        return this;
    }
}