namespace GHB.DP2.Domain.ContractManagement.CaContractDraftVendorEdit;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft.SectionType;
using GHB.DP2.Domain.ContractAgreement.CaContractInvitation;
using GHB.DP2.Domain.ContractManagement.ContractManagement;
using GHB.DP2.Domain.SystemUtility;
using LanguageExt;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct ContractDraftVendorEditId
{
    public static ContractDraftVendorEditId New => From(Guid.CreateVersion7());
}

public enum ContractDraftVendorEditStatus
{
    /// <summary>
    /// สร้างเอกสาร
    /// </summary>
    Draft,

    /// <summary>
    /// อยู่ระหว่างแก้ไข
    /// </summary>
    Editing,

    /// <summary>
    /// ถูก reject
    /// </summary>
    Rejected,

    /// <summary>
    /// รอคณะกรรมการอนุมัติ
    /// </summary>
    WaitingCommitteeApproval,

    /// <summary>
    /// รอมอบหมายผู้รับผิดชอบ
    /// </summary>
    WaitingAssignment,

    /// <summary>
    /// รอ comment
    /// </summary>
    WaitingComment,

    /// <summary>
    /// รออนุมัติ
    /// </summary>
    WaitingApproval,

    /// <summary>
    /// อนุมัติแล้ว
    /// </summary>
    Approved,

    /// <summary>
    /// ส่งกลับผู้รับผิดชอบ
    /// </summary>
    RejectedToAssignee,

    /// <summary>
    /// รอมอบหมายผู้จัดทำบันทึกต่อท้าย
    /// </summary>
    WaitingAddendumAssignment,

    /// <summary>
    /// รอร่างเอกสารบันทึกต่อท้าย
    /// </summary>
    WaitingDraftAddendum,

    /// <summary>
    /// รอตรวจสอบ
    /// </summary>
    WaitingReview,

    /// <summary>
    /// ตรวจสอบแล้ว
    /// </summary>
    Reviewed,
}

/// <summary>
/// แก้ไขร่างสัญญา — clone จาก ContractDraftVendor
/// </summary>
public partial class CaContractDraftVendorEdit : AuditableEntity<ContractDraftVendorEditId>, IHasSoftDelete, IHasActivityInfo
{
    public override ContractDraftVendorEditId Id { get; init; }

    // Reference fields
    public ContractDraftVendorId ContractDraftVendorId { get; init; }

    public Guid ProcurementId { get; init; }

    public ContractManagementId? ContractManagementId { get; private set; }

    // Edit workflow status
    public ContractDraftVendorEditStatus Status { get; private set; }

    // Clone fields from ContractDraftVendor
    public ContractInvitationVendorsId ContractInvitationVendorsId { get; init; }

    public string Email { get; private set; }

    public string ContractName { get; private set; }

    public string PoNumber { get; private set; }

    public ContractDraftNumber ContractDraftNumber { get; private set; }

    public string ContractNumber { get; private set; }

    public decimal Budget { get; private set; }

    public DateTimeOffset? ContractSignedDate { get; private set; }

    public DateTimeOffset? ContractEndDate { get; private set; }

    public ParameterCode? ContractTypeCode { get; private set; }

    public ParameterCode? TemplateCode { get; private set; }

    public string? TemplateText { get; private set; }

    public ParameterCode? SubTemplateCode { get; private set; }

    public string? SubTemplateText { get; private set; }

    public DateTimeOffset? StartDate { get; private set; }

    public DateTimeOffset? EndDate { get; private set; }

    public bool IsWorkingDayOnly { get; private set; }

    public DateTimeOffset? VendorAppointmentMemoDate { get; private set; }

    public DateTimeOffset? DocumentDate { get; private set; }

    public ParameterCode? PeriodConditionTypeCode { get; private set; }

    public virtual SuParameter? PeriodConditionType { get; init; }

    // Owned Types
    public Buyer Buyer { get; private set; }

    public Vendor Vendor { get; private set; }

    public AgreementContract? Agreement { get; private set; }

