namespace GHB.DP2.Application.Features.ContractAgreement.ContractDraft;

using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.ContractAgreement.ContractDraft.Dto;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractManagement.CaContractDraftVendorEdit;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft.SectionType;
using GHB.DP2.Domain.Procurement.PPrincipleApproval;
using GHB.DP2.Domain.Procurement.PPurchaseOrder;
using GreatFriends.ThaiBahtText;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

public static class TemplatePlaceholders
{
    public const string MissingValue = "..................................";
}

public record GetVendorReplaceDto
{
    public Guid Id { get; init; }

    public string Email { get; init; }

    public string ContractName { get; init; }

    public string PoNumber { get; init; }

    public string ContractNumber { get; init; }

    public string? CommandText { get; init; }

    public string Budget { get; init; }

    public string? BudgetFormat { get; init; }

    public string? BudgetText { get; init; }

    public string? ContractSignedDate { get; init; }

    public string? ContractSignedDateFormat { get; init; }

    public string ContractType { get; init; }

    public string Template { get; init; }

    public string PeriodConditionType { get; init; }

    public bool IsWorkingDayOnly { get; init; }

    public Guid? ContractDraftDocumentId { get; init; }

    public bool? IsContractDraftDocumentIdReplace { get; init; }

    public Guid? ApprovalContractDraftDocumentId { get; init; }

    public bool? IsApprovalContractDraftDocumentIdReplace { get; init; }

    public Guid? ConfidentialContractDraftDocumentId { get; init; }

    public bool? IsConfidentialContractDraftDocumentIdReplace { get; init; }

    public string? StartDate { get; init; }

    public string? EndDate { get; init; }

    public string? AcceptorDate { get; init; }

    public ContractDraftVendorStatus Status { get; init; }

    public AcceptorResponseDto[]? Acceptors { get; init; }

    public CreatorResponse? Creator { get; init; }

    public ContractDraftInfoReplaceDto ContractDraftInfoDetail { get; init; }

    public AcceptorSignDto? AcceptorSign { get; init; }

    public string? PurchaseOrderNumber { get; init; }

    public static GetVendorReplaceDto FromEntity(
        CaContractDraftVendor vendor,
        string? commandText,
        PPurchaseOrder? purchaseOrder,
        CreatorResponse? creator,
        bool hasCreator,
        bool hasAcceptor,
        LocationDto? location = default,
        PPrincipleApproval? principleApproval = default)
    {
        var acceptors = hasAcceptor
            ? vendor.Acceptors
                    .Where(x => x.Type == AcceptorType.Approver)
                    .Map(DelegatorExtensions.DelegatorToAcceptor)
                    .Map(MapAcceptor)
                    .ToArray()
            : null;

        var userCreator = hasCreator
            ? creator
            : null;

        var acceptorSign = vendor.Acceptors
                                 .Where(x => x.Type == AcceptorType.AcceptorSign)
                                 .Map(DelegatorExtensions.DelegatorToAcceptor)
                                 .Map(MapAcceptorSign)
                                 .FirstOrDefault();

        var documentDate = vendor.Status is (ContractDraftVendorStatus.Pending
                or ContractDraftVendorStatus.Approved)
            ? vendor.DocumentDate?.ToThaiDateString() ?? DateTimeOffset.Now.ToThaiDateString()
            : null;

        var response = new GetVendorReplaceDto
        {
            Id = vendor.Id.Value,
            Email = vendor.Email ?? "-",
            CommandText = commandText ?? "-",
            ContractName = vendor.ContractName ?? "-",
            ContractNumber = vendor.ContractNumber ?? "-",
            PoNumber = vendor.PoNumber ?? "-",
            Budget = vendor.Budget.ToCurrencyStringWithComma() ?? "-",
            BudgetFormat = vendor.Budget.ToCurrencyStringWithComma() ?? "-",
            BudgetText = vendor.Budget.ThaiBahtText() ?? "-",
            ContractSignedDate = vendor.ContractSignedDate?.ToThaiDateString() ?? "-",
            ContractSignedDateFormat = vendor.ContractSignedDate?.ToThaiDateString() ?? "-",
            ContractType = vendor.ContractTypeCode?.Value ?? "-",
            Template = vendor.TemplateCode?.Value ?? "-",
            PeriodConditionType = vendor.PeriodConditionTypeCode?.Value ?? "-",
            IsWorkingDayOnly = vendor.IsWorkingDayOnly,
            Status = vendor.Status,
            ContractDraftInfoDetail = ContractDraftInfoReplaceDto.FromEntity(vendor, location, principleApproval),
            Acceptors = acceptors,
            Creator = userCreator,
            ContractDraftDocumentId = vendor.ContractDraftDocument?.FileId.Value,
            ApprovalContractDraftDocumentId = vendor.ApprovedDocument?.FileId.Value,
            ConfidentialContractDraftDocumentId = vendor.ConfidentialDocument?.FileId.Value,
            AcceptorSign = acceptorSign,
            PurchaseOrderNumber = purchaseOrder?.PurchaseOrderNumber != null ? purchaseOrder?.PurchaseOrderNumber.ToString() : "-",
            StartDate = vendor.StartDate?.ToThaiDateString() ?? "-",
            EndDate = vendor.EndDate?.ToThaiDateString() ?? "-",
            AcceptorDate = documentDate,
        };

        return response;
    }

    private static AcceptorResponseDto MapAcceptor(CaContractDraftAcceptor acceptor)
    {
        return new AcceptorResponseDto(
            acceptor.Id.Value,
            "เห็นชอบ",
            acceptor.Type,
            acceptor.UserId.Value,
            acceptor.Sequence,
            acceptor.FullName,
            acceptor.PositionName,
            acceptor.BusinessUnitName,
            acceptor.Status,
            acceptor.Remark,
            acceptor.ActionAt,
            DelegateId: acceptor.DelegateeId?.Value);
    }

    private static AcceptorSignDto MapAcceptorSign(CaContractDraftAcceptor acceptor)
    {
        return new AcceptorSignDto(
            acceptor.FullName,
            acceptor.PositionName);
    }
}

public class ContractDraftInfoReplaceDto
{
    public BuyerInfoReplaceDto Buyer { get; init; }

    public AgreementBaseReplaceDto? Agreement { get; init; }

    public TermReplaceDto? Payment { get; init; }

    public GuaranteeInfoReplaceDto? Guarantee { get; init; }

    public PenaltyInfoReplaceDto? Penalty { get; init; }

    public RedeliveryReplaceDto? Redelivery { get; init; }

    public AdvancePaymentReplaceDto? AdvancePayment { get; init; }

    public DeliveryInfoReplaceDto? Delivery { get; init; }

    public WarrantyInfoReplaceDto? Warranty { get; init; }

    public TerminationInfoReplaceDto? Termination { get; init; }

    public CopierLeaseInfoReplaceDto? CopierLease { get; init; }

    public ComputerLeaseInfoReplaceDto? ComputerLease { get; init; }

    public CarLeaseInfoReplaceDto? CarLease { get; init; }

    public AttachmentReplaceDto[] Attachments { get; init; }

    public RetentionPaymentReplaceDto? RetentionPayment { get; init; }

    public VendorInfoReplaceDto Vendor { get; init; }

