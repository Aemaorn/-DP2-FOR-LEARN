namespace GHB.DP2.Domain.Procurement.PPrincipleApproval;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using LanguageExt;
using Vogen;

public enum PPrincipleApprovalStatus
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
    /// อยู่ระหว่างหน่วยงานเห็นชอบ
    /// </summary>
    WaitingUnitApproval,

    /// <summary>
    /// รอ จพ. มอบหมายงาน
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
    /// รอการอนุมัติจากผู้มีอำนาจ
    /// </summary>
    WaitingAcceptance,

    /// <summary>
    /// อนุมัติแล้ว
    /// </summary>
    Approved,
}

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PPrincipleApprovalId
{
    public static PPrincipleApprovalId New() => From(Guid.CreateVersion7());
}

public partial class PPrincipleApproval : AuditableEntity<PPrincipleApprovalId>, IHasSoftDelete, IHasActivityInfo
{
    public override PPrincipleApprovalId Id { get; init; }

    // Reference to Procurement
    public ProcurementId ProcurementId { get; private set; }

    public string BranchLocation { get; private set; }

    public ParameterCode RentTypeCode { get; private set; }

    public DateTimeOffset RentalStartDate { get; private set; }

    public DateTimeOffset RentalEndDate { get; private set; }

    public int RentalDurationYear { get; private set; }

    public int RentalDurationMonth { get; private set; }

    public int RentalDurationDay { get; private set; }

    public decimal MaxMonthlyRent { get; private set; }

    public decimal TotalRentalAmount { get; private set; }

    public DateTimeOffset ExpectedContractDate { get; private set; }

    public string RentalLocationDetails { get; private set; }

    public string SubDistrictCode { get; private set; }

    public string SubDistrictName { get; private set; }

    public string DistrictCode { get; private set; }

    public string DistrictName { get; private set; }

    public string ProvinceCode { get; private set; }

    public string ProvinceName { get; private set; }

    public decimal? ReferencePriceAmount { get; private set; }

    public decimal? AnalysisSummaryNpv { get; private set; }

    public decimal? AnalysisSummaryPaybackYearPeriod { get; private set; }

    public decimal? AnalysisSummaryDiscountedPaybackYearPeriod { get; private set; }

    public bool IsRentCommittee { get; private set; }

    public bool IsAcceptanceCommittee { get; private set; }

    public string? PhoneNumber { get; private set; }

    public PPrincipleApprovalStatus Status { get; private set; }

    public DateTimeOffset? DocumentDate { get; private set; }

    public SuDocumentTemplateId DocumentTemplateId { get; private set; }

    public virtual SuDocumentTemplate DocumentTemplate { get; init; }

    public virtual Procurement Procurement { get; init; }

    public virtual SuParameter RentTypeCodeInfo { get; init; }

    public virtual IReadOnlyCollection<PPrincipleApprovalAcceptor> PrincipleApprovalAcceptors { get; private set; }

    public virtual IReadOnlyCollection<PPrincipleApprovalAssignee> PrincipleApprovalAssignees { get; private set; }

    public virtual IReadOnlyCollection<PPrincipleApprovalCommittee> PrincipleApprovalCommittees { get; private set; }

    public virtual IReadOnlyCollection<PPrincipleApprovalConsoPerfSupportData> PerfSupportData { get; private set; }

    public virtual IReadOnlyCollection<PPrincipleApprovalConsoPerfSupportDataDetails> PerfSupportDataDetails { get; private set; }

    public virtual IReadOnlyCollection<PPrincipleApprovalRoiLoanAndDepositSummary> RoiLoanAndDepositSummaries { get; private set; }

    public virtual IReadOnlyCollection<PPrincipleApprovalRoiPerfResult> RoiPerfResults { get; private set; }

    public virtual IReadOnlyCollection<PPrincipleApprovalBudget> PrincipleApprovalBudgets { get; private set; }

    public virtual IReadOnlyCollection<PPrincipleApprovalRentalAnalysis> PrincipleApprovalRentalAnalyses { get; private set; }

    public virtual IReadOnlyCollection<PPrincipleApprovalDocumentHistory> DocumentHistories { get; private set; }

    public virtual IReadOnlyCollection<PPrincipleApprovalAttachment> Attachments { get; private set; }

