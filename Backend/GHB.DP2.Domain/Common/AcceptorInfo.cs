namespace GHB.DP2.Domain.Common;

using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using LanguageExt;
using Vogen;

public enum AcceptorType
{
    /// <summary>
    /// ส่วนสายงานเห็นชอบ
    /// </summary>
    DepartmentDirectorAgree,

    /// <summary>
    /// คณะกรรมการที่จัดทำหรือร่าง TOR
    /// </summary>
    TorDraftCommittee,

    /// <summary>
    /// คณะกรรมการที่จัดทำหรือร่างราคากลาง
    /// </summary>
    MedianPriceCommittee,

    /// <summary>
    /// ผู้จัดซื้อจัดจ้าง/คณะกรรมการจัดซื้อจัดจ้าง
    /// </summary>
    ProcurementCommittee,

    /// <summary>
    /// ผู้จัดซื้อจัดจ้าง/คณะกรรมการจัดซื้อจัดจ้าง pp005
    /// </summary>
    Jp005Committee,

    /// <summary>
    /// คณะกรรมการจัดเช่า
    /// </summary>
    RentCommittee,

    /// <summary>
    /// ส่วนผู้มีอำนาจเห็นชอบ
    /// </summary>
    Approver,

    /// <summary>
    /// คณะกรรมการตรวจรับพัสดุ
    /// </summary>
    AcceptanceCommittee,

    /// <summary>
    /// คณะกรรมการตรวจรับพัสดุ
    /// </summary>
    InspectionCommittee,

    /// <summary>
    /// ผู้ลงนาม
    /// </summary>
    AcceptorSign,

    Accounting,

    /// <summary>
    /// บัญชีเห็นชอบ/อนุมัติ
    /// </summary>
    AccountingApprover,

    AccountingConfirmer,

    /// <summary>
    /// ผู้ตรวจสอบเอกสารบันทึกต่อท้าย
    /// </summary>
    Reviewer,

    /// <summary>
    /// บัญชีดำเนินการ
    /// </summary>
    AccountingOperator,
}

public enum AcceptorStatus
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
    /// อนุมัติแล้ว
    /// </summary>
    Approved,

    /// <summary>
    /// ปฏิเสธ
    /// </summary>
    Rejected,

    /// <summary>
    /// ไม่สามารถปฎิบัติหน้าที่ได้
    /// </summary>
    UnableToPerformDuties,
}

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct AcceptorId
{
    public static AcceptorId New() => From(Guid.CreateVersion7());
}

public interface IHasAcceptor
{
    public AcceptorId Id { get; }

    public AcceptorType Type { get; }

    public UserId UserId { get; }

    public EmployeeCode EmployeeCode { get; }

    public string FullName { get; }

    public string BusinessUnitName { get; }

    public string PositionName { get; }

    public int Sequence { get; }

    public DelegateeId? DelegateeId { get; }

    public AcceptorStatus Status { get; }

    public DateTimeOffset? ActionAt { get; }

    public string? Remark { get; }

    public bool IsActive { get; }

    public bool IsCurrent { get; }

    public UserId? SendToAcceptorId { get; }

    public SuUser? SendToAcceptor { get; }

    public SuUser User { get; }

    public SuDelegatee? Delegatee { get; }

    public IHasAcceptor Clone();

    public IHasAcceptor SetActive();

    public IHasAcceptor SetDelegatee(
        DelegateeId? delegateeId,
        SuDelegatee? delegatee = null);

    public IHasAcceptor SetType(AcceptorType type);

    public IHasAcceptor SetSequence(int sequence);

    public IHasAcceptor SetStatus(AcceptorStatus status);

    public IHasAcceptor SetActionAt(DateTimeOffset? actionAt);

    public IHasAcceptor SetRemark(string? remark);

    public IHasAcceptor SetUser(
        UserId userId,
        EmployeeCode employeeCode,
        string fullName,
        string positionName,
        string businessUnitName);

    public IHasAcceptor SetCurrent(bool isCurrent = true); // added

    public IHasAcceptor SetSendToAcceptorId(UserId? sendToAcceptorId);

    public Unit Draft();

    public Unit Pending();

    public Unit Approve(string? remark = default);

    public Unit Reject(string? remark = default);

    public Unit UnableToPerformDuties(string? remark = default);
}

public abstract class AcceptorInfoEntity : AuditableEntity<AcceptorId>, IHasAcceptor
{
    public AcceptorType Type { get; private set; }

    public UserId UserId { get; private set; }

    public EmployeeCode EmployeeCode { get; private set; }