    public static ContractDraftInfoReplaceDto FromEntity(CaContractDraftVendor vendor, LocationDto? location = default, PPrincipleApproval? principleApproval = default)
    {
        return new ContractDraftInfoReplaceDto
        {
            Buyer = BuyerInfoReplaceDto.FromEntity(vendor.Buyer),
            Vendor = VendorInfoReplaceDto.FromEntity(VendorInfo.FromEntity(vendor.Vendor, location)),
            Agreement = AgreementBaseReplaceDto.FromEntity(vendor),
            Payment = TermReplaceDto.FromEntity(vendor),
            Guarantee = GuaranteeInfoReplaceDto.FromEntity(vendor.DraftTermsConditions.Guarantee, vendor.ContractNumber, principleApproval),
            Penalty = PenaltyInfoReplaceDto.FromEntity(vendor.DraftTermsConditions.Penalty),
            Redelivery = RedeliveryReplaceDto.FromEntity(vendor.DraftTermsConditions.RedeliveryCorrection),
            AdvancePayment = AdvancePaymentReplaceDto.FromEntity(vendor.DraftTermsConditions.AdvancePayment),
            Delivery = DeliveryInfoReplaceDto.FromEntity(vendor.Delivery),
            Warranty = WarrantyInfoReplaceDto.FromEntity(vendor.DraftTermsConditions.Warranty),
            Termination = TerminationInfoReplaceDto.FromEntity(vendor.Termination),
            CopierLease = CopierLeaseInfoReplaceDto.FromEntity(vendor.DraftEquipmentRental.CopierLease),
            ComputerLease = ComputerLeaseInfoReplaceDto.FromEntity(vendor.DraftEquipmentRental.LeaseDuration),
            CarLease = CarLeaseInfoReplaceDto.FromEntity(vendor.DraftEquipmentRental.CarLease),
            Attachments =
            [
                .. vendor.Attachments.OrderBy(x => x.Sequence)
                         .Map(Attachment.FromEntity)
                         .Map(AttachmentReplaceDto.FromEntity)
            ],
            RetentionPayment = RetentionPaymentReplaceDto.FromEntity(vendor.DraftTermsConditions.RetentionPayment),
        };
    }

    public static ContractDraftInfoReplaceDto FromEntity(CaContractDraftVendorEdit vendor, CaContractDraftVendor? source = null)
    {
        return new ContractDraftInfoReplaceDto
        {
            Buyer = BuyerInfoReplaceDto.FromEntity(vendor.Buyer),
            Vendor = VendorInfoReplaceDto.FromEntity(VendorInfo.FromEntity(vendor.Vendor, null)),
            Agreement = AgreementBaseReplaceDto.FromEntity(vendor, source),
            Payment = TermReplaceDto.FromEntity(vendor),
            Guarantee = GuaranteeInfoReplaceDto.FromEntity(vendor.DraftTermsConditions.Guarantee, vendor.ContractNumber),
            Penalty = PenaltyInfoReplaceDto.FromEntity(vendor.DraftTermsConditions.Penalty),
            Redelivery = RedeliveryReplaceDto.FromEntity(vendor.DraftTermsConditions.RedeliveryCorrection),
            AdvancePayment = AdvancePaymentReplaceDto.FromEntity(vendor.DraftTermsConditions.AdvancePayment),
            Delivery = DeliveryInfoReplaceDto.FromEntity(vendor.Delivery),
            Warranty = WarrantyInfoReplaceDto.FromEntity(vendor.DraftTermsConditions.Warranty),
            Termination = TerminationInfoReplaceDto.FromEntity(vendor.Termination),
            CopierLease = CopierLeaseInfoReplaceDto.FromEntity(vendor.DraftEquipmentRental.CopierLease),
            ComputerLease = ComputerLeaseInfoReplaceDto.FromEntity(vendor.DraftEquipmentRental.LeaseDuration),
            CarLease = CarLeaseInfoReplaceDto.FromEntity(vendor.DraftEquipmentRental.CarLease),
            Attachments =
            [
                .. vendor.Attachments.OrderBy(x => x.Sequence)
                         .Select(x => new Attachment
                         {
                             Id = x.Id.Value,
                             TypeCode = x.TypeCode.Value,
                             Description = x.Description,
                             PageNumber = x.PageNumber,
                             Sequence = x.Sequence,
                             FormatOtherName = x.FormatOtherName,
                             TypeLabel = x.Type?.Label ?? string.Empty,
                             Files = [.. x.Files
                                         .Select(f => new AttachmentFile
                                         {
                                             FileId = f.Id.Value,
                                             FileName = f.FileName,
                                             FileType = f.FileType,
                                             Sequence = f.Sequence,
                                         })
                                         .OrderBy(f => f.Sequence)],
                         })
                         .Map(AttachmentReplaceDto.FromEntity)
            ],
            RetentionPayment = RetentionPaymentReplaceDto.FromEntity(vendor.DraftTermsConditions.RetentionPayment),
        };
    }
}

public sealed record BuyerInfoReplaceDto(
    string Name,
    string Address,
    LocationInfo Province,
    LocationInfo District,
    LocationInfo SubDistrict)
{
    public static BuyerInfoReplaceDto FromEntity(Buyer entity)
    {
        if (entity.Name == null &&
            entity.Address == null &&
            entity.Province == null &&
            entity.District == null &&
            entity.SubDistrict == null)
        {
            return new BuyerInfoReplaceDto(
                "ธนาคารอาคารสงเคราะห์",
                "ธนาคารอาคารสงเคราะห์ สำนักงานใหญ่ ตั้งอยู่เลขที่ 63 ถนนพระราม 9",
                new LocationInfo("1", "กรุงเทพมหานคร"),
                new LocationInfo("1017", "ห้วยขวาง"),
                new LocationInfo("101701", "ห้วยขวาง"));
        }

        return new BuyerInfoReplaceDto(
            entity.Name ?? "ธนาคารอาคารสงเคราะห์",
            entity.Address ?? "ธนาคารอาคารสงเคราะห์ สำนักงานใหญ่ ตั้งอยู่เลขที่ 63 ถนนพระราม 9",
            entity.Province ?? LocationInfo.Default,
            entity.District ?? LocationInfo.Default,
            entity.SubDistrict ?? LocationInfo.Default);
    }
}

public sealed record DeliveryInfoReplaceDto(
    string Address,
    string? Date,
    string? LeadTime,
    string? LeadTimeText,
    string? LeadTimeTypeCode,
    string? LeadTimeTypeName,
    string? LeadOtherTime,
    string? LeadOtherTimeText,
    string? LeadOtherTimeTypeCode,
    string? LeadOtherTimeTypeName,
    string? CountingConditionCode,
    string? CountingConditionName)
{
    public static DeliveryInfoReplaceDto? FromEntity(Delivery entity)
    {
        if (entity.Address == null &&
            entity.Date == null &&
            entity.LeadTime == null &&
            entity.LeadTimeTypeCode == null &&
            entity.LeadOtherTime == null &&
            entity.LeadOtherTimeTypeCode == null &&
            entity.CountingConditionCode == null)
        {
            return null;
        }

        return new DeliveryInfoReplaceDto(
            entity.Address ?? "-",
            entity.Date is not null ? entity.Date.ToThaiDateString() : "-",
            entity.LeadTime?.ToString() ?? "-",
            entity.LeadTime is not null ? entity.LeadTime.Value.ToThaiNumberText() : "-",
            entity.LeadTimeTypeCode?.Value ?? "-",
            entity.LeadTimeType?.Label ?? "-",
            entity.LeadOtherTime?.ToString() ?? "-",
            entity.LeadOtherTime is not null ? entity.LeadOtherTime.Value.ToThaiNumberText() : "-",
            entity.LeadOtherTimeTypeCode?.Value ?? "-",
            entity.LeadOtherTimeType?.Label ?? "-",
            entity.CountingConditionCode?.Value ?? "-",
            entity.CountingCondition?.Label ?? "-");
    }
}

public sealed record RentalDurationInfoDto(
    string Year,
    string? YearText,
    string Month,
    string? MonthText,
    string Day,
    string? DayText,
    string? FullText);

