namespace GHB.DP2.Domain.Procurement.PpPurchaseRequisition;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.PpTorDraft;
using GHB.DP2.Domain.SystemUtility;
using LanguageExt;
using Vogen;

public enum PurchaseRequisitionStatus
{
    /// <summary>
    /// แบบร่าง
    /// </summary>
    Draft,

    /// <summary>
    /// เรียกคืนแก้ไข
    /// </summary>
    Edit,

    /// <summary>
    /// รออนุมัติ
    /// </summary>
    WaitingApproval,

    /// <summary>
    /// รอมอบหมายผู้รับผิดชอบ
    /// </summary>
    WaitingAssign,

    /// <summary>
    /// มอบหมาย
    /// </summary>
    Approved,

    /// <summary>
    /// ส่งกลับแก้ไข
    /// </summary>
    Rejected,

    /// <summary>
    /// ยกเลิกรายการ
    /// </summary>
    Cancelled,
}

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PpPurchaseRequisitionId
{
    public static PpPurchaseRequisitionId New() => From(Guid.CreateVersion7());
}

[ValueObject<string>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PurchaseRequisitionNumber
{
    public static PurchaseRequisitionNumber New(ProcurementNumber procurementNumber)
    {
        if (string.IsNullOrWhiteSpace(procurementNumber.Value))
        {
            throw new ArgumentException("Procurement number cannot be null or empty.", nameof(procurementNumber));
        }

        var newNumber = $"{procurementNumber.Value}-0401";

        return From(newNumber);
    }

    public PurchaseRequisitionNumber Next()
    {
        if (string.IsNullOrWhiteSpace(this.Value))
        {
            throw new InvalidOperationException("Current TorDraftNumber is null or empty.");
        }

        // Assuming the TOR draft number is the format "PYYXXXXX-04XX"
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

public record BasicInfo(
    string? PurchaseRequisitionNumber,
    string? EgpNumber,
    string? PrNumber,
    string? Description,
    string? Telephone);

public record PriceInfo(
    string? PriceReasonablenessInfo,
    decimal? MedianPriceAmount,
    ParameterCode? EvaluationCriteriaCode);

public record DeliveryInfo(
    int? DeliveryPeriod,
    ParameterCode? DeliveryPeriodTypeCode,
    ParameterCode? DeliveryConditionCode,
    DateTimeOffset? DeliveryDate);

public record WarrantyInfo(
    bool HasWarranty,
    int? WarrantyPeriod,
    ParameterCode? WarrantyPeriodCode,
    ParameterCode? WarrantyConditionCode);

public record ContractOptions(
    bool HasFineRate,
    bool HasContractGuarantee,
    bool HasInspectionCommittee,
    bool HasConstructionSupervisor);

public partial class PpPurchaseRequisition : AuditableEntity<PpPurchaseRequisitionId>, IHasSoftDelete, IHasActivityInfo
{
    public override PpPurchaseRequisitionId Id { get; init; }

    public ProcurementId ProcurementId { get; private set; }

    public PpTorDraftId? TorDraftId { get; private set; }

    public PurchaseRequisitionNumber PurchaseRequisitionNumber { get; private set; }

    public string? EgpNumber { get; private set; }

    public string? PrNumber { get; private set; }

    public string? Description { get; private set; }

    public string? PriceReasonablenessInfo { get; private set; }

    public decimal? MedianPriceAmount { get; private set; }

    public ParameterCode? EvaluationCriteriaCode { get; private set; }

    public int? DeliveryPeriod { get; private set; }

    public ParameterCode? DeliveryPeriodTypeCode { get; private set; }

    public ParameterCode? DeliveryConditionCode { get; private set; }

    public DateTimeOffset? DeliveryDate { get; private set; }

    public DateTimeOffset? DocumentDate { get; private set; }

    public bool HasFineRate { get; private set; }

    public bool HasWarranty { get; private set; }

    public int? WarrantyPeriod { get; private set; }

    public ParameterCode? WarrantyPeriodCode { get; private set; }

    public ParameterCode? WarrantyConditionCode { get; private set; }

    public bool HasContractGuarantee { get; private set; }

    public bool HasInspectionCommittee { get; private set; }

    public bool HasConstructionSupervisor { get; private set; }

    public PurchaseRequisitionStatus Status { get; private set; }

    public string? Telephone { get; private set; }

    public string? SendEditRemark { get; private set; }

    public bool? IsMigration { get; init; }

    public ParameterCode? PaymentTypeCode { get; set; }

    public virtual SuParameter? PaymentType { get; init; }

    public virtual Procurement Procurement { get; init; }

    public virtual PpTorDraft? TorDraft { get; init; }

    public virtual SuParameter? EvaluationCriteria { get; init; }

    public virtual SuParameter? DeliveryPeriodType { get; init; }

    public virtual SuParameter? DeliveryCondition { get; init; }

    public virtual SuParameter? WarrantyPeriodType { get; init; }

    public virtual SuParameter? WarrantyCondition { get; init; }

    public virtual IReadOnlyCollection<PpPurchaseRequisitionBudget> Budgets { get; private set; }

    public virtual IReadOnlyCollection<PpPurchaseRequisitionWarranty> Warranties { get; private set; }

    public virtual IReadOnlyCollection<PpPurchaseRequisitionPaymentTerm> PaymentTerms { get; private set; }

    public virtual IReadOnlyCollection<PpPurchaseRequisitionTechnicalSpecifications> TechnicalSpecifications { get; private set; }

    public virtual IReadOnlyCollection<PpPurchaseRequisitionFineRate> FineRates { get; private set; }

    public virtual IReadOnlyCollection<PpPurchaseRequisitionCommittee> Committees { get; private set; }

    public virtual IReadOnlyCollection<PpPurchaseRequisitionAcceptors> Acceptors { get; private set; }

    public virtual IReadOnlyCollection<PpPurchaseRequisitionAssignee> Assignees { get; private set; }

    public virtual IReadOnlyCollection<PurchaseRequisitionDocumentHistory> DocumentHistories { get; private set; }

    public PpPurchaseRequisitionAssignee? LastedAssignee => this.Assignees.MaxBy(s => s.Sequence);

    public PurchaseRequisitionDocumentHistory? LastedDraftDocument =>
        this.DocumentHistories
            .Where(history => history is { IsReplaced: false })
            .OrderVersions()
            .FirstOrDefault();

    public PurchaseRequisitionDocumentHistory? LastedDocument =>
        this.DocumentHistories
            .OrderVersions()
            .FirstOrDefault();

    public PurchaseRequisitionDocumentHistory? LastedNotReplacedDocument =>
        this.DocumentHistories
            .Where(dh =>
                dh is
                {
                    StatusState: PurchaseRequisitionStatus.WaitingApproval,
                    IsReplaced: false
                })
            .OrderVersions()
            .FirstOrDefault();

    public PurchaseRequisitionDocumentHistory? LastedCreateByDocument =>
        this.DocumentHistories
            .Where(d => d.IsReplaced)
            .OrderVersions()
            .FirstOrDefault();

    public bool? IsReplacedDocument =>
        this.DocumentHistories.Any(d => d.IsReplaced);

    public Unit AddDocumentHistory(PurchaseRequisitionDocumentHistory documentHistory)
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
        FileId fileId,
        bool isRelace)
    {
        var histories = this.DocumentHistories.ToHashSet();

        var existingHistory =
            histories
                .OrderVersions()
                .FirstOrDefault();

        var isIncreaseMajorVersion = existingHistory is null || existingHistory.StatusState != this.Status;

        var version = this.DocumentHistories.NextVersion(isIncreaseMajorVersion);

        histories.Add(PurchaseRequisitionDocumentHistory.Create(
            this.Status,
            version,
            fileId,
            isRelace));

        this.DocumentHistories = histories;

        return unit;
    }

    public static PpPurchaseRequisition Create(
        Procurement procurement,
        BasicInfo basicInfo,
        PriceInfo priceInfo,
        DeliveryInfo deliveryInfo,
        WarrantyInfo warrantyInfo,
        ContractOptions contractOptions,
        bool isCheckPlanDepartmentCode,
        PpTorDraftId? torDraftId,
        string? paymentTypeCode)
    {
        if (procurement.ProcurementNumber == null)
        {
            throw new ArgumentException("Procurement number is null.", nameof(procurement));
        }

        var newData = new PpPurchaseRequisition
        {
            Id = PpPurchaseRequisitionId.New(),
            ProcurementId = procurement.Id,
            PurchaseRequisitionNumber = PurchaseRequisitionNumber.New(procurement.ProcurementNumber!.Value),
            EgpNumber = basicInfo.EgpNumber,
            PrNumber = basicInfo.PrNumber,
            Description = basicInfo.Description,
            PriceReasonablenessInfo = priceInfo.PriceReasonablenessInfo,
            MedianPriceAmount = priceInfo.MedianPriceAmount,
            EvaluationCriteriaCode = priceInfo.EvaluationCriteriaCode,
            DeliveryPeriod = deliveryInfo.DeliveryPeriod,
            DeliveryPeriodTypeCode = deliveryInfo.DeliveryPeriodTypeCode,
            DeliveryConditionCode = deliveryInfo.DeliveryConditionCode,
            DeliveryDate = deliveryInfo.DeliveryDate,
            HasFineRate = contractOptions.HasFineRate,
            HasWarranty = warrantyInfo.HasWarranty,
            WarrantyPeriod = warrantyInfo.WarrantyPeriod,
            WarrantyPeriodCode = warrantyInfo.WarrantyPeriodCode,
            WarrantyConditionCode = warrantyInfo.WarrantyConditionCode,
            HasContractGuarantee = contractOptions.HasContractGuarantee,
            HasInspectionCommittee = contractOptions.HasInspectionCommittee,
            HasConstructionSupervisor = contractOptions.HasConstructionSupervisor,
            Telephone = basicInfo.Telephone,
            TorDraftId = torDraftId,
            Status = isCheckPlanDepartmentCode ? PurchaseRequisitionStatus.Approved : PurchaseRequisitionStatus.Draft,
            DocumentHistories = [],
            Budgets = new List<PpPurchaseRequisitionBudget>().AsReadOnly(),
            Warranties = new List<PpPurchaseRequisitionWarranty>().AsReadOnly(),
            PaymentTerms = new List<PpPurchaseRequisitionPaymentTerm>().AsReadOnly(),
            FineRates = new List<PpPurchaseRequisitionFineRate>().AsReadOnly(),
            Committees = new List<PpPurchaseRequisitionCommittee>().AsReadOnly(),
            Acceptors = new List<PpPurchaseRequisitionAcceptors>().AsReadOnly(),
            Assignees = new List<PpPurchaseRequisitionAssignee>().AsReadOnly(),
            TechnicalSpecifications = new List<PpPurchaseRequisitionTechnicalSpecifications>().AsReadOnly(),
            PaymentTypeCode = !string.IsNullOrWhiteSpace(paymentTypeCode) ? ParameterCode.From(paymentTypeCode) : null,
        };
        newData.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Create,
            $"สร้างข้อมูลการแจ้งข้อมูลเบื่้องต้น(จพ.004)",
            newData.Status.ToString()));

        return newData;
    }

    public Unit Update(
        BasicInfo basicInfo,
        PriceInfo priceInfo,
        DeliveryInfo deliveryInfo,
        WarrantyInfo warrantyInfo,
        ContractOptions contractOptions,
        PurchaseRequisitionStatus status,
        string? paymentTypeCode)
    {
        this.EgpNumber = basicInfo.EgpNumber;
        this.PrNumber = basicInfo.PrNumber;
        this.Description = basicInfo.Description;
        this.Telephone = basicInfo.Telephone;
        this.PriceReasonablenessInfo = priceInfo.PriceReasonablenessInfo;
        this.MedianPriceAmount = priceInfo.MedianPriceAmount;
        this.EvaluationCriteriaCode = priceInfo.EvaluationCriteriaCode;
        this.DeliveryPeriod = deliveryInfo.DeliveryPeriod;
        this.DeliveryPeriodTypeCode = deliveryInfo.DeliveryPeriodTypeCode;
        this.DeliveryConditionCode = deliveryInfo.DeliveryConditionCode;
        this.DeliveryDate = deliveryInfo.DeliveryDate;
        this.HasFineRate = contractOptions.HasFineRate;
        this.HasWarranty = warrantyInfo.HasWarranty;
        this.WarrantyPeriod = warrantyInfo.WarrantyPeriod;
        this.WarrantyPeriodCode = warrantyInfo.WarrantyPeriodCode;
        this.WarrantyConditionCode = warrantyInfo.WarrantyConditionCode;
        this.HasContractGuarantee = contractOptions.HasContractGuarantee;
        this.HasInspectionCommittee = contractOptions.HasInspectionCommittee;
        this.HasConstructionSupervisor = contractOptions.HasConstructionSupervisor;
        this.PaymentTypeCode = !string.IsNullOrWhiteSpace(paymentTypeCode) ? ParameterCode.From(paymentTypeCode) : null;

        switch (status, this.Status)
        {
            case (PurchaseRequisitionStatus.WaitingApproval, _):
                this.AddActivity(new ActivityInfo(
                    ActivityLogActionTypeConstant.SendApprove,
                    $"ส่งเห็นชอบ/อนุมัติข้อมูลการแจ้งข้อมูลเบื่้องต้น(จพ.004)",
                    status.ToString()));

                break;

            case (PurchaseRequisitionStatus.WaitingAssign, _):
                this.AddActivity(new ActivityInfo(
                    ActivityLogActionTypeConstant.Assign,
                    $"มอบหมายผู้รับผิดชอบข้อมูลการแจ้งข้อมูลเบื่้องต้น(จพ.004)",
                    status.ToString()));

                break;

            case (PurchaseRequisitionStatus.Approved, PurchaseRequisitionStatus.WaitingAssign):
                this.AddActivity(new ActivityInfo(
                    ActivityLogActionTypeConstant.Assigned,
                    $"มอบหมายผู้รับผิดชอบข้อมูลการแจ้งข้อมูลเบื่้องต้น(จพ.004)",
                    status.ToString()));

                break;

            case (PurchaseRequisitionStatus.Edit, _):
                this.AddActivity(new ActivityInfo(
                    ActivityLogActionTypeConstant.Recall,
                    string.Empty,
                    status.ToString()));

                break;

            case (PurchaseRequisitionStatus.Rejected, PurchaseRequisitionStatus.WaitingAssign):
                this.AddActivity(new ActivityInfo(
                    ActivityLogActionTypeConstant.AssigneeReject,
                    string.Empty,
                    status.ToString()));

                break;

            default:
                this.AddActivity(new ActivityInfo(
                    ActivityLogActionTypeConstant.Update,
                    $"อัปเดตข้อมูลการแจ้งข้อมูลเบื่้องต้น(จพ.004)",
                    status.ToString()));

                break;
        }

        this.Status = status;

        return unit;
    }

    public Unit UpdatePriceConsiderationInfo(
        string? prNumber,
        string? description,
        string? telephone,
        string? priceReasonablenessInfo,
        decimal? medianPriceAmount)
    {
        this.PrNumber = prNumber;
        this.Description = description;
        this.Telephone = telephone;
        this.PriceReasonablenessInfo = priceReasonablenessInfo;
        this.MedianPriceAmount = medianPriceAmount;

        return unit;
    }

    public Unit SetStatus(PurchaseRequisitionStatus status, string? remark = null)
    {
        switch (status, this.Status)
        {
            case (PurchaseRequisitionStatus.Rejected, PurchaseRequisitionStatus.WaitingApproval):
                this.AddActivity(new ActivityInfo(
                    ActivityLogActionTypeConstant.Reject,
                    $"ส่งกลับแก้ไขข้อมูลการแจ้งข้อมูลเบื่้องต้น(จพ.004)",
                    status.ToString(),
                    remark));

                break;

            case (PurchaseRequisitionStatus.Rejected, PurchaseRequisitionStatus.WaitingAssign):
                this.AddActivity(new ActivityInfo(
                    ActivityLogActionTypeConstant.AssigneeReject,
                    $"ส่งกลับแก้ไขข้อมูลการแจ้งข้อมูลเบื่้องต้น(จพ.004)",
                    status.ToString(),
                    remark));

                break;

            case (PurchaseRequisitionStatus.WaitingAssign, _):
                this.AddActivity(new ActivityInfo(
                    ActivityLogActionTypeConstant.WaitingAssign,
                    $"รอมอบหมายผู้รับผิดชอบข้อมูลการแจ้งข้อมูลเบื่้องต้น(จพ.004)",
                    status.ToString(),
                    remark));

                break;
        }

        this.Status = status;

        return unit;
    }

    public PpPurchaseRequisition AddPpPurchaseRequisitionBudget(PpPurchaseRequisitionBudget budget)
    {
        var budgets = this.Budgets.ToHashSet();
        budgets.Add(budget);

        this.Budgets = budgets;

        return this;
    }

    public PpPurchaseRequisition AddPpPurchaseRequisitionWarranty(PpPurchaseRequisitionWarranty warranty)
    {
        var warranties = this.Warranties.ToHashSet();
        warranties.Add(warranty);

        this.Warranties = warranties;

        return this;
    }

    public PpPurchaseRequisition AddPpPurchaseRequisitionPaymentTerm(PpPurchaseRequisitionPaymentTerm paymentTerm)
    {
        var paymentTerms = this.PaymentTerms.ToHashSet();
        paymentTerms.Add(paymentTerm);

        this.PaymentTerms = paymentTerms;

        return this;
    }

    public PpPurchaseRequisition AddPpPurchaseRequisitionFineRate(PpPurchaseRequisitionFineRate fineRate)
    {
        var fineRates = this.FineRates.ToHashSet();
        fineRates.Add(fineRate);

        this.FineRates = fineRates;

        return this;
    }

    public PpPurchaseRequisition AddPpPurchaseRequisitionCommittee(PpPurchaseRequisitionCommittee committee)
    {
        var committees = this.Committees.ToHashSet();
        committees.Add(committee);

        this.Committees = committees;

        return this;
    }

    public PpPurchaseRequisition AddPpPurchaseRequisitionAcceptor(PpPurchaseRequisitionAcceptors acceptor)
    {
        var acceptors = this.Acceptors.ToHashSet();
        acceptors.Add(acceptor);

        this.Acceptors = acceptors;

        return this;
    }

    public PpPurchaseRequisition AddPpPurchaseRequisitionTechnicalSpecification(PpPurchaseRequisitionTechnicalSpecifications technicalSpecification)
    {
        var technicalSpecifications = this.TechnicalSpecifications.ToHashSet();
        technicalSpecifications.Add(technicalSpecification);

        this.TechnicalSpecifications = technicalSpecifications;

        return this;
    }

    public PpPurchaseRequisition RemoveAcceptorById(AcceptorId acceptorId)
    {
        var acceptors = this.Acceptors.ToHashSet();

        acceptors.RemoveWhere(w => w.Id == acceptorId);

        this.Acceptors = acceptors;

        return this;
    }

    public PpPurchaseRequisition AddPpPurchaseRequisitionAssignees(PpPurchaseRequisitionAssignee assignee)
    {
        var assignees = this.Assignees.ToHashSet();
        assignees.Add(assignee);

        this.Assignees = assignees;

        return this;
    }

    public PpPurchaseRequisition RemoveAssigneeById(PpPurchaseRequisitionAssigneeId assigneeId)
    {
        var assignees = this.Assignees.ToHashSet();

        assignees.RemoveWhere(w => w.Id == assigneeId);

        this.Assignees = assignees;

        return this;
    }

    public PpPurchaseRequisition SetDocumentDate(DateTimeOffset? date = null)
    {
        this.DocumentDate = date ?? DateTimeOffset.Now;

        return this;
    }

    public PpPurchaseRequisition EvaluateAcceptorApproval(string? remark = null)
    {
        var allApproved = this.Status == PurchaseRequisitionStatus.WaitingApproval
            ? this.Acceptors.Where(a => a.IsActive && a.Status != AcceptorStatus.Rejected)
                  .All(a => a.Status == AcceptorStatus.Approved)
            : this.Acceptors.Where(a => a.IsActive)
                  .All(a => a.Status == AcceptorStatus.Approved);

        if (!allApproved)
        {
            this.AddActivity(
                new ActivityInfo(
                    ActivityLogActionTypeConstant.Approved,
                    string.Empty,
                    this.Status.ToString(),
                    remark));

            return this;
        }

        this.SetStatus(PurchaseRequisitionStatus.WaitingAssign, remark);

        return this;
    }

    public Unit SetSendEditRemark(string? remark)
    {
        this.Status = PurchaseRequisitionStatus.Rejected;
        this.SendEditRemark = remark;
        this.Procurement.SetProcessType(ProcessType.PurchaseRequisition);

        this.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.SendEdit,
                $"ส่งกลับแก้ไขไปยังผู้จัดทำข้อมูลการแจ้งข้อมูลเบื้องต้น(จพ.004)",
                nameof(PurchaseRequisitionStatus.Rejected),
                remark));

        return unit;
    }
}