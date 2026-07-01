namespace GHB.DP2.Domain.Procurement.PpAppoint;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using LanguageExt;
using Vogen;

public enum AppointStatus
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
    /// ผูัมีอำนาจเห็นชอบ/อนุมัติ
    /// </summary>
    WaitingApproval,

    /// <summary>
    /// อนุมัติแล้ว
    /// </summary>
    Approved,

    /// <summary>
    /// ยกเลิก
    /// </summary>
    Cancelled,
}

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PpAppointId
{
    public static PpAppointId New() => From(Guid.CreateVersion7());
}

[ValueObject<string>(Conversions.EfCoreValueConverter)]
public partial struct PpAppointNumber
{
    public static PpAppointNumber New(ProcurementNumber procurementNumber)
    {
        if (string.IsNullOrWhiteSpace(procurementNumber.Value))
        {
            throw new ArgumentException("Procurement number cannot be null or empty.", nameof(procurementNumber));
        }

        // Procurement number format: "PYY00001"
        // Generate a new plan number in the format "PYY00001-0101"
        var number = $"{procurementNumber.Value}-0101";

        return From(number);
    }

    public PpAppointNumber Next()
    {
        if (string.IsNullOrWhiteSpace(this.Value))
        {
            throw new InvalidOperationException("Appoint number cannot be null or empty.");
        }

        // Assuming the appoint number is in the format "PYYXXXXX-01XX"
        if (this.Value.Length < 7 || !this.Value.StartsWith('P'))
        {
            throw new FormatException("Invalid appoint number format.");
        }

        // Running is the Two last digits after split by '-'
        var parts = this.Value.Split('-');

        if (parts.Length != 2 || parts[1].Length != 4)
        {
            throw new FormatException("Invalid appoint number format.");
        }

        var running = parts[1];

        if (!int.TryParse(running, out var number))
        {
            throw new FormatException("Invalid appoint number format.");
        }

        // Increment the number part
        number++;

        // Format the new appoint number
        var newNumber = $"{parts[0]}-{number:D4}";

        return From(newNumber);
    }
}

public partial class PpAppoint : AuditableEntity<PpAppointId>, IHasSoftDelete, IHasActivityInfo
{
    public override PpAppointId Id { get; init; }

    public PpAppointId? ReferenceId { get; init; }

    public ProcurementId ProcurementId { get; init; }

    public PpAppointNumber AppointNumber { get; init; }

    public DateTimeOffset MemorandumDate { get; private set; }

    public DateTimeOffset? DocumentDate { get; private set; }

    public string? MemorandumNumber { get; private set; }

    public string? Telephone { get; private set; }

    public string? Reason { get; private set; }

    public AppointStatus Status { get; private set; }

    public bool IsTorDraftCommittee { get; init; }

    public bool IsMedianPriceCommittee { get; init; }

    public bool IsChange { get; private set; }

    public bool IsCancel { get; private set; }

    public bool IsActive { get; private set; }

    public bool? IsMigration { get; init; }

    public string? CancelReason { get; private set; }

    public string? ChangeReason { get; private set; }

    public virtual Procurement Procurement { get; init; }

    public virtual IReadOnlyCollection<PpAppointAcceptors> Acceptors { get; private set; }

    public virtual IReadOnlyCollection<PpAppointTorDraftCommittee> TorDraftCommittees { get; private set; }

    public virtual IReadOnlyCollection<PpAppointTorDraftCommitteeDuties> TorDraftCommitteeDuties { get; private set; }

    public virtual IReadOnlyCollection<PpAppointMedianPriceCommittee> MedianPriceCommittees { get; private set; }

    public virtual IReadOnlyCollection<PpAppointMedianPriceCommitteeDuties> MedianPriceCommitteeDuties { get; private set; }

    public virtual IReadOnlyCollection<PpAppointDocumentHistory> DocumentHistories { get; private set; }