public sealed record WarrantyInfoReplaceDto(
    bool HasWarranty,
    string? WarrantyConditionCode,
    RentalDurationInfoDto? WarrantyPeriod,
    RentalDurationInfoDto? WarrantyPeriodText,
    RentalDurationInfoDto? FixingDeadlinePeriod,
    RentalDurationInfoDto? FixingDeadlinePeriodText,
    string? WarrantyMonthlyAllowedDowntimeHours,
    string? WarrantyMonthlyAllowedDowntimeHoursText,
    string? WarrantyDowntimePercentPerMonth,
    string? WarrantyDowntimePercentPerMonthText,
    string? WarrantyPenaltyPerHour,
    string? WarrantyPenaltyPerHourText,
    string? DowntimeResolutionHours,
    string? DowntimeResolutionHoursText,
    string? DowntimeResolutionDay,
    string? DowntimeResolutionDayText,
    string? RepairCompletionHours,
    string? RepairCompletionHoursText,
    string? RepairCompletionDay,
    string? RepairCompletionDayText,
    string? RepairDelayPenaltyPercentPerHour,
    string? RepairDelayPenaltyPercentPerHourText,
    string? MaxMonthlyMalfunctionPenaltyPercentageRate,
    string? MaxMonthlyMalfunctionPenaltyPercentageRateText,
    string? MaxMonthlyMalfunctionPenaltyDueDays,
    string? MaxMonthlyMalfunctionPenaltyDueDaysText,
    string? WarrantyMaintenanceCount,
    string? WarrantyMaintenanceCountText,
    string? WarrantyMaintenanceTypeCode,
    string? WarrantyMaintenanceDescription)
{
    public static WarrantyInfoReplaceDto? FromEntity(Warranty entity)
    {
        if (entity.HasWarranty == null &&
            entity.WarrantyConditionCode == null &&
            entity.WarrantyPeriod == null &&
            entity.FixingDeadlinePeriod == null)
        {
            return null;
        }

        var wpFullText = string.Join(" ", new[]
        {
            entity.WarrantyPeriod?.Year > 0 ? string.Format("{0} ({1}) ปี", entity.WarrantyPeriod?.Year, entity.WarrantyPeriod?.Year.Value.ToThaiNumberText()) : null,
            entity.WarrantyPeriod?.Month > 0 ? string.Format("{0} ({1}) เดือน", entity.WarrantyPeriod?.Month, entity.WarrantyPeriod?.Month.Value.ToThaiNumberText()) : null,
            entity.WarrantyPeriod?.Day > 0 ? string.Format("{0} ({1}) วัน", entity.WarrantyPeriod?.Day, entity.WarrantyPeriod?.Day.Value.ToThaiNumberText()) : null,
        }.Where(s => s != null));

        var fdFullText = string.Join(" ", new[]
        {
            entity.FixingDeadlinePeriod?.Year > 0 ? string.Format("{0} ({1}) ปี", entity.FixingDeadlinePeriod?.Year, entity.FixingDeadlinePeriod?.Year.Value.ToThaiNumberText()) : null,
            entity.FixingDeadlinePeriod?.Month > 0 ? string.Format("{0} ({1}) เดือน", entity.FixingDeadlinePeriod?.Month, entity.FixingDeadlinePeriod?.Month.Value.ToThaiNumberText()) : null,
            entity.FixingDeadlinePeriod?.Day > 0 ? string.Format("{0} ({1}) วัน", entity.FixingDeadlinePeriod?.Day, entity.FixingDeadlinePeriod?.Day.Value.ToThaiNumberText()) : null,
        }.Where(s => s != null));

        return new WarrantyInfoReplaceDto(
            entity.HasWarranty ?? false,
            entity.WarrantyConditionCode?.Value,
            new RentalDurationInfoDto(
                entity.WarrantyPeriod?.Year.ToString() ?? "-",
                entity.WarrantyPeriod?.Year is not null ? entity.WarrantyPeriod?.Year.Value.ToThaiNumberText() : "-",
                entity.WarrantyPeriod?.Month.ToString() ?? "-",
                entity.WarrantyPeriod?.Month is not null ? entity.WarrantyPeriod?.Month.Value.ToThaiNumberText() : "-",
                entity.WarrantyPeriod?.Day.ToString() ?? "-",
                entity.WarrantyPeriod?.Day is not null ? entity.WarrantyPeriod?.Day.Value.ToThaiNumberText() : "-",
                wpFullText.Length > 0 ? wpFullText : "-"),
            new RentalDurationInfoDto(
                entity.WarrantyPeriod?.Year > 0 ? string.Format("{0} ({1}) ปี", entity.WarrantyPeriod?.Year, entity.WarrantyPeriod?.Year.Value.ToThaiNumberText()) : string.Empty,
                "-",
                entity.WarrantyPeriod?.Month > 0 ? string.Format("{0} ({1}) เดือน", entity.WarrantyPeriod?.Month, entity.WarrantyPeriod?.Month.Value.ToThaiNumberText()) : string.Empty,
                "-",
                entity.WarrantyPeriod?.Day > 0 ? string.Format("{0} ({1}) วัน", entity.WarrantyPeriod?.Day, entity.WarrantyPeriod?.Day.Value.ToThaiNumberText()) : string.Empty,
                "-",
                wpFullText.Length > 0 ? wpFullText : "-"),
            new RentalDurationInfoDto(
                entity.FixingDeadlinePeriod?.Year.ToString() ?? "-",
                entity.FixingDeadlinePeriod?.Year is not null ? entity.FixingDeadlinePeriod.Year.Value.ToThaiNumberText() : "-",
                entity.FixingDeadlinePeriod?.Month.ToString() ?? "-",
                entity.FixingDeadlinePeriod?.Month is not null ? entity.FixingDeadlinePeriod.Month.Value.ToThaiNumberText() : "-",
                entity.FixingDeadlinePeriod?.Day.ToString() ?? "-",
                entity.FixingDeadlinePeriod?.Day is not null ? entity.FixingDeadlinePeriod.Day.Value.ToThaiNumberText() : "-",
                fdFullText.Length > 0 ? fdFullText : "-"),
            new RentalDurationInfoDto(
                entity.FixingDeadlinePeriod?.Year > 0 ? string.Format("{0} ({1}) ปี", entity.FixingDeadlinePeriod?.Year, entity.FixingDeadlinePeriod?.Year.Value.ToThaiNumberText()) : string.Empty,
                "-",
                entity.FixingDeadlinePeriod?.Month > 0 ? string.Format("{0} ({1}) เดือน", entity.FixingDeadlinePeriod?.Month, entity.FixingDeadlinePeriod?.Month.Value.ToThaiNumberText()) : string.Empty,
                "-",
                entity.FixingDeadlinePeriod?.Day > 0 ? string.Format("{0} ({1}) วัน", entity.FixingDeadlinePeriod?.Day, entity.FixingDeadlinePeriod?.Day.Value.ToThaiNumberText()) : string.Empty,
                "-",
                fdFullText.Length > 0 ? fdFullText : "-"),
            entity.WarrantyMonthlyAllowedDowntimeHours.ToString() ?? "-",
            entity.WarrantyMonthlyAllowedDowntimeHours is not null ? entity.WarrantyMonthlyAllowedDowntimeHours.GetValueOrDefault().ToThaiNumberText() : "-",
            entity.WarrantyDowntimePercentPerMonth.ToCurrencyStringWithComma() ?? "-",
            entity.WarrantyDowntimePercentPerMonth.ToThaiNumberText(isPercent: true) ?? "-",
            entity.WarrantyPenaltyPerHour.ToCurrencyStringWithComma() ?? "-",
            entity.WarrantyPenaltyPerHour.ToThaiNumberText() ?? "-",
            entity.DowntimeResolutionHours is not null ? entity.DowntimeResolutionHours.ToString() : "-",
            entity.DowntimeResolutionHours is not null ? entity.DowntimeResolutionHours.GetValueOrDefault().ToThaiNumberText() : "-",
            entity.DowntimeResolutionDay is not null ? entity.DowntimeResolutionDay.ToString() : "-",
            entity.DowntimeResolutionDay is not null ? entity.DowntimeResolutionDay.GetValueOrDefault().ToThaiNumberText() : "-",
            entity.RepairCompletionHours is not null ? entity.RepairCompletionHours.ToString() : "-",
            entity.RepairCompletionHours is not null ? entity.RepairCompletionHours.GetValueOrDefault().ToThaiNumberText() : "-",
            entity.RepairCompletionDay is not null ? entity.RepairCompletionDay.ToString() : "-",
            entity.RepairCompletionDay is not null ? entity.RepairCompletionDay.GetValueOrDefault().ToThaiNumberText() : "-",
            entity.RepairDelayPenaltyPercentPerHour is not null ? entity.RepairDelayPenaltyPercentPerHour.ToString() : "-",
            entity.RepairDelayPenaltyPercentPerHour is not null ? entity.RepairDelayPenaltyPercentPerHour.GetValueOrDefault().ToThaiNumberText(isPercent: true) : "-",
            entity.MaxMonthlyMalfunctionPenaltyPercentageRate is not null ? entity.MaxMonthlyMalfunctionPenaltyPercentageRate.ToString() : "-",
            entity.MaxMonthlyMalfunctionPenaltyPercentageRate is not null ? entity.MaxMonthlyMalfunctionPenaltyPercentageRate.GetValueOrDefault().ToThaiNumberText(isPercent: true) : "-",
            entity.MaxMonthlyMalfunctionPenaltyDueDays is not null ? entity.MaxMonthlyMalfunctionPenaltyDueDays.ToString() : "-",
            entity.MaxMonthlyMalfunctionPenaltyDueDays is not null ? entity.MaxMonthlyMalfunctionPenaltyDueDays.GetValueOrDefault().ToThaiNumberText() : "-",
            entity.WarrantyMaintenanceCount is not null ? entity.WarrantyMaintenanceCount.ToString() : "-",
            entity.WarrantyMaintenanceCount is not null ? entity.WarrantyMaintenanceCount.Value.ToThaiNumberText() : "-",
            entity.WarrantyMaintenanceTypeCode?.Value ?? "-",
            GetWarrantyMaintenanceDescription(entity));
    }

    private static string GetWarrantyMaintenanceDescription(Warranty entity)
    {
        if (entity.WarrantyMaintenanceCount is null || entity.WarrantyMaintenanceTypeCode is null)
        {
            return "-";
        }

        var periodTypeName = entity.WarrantyMaintenanceTypeCode?.Value switch
        {
            PeriodTypeConstant.PeriodType001 => "วัน",
            PeriodTypeConstant.PeriodType002 => "เดือน",
            PeriodTypeConstant.PeriodType003 => "ปี",
            _ => "-",
        };

        return string.Format("{0}ละ {1} ({2})", periodTypeName, entity.WarrantyMaintenanceCount, entity.WarrantyMaintenanceCount.Value.ToThaiNumberText());
    }
}

