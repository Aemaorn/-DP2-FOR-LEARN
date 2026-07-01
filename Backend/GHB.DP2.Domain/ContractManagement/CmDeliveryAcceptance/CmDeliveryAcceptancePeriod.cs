namespace GHB.DP2.Domain.ContractManagement.CmDeliveryAcceptance;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility; // ensure ParameterCode available
using LanguageExt;
using System.ComponentModel.DataAnnotations;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct CmDeliveryAcceptancePeriodId
{
    public static CmDeliveryAcceptancePeriodId New() => From(Guid.CreateVersion7());
}

public enum CmDeliveryAcceptancePeriodStatus
{
    /// <summary>
    /// แบบร่าง
    /// </summary>
    Draft,

    /// <summary>
    /// รอเห็นชอบโดย บุคคล/คณะกรรมการตรวจรับพัสดุ
    /// </summary>
    WaitingCommitteeApproval,

    /// <summary>
    /// จพ. มอบหมายงาน (เทียบเท่ากับ Assigned อันเก่า)
    /// </summary>
    WaitingAssign,

    /// <summary>
    /// อยู่ระหว่าง จพ. ให้ความเห็น (*new_status จะมาจากการยืนยันมอบหมาย)
    /// </summary>
    WaitingComment,

    /// <summary>
    /// รอเห็นชอบโดย ผู้มีอำนาจเห็นชอบ/อนุมัติ
    /// </summary>
    WaitingAcceptance,

    /// <summary>
    /// อนุมัติแล้ว
    /// </summary>
    Approved,

    /// <summary>
    /// ส่งกลับแก้ไข
    /// </summary>
    Edit,

    /// <summary>
    /// ปฏิเสธ
    /// </summary>
    Rejected,

    /// <summary>
    /// ส่งกลับแก้ไข (ส่งกลับแก้ไขไปยัง จพ.)
    /// </summary>
    RejectToAssignee,
}

public enum CmDeliveryAcceptancePeriodAccountStatus
{
    WaitingAccountingApproval,
    AccountingRejected,
    WaitingDisbursementDate,
    Paid,
}

/// <summary>
/// รายการงวด ส่งมอบและตรวจรับงาน
/// </summary>
public partial class CmDeliveryAcceptancePeriod : AuditableEntity<CmDeliveryAcceptancePeriodId>, IHasSoftDelete, IHasActivityInfo
{
    public override CmDeliveryAcceptancePeriodId Id { get; init; }

    public CmDeliveryAcceptanceId CmDeliveryAcceptanceId { get; init; }

    public CmDeliveryAcceptancePeriodStatus Status { get; private set; }

    public DateTimeOffset? DocumentDate { get; private set; }

    public virtual CmDeliveryAcceptance CmDeliveryAcceptance { get; init; }

    public DateTimeOffset? AcceptanceDate { get; private set; }

    [MaxLength(200)]
    public string? AcceptanceNumber { get; private set; }

    [MaxLength(2000)]
    public string? Description { get; private set; }

    public decimal? AcceptedAmount { get; private set; }

    public bool HasDeduction { get; private set; }

    public string? DeductionDescription { get; private set; }

    public decimal? DeductionAmount { get; private set; }

    public bool HasInvoiceSlip { get; private set; }

    public string? InvoiceSlipDescription { get; private set; }

    public decimal? InvoiceSlipAmount { get; private set; }

    public string? PhoneNumber { get; private set; }

    public string? ObjectiveDescription { get; private set; }

    public decimal? ContractBudgetAmount { get; private set; }

    public CmDeliveryAcceptancePeriodAccountStatus AccountStatus { get; private set; }

    public DateTimeOffset? DisbursementDate { get; private set; }

    public decimal? DisbursementAmount { get; private set; }

    public string? DisbursementRemark { get; private set; }

    public virtual IReadOnlyCollection<CmDeliveryAcceptancePeriodAcceptor> Acceptors { get; private set; }

    public virtual IReadOnlyCollection<CmDeliveryAcceptancePeriodAssignee> Assignees { get; private set; }

    public virtual IReadOnlyCollection<CmDeliveryAcceptancePeriodDocumentHistory> DocumentHistories { get; private set; }

