namespace GHB.DP2.Application.Features.ContractAgreement.ContractDraft.Dto;

using System.Text.Json.Serialization;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft.SectionType;
using GreatFriends.ThaiBahtText;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(General), nameof(General))]
[JsonDerivedType(typeof(ExchangeGiver), nameof(ExchangeGiver))]
[JsonDerivedType(typeof(Workplace), nameof(Workplace))]
[JsonDerivedType(typeof(WorkplaceSerialNumber), nameof(WorkplaceSerialNumber))]
[JsonDerivedType(typeof(RentalDuration), nameof(RentalDuration))]
[JsonDerivedType(typeof(RentalDurationWorkplace), nameof(RentalDurationWorkplace))]
[JsonDerivedType(typeof(Lease), nameof(Lease))]
[JsonDerivedType(typeof(LeaseCar), nameof(LeaseCar))]
[JsonDerivedType(typeof(LeaseComputer), nameof(LeaseComputer))]
public abstract class AgreementBase
{
    public string ItemDetail { get; init; }

    public string? VatRateTypeCode { get; init; }

    public decimal? AgreementPrice { get; init; }

    public string? AgreementPriceFormat { get; init; }

    public string? AgreementPriceText { get; init; }

    public decimal? VatAmount { get; init; }

    public string? VatAmountFormat { get; init; }

    public string? VatAmountText { get; init; }

    public decimal? TotalAmount { get; init; }

    public string? TotalAmountFormat { get; init; }

    public string? TotalAmountText { get; init; }

    public abstract AgreementContract MapToEntity();

    public static AgreementBase MapToModel(AgreementContract entity)
    {
        return entity.Type switch
        {
            AgreementType.General => General.FromEntity(entity),
            AgreementType.ExchangeGiver => ExchangeGiver.FromEntity(entity),
            AgreementType.Workplace => Workplace.FromEntity(entity),
            AgreementType.WorkplaceSerialNumber => WorkplaceSerialNumber.FromEntity(entity),
            AgreementType.RentalDuration => RentalDuration.FromEntity(entity),
            AgreementType.RentalDurationWorkplace => RentalDurationWorkplace.FromEntity(entity),
            AgreementType.Lease => Lease.FromEntity(entity),
            AgreementType.LeaseCar => LeaseCar.FromEntity(entity),
            AgreementType.LeaseComputer => LeaseComputer.FromEntity(entity),
            _ => throw new NotSupportedException($"Unsupported agreement type: {entity.Type}"),
        };
    }
}

public class General : AgreementBase
{
    public int Quantity { get; init; }

    public string UnitCode { get; init; }

    public override AgreementContract MapToEntity()
    {
        return
            AgreementContract
                .Create(
                    AgreementType.General,
                    this.ItemDetail,
                    this.VatRateTypeCode,
                    this.AgreementPrice,
                    this.VatAmount,
                    this.TotalAmount)
                .SetQuantity(this.Quantity)
                .SetUnitCode(this.UnitCode);
    }

    public static General FromEntity(AgreementContract entity)
    {
        return new General
        {
            ItemDetail = entity.ContractItem,
            VatRateTypeCode = entity.VatRateTypeCode?.Value ?? string.Empty,
            AgreementPrice = entity.Price ?? 0,
            VatAmount = entity.VatAmount,
            VatAmountFormat = entity.VatAmount != null ? entity.VatAmount.Value.ToCurrencyStringWithComma() : null,
            VatAmountText = entity.VatAmount != null ? entity.VatAmount.Value.ThaiBahtText() : null,
            TotalAmount = entity.TotalAmount ?? 0,
            TotalAmountFormat = entity.TotalAmount != null ? entity.TotalAmount.Value.ToCurrencyStringWithComma() : null,
            TotalAmountText = entity.TotalAmount != null ? entity.TotalAmount.Value.ThaiBahtText() : null,
            Quantity = entity.Quantity ?? 0,
            UnitCode = entity.UnitCode?.Value ?? string.Empty,
        };
    }
}

public class ExchangeGiver : AgreementBase
{
    public int Quantity { get; init; }

    public string UnitCode { get; init; }

    public bool IsExchangeGiver { get; init; }

    public override AgreementContract MapToEntity()
    {
        return AgreementContract
               .Create(
                   AgreementType.ExchangeGiver,
                   this.ItemDetail,
                   this.VatRateTypeCode,
                   this.AgreementPrice,
                   this.VatAmount,
                   this.TotalAmount)
               .SetQuantity(this.Quantity)
               .SetUnitCode(this.UnitCode)
               .SetExchangeGiver(this.IsExchangeGiver);
    }