public sealed record TerminationInfoReplaceDto(
    string? StartDate,
    string? EndDate,
    RentalDurationInfoDto? Duration,
    RentalDurationInfoDto? DurationText)
{
    public static TerminationInfoReplaceDto? FromEntity(Termination entity)
    {
        var vptFullText = string.Join(" ", new[]
        {
            entity.VendorProcessingTime?.Year > 0 ? string.Format("{0} ({1}) ปี", entity.VendorProcessingTime?.Year, entity.VendorProcessingTime?.Year.Value.ToThaiNumberText()) : null,
            entity.VendorProcessingTime?.Month > 0 ? string.Format("{0} ({1}) เดือน", entity.VendorProcessingTime?.Month, entity.VendorProcessingTime?.Month.Value.ToThaiNumberText()) : null,
            entity.VendorProcessingTime?.Day > 0 ? string.Format("{0} ({1}) วัน", entity.VendorProcessingTime?.Day, entity.VendorProcessingTime?.Day.Value.ToThaiNumberText()) : null,
        }.Where(s => s != null));

        return new TerminationInfoReplaceDto(
            entity.StartDate.ToThaiDateString() ?? "-",
            entity.EndDate.ToThaiDateString() ?? "-",
            new RentalDurationInfoDto(
                entity.VendorProcessingTime?.Year.ToString() ?? "-",
                entity.VendorProcessingTime?.Year is not null ? entity.VendorProcessingTime?.Year.Value.ToThaiNumberText() : "-",
                entity.VendorProcessingTime?.Month.ToString() ?? "-",
                entity.VendorProcessingTime?.Month is not null ? entity.VendorProcessingTime?.Month.Value.ToThaiNumberText() : "-",
                entity.VendorProcessingTime?.Day.ToString() ?? "-",
                entity.VendorProcessingTime?.Day is not null ? entity.VendorProcessingTime?.Day.Value.ToThaiNumberText() : "-",
                "-"),
            new RentalDurationInfoDto(
                entity.VendorProcessingTime?.Year > 0 ? string.Format("{0} ({1}) ปี", entity.VendorProcessingTime?.Year, entity.VendorProcessingTime?.Year.Value.ToThaiNumberText()) : "............",
                "-",
                entity.VendorProcessingTime?.Month > 0 ? string.Format("{0} ({1}) เดือน", entity.VendorProcessingTime?.Month, entity.VendorProcessingTime?.Month.Value.ToThaiNumberText()) : "............",
                "-",
                entity.VendorProcessingTime?.Day > 0 ? string.Format("{0} ({1}) วัน", entity.VendorProcessingTime?.Day, entity.VendorProcessingTime?.Day.Value.ToThaiNumberText()) : "............",
                "-",
                vptFullText.Length > 0 ? vptFullText : "-"));
    }
}

public sealed record CopierLeaseInfoReplaceDto(
    string? MonthlyRentPerMachine,
    string? MonthlyRentPerMachineText,
    string? NumberOfMachines,
    string? NumberOfMachinesText,
    string? TotalMonthlyRent,
    string? TotalMonthlyRentText,
    string? EstimatedMonthlyCopies,
    string? EstimatedMonthlyCopiesText,
    string? BelowEstimateCondition,
    string? BelowEstimateConditionText,
    string? PerCopyRateCondition,
    string? PerCopyRateConditionText)
{
    public static CopierLeaseInfoReplaceDto? FromEntity(LeaseCopier entity)
    {
        if (entity.NumberOfMachines == null &&
            entity.RentalQuantity == null &&
            entity.MonthlyRentalRate == null &&
            entity.EstimatedMonthlyCopyVolume == null &&
            entity.ActualMonthlyCopyVolume == null &&
            entity.CopyRatePerPage == null)
        {
            return null;
        }

        return new CopierLeaseInfoReplaceDto(
            ((decimal?)entity.NumberOfMachines)?.ToCurrencyStringWithComma() ?? "-",
            ((decimal?)entity.NumberOfMachines)?.ThaiBahtText() ?? "-",
            entity.RentalQuantity is not null ? entity.RentalQuantity.Value.ToString("N0") : "-",
            entity.RentalQuantity is not null ? entity.RentalQuantity.Value.ToThaiNumberText() : "-",
            ((decimal?)entity.MonthlyRentalRate)?.ToCurrencyStringWithComma() ?? "-",
            ((decimal?)entity.MonthlyRentalRate)?.ThaiBahtText() ?? "-",
            entity.EstimatedMonthlyCopyVolume is not null ? entity.EstimatedMonthlyCopyVolume.Value.ToString("N0") : "-",
            entity.EstimatedMonthlyCopyVolume is not null ? entity.EstimatedMonthlyCopyVolume.Value.ToThaiNumberText() : "-",
            entity.ActualMonthlyCopyVolume is not null ? entity.ActualMonthlyCopyVolume.Value.ToString("N0") : "-",
            entity.ActualMonthlyCopyVolume is not null ? entity.ActualMonthlyCopyVolume.Value.ToThaiNumberText() : "-",
            ((decimal?)entity.CopyRatePerPage)?.ToCurrencyStringWithComma() ?? "-",
            ((decimal?)entity.CopyRatePerPage)?.ThaiBahtText() ?? "-");
    }
}

