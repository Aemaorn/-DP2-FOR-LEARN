namespace GHB.DP2.Domain.Procurement.PPrincipleApprovalRental;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using LanguageExt;
using Vogen;

public enum PPrincipleApprovalRentalStatus
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
    /// รออนุมัติจากคณะกรรมการ
    /// </summary>
    WaitingCommitteeApproval,

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
    /// รอการอนุมัติจากผู้มีอำนาจ
    /// </summary>
    WaitingAcceptance,

    /// <summary>
    /// อนุมัติแล้ว
    /// </summary>
    Approved,

    /// <summary>
    /// ส่งกลับแก้ไข
    /// </summary>
    RejectToAssignee,

    /// <summary>
    /// รอมอบหมายผู้จัดทำสัญญา
    /// </summary>
    WaitingContractAssign,

    /// <summary>
    /// มอบหมายผู้จัดทำสัญญา
    /// </summary>
    ContractAssigned,
}

public enum UseContractType
{
    /// <summary>
    /// ใช้สัญญาส่วนกลาง
    /// </summary>
    CentralContract,

    /// <summary>
    /// ใข้สัญญาคู่ค้า
    /// </summary>
    Vendor,
}

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PPrincipleApprovalRentalId
{
    public static PPrincipleApprovalRentalId New() => From(Guid.CreateVersion7());
}

public partial class PPrincipleApprovalRental : AuditableEntity<PPrincipleApprovalRentalId>, IHasSoftDelete, IHasActivityInfo
{
    public override PPrincipleApprovalRentalId Id { get; init; }

    public ProcurementId ProcurementId { get; private set; }

    public UseContractType UseContract { get; private set; }

    // Rental Information Fields (from PPrincipleApproval)
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

    public string? PhoneNumber { get; private set; }

    public PPrincipleApprovalRentalStatus Status { get; private set; }

    public DateTimeOffset? DocumentDate { get; private set; }

    public virtual Procurement Procurement { get; init; }

    public virtual SuParameter RentTypeCodeInfo { get; init; }

    public virtual IReadOnlyCollection<PPrincipleApprovalRentalAcceptor> Acceptors { get; private set; }

    public virtual IReadOnlyCollection<PPrincipleApprovalRentalAssignee> Assignees { get; private set; }

    public virtual IReadOnlyCollection<PPrincipleApprovalRentalConsoPerfSupportData> PerfSupportData { get; private set; }

    public virtual IReadOnlyCollection<PPrincipleApprovalRentalConsoPerfSupportDataDetails> PerfSupportDataDetails { get; private set; }

    public virtual IReadOnlyCollection<PPrincipleApprovalRentalRoiLoanAndDepositSummary> RoiLoanAndDepositSummaries { get; private set; }

    public virtual IReadOnlyCollection<PPrincipleApprovalRentalRoiPerfResult> RoiPerfResults { get; private set; }

    public virtual IReadOnlyCollection<PPrincipleApprovalRentalBudget> Budgets { get; private set; }

    public virtual IReadOnlyCollection<PPrincipleApprovalRentalRentalAnalysis> RentalAnalyses { get; private set; }

    public virtual IReadOnlyCollection<PPrincipleApprovalRentalEntrepreneurs> Entrepreneurs { get; private set; }

    public virtual IReadOnlyCollection<PPrincipleApprovalRentalDocumentHistory> DocumentHistories { get; private set; }

    public virtual IReadOnlyCollection<PPrincipleApprovalRentalComparingAttachments>? ComparingAttachments { get; private set; }

    public PPrincipleApprovalRentalDocumentHistory? LastedDocument(PPrincipleApprovalRentalDocumentType documentType) =>
        this.DocumentHistories
            .Where(dh => dh.DocumentType == documentType)
            .OrderVersions()
            .FirstOrDefault();

