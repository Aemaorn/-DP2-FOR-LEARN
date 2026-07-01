namespace GHB.DP2.Domain.ContractAgreement.CaContractDraft;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft.SectionType;
using GHB.DP2.Domain.ContractAgreement.CaContractInvitation;
using GHB.DP2.Domain.ContractAmendment.CamCertificateRequisition;
using GHB.DP2.Domain.ContractManagement.CmContractGuaranteeReturn;
using GHB.DP2.Domain.ContractManagement.CmContractTermination;
using GHB.DP2.Domain.ContractManagement.CmDeliveryAcceptance;
using GHB.DP2.Domain.Procurement.PPurchaseOrder;
using GHB.DP2.Domain.Report.RpAuditAndRevenue;
using GHB.DP2.Domain.SystemUtility;
using LanguageExt;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct ContractDraftVendorId
{
    public static ContractDraftVendorId New => From(Guid.CreateVersion7());
}

/// <summary>
/// Represents information about a location.
/// </summary>
/// <param name="Code">The unique code identifying the location.</param>
/// <param name="Name">The name of the location.</param>
public record LocationInfo(
    string Code,
    string Name)
{
    public static LocationInfo Default => new(string.Empty, string.Empty);
}

/// <summary>
/// Represents the duration of a rental contract expressed in the past years, months, and days.
/// This is used to specify the total duration of equipment rental agreements.
/// </summary>
/// <param name="Year">The number of complete years in the rental duration.</param>
/// <param name="Month">The number of additional months in the rental duration.</param>
/// <param name="Day">The number of additional days in the rental duration.</param>
public record RentalDurationInfo(
    int? Year,
    int? Month,
    int? Day)
{
    public static RentalDurationInfo Default => new(0, 0, 0);
}

public enum ContractDraftVendorStatus
{
    /// <summary>
    /// แบบร่าง
    /// </summary>
    Draft,

    /// <summary>
    /// รออนุมัติ
    /// </summary>
    Pending,

    Edit,

    /// <summary>
    /// อนุมัติแล้ว
    /// </summary>
    Approved,

    /// <summary>
    /// ปฏิเสธ
    /// </summary>
    Rejected,
}

public enum ContractStatus
{
    /// <summary>
    /// ร่างสัญญา
    /// </summary>
    Draft,

    /// <summary>
    /// บริหารสัญญา
    /// </summary>
    Management,

    /// <summary>
    /// อยู่ในระยะเวลารับประกันสัญญา
    /// </summary>
    Warranty,

    /// <summary>
    /// สิ้นสุดภาระผูกพัน
    /// </summary>
    Completed,

    /// <summary>
    /// ยกเลิกสัญญา
    /// </summary>
    Cancel,
}

public static class CSDPCond
{
    /// <summary>
    /// นับตั้งแต่วันที่ลงนามในสัญญา
    /// </summary>
    public const string CSDPCond001 = "CSDPCond001";

    /// <summary>
    /// นับถัดจากวันลงนามในสัญญา
    /// </summary>
    public const string CSDPCond002 = "CSDPCond002";

    /// <summary>
    /// ตามระยะเวลาที่ระบุ
    /// </summary>
    public const string CSDPCond003 = "CSDPCond003";
}

public static class CSDPPeriodType
{
    /// <summary>
    /// วัน
    /// </summary>
    public const string Day = "PeriodType001";

    /// <summary>
    /// เดือน
    /// </summary>
    public const string Month = "PeriodType002";

    /// <summary>
    /// ปี
    /// </summary>
    public const string Year = "PeriodType003";
}

[ValueObject<string>(Conversions.EfCoreValueConverter)]
public partial struct ContractDraftNumber
{
    public static ContractDraftNumber New(int? year)
    {
        if (year <= 0 && year != null)
        {
            throw new ArgumentOutOfRangeException(nameof(year), "Year must be greater than 0.");
        }

        // Generate a new plan number in the format "CYY00001"
        // where YY is the last two digits of the year and 00001 is the initial number
        var yearString = (year.Value % 100).ToString("D2"); // Get last two digits of year and ensure it's two digits

        var planNumber = $"{RunningPrefixConstant.Contract}{yearString}00001";

        return From(planNumber);
    }

    public ContractDraftNumber Next()
    {
        if (string.IsNullOrWhiteSpace(this.Value))
        {
            throw new InvalidOperationException("ContractDraftNumber cannot be null or empty.");
        }

        // Assuming the ContractDraftNumber is in the format "CYYXXXXX"
        if (this.Value.Length < 4 || !this.Value.StartsWith(RunningPrefixConstant.Contract))
        {
            throw new FormatException("Invalid ContractDraftNumber format.");
        }

        string yearPart = this.Value.Substring(1, 2);
        string numberPart = this.Value.Substring(4);

        if (!int.TryParse(yearPart, out var year) || !int.TryParse(numberPart, out var number))
        {
            throw new FormatException("Invalid ContractDraftNumber format.");
        }

        // Increment the number part
        number++;

        // Format the new ContractDraftNumber
        var newPlanNumber = $"{RunningPrefixConstant.Contract}{year}{number:D5}";

        return From(newPlanNumber);
    }
}

