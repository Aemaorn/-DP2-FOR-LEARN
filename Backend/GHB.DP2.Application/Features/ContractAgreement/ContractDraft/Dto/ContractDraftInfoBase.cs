namespace GHB.DP2.Application.Features.ContractAgreement.ContractDraft.Dto;

using System.IdentityModel.Tokens.Jwt;
using FluentValidation;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.ContractAgreement.ContractInvitation.Abstract;
using GHB.DP2.Application.Features.Procurement.Invite.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft.SectionType;
using GHB.DP2.Domain.ContractAgreement.CaContractInvitation;
using GHB.DP2.Domain.Procurement.PpTorDraft;
using GHB.DP2.Domain.SystemUtility;
using GreatFriends.ThaiBahtText;

public record ContractDraftRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid ProcurementId,
    Guid ContractDraftId) : ContractDraftInfoBase
{
    public class Validator : Validator<ContractDraftRequest>
    {
        public Validator()
        {
            this.RuleFor(c => c.Email)
                .NotEmpty()
                .NotNull()
                .WithMessage("กรุณากรอกข้อมูล Email");

            this.RuleFor(c => c.Budget)
                .NotEmpty()
                .NotNull()
                .WithMessage("กรุณากรอกข้อมูล Budget");

            this.RuleFor(c => c.ContractType)
                .NotEmpty()
                .NotNull()
                .WithMessage("กรุณากรอกข้อมูล ContractType");

            this.RuleFor(c => c.Template)
                .NotEmpty()
                .NotNull()
                .WithMessage("กรุณากรอกข้อมูล Template");
        }
    }
}

public abstract record ContractDraftInfoBase
{
    public Guid Id { get; init; }

    public bool IsSaveDraft { get; init; }

    public string Email { get; init; }

    public string ContractName { get; init; }

    public string PoNumber { get; init; }

    public string ContractNumber { get; init; }

    public decimal Budget { get; init; }

    public DateTimeOffset? ContractSignedDate { get; init; }

    public DateTimeOffset? ContractEndDate { get; init; }

    public string ContractType { get; init; }

    public string Template { get; init; }

    public string? TemplateText { get; init; }

    public string? SubTemplate { get; init; }

    public string? SubTemplateText { get; init; }

    public string? PeriodConditionType { get; init; }

    public DateTimeOffset? StartDate { get; init; }

    public DateTimeOffset? EndDate { get; init; }

    public bool IsWorkingDayOnly { get; init; }

    public Guid? ContractDraftDocumentId { get; init; }

    public bool? IsContractDraftDocumentIdReplace { get; init; }

    public Guid? ApprovalContractDraftDocumentId { get; init; }

    public bool? IsApprovalContractDraftDocumentIdReplace { get; init; }

    public Guid? ConfidentialContractDraftDocumentId { get; init; }

    public bool? IsConfidentialContractDraftDocumentIdReplace { get; init; }

    public ContractDraftVendorStatus Status { get; init; }

    public DateTimeOffset? DocumentDate { get; init; }

    public ContractDraftDetail Detail { get; init; }

    public bool? EgpResult { get; init; }

    public string? EgpRemark { get; init; }

    public DateTimeOffset? EgpDate { get; init; }

    public bool? CoiResult { get; init; }

    public string? CoiRemark { get; init; }

    public DateTimeOffset? CoiDate { get; init; }

    public bool? WatchlistResult { get; init; }

    public string? WatchlistRemark { get; init; }

    public DateTimeOffset? WatchlistDate { get; init; }

    public QualificationResultDto? CoiCheckerResult { get; init; }

    public QualificationResultDto? WatchlistCheckerResult { get; init; }

    public ShareholderDto[]? Shareholder { get; init; }

    public string? VatRateTypeCode { get; init; }

    public IEnumerable<Operators>? Operators { get; init; }

    public AcceptorRequest[]? Acceptors { get; init; }