    public PPrincipleApprovalRentalDocumentHistory? LastedDocument(PPrincipleApprovalRentalDocumentType documentType, PPrincipleApprovalRentalStatus status) =>
        this.DocumentHistories
            .Where(dh => dh.DocumentType == documentType)
            .Where(x => x.StatusState == status)
            .Where(x => x.IsReplaced == false)
            .OrderVersions()
            .FirstOrDefault();

    public PPrincipleApprovalRentalDocumentHistory? LastedWaitingDocument(PPrincipleApprovalRentalDocumentType documentType) =>
        this.DocumentHistories
            .Where(dh => dh.DocumentType == documentType)
            .Where(dh =>
                dh is
                {
                    StatusState: PPrincipleApprovalRentalStatus.WaitingUnitApproval,
                    IsReplaced: false
                })
            .OrderVersions()
            .FirstOrDefault();

    public PPrincipleApprovalRentalDocumentHistory? LastedDraftDocument(PPrincipleApprovalRentalDocumentType documentType) =>
        this.DocumentHistories
            .Where(dh => dh.DocumentType == documentType)
            .Where(dh => dh.StatusState == PPrincipleApprovalRentalStatus.Draft
                || dh.StatusState == PPrincipleApprovalRentalStatus.Edit
                || dh.StatusState == PPrincipleApprovalRentalStatus.Rejected
                || dh.StatusState == PPrincipleApprovalRentalStatus.WaitingComment)
            .OrderVersions()
            .FirstOrDefault();

    public PPrincipleApprovalRentalDocumentHistory? LastedNotReplacedDocument(PPrincipleApprovalRentalDocumentType documentType) =>
        this.DocumentHistories
            .Where(dh => dh.DocumentType == documentType)
            .Where(dh =>
                dh is
                {
                    StatusState: PPrincipleApprovalRentalStatus.WaitingCommitteeApproval,
                    IsReplaced: false
                })
            .OrderVersions()
            .FirstOrDefault();

    public PPrincipleApprovalRentalDocumentHistory? LastedWaitingCommentNotReplacedDocument(PPrincipleApprovalRentalDocumentType documentType) =>
    this.DocumentHistories
        .Where(dh => dh.DocumentType == documentType)
        .Where(dh =>
            dh is
            {
                StatusState: PPrincipleApprovalRentalStatus.WaitingComment,
            })
        .OrderVersions()
        .FirstOrDefault();

    public Unit UpsertAttachment(List<PPrincipleApprovalRentalComparingAttachments> attachment)
    {
        this.ComparingAttachments = attachment;

        return unit;
    }

    public PPrincipleApprovalRental SetReferencePrice(decimal referencePriceAmount)
    {
        this.ReferencePriceAmount = referencePriceAmount;

        return this;
    }

    public PPrincipleApprovalRental SetAnalysisInfo(
        decimal? analysisSummaryNpv,
        decimal? analysisSummaryPaybackYearPeriod,
        decimal? analysisSummaryDiscountedPaybackYearPeriod)
    {
        this.AnalysisSummaryNpv = analysisSummaryNpv;
        this.AnalysisSummaryPaybackYearPeriod = analysisSummaryPaybackYearPeriod;
        this.AnalysisSummaryDiscountedPaybackYearPeriod = analysisSummaryDiscountedPaybackYearPeriod;

        return this;
    }

    public PPrincipleApprovalRental SetUseContract(UseContractType useContract)
    {
        this.UseContract = useContract;

        return this;
    }