/// <summary>
/// ร่างสัญญา - ข้อมูลหลักและข้อตกลง
/// </summary>
public partial class CaContractDraftVendor : AuditableEntity<ContractDraftVendorId>, IHasSoftDelete, IHasActivityInfo
{
    public override ContractDraftVendorId Id { get; init; }

    public ContractInvitationVendorsId ContractInvitationVendorsId { get; init; }

    public string Email { get; private set; }

    public string ContractName { get; private set; }

    public string PoNumber { get; private set; }

    public ContractDraftNumber ContractDraftNumber { get; private set; }

    public string ContractNumber { get; private set; }

    public decimal Budget { get; private set; }

    public DateTimeOffset? ContractSignedDate { get; private set; }

    public DateTimeOffset? ContractStartDate { get; private set; }

    public DateTimeOffset? ContractEndDate { get; private set; }

    public ParameterCode? ContractTypeCode { get; private set; }

    public ParameterCode? TemplateCode { get; private set; }

    public string? TemplateText { get; private set; }

    public ParameterCode? SubTemplateCode { get; private set; }

    public string? SubTemplateText { get; private set; }

    public DateTimeOffset? StartDate { get; private set; }

    public DateTimeOffset? EndDate { get; private set; }

    public bool IsWorkingDayOnly { get; private set; }

    public DateTimeOffset? VendorAppointmentMemoDate { get; init; }

    public DateTimeOffset? DocumentDate { get; private set; }

    public ParameterCode? PeriodConditionTypeCode { get; private set; }

    public virtual SuParameter? PeriodConditionType { get; init; }

    public Buyer Buyer { get; private set; }

    public Vendor Vendor { get; private set; }

    public AgreementContract? Agreement { get; private set; }

    public Payment Payment { get; private set; }

    public virtual IReadOnlyCollection<CaContractDraftPaymentTerm> PaymentTerms { get; private set; }

    public Delivery Delivery { get; private set; }

    public Termination Termination { get; private set; }

    public ContractDraftVendorStatus Status { get; private set; }

    public bool IsPending => this.Status == ContractDraftVendorStatus.Pending;

    public bool IsApproved => this.Status == ContractDraftVendorStatus.Approved;

    public virtual CaContractDraft ContractDraft { get; init; }

    public virtual CaContractInvitationVendors ContractInvitationVendors { get; init; }

    public virtual SuParameter? ContractType { get; init; }

    public virtual SuParameter? Template { get; init; }

    public virtual SuParameter? SubTemplate { get; init; }

    public bool? EgpResult { get; private set; }

    public string? EgpRemark { get; private set; }

    public DateTimeOffset? EgpDate { get; private set; }

    public bool? CoiResult { get; private set; }

    public string? CoiRemark { get; private set; }

    public DateTimeOffset? CoiDate { get; private set; }

    public bool? WatchlistResult { get; private set; }

    public string? WatchlistRemark { get; private set; }

    public DateTimeOffset? WatchlistDate { get; private set; }

    public ContractStatus ContractStatus { get; private set; }

    public virtual CaContractDraftEquipmentRental DraftEquipmentRental { get; init; }

    public virtual CaContractDraftTermsConditions DraftTermsConditions { get; init; }

    public virtual IReadOnlyCollection<CaContractDraftVendorsAttachment> Attachments { get; private set; }

    public virtual IReadOnlyCollection<CaContractDraftAcceptor> Acceptors { get; private set; }

    public virtual IReadOnlyCollection<CmDeliveryAcceptance> DeliveryAcceptances { get; init; }

    public virtual IReadOnlyCollection<CmContractTermination> CmContractTerminations { get; init; }

    public virtual IReadOnlyCollection<CmContractGuaranteeReturn> CmContractGuaranteeReturns { get; init; }

    public virtual IReadOnlyCollection<CaContractDraftVendorDocumentHistory> DocumentHistories { get; private set; }

    public virtual IReadOnlyCollection<CamCertificateRequisition> CamCertificateRequisitions { get; init; }

    public virtual IReadOnlyCollection<RpAuditAndRevenueDetail> RpAuditAndRevenueDetails { get; init; }

    public virtual IReadOnlyCollection<CaContractDraftVendorShareholders> Shareholders { get; private set; }

    public virtual IReadOnlyCollection<CaContractDraftVendorChecker> Checkers { get; private set; }

    public virtual IReadOnlyCollection<CaContractDraftVendorCheckerAttachments> CheckerAttachment { get; private set; }

