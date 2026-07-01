namespace GHB.DP2.Domain.Procurement.PExpenseDisbursement;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using LanguageExt;
using Vogen;

public enum PExpenseDisbursementStatus
{
    /// <summary>
    /// แบบร่าง
    /// </summary>
    Draft,

    /// <summary>
    /// ตีกลับ
    /// </summary>
    Rejected,

    /// <summary>
    /// เรียกคืนแก้ไข
    /// </summary>
    Edit,

    /// <summary>
    /// รอยินยันเบิกจ่าย
    /// </summary>
    WaitingApproval,

    /// <summary>
    /// อนุมัติแล้ว
    /// </summary>
    Approved,

    /// <summary>
    /// รอยืนยันเบิกจ่าย
    /// </summary>
    WaitingForCompletion,
}

public enum PExpenseDisbursementSourceType
{
    ExpenseDisbursement,
    W119,
    Clause79_2,
    ContractGuaranteeReturn,
    PettyCashReimbursement,
    DeliveryAcceptancePeriod,
}

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PExpenseDisbursementId
{
    public static PExpenseDisbursementId New() => From(Guid.CreateVersion7());
}

public partial class PExpenseDisbursement : AuditableEntity<PExpenseDisbursementId>, IHasSoftDelete, IHasActivityInfo
{
    public override PExpenseDisbursementId Id { get; init; }

    public PExpenseDisbursementStatus Status { get; set; }

    public PExpenseDisbursementSourceType SourceType { get; init; }

    public Guid SourceId { get; init; }

    public bool IsAdvance { get; private set; }

    public string? AdvanceName { get; private set; }

    public ParameterCode? AdvancePaymentMethodCode { get; private set; }

    public DateTimeOffset? AdvancePaymentDate { get; private set; }

    public ParameterCode? AdvanceBankCode { get; private set; }

    public bool? IsInvoiceAmount { get; private set; }

    public decimal? InvoiceAmount { get; private set; }

    public string? AdvanceBankAccount { get; private set; }

    public string? AdvanceBankBranch { get; private set; }

    public string? AdvanceBankAccountName { get; private set; }

    public string? AdvanceDetail { get; private set; }

    public DateTimeOffset Date { get; set; }

    public DateTimeOffset? DocumentDate { get; private set; }

    public string? Description { get; set; }

    public virtual SuParameter AdvancePaymentMethod { get; init; }

    public virtual SuParameter? AdvanceBank { get; init; }

    public virtual IReadOnlyCollection<PExpenseDisbursementAcceptor> Acceptors { get; private set; }

    public virtual IReadOnlyCollection<PExpenseDisbursementAssignee> Assignees { get; private set; }

    public virtual IReadOnlyCollection<PExpenseDisbursementGlAccount> GlAccounts { get; private set; }

    public virtual IReadOnlyCollection<PExpenseDisbursementAttachment> Attachments { get; private set; }

