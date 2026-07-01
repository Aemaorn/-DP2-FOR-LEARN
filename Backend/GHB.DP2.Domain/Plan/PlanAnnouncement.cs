namespace GHB.DP2.Domain.Plan;

using System.Text.RegularExpressions;
using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using LanguageExt;
using Vogen;

public enum PlanAnnouncementAction
{
    /// <summary>
    /// เรียกคืนแก้ไข
    /// </summary>
    Recall,

    /// <summary>
    /// ผู้มีอำนาจเห็นชอบ/อนุมัติ (ส่งกลับแก้ไข)
    /// </summary>
    AcceptorReject,

    /// <summary>
    /// ผู้มีอำนาจเห็นชอบ/อนุมัติ (อนุมัติ)
    /// </summary>
    AcceptorApprove,

    /// <summary>
    /// ผอ.จพ. เผยแพร่แผน
    /// </summary>
    DirectorAnnouncement,
}

public enum PlanAnnouncementStatus
{
    /// <summary>
    /// แบบร่าง
    /// </summary>
    Draft,

    /// <summary>
    /// จพ. มอบหมายงาน
    /// </summary>
    WaitingAssign,

    /// <summary>
    /// รออนุมัติ
    /// </summary>
    WaitingAcceptor,

    /// <summary>
    /// อนุมัติ
    /// </summary>
    WaitingAnnouncement,

    /// <summary>
    /// เผยแพร่ประกาศแผน
    /// </summary>
    Announcement,

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
public partial struct PlanAnnouncementId
{
    public static PlanAnnouncementId New() => From(Guid.CreateVersion7());
}

[ValueObject<string>(Conversions.EfCoreValueConverter)]
public partial struct PlanAnnouncementNumber
{
    public static PlanAnnouncementNumber New(int year)
    {
        if (year <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(year), "Year must be greater than 0.");
        }

        // Generate a new plan announcement number in the format "ANYY00001"
        // where YY is the last two digits of the year and 00001 is the initial number
        var yearString = (year % 100).ToString("D2"); // Get last two digits of year and ensure it's two digits

        var planNumber = GenerateAnnouncementNumber(yearString, 1);

        return From(planNumber);
    }

    public readonly PlanAnnouncementNumber Next()
    {
        var match = PlanAnnouncementNumberRegex().Match(this.Value);

        if (!match.Success)
        {
            throw new FormatException("Invalid plan announcement number format.");
        }

        var yearPart = match.Groups["year"].Value;
        var numberPart = int.Parse(match.Groups["announcement_number"].Value);
        var nextAnnouncementNumber = numberPart + 1;

        var planAnnouncementNumber = GenerateAnnouncementNumber(yearPart, nextAnnouncementNumber);

        return From(planAnnouncementNumber);
    }

    private static string GenerateAnnouncementNumber(string year, int announcementNumber)
    {
        var newPlanAnnouncementNumber = $"AN{year}{announcementNumber:D5}";

        return newPlanAnnouncementNumber;
    }

    [GeneratedRegex(@"^AN(?<year>\d{2})(?<announcement_number>\d{5})$")]
    private static partial Regex PlanAnnouncementNumberRegex();
}

public partial class PlanAnnouncement : AuditableEntity<PlanAnnouncementId>, IHasSoftDelete, IHasActivityInfo
{
    public override PlanAnnouncementId Id { get; init; }

    public PlanAnnouncementNumber PlanAnnouncementNumber { get; init; }

    public string? GroupEgpNumber { get; private set; }

    public string? Telephone { get; private set; }

    public int Year { get; private set; }

    public ParameterCode SupplyMethodCode { get; private set; }

    public DateTimeOffset? DocumentDate { get; private set; }

    public string? Remark { get; private set; }

    public string? AnnouncementTitle { get; private set; }

    public DateTimeOffset? AnnouncementDate { get; private set; }

    public PlanAnnouncementStatus Status { get; private set; }