    public PpAppointDocumentHistory? LastedDraftDocument =>
        this.DocumentHistories
            .Where(dh => dh.StatusState == AppointStatus.Draft)
            .OrderVersions()
            .FirstOrDefault();

    public PpAppointDocumentHistory? LastedMaxDocument =>
        this.DocumentHistories
            .OrderVersions()
            .FirstOrDefault();

    public PpAppointDocumentHistory? LastedNotReplacedDocument =>
        this.DocumentHistories
            .Where(dh =>
                dh is
                {
                    StatusState: AppointStatus.WaitingApproval,
                    IsReplaced: false
                })
            .OrderVersions()
            .FirstOrDefault();

    public Unit AddDocumentHistory(
        FileId fileId,
        bool isReplaced = false)
    {
        var histories = this.DocumentHistories.ToHashSet();

        var lastedStatusState =
            histories.MaxBy(h => h.CreatedAt)?.StatusState;

        var incrementMajor =
            lastedStatusState != this.Status;

        var version =
            this.DocumentHistories
                .NextVersion(incrementMajor);

        histories.Add(PpAppointDocumentHistory.Create(
            histories.Any() ? this.Status : AppointStatus.Draft,
            version,
            fileId,
            isReplaced));

        this.DocumentHistories = histories;

        return unit;
    }