    public CaContractDraftVendor Upsert(CaContractDraftVendor vendor, SuUser[]? users = null, UserId? sendToAcceptorId = null)
    {
        _ = vendor
            .SetEmail(this.Email)
            .SetContractName(this.ContractName)
            .SetPoNumber(this.PoNumber)
            .SetContractNumber(this.ContractNumber)
            .SetBudget(this.Budget)
            .SetContractType(this.ContractType)
            .SetTemplate(this.Template)
            .SetTemplateText(this.TemplateText)
            .SetSubTemplate(this.SubTemplate)
            .SetSubTemplateText(this.SubTemplateText)
            .SetPeriodConditionType(this.PeriodConditionType)
            .SetIsWorkingDayOnly(this.IsWorkingDayOnly)
            .SetBuyer(this.Detail.Buyer.MapToEntity())
            .SetCoi(this.CoiResult, this.CoiRemark, this.CoiDate)
            .SetWatchlist(this.WatchlistResult, this.WatchlistRemark, this.WatchlistDate)
            .SetEgp(this.EgpResult, this.EgpRemark, this.EgpDate);

        if (this.PeriodConditionType == "CSDPCond003")
        {
            vendor.SetStartDate(this.StartDate)
                  .SetEndDate(this.EndDate);
        }

        if (this.CoiCheckerResult is not null)
        {
            vendor.AddChecker(
                QualificationType.COI,
                this.CoiCheckerResult.Result,
                this.CoiCheckerResult.ResultAt,
                this.CoiCheckerResult.Remark);
        }

        if (this.WatchlistCheckerResult is not null)
        {
            vendor.AddChecker(
                QualificationType.Watchlist,
                this.WatchlistCheckerResult.Result,
                this.WatchlistCheckerResult.ResultAt,
                this.WatchlistCheckerResult.Remark);
        }

        if (this.Status == ContractDraftVendorStatus.Approved && this.ContractSignedDate is not null)
        {
            _ = vendor.SetContractSignedDate(this.ContractSignedDate.Value);
        }

        if (this.Status == ContractDraftVendorStatus.Pending)
        {
            _ = vendor.SetWaitingForApproval();
        }
        else if (this.Status == ContractDraftVendorStatus.Edit)
        {
            _ = vendor.SetEdit();
        }
        else
        {
            vendor.AddActivity(
                new ActivityInfo(
                    ActivityLogActionTypeConstant.Update,
                    "อัพเดตข้อมูล",
                    this.Status.ToString()));
        }

        this.Detail.Vendor.IfNotNull(v => vendor.Vendor.SetDateDuration(v.StartDate, v.EndDate));
        this.Detail.Vendor.IfNotNull(v => vendor.Vendor.SetDetailAddress(v.RegistrationPlace, v.VendorRegistrationPlace, v.Address, v.Street, v.Province, v.District, v.SubDistrict));
        this.Detail.Agreement.IfNotNull(a => vendor.SetAgreement(a.MapToEntity()));
        this.Detail.Guarantee.IfNotNull(g => vendor.SetGuarantee(g.MapToEntity()));

        if (this.Template is not ("CMRentalTpl001" or "CMRentalTpl002" or "CMRentalTpl003" or "CMRentalTpl004"))
        {
            this.Detail.Penalty.IfNotNull(p => vendor.SetPenalty(p.MapToEntity()));
            this.Detail.Payment.IfNotNull(p => p.MapToEntity(vendor));
            this.Detail.Redelivery.IfNotNull(r => vendor.SetRedeliveryCorrection(r.MapToEntity()));
            this.Detail.AdvancePayment.IfNotNull(ap => vendor.SetAdvancePayment(ap.MapToEntity()));
            this.Detail.Delivery.IfNotNull(d => vendor.SetDelivery(d.MapToEntity()));
            this.Detail.Warranty.IfNotNull(w => vendor.SetWarranty(w.MapToEntity()));
            this.Detail.Termination.IfNotNull(t => vendor.SetTermination(t.MapToEntity(this.ContractSignedDate, this.PeriodConditionType, vendor, this.StartDate, this.EndDate)));
            this.Detail.CopierLease.IfNotNull(c => vendor.SetCopierLease(c.MapToEntity()));
            this.Detail.ComputerLease.IfNotNull(c => vendor.SetComputerLease(c.MapToEntity()));
            this.Detail.CarLease.IfNotNull(c => vendor.SetCarLease(c.MapToEntity()));

            if (this.Detail.DefectWarrantyTypeCode != null)
            {
                vendor.SetDefectWarrantyTypeCode(this.Detail.DefectWarrantyTypeCode);
            }
        }

        var incomingAttachmentIds = this.Detail.Attachments
                                        .Where(a => a.Id.HasValue)
                                        .Select(a => a.Id.Value)
                                        .ToHashSet();

        var attachmentsToRemove = vendor.Attachments
                                        .Where(a => !incomingAttachmentIds.Contains(a.Id.Value))
                                        .ToList();

        foreach (var attachment in attachmentsToRemove)
        {
            attachment.ClearAllFiles();
        }

        _ = attachmentsToRemove
            .Map(vendor.RemoveAttachment)
            .ToHashSet();

        _ = this.Detail.Attachments
                .Map(a => a.MapToEntity())
                .Map(vendor.SetAttachments)
                .ToHashSet();

        if (this.Status == ContractDraftVendorStatus.Approved && this.ContractSignedDate is not null)
        {
            var lastDeliveryDate = vendor.PaymentTerms
                .OrderBy(t => t.Sequence)
                .LastOrDefault()
                ?.DeliveryDate;

            if (lastDeliveryDate is not null && lastDeliveryDate != default)
            {
                _ = vendor.SetContractEndDate(lastDeliveryDate.Value);
            }
        }

        this.Detail.RetentionPayment.IfNotNull(r => vendor.SetRetentionPayment(r.MapToEntity()));

        if (this.Acceptors is not null && users is not null)
        {
            var incomingAcceptorIds = this.Acceptors
                .Where(a => a.Id.HasValue)
                .Select(a => a.Id!.Value)
                .ToHashSet();

            var acceptorsToDelete = vendor.Acceptors
                .Where(a => !a.IsDeleted
                         && a.Type != AcceptorType.AcceptorSign
                         && !incomingAcceptorIds.Contains(a.Id.Value))
                .ToList();

            foreach (var acceptor in acceptorsToDelete)
            {
                acceptor.Delete();
            }

            var existingAcceptors = this.Acceptors.Where(a => a.Id.HasValue).ToList();

            foreach (var acceptor in existingAcceptors)
            {
                var existing = vendor.Acceptors.FirstOrDefault(x => x.Id == (Guid)acceptor.Id);
                var user = users.FirstOrDefault(u => u.Id.Value == acceptor.UserId);

                if (existing is not null && user is not null)
                {
                    existing.Update(user);
                    existing.SetSendToAcceptorId(sendToAcceptorId);
                }
            }

            var newAcceptors = this.Acceptors
                                   .Where(a => !a.Id.HasValue)
                                   .Join(
                                       users,
                                       a => a.UserId,
                                       u => u.Id.Value,
                                       (a, u) => CaContractDraftAcceptor.Create(u, a.AcceptorType, a.Sequence))
                                   .ToList();

            foreach (var acceptor in newAcceptors)
            {
                acceptor.SetSendToAcceptorId(sendToAcceptorId);
                vendor.AddAcceptor(acceptor);
            }
        }

        return vendor;
    }
}