public sealed record ComputerLeaseInfoReplaceDto(
    RentalDurationInfoDto Duration,
    string RentalStartCondition)
{
    public static ComputerLeaseInfoReplaceDto? FromEntity(LeaseDuration entity)
    {
        if (entity.Duration == null && entity.CountFromConditionCode == null)
        {
            return null;
        }

        var clFullText = string.Join(" ", new[]
        {
            entity.Duration?.Year > 0 ? string.Format("{0} ({1}) ปี", entity.Duration?.Year, entity.Duration?.Year.Value.ToThaiNumberText()) : null,
            entity.Duration?.Month > 0 ? string.Format("{0} ({1}) เดือน", entity.Duration?.Month, entity.Duration?.Month.Value.ToThaiNumberText()) : null,
            entity.Duration?.Day > 0 ? string.Format("{0} ({1}) วัน", entity.Duration?.Day, entity.Duration?.Day.Value.ToThaiNumberText()) : null,
        }.Where(s => s != null));

        var durationText = new RentalDurationInfoDto(
            entity.Duration?.Year > 0 ? string.Format("{0} ({1}) ปี", entity.Duration?.Year, entity.Duration?.Year.Value.ToThaiNumberText()) : string.Empty,
            "-",
            entity.Duration?.Month > 0 ? string.Format("{0} ({1}) เดือน", entity.Duration?.Month, entity.Duration?.Month.Value.ToThaiNumberText()) : string.Empty,
            "-",
            entity.Duration?.Day > 0 ? string.Format("{0} ({1}) วัน", entity.Duration?.Day, entity.Duration?.Day.Value.ToThaiNumberText()) : string.Empty,
            "-",
            clFullText.Length > 0 ? clFullText : "-");

        return new ComputerLeaseInfoReplaceDto(
            durationText,
            entity.CountFromCondition?.Label ?? "-");
    }
}

public sealed record CarLeaseInfoReplaceDto(
    string RentPerVehicle,
    string RentPerVehicleText,
    string UnitCode)
{
    public static CarLeaseInfoReplaceDto? FromEntity(GHB.DP2.Domain.ContractAgreement.CaContractDraft.SectionType.LeaseCar entity)
    {
        if (entity.RentPerVehicle == null &&
            entity.UnitCode == null)
        {
            return null;
        }

        return new CarLeaseInfoReplaceDto(
            entity.RentPerVehicle.ToCurrencyStringWithComma() ?? "-",
            entity.RentPerVehicle.ThaiBahtText() ?? "-",
            entity.UnitCode?.Value ?? "-");
    }
}

public sealed record AttachmentFileReplaceDto(
    Guid FileId,
    string FileName,
    string FileType,
    int Sequence)
{
    public static AttachmentFileReplaceDto FromEntity(AttachmentFile entity)
    {
        return new AttachmentFileReplaceDto(
            entity.FileId,
            entity.FileName,
            entity.FileType,
            entity.Sequence);
    }
}

public sealed record AttachmentReplaceDto(
    Guid? Id,
    string? TypeCode,
    string? Description,
    string? PageNumber,
    int Sequence,
    AttachmentFileReplaceDto[] Files)
{
    public static AttachmentReplaceDto FromEntity(Attachment entity)
    {
        var typeName = $"ผนวก {entity.Sequence} {(entity.TypeCode == "CAppendOther001" ? entity.FormatOtherName ?? "-" : entity.TypeLabel ?? "-")}";

        return new AttachmentReplaceDto(
            entity.Id,
            typeName,
            entity.Description,
            string.Format("จำนวน {0} หน้า", entity.PageNumber > 0 ? entity.PageNumber.ToString() : "-"),
            entity.Sequence,
            [.. entity.Files.Map(AttachmentFileReplaceDto.FromEntity)]);
    }
}

public sealed record RetentionPaymentReplaceDto(
    bool HasRetentionPayment,
    string Amount,
    string? AmountFormat,
    string? AmountText,
    string Percentage,
    string? PercentageText)
{
    public static RetentionPaymentReplaceDto? FromEntity(GHB.DP2.Domain.ContractAgreement.CaContractDraft.SectionType.RetentionPayment entity)
    {
        if (entity.IsIncluded == null &&
            entity.Amount == null &&
            entity.Percentage == null)
        {
            return null;
        }

        return new RetentionPaymentReplaceDto(
            entity.IsIncluded ?? false,
            entity.Amount.ToCurrencyStringWithComma() ?? "-",
            entity.Amount?.ToCurrencyStringWithComma() ?? "-",
            entity.Amount.ThaiBahtText() ?? "-",
            entity.Percentage.ToCurrencyStringWithComma() ?? "-",
            entity.Percentage.ToThaiNumberText(isPercent: true) ?? "-");
    }
}

public sealed record VendorInfoReplaceDto(
    string Type,
    string Name,
    string TaxpayerIdentificationNo,
    string? RegistrationPlace,
    string? VendorRegistrationPlace,
    string? Address,
    string? Street,
    string? SubDistrictName,
    string? DistrictName,
    string? ProvinceName,
    string? PostalCode,
    string? StartDate,
    string? EndDate)
{
    public static VendorInfoReplaceDto FromEntity(VendorInfo entity)
    {
        return new VendorInfoReplaceDto(
            entity.Type.GetDescription() ?? "-",
            entity.Name ?? "-",
            entity.TaxpayerIdentificationNo ?? "-",
            entity.RegistrationPlace ?? "-",
            entity.VendorRegistrationPlace ?? "-",
            entity.Address ?? "-",
            entity.Street ?? "-",
            entity.SubDistrictName ?? "-",
            entity.DistrictName ?? "-",
            entity.ProvinceName ?? "-",
            entity.PostalCode ?? "-",
            entity.StartDate.ToThaiDateString() ?? "-",
            entity.EndDate.ToThaiDateString() ?? "-");
    }
}

public sealed record GuaranteeInfoReplaceDto(
    bool HasGuarantee,
    string? TypeCode,
    string? TypeName,
    string? ReferenceNumber,
    string? GuaranteeDate,
    string Amount,
    string? AmountFormat,
    string? AmountText,
    string? Percentage,
    string? PercentageText,
    string? GuaranteeTitle,
    string? GuaranteeDiscription)
{
    public static GuaranteeInfoReplaceDto? FromEntity(Guarantee entity, string? contractNumber = default, PPrincipleApproval? principleApproval = default)
    {
        if (entity.IsSubmitted == null &&
            entity.TypeCode == null &&
            entity.Amount == null &&
            entity.Percentage == null)
        {
            return null;
        }

        var referenceNumber = entity.ReferenceNumber != null ? string.Format(" เลขที่ {0}", entity.ReferenceNumber) : "-";

        if (entity.TypeCode?.Value == BondTypeConstant.PBondType002
            || entity.TypeCode?.Value == BondTypeConstant.PBondType003
            || entity.TypeCode?.Value == BondTypeConstant.PBondType005
            || entity.TypeCode?.Value == BondTypeConstant.PBondType006)
        {
            referenceNumber = string.Format(" {0} สาขา {1} เลขที่ {2}", entity.Bank?.Label, entity.BankBranch, entity.BankAccountNumber);
        }

        var guaranteeTitle = string.Empty;
        var guaranteeDiscription = string.Empty;

        if (entity.IsSubmitted is true)
        {
            guaranteeTitle = "ข้อ 16. เงินประกันการเช่า";
            guaranteeDiscription = string.Format(
                "ในการทำสัญญาเช่าอาคารสถานที่ฉบับนี้ ผู้เช่าได้วางเงินประกันการเช่าเป็นจำนวนเงิน {0} บาท ({1}) โดยโอนเงินประกันการเช่าจากสัญญาเช่าพื้นที่เพื่อจัดตั้งเป็นที่ทำการสาขา{2} ของธนาคารอาคารสงเคราะห์ เลขที่สัญญา {3} ลงวันที่.......................... ไว้แก่ผู้ให้เช่าเพื่อเป็นประกันความเสียหายที่อาจเกิดขึ้นในอนาคต โดยเงินประกันการเช่าดังกล่าว ผู้ให้เช่าจะคืนให้แก่ผู้เช่าเมื่อสัญญาสิ้นสุดลงภายใน 30 (สามสิบ) วัน และไม่เกิดความเสียหายต่อสถานที่เช่าอันเป็นความผิดของผู้เช่าแต่ปรากฏว่าความเสียหายนั้นเกิดจากความผิดของผู้เช่า ผู้เช่ายินยอมให้ผู้ให้เช่าหักค่าเสียหายเพียงเท่าที่เกิดขึ้นจริงจากเงินประกันการเช่าดังกล่าวเมื่อหักค่าเสียหายแล้วมีเงินเหลืออยู่เท่าใด ผู้ให้เช่าตกลงคืนให้กับผู้เช่าภายในกำหนดเวลาข้าข้างต้น ทั้งนี้ ความเสียหายที่เกิดขึ้นกับสถานที่เช่านั้นไม่รวมถึงการใช้งานที่เป็นไปตามปกติวิสัยของการเช่า",
                entity.Amount.ToCurrencyStringWithComma(),
                entity.Amount.ThaiBahtText(),
                principleApproval?.BranchLocation,
                contractNumber);
        }

        return new GuaranteeInfoReplaceDto(
            entity.IsSubmitted ?? false,
            entity.TypeCode?.Value ?? "-",
            entity.Type?.Label ?? "-",
            referenceNumber ?? "-",
            entity.GuaranteeDate?.ToThaiDateString() ?? "-",
            entity.Amount.ToCurrencyStringWithComma() ?? "-",
            entity.Amount > 0 ? entity.Amount?.ToCurrencyStringWithComma() : "-",
            entity.Amount > 0 ? entity.Amount.ThaiBahtText() : "-",
            entity.Percentage > 0 ? entity.Percentage.ToCurrencyStringWithComma() : "-",
            entity.Percentage > 0 ? entity.Percentage.ToThaiNumberText(isPercent: true) : "-",
            guaranteeTitle,
            guaranteeDiscription);
    }
}