    public Payment Payment { get; private set; }

    public Delivery Delivery { get; private set; }

    public Termination Termination { get; private set; }

    // Qualification check fields
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

    public string? Title { get; private set; }

    public string? Description { get; private set; }

    // Navigation properties — parent
    public virtual CaContractDraftVendor ContractDraftVendor { get; private set; }

    public virtual ContractManagement? ContractManagement { get; init; }

    // Navigation properties — parameters
    public virtual SuParameter? ContractType { get; init; }

    public virtual SuParameter? Template { get; init; }

    public virtual SuParameter? SubTemplate { get; init; }

    // Navigation properties — child tables
    public virtual IReadOnlyCollection<CaContractDraftVendorEditAssignee> Assignees { get; private set; }

    public virtual IReadOnlyCollection<CaContractDraftVendorEditComponent> Components { get; private set; }

    public virtual IReadOnlyCollection<CaContractDraftEditPaymentTerm> PaymentTerms { get; private set; }

    public virtual IReadOnlyCollection<CaContractDraftEditVendorsAttachment> Attachments { get; private set; }

    public virtual IReadOnlyCollection<CaContractDraftEditAcceptor> Acceptors { get; private set; }

    public virtual IReadOnlyCollection<CaContractDraftEditVendorShareholders> Shareholders { get; private set; }

    public virtual IReadOnlyCollection<CaContractDraftEditVendorChecker> Checkers { get; private set; }

    public virtual IReadOnlyCollection<CaContractDraftEditVendorCheckerAttachments> CheckerAttachment { get; private set; }

    public virtual IReadOnlyCollection<CaContractDraftEditVendorDocumentHistory> DocumentHistories { get; private set; }

    public virtual CaContractDraftEditTermsConditions DraftTermsConditions { get; init; }

    public virtual CaContractDraftEditEquipmentRental DraftEquipmentRental { get; init; }

    // Setter methods
    public CaContractDraftVendorEdit SetContractManagementId(ContractManagementId? contractManagementId)
    {
        this.ContractManagementId = contractManagementId;

        return this;
    }

    public CaContractDraftVendorEdit SetEmail(string email)
    {
        this.Email = email;

        return this;
    }

    public CaContractDraftVendorEdit SetTitle(string? title)
    {
        this.Title = title;

        return this;
    }

    public CaContractDraftVendorEdit SetDescription(string? description)
    {
        this.Description = description;

        return this;
    }

    public CaContractDraftVendorEdit SetContractName(string contractName)
    {
        if (string.IsNullOrWhiteSpace(contractName))
        {
            throw new ArgumentException("Contract name cannot be null or empty.", nameof(contractName));
        }

        this.ContractName = contractName;

        return this;
    }

    public CaContractDraftVendorEdit SetPoNumber(string poNumber)
    {
        if (string.IsNullOrWhiteSpace(poNumber))
        {
            throw new ArgumentException("PO number cannot be null or empty.", nameof(poNumber));
        }

        this.PoNumber = poNumber;

        return this;
    }

    public CaContractDraftVendorEdit SetContractDraftNumber(ContractDraftNumber contractDraftNumber)
    {
        this.ContractDraftNumber = contractDraftNumber;

        return this;
    }

    public CaContractDraftVendorEdit SetContractNumber(string contractNumber)
    {
        this.ContractNumber = contractNumber;

        return this;
    }

    public CaContractDraftVendorEdit SetBudget(decimal budget)
    {
        if (budget < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(budget), "Budget cannot be negative.");
        }

        this.Budget = budget;

