namespace GHB.DP2.Domain.ContractAgreement.CaContractDraft.SectionType;

using GHB.DP2.Domain.SystemUtility;

public enum AgreementType
{
    /// <summary>
    /// General agreement without specific conditions.
    /// </summary>
    General,

    /// <summary>
    /// Agreement where the vendor is an exchange giver.
    /// </summary>
    ExchangeGiver,

    /// <summary>
    /// Agreement with a specified workplace address.
    /// </summary>
    Workplace,

    /// <summary>
    /// Agreement with a specified workplace address and serial number.
    /// </summary>
    WorkplaceSerialNumber,

    /// <summary>
    /// Agreement with a rental duration specified.
    /// </summary>
    RentalDuration,

    /// <summary>
    /// Agreement with a rental duration and workplace address specified.
    /// </summary>
    RentalDurationWorkplace,

    /// <summary>
    /// Lease agreement.
    /// </summary>
    Lease,

    /// <summary>
    /// Lease agreement for computer equipment.
    /// </summary>
    LeaseComputer,

    /// <summary>
    /// Lease agreement for a car.
    /// </summary>
    LeaseCar,
}

public class AgreementContract
{
    public AgreementType? Type { get; init; }

    public string ContractItem { get; init; }

    public bool? IsExchangeGiver { get; private set; }

    public string? WorkplaceAddress { get; private set; }

    public LocationInfo? WorkplaceProvince { get; private set; }

    public LocationInfo? WorkplaceDistrict { get; private set; }

    public LocationInfo? WorkplaceSubDistrict { get; private set; }

    public RentalDurationInfo? RentalDuration { get; private set; }

    public DateTimeOffset? StartDate { get; private set; }

    public DateTimeOffset? EndDate { get; private set; }

    public string? Brand { get; private set; }

    public string? Model { get; private set; }

    public string? SerialNumber { get; private set; }

    public decimal? EngineCapacityCc { get; private set; }

    public int? Quantity { get; private set; }

    public ParameterCode? UnitCode { get; private set; }

    public ParameterCode? VatRateTypeCode { get; private init; }

    public decimal? Price { get; private init; }

    public decimal? VatAmount { get; private init; }

    public decimal? TotalAmount { get; private init; }

    public virtual SuParameter? Unit { get; init; }

    public virtual SuParameter? VatRateType { get; init; }

    public AgreementContract SetExchangeGiver(bool isExchangeGiver)
    {
        this.IsExchangeGiver = isExchangeGiver;

        return this;
    }

    public AgreementContract SetWorkplace(
        string workplaceAddress,
        LocationInfo workplaceProvince,
        LocationInfo workplaceDistrict,
        LocationInfo workplaceSubDistrict)
    {
        this.WorkplaceAddress = workplaceAddress;
        this.WorkplaceProvince = workplaceProvince;
        this.WorkplaceDistrict = workplaceDistrict;
        this.WorkplaceSubDistrict = workplaceSubDistrict;

        return this;
    }

    public AgreementContract SetWorkplaceAddress(
    string? workplaceAddress)
    {
        this.WorkplaceAddress = workplaceAddress;

        return this;
    }

    public AgreementContract SetRentalDuration(
        RentalDurationInfo? rentalDuration)
    {
        this.RentalDuration = rentalDuration;

        return this;
    }

    public AgreementContract SetStartAndEndDate(
        DateTimeOffset startDate,
        DateTimeOffset endDate)
    {
        this.StartDate = startDate;
        this.EndDate = endDate;

        return this;
    }

    public AgreementContract SetBrandAndModel(
        string brand,
        string model)
    {
        this.Brand = brand;
        this.Model = model;

        return this;
    }

    public AgreementContract SetSerialNumber(string? serialNumber)
    {
        this.SerialNumber = serialNumber ?? string.Empty;

        return this;
    }

    public AgreementContract SetEngineCapacityCc(decimal? engineCapacityCc)
    {
        this.EngineCapacityCc = engineCapacityCc;

        return this;
    }

    public AgreementContract SetQuantity(int quantity)
    {
        if (quantity > 0)
        {
            this.Quantity = quantity;
        }

        return this;
    }

    public AgreementContract SetUnitCode(string unitCode)
    {
        this.UnitCode = !string.IsNullOrEmpty(unitCode) ? ParameterCode.From(unitCode) : null;

        return this;
    }

    public static AgreementContract Create(
        AgreementType type,
        string contractItem,
        string? vatRateTypeCode,
        decimal? price,
        decimal? vatAmount,
        decimal? totalAmount)
    {
        return new AgreementContract
        {
            Type = type,
            ContractItem = contractItem,
            VatRateTypeCode = !string.IsNullOrWhiteSpace(vatRateTypeCode) ? ParameterCode.From(vatRateTypeCode) : null,
            Price = price,
            VatAmount = vatAmount,
            TotalAmount = totalAmount,
        };
    }
}