public sealed record PenaltyInfoReplaceDto(
    string TypeCode,
    string TypeName,
    string? Rate,
    string? RateText,
    string? Amount,
    string? AmountFormat,
    string? AmountText,
    string RateTypeCode,
    string RateTypeName)
{
    public static PenaltyInfoReplaceDto? FromEntity(Penalty entity)
    {
        return new PenaltyInfoReplaceDto(
            entity.TypeCode?.Value ?? "-",
            entity.Type?.Label ?? "-",
            entity.Rate > 0 ? $"{entity.Rate.Value:0.00#}" : "-",
            entity.Rate > 0 ? entity.Rate.ToThaiNumberText(isPercent: true) : "-",
            entity.Amount > 0 ? entity.Amount?.ToCurrencyStringWithComma() : "-",
            entity.Amount > 0 ? entity.Amount?.ToCurrencyStringWithComma() : "-",
            entity.Amount > 0 ? entity.Amount.ThaiBahtText() : "-",
            entity.RateTypeCode?.Value ?? "-",
            entity.RateType?.Label ?? "-");
    }
}

public class TermReplaceDto
{
    public string? PaymentTypeCode { get; init; }

    public PaymentTermDetail[] Details { get; init; }

    public string? DueDay { get; init; }

    public string? DueDayText { get; init; }

    public string? RedeliveryTypeCode { get; init; }

    public string TotalAdvanceDeductionAmount { get; init; }

    public string TotalAdvanceDeductionAmountText { get; init; }

    public string PaymentTermCount { get; init; }

    public string PaymentTermCountText { get; init; }

    public string? PaymentSummary { get; init; }

    public static TermReplaceDto FromEntity(CaContractDraftVendor vendor)
    {
        var totalAdvanceDeductionAmount = vendor.PaymentTerms?.Sum(x => x.AdvanceDeductionAmount) ?? 0m;
        var paymentTermCount = vendor.PaymentTerms?.Count() ?? 0;
        var details = BuildDetailsWithWorkDescription(vendor);
        var templateCode = vendor.TemplateCode?.Value;

        return new TermReplaceDto
        {
            DueDay = vendor.Payment.DueDays?.ToString("N0") ?? "-",
            DueDayText = vendor.Payment.DueDays.HasValue ? vendor.Payment.DueDays.Value.ToThaiNumberText() : "-",
            RedeliveryTypeCode = vendor.Payment.RedeliveryDate?.Label ?? "-",
            PaymentTypeCode = vendor.Payment.Type?.Label ?? "-",
            Details = details,
            TotalAdvanceDeductionAmount = vendor.PaymentTerms != null ? totalAdvanceDeductionAmount.ToCurrencyStringWithComma() : "-",
            TotalAdvanceDeductionAmountText = vendor.PaymentTerms != null ? totalAdvanceDeductionAmount.ThaiBahtText() : "-",
            PaymentTermCount = vendor.PaymentTerms != null ? paymentTermCount.ToString("N0") : "-",
            PaymentTermCountText = vendor.PaymentTerms != null ? paymentTermCount.ToThaiNumberText() : "-",
            PaymentSummary = PaymentTermDetail.GenerateSingleWorkDescription(
                templateCode,
                details,
                vendor.Agreement?.TotalAmount,
                vendor.Agreement?.VatRateTypeCode?.Value),
        };
    }

    public static TermReplaceDto FromEntity(CaContractDraftVendorEdit vendor)
    {
        var totalAdvanceDeductionAmount = vendor.PaymentTerms?.Sum(x => x.AdvanceDeductionAmount) ?? 0m;
        var paymentTermCount = vendor.PaymentTerms?.Count() ?? 0;
        var details = BuildDetailsWithWorkDescription(vendor);
        var templateCode = vendor.TemplateCode?.Value;

        return new TermReplaceDto
        {
            DueDay = vendor.Payment.DueDays?.ToString("N0") ?? "-",
            DueDayText = vendor.Payment.DueDays.HasValue ? vendor.Payment.DueDays.Value.ToThaiNumberText() : "-",
            RedeliveryTypeCode = vendor.Payment.RedeliveryDate?.Label ?? "-",
            PaymentTypeCode = vendor.Payment.Type?.Label ?? "-",
            Details = details,
            TotalAdvanceDeductionAmount = vendor.PaymentTerms != null ? totalAdvanceDeductionAmount.ToCurrencyStringWithComma() : "-",
            TotalAdvanceDeductionAmountText = vendor.PaymentTerms != null ? totalAdvanceDeductionAmount.ThaiBahtText() : "-",
            PaymentTermCount = vendor.PaymentTerms != null ? paymentTermCount.ToString("N0") : "-",
            PaymentTermCountText = vendor.PaymentTerms != null ? paymentTermCount.ToThaiNumberText() : "-",
            PaymentSummary = PaymentTermDetail.GenerateSingleWorkDescription(
                templateCode,
                details,
                vendor.Agreement?.TotalAmount,
                vendor.Agreement?.VatRateTypeCode?.Value),
        };
    }

    private static PaymentTermDetail[] BuildDetailsWithWorkDescription(CaContractDraftVendor vendor)
    {
        var details = (vendor.PaymentTerms ?? [])
            .OrderBy(x => x.Sequence)
            .Select(x => PaymentTermDetail.FromEntity(x, vendor.Payment.TypeCode?.Value))
            .ToArray();

        var templateCode = vendor.TemplateCode?.Value;

        for (int i = 0; i < details.Length; i++)
        {
            bool isLastTerm = i == details.Length - 1;
            details[i].WorkDescription = PaymentTermDetail.GenerateWorkDescription(details[i], templateCode, isLastTerm);
        }

        return details;
    }

    private static PaymentTermDetail[] BuildDetailsWithWorkDescription(CaContractDraftVendorEdit vendor)
    {
        var details = (vendor.PaymentTerms ?? [])
            .OrderBy(x => x.Sequence)
            .Select(x => PaymentTermDetail.FromEntity(x, vendor.Payment.TypeCode?.Value))
            .ToArray();

        var templateCode = vendor.TemplateCode?.Value;

        for (int i = 0; i < details.Length; i++)
        {
            bool isLastTerm = i == details.Length - 1;
            details[i].WorkDescription = PaymentTermDetail.GenerateWorkDescription(details[i], templateCode, isLastTerm);
        }

        return details;
    }
}