    public CaContractDraftVendorDocumentHistory? LastedDraftContractDraftDocument =>
        this.DocumentHistories
            .Where(x => x.DocumentType == CaContractDraftVendorDocumentType.ContractDraft)
            .Where(x => x.StatusState == ContractDraftVendorStatus.Draft)
            .OrderVersions()
            .FirstOrDefault();

    public CaContractDraftVendorDocumentHistory? LastedDraftApprovedDocument =>
        this.DocumentHistories
            .Where(x => x.DocumentType == CaContractDraftVendorDocumentType.ApprovalContractDraft)
            .Where(x => x.StatusState == ContractDraftVendorStatus.Draft)
            .OrderVersions()
            .FirstOrDefault();

    public CaContractDraftVendorDocumentHistory? LastedDraftConfidentialDocument =>
        this.DocumentHistories
            .Where(x => x.DocumentType == CaContractDraftVendorDocumentType.ConfidentialContractDraft)
            .Where(x => x.StatusState == ContractDraftVendorStatus.Draft)
            .OrderVersions()
            .FirstOrDefault();

    public CaContractDraftVendorDocumentHistory? LastedPendingContractDraftDocument =>
        this.DocumentHistories
            .Where(d => d.DocumentType == CaContractDraftVendorDocumentType.ContractDraft)
            .Where(d => d is { IsReplaced: false, StatusState: ContractDraftVendorStatus.Pending })
            .OrderVersions()
            .FirstOrDefault();

    public CaContractDraftVendorDocumentHistory? LastedPendingApprovedDocument =>
        this.DocumentHistories
            .Where(d => d.DocumentType == CaContractDraftVendorDocumentType.ApprovalContractDraft)
            .Where(d => d is { IsReplaced: false, StatusState: ContractDraftVendorStatus.Pending })
            .OrderVersions()
            .FirstOrDefault();

    public CaContractDraftVendorDocumentHistory? LastedPendingConfidentialDocument =>
        this.DocumentHistories
            .Where(d => d.DocumentType == CaContractDraftVendorDocumentType.ConfidentialContractDraft)
            .Where(d => d is { IsReplaced: false, StatusState: ContractDraftVendorStatus.Pending })
            .OrderVersions()
            .FirstOrDefault();

    public CaContractDraftVendorDocumentHistory? ContractDraftDocument =>
        this.DocumentHistories
            .Where(d => d.DocumentType == CaContractDraftVendorDocumentType.ContractDraft)
            .OrderVersions()
            .FirstOrDefault();

    public CaContractDraftVendorDocumentHistory? ApprovedDocument =>
        this.DocumentHistories
            .Where(d => d.DocumentType == CaContractDraftVendorDocumentType.ApprovalContractDraft)
            .OrderVersions()
            .FirstOrDefault();

    public CaContractDraftVendorDocumentHistory? ConfidentialDocument =>
        this.DocumentHistories
            .Where(d => d.DocumentType == CaContractDraftVendorDocumentType.ConfidentialContractDraft)
            .OrderVersions()
            .FirstOrDefault();

    public CaContractDraftVendorDocumentHistory? LastedDocumentByType(CaContractDraftVendorDocumentType docType) =>
        this.DocumentHistories
            .Where(d => d.DocumentType == docType)
            .OrderVersions()
            .FirstOrDefault();

    public Unit AddDocumentHistory(
        CaContractDraftVendorDocumentType documentType,
        FileId fileId,
        bool? replace,
        bool incrementMajor = false)
    {
        var histories = this.DocumentHistories.ToHashSet();

        var existingStatus =
            histories
                .Where(p => p.DocumentType == documentType)
                .Any(p => p.StatusState == this.Status);

        var version = this.DocumentHistories
                          .Where(p =>
                              p.DocumentType == documentType)
                          .NextVersion(incrementMajor || !existingStatus);

        histories.Add(CaContractDraftVendorDocumentHistory.Create(
            documentType,
            this.Status,
            version,
            fileId,
            replace));

        this.DocumentHistories = histories;

        return unit;
    }

    public CaContractDraftVendor AddAcceptor(CaContractDraftAcceptor acceptor)
    {
        if (acceptor == null)
        {
            throw new ArgumentNullException(nameof(acceptor), "Acceptor cannot be null.");
        }

        var acceptors = this.Acceptors.ToHashSet();

        acceptors.Add(acceptor);

        this.Acceptors = acceptors;

        return this;
    }

    private CaContractDraftVendor AddPaymentTerm(CaContractDraftPaymentTerm paymentTerm)
    {
        var paymentTerms =
            this.PaymentTerms.ToHashSet();

        paymentTerms.Add(paymentTerm);

        this.PaymentTerms = paymentTerms;

        return this;
    }

    private CaContractDraftVendor RemovePaymentTerm(CaContractDraftPaymentTerm paymentTerm)
    {
        var paymentTerms = this.PaymentTerms.ToHashSet();

        if (paymentTerms.Remove(paymentTerm))
        {
            this.PaymentTerms = paymentTerms;
        }

        return this;
    }

