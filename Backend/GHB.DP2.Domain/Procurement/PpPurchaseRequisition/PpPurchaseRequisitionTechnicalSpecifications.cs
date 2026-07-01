namespace GHB.DP2.Domain.Procurement.PpPurchaseRequisition;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using LanguageExt;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PpPurchaseRequisitionTechnicalSpecificationsId
{
    public static PpPurchaseRequisitionTechnicalSpecificationsId New() => From(Guid.CreateVersion7());
}

public class PpPurchaseRequisitionTechnicalSpecifications : AuditableEntity<PpPurchaseRequisitionTechnicalSpecificationsId>
{
    public override PpPurchaseRequisitionTechnicalSpecificationsId Id { get; init; }

    public PpPurchaseRequisitionId PpPurchaseRequisitionId { get; init; }

    public int Sequence { get; set; }

    public string Name { get; set; }

    public string Description { get; set; }

    public int Quantity { get; set; }

    public ParameterCode? UnitCode { get; set; }

    public virtual PpPurchaseRequisition PurchaseRequisition { get; init; }

    public virtual SuParameter Unit { get; init; }

    public static PpPurchaseRequisitionTechnicalSpecifications Create(
        int sequence,
        string name,
        string description,
        int quantity,
        ParameterCode? unitCode,
        PpPurchaseRequisitionId ppPurchaseRequisitionId)
    {
        return new PpPurchaseRequisitionTechnicalSpecifications
        {
            Id = PpPurchaseRequisitionTechnicalSpecificationsId.New(),
            Sequence = sequence,
            Name = name,
            Description = description,
            Quantity = quantity,
            UnitCode = unitCode,
            PpPurchaseRequisitionId = ppPurchaseRequisitionId,
        };
    }

    public Unit Update(
        int sequence,
        string name,
        string description,
        int quantity,
        ParameterCode? unitCode)
    {
        this.Sequence = sequence;
        this.Name = name;
        this.Description = description;
        this.Quantity = quantity;
        this.UnitCode = unitCode;

        return unit;
    }
}