    public string FullName { get; private set; }

    public string? Signature => $"{this.User?.Employee.FirstName} {this.User?.Employee.LastName}";

    public string? SignatureDelegatee =>
      $"{this.Delegatee?.SuUser?.Employee?.FirstName ?? string.Empty} {this.Delegatee?.SuUser?.Employee?.LastName ?? string.Empty}".Trim();

    public string BusinessUnitName { get; private set; }

    public string PositionName { get; private set; }

    public int Sequence { get; private set; }

    public DelegateeId? DelegateeId { get; private set; }

    public AcceptorStatus Status { get; protected set; }

    public DateTimeOffset? ActionAt { get; private set; }

    public string? Remark { get; private set; }

    public bool IsActive { get; private set; }

    public bool IsCurrent { get; private set; }

    public UserId? SendToAcceptorId { get; private set; }

    public virtual SuUser? SendToAcceptor { get; private set; }

    public virtual SuUser User { get; init; }

    public virtual SuDelegatee? Delegatee { get; private set; }

    public IHasAcceptor Clone()
    {
        return (IHasAcceptor)this.MemberwiseClone();
    }

    public IHasAcceptor SetActive()
    {
        this.IsActive = true;

        return this;
    }

    public IHasAcceptor SetDelegatee(
        DelegateeId? delegateeId,
        SuDelegatee? delegatee = null)
    {
        this.DelegateeId = delegateeId;
        this.Delegatee = delegatee;

        return this;
    }

    public IHasAcceptor SetType(AcceptorType type)
    {
        if (!Enum.IsDefined(typeof(AcceptorType), type))
        {
            throw new ArgumentOutOfRangeException(nameof(type), "Invalid acceptor type.");
        }

        this.Type = type;

        return this;
    }

    public IHasAcceptor SetSequence(int sequence)
    {
        if (sequence < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sequence), "Sequence must be a non-negative integer.");
        }

        this.Sequence = sequence;

        return this;
    }

    public IHasAcceptor SetUser(
        UserId userId,
        EmployeeCode employeeCode,
        string fullName,
        string positionName,
        string businessUnitName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException("Full name cannot be empty.", nameof(fullName));
        }

        this.UserId = userId;
        this.EmployeeCode = employeeCode;
        this.FullName = fullName;
        this.PositionName = positionName;
        this.BusinessUnitName = businessUnitName;

        return this;
    }

    public IHasAcceptor SetCurrent(bool isCurrent = true)
    {
        this.IsCurrent = isCurrent;

        return this;
    }

    public Unit Draft()
    {
        this.Status = AcceptorStatus.Draft;

        return Unit.Default;
    }

    public Unit Pending()
    {
        this.Status = AcceptorStatus.Pending;
        this.ActionAt = null;
        this.Remark = null;
        this.DelegateeId = null;

        return Unit.Default;
    }

    public Unit Approve(string? remark = null)
    {
        if (this.Status != AcceptorStatus.Pending
            && this.Status != AcceptorStatus.Draft)
        {
            throw new InvalidOperationException("Cannot approve when not in Pending or Draft status.");
        }

        this.Status = AcceptorStatus.Approved;
        this.ActionAt = DateTimeOffset.UtcNow;
        this.Remark = remark;

        return Unit.Default;
    }

    public Unit Reject(string? remark = null)
    {
        this.IsCurrent = false;
        this.Status = AcceptorStatus.Rejected;
        this.ActionAt = DateTimeOffset.UtcNow;
        this.Remark = remark;

        return Unit.Default;
    }

    public Unit UnableToPerformDuties(string? remark = null)
    {
        this.IsCurrent = false;
        this.Status = AcceptorStatus.UnableToPerformDuties;
        this.ActionAt = DateTimeOffset.UtcNow;
        this.Remark = remark;

        return Unit.Default;
    }

    public IHasAcceptor SetStatus(AcceptorStatus status)
    {
        this.Status = status;

        return this;
    }

    public IHasAcceptor SetActionAt(DateTimeOffset? actionAt)
    {
        this.ActionAt = actionAt;

        return this;
    }

    public IHasAcceptor SetRemark(string? remark)
    {
        this.Remark = remark;

        return this;
    }

    public IHasAcceptor SetIsCurrent(bool isCurrent)
    {
        this.IsCurrent = isCurrent;

        return this;
    }

    public IHasAcceptor SetSendToAcceptorId(UserId? sendToAcceptorId)
    {
        this.SendToAcceptorId = sendToAcceptorId;

        return this;
    }
}