    public static PpAppoint Create(
        Procurement procurement,
        DateTimeOffset memorandumDate,
        string? memorandumNumber,
        string? telephone,
        string? reason)
    {
        if (procurement.ProcurementNumber == null)
        {
            throw new ArgumentException("Procurement number cannot be null.", nameof(procurement));
        }

        var newData = new PpAppoint
        {
            Id = PpAppointId.New(),
            ProcurementId = procurement.Id,
            AppointNumber = PpAppointNumber.New(procurement.ProcurementNumber!.Value),
            MemorandumDate = memorandumDate,
            MemorandumNumber = memorandumNumber,
            Telephone = telephone,
            Reason = reason,
            IsChange = false,
            IsCancel = false,
            IsActive = true,
            TorDraftCommittees = [],
            TorDraftCommitteeDuties = [],
            MedianPriceCommittees = [],
            MedianPriceCommitteeDuties = [],
            Acceptors = [],
            DocumentHistories = [],
        };

        newData.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Create,
            $"สร้างข้อมูล",
            newData.Status.ToString()));

        return newData;
    }

    public Unit Update(
        DateTimeOffset memorandumDate,
        string? memorandumNumber,
        string? telephone,
        string? reason,
        AppointStatus status,
        string? changeReason,
        string? cancelReason)
    {
        this.MemorandumDate = memorandumDate;
        this.MemorandumNumber = memorandumNumber;
        this.Telephone = telephone;
        this.Reason = reason;
        this.ChangeReason = changeReason;
        this.CancelReason = cancelReason;

        switch (status, this.Status)
        {
            case (AppointStatus.WaitingApproval, _):
                this.AddActivity(
                    new ActivityInfo(
                        ActivityLogActionTypeConstant.SendApprove,
                        $"ส่งเห็นชอบ/อนุมัติข้อมูลขอแต่งตั้งบุคคล/คกก. จัดทำขอบเขตของงาน/ราคากลาง",
                        status.ToString()));

                break;

            case (AppointStatus.Edit, AppointStatus.WaitingApproval):
                this.AddActivity(
                    new ActivityInfo(
                        ActivityLogActionTypeConstant.Recall,
                        $"เรียกคืนแก้ไขข้อมูลขอแต่งตั้งบุคคล/คกก. จัดทำขอบเขตของงาน/ราคากลาง",
                        nameof(AppointStatus.Edit)));

                break;

            case (AppointStatus.Rejected, AppointStatus.Rejected):
            case (AppointStatus.Draft, _):
            case (AppointStatus.Edit, AppointStatus.Edit):
                this.AddActivity(
                    new ActivityInfo(
                        ActivityLogActionTypeConstant.Update,
                        $"อัปเดตข้อมูล",
                        status.ToString()));

                break;
        }

        this.Status = status;

        return unit;
    }

    public PpAppoint Clone(bool isChange, bool isCancel, string reason)
    {
        var newId = PpAppointId.From(Guid.NewGuid());

        var wording = isChange ? "ขอเปลี่ยนแปลงข้อมูล" : "ขอยกเลิกข้อมูล";

        this.AddActivity(new ActivityInfo(
            isChange ? ActivityLogActionTypeConstant.RequestChange : ActivityLogActionTypeConstant.RequestCancel,
            $"{wording}",
            this.Status.ToString()));

        if (this.Procurement.ProcurementNumber == null)
        {
            throw new ArgumentException("Procurement number cannot be null.", nameof(this.Procurement));
        }

        var newAppointNumber =
            this.AppointNumber.Value.StartsWith($"{this.Procurement.ProcurementNumber}-01")
                ? this.AppointNumber.Next()
                : PpAppointNumber.New(this.Procurement.ProcurementNumber!.Value);

        var cloneData = new PpAppoint
        {
            Id = newId,
            ProcurementId = this.ProcurementId,
            AppointNumber = newAppointNumber,
            MemorandumDate = this.MemorandumDate,
            MemorandumNumber = this.MemorandumNumber,
            Telephone = this.Telephone,
            Reason = this.Reason,
            Status = AppointStatus.Draft,
            IsChange = isChange,
            IsCancel = isCancel,
            IsActive = true,
            Acceptors = this.Acceptors.Select(acceptor => acceptor.Clone(newId)).ToList(),
            MedianPriceCommittees = this.MedianPriceCommittees.Select(medCommittee => medCommittee.Clone(newId)).ToList(),
            MedianPriceCommitteeDuties = this.MedianPriceCommitteeDuties.Select(duty => duty.Clone(newId)).ToList(),
            TorDraftCommittees = this.TorDraftCommittees.Select(committee => committee.Clone(newId)).ToList(),
            TorDraftCommitteeDuties = this.TorDraftCommitteeDuties.Select(duty => duty.Clone(newId)).ToList(),
            ReferenceId = this.Id,
            DocumentHistories = [],
            CancelReason = isCancel ? reason : null,
            ChangeReason = isChange ? reason : null,
        };

        return cloneData;
    }

    public PpAppoint SetStatus(AppointStatus status)
    {
        this.Status = status;

        return this;
    }

    public PpAppoint SetDocumentDate(DateTimeOffset? documentDate)
    {
        this.DocumentDate = documentDate ?? DateTimeOffset.Now;

        return this;
    }

    public Unit SetApproved(string? remark)
    {
        this.Status = AppointStatus.Approved;

        var wording = this.ReferenceId != null ? "เปลี่ยนแปลงข้อมูล" : "เห็นชอบ/อนุมัติข้อมูล";

        this.AddActivity(
            new ActivityInfo(
                this.ReferenceId != null ? ActivityLogActionTypeConstant.Changed : ActivityLogActionTypeConstant.Approved,
                $"{wording}",
                nameof(AppointStatus.Approved),
                remark));

        return unit;
    }

    public Unit SetRejected(string? remark)
    {
        this.Status = AppointStatus.Rejected;

        this.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.Reject,
                $"ส่งกลับแก้ไขข้อมูล",
                nameof(AppointStatus.Rejected),
                remark));

        return unit;
    }

    public Unit SetCancelled()
    {
        this.Status = AppointStatus.Cancelled;
        this.Procurement.SetProcessType(ProcessType.Appoint);
        this.Procurement.SetStatus(ProcurementStatus.Cancelled);

        this.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.Cancelled,
                $"ขอยกเลิกข้อมูล",
                nameof(AppointStatus.Cancelled)));

        return unit;
    }

    public PpAppoint SetActive(bool isActive)
    {
        this.IsActive = isActive;

        return this;
    }

    public PpAppoint AddPpAppointAcceptor(PpAppointAcceptors ppAppointAcceptor)
    {
        if (ppAppointAcceptor == null)
        {
            throw new ArgumentNullException(nameof(ppAppointAcceptor), "Acceptor cannot be null.");
        }

        if (this.Acceptors.Contains(ppAppointAcceptor))
        {
            throw new InvalidOperationException("Acceptor already exists in the Appoint.");
        }

        var acceptors = this.Acceptors.ToHashSet();

        acceptors.Add(ppAppointAcceptor);

        this.Acceptors = acceptors;

        return this;
    }

    public PpAppoint AddPpAppointTorDraftCommittee(PpAppointTorDraftCommittee ppAppointTorDraftCommittee)
    {
        var torDraftCommittees = this.TorDraftCommittees.ToHashSet();

        torDraftCommittees.Add(ppAppointTorDraftCommittee);

        this.TorDraftCommittees = torDraftCommittees;

        return this;
    }

    public Unit RemoveTorDraftCommittee(PpAppointTorDraftCommitteeId torDraftCommitteeId)
    {
        var torDraftCommittees = this.TorDraftCommittees.ToHashSet();

        torDraftCommittees.RemoveWhere(w => w.Id == torDraftCommitteeId);

        this.TorDraftCommittees = torDraftCommittees;

        return unit;
    }

    public PpAppoint AddPpAppointTorDraftCommitteeDuties(PpAppointTorDraftCommitteeDuties ppAppointTorDraftCommitteeDuties)
    {
        var torDraftCommitteeDuties = this.TorDraftCommitteeDuties.ToHashSet();

        torDraftCommitteeDuties.Add(ppAppointTorDraftCommitteeDuties);

        this.TorDraftCommitteeDuties = torDraftCommitteeDuties;

        return this;
    }

    public PpAppoint AddPpAppointMedianPriceCommittee(PpAppointMedianPriceCommittee ppAppointMedianPriceCommittee)
    {
        var medianPriceCommittees = this.MedianPriceCommittees.ToHashSet();

        medianPriceCommittees.Add(ppAppointMedianPriceCommittee);

        this.MedianPriceCommittees = medianPriceCommittees;

        return this;
    }

    public Unit RemoveMedianPriceCommittee(PpAppointMedianPriceCommitteeId medianPriceCommitteeId)
    {
        var medianPriceCommittees = this.MedianPriceCommittees.ToHashSet();

        medianPriceCommittees.RemoveWhere(x => x.Id == medianPriceCommitteeId);

        this.MedianPriceCommittees = medianPriceCommittees;

        return unit;
    }

    public PpAppoint AddPpAppointMedianPriceCommitteeDuties(PpAppointMedianPriceCommitteeDuties ppAppointMedianPriceCommitteeDuties)
    {
        var medianPriceCommitteeDuties = this.MedianPriceCommitteeDuties.ToHashSet();

        medianPriceCommitteeDuties.Add(ppAppointMedianPriceCommitteeDuties);

        this.MedianPriceCommitteeDuties = medianPriceCommitteeDuties;

        return this;
    }

    public Unit RemoveAcceptorById(AcceptorId acceptorId)
    {
        var acceptors = this.Acceptors.ToHashSet();

        acceptors.RemoveWhere(w => w.Id == acceptorId);

        this.Acceptors = acceptors;

        return unit;
    }

    public PpAppoint SetChangeReason(string reason)
    {
        this.ChangeReason = reason;

        this.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.RequestChange,
            $"อัปเดตข้อมูล",
            this.Status.ToString(),
            reason));

        return this;
    }

    public PpAppoint SetCancelReason(string reason)
    {
        this.CancelReason = reason;

        this.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.RequestCancel,
            $"อัปเดตข้อมูล",
            this.Status.ToString(),
            reason));

        return this;
    }
}