    public PPrincipleApprovalRental SetRentalInfo(
        string branchLocation,
        ParameterCode rentTypeCode,
        DateTimeOffset rentalStartDate,
        DateTimeOffset rentalEndDate,
        int durationYear,
        int durationMonth,
        int durationDay,
        decimal maxMonthlyRent,
        decimal totalRentalAmount,
        DateTimeOffset expectedContractDate,
        string rentalLocationDetails,
        string subDistrictCode,
        string subDistrictName,
        string districtCode,
        string districtName,
        string provinceCode,
        string provinceName,
        string? phoneNumber)
    {
        this.BranchLocation = branchLocation;
        this.RentTypeCode = rentTypeCode;
        this.RentalStartDate = rentalStartDate;
        this.RentalEndDate = rentalEndDate;
        this.RentalDurationYear = durationYear;
        this.RentalDurationMonth = durationMonth;
        this.RentalDurationDay = durationDay;
        this.MaxMonthlyRent = maxMonthlyRent;
        this.TotalRentalAmount = totalRentalAmount;
        this.ExpectedContractDate = expectedContractDate;
        this.RentalLocationDetails = rentalLocationDetails;
        this.SubDistrictCode = subDistrictCode;
        this.SubDistrictName = subDistrictName;
        this.DistrictCode = districtCode;
        this.DistrictName = districtName;
        this.ProvinceCode = provinceCode;
        this.ProvinceName = provinceName;
        this.PhoneNumber = phoneNumber;

        return this;
    }