public record CreatorResponse(
    string? Action,
    string? FullName,
    string? FullPositionName);

public class ContractDraftDetail : ContractDraftDetailBase;

public abstract class ContractDraftDetailBase
{
    public ParameterCode? DefectWarrantyTypeCode { get; init; }

    /// <summary>
    /// ข้อมูลผู้ซื้อ/ผู้รับจ้าง/ผู้ให้เช่า
    /// </summary>
    public BuyerInfo Buyer { get; init; }

    /// <summary>
    /// ข้อมูลผู้ขาย/ผู้รับจ้าง/ผู้ให้เช่า
    /// </summary>
    public VendorInfo? Vendor { get; init; }

    /// <summary>
    /// ข้อมูลข้อตกลงในสัญญา
    /// </summary>
    public AgreementBase? Agreement { get; init; }

    /// <summary>
    /// ข้อมูลเงื่อนไขการชำระเงิน
    /// </summary>
    public PaymentBase? Payment { get; init; }

    /// <summary>
    /// ข้อมูลการหลักประกัน
    /// </summary>
    public GuaranteeInfo? Guarantee { get; init; }

    /// <summary>
    /// ข้อมูลเกี่ยวกับเบี้ยปรับ/ค่าปรับ
    /// </summary>
    public PenaltyInfo? Penalty { get; init; }

    /// <summary>
    /// การตรวจรับ
    /// </summary>
    public RedeliveryBase? Redelivery { get; init; }

    /// <summary>
    /// ข้อมูลการชำระเงินล่วงหน้า
    /// </summary>
    public AdvancePayment? AdvancePayment { get; init; }

    /// <summary>
    /// ข้อมูลการส่งมอบ
    /// </summary>
    public DeliveryInfo? Delivery { get; init; }

    /// <summary>
    /// ข้อมูลการรับประกัน
    /// </summary>
    public WarrantyInfo? Warranty { get; init; }

    /// <summary>
    /// ข้อมูลการยกเลิกสัญญาหรือการสิ้นสุดสัญญา
    /// </summary>
    public TerminationInfo? Termination { get; init; }

    /// <summary>
    /// ข้อมูลเช่าบริการเครื่องถ่ายเอกสาร
    /// </summary>
    public CopierLeaseInfo? CopierLease { get; init; }

    /// <summary>
    /// ข้อมูลเช่าบริการคอมพิวเตอร์
    /// </summary>
    public ComputerLeaseInfo? ComputerLease { get; init; }