    public static ExchangeGiver FromEntity(AgreementContract entity)
    {
        return new ExchangeGiver
        {
            ItemDetail = entity.ContractItem,
            VatRateTypeCode = entity.VatRateTypeCode?.Value ?? string.Empty,
            AgreementPrice = entity.Price ?? 0,
            VatAmount = entity.VatAmount,
            VatAmountFormat = entity.VatAmount != null ? entity.VatAmount.Value.ToCurrencyStringWithComma() : null,
            VatAmountText = entity.VatAmount != null ? entity.VatAmount.Value.ThaiBahtText() : null,
            TotalAmount = entity.TotalAmount ?? 0,
            TotalAmountFormat = entity.TotalAmount != null ? entity.TotalAmount.Value.ToCurrencyStringWithComma() : null,
            TotalAmountText = entity.TotalAmount != null ? entity.TotalAmount.Value.ThaiBahtText() : null,
            Quantity = entity.Quantity ?? 0,
            UnitCode = entity.UnitCode?.Value ?? string.Empty,
            IsExchangeGiver = entity.IsExchangeGiver ?? false,
        };
    }
}

public class Workplace : AgreementBase
{
    public int Quantity { get; init; }

    public string UnitCode { get; init; }

    public string WorkplaceAddress { get; init; }

    public LocationInfo WorkplaceProvince { get; init; }

    public LocationInfo WorkplaceDistrict { get; init; }

    public LocationInfo WorkplaceSubDistrict { get; init; }

    public override AgreementContract MapToEntity()
    {
        return
            AgreementContract
                .Create(
                    AgreementType.Workplace,
                    this.ItemDetail,
                    this.VatRateTypeCode,
                    this.AgreementPrice,
                    this.VatAmount,
                    this.TotalAmount)
                .SetQuantity(this.Quantity)
                .SetUnitCode(this.UnitCode)
                .SetWorkplace(
                    this.WorkplaceAddress,
                    this.WorkplaceProvince,
                    this.WorkplaceDistrict,
                    this.WorkplaceSubDistrict);
    }

    public static Workplace FromEntity(AgreementContract entity)
    {
        return new Workplace
        {
            ItemDetail = entity.ContractItem,
            VatRateTypeCode = entity.VatRateTypeCode?.Value ?? string.Empty,
            AgreementPrice = entity.Price ?? 0,
            VatAmount = entity.VatAmount,
            VatAmountFormat = entity.VatAmount != null ? entity.VatAmount.Value.ToCurrencyStringWithComma() : null,
            VatAmountText = entity.VatAmount != null ? entity.VatAmount.Value.ThaiBahtText() : null,
            TotalAmount = entity.TotalAmount ?? 0,
            TotalAmountFormat = entity.TotalAmount != null ? entity.TotalAmount.Value.ToCurrencyStringWithComma() : null,
            TotalAmountText = entity.TotalAmount != null ? entity.TotalAmount.Value.ThaiBahtText() : null,
            Quantity = entity.Quantity ?? 0,
            UnitCode = entity.UnitCode?.Value ?? string.Empty,
            WorkplaceAddress = entity.WorkplaceAddress ?? string.Empty,
            WorkplaceProvince = entity.WorkplaceProvince ?? LocationInfo.Default,
            WorkplaceDistrict = entity.WorkplaceDistrict ?? LocationInfo.Default,
            WorkplaceSubDistrict = entity.WorkplaceSubDistrict ?? LocationInfo.Default,
        };
    }
}

public class WorkplaceSerialNumber : AgreementBase
{
    public int Quantity { get; init; }

    public string UnitCode { get; init; }

    public string WorkplaceAddress { get; init; }

    public LocationInfo WorkplaceProvince { get; init; }

    public LocationInfo WorkplaceDistrict { get; init; }

    public LocationInfo WorkplaceSubDistrict { get; init; }

    public string? SerialNumber { get; init; }

    public override AgreementContract MapToEntity()
    {
        return AgreementContract
               .Create(
                   AgreementType.WorkplaceSerialNumber,
                   this.ItemDetail,
                   this.VatRateTypeCode,
                   this.AgreementPrice,
                   this.VatAmount,
                   this.TotalAmount)
               .SetQuantity(this.Quantity)
               .SetWorkplace(
                   this.WorkplaceAddress,
                   this.WorkplaceProvince,
                   this.WorkplaceDistrict,
                   this.WorkplaceSubDistrict)
               .SetSerialNumber(this.SerialNumber)
               .SetUnitCode(this.UnitCode);
    }

