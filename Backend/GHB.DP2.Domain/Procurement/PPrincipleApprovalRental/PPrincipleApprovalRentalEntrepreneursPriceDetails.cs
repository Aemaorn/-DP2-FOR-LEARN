namespace GHB.DP2.Domain.Procurement.PPrincipleApprovalRental;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PPrincipleApprovalRentalEntrepreneursPriceDetailsId
{
    public static PPrincipleApprovalRentalEntrepreneursPriceDetailsId New() => From(Guid.CreateVersion7());
}

public partial class PPrincipleApprovalRentalEntrepreneursPriceDetails : AuditableEntity<PPrincipleApprovalRentalEntrepreneursPriceDetailsId>, IHasSoftDelete
{
    public override PPrincipleApprovalRentalEntrepreneursPriceDetailsId Id { get; init; }

    public int Sequence { get; private set; }

    public string ParcelName { get; private set; }

    public string Description { get; private set; }

    public int ParcelQuantity { get; private set; }

    public ParameterCode ParcelUnitCode { get; private set; }

    public ParameterCode VatTypeCode { get; private set; }

    public decimal OfferedPrice { get; private set; }

    public decimal AgreedPrice { get; private set; }

    public virtual SuParameter ParcelUnit { get; init; }

    public virtual SuParameter VatType { get; init; }

    public virtual PPrincipleApprovalRentalEntrepreneurs Entrepreneurs { get; init; }

    public static PPrincipleApprovalRentalEntrepreneursPriceDetails Create()
    {
        return new PPrincipleApprovalRentalEntrepreneursPriceDetails
        {
            Id = PPrincipleApprovalRentalEntrepreneursPriceDetailsId.New(),
        };
    }

    public PPrincipleApprovalRentalEntrepreneursPriceDetails SetDetails(
        int sequence,
        string parcelName,
        int parcelQuantity,
        ParameterCode parcelUnitCode,
        ParameterCode vatTypeCode,
        decimal offeredPrice,
        decimal agreedPrice,
        string description)
    {
        this.Sequence = sequence;
        this.ParcelName = parcelName;
        this.ParcelQuantity = parcelQuantity;
        this.ParcelUnitCode = parcelUnitCode;
        this.VatTypeCode = vatTypeCode;
        this.OfferedPrice = offeredPrice;
        this.AgreedPrice = agreedPrice;
        this.Description = description;

        return this;
    }
}