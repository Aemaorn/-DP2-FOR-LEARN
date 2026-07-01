namespace GHB.DP2.Domain.Common;

using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using LanguageExt;

public enum AssigneeGroup
{
    /// <summary>
    /// กลุ่ม จพ.
    /// </summary>
    JorPor,

    /// <summary>
    /// กลุ่ม สัญญา
    /// </summary>
    Contract,

    /// <summary>
    /// กลุ่ม หัวหน้าส่วนทั่วไป
    /// </summary>
    GeneralHead,

    /// <summary>
    /// กลุ่ม บัญชี
    /// </summary>
    Accounting,

    /// <summary>
    /// กลุ่ม ผู้จัดทำเอกสารบันทึกต่อท้าย
    /// </summary>
    AddendumDrafter,
}

public enum AssigneeType
{
    /// <summary>
    /// ผอ. จพ. มอบหมาย
    /// </summary>
    Director,

    /// <summary>
    /// ผู้ที่ได้รับมอบหมาย
    /// </summary>
    Assignee,
}

public enum AssigneeStatus
{
    /// <summary>
    /// แบบร่าง
    /// </summary>
    Draft,

    /// <summary>
    /// รออนุมัติ
    /// </summary>
    Pending,

    /// <summary>
    /// มอบหมายแล้ว
    /// </summary>
    Assigned,

    /// <summary>
    /// ปฏิเสธ
    /// </summary>
    Rejected,
}

public interface IHasAssignee
{
    public AssigneeGroup Group { get; }

    public AssigneeType Type { get; }

    public UserId UserId { get; }

    public EmployeeCode EmployeeCode { get; }

    public string FullName { get; }

    public string PositionName { get; }

    public string BusinessUnitName { get; }

    public int Sequence { get; }

    public DelegateeId? DelegateeId { get; }

    public AssigneeStatus Status { get; }

    public DateTimeOffset? ActionAt { get; }

    public string? Remark { get; }

    public SuUser User { get; }

    public SuDelegatee? Delegatee { get; }

    public UserId? SendToAcceptorId { get; }

    public SuUser? SendToAcceptor { get; }

    public IHasAssignee Clone();

    public IHasAssignee SetDelegatee(
        DelegateeId? delegateeId,
        SuDelegatee? delegatee = null);

    public IHasAssignee SetType(AssigneeType type);

    public IHasAssignee SetSequence(int sequence);

    public IHasAssignee SetUser(
        UserId userId,
        EmployeeCode employeeCode,
        string fullName,
        string positionName,
        string businessUnitName);

    public Unit Draft();

    public Unit Pending();

    public Unit Assigned();

    public Unit Reject(string? remark = default);

    public IHasAssignee SetRemark(string? remark);

    public IHasAssignee SetSendToAcceptorId(UserId? sendToAcceptorId);
}

public abstract class AssigneeInfoEntity<TKey> : AuditableEntity<TKey>, IHasAssignee
    where TKey : struct
{
    public AssigneeGroup Group { get; private set; }

    public AssigneeType Type { get; private set; }

    public UserId UserId { get; private set; }

    public EmployeeCode EmployeeCode { get; private set; }

    public string FullName { get; private set; }

    public string? Signature =>
        $"{this.User?.Employee?.FirstName ?? string.Empty} {this.User?.Employee?.LastName ?? string.Empty}".Trim();

    public string? SignatureDelegatee =>
       $"{this.Delegatee?.SuUser?.Employee?.FirstName ?? string.Empty} {this.Delegatee?.SuUser?.Employee?.LastName ?? string.Empty}".Trim();

    public string PositionName { get; private set; }

    public string BusinessUnitName { get; private set; }

    public int Sequence { get; private set; }

    public DelegateeId? DelegateeId { get; private set; }

    public AssigneeStatus Status { get; private set; }

    public DateTimeOffset? ActionAt { get; private set; }

    public string? Remark { get; private set; }

    public virtual SuUser User { get; init; }

    public virtual SuDelegatee? Delegatee { get; private set; }

    public UserId? SendToAcceptorId { get; private set; }

    public virtual SuUser? SendToAcceptor { get; private set; }

    public IHasAssignee Clone()
    {
        return (IHasAssignee)this.MemberwiseClone();
    }

    public IHasAssignee SetDelegatee(
        DelegateeId? delegateeId,
        SuDelegatee? delegatee = null)
    {
        this.DelegateeId = delegateeId;
        this.Delegatee = delegatee;

        return this;
    }

    public IHasAssignee SetGroup(AssigneeGroup group)
    {
        this.Group = group;

        return this;
    }

    public IHasAssignee SetType(AssigneeType type)
    {
        this.Type = type;

        return this;
    }

    public IHasAssignee SetSequence(int sequence)
    {
        if (sequence < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sequence), "Sequence must be a non-negative integer.");
        }

        this.Sequence = sequence;

        return this;
    }

    public IHasAssignee SetUser(
        UserId userId,
        EmployeeCode employeeCode,
        string fullName,
        string positionName,
        string businessUnitName)
    {
        this.UserId = userId;
        this.EmployeeCode = employeeCode;
        this.FullName = fullName;
        this.PositionName = positionName;
        this.BusinessUnitName = businessUnitName;

        return this;
    }

    public Unit Draft()
    {
        this.Status = AssigneeStatus.Draft;

        return Unit.Default;
    }

    public Unit Pending()
    {
        this.Status = AssigneeStatus.Pending;
        this.ActionAt = null;
        this.DelegateeId = null;
        this.Remark = null;

        return Unit.Default;
    }

    public Unit Assigned()
    {
        this.Status = AssigneeStatus.Assigned;
        this.ActionAt = DateTimeOffset.UtcNow;

        return Unit.Default;
    }

    public Unit Reject(string? remark = default)
    {
        this.Status = AssigneeStatus.Rejected;
        this.ActionAt = DateTimeOffset.UtcNow;
        this.Remark = remark;

        return Unit.Default;
    }

    public IHasAssignee SetRemark(string? remark)
    {
        this.Remark = remark;
        this.ActionAt = DateTimeOffset.UtcNow;

        return this;
    }

    public IHasAssignee SetSendToAcceptorId(UserId? sendToAcceptorId)
    {
        this.SendToAcceptorId = sendToAcceptorId;

        return this;
    }

    public IHasAssignee ResetAction()
    {
        this.Remark = null;
        this.ActionAt = null;

        return this;
    }
}