    public PPrincipleApproval SetRentalInfo(
        int durationYear,
        int durationMonth,
        int durationDay,
        decimal maxMonthlyRent,
        decimal totalRentalAmount,
        DateTimeOffset expectedContractDate)
    {
        this.RentalDurationYear = durationYear;
        this.RentalDurationMonth = durationMonth;
        this.RentalDurationDay = durationDay;
        this.MaxMonthlyRent = maxMonthlyRent;
        this.TotalRentalAmount = totalRentalAmount;
        this.ExpectedContractDate = expectedContractDate;

        return this;
    }

    public PPrincipleApproval SetLocationInfo(
        string rentalLocationDetails,
        string subDistrictCode,
        string subDistrictName,
        string districtCode,
        string districtName,
        string provinceCode,
        string provinceName)
    {
        this.RentalLocationDetails = rentalLocationDetails;
        this.SubDistrictCode = subDistrictCode;
        this.SubDistrictName = subDistrictName;
        this.DistrictCode = districtCode;
        this.DistrictName = districtName;
        this.ProvinceCode = provinceCode;
        this.ProvinceName = provinceName;

        return this;
    }

    public PPrincipleApproval SetAnalysisInfo(
        decimal? analysisSummaryNpv,
        decimal? analysisSummaryPaybackYearPeriod,
        decimal? analysisSummaryDiscountedPaybackYearPeriod)
    {
        this.AnalysisSummaryNpv = analysisSummaryNpv;
        this.AnalysisSummaryPaybackYearPeriod = analysisSummaryPaybackYearPeriod;
        this.AnalysisSummaryDiscountedPaybackYearPeriod = analysisSummaryDiscountedPaybackYearPeriod;

        return this;
    }

    public PPrincipleApproval SetReferencePrice(
        decimal referencePriceAmount)
    {
        this.ReferencePriceAmount = referencePriceAmount;

        return this;
    }

    public PPrincipleApproval SetPPrincipleApproval(
        string branchLocation,
        ParameterCode rentTypeCode,
        DateTimeOffset rentalStartDate,
        DateTimeOffset rentalEndDate)
    {
        this.BranchLocation = branchLocation;
        this.RentTypeCode = rentTypeCode;
        this.RentalStartDate = rentalStartDate;
        this.RentalEndDate = rentalEndDate;

        return this;
    }

    public PPrincipleApproval SetPhoneNumber(string? phoneNumber)
    {
        this.PhoneNumber = phoneNumber;

        return this;
    }

    public PPrincipleApproval SetIsRentalCommittee(bool isRental)
    {
        this.IsRentCommittee = isRental;

        return this;
    }

    public PPrincipleApproval SetIsAcceptanceCommittee(bool isAcceptance)
    {
        this.IsAcceptanceCommittee = isAcceptance;

        return this;
    }