    public virtual IReadOnlyCollection<CmDeliveryAcceptancePeriodBudget> Budgets { get; private set; }

    public virtual IReadOnlyCollection<CmDeliveryAcceptancePeriodPaymentTerm> PaymentTerms { get; private set; }

    public virtual IReadOnlyCollection<CmDeliveryAcceptancePeriodAttachment> Attachments { get; private set; }

    public CmDeliveryAcceptancePeriodDocumentHistory? Document =>
        this.DocumentHistories
            .Where(p => p.DocumentType == CmDeliveryAcceptanceDocumentType.DeliveryAcceptance)
            .OrderVersions()
            .FirstOrDefault();

    public CmDeliveryAcceptancePeriodDocumentHistory? LastedDraftDocument =>
        this.DocumentHistories
            .Where(dh =>
                dh.DocumentType == CmDeliveryAcceptanceDocumentType.DeliveryAcceptance)
            .Where(dh =>
                dh.StatusState == CmDeliveryAcceptancePeriodStatus.Draft ||
                dh.StatusState == CmDeliveryAcceptancePeriodStatus.Edit ||
                dh.StatusState == CmDeliveryAcceptancePeriodStatus.Rejected ||
                dh.StatusState == CmDeliveryAcceptancePeriodStatus.WaitingCommitteeApproval ||
                dh.StatusState == CmDeliveryAcceptancePeriodStatus.WaitingAcceptance ||
                dh.StatusState == CmDeliveryAcceptancePeriodStatus.Approved)
            .OrderVersions()
            .FirstOrDefault();

    public CmDeliveryAcceptancePeriodDocumentHistory? LastedNotReplacedDocument =>
        this.DocumentHistories
            .Where(dh =>
                dh is
                {
                    StatusState: CmDeliveryAcceptancePeriodStatus.WaitingCommitteeApproval,
                    IsReplaced: false,
                })
            .Where(dh =>
                dh.DocumentType == CmDeliveryAcceptanceDocumentType.DeliveryAcceptance)
            .OrderVersions()
            .FirstOrDefault();

    public CmDeliveryAcceptancePeriodDocumentHistory? GetDocumentForStatus(
        CmDeliveryAcceptancePeriodStatus statusContext) =>
        statusContext switch
        {
            CmDeliveryAcceptancePeriodStatus.WaitingCommitteeApproval or
            CmDeliveryAcceptancePeriodStatus.Edit =>
                this.DocumentHistories
                    .Where(dh => dh.DocumentType == CmDeliveryAcceptanceDocumentType.DeliveryAcceptance)
                    .Where(dh =>
                        dh.StatusState == CmDeliveryAcceptancePeriodStatus.Draft ||
                        dh.StatusState == CmDeliveryAcceptancePeriodStatus.Edit ||
                        dh.StatusState == CmDeliveryAcceptancePeriodStatus.Rejected ||
                        (dh.StatusState == CmDeliveryAcceptancePeriodStatus.WaitingCommitteeApproval && dh.IsReplaced == true))
                    .OrderVersions()
                    .FirstOrDefault(),

            CmDeliveryAcceptancePeriodStatus.Approved =>
                this.DocumentHistories
                    .Where(dh => dh.DocumentType == CmDeliveryAcceptanceDocumentType.DeliveryAcceptance)
                    .Where(dh => dh.IsReplaced == true &&
                        (dh.StatusState == CmDeliveryAcceptancePeriodStatus.WaitingCommitteeApproval ||
                         dh.StatusState == CmDeliveryAcceptancePeriodStatus.Approved))
                    .OrderVersions()
                    .FirstOrDefault(),

            CmDeliveryAcceptancePeriodStatus.Rejected or
            CmDeliveryAcceptancePeriodStatus.WaitingComment =>
                // Covers both committee/approver Reject AND AccountingReject
                this.DocumentHistories
                    .Where(dh => dh.DocumentType == CmDeliveryAcceptanceDocumentType.DeliveryAcceptance)
                    .Where(dh => dh is
                    {
                        StatusState: CmDeliveryAcceptancePeriodStatus.WaitingCommitteeApproval,
                        IsReplaced: false,
                    })
                    .OrderVersions()
                    .FirstOrDefault(),

            CmDeliveryAcceptancePeriodStatus.RejectToAssignee =>
                this.DocumentHistories
                    .Where(dh => dh.DocumentType == CmDeliveryAcceptanceDocumentType.DeliveryAcceptance)
                    .Where(dh => dh is
                    {
                        StatusState: CmDeliveryAcceptancePeriodStatus.WaitingAcceptance,
                        IsReplaced: false,
                    })
                    .OrderVersions()
                    .FirstOrDefault(),

            _ => this.DocumentHistories
                     .Where(dh => dh.DocumentType == CmDeliveryAcceptanceDocumentType.DeliveryAcceptance)
                     .OrderVersions()
                     .FirstOrDefault(),
        };

