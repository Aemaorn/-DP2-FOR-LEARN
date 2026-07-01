namespace GHB.DP2.Domain.Procurement.PpTorDraft;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using LanguageExt;
using Vogen;

public enum TorDraftStatus
{
    /// <summary>
    /// แบบร่าง
    /// </summary>
    Draft,

    /// <summary>
    /// ส่งกลับแก้ไข
    /// </summary>
    Rejected,

    /// <summary>
    /// เรียกคืนแก้ไข
    /// </summary>
    Edit,

    /// <summary>
    /// อยู่ระหว่าง คกก. เห็นชอบ
    /// </summary>
    WaitingCommitteeApproval,

    /// <summary>
    /// อยู่ระหว่างหน่วยงานเห็นชอบ
    /// </summary>
    WaitingUnitApproval,

    /// <summary>
    /// รอ จพ. มอบหมาย
    /// </summary>
    WaitingAssign,

    /// <summary>
    /// รอ จพ. ให้ความเห็น
    /// </summary>
    WaitingComment,

    /// <summary>
    /// ส่งกลับแก้ไข
    /// </summary>
    RejectToAssignee,

    /// <summary>
    /// รออนุมัติ
    /// </summary>
    WaitingApproval,

    /// <summary>
    /// อนุมัติแล้ว
    /// </summary>
    Approved,

    /// <summary>
    /// ยกเลิกรายการ
    /// </summary>
    Cancelled,
}

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PpTorDraftId
{
    public static PpTorDraftId New() => From(Guid.CreateVersion7());
}

