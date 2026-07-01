namespace GHB.DP2.Domain.ContractAmendment.ContractAmendment.CamContractAmendmentWaiveOrReducePenalty;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using LanguageExt;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct WaiveOrReducePenaltyId
{
    public static WaiveOrReducePenaltyId New() => From(Guid.CreateVersion7());
}

public enum CamContractAmendmentWaiveOrReducePenaltyStatus
{
    /// <summary>
    /// แบบร่าง
    /// </summary>
    Draft,

    Edit,

    WaitingCommitteeApproval,

    WaitingAssigned,

    WaitingComment,

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

public partial class CamContractAmendmentWaiveOrReducePenalty : AuditableEntity<WaiveOrReducePenaltyId>, IHasActivityInfo
{
    public override WaiveOrReducePenaltyId Id { get; init; }

    public bool WaiveAll { get; private set; }

    public CamContractAmendmentId CamContractAmendmentId { get; init; }

    public ParameterCode? PenaltyTypeCode { get; private set; }

    public decimal? Rate { get; private set; }

    public decimal? Amount { get; private set; }

    public ParameterCode? RateTypeCode { get; private set; }

    public CamContractAmendmentWaiveOrReducePenaltyStatus Status { get; private set; }

    public virtual CamContractAmendment CamContractAmendment { get; init; }

    public virtual SuParameter? PenaltyType { get; init; }

    public virtual SuParameter? RateType { get; init; }

    public virtual IReadOnlyCollection<CamContractAmendmentWaiveOrReducePenaltyAcceptor> Acceptors { get; private set; }

    public virtual IReadOnlyCollection<CamContractAmendmentWaiveOrReducePenaltyAssignee> Assignees { get; private set; }

    public virtual IReadOnlyCollection<CamContractAmendmentWaiveOrReducePenaltyDocumentHistory> DocumentHistories { get; private set; }

    public CamContractAmendmentWaiveOrReducePenaltyDocumentHistory? LastedWaiveOrReducePenaltyDocument =>
        this.DocumentHistories
            .Where(d => d.DocumentType == WaiveOrReducePenaltyDocumentType.WaiveOrReducePenalty)
            .OrderVersions()
            .FirstOrDefault();

    public CamContractAmendmentWaiveOrReducePenaltyDocumentHistory? LastedApprovedRequestDocument =>
        this.DocumentHistories
            .Where(d => d.DocumentType == WaiveOrReducePenaltyDocumentType.Approved)
            .OrderVersions()
            .FirstOrDefault();

    public CamContractAmendmentWaiveOrReducePenaltyDocumentHistory? LastedDraftWaiveOrReducePenaltyDocument =>
        this.DocumentHistories
            .Where(d => d.DocumentType == WaiveOrReducePenaltyDocumentType.WaiveOrReducePenalty)
            .Where(d => d.StatusState == CamContractAmendmentWaiveOrReducePenaltyStatus.Draft)
            .OrderVersions()
            .FirstOrDefault();

    public CamContractAmendmentWaiveOrReducePenaltyDocumentHistory? LastedDraftApprovedRequestDocument =>
        this.DocumentHistories
            .Where(d => d.DocumentType == WaiveOrReducePenaltyDocumentType.Approved)
            .Where(d => d.StatusState == CamContractAmendmentWaiveOrReducePenaltyStatus.Draft)
            .OrderVersions()
            .FirstOrDefault();

    public CamContractAmendmentWaiveOrReducePenaltyDocumentHistory? LastedWaitingCommitteeApprovalWaiveOrReducePenaltyDocument =>
        this.DocumentHistories
            .Where(d => d.DocumentType == WaiveOrReducePenaltyDocumentType.WaiveOrReducePenalty)
            .Where(d => d.StatusState == CamContractAmendmentWaiveOrReducePenaltyStatus.WaitingCommitteeApproval)
            .OrderVersions()
            .FirstOrDefault();

    public CamContractAmendmentWaiveOrReducePenaltyDocumentHistory? LastedWaitingCommitteeApprovalApprovedRequestDocument =>
        this.DocumentHistories
            .Where(d => d.DocumentType == WaiveOrReducePenaltyDocumentType.Approved)
            .Where(d => d.StatusState == CamContractAmendmentWaiveOrReducePenaltyStatus.WaitingCommitteeApproval)
            .OrderVersions()
            .FirstOrDefault();

    public Unit AddDocumentHistory(
        WaiveOrReducePenaltyDocumentType documentType,
        FileId fileId,
        bool isReplace,
        bool incrementMajor = false)
    {
        var histories = this.DocumentHistories
                            .ToHashSet();

        var existingStatus =
            histories
                .Any(p => p.StatusState == this.Status && p.DocumentType == documentType);

        var version = this.DocumentHistories
                          .Where(p => p.DocumentType == documentType)
                          .NextVersion(incrementMajor || !existingStatus);

        histories.Add(
            CamContractAmendmentWaiveOrReducePenaltyDocumentHistory.Create(
                documentType,
                this.Status,
                version,
                fileId,
                isReplace));

        this.DocumentHistories = histories;

        return unit;
    }

    public CamContractAmendmentWaiveOrReducePenalty SetPenaltyNew(
        ParameterCode? penaltyTypeCode,
        decimal? rate,
        decimal? amount,
        ParameterCode? rateTypeCode)
    {
        this.PenaltyTypeCode = penaltyTypeCode;
        this.Rate = rate;
        this.Amount = amount;
        this.RateTypeCode = rateTypeCode;

        return this;
    }