    public static WorkplaceSerialNumber FromEntity(AgreementContract entity)
    {
        return new WorkplaceSerialNumber
        {
            ItemDetail = entity.ContractItem,
            VatRateTypeCode = entity.VatRateTypeCode?.Value ?? string.Empty,
            AgreementPrice = entity.Price ?? 0,
            VatAmount = entity.VatAmount,
            VatAmountFormat = entity.VatAmount != null ? entity.VatAmount.Value.ToCurrencyStringWithComma() : null,
            VatAmountText = entity.VatAmount != null ? entity.VatAmount.Value.ThaiBahtText() : null,
            TotalAmount = entity.TotalAmount ?? 0,
            TotalAmountFormat = entity.TotalAmount != null ? entity.TotalAmount.Value.ToCurrencyStringWithComma() : null,
            TotalAmountText = entity.TotalAmount != null ? entity.TotalAmount.Value.ThaiBahtText() : null,
            Quantity = entity.Quantity ?? 0,
            WorkplaceAddress = entity.WorkplaceAddress ?? string.Empty,
            WorkplaceProvince = entity.WorkplaceProvince ?? LocationInfo.Default,
            WorkplaceDistrict = entity.WorkplaceDistrict ?? LocationInfo.Default,
            WorkplaceSubDistrict = entity.WorkplaceSubDistrict ?? LocationInfo.Default,
            SerialNumber = entity.SerialNumber ?? string.Empty,
            UnitCode = entity.UnitCode?.Value ?? string.Empty,
        };
    }
}

public class RentalDuration : AgreementBase
{
    public int Quantity { get; init; }

    public string UnitCode { get; init; }

    public RentalDurationInfo? Duration { get; init; }

    public DateTimeOffset StartDate { get; init; }

    public DateTimeOffset EndDate { get; init; }

    public string? WorkplaceAddress { get; init; }

    public override AgreementContract MapToEntity()
    {
        return
            AgreementContract
                .Create(
                    AgreementType.RentalDuration,
                    this.ItemDetail,
                    this.VatRateTypeCode,
                    this.AgreementPrice,
                    this.VatAmount,
                    this.TotalAmount)
                .SetQuantity(this.Quantity)
                .SetUnitCode(this.UnitCode)
                .SetRentalDuration(this.Duration)
                .SetStartAndEndDate(
                    this.StartDate,
                    this.EndDate)
                .SetWorkplaceAddress(this.WorkplaceAddress);
    }

    public static RentalDuration FromEntity(AgreementContract entity)
    {
        return new RentalDuration
        {
            ItemDetail = entity.ContractItem,
            VatRateTypeCode = entity.VatRateTypeCode?.Value ?? string.Empty,
            AgreementPrice = entity.Price ?? 0,
            VatAmount = entity.VatAmount,
            VatAmountFormat = entity.VatAmount != null ? entity.VatAmount.Value.ToCurrencyStringWithComma() : null,
            VatAmountText = entity.VatAmount != null ? entity.VatAmount.Value.ThaiBahtText() : null,
            TotalAmount = entity.TotalAmount ?? 0,
            TotalAmountFormat = entity.TotalAmount != null ? entity.TotalAmount.Value.ToCurrencyStringWithComma() : null,
            TotalAmountText = entity.TotalAmount != null ? entity.TotalAmount.Value.ThaiBahtText() : null,
            Quantity = entity.Quantity ?? 0,
            UnitCode = entity.UnitCode?.Value ?? string.Empty,
            Duration = entity.RentalDuration,
            StartDate = entity.StartDate ?? DateTimeOffset.MinValue,
            EndDate = entity.EndDate ?? DateTimeOffset.MinValue,
            WorkplaceAddress = entity.WorkplaceAddress,
        };
    }
}

public class RentalDurationWorkplace : AgreementBase
{
    public int Quantity { get; init; }

    public string UnitCode { get; init; }

    public RentalDurationInfo Duration { get; init; }

    public DateTimeOffset StartDate { get; init; }

    public DateTimeOffset EndDate { get; init; }

    public string WorkplaceAddress { get; init; }

    public LocationInfo WorkplaceProvince { get; init; }

    public LocationInfo WorkplaceDistrict { get; init; }

    public LocationInfo WorkplaceSubDistrict { get; init; }