public class AgreementBaseReplaceDto
{
    public string ItemDetail { get; init; }

    public string VatRateTypeCode { get; init; }

    public string AgreementPrice { get; init; }

    public string? AgreementPriceFormat { get; init; }

    public string? AgreementPriceText { get; init; }

    public string? VatAmount { get; init; }

    public string? VatAmountFormat { get; init; }

    public string? VatAmountText { get; init; }

    public string TotalAmount { get; init; }

    public string? TotalAmountFormat { get; init; }

    public string? TotalAmountText { get; init; }

    public string Quantity { get; init; }

    public string UnitCode { get; init; }

    // ExchangeGiver specific
    public bool IsExchangeGiver { get; init; }

    // Workplace specific
    public string WorkplaceAddress { get; init; }

    public LocationInfo WorkplaceProvince { get; init; }

    public LocationInfo WorkplaceDistrict { get; init; }

    public LocationInfo WorkplaceSubDistrict { get; init; }

    // WorkplaceSerialNumber specific
    public string? SerialNumber { get; init; }

    // RentalDuration specific
    public RentalDurationInfoDto? Duration { get; init; }

    public RentalDurationInfoDto? DurationText { get; init; }

    public string StartDate { get; init; }

    public string EndDate { get; init; }

    // Lease specific
    public string Brand { get; init; }

    public string Model { get; init; }

    // LeaseCar specific
    public string EngineCapacityCc { get; init; }

    public string EngineCapacityCcText { get; init; }

    // การแสดงผลค่าจ้างและการจ่ายเงินตามประเภทการจ่ายเงิน (ก) และ (ข)
    public string PaymentConditionTypeLText { get; init; }

    public static AgreementBaseReplaceDto? FromEntity(CaContractDraftVendorEdit vendor, CaContractDraftVendor? source = null)
        => FromEntityCore(
            vendor.Agreement,
            vendor.Payment,
            fallbackStartDate: source?.ContractStartDate,
            fallbackEndDate: source?.ContractEndDate);

    public static AgreementBaseReplaceDto? FromEntity(CaContractDraftVendor vendor)
        => FromEntityCore(vendor.Agreement, vendor.Payment);

    private static AgreementBaseReplaceDto? FromEntityCore(
        AgreementContract? agreement,
        Payment payment,
        DateTimeOffset? fallbackStartDate = null,
        DateTimeOffset? fallbackEndDate = null)
    {
        if (agreement == null)
        {
            return new AgreementBaseReplaceDto
            {
                ItemDetail = "-",
                VatRateTypeCode = "-",
                AgreementPrice = "-",
                AgreementPriceFormat = "-",
                AgreementPriceText = "-",
                VatAmount = "-",
                VatAmountFormat = "-",
                VatAmountText = "-",
                TotalAmount = "-",
                TotalAmountFormat = "-",
                TotalAmountText = "-",
                Quantity = "-",
                UnitCode = "-",
                PaymentConditionTypeLText = "-",
            };
        }

        var rdYear = agreement.RentalDuration?.Year;
        var rdMonth = agreement.RentalDuration?.Month;
        var rdDay = agreement.RentalDuration?.Day;

        var rentalDuration = new RentalDurationInfoDto(
            rdYear?.ToString() ?? "-",
            rdYear.HasValue ? rdYear.Value.ToThaiNumberText() : "-",
            rdMonth?.ToString() ?? "-",
            rdMonth.HasValue ? rdMonth.Value.ToThaiNumberText() : "-",
            rdDay?.ToString() ?? "-",
            rdDay.HasValue ? rdDay.Value.ToThaiNumberText() : "-",
            "-");

        var rdFullText = string.Join(" ", new[]
        {
            rdYear.HasValue && rdYear.Value > 0 ? string.Format("{0} ({1}) ปี", rdYear, rdYear.Value.ToThaiNumberText()) : null,
            rdMonth.HasValue && rdMonth.Value > 0 ? string.Format("{0} ({1}) เดือน", rdMonth, rdMonth.Value.ToThaiNumberText()) : null,
            rdDay.HasValue && rdDay.Value > 0 ? string.Format("{0} ({1}) วัน", rdDay, rdDay.Value.ToThaiNumberText()) : null,
        }.Where(s => s != null));

        var durationText = new RentalDurationInfoDto(
            rdYear.HasValue && rdYear.Value > 0 ? string.Format("{0} ({1}) ปี", rdYear, rdYear.Value.ToThaiNumberText()) : string.Empty,
            "-",
            rdMonth.HasValue && rdMonth.Value > 0 ? string.Format("{0} ({1}) เดือน", rdMonth, rdMonth.Value.ToThaiNumberText()) : string.Empty,
            "-",
            rdDay.HasValue && rdDay.Value > 0 ? string.Format("{0} ({1}) วัน", rdDay, rdDay.Value.ToThaiNumberText()) : string.Empty,
            "-",
            rdFullText.Length > 0 ? rdFullText : "-");

        return new AgreementBaseReplaceDto
        {
            ItemDetail = agreement.ContractItem ?? "-",
            VatRateTypeCode = agreement.VatRateTypeCode?.Value ?? "-",
            AgreementPrice = agreement.Price?.ToCurrencyStringWithComma() ?? "-",
            AgreementPriceFormat = agreement.Price?.ToCurrencyStringWithComma() ?? "-",
            AgreementPriceText = agreement.Price is { } price ? price.ThaiBahtText() : "-",
            VatAmount = agreement.VatAmount?.ToCurrencyStringWithComma() ?? "-",
            VatAmountFormat = agreement.VatAmount?.ToCurrencyStringWithComma() ?? "-",
            VatAmountText = agreement.VatAmount?.ThaiBahtText() ?? "-",
            TotalAmount = agreement.TotalAmount?.ToCurrencyStringWithComma() ?? "-",
            TotalAmountFormat = agreement.TotalAmount?.ToCurrencyStringWithComma() ?? "-",
            TotalAmountText = agreement.TotalAmount?.ThaiBahtText() ?? "-",
            Quantity = agreement.Quantity.GetValueOrDefault().ToString("N0") ?? "-",
            UnitCode = agreement.UnitCode?.Value ?? "-",
            Duration = rentalDuration,
            DurationText = durationText,
            StartDate = (agreement.StartDate ?? fallbackStartDate)?.ToThaiDateString() ?? "-",
            EndDate = (agreement.EndDate ?? fallbackEndDate)?.ToThaiDateString() ?? "-",
            IsExchangeGiver = agreement.IsExchangeGiver ?? false,
            WorkplaceAddress = agreement.WorkplaceAddress ?? "-",
            WorkplaceProvince = agreement.WorkplaceProvince ?? LocationInfo.Default,
            WorkplaceDistrict = agreement.WorkplaceDistrict ?? LocationInfo.Default,
            WorkplaceSubDistrict = agreement.WorkplaceSubDistrict ?? LocationInfo.Default,
            SerialNumber = agreement.SerialNumber ?? "-",
            Brand = agreement.Brand ?? "-",
            Model = agreement.Model ?? "-",
            EngineCapacityCc = agreement.EngineCapacityCc?.ToCurrencyStringWithComma() ?? "-",
            EngineCapacityCcText = agreement.EngineCapacityCc is { } ecc ? ecc.ToThaiNumberText() : "-",
            PaymentConditionTypeLText = payment.TypeCode.HasValue ? GetPaymentConditionTypeLText(payment.TypeCode.Value.Value, agreement) : "-",
        };
    }