    public CamContractAmendmentWaiveOrReducePenalty SetWaiveAll(bool waiveAll)
    {
        this.WaiveAll = waiveAll;

        return this;
    }

    public CamContractAmendmentWaiveOrReducePenalty SetStatus(
        CamContractAmendmentWaiveOrReducePenaltyStatus status)
    {
        this.Status = status;

        return this;
    }

    public CamContractAmendmentWaiveOrReducePenalty SetEdit()
    {
        this.Status = CamContractAmendmentWaiveOrReducePenaltyStatus.Edit;

        this.Acceptors.Where(x => !x.IsUnableToPerformDuties)
            .Iter(a => a.Draft());

        return this;
    }

    public CamContractAmendmentWaiveOrReducePenalty SetWaitingCommitteeApproval()
    {
        this.Assignees
            .Iter(a => a.Pending());

        this.Status = CamContractAmendmentWaiveOrReducePenaltyStatus.WaitingCommitteeApproval;

        this.Acceptors
            .Where(p => p.Type != AcceptorType.AcceptanceCommittee)
            .Iter(a => a.Draft());

        this.Acceptors
            .Where(p => p.Type == AcceptorType.AcceptanceCommittee && !p.IsUnableToPerformDuties)
            .Iter(a => a.Pending());

        return this;
    }

    public CamContractAmendmentWaiveOrReducePenalty SetApproval()
    {
        this.Status = CamContractAmendmentWaiveOrReducePenaltyStatus.Approved;
        this.CamContractAmendment.SetStatus(CamContractAmendmentStatus.Completed);

        return this;
    }

    public CamContractAmendmentWaiveOrReducePenalty SetWaitingComment()
    {
        this.Status = CamContractAmendmentWaiveOrReducePenaltyStatus.WaitingComment;

        _ = this.Assignees
                .Where(p => p.Type == AssigneeType.Director)
                .Iter(p => p.Assigned());

        return this;
    }

    public CamContractAmendmentWaiveOrReducePenalty SetWaitingApproval()
    {
        this.Status = CamContractAmendmentWaiveOrReducePenaltyStatus.WaitingApproval;

        var approvers = this.Acceptors
                            .Where(p => p.Type == AcceptorType.Approver)
                            .OrderBy(a => a.Sequence)
                            .ToList();

        approvers.Iter(a =>
        {
            a.SetCurrent(false).Pending();
        });

        var firstPending = approvers.FirstOrDefault(a => a.Status == AcceptorStatus.Pending);

        if (firstPending != null)
        {
            firstPending.SetCurrent();
        }

        return this;
    }

    public CamContractAmendmentWaiveOrReducePenalty SetWaitingAssigned()
    {
        this.Status = CamContractAmendmentWaiveOrReducePenaltyStatus.WaitingAssigned;
        this.Assignees.Iter(r => r.Pending());

        return this;
    }

    public bool HasMajorityRejection()
    {
        if (this.Status != CamContractAmendmentWaiveOrReducePenaltyStatus.WaitingCommitteeApproval)
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

    public CamContractAmendmentWaiveOrReducePenalty SetRejected(string? remark)
    {
        this.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Reject,
            $"ส่งกลับแก้ไข",
            nameof(CamContractAmendmentWaiveOrReducePenaltyStatus.Rejected),
            remark));

        this.Status = CamContractAmendmentWaiveOrReducePenaltyStatus.Rejected;

        return this;
    }

    public CamContractAmendmentWaiveOrReducePenalty AddAcceptor(
        CamContractAmendmentWaiveOrReducePenaltyAcceptor acceptor)
    {
        var acceptors = this.Acceptors.ToList();

        acceptors.Add(acceptor);

        this.Acceptors = acceptors;

        return this;
    }

    public CamContractAmendmentWaiveOrReducePenalty RemoveAcceptor(
        CamContractAmendmentWaiveOrReducePenaltyAcceptor acceptor)
    {
        var acceptors = this.Acceptors.ToList();

        acceptors.Remove(acceptor);

        this.Acceptors = acceptors;

        return this;
    }

    public CamContractAmendmentWaiveOrReducePenalty AddAssignee(
        CamContractAmendmentWaiveOrReducePenaltyAssignee assignee)
    {
        var assignees = this.Assignees.ToHashSet();

        assignees.Add(assignee);

        this.Assignees = assignees;

        return this;
    }

    public CamContractAmendmentWaiveOrReducePenalty RemoveAssignee(
        CamContractAmendmentWaiveOrReducePenaltyAssignee assignee)
    {
        var assignees = this.Assignees.ToHashSet();

        assignees.Remove(assignee);

        this.Assignees = assignees;

        return this;
    }

    public static CamContractAmendmentWaiveOrReducePenalty Create(
        CamContractAmendmentId contractAmendmentId,
        bool waiveAll)
    {
        var entity = new CamContractAmendmentWaiveOrReducePenalty
        {
            Id = WaiveOrReducePenaltyId.New(),
            CamContractAmendmentId = contractAmendmentId,
            WaiveAll = waiveAll,
            Status = CamContractAmendmentWaiveOrReducePenaltyStatus.Draft,
            Acceptors = new List<CamContractAmendmentWaiveOrReducePenaltyAcceptor>(),
            Assignees = new List<CamContractAmendmentWaiveOrReducePenaltyAssignee>(),
            DocumentHistories = new List<CamContractAmendmentWaiveOrReducePenaltyDocumentHistory>(),
        };

        entity.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.Create,
                $"สร้างคำขอแก้ไขสัญญา (ยกเว้นหรือลดค่าปรับ)",
                entity.Status.ToString()));

        return entity;
    }
}