    public virtual SuParameter SupplyMethodInfo { get; init; }

    public virtual IReadOnlyCollection<PlanAnnouncementAcceptor> Acceptors { get; private set; }

    public virtual IReadOnlyCollection<PlanAnnouncementAssignee> Assignees { get; private set; }

    public virtual IReadOnlyCollection<PlanAnnouncementAttachments> Attachments { get; private set; }

    public virtual IReadOnlyCollection<PlanAnnouncementSelected> AnnouncementSelectedInformations { get; private set; }

    public virtual IReadOnlyCollection<PlanAnnouncementDocumentHistory> DocumentHistories { get; private set; }

    public PlanAnnouncementDocumentHistory? LastedWaitingAcceptorDocument =>
        this.DocumentHistories
            .Where(d =>
                d.DocumentType == PlanAnnouncementDocumentType.Approve &&
                d.StatusState == PlanAnnouncementStatus.WaitingAcceptor &&
                !d.IsReplaced)
            .OrderVersions()
            .FirstOrDefault();

    public PlanAnnouncementDocumentHistory? LastDraftDocument =>
        this.DocumentHistories
            .Where(p =>
                p is { DocumentType: PlanAnnouncementDocumentType.Approve })
            .OrderVersions()
            .FirstOrDefault();

    public PlanAnnouncementDocumentHistory? Document =>
        this.DocumentHistories
            .Where(p =>
                p.DocumentType == PlanAnnouncementDocumentType.Approve)
            .OrderVersions()
            .FirstOrDefault();

    public PlanAnnouncementDocumentHistory? LastedWaitingAcceptorAnnouncementDocument =>
        this.DocumentHistories
            .Where(d =>
                d.DocumentType == PlanAnnouncementDocumentType.Announcement &&
                d.StatusState == PlanAnnouncementStatus.WaitingAcceptor &&
                !d.IsReplaced)
            .OrderVersions()
            .FirstOrDefault();

    public PlanAnnouncementDocumentHistory? LastDraftAnnouncementDocument =>
        this.DocumentHistories
            .Where(p =>
                p is { DocumentType: PlanAnnouncementDocumentType.Announcement })
            .OrderVersions()
            .FirstOrDefault();

    public PlanAnnouncementDocumentHistory? AnnouncementDocument =>
        this.DocumentHistories
            .Where(p =>
                p.DocumentType == PlanAnnouncementDocumentType.Announcement)
            .OrderVersions()
            .FirstOrDefault();

    public Unit AddDocumentHistory(
        PlanAnnouncementDocumentType documentType,
        FileId fileId,
        bool isReplace,
        bool incrementMajor = false)
    {
        var histories = this.DocumentHistories.ToHashSet();

        var existingStatus =
            histories
                .Where(p => p.DocumentType == documentType)
                .Any(p => p.StatusState == this.Status);

        var version = this.DocumentHistories
                          .Where(p => p.DocumentType == documentType)
                          .NextVersion(incrementMajor || !existingStatus);

        histories.Add(
            PlanAnnouncementDocumentHistory.Create(
                documentType,
                this.Status,
                version,
                fileId,
                isReplace));

        this.DocumentHistories = histories;

        return unit;
    }

    public static PlanAnnouncement Create(
        PlanAnnouncementNumber planAnnouncementNumber,
        string? groupEgpNumber,
        int year,
        ParameterCode supplyMethodCode)
    {
        if (year <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(year), "Year must be greater than 0.");
        }

        if (planAnnouncementNumber == null || string.IsNullOrWhiteSpace(planAnnouncementNumber.Value))
        {
            throw new ArgumentException("Plan announcement number cannot be null or empty.", nameof(planAnnouncementNumber));
        }