[ValueObject<string>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct TorDraftNumber
{
    public static TorDraftNumber New(ProcurementNumber procurementNumber)
    {
        if (string.IsNullOrWhiteSpace(procurementNumber.Value))
        {
            throw new ArgumentException("Procurement number cannot be null or empty.", nameof(procurementNumber));
        }

        var newNumber = $"{procurementNumber.Value}-0201";

        return From(newNumber);
    }

    public TorDraftNumber Next()
    {
        if (string.IsNullOrWhiteSpace(this.Value))
        {
            throw new InvalidOperationException("Current TorDraftNumber is null or empty.");
        }

        // Assuming the TOR draft number is the format "PYYXXXXX-02XX"
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

public partial class PpTorDraft : AuditableEntity<PpTorDraftId>, IHasSoftDelete, IHasActivityInfo
{
    public override PpTorDraftId Id { get; init; }

    public PpTorDraftId? ReferenceId { get; set; }

    public ProcurementId ProcurementId { get; init; }

    public TorDraftNumber ReferenceNumber { get; set; }

    public DateTimeOffset? Date { get; set; }

    public DateTimeOffset? DocumentDate { get; private set; }

    public string? Telephone { get; set; }

    public bool? BidGuarantee { get; set; }

    public bool IsStock { get; set; }

    public string? Reason { get; set; }

    public string? EvaluationCriteria { get; set; }

    public bool IsChange { get; set; }

    public bool IsCancel { get; set; }

    public bool? IsMigration { get; init; }

    public bool? IsMA { get; set; }

    public TorDraftStatus Status { get; private set; }

    public bool IsActive { get; set; }

    public string? CancelReason { get; private set; }

    public string? ChangeReason { get; private set; }

    public bool? IsContractGuarantee { get; set; }

    public decimal? PercentageContract { get; set; }

    public bool? IsCM { get; set; }

    public bool? IsPM { get; set; }

    public bool? IsTraining { get; set; }

    public bool? IsImpediment { get; set; }

    public SuDocumentTemplateId? DocumentTemplateId { get; private set; }

    public virtual SuDocumentTemplate? DocumentTemplate { get; init; }

    public virtual Procurement Procurement { get; init; }

    public virtual ICollection<PpTorDraftObject> PpTorDraftObjects { get; set; }

    public virtual ICollection<PpTorDraftQualifications> PpTorDraftQualifications { get; set; }

    public virtual ICollection<PpTorDraftTechnicalPeriod> PpTorDraftTechnicalPeriods { get; set; }

    public virtual ICollection<PpTorDraftTechnicalSpecifications> PpTorDraftTechnicalSpecifications { get; set; }

    public virtual ICollection<PpTorDraftBudget> PpTorDraftBudgets { get; set; }

    public virtual ICollection<PpTorDraftWarranty> PpTorDraftWarranties { get; set; }

    public virtual ICollection<PpTorDraftPaymentTerm> PpTorDraftPaymentTerms { get; set; }

    public virtual ICollection<PpTorPaymentTermPeriod> PpTorPaymentTermPeriods { get; set; }

    public virtual ICollection<PpTorDraftFineRate> PpTorDraftFineRates { get; set; }

    public virtual ICollection<PpTorDraftAcceptors> PpTorDraftAcceptors { get; set; }

    public virtual IReadOnlyCollection<PpTorDraftAssignee> Assignees { get; private set; }

    public virtual IReadOnlyCollection<PpTorDraftDocumentHistory> DocumentHistories { get; private set; }

    public virtual PpTorTemplateComputer? PpTorTemplateComputer { get; set; }

    public virtual ICollection<PpTorImpediment> PpTorImpediments { get; set; }

    public virtual ICollection<PpTorTrainingItem> PpTorTrainingItems { get; set; }

    public PpTorDraftDocumentHistory? LastedDocument(PpTorDraftDocumentType documentType) =>
        this.DocumentHistories
            .Where(dh => dh.DocumentType == documentType)
            .OrderVersions()
            .FirstOrDefault();

    public PpTorDraftDocumentHistory? LastedDraftDocument(PpTorDraftDocumentType documentType) =>
        this.DocumentHistories
            .Where(dh =>
                (
                    dh.StatusState == TorDraftStatus.Draft ||
                    dh.StatusState == TorDraftStatus.Edit ||
                    dh.StatusState == TorDraftStatus.Rejected ||
                    dh.StatusState == TorDraftStatus.WaitingComment ||
                    dh.StatusState == TorDraftStatus.WaitingAssign
                ) &&
                dh.DocumentType == documentType)
            .OrderVersions()
            .FirstOrDefault();

    public PpTorDraftDocumentHistory? LastedNotReplacedDocument(PpTorDraftDocumentType documentType) =>
        this.DocumentHistories
            .Where(dh => dh.DocumentType == documentType)
            .Where(dh =>
                dh is
                    {
                        StatusState: TorDraftStatus.WaitingCommitteeApproval,
                        IsReplaced: false,
                    })
            .OrderVersions()
            .FirstOrDefault();

    public PpTorDraftDocumentHistory? LastedNotReplacedWaitingApprovalDocument(PpTorDraftDocumentType documentType) =>
        this.DocumentHistories
            .Where(dh => dh.DocumentType == documentType)
            .Where(dh =>
                dh is
                {
                    StatusState: TorDraftStatus.WaitingApproval,
                    IsReplaced: false,
                })
            .OrderVersions()
            .FirstOrDefault();

    public Unit AddDocumentHistory(
        PpTorDraftDocumentType documentType,
        FileId fileId,
        bool isReplace = false,
        bool incrementMajor = false)
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
                .NextVersion(incrementMajor || isIncreaseMajorVersion);

        histories.Add(PpTorDraftDocumentHistory.Create(
            documentType,
            histories.Any() ? this.Status : TorDraftStatus.Draft,
            version,
            fileId,
            isReplace));

        this.DocumentHistories = histories;

        return unit;
    }

    public PpTorDraft SetDocumentTemplate(SuDocumentTemplateId? documentTemplateId)
    {
        this.DocumentTemplateId = documentTemplateId;

        return this;
    }

    public PpTorDraft SerAssigneesDefault()
    {
        this.Assignees = [];

        return this;
    }

    public PpTorDraft AddAcceptor(PpTorDraftAcceptors acceptor)
    {
        if (acceptor is null)
        {
            throw new ArgumentNullException(nameof(acceptor), "Acceptor cannot be null.");
        }

        if (this.PpTorDraftAcceptors.Any(a => a.Id == acceptor.Id))
        {
            throw new InvalidOperationException("Acceptor already exists in the draft.");
        }

        var acceptorsList = (this.PpTorDraftAcceptors ?? []).ToHashSet();

        acceptorsList.Add(acceptor);

        this.PpTorDraftAcceptors = acceptorsList;

        return this;
    }

    public PpTorDraft RemoveAcceptor(PpTorDraftAcceptors acceptor)
    {
        if (acceptor is null)
        {
            throw new ArgumentNullException(nameof(acceptor), "Acceptor cannot be null.");
        }

        var acceptorsList = (this.PpTorDraftAcceptors ?? []).ToHashSet();

        if (!acceptorsList.Remove(acceptor))
        {
            throw new InvalidOperationException("Acceptor does not exist in the draft.");
        }

        this.PpTorDraftAcceptors = acceptorsList;

        return this;
    }

    public PpTorDraft AddAssignee(PpTorDraftAssignee assignee)
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

    public PpTorDraft RemoveAssignee(PpTorDraftAssignee assignee)
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

    public PpTorDraft SetEdit()
    {
        this.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Recall,
            $"เรียกคืนแก้ไขข้อมูล",
            TorDraftStatus.Edit.ToString()));

        this.Status = TorDraftStatus.Edit;

        this.PpTorDraftAcceptors
            .Iter(a => a.Draft());

        return this;
    }

    public PpTorDraft SetWaitingCommitteeApproval()
    {
        this.AddActivity(new ActivityInfo(
            "รอบุคคล/คณะกรรมการจัดทำร่างขอบเขตของงานเห็นชอบ",
            $"ส่งคณะกรรมการเห็นชอบ/อนุมัติข้อมูล",
            TorDraftStatus.WaitingCommitteeApproval.ToString()));

        this.Assignees
            .Iter(a => a.Pending());

        this.Status = TorDraftStatus.WaitingCommitteeApproval;

        this.PpTorDraftAcceptors
            .Where(p => p.Type != AcceptorType.TorDraftCommittee)
            .Iter(a => a.Draft());

        this.PpTorDraftAcceptors
            .Where(p => p is { Type: AcceptorType.TorDraftCommittee, IsUnableToPerformDuties: false })
            .Iter(a => a.Pending());

        return this;
    }

    public PpTorDraft SetWaitingUnitApproval()
    {
        this.AddActivity(new ActivityInfo(
            "ส่งบุคคล/คณะกรรมการจัดทำร่างขอบเขตของงานเห็นชอบ",
            $"ส่งลำดับเห็นชอบ/อนุมัติข้อมูล",
            TorDraftStatus.WaitingUnitApproval.ToString()));

        this.Status = TorDraftStatus.WaitingUnitApproval;

        this.PpTorDraftAcceptors
            .Where(p => p.Type == AcceptorType.DepartmentDirectorAgree)
            .Iter(a => a.Pending());

        return this;
    }

    public PpTorDraft SetWaitingComment()
    {
        this.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.WaitingComment,
            $"รอเจ้าหน้าที่พัสดุให้ความเห็นข้อมูล",
            TorDraftStatus.WaitingComment.ToString()));

        this.Status = TorDraftStatus.WaitingComment;

        return this;
    }

    public PpTorDraft SetActive(bool isActive)
    {
        this.IsActive = isActive;

        return this;
    }

    public bool HasMajorityRejection()
    {
        if (this.Status != TorDraftStatus.WaitingCommitteeApproval)
        {
            return false;
        }

        var committeesAble =
            this.PpTorDraftAcceptors
                .Where(a => a is
                {
                    Type: AcceptorType.TorDraftCommittee,
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

    public PpTorDraft SetUnitApproved()
    {
        this.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.WaitingAssign,
            $"ลำดับเห็นชอบ/อนุมัติข้อมูล",
            TorDraftStatus.WaitingAssign.ToString()));

        this.Status = TorDraftStatus.WaitingAssign;

        _ = this.Assignees
                .Where(p => p.Type == AssigneeType.Director)
                .Iter(a => a.Pending());

        return this;
    }

    public PpTorDraft SetAssigned()
    {
        this.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.WaitingAssign,
            $"รอเจ้าหน้าที่พัสดุมอบหมายผู้รับผิดชอบข้อมูล",
            TorDraftStatus.WaitingAssign.ToString()));

        this.Status = TorDraftStatus.WaitingAssign;

        _ = this.Assignees
                .Where(p => p.Type == AssigneeType.Director)
                .Iter(p => p.Assigned());

        return this;
    }

    public PpTorDraft SetWaitingApproval()
    {
        this.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.SendApprove,
            $"ส่งผู้มีอำนาจเห็นชอบ/อนุมัติ",
            TorDraftStatus.WaitingApproval.ToString()));

        this.Status = TorDraftStatus.WaitingApproval;

        var approvers = this.PpTorDraftAcceptors
                            .Where(p => p.Type == AcceptorType.Approver)
                            .OrderBy(a => a.Sequence)
                            .ToList();

        approvers.Iter(a =>
        {
            if (a.Status == AcceptorStatus.Draft || a.Status == AcceptorStatus.Rejected || a.Status == AcceptorStatus.Approved)
            {
                a.Pending();
            }

            a.SetCurrent(false);
        });

        var firstPending = approvers.FirstOrDefault(a => a.Status == AcceptorStatus.Pending);

        if (firstPending != null)
        {
            firstPending.SetCurrent(true);
        }

        return this;
    }

    public PpTorDraft SetApproved()
    {
        this.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Approved,
            $"ผู้มีอำนาจเห็นชอบ/อนุมัติข้อมูล",
            TorDraftStatus.Approved.ToString()));

        this.Status = TorDraftStatus.Approved;

        this.Procurement.SetProcessType(this.Procurement.Budget <= 100000 ? ProcessType.PurchaseRequisition : ProcessType.MedianPrice);

        return this;
    }

    public PpTorDraft SetCancelled()
    {
        this.Status = TorDraftStatus.Cancelled;
        this.Procurement.SetProcessType(ProcessType.TorDraft);
        this.Procurement.SetStatus(ProcurementStatus.Cancelled);

        this.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.Cancelled,
                $"ขอยกเลิกข้อมูล",
                nameof(TorDraftStatus.Cancelled)));

        return this;
    }

    public PpTorDraft SetRejected(string? remark, TorDraftStatus? status = null)
    {
        switch (status)
        {
            case TorDraftStatus.WaitingCommitteeApproval:
                this.AddActivity(new ActivityInfo(
                    "บุคคล/คณะกรรมการจัดทำร่างขอบเขตของงานไม่เห็นชอบ",
                    $"ส่งกลับแก้ไข",
                    TorDraftStatus.Rejected.ToString(),
                    remark));

                break;

            case TorDraftStatus.WaitingUnitApproval:
                this.AddActivity(new ActivityInfo(
                    "สายงานส่งกลับแก้ไข",
                    $"ส่งกลับแก้ไข",
                    TorDraftStatus.Rejected.ToString(),
                    remark));

                break;

            case TorDraftStatus.WaitingAssign:
            case TorDraftStatus.RejectToAssignee:
                this.AddActivity(new ActivityInfo(
                    "เจ้าหน้าที่พัสดุส่งกลับแก้ไข",
                    $"ส่งกลับแก้ไข",
                    TorDraftStatus.Rejected.ToString(),
                    remark));

                break;

            default:
                this.AddActivity(new ActivityInfo(
                    ActivityLogActionTypeConstant.Reject,
                    $"ส่งกลับแก้ไข",
                    "ส่งกลับแก้ไขผู้รับผิดชอบ",
                    remark));

                break;
        }

        this.Status = TorDraftStatus.Rejected;

        return this;
    }

    public PpTorDraft SetUnitRejected(string? remark)
    {
        this.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Reject,
            $"ผู้มีอำนาจเห็นชอบส่งกลับแก้ไขข้อมูล",
            TorDraftStatus.RejectToAssignee.ToString(),
            remark));

        this.Status = TorDraftStatus.RejectToAssignee;

        return this;
    }

    public static PpTorDraft CreateBasic(
        TorDraftStatus status,
        Procurement procurement,
        string? telephone,
        bool? bidGuarantee,
        bool isStock,
        bool? isMA,
        bool? isContractGuarantee,
        decimal? percentageContract)
    {
        if (procurement.ProcurementNumber is null)
        {
            throw new ArgumentException("Procurement must have a valid procurement number.", nameof(procurement));
        }

        var newData = new PpTorDraft
        {
            Id = PpTorDraftId.New(),
            ProcurementId = procurement.Id,
            Status = status,
            ReferenceNumber = TorDraftNumber.New(procurement.ProcurementNumber!.Value),
            Telephone = telephone,
            BidGuarantee = bidGuarantee,
            IsStock = isStock,
            IsMA = isMA,
            IsActive = true,
            DocumentHistories = [],
            IsContractGuarantee = isContractGuarantee,
            PercentageContract = percentageContract,
        };

        newData.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Create,
            $"สร้างข้อมูลข้อมูล",
            newData.Status.ToString()));

        return newData;
    }

    public PpTorDraft SetReasonAndCriteria(
        string? reason,
        string? evaluationCriteria)
    {
        this.Reason = reason;
        this.EvaluationCriteria = evaluationCriteria;

        return this;
    }

    public PpTorDraft SetChangeAndCancel(
        bool isChange,
        bool isCancel)
    {
        this.IsChange = isChange;
        this.IsCancel = isCancel;

        return this;
    }

    public PpTorDraft SetDocumentDate(DateTimeOffset? documentDate = null)
    {
        this.DocumentDate = documentDate ?? DateTimeOffset.Now;

        return this;
    }

    public PpTorDraft SetChangeReason(string reason)
    {
        this.ChangeReason = reason;

        this.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.RequestChange,
            $"อัปเดตข้อมูล",
            this.Status.ToString(),
            reason));

        return this;
    }

    public PpTorDraft SetCancelReason(string reason)
    {
        this.CancelReason = reason;

        this.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.RequestCancel,
            $"อัปเดตข้อมูล",
            this.Status.ToString(),
            reason));

        return this;
    }

    public PpTorDraft Clone(bool isChange, bool isCancel)
    {
        if (this.Procurement.ProcurementNumber is null)
        {
            throw new InvalidOperationException("Procurement must have a valid procurement number.");
        }

        var newTorNumber =
            this.ReferenceNumber.Value.StartsWith($"{this.Procurement.ProcurementNumber}-02")
                ? this.ReferenceNumber.Next()
                : TorDraftNumber.New(this.Procurement.ProcurementNumber!.Value);

        var newTor = new PpTorDraft
        {
            Id = PpTorDraftId.New(),
            DocumentTemplateId = this.DocumentTemplateId,
            ReferenceId = this.Id,
            ProcurementId = this.ProcurementId,
            ReferenceNumber = newTorNumber,
            Date = this.Date,
            Telephone = this.Telephone,
            BidGuarantee = this.BidGuarantee,
            IsStock = this.IsStock,
            Reason = this.Reason,
            EvaluationCriteria = this.EvaluationCriteria,
            Status = TorDraftStatus.Draft,
            IsActive = true,
            IsChange = isChange,
            IsCancel = isCancel,
            DocumentHistories = [],
            PpTorDraftObjects = this.PpTorDraftObjects?.Select(o => new PpTorDraftObject
            {
                Id = PpTorDraftObjectId.New(),
                Sequence = o.Sequence,
                Description = o.Description,
            }).ToList() ?? new List<PpTorDraftObject>(),
            PpTorDraftQualifications =
            [
                .. this.PpTorDraftQualifications.Select(q => new PpTorDraftQualifications
                {
                    Id = PpTorDraftQualificationsId.New(),
                    Sequence = q.Sequence,
                    Description = q.Description,
                })
            ],
            PpTorDraftTechnicalSpecifications =
            [
                .. this.PpTorDraftTechnicalSpecifications.Select(ts => new PpTorDraftTechnicalSpecifications
                {
                    Id = PpTorDraftTechnicalSpecificationsId.New(),
                    Sequence = ts.Sequence,
                    Name = ts.Name,
                    Description = ts.Description,
                    Quantity = ts.Quantity,
                    UnitCode = ts.UnitCode,
                })
            ],
            PpTorDraftTechnicalPeriods =
            [
                .. this.PpTorDraftTechnicalPeriods.Select(tp => new PpTorDraftTechnicalPeriod
                {
                    Id = PpTorDraftTechnicalPeriodId.New(),
                    Period = tp.Period,
                    PeriodTypeCode = tp.PeriodTypeCode,
                    PeriodType = tp.PeriodType,
                    PeriodConditionCode = tp.PeriodConditionCode,
                    DeliveryConditionCode = tp.DeliveryConditionCode,
                    DeliveryDate = tp.DeliveryDate,
                    StartDate = tp.StartDate,
                    EndDate = tp.EndDate,
                    PpTorDraftTechnicalPeriodDetails = tp.PpTorDraftTechnicalPeriodDetails != null
                        ?
                        [
                            .. tp.PpTorDraftTechnicalPeriodDetails.Select(tpDetail => new PpTorDraftTechnicalPeriodDetail
                            {
                                Id = PpTorDraftTechnicalPeriodDetailId.New(),
                                PpTorDraftTechnicalPeriodId = tp.Id,
                                Branch = tpDetail.Branch,
                                PersonalCount = tpDetail.PersonalCount,
                                StartDate = tpDetail.StartDate,
                                EndDate = tpDetail.EndDate,
                            })
                        ]
                        : new List<PpTorDraftTechnicalPeriodDetail>(),
                })
            ],
            PpTorDraftBudgets = this.PpTorDraftBudgets?.Select(b => new PpTorDraftBudget
            {
                Id = PpTorDraftBudgetId.New(),
                Sequence = b.Sequence,
                Description = b.Description,
                BudgetAmount = b.BudgetAmount,
                PpTorDraftBudgetDetails = b.PpTorDraftBudgetDetails != null
                    ?
                    [
                        .. b.PpTorDraftBudgetDetails.Select(bDetail => new PpTorDraftBudgetDetail
                        {
                            Id = PpTorDraftBudgetDetailId.New(),
                            PpTorDraftBudgetId = b.Id,
                            Sequence = bDetail.Sequence,
                            Department = bDetail.Department,
                            BudgetType = bDetail.BudgetType,
                            ProjectCode = bDetail.ProjectCode,
                            AccountNo = bDetail.AccountNo,
                            Budget = bDetail.Budget,
                        })
                    ]
                    : new List<PpTorDraftBudgetDetail>(),
            }).ToList() ?? new List<PpTorDraftBudget>(),
            PpTorDraftPaymentTerms =
            [
                .. this.PpTorDraftPaymentTerms.Select(pt => new PpTorDraftPaymentTerm
                {
                    Id = PpTorDraftPaymentTermId.New(),
                    ProRateTypeCode = pt.ProRateTypeCode,
                    PaymentPercent = pt.PaymentPercent,
                    Period = pt.Period,
                    TotalPeriod = pt.TotalPeriod,
                    TotalPeriodTypeCode = pt.TotalPeriodTypeCode,
                    PeriodTypeCode = pt.PeriodTypeCode,
                    PpTorDraftPaymentTermDetails = pt.PpTorDraftPaymentTermDetails != null
                        ?
                        [
                            .. pt.PpTorDraftPaymentTermDetails.Select(ptDetail => new PpTorDraftPaymentTermDetail
                            {
                                Id = PpTorDraftPaymentTermDetailId.New(),
                                PpTorDraftPaymentTermId = pt.Id, // This will be updated after save if needed
                                TermNumber = ptDetail.TermNumber,
                                Percent = ptDetail.Percent,
                                Period = ptDetail.Period,
                                Description = ptDetail.Description,
                            })
                        ]
                        : new List<PpTorDraftPaymentTermDetail>(),
                })
            ],
            PpTorDraftWarranties = this.PpTorDraftWarranties?.Select(w => new PpTorDraftWarranty
            {
                Id = PpTorDraftWarrantyId.New(),
                HasWarranty = w.HasWarranty,
                Period = w.Period,
                PeriodTypeCode = w.PeriodTypeCode,
                ConditionOther = w.ConditionOther,
            }).ToList() ?? new List<PpTorDraftWarranty>(),
            PpTorDraftFineRates = this.PpTorDraftFineRates?.Select(fr => new PpTorDraftFineRate
            {
                Id = PpTorDraftFineRateId.New(),
                Sequence = fr.Sequence,
                Rate = fr.Rate,
                Description = fr.Description,
                PeriodTypeCode = fr.PeriodTypeCode,
                ConditionCode = fr.ConditionCode,
                ConditionOther = fr.ConditionOther,
            }).ToList() ?? new List<PpTorDraftFineRate>(),
            Assignees = this.Assignees.Map(r => r.Clone()).ToHashSet(),
            PpTorDraftAcceptors = this.PpTorDraftAcceptors.Map(r => r.Clone()).ToHashSet(),
            IsContractGuarantee = this.IsContractGuarantee,
            PercentageContract = this.PercentageContract,
        };

        return newTor;
    }
}