    public CaContractDraftVendor RemoveAttachment(CaContractDraftVendorsAttachment attachment)
    {
        if (attachment == null)
        {
            throw new ArgumentNullException(nameof(attachment), "Attachment cannot be null.");
        }

        var attachmentToRemove = this.Attachments.FirstOrDefault(a => a.Id == attachment.Id);

        if (attachmentToRemove != null)
        {
            attachmentToRemove.ClearAllFiles();

            var attachments = this.Attachments.Where(a => a.Id != attachment.Id).ToList();
            this.Attachments = attachments;
        }

        return this;
    }

    public CaContractDraftVendor SetEmail(string email)
    {
        this.Email = email;

        return this;
    }

    public CaContractDraftVendor SetContractName(string contractName)
    {
        if (string.IsNullOrWhiteSpace(contractName))
        {
            throw new ArgumentException("Contract name cannot be null or empty.", nameof(contractName));
        }

        this.ContractName = contractName;

        return this;
    }

    public CaContractDraftVendor SetPoNumber(string poNumber)
    {
        if (string.IsNullOrWhiteSpace(poNumber))
        {
            throw new ArgumentException("PO number cannot be null or empty.", nameof(poNumber));
        }

        this.PoNumber = poNumber;

        return this;
    }

    public CaContractDraftVendor SetContractDraftNumber(ContractDraftNumber contractDraftNumber)
    {
        this.ContractDraftNumber = contractDraftNumber;

        return this;
    }

    public CaContractDraftVendor SetContractNumber(string contractNumber)
    {
        this.ContractNumber = contractNumber;

        return this;
    }

    public CaContractDraftVendor SetBudget(decimal budget)
    {
        if (budget < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(budget), "Budget cannot be negative.");
        }

        this.Budget = budget;

