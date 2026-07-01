namespace GHB.DP2.Domain.ContractAmendment.ContractAmendment.CamContractAmendmentPoSap;

using GHB.DP2.Domain.Common;
using Vogen;

public enum CamContractAmendmentPoSapStatus
{
    /// <summary>
    /// แบบร่าง
    /// </summary>
    Draft,

    /// <summary>
    /// ส่งกลับแก้ไข
    /// </summary>
    Edit,

    /// <summary>
    /// รอเห็นชอบโดย ผู้มีอำนาจเห็นชอบ/อนุมัติ
    /// </summary>
    WaitingApproval,

    /// <summary>
    /// อนุมัติแล้ว
    /// </summary>
    Approved,

    /// <summary>
    /// ปฏิเสธ
    /// </summary>
    Rejected,
}

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct CamContractAmendmentPoSapId
{
    public static CamContractAmendmentPoSapId New() => From(Guid.CreateVersion7());
}

public partial class CamContractAmendmentPoSap : AuditableEntity<CamContractAmendmentPoSapId>, IHasActivityInfo
{
    public override CamContractAmendmentPoSapId Id { get; init; }

    public CamContractAmendmentId CamContractAmendmentId { get; init; }

    public string? PoSapNumber { get; private set; }

    public CamContractAmendmentPoSapStatus Status { get; private set; }

    public virtual CamContractAmendment CamContractAmendment { get; init; }

    public virtual IReadOnlyCollection<CamContractAmendmentPoSapAcceptor> Acceptors { get; private set; }

    public CamContractAmendmentPoSap SetPoSapNumber(string? poSapNumber)
    {
        this.PoSapNumber = poSapNumber;

        return this;
    }

    public CamContractAmendmentPoSap SetDraft()
    {
        this.Status = CamContractAmendmentPoSapStatus.Draft;

        this.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Create,
            "บันทึกแบบร่าง",
            nameof(CamContractAmendmentPoSapStatus.Draft)));

        return this;
    }

    public CamContractAmendmentPoSap SetEdit()
    {
        this.Status = CamContractAmendmentPoSapStatus.Edit;
        this.Acceptors.Iter(r => r.Draft());

        this.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Recall,
            "ขอเปลี่ยนแปลงข้อมูล",
            nameof(CamContractAmendmentPoSapStatus.Edit)));

        return this;
    }

    public CamContractAmendmentPoSap SetWaitingApproval()
    {
        this.Status = CamContractAmendmentPoSapStatus.WaitingApproval;

        this.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.SendUnitApprove,
            "ผู้มีอำนาจเห็นชอบ/อนุมัติ",
            nameof(CamContractAmendmentPoSapStatus.WaitingApproval)));

        if (!this.Acceptors.Any())
        {
            throw new InvalidOperationException("No acceptors defined");
        }

        var acceptors = this.Acceptors.ToHashSet();

        foreach (var acceptor in acceptors)
        {
            acceptor.Pending();
        }

        _ =
            acceptors
                .Where(a =>
                    a is { IsActive: true, Status: AcceptorStatus.Pending })
                .OrderBy(a => a.Sequence)
                .Take(1)
                .Map(a => a.SetCurrent())
                .ToHashSet();

        this.Acceptors = acceptors;

        return this;
    }

    public CamContractAmendmentPoSap SetApproved()
    {
        this.Status = CamContractAmendmentPoSapStatus.Approved;
        this.CamContractAmendment.SetStatus(CamContractAmendmentStatus.Completed);

        this.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Approved,
            "ผู้มีอำนาจเห็นชอบ/อนุมัติ",
            nameof(CamContractAmendmentPoSapStatus.Approved)));

        return this;
    }

    public CamContractAmendmentPoSap SetRejected()
    {
        this.Status = CamContractAmendmentPoSapStatus.Rejected;

        this.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Reject,
            "ผู้มีอำนาจเห็นชอบ/อนุมัติ",
            nameof(CamContractAmendmentPoSapStatus.Rejected)));

        var acceptors = this.Acceptors.ToHashSet();

        foreach (var acceptor in acceptors)
        {
            if (acceptor.Status == AcceptorStatus.Pending)
            {
                acceptor.SetCurrent(false);
            }
        }

        this.Acceptors = acceptors;

        return this;
    }

    public CamContractAmendmentPoSap AddAcceptor(CamContractAmendmentPoSapAcceptor acceptor)
    {
        var acceptors = this.Acceptors.ToHashSet();

        acceptors.Add(acceptor);

        this.Acceptors = acceptors;

        return this;
    }

    public CamContractAmendmentPoSap RemoveAcceptor(CamContractAmendmentPoSapAcceptor acceptor)
    {
        var acceptors = this.Acceptors.ToHashSet();

        if (!acceptors.Remove(acceptor))
        {
            throw new ArgumentException("Acceptor not found");
        }

        this.Acceptors = acceptors;

        return this;
    }

    public static CamContractAmendmentPoSap Create(
        CamContractAmendmentId camContractAmendmentId)
    {
        return new CamContractAmendmentPoSap
        {
            Id = CamContractAmendmentPoSapId.New(),
            CamContractAmendmentId = camContractAmendmentId,
            Status = CamContractAmendmentPoSapStatus.Draft,
            Acceptors = [],
        };
    }
}