        var announcement = new PlanAnnouncement
        {
            Id = PlanAnnouncementId.New(),
            PlanAnnouncementNumber = planAnnouncementNumber,
            GroupEgpNumber = groupEgpNumber,
            Year = year,
            SupplyMethodCode = supplyMethodCode,
            Assignees = [],
            Acceptors = [],
            Attachments = [],
            AnnouncementSelectedInformations = [],
            DocumentHistories = [],
        };

        announcement.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Create,
            $"สร้างข้อมูลขออนุมัติเผยแพร่แผนจัดซื้อจัดจ้าง",
            nameof(PlanAnnouncementStatus.Draft)));

        return announcement;
    }

    public PlanAnnouncement SetTelephone(string? telephone)
    {
        this.Telephone = telephone;

        return this;
    }

    public PlanAnnouncement SetEgpGroup(string? groupEgpNumber)
    {
        this.GroupEgpNumber = groupEgpNumber;

        return this;
    }

    public PlanAnnouncement SetDocumentDate(DateTimeOffset? documentDate = null)
    {
        this.DocumentDate = documentDate ?? DateTimeOffset.Now;

        return this;
    }

    public PlanAnnouncement SetAnnouncementTitle(string? announcementTitle)
    {
        this.AnnouncementTitle = announcementTitle;

        return this;
    }

    public PlanAnnouncement SetAnnouncementDate(DateTimeOffset? announcementDate)
    {
        this.AnnouncementDate = announcementDate;

        return this;
    }

    public PlanAnnouncement SetStatus(PlanAnnouncementStatus status, string? remark = null)
    {
        switch (status, this.Status)
        {
            case (PlanAnnouncementStatus.WaitingAssign, PlanAnnouncementStatus.Draft):
            case (PlanAnnouncementStatus.WaitingAssign, PlanAnnouncementStatus.Rejected):
                this.AddActivity(new ActivityInfo(
                    ActivityLogActionTypeConstant.Assigned,
                    $"ยืนยันมอบหมายผู้รับผิดชอบ",
                    status.ToString()));

                break;

            case (PlanAnnouncementStatus.WaitingAcceptor, PlanAnnouncementStatus.WaitingAssign):
                this.AddActivity(new ActivityInfo(
                    ActivityLogActionTypeConstant.SendApprove,
                    $"ส่งผู้มีอำนาจเห็นชอบ/อนุมัติ",
                    status.ToString()));

                break;

            case (PlanAnnouncementStatus.WaitingAcceptor, _):
                this.AddActivity(new ActivityInfo(
                    ActivityLogActionTypeConstant.Approved,
                    $"ผู้มีอำนาจเห็นชอบ/อนุมัติ",
                    status.ToString(),
                    remark));

                break;

            case (PlanAnnouncementStatus.Rejected, PlanAnnouncementStatus.WaitingAcceptor):
                this.AddActivity(new ActivityInfo(
                    ActivityLogActionTypeConstant.Reject,
                    $"ส่งกลับแก้ไข",
                    status.ToString(),
                    remark));

                break;

            case (PlanAnnouncementStatus.WaitingAnnouncement, _):
                this.AddActivity(new ActivityInfo(
                    ActivityLogActionTypeConstant.WaitingAnnouncement,
                    $"รอประกาศเผยแพร่",
                    status.ToString()));

                break;

            case (PlanAnnouncementStatus.Announcement, _):
                this.AddActivity(new ActivityInfo(
                    ActivityLogActionTypeConstant.Announcement,
                    $"ประกาศเผยแพร่",
                    status.ToString()));

                break;
        }

        this.Status = status;

        return this;
    }

    public PlanAnnouncement SetRecall(PlanAnnouncementStatus reqStatus)
    {
        if (this.Status == reqStatus)
        {
            this.AddActivity(
                new ActivityInfo(
                    ActivityLogActionTypeConstant.Update,
                    $"อัปเดตข้อมูลเผยแพร่แผนจัดซื้อจัดจ้าง",
                    this.Status.ToString()));
        }

        if (this.Status != reqStatus)
        {
            this.AddActivity(
                new ActivityInfo(
                    ActivityLogActionTypeConstant.Recall,
                    $"เรียกคืนแก้ไขข้อมูลเผยแพร่แผนจัดซื้อจัดจ้าง",
                    nameof(PlanStatus.EditPlan)));
        }

        this.Status = PlanAnnouncementStatus.WaitingAssign;

        return this;
    }

    public PlanAnnouncement SetRemark(string? remark)
    {
        this.Remark = remark;

        return this;
    }

    public Unit AddAssignee(PlanAnnouncementAssignee assignee)
    {
        if (assignee == null)
        {
            throw new ArgumentNullException(nameof(assignee), "Assignee cannot be null.");
        }

        if (this.Assignees.Contains(assignee))
        {
            throw new InvalidOperationException("Assignee already exists in the plan.");
        }

        var assignees = this.Assignees.ToHashSet();

        assignees.Add(assignee);

        this.Assignees = assignees;

        return unit;
    }

    public Unit RemoveAssigneeById(PlanAnnouncementAssigneeId assigneeId)
    {
        var assignees = this.Assignees.ToHashSet();

        assignees.RemoveWhere(w => w.Id == assigneeId);

        this.Assignees = assignees;

        return unit;
    }

    public Unit AddAcceptor(PlanAnnouncementAcceptor acceptor)
    {
        if (acceptor == null)
        {
            throw new ArgumentNullException(nameof(acceptor), "Acceptor cannot be null.");
        }

        if (this.Acceptors.Contains(acceptor))
        {
            throw new InvalidOperationException("acceptor already exists in the plan.");
        }

        var acceptors = this.Acceptors.ToHashSet();

        acceptors.Add(acceptor);

        this.Acceptors = acceptors;

        return unit;
    }

    public Unit RemoveAcceptorById(AcceptorId acceptorId)
    {
        var acceptors = this.Acceptors.ToHashSet();

        acceptors.RemoveWhere(w => w.Id == acceptorId);

        this.Acceptors = acceptors;

        return unit;
    }

    public Unit AddAttachment(PlanAnnouncementAttachments attachment)
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

        return unit;
    }

    public Unit RemoveAttachmentById(FileId attachmentId)
    {
        var attachments = this.Attachments.ToHashSet();

        attachments.RemoveWhere(w => w.Id == attachmentId);

        this.Attachments = attachments;

        return unit;
    }

    public Unit AddPlanAnnouncementSelected(PlanAnnouncementSelected planSelected)
    {
        var announcementSelectedInformations = this.AnnouncementSelectedInformations.ToHashSet();

        announcementSelectedInformations.Add(planSelected);

        this.AnnouncementSelectedInformations = announcementSelectedInformations;

        return unit;
    }

    public Unit RemovePlanAnnouncementSelectedById(PlanAnnouncementSelectedId deleteId)
    {
        var announcementSelectedInformations = this.AnnouncementSelectedInformations.ToHashSet();

        announcementSelectedInformations.RemoveWhere(w => w.Id == deleteId);

        this.AnnouncementSelectedInformations = announcementSelectedInformations;

        return unit;
    }
}

public class PlanAnnouncementAttachments : AuditableEntity<FileId>
{
    public override FileId Id { get; init; }

    public ParameterCode DocumentTypeCode { get; private set; }

    public string FileName { get; private init; }

    public bool IsPublic { get; private init; }

    public int Sequence { get; private set; }

    public virtual SuParameter DocumentType { get; init; }

    public static PlanAnnouncementAttachments Create(
        ParameterCode documentTypeCode,
        FileId fileId,
        string fileName,
        int sequence,
        bool isPublic)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name cannot be null or empty.", nameof(fileName));
        }

        return new PlanAnnouncementAttachments
        {
            Id = fileId,
            Sequence = sequence,
            DocumentTypeCode = documentTypeCode,
            FileName = fileName,
            IsPublic = isPublic,
        };
    }
}