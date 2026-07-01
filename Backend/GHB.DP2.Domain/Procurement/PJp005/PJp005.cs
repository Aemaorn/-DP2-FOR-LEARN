namespace GHB.DP2.Domain.Procurement.PJp005;

using System.ComponentModel.DataAnnotations;
using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.PpPurchaseRequisition;
using LanguageExt;
using Vogen;

public enum PJp005Status
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
    /// รอการอนุมัติจากผู้มีอำนาจ
    /// </summary>
    WaitingApproval,

    /// <summary>
    /// อนุมัติแล้ว
    /// </summary>
    Approved,

    /// <summary>
    /// ยกเลิกแล้ว
    /// </summary>
    Cancelled,
}

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PJp005Id
{
    public static PJp005Id New() => From(Guid.CreateVersion7());
}

[ValueObject<string>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PJp005Number
{
    public static PJp005Number New(ProcurementNumber procurementNumber)
    {
        if (string.IsNullOrWhiteSpace(procurementNumber.Value))
        {
            throw new ArgumentException("Procurement number cannot be null or empty.", nameof(procurementNumber));
        }

        var newNumber = $"{procurementNumber.Value}-0501";

        return From(newNumber);
    }

    public PJp005Number Next()
    {
        if (string.IsNullOrWhiteSpace(this.Value))
        {
            throw new InvalidOperationException("Current TorDraftNumber is null or empty.");
        }

        // Assuming the TOR draft number is the format "PYYXXXXX-05XX"
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

public partial class PJp005 : AuditableEntity<PJp005Id>, IHasSoftDelete, IHasActivityInfo
{
    public override PJp005Id Id { get; init; }

    public PJp005Number PJp005Number { get; init; }

    public ProcurementId ProcurementId { get; init; }

    public PpPurchaseRequisitionId PpPurchaseRequisitionId { get; init; }

    public int EvaluationDueDate { get; private set; }

    [MaxLength(50)]
    public string EvaluationPeriodTypeCode { get; private set; }

    [MaxLength(50)]
    public string EvaluationPeriodConditionCode { get; private set; }

    [MaxLength(100)]
    public string? EgpProjectNumber { get; private set; }

    public string? JorPorNumber { get; private set; }

    public PJp005Status Status { get; private set; }

    public bool IsActive { get; private set; }

    public bool? IsMigration { get; init; }

    public DateTimeOffset? DocumentDate { get; private set; }

    public virtual Procurement Procurement { get; init; }

    public virtual PpPurchaseRequisition PpPurchaseRequisition { get; init; }

    public virtual IReadOnlyCollection<PJp005Committee> Committees { get; private set; }

    public virtual IReadOnlyCollection<PJp005ProcurementSuppliesDivision> ProcurementSuppliesDivisions { get; private set; }

    public virtual IReadOnlyCollection<PJp005CommitteeDuties> CommitteeDuties { get; private set; }

    public virtual IReadOnlyCollection<PJp005Acceptors> Acceptors { get; set; }

    public virtual IReadOnlyCollection<PJp005DocumentHistory> DocumentHistories { get; private set; }

    public PJp005DocumentHistory? LastedDraftApprovalDocument =>
        this.DocumentHistories
            .Where(x => x.DocumentType == PJp005DocumentType.Approval)
            .Where(x => x.StatusState == PJp005Status.Draft)
            .Where(x => !x.IsReplaced)
            .OrderVersions()
            .FirstOrDefault();

    public PJp005DocumentHistory? LastedManageApprovalDocument =>
        this.DocumentHistories
            .Where(x => x.DocumentType == PJp005DocumentType.Approval)
            .Where(x => x.StatusState is PJp005Status.Draft or PJp005Status.Rejected or PJp005Status.Edit)
            .OrderVersions()
            .FirstOrDefault();

    public PJp005DocumentHistory? LastedWaitingApprovalIsReplaceApprovalDocument =>
        this.DocumentHistories
            .Where(x => x.DocumentType == PJp005DocumentType.Approval)
            .Where(x => x.StatusState == PJp005Status.WaitingApproval)
            .Where(x => !x.IsReplaced)
            .OrderVersions()
            .FirstOrDefault();

    public PJp005DocumentHistory? LastedDraftCommandDocument =>
        this.DocumentHistories
            .Where(x => x.DocumentType == PJp005DocumentType.Command)
            .Where(x => x.StatusState == PJp005Status.Draft)
            .Where(x => x.IsReplaced == false)
            .OrderVersions()
            .FirstOrDefault();

    public PJp005DocumentHistory? LastedManageCommandDocument =>
      this.DocumentHistories
          .Where(x => x.DocumentType == PJp005DocumentType.Command)
          .Where(x => x.StatusState is PJp005Status.Draft or PJp005Status.Rejected or PJp005Status.Edit)
          .OrderVersions()
          .FirstOrDefault();

    public PJp005DocumentHistory? LastedWaitingApprovalCommandDocument =>
        this.DocumentHistories
            .Where(x => x.DocumentType == PJp005DocumentType.Command)
            .Where(x => x.StatusState == PJp005Status.WaitingApproval)
            .Where(x => x.IsReplaced == false)
            .OrderVersions()
            .FirstOrDefault();

    public PJp005DocumentHistory? LastedApprovedMajorCommandDocument =>
        this.DocumentHistories
            .Where(x => x.DocumentType == PJp005DocumentType.Command)
            .Where(x => x.StatusState == PJp005Status.Approved)
            .OrderVersions()
            .LastOrDefault();

    public Unit AddDocumentHistory(
        PJp005DocumentType documentType,
        FileId fileId,
        bool isReplace)
    {
        var histories = this.DocumentHistories.ToHashSet();

        var existingStatus =
            histories
                .Where(p => p.DocumentType == documentType)
                .Any(p => p.StatusState == this.Status);

        var version = this.DocumentHistories
                          .Where(p => p.DocumentType == documentType)
                          .NextVersion(!existingStatus);

        histories.Add(
            PJp005DocumentHistory.Create(
                documentType,
                this.Status,
                version,
                fileId,
                isReplace));

        this.DocumentHistories = histories;

        return unit;
    }

    public Unit AddDocumentHistory(PJp005DocumentHistory documentHistory)
    {
        if (documentHistory == null)
        {
            throw new ArgumentNullException(nameof(documentHistory), "Document history cannot be null.");
        }

        if (this.DocumentHistories.Contains(documentHistory))
        {
            throw new InvalidOperationException("Document history already exists in the plan.");
        }

        var histories = this.DocumentHistories.ToHashSet();

        histories.Add(documentHistory);

        this.DocumentHistories = histories;

        return unit;
    }

    public Unit AddDocumentHistory(
        PJp005DocumentType docType,
        FileId? fileId,
        bool isReplace = false,
        bool incrementMajor = false)
    {
        if (fileId is not null)
        {
            var histories = this.DocumentHistories.ToHashSet();

            var existingStatus =
                histories
                    .Where(p => p.DocumentType == docType)
                    .OrderVersions()
                    .FirstOrDefault();

            var isIncreasedMajor = existingStatus == null || existingStatus.StatusState == this.Status;

            var version = this.DocumentHistories
                              .Where(p => p.DocumentType == docType)
                              .NextVersion(incrementMajor || !isIncreasedMajor);

            histories.Add(PJp005DocumentHistory.Create(
                docType,
                this.Status,
                version,
                fileId.Value,
                isReplace));

            this.DocumentHistories = histories;
        }

        return unit;
    }

    public static PJp005 Create(
        Procurement procurement,
        PpPurchaseRequisitionId ppPurchaseRequisitionId,
        int evaluationDueDate,
        string evaluationPeriodTypeCode,
        string evaluationPeriodConditionCode,
        string? egpProjectNumber = null)
    {
        if (procurement.ProcurementNumber == null)
        {
            throw new ArgumentException("Procurement number cannot be null.", nameof(procurement));
        }

        var newData = new PJp005
        {
            Id = PJp005Id.New(),
            ProcurementId = procurement.Id,
            PJp005Number = PJp005Number.New(procurement.ProcurementNumber!.Value),
            PpPurchaseRequisitionId = ppPurchaseRequisitionId,
            EvaluationDueDate = evaluationDueDate,
            EvaluationPeriodTypeCode = evaluationPeriodTypeCode,
            EvaluationPeriodConditionCode = evaluationPeriodConditionCode,
            EgpProjectNumber = egpProjectNumber,
            IsActive = true,
            Acceptors = [],
            Committees = [],
            CommitteeDuties = [],
            DocumentHistories = [],
            ProcurementSuppliesDivisions = [],
        };

        newData.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Create,
            $"สร้างข้อมูล",
            nameof(newData.Status)));

        return newData;
    }

    public PJp005 UpdateStatus(PJp005Status status)
    {
        var isSixTyOverPrice = this.Procurement.SupplyMethodCode.Value == "SMethod002" && this.Procurement.Budget > 100000;

        switch (this.Status, status)
        {
            case (_, PJp005Status.WaitingApproval):
                this.AddActivity(new ActivityInfo(
                    isSixTyOverPrice ? ActivityLogActionTypeConstant.SendApproveSegment : ActivityLogActionTypeConstant.SendApprove,
                    $"ส่งเห็นชอบ",
                    status.ToString()));

                break;

            case (_, PJp005Status.Cancelled):
                this.AddActivity(new ActivityInfo(
                    ActivityLogActionTypeConstant.Cancelled,
                    $"ขอยกเลิกสำเร็จ",
                    status.ToString()));

                break;

            case (_, PJp005Status.Edit):
                this.AddActivity(new ActivityInfo(
                    ActivityLogActionTypeConstant.Recall,
                    $"ขอเปลี่ยนแปลงข้อมูล",
                    status.ToString()));

                break;
        }

        this.Status = status;

        if (status == PJp005Status.WaitingApproval)
        {
            this.SetAcceptorsToPending();
        }

        if (status == PJp005Status.Edit)
        {
            this.SetAcceptorsToDraft();
        }

        return this;
    }

    private void SetAcceptorsToPending()
    {
        _ = this.Acceptors
                .Where(a => a is
                {
                    IsActive: true
                })
                .Iter(a => a.SetCurrent(false).Pending());

        var firstPending = this.Acceptors.FirstOrDefault(a => a.Status == AcceptorStatus.Pending);

        if (firstPending != null)
        {
            firstPending.SetCurrent();
        }
    }

    private void SetAcceptorsToDraft()
    {
        _ = this.Acceptors
                .Where(a => a is
                {
                    IsActive: true
                })
                .Iter(a => a.Draft());
    }

    public PJp005 SetEvaluationData(
        int evaluationDueDate,
        string evaluationPeriodTypeCode,
        string evaluationPeriodConditionCode)
    {
        this.EvaluationDueDate = evaluationDueDate;
        this.EvaluationPeriodTypeCode = evaluationPeriodTypeCode;
        this.EvaluationPeriodConditionCode = evaluationPeriodConditionCode;

        return this;
    }

    public PJp005 SetDocumentDate(DateTimeOffset? date = null)
    {
        this.DocumentDate = date ?? DateTimeOffset.Now;

        return this;
    }

    public PJp005 SetEgpProjectNumber(string? egpProjectNumber)
    {
        this.EgpProjectNumber = egpProjectNumber;

        return this;
    }

    public PJp005 SetJorPorNumber(string? jorPorNumber)
    {
        this.JorPorNumber = jorPorNumber;

        return this;
    }

    public PJp005 AddCommittee(PJp005Committee committee)
    {
        if (committee == null)
        {
            throw new ArgumentNullException(nameof(committee));
        }

        var committees = (this.Committees ?? []).ToHashSet();

        if (committees.Any(a => a.Id == committee.Id))
        {
            throw new InvalidOperationException("Committees with the same Id already exists.");
        }

        committees.Add(committee);

        this.Committees = committees;

        return this;
    }

    public PJp005 AddProcurementSuppliesDivision(PJp005ProcurementSuppliesDivision procurementSupplies)
    {
        if (procurementSupplies == null)
        {
            throw new ArgumentNullException(nameof(procurementSupplies));
        }

        var procurementSuppliesData = (this.ProcurementSuppliesDivisions ?? []).ToHashSet();

        if (procurementSuppliesData.Any(a => a.Id == procurementSupplies.Id))
        {
            throw new InvalidOperationException("Procurement supplies division with the same Id already exists.");
        }

        procurementSuppliesData.Add(procurementSupplies);

        this.ProcurementSuppliesDivisions = procurementSuppliesData;

        return this;
    }

    public PJp005 RemoveProcurementSuppliesDivision(PJp005ProcurementSuppliesDivision procurementSupplies)
    {
        if (procurementSupplies == null)
        {
            throw new ArgumentNullException(nameof(procurementSupplies));
        }

        var procurementSuppliesData = this.ProcurementSuppliesDivisions?.ToHashSet();

        if (procurementSuppliesData == null)
        {
            return this;
        }

        if (!procurementSuppliesData.Remove(procurementSupplies))
        {
            throw new InvalidOperationException("Procurement supplies division not found.");
        }

        this.ProcurementSuppliesDivisions = procurementSuppliesData;

        return this;
    }

    public PJp005 AddCommitteeDuties(PJp005CommitteeDuties committeeDuty)
    {
        var committeeDuties = (this.CommitteeDuties ?? []).ToHashSet();

        committeeDuties.Add(committeeDuty);

        this.CommitteeDuties = committeeDuties;

        return this;
    }

    public PJp005 AddAcceptor(PJp005Acceptors acceptor)
    {
        var acceptors = (this.Acceptors ?? []).ToHashSet();

        acceptors.Add(acceptor);

        this.Acceptors = acceptors;

        return this;
    }

    public PJp005 EvaluateAcceptorApproval()
    {
        var isAllApproved =
            this.Acceptors
                .Where(a => a is
                {
                    Type: AcceptorType.Approver or AcceptorType.DepartmentDirectorAgree,
                    IsActive: true,
                })
                .All(a => a.Status == AcceptorStatus.Approved);

        if (!isAllApproved)
        {
            return this;
        }

        this.SetApproved();
        this.Procurement.SetProcessType(ProcessType.Invite);

        return this;
    }

    private void SetApproved()
    {
        this.Status = PJp005Status.Approved;
    }

    public PJp005 SetRejected(string? remark)
    {
        var isSixTyOverPrice = this.Procurement.SupplyMethodCode.Value == "SMethod002" && this.Procurement.Budget > 100000;

        this.AddActivity(
            new ActivityInfo(
                isSixTyOverPrice ? ActivityLogActionTypeConstant.SegmentReject : ActivityLogActionTypeConstant.Reject,
                "ส่งกลับแก้ไข",
                this.Status.ToString(),
                remark));

        this.Status = PJp005Status.Rejected;

        return this;
    }

    public PJp005 RemoveAcceptor(PJp005Acceptors acceptor)
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

    public Unit Deactivate()
    {
        this.IsActive = false;

        return unit;
    }
}