    public static PPrincipleApprovalRental Create(
        ProcurementId procurementId)
    {
        var newData = new PPrincipleApprovalRental
        {
            Id = PPrincipleApprovalRentalId.New(),
            ProcurementId = procurementId,
            Acceptors = [],
            Assignees = [],
            PerfSupportData = [],
            PerfSupportDataDetails = [],
            RoiLoanAndDepositSummaries = [],
            RoiPerfResults = [],
            Budgets = [],
            RentalAnalyses = [],
            Entrepreneurs = [],
            DocumentHistories = [],
        };

        newData.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Create,
            $"สร้างข้อมูล",
            nameof(newData.Status)));

        return newData;
    }

    public PPrincipleApprovalRental AddAcceptor(PPrincipleApprovalRentalAcceptor acceptor)
    {
        var acceptors = this.Acceptors?.ToList() ?? new List<PPrincipleApprovalRentalAcceptor>();
        acceptors.Add(acceptor);
        this.Acceptors = acceptors;

        return this;
    }

    public PPrincipleApprovalRental RemoveAcceptor(PPrincipleApprovalRentalAcceptor acceptor)
    {
        var list = this.Acceptors?.ToList() ?? new List<PPrincipleApprovalRentalAcceptor>();
        list.Remove(acceptor);
        this.Acceptors = list;

        return this;
    }

    public PPrincipleApprovalRental AddAssignee(PPrincipleApprovalRentalAssignee assignee)
    {
        var assignees = this.Assignees?.ToList() ?? new List<PPrincipleApprovalRentalAssignee>();
        assignees.Add(assignee);
        this.Assignees = assignees;

        return this;
    }

    public PPrincipleApprovalRental RemoveAssignee(PPrincipleApprovalRentalAssignee? assign)
    {
        if (assign is null)
        {
            this.Assignees = new List<PPrincipleApprovalRentalAssignee>();

            return this;
        }

        var assignees = this.Assignees?.ToList() ?? new List<PPrincipleApprovalRentalAssignee>();

        var removed = assignees.Remove(assign);

        if (!removed)
        {
            throw new InvalidOperationException("Assignee not found.");
        }

        this.Assignees = assignees;

        return this;
    }

    public PPrincipleApprovalRental SetPerfSupportData(PPrincipleApprovalRentalConsoPerfSupportData newData)
    {
        this.PerfSupportData = new List<PPrincipleApprovalRentalConsoPerfSupportData> { newData };

        return this;
    }

    public PPrincipleApprovalRental AddPerfSupportDataDetail(PPrincipleApprovalRentalConsoPerfSupportDataDetails detail)
    {
        var list = this.PerfSupportDataDetails?.ToList() ?? new List<PPrincipleApprovalRentalConsoPerfSupportDataDetails>();
        list.Add(detail);
        this.PerfSupportDataDetails = list;

        return this;
    }

    public PPrincipleApprovalRental RemovePerfSupportDataDetail(PPrincipleApprovalRentalConsoPerfSupportDataDetails detail)
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

    public PPrincipleApprovalRental AddRoiLoanAndDepositSummary(PPrincipleApprovalRentalRoiLoanAndDepositSummary summary)
    {
        var list = this.RoiLoanAndDepositSummaries?.ToList() ?? new List<PPrincipleApprovalRentalRoiLoanAndDepositSummary>();
        list.Add(summary);
        this.RoiLoanAndDepositSummaries = list;

        return this;
    }

    public PPrincipleApprovalRental RemoveRoiLoanAndDepositSummary(PPrincipleApprovalRentalRoiLoanAndDepositSummary summary)
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

    public PPrincipleApprovalRental AddRoiPerfResult(PPrincipleApprovalRentalRoiPerfResult item)
    {
        var list = this.RoiPerfResults?.ToList() ?? new List<PPrincipleApprovalRentalRoiPerfResult>();
        list.Add(item);
        this.RoiPerfResults = list;

        return this;
    }

    public PPrincipleApprovalRental RemoveRoiPerfResult(PPrincipleApprovalRentalRoiPerfResult item)
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

    public PPrincipleApprovalRental SetWaitingAcceptance()
    {
        this.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.SendApprove,
            "ส่งผู้มีอำนาจเห็นชอบ/อนุมัติ",
            nameof(this.Status)));

        this.Status = PPrincipleApprovalRentalStatus.WaitingAcceptance;

        this.Acceptors
            .Where(p => p.Type == AcceptorType.Approver)
            .Iter(a => a.SetAcceptorStatus(AcceptorStatus.Pending));

        var firstPending = this.Acceptors.FirstOrDefault(a => a.Status == AcceptorStatus.Pending);

        if (firstPending != null)
        {
            firstPending.SetCurrent();
        }

        return this;
    }

    public PPrincipleApprovalRental SetStatusAssignee()
    {
        this.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.WaitingAssign,
            $"ส่งเจ้าหน้าที่พัสดุมอบหมายผู้รับผิดชอบ",
            nameof(this.Status)));

        this.Status = PPrincipleApprovalRentalStatus.WaitingAssign;

        return this;
    }

    public PPrincipleApprovalRental SetStatusWaitingComment()
    {
        this.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.WaitingComment,
            $"รอเจ้าหน้าที่พัสดุให้ความเห็น",
            nameof(this.Status)));

        this.Status = PPrincipleApprovalRentalStatus.WaitingComment;

        return this;
    }

    public PPrincipleApprovalRental SetDocumentDate(DateTimeOffset? date = null)
    {
        this.DocumentDate = date ?? DateTimeOffset.Now;

        return this;
    }

    public PPrincipleApprovalRental SetStatus(PPrincipleApprovalRentalStatus status)
    {
        this.Status = status;

        return this;
    }

    public PPrincipleApprovalRental SetStatusRejected(string? remark)
    {
        this.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Reject,
            $"ส่งกลับแก้ไข",
            nameof(this.Status),
            remark));

        this.Status = PPrincipleApprovalRentalStatus.Rejected;

        return this;
    }

    public PPrincipleApprovalRental SetStatusApproved()
    {
        this.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.WaitingAssign,
            ActivityLogActionTypeConstant.WaitingAssign,
            nameof(this.Status)));

        this.Status = PPrincipleApprovalRentalStatus.WaitingAssign;

        return this;
    }

    public PPrincipleApprovalRental SetStatusWaitingContractAssign()
    {
        this.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.WaitingAssign,
            ActivityLogActionTypeConstant.WaitingAssign,
            nameof(this.Status)));

        this.Status = PPrincipleApprovalRentalStatus.WaitingContractAssign;

        return this;
    }

    public PPrincipleApprovalRental AddBudget(PPrincipleApprovalRentalBudget item)
    {
        var list = this.Budgets?.ToList() ?? new List<PPrincipleApprovalRentalBudget>();
        list.Add(item);
        this.Budgets = list;

        return this;
    }

    public PPrincipleApprovalRental RemoveBudget(PPrincipleApprovalRentalBudget item)
    {
        if (item == null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        var principleApprovalBudgets = this.Budgets.ToHashSet();

        if (!principleApprovalBudgets.Remove(item))
        {
            throw new InvalidOperationException("Budget not found.");
        }

        this.Budgets = principleApprovalBudgets;

        return this;
    }

    public PPrincipleApprovalRental AddRentalAnalysis(PPrincipleApprovalRentalRentalAnalysis item)
    {
        var list = this.RentalAnalyses?.ToList() ?? new List<PPrincipleApprovalRentalRentalAnalysis>();
        list.Add(item);
        this.RentalAnalyses = list;

        return this;
    }

    public PPrincipleApprovalRental RemoveRentalAnalysis(PPrincipleApprovalRentalRentalAnalysis item)
    {
        if (item == null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        var principleApprovalRentalAnalyses = this.RentalAnalyses.ToHashSet();

        if (!principleApprovalRentalAnalyses.Remove(item))
        {
            throw new InvalidOperationException("RentalAnalysis not found.");
        }

        this.RentalAnalyses = principleApprovalRentalAnalyses;

        return this;
    }

    public PPrincipleApprovalRental AddEntrepreneurs(PPrincipleApprovalRentalEntrepreneurs item)
    {
        var list = this.Entrepreneurs?.ToList() ?? new List<PPrincipleApprovalRentalEntrepreneurs>();
        list.Add(item);
        this.Entrepreneurs = list;

        return this;
    }

    public PPrincipleApprovalRental RemoveEntrepreneurs(PPrincipleApprovalRentalEntrepreneurs item)
    {
        if (item == null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        var entrepreneurs = this.Entrepreneurs.ToHashSet();

        if (!entrepreneurs.Remove(item))
        {
            throw new InvalidOperationException("Entrepreneurs not found.");
        }

        this.Entrepreneurs = entrepreneurs;

        return this;
    }

    public PPrincipleApprovalRental AddEntrepreneur(PPrincipleApprovalRentalEntrepreneurs entrepreneurs)
    {
        var list = this.Entrepreneurs?.ToList() ?? new List<PPrincipleApprovalRentalEntrepreneurs>();
        list.Add(entrepreneurs);
        this.Entrepreneurs = list;

        return this;
    }

    public bool HasMajorityRejection()
    {
        if (this.Status != PPrincipleApprovalRentalStatus.WaitingCommitteeApproval)
        {
            return false;
        }

        var committeesAble =
            this.Acceptors
                .Where(a => a is
                {
                    Type: AcceptorType.RentCommittee,
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

    public PPrincipleApprovalRental SetRejected()
    {
        this.Status = PPrincipleApprovalRentalStatus.Rejected;

        return this;
    }

    public PPrincipleApprovalRental SetRejectToAssignee()
    {
        this.Status = PPrincipleApprovalRentalStatus.RejectToAssignee;

        return this;
    }

    public Unit AddDocumentHistory(
        PPrincipleApprovalRentalDocumentType documentType,
        FileId fileId,
        bool? isReplace = false,
        bool incrementMajor = false)
    {
        var histories = this.DocumentHistories.ToHashSet();

        var existingHistory =
            histories
                .Where(p => p.DocumentType == documentType)
                .OrderVersions()
                .FirstOrDefault();

        var isIncreaseMajorVersion =
            existingHistory is null ||
            existingHistory.StatusState != this.Status;

        var version =
            this.DocumentHistories
                .Where(dh => dh.DocumentType == documentType)
                .NextVersion(incrementMajor || isIncreaseMajorVersion);

        histories.Add(
            PPrincipleApprovalRentalDocumentHistory.Create(
                documentType,
                this.Status,
                version,
                fileId,
                isReplace));

        this.DocumentHistories = histories;

        return unit;
    }
}