    public static PPrincipleApproval Create(
        ProcurementId procurementId,
        string branchLocation,
        ParameterCode rentTypeCode,
        DateTimeOffset rentalStartDate,
        DateTimeOffset rentalEndDate,
        PPrincipleApprovalStatus status)
    {
        var newData = new PPrincipleApproval
        {
            Id = PPrincipleApprovalId.New(),
            ProcurementId = procurementId,
            BranchLocation = branchLocation,
            RentTypeCode = rentTypeCode,
            RentalStartDate = rentalStartDate,
            RentalEndDate = rentalEndDate,
            Status = status,
            PrincipleApprovalAcceptors = [],
            PrincipleApprovalAssignees = [],
            PrincipleApprovalCommittees = [],
            PerfSupportData = [],
            PerfSupportDataDetails = [],
            RoiLoanAndDepositSummaries = [],
            RoiPerfResults = [],
            DocumentHistories = [],
            Attachments = [],
        };

        newData.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Create,
            $"สร้างข้อมูลขอนุมัติหลักการ",
            nameof(PPrincipleApprovalStatus.Draft)));

        return newData;
    }

    public PPrincipleApproval AddAcceptor(PPrincipleApprovalAcceptor acceptor)
    {
        var acceptors = this.PrincipleApprovalAcceptors?.ToList() ?? new List<PPrincipleApprovalAcceptor>();
        acceptors.Add(acceptor);
        this.PrincipleApprovalAcceptors = acceptors;

        return this;
    }

    public PPrincipleApproval RemoveAcceptor(PPrincipleApprovalAcceptor acceptor)
    {
        var list = this.PrincipleApprovalAcceptors?.ToList() ?? new List<PPrincipleApprovalAcceptor>();
        list.Remove(acceptor);
        this.PrincipleApprovalAcceptors = list;

        return this;
    }

    public PPrincipleApproval AddAssignee(PPrincipleApprovalAssignee assignee)
    {
        var assignees = this.PrincipleApprovalAssignees?.ToList() ?? new List<PPrincipleApprovalAssignee>();
        assignees.Add(assignee);
        this.PrincipleApprovalAssignees = assignees;

        return this;
    }

    public PPrincipleApproval RemoveAssignee(PPrincipleApprovalAssignee? assign)
    {
        if (assign is null)
        {
            this.PrincipleApprovalAssignees = new List<PPrincipleApprovalAssignee>();

            return this;
        }

        var assignees = this.PrincipleApprovalAssignees?.ToList() ?? new List<PPrincipleApprovalAssignee>();

        var removed = assignees.Remove(assign);

        if (!removed)
        {
            throw new InvalidOperationException("Assignee not found.");
        }

        this.PrincipleApprovalAssignees = assignees;

        return this;
    }

    public void AddCommittee(PPrincipleApprovalCommittee committee)
    {
        var list = this.PrincipleApprovalCommittees?.ToList() ?? new List<PPrincipleApprovalCommittee>();
        list.Add(committee);
        this.PrincipleApprovalCommittees = list;
    }

    public PPrincipleApproval RemoveCommittee(PPrincipleApprovalCommittee committee)
    {
        if (committee == null)
        {
            throw new ArgumentNullException(nameof(committee));
        }

        var committees = this.PrincipleApprovalCommittees.ToHashSet();

        if (!committees.Remove(committee))
        {
            throw new InvalidOperationException("RoiPerfResult not found.");
        }

        this.PrincipleApprovalCommittees = committees;

        return this;
    }

    public PPrincipleApproval SetPerfSupportData(PPrincipleApprovalConsoPerfSupportData newData)
    {
        this.PerfSupportData = new List<PPrincipleApprovalConsoPerfSupportData> { newData };

        return this;
    }

    public PPrincipleApproval AddPerfSupportDataDetail(PPrincipleApprovalConsoPerfSupportDataDetails detail)
    {
        var list = this.PerfSupportDataDetails?.ToList() ?? new List<PPrincipleApprovalConsoPerfSupportDataDetails>();
        list.Add(detail);
        this.PerfSupportDataDetails = list;

        return this;
    }

    public PPrincipleApproval RemovePerfSupportDataDetail(PPrincipleApprovalConsoPerfSupportDataDetails detail)
    {
        if (detail == null)
        {
            throw new ArgumentNullException(nameof(detail));
        }

        var perfSupportDataDetails = this.PerfSupportDataDetails.ToHashSet();

        if (!perfSupportDataDetails.Remove(detail))
        {
            throw new InvalidOperationException("RoiLoanAndDepositSummary not found.");
        }

        this.PerfSupportDataDetails = perfSupportDataDetails;

        return this;
    }

    public PPrincipleApproval AddRoiLoanAndDepositSummary(PPrincipleApprovalRoiLoanAndDepositSummary summary)
    {
        var list = this.RoiLoanAndDepositSummaries?.ToList() ?? new List<PPrincipleApprovalRoiLoanAndDepositSummary>();
        list.Add(summary);
        this.RoiLoanAndDepositSummaries = list;

        return this;
    }

    public PPrincipleApproval RemoveRoiLoanAndDepositSummary(PPrincipleApprovalRoiLoanAndDepositSummary summary)
    {
        if (summary == null)
        {
            throw new ArgumentNullException(nameof(summary));
        }

        var roiLoanAndDepositSummaries = this.RoiLoanAndDepositSummaries.ToHashSet();

        if (!roiLoanAndDepositSummaries.Remove(summary))
        {
            throw new InvalidOperationException("RoiLoanAndDepositSummary not found.");
        }

        this.RoiLoanAndDepositSummaries = roiLoanAndDepositSummaries;

        return this;
    }

    public PPrincipleApproval AddRoiPerfResult(PPrincipleApprovalRoiPerfResult item)
    {
        var list = this.RoiPerfResults?.ToList() ?? new List<PPrincipleApprovalRoiPerfResult>();
        list.Add(item);
        this.RoiPerfResults = list;

        return this;
    }

    public PPrincipleApproval RemoveRoiPerfResult(PPrincipleApprovalRoiPerfResult item)
    {
        if (item == null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        var roiPerfResults = this.RoiPerfResults.ToHashSet();

        if (!roiPerfResults.Remove(item))
        {
            throw new InvalidOperationException("RoiPerfResult not found.");
        }

        this.RoiPerfResults = roiPerfResults;

        return this;
    }

    public PPrincipleApproval SetWaitingAcceptance()
    {
        this.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.SendApprove,
            $"เปลี่ยนสถานะจาก {this.Status} เปลี่ยนสถานะเป็น {nameof(PPrincipleApprovalStatus.WaitingAcceptance)}",
            this.Status.ToString()));

        this.Status = PPrincipleApprovalStatus.WaitingAcceptance;

        this.PrincipleApprovalAcceptors
            .Where(p => p.Type == AcceptorType.Approver)
            .Iter(a => a.SetAcceptorStatus(AcceptorStatus.Pending));

        return this;
    }

    public PPrincipleApproval SetDocumentDate(DateTimeOffset? date = null)
    {
        this.DocumentDate = date ?? DateTimeOffset.Now;

        return this;
    }

    public PPrincipleApproval SetStatus(PPrincipleApprovalStatus status)
    {
        switch (status, this.Status)
        {
            case (PPrincipleApprovalStatus.WaitingUnitApproval, _):
                this.PrincipleApprovalAcceptors
                    .Where(w => w.Type is AcceptorType.DepartmentDirectorAgree)
                    .Iter(i => i.Pending());

                this.AddActivity(new ActivityInfo(
                    ActivityLogActionTypeConstant.SendUnitApprove,
                    $"ส่งสายงานเห็นชอบ/อนุมัติ",
                    this.Status.ToString()));

                break;

            case (PPrincipleApprovalStatus.WaitingAssign, _):
                this.AddActivity(new ActivityInfo(
                    ActivityLogActionTypeConstant.WaitingAssign,
                    $"ส่งเจ้าหน้าที่พัสดุมอบหมาย",
                    this.Status.ToString()));

                break;

            case (PPrincipleApprovalStatus.WaitingComment, _):
                this.AddActivity(new ActivityInfo(
                    ActivityLogActionTypeConstant.WaitingComment,
                    $"รอเจ้าหน้าที่พัสดุให้ความเห็น",
                    this.Status.ToString()));

                break;

            case (PPrincipleApprovalStatus.WaitingAcceptance, _):
                this.AddActivity(new ActivityInfo(
                    ActivityLogActionTypeConstant.SendApprove,
                    $"ส่งเจ้าหน้าที่พัสดุุเห็นชอบ/อนุมัติ",
                    this.Status.ToString()));

                break;
        }

        this.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Update,
            $"เปลี่ยนสถานะจาก {this.Status} เปลี่ยนสถานะเป็น {status.ToString()}",
            this.Status.ToString()));

        this.Status = status;

        return this;
    }

    public PPrincipleApproval SetRejected(string? remark)
    {
        this.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Reject,
            $"เปลี่ยนสถานะจาก {this.Status} เปลี่ยนสถานะเป็น {nameof(PPrincipleApprovalStatus.Rejected)}",
            this.Status.ToString(),
            remark));

        this.Status = PPrincipleApprovalStatus.Rejected;

        return this;
    }

    public PPrincipleApproval SetRejectToAssignee()
    {
        this.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Reject,
            $"เปลี่ยนสถานะจาก {this.Status} เปลี่ยนสถานะเป็น {nameof(PPrincipleApprovalStatus.RejectToAssignee)}",
            this.Status.ToString()));

        this.Status = PPrincipleApprovalStatus.RejectToAssignee;

        return this;
    }

    public PPrincipleApproval AddBudget(PPrincipleApprovalBudget item)
    {
        var list = this.PrincipleApprovalBudgets?.ToList() ?? new List<PPrincipleApprovalBudget>();
        list.Add(item);
        this.PrincipleApprovalBudgets = list;

        return this;
    }

    public PPrincipleApproval RemoveBudget(PPrincipleApprovalBudget item)
    {
        if (item == null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        var principleApprovalBudgets = this.PrincipleApprovalBudgets.ToHashSet();

        if (!principleApprovalBudgets.Remove(item))
        {
            throw new InvalidOperationException("Budget not found.");
        }

        this.PrincipleApprovalBudgets = principleApprovalBudgets;

        return this;
    }

    public PPrincipleApproval AddRentalAnalysis(PPrincipleApprovalRentalAnalysis item)
    {
        var list = this.PrincipleApprovalRentalAnalyses?.ToList() ?? new List<PPrincipleApprovalRentalAnalysis>();
        list.Add(item);
        this.PrincipleApprovalRentalAnalyses = list;

        return this;
    }

    public PPrincipleApproval RemoveRentalAnalysis(PPrincipleApprovalRentalAnalysis item)
    {
        if (item == null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        var principleApprovalRentalAnalyses = this.PrincipleApprovalRentalAnalyses.ToHashSet();

        if (!principleApprovalRentalAnalyses.Remove(item))
        {
            throw new InvalidOperationException("RentalAnalysis not found.");
        }

        this.PrincipleApprovalRentalAnalyses = principleApprovalRentalAnalyses;

        return this;
    }

    public PPrincipleApprovalDocumentHistory? LastedDraftDocument =>
        this.DocumentHistories
            .Where(dh =>
                dh.StatusState == PPrincipleApprovalStatus.Draft)
            .OrderVersions()
            .FirstOrDefault();

    public PPrincipleApprovalDocumentHistory? LastedVersionDocument =>
    this.DocumentHistories
        .Where(dh =>
            dh.StatusState == PPrincipleApprovalStatus.Draft ||
            dh.StatusState == PPrincipleApprovalStatus.Edit ||
            dh.StatusState == PPrincipleApprovalStatus.Rejected ||
            dh.StatusState == PPrincipleApprovalStatus.RejectToAssignee)
        .OrderVersions()
        .FirstOrDefault();

    public PPrincipleApprovalDocumentHistory? LastedDocument =>
        this.DocumentHistories
            .OrderVersions()
            .FirstOrDefault();

    public PPrincipleApprovalDocumentHistory? LastedWaitingDocument =>
        this.DocumentHistories
            .Where(dh =>
                dh is
                {
                    StatusState: PPrincipleApprovalStatus.WaitingAcceptance,
                    IsReplaced: false
                })
            .OrderVersions()
            .FirstOrDefault();

    public PPrincipleApprovalDocumentHistory? LastedNotReplacedDocument =>
        this.DocumentHistories
            .Where(dh =>
                dh is
                {
                    StatusState: PPrincipleApprovalStatus.WaitingUnitApproval,
                    IsReplaced: false
                })
            .OrderVersions()
            .FirstOrDefault();

    public Unit AddDocumentHistory(
        FileId fileId,
        bool isReplace = false)
    {
        var histories =
            this.DocumentHistories.ToHashSet();

        var existingHistory =
            histories
                .OrderVersions()
                .FirstOrDefault();

        var isIncreaseMajorVersion =
            existingHistory is null ||
            existingHistory.StatusState != this.Status;

        var version =
            this.DocumentHistories
                .NextVersion(isIncreaseMajorVersion);

        histories.Add(
            PPrincipleApprovalDocumentHistory.Create(
                this.Status,
                version,
                fileId,
                isReplace));

        this.DocumentHistories = histories;

        return unit;
    }

    public PPrincipleApproval SetDocumentTemplate(SuDocumentTemplateId documentTemplateId)
    {
        this.DocumentTemplateId = documentTemplateId;

        return this;
    }

    public PPrincipleApproval RemoveAttachment(PPrincipleApprovalAttachment attachment)
    {
        var list = this.Attachments.ToHashSet();
        list.Remove(attachment);
        this.Attachments = list;

        return this;
    }

    public PPrincipleApproval AddAttachment(PPrincipleApprovalAttachment attachment)
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
}