    /// <summary>
    /// ข้อมูลเช่าบริการรถยนต์
    /// ข้อมูลเช่าบริการรถยนต์
    /// </summary>
    public CarLeaseInfo? CarLease { get; init; }

    /// <summary>
    /// เอกสารอันเป็นส่วนหนึ่งของสัญญา
    /// </summary>
    public Attachment[] Attachments { get; init; }

    /// <summary>
    /// การหักเงินประกันผลงาน
    /// </summary>
    public RetentionPayment? RetentionPayment { get; init; }
}

public sealed record BuyerInfo(
    string? Name,
    string? Address,
    LocationInfo Province,
    LocationInfo District,
    LocationInfo SubDistrict)
{
    public Buyer MapToEntity()
    {
        return new Buyer(
            this.Name,
            this.Address,
            this.Province,
            this.District,
            this.SubDistrict);
    }

    public static BuyerInfo FromEntity(Buyer entity)
    {
        if (entity.Name == null &&
            entity.Address == null &&
            entity.Province == null &&
            entity.District == null &&
            entity.SubDistrict == null)
        {
            return new BuyerInfo(
                "ธนาคารอาคารสงเคราะห์",
                "ธนาคารอาคารสงเคราะห์ สำนักงานใหญ่ ตั้งอยู่เลขที่ 63 ถนนพระราม 9",
                new LocationInfo("1", "กรุงเทพมหานคร"),
                new LocationInfo("1017", "ห้วยขวาง"),
                new LocationInfo("101701", "ห้วยขวาง"));
        }

        return new BuyerInfo(
            entity.Name ?? "ธนาคารอาคารสงเคราะห์",
            entity.Address ?? "ธนาคารอาคารสงเคราะห์ สำนักงานใหญ่ ตั้งอยู่เลขที่ 63 ถนนพระราม 9",
            entity.Province ?? LocationInfo.Default,
            entity.District ?? LocationInfo.Default,
            entity.SubDistrict ?? LocationInfo.Default);
    }
}

public record VendorInfo(
    DateTimeOffset? StartDate,
    DateTimeOffset? EndDate,
    string? RegistrationPlace,
    string? VendorRegistrationPlace,
    string? Address,
    string? Street,
    string? Province,
    string? District,
    string? SubDistrict,
    string? PostalCode);

public sealed record DeliveryInfo(
    string Address,
    DateTimeOffset? Date,
    int? LeadTime,
    string? PeriodTypeCode,
    string? LeadTimeTypeCode,
    int? LeadOtherTime,
    string? LeadOtherTimeTypeCode,
    string? CountingConditionCode)
{
    public Delivery MapToEntity()
    {
        return new Delivery()
               .SetAddress(this.Address)
               .SetDate(this.Date)
               .SetLeadTime(this.LeadTime, this.LeadTimeTypeCode, this.PeriodTypeCode)
               .SetLeadOtherTime(this.LeadOtherTime, this.LeadOtherTimeTypeCode)
               .SetCountingCondition(this.CountingConditionCode);
    }

    public static DeliveryInfo? FromEntity(Delivery entity, PpTorDraft? tor)
    {
        if (entity.Address == null &&
            entity.Date == null &&
            entity.LeadTime == null &&
            entity.LeadOtherTime == null &&
            entity.LeadTimeTypeCode == null &&
            entity.LeadOtherTime == null &&
            entity.LeadOtherTimeTypeCode == null &&
            entity.CountingConditionCode == null)
        {
            var periodCondition =
                tor?.PpTorDraftTechnicalPeriods?
                    .FirstOrDefault()?
                    .PeriodConditionCode?
                    .Value;

            var periodConditionCode =
                periodCondition == "DelvCUnit004"
                    ? null
                    : periodCondition?.Replace("DelvCUnit", "CSDPCond");

            return new DeliveryInfo(
                string.Empty,
                null,
                tor?.PpTorDraftTechnicalPeriods?.FirstOrDefault()?.Period,
                tor?.PpTorDraftTechnicalPeriods?.FirstOrDefault()?.PeriodTypeCode?.Value,
                periodConditionCode,
                null,
                null,
                null);
        }

        return new DeliveryInfo(
            entity.Address ?? string.Empty,
            entity.Date,
            entity.LeadTime,
            entity.PeriodTypeCode?.Value ?? string.Empty,
            entity.LeadTimeTypeCode?.Value,
            entity.LeadOtherTime,
            entity.LeadOtherTimeTypeCode?.Value,
            entity.CountingConditionCode?.Value);
    }
}