        return this;
    }

    public CaContractDraftVendor SetContractSignedDate(DateTimeOffset contractSignedDate)
    {
        if (contractSignedDate == default)
        {
            throw new ArgumentException("Contract signed date cannot be default value.", nameof(contractSignedDate));
        }

        this.ContractSignedDate = contractSignedDate;
        this.ContractStatus = ContractStatus.Management;

        return this;
    }

    public CaContractDraftVendor SetContractEndDate(DateTimeOffset contractEndDate)
    {
        if (contractEndDate == default)
        {
            throw new ArgumentException("Contract end date cannot be default value.", nameof(contractEndDate));
        }

        this.ContractEndDate = contractEndDate;

        return this;
    }

    public CaContractDraftVendor SetContractType(string contractTypeCode)
    {
        this.ContractTypeCode = !string.IsNullOrWhiteSpace(contractTypeCode) ? ParameterCode.From(contractTypeCode) : null;

        return this;
    }

    public CaContractDraftVendor SetTemplate(string templateCode)
    {
        this.TemplateCode = !string.IsNullOrWhiteSpace(templateCode) ? ParameterCode.From(templateCode) : null;

        return this;
    }

    public CaContractDraftVendor SetTemplateText(string? templateText)
    {
        if (!string.IsNullOrWhiteSpace(templateText))
        {
            this.TemplateText = templateText;
        }

        return this;
    }

    public CaContractDraftVendor SetSubTemplate(string? subTemplateCode)
    {
        this.SubTemplateCode = !string.IsNullOrWhiteSpace(subTemplateCode) ? ParameterCode.From(subTemplateCode) : null;

        return this;
    }

    public CaContractDraftVendor SetSubTemplateText(string? subTemplateText)
    {
        this.SubTemplateText = subTemplateText;

        return this;
    }

    public CaContractDraftVendor SetIsWorkingDayOnly(bool isWorkingDayOnly)
    {
        this.IsWorkingDayOnly = isWorkingDayOnly;

        return this;
    }

    public CaContractDraftVendor SetWaitingForApproval()
    {
        this.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.SendApprove,
                "ส่งผู้มีอำนาจเห็นชอบ/อนุมัติ",
                ContractDraftVendorStatus.Pending.ToString()));

        this.Status = ContractDraftVendorStatus.Pending;

        var approvers = this.Acceptors
                            .Where(p => p.Type == AcceptorType.Approver)
                            .OrderBy(a => a.Sequence)
                            .ToList();

        approvers.Iter(a =>
        {
            if (a.Status == AcceptorStatus.Draft || a.Status == AcceptorStatus.Rejected)
            {
                a.Pending();
            }

            a.SetCurrent(false);
        });

        var firstPending = approvers.FirstOrDefault(a => a.Status == AcceptorStatus.Pending || a.Status == AcceptorStatus.Rejected);

        if (firstPending != null)
        {
            firstPending.SetCurrent(true);
        }

        return this;
    }

    public CaContractDraftVendor SetEdit()
    {
        this.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.Recall,
                "ส่งผู้มีอำนาจเห็นชอบ/อนุมัติ",
                ContractDraftVendorStatus.Edit.ToString()));

        this.Status = ContractDraftVendorStatus.Edit;

        return this;
    }

    public CaContractDraftVendor SetApproved(
        AcceptorId id,
        string? remark = null,
        DelegateeId? delegateId = null)
    {
        if (this.Status != ContractDraftVendorStatus.Pending)
        {
            throw new InvalidOperationException("สถานะร่างสัญญาไม่อยู่ระหว่างรออนุมัติ");
        }

        var target = this.Acceptors
                         .FirstOrDefault(a => a.Id == id && a.IsActive && a.Type == AcceptorType.Approver);

        if (target is null)
        {
            throw new InvalidOperationException("ไม่พบผู้อนุมัติ");
        }

        if (!target.IsCurrent)
        {
            throw new InvalidOperationException("ยังไม่ถึงลำดับการอนุมัติของผู้ใช้งานนี้");
        }

        if (target.Status != AcceptorStatus.Approved)
        {
            target.SetDelegatee(delegateId)
                  .Approve(remark);

            target.SetCurrent(true);
        }

        var approvers = this.Acceptors
                            .Where(a => a.Type == AcceptorType.Approver && a.IsActive)
                            .OrderBy(a => a.Sequence)
                            .ToList();

        var allApproved = approvers.All(a => a.Status == AcceptorStatus.Approved);

        if (allApproved)
        {
            this.Status = ContractDraftVendorStatus.Approved;
            this.AddActivity(
                new ActivityInfo(
                    ActivityLogActionTypeConstant.Approved,
                    "อนุมัติร่างสัญญาครบ",
                    ContractDraftVendorStatus.Approved.ToString(),
                    remark));

            return this;
        }

        // advance to next pending approver
        approvers.ForEach(a =>
        {
            if (a.Status == AcceptorStatus.Pending)
            {
                a.SetCurrent(false);
            }
        });

        var next = approvers.FirstOrDefault(a => a.Status == AcceptorStatus.Pending);
        next?.SetCurrent(true);

        this.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.Approved,
                "อนุมัติร่างสัญญา (บางส่วน)",
                ContractDraftVendorStatus.Pending.ToString(),
                remark));

        return this;
    }

    public CaContractDraftVendor SetRejected(
        string? remark = null)
    {
        this.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.Reject,
                "ส่งกลับแก้ไข",
                ContractDraftVendorStatus.Rejected.ToString(),
                remark));

        this.Status = ContractDraftVendorStatus.Rejected;

        return this;
    }

    public CaContractDraftVendor SetStartDate(DateTimeOffset? startDate)
    {
        this.StartDate = startDate;

        return this;
    }

    public CaContractDraftVendor SetDocumentDate(DateTimeOffset? date = null)
    {
        this.DocumentDate = date ?? DateTimeOffset.Now;

        return this;
    }

    public CaContractDraftVendor SetEndDate(DateTimeOffset? endDate)
    {
        this.EndDate = endDate;

        return this;
    }

    public CaContractDraftVendor SetPeriodConditionType(string? periodConditionType)
    {
        this.PeriodConditionTypeCode = !string.IsNullOrWhiteSpace(periodConditionType) ? ParameterCode.From(periodConditionType) : null;

        return this;
    }

    public CaContractDraftVendor SetAttachments(CaContractDraftVendorsAttachment attachment)
    {
        if (attachment == null)
        {
            throw new ArgumentNullException(nameof(attachment), "Attachment cannot be null.");
        }

        var attachments = this.Attachments.ToHashSet();

        var existingAttachment = attachments.FirstOrDefault(a => a.Id == attachment.Id);

        if (existingAttachment == null)
        {
            attachments.Add(attachment);

            this.Attachments = attachments;

            return this;
        }

        existingAttachment.SetTypeCode(attachment.TypeCode)
                          .SetDescription(attachment.Description)
                          .SetPageNumber(attachment.PageNumber)
                          .SetSequence(attachment.Sequence)
                          .SetFormatOtherName(attachment.FormatOtherName);

        var incomingFileIds = attachment.Files.Select(f => f.Id.Value).ToHashSet();

        var filesToRemove = existingAttachment.Files
                                              .Where(f => !incomingFileIds.Contains(f.Id.Value))
                                              .ToList();

        foreach (var file in filesToRemove)
        {
            existingAttachment.RemoveFile(file);
        }

        var existingFileIds = existingAttachment.Files.Select(f => f.Id.Value).ToHashSet();

        var filesToAdd = attachment.Files
                                   .Where(f => !existingFileIds.Contains(f.Id.Value))
                                   .ToList();

        foreach (var file in filesToAdd)
        {
            existingAttachment.AddFile(file);
        }

        this.Attachments = attachments;

        return this;
    }

    public CaContractDraftVendor SetPaymentTerm(IEnumerable<CaContractDraftPaymentTerm> paymentTerms)
    {
        var paymentTermList = paymentTerms.ToList();

        _ = this.PaymentTerms
                .Join(
                    paymentTermList,
                    existing => existing.Id,
                    newDetail => newDetail.Id,
                    (existing, newDetail) =>
                    {
                        return existing
                               .SetPaymentTermNo(newDetail.PaymentTermNo)
                               .SetLeadTime(newDetail.LeadTime)
                               .SetInstallmentPercentage(newDetail.InstallmentPercentage)
                               .SetAmount(newDetail.Amount)
                               .SetAdvanceDeductionAmount(newDetail.AdvanceDeductionAmount)
                               .SetPerformanceDeductionAmount(newDetail.PerformanceDeductionAmount)
                               .SetDescription(newDetail.Description)
                               .SetSequence(newDetail.Sequence)
                               .SetPeriodType(newDetail.PeriodTypeCode);
                    })
                .ToHashSet();

        _ = this.PaymentTerms
                .Except(paymentTermList)
                .Map(this.RemovePaymentTerm)
                .ToHashSet();

        _ = paymentTermList
            .Except(this.PaymentTerms)
            .Map(this.AddPaymentTerm)
            .ToHashSet();

        this.CalculateAndSetDeliveryDates();

        return this;
    }

    private void CalculateAndSetDeliveryDates()
    {
        if (this.Status != ContractDraftVendorStatus.Approved || this.ContractSignedDate is null)
        {
            return;
        }

        var orderedTerms = this.PaymentTerms.OrderBy(t => t.Sequence).ToList();

        foreach (var term in orderedTerms)
        {
            DateTimeOffset? deliveryDate;

            if (this.PeriodConditionTypeCode == ParameterCode.From(CSDPCond.CSDPCond003))
            {
                deliveryDate = this.EndDate;
            }
            else
            {
                var baseDate = this.ContractSignedDate.Value;

                if (this.PeriodConditionTypeCode == ParameterCode.From(CSDPCond.CSDPCond002))
                {
                    baseDate = baseDate.AddDays(1);
                }

                if (term.PeriodTypeCode == ParameterCode.From(CSDPPeriodType.Month))
                {
                    deliveryDate = baseDate.AddMonths(term.LeadTime ?? 0);
                }
                else if (term.PeriodTypeCode == ParameterCode.From(CSDPPeriodType.Year))
                {
                    deliveryDate = baseDate.AddYears(term.LeadTime ?? 0);
                }
                else
                {
                    deliveryDate = baseDate.AddDays(term.LeadTime ?? 0);
                }
            }

            term.SetDeliveryDate(deliveryDate);
        }
    }

    public CaContractDraftVendor SetBuyer(Buyer buyer)
    {
        this.Buyer = buyer;

        return this;
    }

    public CaContractDraftVendor SetAgreement(AgreementContract agreement)
    {
        this.Agreement = agreement;

        return this;
    }

    public CaContractDraftVendor SetPayment(Payment payment)
    {
        this.Payment = payment;

        return this;
    }

    public CaContractDraftVendor SetGuarantee(Guarantee guarantee)
    {
        if (guarantee == null)
        {
            throw new ArgumentNullException(nameof(guarantee), "Guarantee cannot be null.");
        }

        this.DraftTermsConditions.SetGuarantee(guarantee);

        return this;
    }

    public CaContractDraftVendor SetPenalty(Penalty penalty)
    {
        if (penalty == null)
        {
            throw new ArgumentNullException(nameof(penalty), "Penalty cannot be null.");
        }

        this.DraftTermsConditions.SetPenalty(penalty);

        return this;
    }

    public CaContractDraftVendor SetRedeliveryCorrection(RedeliveryCorrection redeliveryCorrection)
    {
        if (redeliveryCorrection == null)
        {
            throw new ArgumentNullException(nameof(redeliveryCorrection), "Redelivery correction cannot be null.");
        }

        this.DraftTermsConditions.SetRedeliveryCorrection(redeliveryCorrection);

        return this;
    }

    public CaContractDraftVendor SetAdvancePayment(AdvancePayment advancePayment)
    {
        if (advancePayment == null)
        {
            throw new ArgumentNullException(nameof(advancePayment), "Advance payment cannot be null.");
        }

        this.DraftTermsConditions.SetAdvancePayment(advancePayment);

        return this;
    }

    public CaContractDraftVendor SetRetentionPayment(RetentionPayment retentionPayment)
    {
        if (retentionPayment == null)
        {
            throw new ArgumentNullException(nameof(retentionPayment), "Retention payment cannot be null.");
        }

        this.DraftTermsConditions.SetRetentionPayment(retentionPayment);

        return this;
    }

    public CaContractDraftVendor SetDelivery(Delivery delivery)
    {
        this.Delivery = delivery;

        return this;
    }

    public CaContractDraftVendor SetDefectWarrantyTypeCode(ParameterCode? defectWarrantyTypeCode)
    {
        this.DraftTermsConditions.SetDefectWarrantyTypeCode(defectWarrantyTypeCode);

        return this;
    }

    public CaContractDraftVendor SetWarranty(Warranty warranty)
    {
        if (warranty == null)
        {
            throw new ArgumentNullException(nameof(warranty), "Warranty cannot be null.");
        }

        this.DraftTermsConditions.SetWarranty(warranty);

        return this;
    }

    public CaContractDraftVendor SetTermination(Termination termination)
    {
        this.Termination = termination;

        return this;
    }

    public CaContractDraftVendor SetCopierLease(LeaseCopier copierLease)
    {
        this.DraftEquipmentRental.SetCopierLease(copierLease);

        return this;
    }

    public CaContractDraftVendor SetComputerLease(LeaseDuration computerLease)
    {
        this.DraftEquipmentRental.SetLeaseDuration(computerLease);

        return this;
    }

    public CaContractDraftVendor SetCarLease(LeaseCar leaseCar)
    {
        this.DraftEquipmentRental.SetCarLease(leaseCar);

        return this;
    }

    public CaContractDraftVendor SetContractStatus(ContractStatus contractStatus)
    {
        this.ContractStatus = contractStatus;

        return this;
    }

    public static CaContractDraftVendor Create(
        ContractInvitationVendorsId contractInvitationVendorsId,
        SuVendorId vendorId)
    {
        var newData = new CaContractDraftVendor
        {
            Id = ContractDraftVendorId.New,
            ContractInvitationVendorsId = contractInvitationVendorsId,
            Agreement = new AgreementContract(),
            DraftEquipmentRental = CaContractDraftEquipmentRental.Create(),
            DraftTermsConditions = CaContractDraftTermsConditions.Create(),
            Payment = Payment.Default(),
            Vendor = Vendor.Create(vendorId),
            Status = ContractDraftVendorStatus.Draft,
            Acceptors = [],
            DocumentHistories = [],
            PaymentTerms = [],
            Shareholders = [],
            Checkers = [],
        };

        newData.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.Create,
                "สร้างข้อมูล",
                newData.Status.ToString()));

        return newData;
    }

    public static CaContractDraftVendor Create(
        Guid id,
        Guid contractInvitationVendorsId)
    {
        var newData = new CaContractDraftVendor
        {
            Id = ContractDraftVendorId.From(id),
            ContractInvitationVendorsId = ContractInvitationVendorsId.From(contractInvitationVendorsId),
            Agreement = new AgreementContract(),
            DraftEquipmentRental = CaContractDraftEquipmentRental.Create(),
            DraftTermsConditions = CaContractDraftTermsConditions.Create(),
            Payment = Payment.Default(),
            Status = ContractDraftVendorStatus.Draft,
            Acceptors = [],
            Shareholders = [],
            Checkers = [],
        };

        newData.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.Create,
                "สร้างข้อมูล",
                newData.Status.ToString()));

        return newData;
    }

    public CaContractDraftVendor SetWatchlist(bool? result, string? remark, DateTimeOffset? at)
    {
        if (result != null)
        {
            this.WatchlistResult = result;
        }

        this.WatchlistRemark = remark;
        this.WatchlistDate = at;

        return this;
    }

    public CaContractDraftVendor SetCoi(bool? result, string? remark, DateTimeOffset? at)
    {
        if (result != null)
        {
            this.CoiResult = result;
        }

        this.CoiRemark = remark;
        this.CoiDate = at;

        return this;
    }

    public CaContractDraftVendor SetEgp(bool? result, string? remark, DateTimeOffset? at)
    {
        if (result != null)
        {
            this.EgpResult = result;
        }

        this.EgpRemark = remark;
        this.EgpDate = at;

        return this;
    }

    public CaContractDraftVendor AddCaContractDraftVendorShareholderList(List<CaContractDraftVendorShareholders> shareholders)
    {
        this.Shareholders = shareholders;

        return this;
    }

    public CaContractDraftVendor AddCaContractDraftVendorShareholder(CaContractDraftVendorShareholders shareholders)
    {
        var shareholdersList = this.Shareholders?.ToList() ?? new List<CaContractDraftVendorShareholders>();
        shareholdersList.Add(shareholders);

        this.Shareholders = shareholdersList;

        return this;
    }

    public CaContractDraftVendor UpdateCaContractDraftVendorShareholder(CaContractDraftVendorShareholders shareholders)
    {
        var shareholdersList = this.Shareholders?.ToList() ?? new List<CaContractDraftVendorShareholders>();
        var idx = shareholdersList.FindIndex(a => a.Id == shareholders.Id);

        if (idx >= 0)
        {
            shareholdersList[idx] = shareholders;
            this.Shareholders = shareholdersList;
        }

        return this;
    }

    public CaContractDraftVendor RemoveCaContractDraftVendorShareholder(CaContractDraftVendorShareholderId shareholdersId)
    {
        var entrepreneurShareholdersList = this.Shareholders?.ToList() ?? new List<CaContractDraftVendorShareholders>();
        entrepreneurShareholdersList.RemoveAll(a => a.Id == shareholdersId);
        this.Shareholders = entrepreneurShareholdersList;

        return this;
    }

    public CaContractDraftVendor AddChecker(
        QualificationType checkType,
        QualificationResult result,
        DateTimeOffset resultAt,
        string? remark)
    {
        var checkerList = this.Checkers?.ToList() ?? [];

        var checkerExists =
            resultAt == checkerList.Where(x => x.CheckType == checkType).MaxBy(x => x.ResultAt)?.ResultAt;

        if (checkerExists)
        {
            return this;
        }

        var checker =
            (CaContractDraftVendorChecker)CaContractDraftVendorChecker
                .Create(
                    checkType,
                    result,
                    resultAt,
                    remark);

        checkerList.Add(checker);

        this.Checkers = checkerList;

        return this;
    }

    public CaContractDraftVendor RemoveAttachment(CaContractDraftVendorCheckerAttachments attachment)
    {
        var list = this.CheckerAttachment.ToHashSet();
        list.Remove(attachment);
        this.CheckerAttachment = list;

        return this;
    }

    public CaContractDraftVendor RemoveConfidentialDocument()
    {
        var documentHistories = this.DocumentHistories.ToHashSet();

        var confidentialDoc = documentHistories.Where(c => c.DocumentType == CaContractDraftVendorDocumentType.ConfidentialContractDraft);
        confidentialDoc.Iter(r => documentHistories.Remove(r));

        this.DocumentHistories = documentHistories;

        return this;
    }

    public CaContractDraftVendor AddAttachment(CaContractDraftVendorCheckerAttachments attachment)
    {
        if (attachment == null)
        {
            throw new ArgumentNullException(nameof(attachment), "Attachment cannot be null.");
        }

        if (this.CheckerAttachment.Contains(attachment))
        {
            throw new InvalidOperationException("Attachment already exists in the plan.");
        }

        var attachments = this.CheckerAttachment.ToHashSet();

        attachments.Add(attachment);

        this.CheckerAttachment = attachments;

        return this;
    }
}

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct CaContractDraftVendorCheckerAttachmentId
{
    public static CaContractDraftVendorCheckerAttachmentId New() => From(Guid.CreateVersion7());
}