    public override AgreementContract MapToEntity()
    {
        return AgreementContract
               .Create(
                   AgreementType.RentalDurationWorkplace,
                   this.ItemDetail,
                   this.VatRateTypeCode,
                   this.AgreementPrice,
                   this.VatAmount,
                   this.TotalAmount)
               .SetQuantity(this.Quantity)
               .SetUnitCode(this.UnitCode)
               .SetRentalDuration(this.Duration)
               .SetStartAndEndDate(
                   this.StartDate,
                   this.EndDate)
               .SetWorkplace(
                   this.WorkplaceAddress,
                   this.WorkplaceProvince,
                   this.WorkplaceDistrict,
                   this.WorkplaceSubDistrict);
    }

    public static RentalDurationWorkplace FromEntity(AgreementContract entity)
    {
        return new RentalDurationWorkplace
        {
            ItemDetail = entity.ContractItem,
            VatRateTypeCode = entity.VatRateTypeCode?.Value ?? string.Empty,
            AgreementPrice = entity.Price ?? 0,
            VatAmount = entity.VatAmount,
            VatAmountFormat = entity.VatAmount != null ? entity.VatAmount.Value.ToCurrencyStringWithComma() : null,
            VatAmountText = entity.VatAmount != null ? entity.VatAmount.Value.ThaiBahtText() : null,
            TotalAmount = entity.TotalAmount ?? 0,
            TotalAmountFormat = entity.TotalAmount != null ? entity.TotalAmount.Value.ToCurrencyStringWithComma() : null,
            TotalAmountText = entity.TotalAmount != null ? entity.TotalAmount.Value.ThaiBahtText() : null,
            Quantity = entity.Quantity ?? 0,
            UnitCode = entity.UnitCode?.Value ?? string.Empty,
            Duration = entity.RentalDuration ?? RentalDurationInfo.Default,
            StartDate = entity.StartDate ?? DateTimeOffset.MinValue,
            EndDate = entity.EndDate ?? DateTimeOffset.MinValue,
            WorkplaceAddress = entity.WorkplaceAddress ?? string.Empty,
            WorkplaceProvince = entity.WorkplaceProvince ?? LocationInfo.Default,
            WorkplaceDistrict = entity.WorkplaceDistrict ?? LocationInfo.Default,
            WorkplaceSubDistrict = entity.WorkplaceSubDistrict ?? LocationInfo.Default,
        };
    }
}

public class Lease : AgreementBase
{
    public int Quantity { get; init; }

    public string UnitCode { get; init; }

    public RentalDurationInfo Duration { get; init; }

    public DateTimeOffset StartDate { get; init; }

    public DateTimeOffset EndDate { get; init; }

    public string Brand { get; init; }

    public string Model { get; init; }

    public string? SerialNumber { get; init; }

    public string? WorkplaceAddress { get; init; }

    public override AgreementContract MapToEntity()
    {
        return
            AgreementContract
                .Create(
                    AgreementType.Lease,
                    this.ItemDetail,
                    this.VatRateTypeCode,
                    this.AgreementPrice,
                    this.VatAmount,
                    this.TotalAmount)
                .SetUnitCode(this.UnitCode)
                .SetQuantity(this.Quantity)
                .SetRentalDuration(this.Duration)
                .SetStartAndEndDate(
                    this.StartDate,
                    this.EndDate)
                .SetBrandAndModel(
                    this.Brand,
                    this.Model)
                .SetSerialNumber(this.SerialNumber)
                .SetWorkplaceAddress(this.WorkplaceAddress);
    }

    public static Lease FromEntity(AgreementContract entity)
    {
        return new Lease
        {
            ItemDetail = entity.ContractItem,
            VatRateTypeCode = entity.VatRateTypeCode?.Value ?? string.Empty,
            AgreementPrice = entity.Price ?? 0,
            VatAmount = entity.VatAmount,
            VatAmountFormat = entity.VatAmount != null ? entity.VatAmount.Value.ToCurrencyStringWithComma() : null,
            VatAmountText = entity.VatAmount != null ? entity.VatAmount.Value.ThaiBahtText() : null,
            TotalAmount = entity.TotalAmount ?? 0,
            TotalAmountFormat = entity.TotalAmount != null ? entity.TotalAmount.Value.ToCurrencyStringWithComma() : null,
            TotalAmountText = entity.TotalAmount != null ? entity.TotalAmount.Value.ThaiBahtText() : null,
            Quantity = entity.Quantity ?? 0,
            Duration = entity.RentalDuration ?? RentalDurationInfo.Default,
            StartDate = entity.StartDate ?? DateTimeOffset.MinValue,
            EndDate = entity.EndDate ?? DateTimeOffset.MinValue,
            Brand = entity.Brand ?? string.Empty,
            Model = entity.Model ?? string.Empty,
            SerialNumber = entity.SerialNumber ?? string.Empty,
            UnitCode = entity.UnitCode?.Value ?? string.Empty,
            WorkplaceAddress = entity.WorkplaceAddress,
        };
    }
}