public sealed record WarrantyInfo(
    bool HasWarranty,
    string? WarrantyConditionCode,
    RentalDurationInfo? WarrantyPeriod,
    RentalDurationInfo? FixingDeadlinePeriod,
    int? WarrantyMonthlyAllowedDowntimeHours,
    decimal? WarrantyDowntimePercentPerMonth,
    decimal? WarrantyPenaltyPerHour,
    int? DowntimeResolutionHours,
    int? DowntimeResolutionDay,
    int? RepairCompletionHours,
    int? RepairCompletionDay,
    decimal? RepairDelayPenaltyPercentPerHour,
    int? MaxMonthlyMalfunction,
    ParameterCode? MaxMonthlyMalfunctionTypeCode,
    decimal? MaxMonthlyMalfunctionRate,
    decimal? MaxMonthlyMalfunctionPenaltyPercentageRate,
    decimal? MaxMonthlyMalfunctionPenaltyPerHour,
    int? MaxMonthlyMalfunctionPenaltyDueDays,
    DateTimeOffset? WarrantyStartDate,
    DateTimeOffset? WarrantyEndDate,
    int? WarrantyMaintenanceCount,
    ParameterCode? WarrantyMaintenanceTypeCode)
{
    public Warranty MapToEntity()
    {
        return new Warranty(
            this.HasWarranty,
            string.IsNullOrWhiteSpace(this.WarrantyConditionCode) ? null : ParameterCode.From(this.WarrantyConditionCode),
            this.WarrantyPeriod,
            this.FixingDeadlinePeriod,
            this.WarrantyMonthlyAllowedDowntimeHours,
            this.WarrantyDowntimePercentPerMonth,
            this.WarrantyPenaltyPerHour,
            this.DowntimeResolutionHours,
            this.DowntimeResolutionDay,
            this.RepairCompletionHours,
            this.RepairCompletionDay,
            this.RepairDelayPenaltyPercentPerHour,
            this.MaxMonthlyMalfunction,
            this.MaxMonthlyMalfunctionTypeCode,
            this.MaxMonthlyMalfunctionRate,
            this.MaxMonthlyMalfunctionPenaltyPercentageRate,
            this.MaxMonthlyMalfunctionPenaltyPerHour,
            this.MaxMonthlyMalfunctionPenaltyDueDays,
            this.WarrantyStartDate,
            this.WarrantyEndDate,
            this.WarrantyMaintenanceCount,
            this.WarrantyMaintenanceTypeCode);
    }

    public static WarrantyInfo? FromEntity(Warranty entity, PpTorDraft? tor)
    {
        if (entity.HasWarranty == null &&
            entity.WarrantyConditionCode == null &&
            entity.WarrantyPeriod == null &&
            entity.FixingDeadlinePeriod == null)
        {
            var cm = tor?.PpTorTemplateComputer?.CorrectiveMaintenance;
            var pm = tor?.PpTorTemplateComputer?.PreventiveMaintenance;

            int? downtimeResolutionDay = null;
            int? downtimeResolutionHours = null;
            int? repairCompletionDay = null;
            int? repairCompletionHours = null;
            decimal? repairDelayPenaltyPercentPerHour = null;

            if (cm != null)
            {
                if (cm.CmUnit == ParameterCode.From("PeriodType001"))
                {
                    downtimeResolutionDay = cm.CmCount;
                }
                else if (cm.CmUnit == ParameterCode.From("PeriodType005"))
                {
                    downtimeResolutionHours = cm.CmCount;
                }

                if (cm.CmCompleteUnit == ParameterCode.From("PeriodType001"))
                {
                    repairCompletionDay = cm.CmCompleteCount;
                }
                else if (cm.CmCompleteUnit == ParameterCode.From("PeriodType005"))
                {
                    repairCompletionHours = cm.CmCompleteCount;
                }

                repairDelayPenaltyPercentPerHour = cm.CmFinePercent;
            }

            int? warrantyMonthlyAllowedDowntimeHours = null;
            decimal? warrantyDowntimePercentPerMonth = null;
            decimal? warrantyPenaltyPerHour = null;

            if (pm != null)
            {
                if (pm.DisruptedCountUnit == ParameterCode.From("PTimeType001"))
                {
                    warrantyMonthlyAllowedDowntimeHours = pm.DisruptedCount;
                }
                else if (pm.DisruptedCountUnit == ParameterCode.From("PTimeType002"))
                {
                    warrantyMonthlyAllowedDowntimeHours = pm.DisruptedCount / 60;
                }

                warrantyDowntimePercentPerMonth = pm.DisruptedPercent;
                warrantyPenaltyPerHour = pm.DisruptedFineAmount;
            }

            return new WarrantyInfo(
                tor?.PpTorDraftWarranties.FirstOrDefault()?.HasWarranty ?? false,
                null,
                new RentalDurationInfo(
                    tor?.PpTorDraftWarranties.FirstOrDefault()?.PeriodTypeCode == ParameterCode.From(PeriodTypeConstant.PeriodType003) ? tor?.PpTorDraftWarranties?.FirstOrDefault()?.Period ?? 0 : 0,
                    tor?.PpTorDraftWarranties?.FirstOrDefault()?.PeriodTypeCode == ParameterCode.From(PeriodTypeConstant.PeriodType002) ? tor?.PpTorDraftWarranties?.FirstOrDefault()?.Period ?? 0 : 0,
                    tor?.PpTorDraftWarranties?.FirstOrDefault()?.PeriodTypeCode == ParameterCode.From(PeriodTypeConstant.PeriodType001) ? tor?.PpTorDraftWarranties?.FirstOrDefault()?.Period ?? 0 : 0),
                null,
                warrantyMonthlyAllowedDowntimeHours,
                warrantyDowntimePercentPerMonth,
                warrantyPenaltyPerHour,
                downtimeResolutionHours,
                downtimeResolutionDay,
                repairCompletionHours,
                repairCompletionDay,
                repairDelayPenaltyPercentPerHour,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null);
        }

        return new WarrantyInfo(
            entity.HasWarranty ?? false,
            entity.WarrantyConditionCode?.Value,
            entity.WarrantyPeriod,
            entity.FixingDeadlinePeriod,
            entity.WarrantyMonthlyAllowedDowntimeHours,
            entity.WarrantyDowntimePercentPerMonth,
            entity.WarrantyPenaltyPerHour,
            entity.DowntimeResolutionHours,
            entity.DowntimeResolutionDay,
            entity.RepairCompletionHours,
            entity.RepairCompletionDay,
            entity.RepairDelayPenaltyPercentPerHour,
            entity.MaxMonthlyMalfunction,
            entity.MaxMonthlyMalfunctionTypeCode,
            entity.MaxMonthlyMalfunctionRate,
            entity.MaxMonthlyMalfunctionPenaltyPercentageRate,
            entity.MaxMonthlyMalfunctionPenaltyPerHour,
            entity.MaxMonthlyMalfunctionPenaltyDueDays,
            entity.WarrantyStartDate,
            entity.WarrantyEndDate,
            entity.WarrantyMaintenanceCount,
            entity.WarrantyMaintenanceTypeCode);
    }
}