    public Unit AddDocumentHistory(
        CmDeliveryAcceptanceDocumentType documentType,
        FileId fileId,
        bool isReplace = false)
    {
        var histories = this.DocumentHistories.ToHashSet();

        var existingHistory =
            histories
                .Where(dh => dh.DocumentType == documentType)
                .OrderVersions()
                .FirstOrDefault();

        var isIncreaseMajorVersion =
            existingHistory is null ||
            existingHistory.StatusState != this.Status;

        var version =
            this.DocumentHistories
                .Where(dh => dh.DocumentType == documentType)
                .NextVersion(isIncreaseMajorVersion);

        histories.Add(
            CmDeliveryAcceptancePeriodDocumentHistory.Create(
                documentType,
                this.Status,
                version,
                fileId,
                isReplace));

        this.DocumentHistories = histories;

        return unit;
    }

    public static CmDeliveryAcceptancePeriod Create(
        CmDeliveryAcceptanceId cmDeliveryAcceptanceId,
        CmDeliveryAcceptancePeriodStatus status)
    {
        return new CmDeliveryAcceptancePeriod
        {
            Id = CmDeliveryAcceptancePeriodId.New(),
            CmDeliveryAcceptanceId = cmDeliveryAcceptanceId,
            Status = status,
            DocumentHistories = [],
            Acceptors = [],
            Assignees = [],
            Budgets = [],
            PaymentTerms = [],
            Attachments = [],
            AccountStatus = CmDeliveryAcceptancePeriodAccountStatus.WaitingAccountingApproval,
        };
    }