public class LeaseCar : AgreementBase
{
    public int Quantity { get; init; }

    public RentalDurationInfo Duration { get; init; }

    public DateTimeOffset StartDate { get; init; }

    public DateTimeOffset EndDate { get; init; }

    public string Brand { get; init; }

    public string Model { get; init; }

    public decimal EngineCapacityCc { get; init; }

    public string? WorkplaceAddress { get; init; }

    public override AgreementContract MapToEntity()
    {
        return
            AgreementContract
                .Create(
                    AgreementType.LeaseCar,
                    this.ItemDetail,
                    this.VatRateTypeCode,
                    this.AgreementPrice,
                    this.VatAmount,
                    this.TotalAmount)
                .SetQuantity(this.Quantity)
                .SetRentalDuration(this.Duration)
                .SetStartAndEndDate(
                    this.StartDate,
                    this.EndDate)
                .SetBrandAndModel(
                    this.Brand,
                    this.Model)
                .SetEngineCapacityCc(this.EngineCapacityCc)
                .SetWorkplaceAddress(this.WorkplaceAddress);
    }

    public static LeaseCar FromEntity(AgreementContract entity)
    {
        return new LeaseCar
        {
            ItemDetail = entity.ContractItem,
            VatRateTypeCode = entity.VatRateTypeCode?.Value ?? string.Empty,
            AgreementPrice = entity.Price ?? 0,
            VatAmount = entity.VatAmount,
            VatAmountFormat = entity.VatAmount != null ? entity.VatAmount.Value.ToCurrencyStringWithComma() : null,
            VatAmountText = entity.VatAmount != null ? entity.VatAmount.Value.ThaiBahtText() : null,
            TotalAmount = entity.TotalAmount ?? 0,
            TotalAmountFormat = entity.TotalAmount != null ? entity.TotalAmount.Value.ToCurrencyStringWithComma() : null,
            TotalAmountText = entity.TotalAmount != null ? entity.TotalAmount.Value.ThaiBahtText() : null,
            Quantity = entity.Quantity ?? 0,
            Duration = entity.RentalDuration ?? RentalDurationInfo.Default,
            StartDate = entity.StartDate ?? DateTimeOffset.MinValue,
            EndDate = entity.EndDate ?? DateTimeOffset.MinValue,
            Brand = entity.Brand ?? string.Empty,
            Model = entity.Model ?? string.Empty,
            EngineCapacityCc = entity.EngineCapacityCc ?? 0m,
            WorkplaceAddress = entity.WorkplaceAddress,
        };
    }
}

public class LeaseComputer : AgreementBase
{
    public int Quantity { get; init; }

    public string? WorkplaceAddress { get; init; }

    public override AgreementContract MapToEntity()
    {
        return
            AgreementContract
                .Create(
                    AgreementType.LeaseComputer,
                    this.ItemDetail,
                    this.VatRateTypeCode,
                    this.AgreementPrice,
                    this.VatAmount,
                    this.TotalAmount)
                .SetQuantity(this.Quantity)
                .SetWorkplaceAddress(this.WorkplaceAddress);
    }

    public static LeaseComputer FromEntity(AgreementContract entity)
    {
        return new LeaseComputer
        {
            ItemDetail = entity.ContractItem,
            VatRateTypeCode = entity.VatRateTypeCode?.Value ?? string.Empty,
            AgreementPrice = entity.Price ?? 0,
            VatAmount = entity.VatAmount,
            VatAmountFormat = entity.VatAmount != null ? entity.VatAmount.Value.ToCurrencyStringWithComma() : null,
            VatAmountText = entity.VatAmount != null ? entity.VatAmount.Value.ThaiBahtText() : null,
            TotalAmount = entity.TotalAmount ?? 0,
            TotalAmountFormat = entity.TotalAmount != null ? entity.TotalAmount.Value.ToCurrencyStringWithComma() : null,
            TotalAmountText = entity.TotalAmount != null ? entity.TotalAmount.Value.ThaiBahtText() : null,
            Quantity = entity.Quantity ?? 0,
            WorkplaceAddress = entity.WorkplaceAddress,
        };
    }
}