public sealed record PenaltyInfo(
    bool IsPenalty,
    string TypeCode,
    decimal Rate,
    decimal Amount,
    string? AmountFormat,
    string? AmountText,
    string RateTypeCode)
{
    public Penalty MapToEntity()
    {
        if (!this.IsPenalty)
        {
            return new Penalty(false);
        }

        return new Penalty(
            this.IsPenalty,
            !string.IsNullOrWhiteSpace(this.TypeCode) ? ParameterCode.From(this.TypeCode) : null,
            this.Rate,
            this.Amount,
            !string.IsNullOrWhiteSpace(this.RateTypeCode) ? ParameterCode.From(this.RateTypeCode) : null);
    }

    public static PenaltyInfo? FromEntity(Penalty entity, decimal budget, PpTorDraft? tor)
    {
        var rate = tor?.PpTorDraftFineRates?.FirstOrDefault();
        var amount = rate != null && rate.Rate > 0 ? (budget / 100) * rate?.Rate : 0;

        if (entity.TypeCode == null &&
            entity.Rate == null &&
            entity.Amount == null &&
            entity.RateTypeCode == null)
        {
            return new PenaltyInfo(
                entity.IsPenalty,
                rate?.ConditionCode?.Value ?? string.Empty,
                rate?.Rate ?? 0,
                amount ?? 0,
                amount.ToCurrencyStringWithComma() ?? string.Empty,
                amount.ThaiBahtText() ?? string.Empty,
                rate?.PeriodTypeCode?.Value ?? string.Empty);
        }

        return new PenaltyInfo(
            entity.IsPenalty,
            entity.TypeCode?.Value ?? string.Empty,
            entity.Rate ?? 0,
            entity.Amount ?? 0,
            entity.Amount?.ToCurrencyStringWithComma() ?? string.Empty,
            entity.Amount?.ThaiBahtText() ?? string.Empty,
            entity.RateTypeCode?.Value ?? string.Empty);
    }
}

