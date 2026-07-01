namespace GHB.DP2.Domain.Procurement.PInvite;

using GHB.DP2.Domain.Common;
using LanguageExt;
using Vogen;

public enum PInviteStatus
{
    /// <summary>
    /// ปฏิเสธ
    /// </summary>
    Rejected,

    /// <summary>
    /// แบบร่าง
    /// </summary>
    Draft,

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
    ///  ไม่ได้เชิญ
    /// </summary>
    NotInvited,
}

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PInviteId
{
    public static PInviteId New() => From(Guid.CreateVersion7());
}

[ValueObject<string>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct InviteNumber
{
    public static InviteNumber New(ProcurementNumber procurementNumber)
    {
        if (string.IsNullOrWhiteSpace(procurementNumber.Value))
        {
            throw new ArgumentException("Procurement number cannot be null or empty.", nameof(procurementNumber));
        }

        var newNumber = $"{procurementNumber.Value}-0601";

        return From(newNumber);
    }

    public InviteNumber Next()
    {
        if (string.IsNullOrWhiteSpace(this.Value))
        {
            throw new InvalidOperationException("Current TorDraftNumber is null or empty.");
        }

        // Assuming the TOR draft number is the format "PYYXXXXX-06XX"
        if (this.Value.Length < 7 || !this.Value.StartsWith("P"))
        {
            throw new FormatException("Invalid TorDraftNumber format.");
        }

        // Running is the two last digits after splitting by '-'
        var parts = this.Value.Split('-');

        if (parts.Length != 2 || parts[1].Length != 4 || !int.TryParse(parts[1], out var running))
        {
            throw new FormatException("Invalid TorDraftNumber format.");
        }

        running++;

        var newNumber = $"{parts[0]}-{running:D4}";

        return From(newNumber);
    }
}

public partial class PInvite : AuditableEntity<PInviteId>, IHasSoftDelete, IHasActivityInfo
{
    public override PInviteId Id { get; init; }

    public ProcurementId ProcurementId { get; set; }

    public InviteNumber InviteNumber { get; init; }

    public bool IsInvite { get; private set; }

    public bool? IsMigration { get; init; }

    public DateTimeOffset? SubmitProposalStartDate { get; private set; }

    public DateTimeOffset? SubmitProposalEndDate { get; private set; }

    public DateTimeOffset? SubmitProposalStartTime { get; private set; }

    public DateTimeOffset? SubmitProposalEndTime { get; private set; }

    public DateTimeOffset? NeedToKnowWithinDate { get; private set; }

    public DateTimeOffset? ClarifyDetailViaDate { get; private set; }

    public DateTimeOffset? DocumentDate { get; private set; }

    public string? PhoneNumber { get; private set; }

    public PInviteStatus Status { get; private set; }

    public virtual Procurement Procurement { get; init; }

    public virtual IReadOnlyCollection<PInviteAcceptors> Acceptors { get; private set; }

    public virtual IReadOnlyCollection<PInvitedEntrepreneurs> InvitedEntrepreneurs { get; private set; }

    public PInvite SetSubmitProposal(
        DateTimeOffset? submitProposalStartDate,
        DateTimeOffset? submitProposalEndDate,
        DateTimeOffset? submitProposalStartTime,
        DateTimeOffset? submitProposalEndTime)
    {
        this.SubmitProposalStartDate = submitProposalStartDate;
        this.SubmitProposalEndDate = submitProposalEndDate;
        this.SubmitProposalStartTime = submitProposalStartTime;
        this.SubmitProposalEndTime = submitProposalEndTime;

        return this;
    }

    public static PInvite CreateInvite(
        Procurement procurement,
        bool isInvite,
        DateTimeOffset? needToKnowWithinDate,
        DateTimeOffset? clarifyDetailViaDate,
        string? phoneNumber,
        PInviteStatus status)
    {
        if (procurement.ProcurementNumber == null)
        {
            throw new ArgumentNullException(nameof(procurement), "Procurement cannot be null.");
        }

        var newData = new PInvite
        {
            Id = PInviteId.New(),
            ProcurementId = procurement.Id,
            InviteNumber = InviteNumber.New(procurement.ProcurementNumber!.Value),
            IsInvite = isInvite,
            NeedToKnowWithinDate = needToKnowWithinDate,
            ClarifyDetailViaDate = clarifyDetailViaDate,
            PhoneNumber = phoneNumber,
            Status = status,
        };

        newData.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Create,
            $"สร้างข้อมูลใหม่ {nameof(PInvite)}",
            newData.Status.ToString()));

        return newData;
    }

    public PInvite Update(
        ProcurementId procurementId,
        bool isInvite,
        DateTimeOffset? needToKnowWithinDate,
        DateTimeOffset? clarifyDetailViaDate,
        string? phoneNumber,
        PInviteStatus status)
    {
        this.ProcurementId = procurementId;
        this.IsInvite = isInvite;
        this.NeedToKnowWithinDate = needToKnowWithinDate;
        this.ClarifyDetailViaDate = clarifyDetailViaDate;
        this.PhoneNumber = phoneNumber;

        switch (status, this.Status)
        {
            case (PInviteStatus.Edit, _):
                this.AddActivity(new ActivityInfo(
                    ActivityLogActionTypeConstant.Recall,
                    $"ขอเปลี่ยนแปลงข้อมูล",
                    status.ToString()));

                break;

            case (PInviteStatus.WaitingApproval, _):
                if (this.Procurement.SupplyMethodCode == "SMethod002" && this.Procurement.Budget > 100000)
                {
                    this.AddActivity(new ActivityInfo(
                        "ส่งหัวหน้าส่วนเห็นชอบ/อนุมัติ",
                        $"ส่งผู้มีอำนาจเห็นชอบ/อนุมัติ",
                        status.ToString()));
                }
                else
                {
                    this.AddActivity(new ActivityInfo(
                        ActivityLogActionTypeConstant.SendCommitteeApprove,
                        $"ส่งผู้มีอำนาจเห็นชอบ/อนุมัติ",
                        status.ToString()));
                }

                break;

            default:
                this.AddActivity(new ActivityInfo(
                    ActivityLogActionTypeConstant.Update,
                    $"อัพเดตข้อมูล",
                    status.ToString()));

                break;
        }

        this.Status = status;

        return this;
    }

    public bool HasMajorityRejection()
    {
        if (this.Status != PInviteStatus.WaitingApproval)
        {
            return false;
        }

        var committeesAble =
            this.Acceptors
                .Where(a => a is
                {
                    Type: AcceptorType.ProcurementCommittee,
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

    public PInvite AddPInviteAcceptor(PInviteAcceptors acceptor)
    {
        var acceptors = this.Acceptors?.ToList() ?? new List<PInviteAcceptors>();
        acceptors.Add(acceptor);
        this.Acceptors = acceptors;

        return this;
    }

    public PInvite UpdatePInviteAcceptor(PInviteAcceptors acceptor)
    {
        var acceptors = this.Acceptors?.ToList() ?? new List<PInviteAcceptors>();
        var idx = acceptors.FindIndex(a => a.Id == acceptor.Id);

        if (idx >= 0)
        {
            acceptors[idx] = acceptor;
            this.Acceptors = acceptors;
        }

        return this;
    }

    public PInvite SetDocumentDate(DateTimeOffset? date = null)
    {
        this.DocumentDate = date ?? DateTimeOffset.Now;

        return this;
    }

    public PInvite RemovePInviteAcceptor(PInviteAcceptors acceptor)
    {
        var acceptors = this.Acceptors?.ToList() ?? new List<PInviteAcceptors>();
        acceptors.RemoveAll(a => a.Id == acceptor.Id);
        this.Acceptors = acceptors;

        return this;
    }

    public PInvite UpdatePInviteEntrepreneurs(PInvitedEntrepreneurs invitedEntrepreneurs)
    {
        var invitedEntrepreneursList = this.InvitedEntrepreneurs?.ToList() ?? new List<PInvitedEntrepreneurs>();
        var idx = invitedEntrepreneursList.FindIndex(a => a.Id == invitedEntrepreneurs.Id);

        if (idx >= 0)
        {
            invitedEntrepreneursList[idx] = invitedEntrepreneurs;
            this.InvitedEntrepreneurs = invitedEntrepreneursList;
        }

        return this;
    }

    public PInvite RemovePInviteEntrepreneurs(PInvitedEntrepreneursId invitedEntrepreneursId)
    {
        var entrepreneursList = this.InvitedEntrepreneurs?.ToList() ?? new List<PInvitedEntrepreneurs>();
        entrepreneursList.RemoveAll(a => a.Id == invitedEntrepreneursId);
        this.InvitedEntrepreneurs = entrepreneursList;

        return this;
    }

    public PInvite SetApproved(string? remark)
    {
        if (this.Procurement.SupplyMethodCode == "SMethod002" && this.Procurement.Budget > 100000)
        {
            this.AddActivity(new ActivityInfo(
                "หัวหน้าส่วนเห็นชอบ/อนุมัติ",
                $"ส่งผู้มีอำนาจเห็นชอบ/อนุมัติ",
                this.Status.ToString(),
                remark));
        }
        else
        {
            this.AddActivity(new ActivityInfo(
                ActivityLogActionTypeConstant.Approved,
                $"ส่งผู้มีอำนาจเห็นชอบ/อนุมัติ",
                PInviteStatus.Approved.ToString(),
                remark));
        }

        this.Status = PInviteStatus.Approved;
        this.Procurement.SetProcessType(ProcessType.PurchaseOrder);

        return this;
    }

    public PInvite SetRejected(string? remark, bool isSixtyOver100K)
    {
        this.AddActivity(new ActivityInfo(
            isSixtyOver100K ? ActivityLogActionTypeConstant.HeadReject : ActivityLogActionTypeConstant.CommitteeReject,
            $"ส่งกลับแก้ไข",
            PInviteStatus.Rejected.ToString(),
            remark));

        this.Status = PInviteStatus.Rejected;

        return this;
    }

    public PInvite SetNotInvited()
    {
        this.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Approved,
            $"ไม่ได้เชิญและดำเนินการต่อ",
            PInviteStatus.NotInvited.ToString(),
            string.Empty));

        this.Status = PInviteStatus.NotInvited;
        this.Procurement.SetProcessType(ProcessType.PurchaseOrder);

        return this;
    }
}