public class CaContractDraftVendorCheckerAttachments : AuditableEntity<CaContractDraftVendorCheckerAttachmentId>
{
    public ParameterCode DocumentTypeCode { get; private set; }

    public override CaContractDraftVendorCheckerAttachmentId Id { get; init; }

    public EntrepreneurAttachmentType Type { get; init; }

    public FileId FileId { get; private init; }

    public string FileName { get; private init; }

    public bool IsPublic { get; private set; }

    public int Sequence { get; private set; }

    public virtual SuParameter DocumentType { get; init; }

    public static CaContractDraftVendorCheckerAttachments Create(
        ParameterCode documentTypeCode,
        FileId fileId,
        string fileName,
        EntrepreneurAttachmentType type,
        int sequence,
        bool isPublic)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name cannot be null or empty.", nameof(fileName));
        }

        return new CaContractDraftVendorCheckerAttachments
        {
            Id = CaContractDraftVendorCheckerAttachmentId.New(),
            Sequence = sequence,
            DocumentTypeCode = documentTypeCode,
            Type = type,
            FileId = fileId,
            FileName = fileName,
            IsPublic = isPublic,
        };
    }

    public CaContractDraftVendorCheckerAttachments SetSequence(int sequence)
    {
        if (sequence < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sequence), "Sequence must be a non-negative integer.");
        }

        this.Sequence = sequence;

        return this;
    }

    public CaContractDraftVendorCheckerAttachments SetIsPublic(bool isPublic)
    {
        this.IsPublic = isPublic;

        return this;
    }

    public Unit SetDocumentType(ParameterCode documentTypeCode)
    {
        this.DocumentTypeCode = documentTypeCode;

        return unit;
    }

    public CaContractDraftVendorCheckerAttachments Clone()
    {
        return new CaContractDraftVendorCheckerAttachments
        {
            Id = CaContractDraftVendorCheckerAttachmentId.New(),
            Sequence = this.Sequence,
            DocumentTypeCode = this.DocumentTypeCode,
            FileId = this.FileId,
            FileName = this.FileName,
            IsPublic = this.IsPublic,
        };
    }
}