public sealed record GuaranteeInfo(
    bool HasGuarantee,
    string? TypeCode,
    decimal Amount,
    string? AmountFormat,
    string? AmountText,
    decimal Percentage,
    string? ReferenceNumber,
    ParameterCode? BankCode,
    string? BankBranch,
    string? BankAccountNumber,
    DateTimeOffset? BankCollateralStartDate,
    DateTimeOffset? BankCollateralEndDate,
    DateTimeOffset? GuaranteeDate,
    string? OtherDetails)
{
    public Guarantee MapToEntity()
    {
        return new Guarantee(
            this.HasGuarantee,
            this.TypeCode is null ? null : ParameterCode.From(this.TypeCode),
            this.Amount,
            this.Percentage,
            this.ReferenceNumber,
            this.BankCode,
            this.BankBranch,
            this.BankAccountNumber,
            this.BankCollateralStartDate,
            this.BankCollateralEndDate,
            this.GuaranteeDate,
            this.OtherDetails);
    }

    public static GuaranteeInfo? FromEntity(Guarantee entity, CaContractInvitationVendors? contractInvitation, PpTorDraft? tor)
    {
        if (entity.IsSubmitted == null &&
            entity.TypeCode == null &&
            entity.Amount == null &&
            entity.Percentage == null)
        {
            return new GuaranteeInfo(
                tor?.IsContractGuarantee ?? false,
                null,
                contractInvitation?.GuaranteeAmount ?? 0,
                (contractInvitation?.GuaranteeAmount ?? 0).ToCurrencyStringWithComma() ?? string.Empty,
                (contractInvitation?.GuaranteeAmount ?? 0).ThaiBahtText() ?? string.Empty,
                contractInvitation?.ContractGuaranteePercent ?? 0,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                string.Empty);
        }

        return new GuaranteeInfo(
            entity.IsSubmitted ?? false,
            entity.TypeCode?.Value,
            entity.Amount ?? 0,
            (entity.Amount ?? 0).ToCurrencyStringWithComma() ?? string.Empty,
            (entity.Amount ?? 0).ThaiBahtText() ?? string.Empty,
            entity.Percentage ?? 0,
            entity.ReferenceNumber,
            entity.BankCode,
            entity.BankBranch,
            entity.BankAccountNumber,
            entity.BankCollateralStartDate,
            entity.BankCollateralEndDate,
            entity.GuaranteeDate,
            entity.OtherDetails);
    }
}

public sealed record AdvancePayment(
    bool HasAdvancePayment,
    decimal Amount,
    string? AmountFormat,
    string? AmountText,
    decimal Percentage,
    int? DueDate,
    string? ConditionCode)
{
    public GHB.DP2.Domain.ContractAgreement.CaContractDraft.SectionType.AdvancePayment MapToEntity()
    {
        return new GHB.DP2.Domain.ContractAgreement.CaContractDraft.SectionType.AdvancePayment(
            this.HasAdvancePayment,
            this.Amount,
            this.Percentage,
            this.DueDate,
            string.IsNullOrWhiteSpace(this.ConditionCode) ? null : ParameterCode.From(this.ConditionCode));
    }

    public static AdvancePayment? FromEntity(GHB.DP2.Domain.ContractAgreement.CaContractDraft.SectionType.AdvancePayment entity)
    {
        if (entity.IsIncluded == null &&
            entity.Amount == null &&
            entity.Percentage == null &&
            entity.DueDate == null &&
            entity.ConditionCode == null)
        {
            return null;
        }

        return new AdvancePayment(
            entity.IsIncluded ?? false,
            entity.Amount ?? 0,
            (entity.Amount ?? 0).ToCurrencyStringWithComma() ?? string.Empty,
            (entity.Amount ?? 0).ThaiBahtText() ?? string.Empty,
            entity.Percentage ?? 0,
            entity.DueDate ?? 0,
            entity.ConditionCode?.Value);
    }
}

public sealed record RetentionPayment(
    bool HasRetentionPayment,
    decimal Amount,
    string? AmountFormat,
    string? AmountText,
    decimal Percentage)
{
    public GHB.DP2.Domain.ContractAgreement.CaContractDraft.SectionType.RetentionPayment MapToEntity()
    {
        return new GHB.DP2.Domain.ContractAgreement.CaContractDraft.SectionType.RetentionPayment(
            this.HasRetentionPayment,
            this.Amount,
            this.Percentage);
    }

    public static RetentionPayment? FromEntity(GHB.DP2.Domain.ContractAgreement.CaContractDraft.SectionType.RetentionPayment entity)
    {
        if (entity.IsIncluded == null &&
            entity.Amount == null &&
            entity.Percentage == null)
        {
            return null;
        }

        return new RetentionPayment(
            entity.IsIncluded ?? false,
            entity.Amount ?? 0,
            (entity.Amount ?? 0).ToCurrencyStringWithComma() ?? string.Empty,
            (entity.Amount ?? 0).ThaiBahtText() ?? string.Empty,
            entity.Percentage ?? 0);
    }
}