    private static string GetPaymentConditionTypeLText(
        string? type,
        AgreementContract agreement)
    {
        var totalAmount = agreement.TotalAmount.HasValue ? agreement.TotalAmount.ToCurrencyStringWithComma() : string.Empty;
        var totalAmountText = agreement.TotalAmount.HasValue ? agreement.TotalAmount.ThaiBahtText() : string.Empty;
        var vatAmount = agreement.VatAmount.HasValue ? agreement.VatAmount.ToCurrencyStringWithComma() : string.Empty;
        var vatAmountText = agreement.VatAmount.HasValue ? agreement.VatAmount.ThaiBahtText() : string.Empty;

        var paymentConditionType = type switch
        {
            "PayType001" =>
                $"(ก) สำหรับการจ่ายเงินค่าจ้างให้ผู้รับจ้างเป็นงวด\n\t\t\tผู้ว่าจ้างตกลงจ่ายและผู้รับจ้างตกลงรับเงินค่าจ้างจำนวนเงิน {totalAmount} บาท ({totalAmountText}) ซึ่งได้รวมภาษีมูลค่าเพิ่ม จำนวน {vatAmount} บาท ({vatAmountText}) ตลอดจนภาษีอากรอื่น ๆ และค่าใช้จ่ายทั้งปวงด้วยแล้ว โดยกำหนดการจ่ายเงินเป็นงวด ๆ ดังนี้",
            "PayType002" =>
                $"(ข) สำหรับการจ่ายเงินค่าจ้างให้ผู้รับจ้างครั้งเดียว\n\t\t\tผู้ว่าจ้างตกลงจ่ายและผู้รับจ้างตกลงรับเงินค่าจ้างจำนวนเงิน {totalAmount} บาท ({totalAmountText}) ซึ่งได้รวมภาษีมูลค่าเพิ่ม จำนวน {vatAmount} บาท ({vatAmountText}) ตลอดจนภาษีอากรอื่นๆ และค่าใช้จ่ายทั้งปวงด้วยแล้ว เมื่อผู้รับจ้างได้ปฏิบัติงานทั้งหมดให้แล้วเสร็จเรียบร้อยตามสัญญาและผู้ว่าจ้างได้ตรวจรับงานจ้างตามข้อ 11 ไว้โดยครบถ้วนแล้ว\nการจ่ายเงินตามเงื่อนไขแห่งสัญญานี้ ผู้ว่าจ้างจะโอนเงินเข้าบัญชีเงินฝากธนาคารของผู้รับจ้าง ชื่อธนาคาร……….…..…….…….….สาขา……….…..…….…….….ชื่อบัญชี……….…..…….…….….เลขที่บัญชี……….…..…….…….….ทั้งนี้ ผู้รับจ้างตกลงเป็นผู้รับภาระเงินค่าธรรมเนียมหรือค่าบริการอื่นใดเกี่ยวกับการโอน รวมทั้งค่าใช้จ่ายอื่นใด (ถ้ามี) ที่ธนาคารเรียกเก็บ และยินยอมให้มีการหักเงินดังกล่าว\nจากจำนวนเงินโอนในงวดนั้น ๆ (ความในวรรคนี้ใช้สำหรับกรณีที่หน่วยงานของรัฐจะจ่ายเงินตรงให้แก่ผู้รับจ้าง (ระบบ Direct Payment) โดยการโอนเงินเข้าบัญชีเงินฝากธนาคารของผู้รับจ้างตามแนวทางที่กระทรวงการคลังหรือหน่วยงานของรัฐเจ้าของงบประมาณเป็นผู้กำหนด แล้วแต่กรณี)",
            _ => "-",
        };

        return paymentConditionType;
    }
}

public class RedeliveryReplaceDto
{
    // Acceptance specific
    public string Description { get; init; }

    public RentalDurationInfo RentalDuration { get; init; }

    // Redelivery specific
    public string RedeliveryDeadline { get; init; }

    public string RedeliveryDeadlineText { get; init; }

    public string RedeliveryDeadlineTypeCode { get; init; }

    public string CorrectionDue { get; init; }

    public string CorrectionDueText { get; init; }

    public string CorrectionDueTypeCode { get; init; }

    public static RedeliveryReplaceDto? FromEntity(RedeliveryCorrection? entity)
    {
        if (entity == null)
        {
            return null;
        }

        return entity.Type switch
        {
            RedeliveryType.Acceptance => new RedeliveryReplaceDto
            {
                Description = entity.Description ?? "-",
                RentalDuration = entity.RentalDuration ?? RentalDurationInfo.Default,
            },
            RedeliveryType.Redelivery => new RedeliveryReplaceDto
            {
                RedeliveryDeadline = entity.RedeliveryDeadline is not null ? entity.RedeliveryDeadline.Value.ToString("N2") : "-",
                RedeliveryDeadlineText = entity.RedeliveryDeadline is not null ? entity.RedeliveryDeadline.Value.ToThaiNumberText() : "-",
                RedeliveryDeadlineTypeCode = entity.RedeliveryDeadlineType?.Label ?? "-",
                CorrectionDue = entity.CorrectionDue is not null ? entity.CorrectionDue.Value.ToString("N2") : "-",
                CorrectionDueText = entity.CorrectionDue is not null ? entity.CorrectionDue.Value.ToThaiNumberText() : "-",
                CorrectionDueTypeCode = entity.CorrectionDueType?.Label ?? "-",
            },
            _ => null,
        };
    }
}

public sealed record AdvancePaymentReplaceDto(
    bool HasAdvancePayment,
    string? Amount,
    string? AmountFormat,
    string? AmountText,
    string Percentage,
    string PercentageText,
    string? DueDate,
    string? ConditionCode)
{
    public static AdvancePaymentReplaceDto? FromEntity(GHB.DP2.Domain.ContractAgreement.CaContractDraft.SectionType.AdvancePayment entity)
    {
        if (entity.IsIncluded == null &&
            entity.Amount == null &&
            entity.Percentage == null &&
            entity.DueDate == null &&
            entity.ConditionCode == null)
        {
            return null;
        }

        return new AdvancePaymentReplaceDto(
            entity.IsIncluded ?? false,
            entity.Amount > 0 ? entity.Amount.ToString() : "-",
            entity.Amount > 0 ? entity.Amount.Value.ToCurrencyStringWithComma() : "-",
            entity.Amount.ThaiBahtText() ?? "-",
            entity.Percentage > 0 ? entity.Percentage.Value.ToCurrencyStringWithCommaTwoDigit() : "-",
            entity.Percentage > 0 ? entity.Percentage.Value.ToThaiNumberText(isPercent: true) : "-",
            entity.DueDate?.ToString() ?? "-",
            entity.ConditionCode?.Value ?? "-");
    }
}

public record AcceptorResponseDto(
    Guid Id,
    string Action,
    AcceptorType AcceptorType,
    Guid UserId,
    int Sequence,
    string FullName,
    string PositionName,
    string DepartmentName,
    AcceptorStatus Status,
    string? Remark = default,
    DateTimeOffset? ActionAt = default,
    string? CommitteePositionsCode = default,
    string? CommitteePositionName = default,
    bool? IsUnableToPerformDuties = default,
    string? DepartmentCode = default,
    Guid? DelegateId = default,
    bool IsCurrent = default) : AcceptorResponseBase<Guid>(Id, AcceptorType, UserId, Sequence, FullName, PositionName, DepartmentName, Status, Remark, ActionAt, CommitteePositionsCode, CommitteePositionName,
    IsUnableToPerformDuties, DepartmentCode, DelegateId, IsCurrent);

public record LocationDto(
    string? ProvinceName,
    string? DistrictName,
    string? SubDistrictName);

public record AcceptorSignDto(
    string? FullName,
    string? PositionName);

public class GetListMappingContractDraftDocumentEndpoint : EndpointBase<Ok<ResponseDtoDescriptionExtension>>
{
    public GetListMappingContractDraftDocumentEndpoint(ILogger<GetListMappingContractDraftDocumentEndpoint> logger)
        : base(logger)
    {
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags(nameof(ContractDraft)));
        this.Get("procurement/contract-draft/mapping-document");
    }

    protected override async ValueTask<Ok<ResponseDtoDescriptionExtension>> HandleRequestAsync(CancellationToken ct)
    {
        await Task.CompletedTask;

        var dtoType = typeof(GetVendorReplaceDto);

        var result = DtoDescriptionExtensions.ToDictionary(dtoType);

        return TypedResults.Ok(result);
    }
}