    public static CmDeliveryAcceptancePeriod Create(
        CmDeliveryAcceptancePeriodStatus status)
    {
        var newData = new CmDeliveryAcceptancePeriod
        {
            Id = CmDeliveryAcceptancePeriodId.New(),
            Status = status,
            DocumentHistories = [],
            Acceptors = [],
            Assignees = [],
            Budgets = [],
            PaymentTerms = [],
            Attachments = [],
        };

        newData.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.Create,
                string.Empty,
                status.ToString()));

        return newData;
    }

    public static CmDeliveryAcceptancePeriod Create(CmDeliveryAcceptanceId cmDeliveryAcceptanceId)
    {
        var newData = new CmDeliveryAcceptancePeriod
        {
            Id = CmDeliveryAcceptancePeriodId.New(),
            CmDeliveryAcceptanceId = cmDeliveryAcceptanceId,
            Status = CmDeliveryAcceptancePeriodStatus.Draft,
            DocumentHistories = [],
            Acceptors = [],
            Assignees = [],
            Budgets = [],
            Attachments = [],
        };

        return newData;
    }

    public static CmDeliveryAcceptancePeriod Create(CmDeliveryAcceptance cmDeliveryAcceptance)
    {
        var newData = new CmDeliveryAcceptancePeriod
        {
            Id = CmDeliveryAcceptancePeriodId.New(),
            Status = CmDeliveryAcceptancePeriodStatus.Draft,
            DocumentHistories = [],
            Acceptors = [],
            Assignees = [],
            Budgets = [],
            Attachments = [],
            CmDeliveryAcceptance = cmDeliveryAcceptance,
        };

        return newData;
    }

    public CmDeliveryAcceptancePeriod UpdateAcceptanceInfo(
        DateTimeOffset? acceptanceDate,
        string? acceptanceNumber,
        string? description,
        decimal? acceptedAmount,
        bool hasDeduction)
    {
        this.AcceptanceDate = acceptanceDate;
        this.AcceptanceNumber = acceptanceNumber;
        this.Description = description;
        this.AcceptedAmount = acceptedAmount;
        this.HasDeduction = hasDeduction;

        return this;
    }

    public CmDeliveryAcceptancePeriod SetValue(
        bool hasDeduction,
        string? deductionDescription,
        decimal? deductionAmount,
        bool hasInvoiceSlip,
        string? invoiceSlipDescription,
        decimal? invoiceSlipAmount,
        string? phoneNumber,
        string? objectiveDescription,
        decimal? contractBudgetAmount,
        string? description)
    {
        this.HasDeduction = hasDeduction;
        this.DeductionDescription = deductionDescription;
        this.DeductionAmount = deductionAmount;
        this.HasInvoiceSlip = hasInvoiceSlip;
        this.InvoiceSlipDescription = invoiceSlipDescription;
        this.InvoiceSlipAmount = invoiceSlipAmount;
        this.PhoneNumber = phoneNumber;
        this.ObjectiveDescription = objectiveDescription;
        this.ContractBudgetAmount = contractBudgetAmount;
        this.Description = description;

        return this;
    }

    public CmDeliveryAcceptancePeriod SetContractBudget(decimal? contractBudgetAmount)
    {
        this.ContractBudgetAmount = contractBudgetAmount;

        return this;
    }

    public CmDeliveryAcceptancePeriod SetAcceptanceNumber(string? acceptanceNumber)
    {
        this.AcceptanceNumber = acceptanceNumber;

        return this;
    }

    public CmDeliveryAcceptancePeriod SetDocumentDate(DateTimeOffset? date = null)
    {
        this.DocumentDate = date ?? DateTimeOffset.Now;

        return this;
    }

    public CmDeliveryAcceptancePeriod UpdateStatus(CmDeliveryAcceptancePeriodStatus status, string? remark = null)
    {
        if (this.Status == status)
        {
            return this;
        }

        this.Status = status;

        if (status == CmDeliveryAcceptancePeriodStatus.WaitingCommitteeApproval)
        {
            this.SetCommitteesToPending();
            this.EnsureInitialAcceptorCurrents();
            this.SetAssigneeToPending();

            this.AddActivity(
                new ActivityInfo(
                    ActivityLogActionTypeConstant.SendCommitteeApprove,
                    string.Empty,
                    nameof(CmDeliveryAcceptancePeriodStatus.WaitingAcceptance)));
        }

        if (status == CmDeliveryAcceptancePeriodStatus.WaitingComment)
        {
            this.SetAssigneeToPending();

            this.AddActivity(
                new ActivityInfo(
                    ActivityLogActionTypeConstant.WaitingComment,
                    string.Empty,
                    nameof(CmDeliveryAcceptancePeriodStatus.WaitingComment)));
        }

        if (status == CmDeliveryAcceptancePeriodStatus.WaitingAcceptance)
        {
            this.SetAcceptorsToPending();
            this.SetInitialApproverCurrent();

            this.AddActivity(
                new ActivityInfo(
                    ActivityLogActionTypeConstant.SendApprove,
                    string.Empty,
                    nameof(CmDeliveryAcceptancePeriodStatus.WaitingAcceptance),
                    remark));
        }

        if (status == CmDeliveryAcceptancePeriodStatus.Edit)
        {
            this.SetCommitteesToDraft();

            this.AddActivity(
                new ActivityInfo(
                    ActivityLogActionTypeConstant.Recall,
                    string.Empty,
                    nameof(CmDeliveryAcceptancePeriodStatus.WaitingAcceptance)));
        }

        if (status == CmDeliveryAcceptancePeriodStatus.WaitingAssign)
        {
            this.SetAssigneeToPending();

            this.AddActivity(
                new ActivityInfo(
                    ActivityLogActionTypeConstant.CommitteeApproved,
                    string.Empty,
                    nameof(CmDeliveryAcceptancePeriodStatus.WaitingAssign),
                    remark));
        }

        return this;
    }

    private void EnsureInitialAcceptorCurrents()
    {
        var approvers = this.Acceptors?
                            .Where(a => a.Type == AcceptorType.AcceptanceCommittee && a.IsActive && a.Status == AcceptorStatus.Pending && !a.IsUnableToPerformDuties)
                            .ToList();

        if (approvers == null || approvers.Count == 0)
        {
            return;
        }

        // if any approver already approved, do not reset currents
        if (this.Acceptors!.Any(a => a.Type == AcceptorType.AcceptanceCommittee && a.Status == AcceptorStatus.Approved))
        {
            return; // progress already started
        }

        var chairman = approvers.FirstOrDefault(a => a.CommitteePositionsCode != null && a.CommitteePositionsCode == ParameterCode.From("PosBoard001"));

        foreach (var a in approvers)
        {
            a.SetCurrent(false);
        }

        var nonChair = approvers.Where(a => chairman == null || a.Id != chairman.Id).ToList();

        if (nonChair.Count == 0 && chairman != null)
        {
            chairman.SetCurrent(true); // only chairman

            return;
        }

        foreach (var a in nonChair)
        {
            a.SetCurrent(true);
        }

        if (chairman != null)
        {
            chairman.SetCurrent(false);
        }
    }

    private void SetInitialApproverCurrent()
    {
        if (this.Status != CmDeliveryAcceptancePeriodStatus.WaitingAcceptance)
        {
            return;
        }

        var approvers = this.Acceptors?
                            .Where(a => a.Type == AcceptorType.Approver && a.IsActive && a.Status == AcceptorStatus.Pending && !a.IsUnableToPerformDuties)
                            .OrderBy(a => a.Sequence)
                            .ToList();

        if (approvers is null || approvers.Count == 0)
        {
            return;
        }

        // reset all current
        foreach (var a in approvers)
        {
            a.SetCurrent(false);
        }

        var firstSeq = approvers[0].Sequence;

        foreach (var a in approvers.Where(a => a.Sequence == firstSeq))
        {
            a.SetCurrent(true);
        }
    }

    public CmDeliveryAcceptancePeriod AddBudget(CmDeliveryAcceptancePeriodBudget budget)
    {
        var budgets = (this.Budgets ?? []).ToHashSet();

        if (budgets.Any(b => b.Id == budget.Id))
        {
            throw new InvalidOperationException("Budget with the same Id already exists.");
        }

        budgets.Add(budget);

        this.Budgets = budgets;

        return this;
    }

    public CmDeliveryAcceptancePeriod RemoveBudget(CmDeliveryAcceptancePeriodBudget budget)
    {
        if (budget == null)
        {
            throw new ArgumentNullException(nameof(budget));
        }

        var budgets = this.Budgets.ToHashSet();

        if (!budgets.Remove(budget))
        {
            throw new InvalidOperationException("Budget not found.");
        }

        this.Budgets = budgets;

        return this;
    }

    public CmDeliveryAcceptancePeriod UpdateBudget(
        CmDeliveryAcceptancePeriodBudget budget,
        int sequence,
        string department,
        ParameterCode budgetTypeCode,
        string? projectCode,
        ParameterCode accountNoCode,
        decimal budgetAmount)
    {
        if (budget == null)
        {
            throw new ArgumentNullException(nameof(budget));
        }

        var budgets = this.Budgets.ToHashSet();

        if (!budgets.Contains(budget))
        {
            throw new InvalidOperationException("Budget not found.");
        }

        budget.Update(sequence, department, budgetTypeCode, projectCode, accountNoCode, budgetAmount);

        return this;
    }

    public CmDeliveryAcceptancePeriod AddPaymentTerm(CmDeliveryAcceptancePeriodPaymentTerm paymentTerm)
    {
        var paymentTermHasSet = (this.PaymentTerms ?? []).ToHashSet();

        if (paymentTermHasSet.Any(b => b.Id == paymentTerm.Id))
        {
            throw new InvalidOperationException("PaymentTerm with the same Id already exists.");
        }

        paymentTermHasSet.Add(paymentTerm);

        this.PaymentTerms = paymentTermHasSet;

        return this;
    }

    public CmDeliveryAcceptancePeriod RemovePaymentTerm(CmDeliveryAcceptancePeriodPaymentTerm paymentTerm)
    {
        if (paymentTerm == null)
        {
            throw new ArgumentNullException(nameof(paymentTerm));
        }

        var paymentTermHasSet = this.PaymentTerms.ToHashSet();

        if (!paymentTermHasSet.Remove(paymentTerm))
        {
            throw new InvalidOperationException("PaymentTerm not found.");
        }

        this.PaymentTerms = paymentTermHasSet;

        return this;
    }

    public CmDeliveryAcceptancePeriod UpdatePaymentTerm(
        CmDeliveryAcceptancePeriodPaymentTerm paymentTerm,
        int sequence,
        int paymentTermNo,
        string description,
        decimal amount)
    {
        if (paymentTerm == null)
        {
            throw new ArgumentNullException(nameof(paymentTerm));
        }

        var paymentTermHasSet = this.PaymentTerms.ToHashSet();

        if (!paymentTermHasSet.Contains(paymentTerm))
        {
            throw new InvalidOperationException("PaymentTerm not found.");
        }

        paymentTerm.Update(sequence, paymentTermNo, description, amount);

        return this;
    }

    public CmDeliveryAcceptancePeriod AddAttachment(CmDeliveryAcceptancePeriodAttachment attachment)
    {
        var attachments = (this.Attachments ?? []).ToHashSet();

        if (attachments.Any(a => a.Id == attachment.Id))
        {
            throw new InvalidOperationException("Attachment with the same Id already exists.");
        }

        attachments.Add(attachment);

        this.Attachments = attachments;

        return this;
    }

    public CmDeliveryAcceptancePeriod RemoveAttachment(CmDeliveryAcceptancePeriodAttachment attachment)
    {
        if (attachment == null)
        {
            throw new ArgumentNullException(nameof(attachment));
        }

        var attachments = this.Attachments.ToHashSet();

        if (!attachments.Remove(attachment))
        {
            throw new InvalidOperationException("Attachment not found.");
        }

        this.Attachments = attachments;

        return this;
    }

    public CmDeliveryAcceptancePeriod AddAcceptor(CmDeliveryAcceptancePeriodAcceptor acceptor)
    {
        var acceptors = this.Acceptors?.ToList() ?? new List<CmDeliveryAcceptancePeriodAcceptor>();

        acceptors.Add(acceptor);

        this.Acceptors = acceptors;

        return this;
    }

    public CmDeliveryAcceptancePeriod RemoveAcceptor(CmDeliveryAcceptancePeriodAcceptor acceptor)
    {
        if (acceptor == null)
        {
            throw new ArgumentNullException(nameof(acceptor));
        }

        var acceptors = this.Acceptors.ToHashSet();

        if (!acceptors.Remove(acceptor))
        {
            throw new InvalidOperationException("Acceptor not found.");
        }

        this.Acceptors = acceptors;

        return this;
    }

    private void SetCommitteesToPending()
    {
        _ = this.Acceptors
                .Where(a => a.Type == AcceptorType.AcceptanceCommittee && a.IsActive && !a.IsUnableToPerformDuties)
                .Iter(a => a.Pending());
    }

    private void SetCommitteesToDraft()
    {
        _ = this.Acceptors
                .Where(a => a is
                {
                    IsActive: true,
                    Type: AcceptorType.AcceptanceCommittee,
                    IsUnableToPerformDuties: false,
                })
                .Iter(a => a.Draft());
    }

    private void SetAssigneeToPending()
    {
        _ = this.Assignees.Iter(r => r.Pending());
    }

    private void SetAcceptorsToPending()
    {
        _ = this.Assignees
                .Where(a => a is
                {
                    Type: AssigneeType.Assignee,
                    IsDeleted: false
                })
                .Iter(a => a.Assigned());

        _ = this.Acceptors
                .Where(a => a.Type == AcceptorType.Approver && a.IsActive)
                .Iter(a => a.Pending());
    }

    public CmDeliveryAcceptancePeriod EvaluateAcceptorApproval()
    {
        var isAllApproved =
            this.Acceptors
                .Where(a => a is
                {
                    Type: AcceptorType.Approver,
                    IsActive: true,
                })
                .All(a => a.Status == AcceptorStatus.Approved);

        if (!isAllApproved)
        {
            return this;
        }

        this.SetApproved();

        return this;
    }

    private void SetApproved()
    {
        this.AcceptanceDate = DateTimeOffset.UtcNow;

        this.Status = CmDeliveryAcceptancePeriodStatus.Approved;
    }

    public CmDeliveryAcceptancePeriod SetRejected(string? remark = null)
    {
        switch (this.Status)
        {
            case CmDeliveryAcceptancePeriodStatus.WaitingAcceptance:

                this.AddActivity(
                    new ActivityInfo(
                        ActivityLogActionTypeConstant.Reject,
                        string.Empty,
                        this.Status.ToString(),
                        remark));

                break;

            case CmDeliveryAcceptancePeriodStatus.WaitingAssign:
            case CmDeliveryAcceptancePeriodStatus.WaitingComment:

                this.AddActivity(
                    new ActivityInfo(
                        "เจ้าหน้าที่พัสดุส่งกลับแก้ไข",
                        string.Empty,
                        this.Status.ToString(),
                        remark));

                break;
        }

        this.Status = CmDeliveryAcceptancePeriodStatus.Rejected;

        return this;
    }

    public CmDeliveryAcceptancePeriod SetRejectedCommittee(string? remark = null)
    {
        this.Status = CmDeliveryAcceptancePeriodStatus.Rejected;

        this.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.CommitteeReject,
                string.Empty,
                this.Status.ToString(),
                remark));

        return this;
    }

    public CmDeliveryAcceptancePeriod SetRejectToAssignee(string? remark = null)
    {
        this.Assignees.Iter(r => r.Pending());
        this.Status = CmDeliveryAcceptancePeriodStatus.RejectToAssignee;

        this.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.Reject,
                string.Empty,
                this.Status.ToString(),
                remark));

        return this;
    }

    public CmDeliveryAcceptancePeriod AddAssignee(CmDeliveryAcceptancePeriodAssignee assignee)
    {
        if (assignee is null)
        {
            throw new ArgumentNullException(nameof(assignee), "Assignee cannot be null.");
        }

        if (this.Assignees.Any(a => a.Id == assignee.Id))
        {
            throw new InvalidOperationException("Assignee already exists in the draft.");
        }

        var assigneesList = (this.Assignees ?? []).ToHashSet();

        assigneesList.Add(assignee);

        this.Assignees = assigneesList;

        return this;
    }

    public CmDeliveryAcceptancePeriod RemoveAssignee(CmDeliveryAcceptancePeriodAssignee assignee)
    {
        if (assignee is null)
        {
            throw new ArgumentNullException(nameof(assignee), "Assignee cannot be null.");
        }

        var assigneesList = (this.Assignees ?? []).ToHashSet();

        if (!assigneesList.Remove(assignee))
        {
            throw new InvalidOperationException("Assignee does not exist in the draft.");
        }

        this.Assignees = assigneesList;

        return this;
    }

    public bool HasMajorityRejection()
    {
        if (this.Status != CmDeliveryAcceptancePeriodStatus.WaitingCommitteeApproval)
        {
            return false;
        }

        var committeesAble =
            this.Acceptors
                .Where(a => a is
                {
                    Type: AcceptorType.AcceptanceCommittee,
                    IsUnableToPerformDuties: false,
                    IsActive: true,
                })
                .ToHashSet();

        var totalCommittees = committeesAble.Count;

        if (totalCommittees == 0)
        {
            throw new InvalidOperationException(
                "Cannot evaluate committee approval when there are no committees able to perform duties.");
        }

        var totalReject =
            committeesAble.Count(a => a.Status == AcceptorStatus.Rejected);

        return totalReject > totalCommittees / 2.0;
    }

    public CmDeliveryAcceptancePeriod AccountingUpdateStatus(
        CmDeliveryAcceptancePeriodAccountStatus accountStatus)
    {
        this.AccountStatus = accountStatus;

        return this;
    }

    public CmDeliveryAcceptancePeriod AccountingUpdateValue(
        DateTimeOffset? disbursementDate,
        decimal? disbursementAmount,
        string? disbursementRemark)
    {
        this.DisbursementDate = disbursementDate;
        this.DisbursementAmount = disbursementAmount;
        this.DisbursementRemark = disbursementRemark;

        return this;
    }
}