public sealed record TerminationInfo(
    DateTimeOffset? StartDate,
    DateTimeOffset? EndDate,
    RentalDurationInfo? VendorProcessingTime)
{
    public Termination MapToEntity(
        DateTimeOffset? contractSignedDate,
        string? periodConditionType,
        CaContractDraftVendor vendor,
        DateTimeOffset? startDate,
        DateTimeOffset? endDate)
    {
        var contractStartDate = startDate;
        var contractEndDate = endDate;

        if (vendor.PaymentTerms.Any())
        {
            var leadTime = vendor.PaymentTerms.Sum(x => x.LeadTime ?? 0);

            if (contractSignedDate != null && periodConditionType == CSDPCond.CSDPCond001)
            {
                contractStartDate = contractSignedDate.Value;
                contractEndDate = contractStartDate.Value.AddDays(leadTime - 1);
            }
            else if (contractSignedDate != null && periodConditionType == CSDPCond.CSDPCond002)
            {
                contractStartDate = contractSignedDate.Value.AddDays(1);
                contractEndDate = contractStartDate.Value.AddDays(leadTime - 1);
            }
        }

        return new Termination(
            this.StartDate ?? contractStartDate,
            this.EndDate ?? contractEndDate,
            this.VendorProcessingTime);
    }

    public static TerminationInfo? FromEntity(Termination entity)
    {
        if (entity.StartDate == null &&
            entity.EndDate == null &&
            entity.VendorProcessingTime == null)
        {
            return null;
        }

        return new TerminationInfo(
            entity.StartDate,
            entity.EndDate,
            entity.VendorProcessingTime);
    }
}

public sealed record CopierLeaseInfo(
    int? MonthlyRentPerMachine,
    int? NumberOfMachines,
    int? TotalMonthlyRent,
    int? EstimatedMonthlyCopies,
    int? BelowEstimateCondition,
    decimal? PerCopyRateCondition)
{
    public LeaseCopier MapToEntity()
    {
        return new LeaseCopier(
            this.MonthlyRentPerMachine,
            this.NumberOfMachines,
            this.TotalMonthlyRent,
            this.EstimatedMonthlyCopies,
            this.BelowEstimateCondition,
            this.PerCopyRateCondition);
    }

    public static CopierLeaseInfo? FromEntity(LeaseCopier entity)
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

        return new CopierLeaseInfo(
            entity.NumberOfMachines,
            entity.RentalQuantity,
            entity.MonthlyRentalRate,
            entity.EstimatedMonthlyCopyVolume,
            entity.ActualMonthlyCopyVolume,
            entity.CopyRatePerPage);
    }
}

public sealed record ComputerLeaseInfo(
    RentalDurationInfo Duration,
    string RentalStartCondition)
{
    public LeaseDuration MapToEntity()
    {
        return new LeaseDuration(
            this.Duration,
            !string.IsNullOrWhiteSpace(this.RentalStartCondition) ? ParameterCode.From(this.RentalStartCondition) : null);
    }

    public static ComputerLeaseInfo? FromEntity(LeaseDuration entity)
    {
        if (entity.Duration == null && entity.CountFromConditionCode == null)
        {
            return null;
        }

        return new ComputerLeaseInfo(
            entity.Duration ?? RentalDurationInfo.Default,
            entity.CountFromConditionCode?.Value ?? string.Empty);
    }
}

public sealed record CarLeaseInfo(
    decimal RentPerVehicle,
    string UnitCode)
{
    public GHB.DP2.Domain.ContractAgreement.CaContractDraft.SectionType.LeaseCar MapToEntity()
    {
        return new GHB.DP2.Domain.ContractAgreement.CaContractDraft.SectionType.LeaseCar(
            this.RentPerVehicle,
            !string.IsNullOrWhiteSpace(this.UnitCode) ? ParameterCode.From(this.UnitCode) : null);
    }

    public static CarLeaseInfo? FromEntity(GHB.DP2.Domain.ContractAgreement.CaContractDraft.SectionType.LeaseCar entity)
    {
        if (entity.RentPerVehicle == null &&
            entity.UnitCode == null)
        {
            return null;
        }

        return new CarLeaseInfo(
            entity.RentPerVehicle ?? 0,
            entity.UnitCode?.Value ?? string.Empty);
    }
}