    public static PExpenseDisbursement Create(
        PExpenseDisbursementSourceType sourceType,
        Guid sourceId)
    {
        var newData = new PExpenseDisbursement
        {
            Id = PExpenseDisbursementId.New(),
            SourceType = sourceType,
            SourceId = sourceId,
            Status = PExpenseDisbursementStatus.Draft,
        };

        newData.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Create,
            $"สร้างข้อมูลใหม่ {nameof(PExpenseDisbursement)}",
            newData.Status.ToString()));

        return newData;
    }

    public record ExpenseDisbursementUpdateInfo(
        PExpenseDisbursementStatus Status,
        bool IsAdvance,
        string? AdvanceName,
        ParameterCode? AdvancePaymentMethodCode,
        DateTimeOffset? AdvancePaymentDate,
        ParameterCode? AdvanceBankCode,
        bool? IsInvoiceAmount,
        decimal? InvoiceAmount,
        string? AdvanceBankAccount,
        string? AdvanceBankBranch,
        string? AdvanceBankAccountName,
        string? AdvanceDetail,
        DateTimeOffset Date,
        string? Description);

    public PExpenseDisbursement SetValue(ExpenseDisbursementUpdateInfo updateInfo)
    {
        var previousStatus = this.Status; // capture previous status before change
        this.Status = updateInfo.Status;
        this.IsAdvance = updateInfo.IsAdvance;
        this.AdvanceName = updateInfo.AdvanceName;
        this.AdvancePaymentMethodCode = updateInfo.AdvancePaymentMethodCode;
        this.AdvancePaymentDate = updateInfo.AdvancePaymentDate;
        this.AdvanceBankCode = updateInfo.AdvanceBankCode;
        this.IsInvoiceAmount = updateInfo.IsInvoiceAmount;
        this.InvoiceAmount = updateInfo.InvoiceAmount;
        this.AdvanceBankAccount = updateInfo.AdvanceBankAccount;
        this.AdvanceBankBranch = updateInfo.AdvanceBankBranch;
        this.AdvanceBankAccountName = updateInfo.AdvanceBankAccountName;
        this.AdvanceDetail = updateInfo.AdvanceDetail;
        this.Date = updateInfo.Date;
        this.Description = updateInfo.Description;

        if (previousStatus != PExpenseDisbursementStatus.WaitingApproval && updateInfo.Status == PExpenseDisbursementStatus.WaitingApproval)
        {
            this.SetInitialApproverCurrent();
        }

        return this;
    }

    public PExpenseDisbursement SetStatus(PExpenseDisbursementStatus status)
    {
        this.Status = status;

        this.SetAllAcceptorsPending();

        return this;
    }

    public PExpenseDisbursement SetAllAcceptorsPending(bool resetCurrent = true)
    {
        if (this.Acceptors == null || this.Acceptors.Count == 0)
        {
            return this;
        }

        foreach (var acceptor in this.Acceptors.ToList())
        {
            acceptor.SetStatus(AcceptorStatus.Pending);

            if (resetCurrent)
            {
                acceptor.SetCurrent(false);
            }
        }

        if (resetCurrent)
        {
            var approvers = this.Acceptors?
                                .Where(a => a.Type == AcceptorType.Approver && a.IsActive && a.Status == AcceptorStatus.Pending)
                                .OrderBy(a => a.Sequence)
                                .ToList();

            if (approvers is not null && approvers.Count > 0)
            {
                var firstSeq = approvers.First().Sequence;
                foreach (var a in approvers.Where(a => a.Sequence == firstSeq))
                {
                    a.SetCurrent(true);
                }
            }
        }

        return this;
    }

    public PExpenseDisbursement AddAcceptor(PExpenseDisbursementAcceptor acceptor)
    {
        var acceptors = this.Acceptors?.ToList() ?? new List<PExpenseDisbursementAcceptor>();
        acceptors.Add(acceptor);
        this.Acceptors = acceptors;

        return this;
    }

    public PExpenseDisbursement RemoveAcceptor(PExpenseDisbursementAcceptor acceptor)
    {
        var list = this.Acceptors?.ToList() ?? new List<PExpenseDisbursementAcceptor>();
        list.Remove(acceptor);
        this.Acceptors = list;

        return this;
    }

    public PExpenseDisbursement AddGlAccount(PExpenseDisbursementGlAccount glAccount)
    {
        var glAccounts = this.GlAccounts?.ToList() ?? new List<PExpenseDisbursementGlAccount>();
        glAccounts.Add(glAccount);
        this.GlAccounts = glAccounts;

        return this;
    }

    public PExpenseDisbursement RemoveGlAccount(PExpenseDisbursementGlAccount glAccount)
    {
        var glAccounts = this.GlAccounts?.ToList() ?? new List<PExpenseDisbursementGlAccount>();
        glAccounts.Remove(glAccount);
        this.GlAccounts = glAccounts;

        return this;
    }

    private void SetInitialApproverCurrent()
    {
        if (this.Status != PExpenseDisbursementStatus.WaitingApproval)
        {
            return;
        }

        var approvers = this.Acceptors?
                            .Where(a => a.Type == AcceptorType.Approver && a.IsActive && a.Status == AcceptorStatus.Pending)
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

        var firstSeq = approvers.First().Sequence;

        foreach (var a in approvers.Where(a => a.Sequence == firstSeq))
        {
            a.SetCurrent(true);
        }
    }

    public PExpenseDisbursement EvaluateAcceptorApproval()
    {
        var approvers = this.Acceptors?
                            .Where(a => a.Type == AcceptorType.Approver && a.IsActive)
                            .ToList();

        if (approvers == null || approvers.Count == 0)
        {
            return this;
        }

        if (approvers.All(a => a.Status == AcceptorStatus.Approved) && this.Status == PExpenseDisbursementStatus.WaitingApproval)
        {
            this.Status = PExpenseDisbursementStatus.WaitingForCompletion;
            this.AddActivity(new ActivityInfo(
                ActivityLogActionTypeConstant.Update,
                "เห็นชอบ/อนุมัติ",
                this.Status.ToString()));
        }

        return this;
    }

    public PExpenseDisbursement SetRejected(string? remark)
    {
        if (this.Status is not (PExpenseDisbursementStatus.WaitingApproval or PExpenseDisbursementStatus.Edit))
        {
            return this;
        }

        this.Status = PExpenseDisbursementStatus.Rejected;
        this.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Reject,
            "ตีกลับแก้ไข เบิกจ่าย",
            this.Status.ToString(),
            remark));

        return this;
    }

    public PExpenseDisbursement AddAttachment(PExpenseDisbursementAttachment attachment)
    {
        if (attachment == null)
        {
            throw new ArgumentNullException(nameof(attachment), "Attachment cannot be null.");
        }

        if (this.Attachments.Contains(attachment))
        {
            throw new InvalidOperationException("Attachment already exists in the plan.");
        }

        var attachments = this.Attachments.ToHashSet();

        attachments.Add(attachment);

        this.Attachments = attachments;

        return this;
    }

    public PExpenseDisbursement RemoveAttachment(PExpenseDisbursementAttachment attachment)
    {
        var list = this.Attachments.ToHashSet();
        list.Remove(attachment);
        this.Attachments = list;

        return this;
    }

    public Unit AddAssignee(PExpenseDisbursementAssignee assignee)
    {
        if (assignee == null)
        {
            throw new ArgumentNullException(nameof(assignee), "Assignee cannot be null.");
        }

        if (this.Assignees.Contains(assignee))
        {
            throw new InvalidOperationException("Assignee already exists in the plan.");
        }

        var assignees = this.Assignees.ToHashSet();

        assignees.Add(assignee);

        this.Assignees = assignees;

        return unit;
    }

    public Unit RemoveAssigneeById(PExpenseDisbursementAssigneeId assigneeId)
    {
        var assignees = this.Assignees.ToHashSet();

        assignees.RemoveWhere(w => w.Id == assigneeId);

        this.Assignees = assignees;

        return unit;
    }
}