        return this;
    }

    public CaContractDraftVendorEdit SetContractSignedDate(DateTimeOffset contractSignedDate)
    {
        if (contractSignedDate == default)
        {
            throw new ArgumentException("Contract signed date cannot be default value.", nameof(contractSignedDate));
        }

        this.ContractSignedDate = contractSignedDate;

        return this;
    }

    public CaContractDraftVendorEdit SetContractEndDate(DateTimeOffset contractEndDate)
    {
        if (contractEndDate == default)
        {
            throw new ArgumentException("Contract end date cannot be default value.", nameof(contractEndDate));
        }

        this.ContractEndDate = contractEndDate;

        return this;
    }

    public CaContractDraftVendorEdit SetContractType(string contractTypeCode)
    {
        this.ContractTypeCode = !string.IsNullOrWhiteSpace(contractTypeCode) ? ParameterCode.From(contractTypeCode) : null;

        return this;
    }

    public CaContractDraftVendorEdit SetTemplate(string templateCode)
    {
        this.TemplateCode = !string.IsNullOrWhiteSpace(templateCode) ? ParameterCode.From(templateCode) : null;

        return this;
    }

    public CaContractDraftVendorEdit SetTemplateText(string? templateText)
    {
        if (!string.IsNullOrWhiteSpace(templateText))
        {
            this.TemplateText = templateText;
        }

        return this;
    }

    public CaContractDraftVendorEdit SetSubTemplate(string? subTemplateCode)
    {
        this.SubTemplateCode = !string.IsNullOrWhiteSpace(subTemplateCode) ? ParameterCode.From(subTemplateCode) : null;

        return this;
    }

    public CaContractDraftVendorEdit SetSubTemplateText(string? subTemplateText)
    {
        this.SubTemplateText = subTemplateText;

        return this;
    }

    public CaContractDraftVendorEdit SetIsWorkingDayOnly(bool isWorkingDayOnly)
    {
        this.IsWorkingDayOnly = isWorkingDayOnly;

        return this;
    }

    public CaContractDraftVendorEdit SetStartDate(DateTimeOffset? startDate)
    {
        this.StartDate = startDate;

        return this;
    }

    public CaContractDraftVendorEdit SetEndDate(DateTimeOffset? endDate)
    {
        this.EndDate = endDate;

        return this;
    }

    public CaContractDraftVendorEdit SetVendorAppointmentMemoDate(DateTimeOffset? vendorAppointmentMemoDate)
    {
        this.VendorAppointmentMemoDate = vendorAppointmentMemoDate;

        return this;
    }

    public CaContractDraftVendorEdit SetDocumentDate(DateTimeOffset? date = null)
    {
        this.DocumentDate = date ?? DateTimeOffset.Now;

        return this;
    }

    public CaContractDraftVendorEdit SetPeriodConditionType(string? periodConditionType)
    {
        this.PeriodConditionTypeCode = !string.IsNullOrWhiteSpace(periodConditionType) ? ParameterCode.From(periodConditionType) : null;

        return this;
    }

    public CaContractDraftVendorEdit SetBuyer(Buyer buyer)
    {
        this.Buyer = buyer;

        return this;
    }

    public CaContractDraftVendorEdit SetVendor(Vendor vendor)
    {
        this.Vendor = vendor;

        return this;
    }

    public CaContractDraftVendorEdit SetAgreement(AgreementContract agreement)
    {
        this.Agreement = agreement;

        return this;
    }

    public CaContractDraftVendorEdit SetPayment(Payment payment)
    {
        this.Payment = payment;

        return this;
    }

    public CaContractDraftVendorEdit SetDelivery(Delivery delivery)
    {
        this.Delivery = delivery;

        return this;
    }

    public CaContractDraftVendorEdit SetTermination(Termination termination)
    {
        this.Termination = termination;

        return this;
    }

    public CaContractDraftVendorEdit SetGuarantee(Guarantee guarantee)
    {
        if (guarantee == null)
        {
            throw new ArgumentNullException(nameof(guarantee), "Guarantee cannot be null.");
        }

        this.DraftTermsConditions.SetGuarantee(guarantee);

        return this;
    }

    public CaContractDraftVendorEdit SetPenalty(Penalty penalty)
    {
        if (penalty == null)
        {
            throw new ArgumentNullException(nameof(penalty), "Penalty cannot be null.");
        }

        this.DraftTermsConditions.SetPenalty(penalty);

        return this;
    }

    public CaContractDraftVendorEdit SetRedeliveryCorrection(RedeliveryCorrection redeliveryCorrection)
    {
        if (redeliveryCorrection == null)
        {
            throw new ArgumentNullException(nameof(redeliveryCorrection), "Redelivery correction cannot be null.");
        }

        this.DraftTermsConditions.SetRedeliveryCorrection(redeliveryCorrection);

        return this;
    }

    public CaContractDraftVendorEdit SetAdvancePayment(AdvancePayment advancePayment)
    {
        if (advancePayment == null)
        {
            throw new ArgumentNullException(nameof(advancePayment), "Advance payment cannot be null.");
        }

        this.DraftTermsConditions.SetAdvancePayment(advancePayment);

        return this;
    }

    public CaContractDraftVendorEdit SetRetentionPayment(RetentionPayment retentionPayment)
    {
        if (retentionPayment == null)
        {
            throw new ArgumentNullException(nameof(retentionPayment), "Retention payment cannot be null.");
        }

        this.DraftTermsConditions.SetRetentionPayment(retentionPayment);

        return this;
    }

    public CaContractDraftVendorEdit SetDefectWarrantyTypeCode(ParameterCode? defectWarrantyTypeCode)
    {
        this.DraftTermsConditions.SetDefectWarrantyTypeCode(defectWarrantyTypeCode);

        return this;
    }

    public CaContractDraftVendorEdit SetWarranty(Warranty warranty)
    {
        if (warranty == null)
        {
            throw new ArgumentNullException(nameof(warranty), "Warranty cannot be null.");
        }

        this.DraftTermsConditions.SetWarranty(warranty);

        return this;
    }

    public CaContractDraftVendorEdit SetCopierLease(LeaseCopier copierLease)
    {
        this.DraftEquipmentRental.SetCopierLease(copierLease);

        return this;
    }

    public CaContractDraftVendorEdit SetComputerLease(LeaseDuration computerLease)
    {
        this.DraftEquipmentRental.SetLeaseDuration(computerLease);

        return this;
    }

    public CaContractDraftVendorEdit SetCarLease(LeaseCar leaseCar)
    {
        this.DraftEquipmentRental.SetCarLease(leaseCar);

        return this;
    }

    public CaContractDraftVendorEdit SetContractStatus(ContractStatus contractStatus)
    {
        this.ContractStatus = contractStatus;

        return this;
    }

    public CaContractDraftVendorEdit SetWatchlist(bool? result, string? remark, DateTimeOffset? at)
    {
        if (result != null)
        {
            this.WatchlistResult = result;
        }

        this.WatchlistRemark = remark;
        this.WatchlistDate = at;

        return this;
    }

    public CaContractDraftVendorEdit SetCoi(bool? result, string? remark, DateTimeOffset? at)
    {
        if (result != null)
        {
            this.CoiResult = result;
        }

        this.CoiRemark = remark;
        this.CoiDate = at;

        return this;
    }

    public CaContractDraftVendorEdit SetEgp(bool? result, string? remark, DateTimeOffset? at)
    {
        if (result != null)
        {
            this.EgpResult = result;
        }

        this.EgpRemark = remark;
        this.EgpDate = at;

        return this;
    }

    // Assignee management
    public CaContractDraftVendorEdit AddAssignee(CaContractDraftVendorEditAssignee assignee)
    {
        if (assignee == null)
        {
            throw new ArgumentNullException(nameof(assignee), "Assignee cannot be null.");
        }

        var assignees = this.Assignees.ToList();

        assignees.Add(assignee);

        this.Assignees = assignees;

        return this;
    }

    // Acceptor management
    public CaContractDraftVendorEdit AddAcceptor(CaContractDraftEditAcceptor acceptor)
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

    public CaContractDraftVendorEdit AddComponent(CaContractDraftVendorEditComponent component)
    {
        var components = this.Components.ToHashSet();

        components.Add(component);

        this.Components = components;

        return this;
    }

    public CaContractDraftVendorEdit RemoveComponent(CaContractDraftVendorEditComponent component)
    {
        var components = this.Components.ToHashSet();

        components.Remove(component);

        this.Components = components;

        return this;
    }

    public CaContractDraftVendorEdit RemoveAcceptor(CaContractDraftEditAcceptor acceptor)
    {
        var acceptors = this.Acceptors.ToHashSet();

        acceptors.Remove(acceptor);

        this.Acceptors = acceptors;

        return this;
    }

    // PaymentTerm management
    public CaContractDraftVendorEdit SetPaymentTerm(IEnumerable<CaContractDraftEditPaymentTerm> paymentTerms)
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

    public void CalculateAndSetDeliveryDates()
    {
        if (this.ContractSignedDate is null)
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

    private CaContractDraftVendorEdit AddPaymentTerm(CaContractDraftEditPaymentTerm paymentTerm)
    {
        var paymentTerms = this.PaymentTerms.ToHashSet();

        paymentTerms.Add(paymentTerm);

        this.PaymentTerms = paymentTerms;

        return this;
    }

    private CaContractDraftVendorEdit RemovePaymentTerm(CaContractDraftEditPaymentTerm paymentTerm)
    {
        var paymentTerms = this.PaymentTerms.ToHashSet();

        if (paymentTerms.Remove(paymentTerm))
        {
            this.PaymentTerms = paymentTerms;
        }

        return this;
    }

    // Attachment management
    public CaContractDraftVendorEdit SetAttachments(CaContractDraftEditVendorsAttachment attachment)
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

    public CaContractDraftVendorEdit RemoveAttachment(CaContractDraftEditVendorsAttachment attachment)
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

    // Shareholder management
    public CaContractDraftVendorEdit AddCaContractDraftEditVendorShareholderList(List<CaContractDraftEditVendorShareholders> shareholders)
    {
        this.Shareholders = shareholders;

        return this;
    }

    public CaContractDraftVendorEdit AddCaContractDraftEditVendorShareholder(CaContractDraftEditVendorShareholders shareholders)
    {
        var shareholdersList = this.Shareholders?.ToList() ?? new List<CaContractDraftEditVendorShareholders>();
        shareholdersList.Add(shareholders);

        this.Shareholders = shareholdersList;

        return this;
    }

    public CaContractDraftVendorEdit UpdateCaContractDraftEditVendorShareholder(CaContractDraftEditVendorShareholders shareholders)
    {
        var shareholdersList = this.Shareholders?.ToList() ?? new List<CaContractDraftEditVendorShareholders>();
        var idx = shareholdersList.FindIndex(a => a.Id == shareholders.Id);

        if (idx >= 0)
        {
            shareholdersList[idx] = shareholders;
            this.Shareholders = shareholdersList;
        }

        return this;
    }

    public CaContractDraftVendorEdit RemoveCaContractDraftEditVendorShareholder(CaContractDraftEditVendorShareholderId shareholdersId)
    {
        var entrepreneurShareholdersList = this.Shareholders?.ToList() ?? new List<CaContractDraftEditVendorShareholders>();
        entrepreneurShareholdersList.RemoveAll(a => a.Id == shareholdersId);
        this.Shareholders = entrepreneurShareholdersList;

        return this;
    }

    // Checker management
    public CaContractDraftVendorEdit AddChecker(
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
            (CaContractDraftEditVendorChecker)CaContractDraftEditVendorChecker
                .Create(
                    checkType,
                    result,
                    resultAt,
                    remark);

        checkerList.Add(checker);

        this.Checkers = checkerList;

        return this;
    }

    // Checker Attachment management
    public CaContractDraftVendorEdit AddAttachment(CaContractDraftEditVendorCheckerAttachments attachment)
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

    public CaContractDraftVendorEdit RemoveAttachment(CaContractDraftEditVendorCheckerAttachments attachment)
    {
        var list = this.CheckerAttachment.ToHashSet();
        list.Remove(attachment);
        this.CheckerAttachment = list;

        return this;
    }

    // Document History management
    public CaContractDraftEditVendorDocumentHistory? LastedDocumentByType(CaContractDraftEditVendorDocumentType docType) =>
        this.DocumentHistories
            .Where(d => d.DocumentType == docType)
            .OrderVersions()
            .FirstOrDefault();

    public Unit AddDocumentHistory(
        CaContractDraftEditVendorDocumentType documentType,
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

        histories.Add(CaContractDraftEditVendorDocumentHistory.Create(
            documentType,
            this.Status,
            version,
            fileId,
            replace));

        this.DocumentHistories = histories;

        return unit;
    }

    public CaContractDraftEditVendorDocumentHistory? GetAmendmentDocumentForStatus(ContractDraftVendorEditStatus statusContext) =>
        this.GetDocumentForStatus(CaContractDraftEditVendorDocumentType.Amendment, statusContext);

    public CaContractDraftEditVendorDocumentHistory? GetApprovalRequestDocumentForStatus(ContractDraftVendorEditStatus statusContext) =>
        this.GetDocumentForStatus(CaContractDraftEditVendorDocumentType.AmendmentApprovalRequest, statusContext);

    private CaContractDraftEditVendorDocumentHistory? GetDocumentForStatus(
        CaContractDraftEditVendorDocumentType docType,
        ContractDraftVendorEditStatus statusContext) =>
        statusContext switch
        {
            ContractDraftVendorEditStatus.Draft or
                ContractDraftVendorEditStatus.Rejected =>
                this.DocumentHistories
                    .Where(dh => dh.DocumentType == docType)
                    .Where(dh =>
                        dh.StatusState == ContractDraftVendorEditStatus.Draft ||
                        dh.StatusState == ContractDraftVendorEditStatus.Rejected)
                    .OrderVersions()
                    .FirstOrDefault(),

            ContractDraftVendorEditStatus.Editing =>
                this.DocumentHistories
                    .Where(dh => dh.DocumentType == docType)
                    .Where(dh =>
                        dh.StatusState == ContractDraftVendorEditStatus.Draft ||
                        dh.StatusState == ContractDraftVendorEditStatus.Rejected ||
                        dh.StatusState == ContractDraftVendorEditStatus.Editing)
                    .OrderVersions()
                    .FirstOrDefault(),

            ContractDraftVendorEditStatus.WaitingCommitteeApproval =>
                this.DocumentHistories
                    .Where(dh => dh.DocumentType == docType)
                    .Where(dh =>
                        dh.StatusState == ContractDraftVendorEditStatus.Draft ||
                        dh.StatusState == ContractDraftVendorEditStatus.Rejected ||
                        dh.StatusState == ContractDraftVendorEditStatus.Editing ||
                        (dh.StatusState == ContractDraftVendorEditStatus.WaitingCommitteeApproval && dh.IsReplaced))
                    .OrderVersions()
                    .FirstOrDefault(),

            ContractDraftVendorEditStatus.WaitingAssignment =>
                this.DocumentHistories
                    .Where(dh => dh.DocumentType == docType)
                    .Where(dh =>
                        dh.StatusState == ContractDraftVendorEditStatus.Draft ||
                        dh.StatusState == ContractDraftVendorEditStatus.Rejected ||
                        dh.StatusState == ContractDraftVendorEditStatus.Editing ||
                        dh.StatusState == ContractDraftVendorEditStatus.WaitingCommitteeApproval ||
                        dh.StatusState == ContractDraftVendorEditStatus.WaitingAssignment)
                    .OrderVersions()
                    .FirstOrDefault(),

            ContractDraftVendorEditStatus.WaitingComment =>
                this.DocumentHistories
                    .Where(dh => dh.DocumentType == docType)
                    .Where(dh =>
                        dh.StatusState == ContractDraftVendorEditStatus.Draft ||
                        dh.StatusState == ContractDraftVendorEditStatus.Rejected ||
                        dh.StatusState == ContractDraftVendorEditStatus.Editing ||
                        dh.StatusState == ContractDraftVendorEditStatus.WaitingCommitteeApproval ||
                        dh.StatusState == ContractDraftVendorEditStatus.WaitingAssignment ||
                        (dh.StatusState == ContractDraftVendorEditStatus.WaitingComment && dh.IsReplaced))
                    .OrderVersions()
                    .FirstOrDefault(),

            ContractDraftVendorEditStatus.WaitingApproval =>
                this.DocumentHistories
                    .Where(dh => dh.DocumentType == docType)
                    .Where(dh =>
                        dh.StatusState == ContractDraftVendorEditStatus.WaitingComment ||
                        (dh.StatusState == ContractDraftVendorEditStatus.WaitingApproval && dh.IsReplaced))
                    .OrderVersions()
                    .FirstOrDefault(),

            ContractDraftVendorEditStatus.Approved =>
                this.DocumentHistories
                    .Where(dh => dh.DocumentType == docType)
                    .Where(dh => dh.IsReplaced ||
                                 (dh.StatusState == ContractDraftVendorEditStatus.WaitingApproval && dh.IsReplaced) ||
                                 dh.StatusState == ContractDraftVendorEditStatus.Approved)
                    .OrderVersions()
                    .FirstOrDefault(),

            _ => this.DocumentHistories
                     .Where(dh => dh.DocumentType == docType)
                     .OrderVersions()
                     .FirstOrDefault(),
        };

    // Status transitions
    public CaContractDraftVendorEdit SetEditing()
    {
        this.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.Update,
                "แก้ไขข้อมูล",
                ContractDraftVendorEditStatus.Editing.ToString()));

        this.Status = ContractDraftVendorEditStatus.Editing;

        return this;
    }

    public CaContractDraftVendorEdit SetWaitingAssignment()
    {
        this.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.SendApprove,
                "ส่งมอบหมายผู้รับผิดชอบ",
                ContractDraftVendorEditStatus.WaitingAssignment.ToString()));

        this.Status = ContractDraftVendorEditStatus.WaitingAssignment;

        return this;
    }

    public CaContractDraftVendorEdit SetWaitingComment()
    {
        this.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.Update,
                "มอบหมายผู้รับผิดชอบ",
                ContractDraftVendorEditStatus.WaitingComment.ToString()));

        this.Status = ContractDraftVendorEditStatus.WaitingComment;

        return this;
    }

    public CaContractDraftVendorEdit SetWaitingCommitteeApproval()
    {
        this.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.SendApprove,
                "ส่งคณะกรรมการอนุมัติ",
                ContractDraftVendorEditStatus.WaitingCommitteeApproval.ToString()));

        this.Status = ContractDraftVendorEditStatus.WaitingCommitteeApproval;

        return this;
    }

    public CaContractDraftVendorEdit SetWaitingApproval()
    {
        this.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.SendApprove,
                "ส่งผู้มีอำนาจเห็นชอบ/อนุมัติ",
                ContractDraftVendorEditStatus.WaitingApproval.ToString()));

        this.Status = ContractDraftVendorEditStatus.WaitingApproval;

        var approvers = this.Acceptors
                            .Where(p => p.Type == AcceptorType.Approver)
                            .OrderBy(a => a.Sequence)
                            .ToList();

        approvers.Iter(a =>
        {
            if ((a.Status == AcceptorStatus.Draft || a.Status == AcceptorStatus.Rejected) && !a.IsUnableToPerformDuties)
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

    public CaContractDraftVendorEdit SetApproved(
        AcceptorId id,
        string? remark = null,
        DelegateeId? delegateId = null)
    {
        if (this.Status != ContractDraftVendorEditStatus.WaitingApproval)
        {
            throw new InvalidOperationException("สถานะไม่อยู่ระหว่างรออนุมัติ");
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
            this.Status = ContractDraftVendorEditStatus.Approved;
            this.AddActivity(
                new ActivityInfo(
                    ActivityLogActionTypeConstant.Approved,
                    "อนุมัติแก้ไขร่างสัญญาครบ",
                    ContractDraftVendorEditStatus.Approved.ToString(),
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
                "อนุมัติแก้ไขร่างสัญญา (บางส่วน)",
                ContractDraftVendorEditStatus.WaitingApproval.ToString(),
                remark));

        return this;
    }

    public CaContractDraftVendorEdit SetWaitingAddendumAssignment()
    {
        this.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.Update,
                "รอมอบหมายผู้จัดทำบันทึกต่อท้าย",
                ContractDraftVendorEditStatus.WaitingAddendumAssignment.ToString()));

        this.Status = ContractDraftVendorEditStatus.WaitingAddendumAssignment;

        return this;
    }

    public CaContractDraftVendorEdit SetWaitingDraftAddendum()
    {
        this.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.Update,
                "รอร่างเอกสารบันทึกต่อท้าย",
                ContractDraftVendorEditStatus.WaitingDraftAddendum.ToString()));

        this.Status = ContractDraftVendorEditStatus.WaitingDraftAddendum;

        return this;
    }

    public CaContractDraftVendorEdit SetWaitingReview()
    {
        this.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.Update,
                "รอตรวจสอบ",
                ContractDraftVendorEditStatus.WaitingReview.ToString()));

        this.Status = ContractDraftVendorEditStatus.WaitingReview;

        return this;
    }

    public CaContractDraftVendorEdit SetApprovedByReview()
    {
        this.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.Approved,
                "ตรวจสอบแล้ว",
                ContractDraftVendorEditStatus.Approved.ToString()));

        this.Status = ContractDraftVendorEditStatus.Approved;

        return this;
    }

    public CaContractDraftVendorEdit SetRejected(string? remark = null)
    {
        this.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.Reject,
                "ส่งกลับแก้ไข",
                ContractDraftVendorEditStatus.Rejected.ToString(),
                remark));

        this.Status = ContractDraftVendorEditStatus.Rejected;

        return this;
    }

    public CaContractDraftVendorEdit SetRejectedToAssignee(string? remark = null)
    {
        this.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.Reject,
                "ส่งกลับผู้รับผิดชอบ",
                ContractDraftVendorEditStatus.RejectedToAssignee.ToString(),
                remark));

        this.Status = ContractDraftVendorEditStatus.RejectedToAssignee;

        return this;
    }

    // Factory method
    public static CaContractDraftVendorEdit Create(
        ContractDraftVendorId contractDraftVendorId,
        Guid procurementId,
        ContractInvitationVendorsId contractInvitationVendorsId)
    {
        var newData = new CaContractDraftVendorEdit
        {
            Id = ContractDraftVendorEditId.New,
            ContractDraftVendorId = contractDraftVendorId,
            ProcurementId = procurementId,
            ContractInvitationVendorsId = contractInvitationVendorsId,
            Agreement = new AgreementContract(),
            DraftEquipmentRental = CaContractDraftEditEquipmentRental.Create(),
            DraftTermsConditions = CaContractDraftEditTermsConditions.Create(),
            Payment = Payment.Default(),
            Status = ContractDraftVendorEditStatus.Draft,
            Assignees = new List<CaContractDraftVendorEditAssignee>(),
            Components = new List<CaContractDraftVendorEditComponent>(),
            Acceptors = new List<CaContractDraftEditAcceptor>(),
            DocumentHistories = new List<CaContractDraftEditVendorDocumentHistory>(),
            PaymentTerms = new List<CaContractDraftEditPaymentTerm>(),
            Shareholders = new List<CaContractDraftEditVendorShareholders>(),
            Checkers = new List<CaContractDraftEditVendorChecker>(),
            CheckerAttachment = new List<CaContractDraftEditVendorCheckerAttachments>(),
            Attachments = new List<CaContractDraftEditVendorsAttachment>(),
        };

        newData.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.Create,
                "สร้างข้อมูลแก้ไขร่างสัญญา",
                newData.Status.ToString()));

        return newData;
    }
}