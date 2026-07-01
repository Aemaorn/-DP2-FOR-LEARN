namespace GHB.DP2.Domain.ContractManagement.CmContractTermination;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.SystemUtility;
using LanguageExt;
using Vogen;

public enum CmContractTerminationStatus
{
    /// <summary>
    /// แบบร่าง
    /// </summary>
    Draft,

    /// <summary>
    /// อยู่ระหว่าง คกก. เห็นชอบ
    /// </summary>
    WaitingCommitteeApproval,

    /// <summary>
    /// รอ จพ. มอบหมาย
    /// </summary>
    WaitingAssign,

    /// <summary>
    /// ส่งกลับแก้ไข
    /// </summary>
    Rejected,

    /// <summary>
    /// อยู่ระหว่าง จพ. ให้ความเห็น
    /// </summary>
    WaitingComment,

    /// <summary>
    /// รออนุมัติ
    /// </summary>
    WaitingApproval,

    /// <summary>
    /// อนุมัติ
    /// </summary>
    Approved,

    /// <summary>
    /// ส่งกลับแก้ไข
    /// </summary>
    RejectToAssignee,
}

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct CmContractTerminationId
{
    public static CmContractTerminationId New => From(Guid.CreateVersion7());
}

public partial class CmContractTermination : AuditableEntity<CmContractTerminationId>, IHasSoftDelete, IHasActivityInfo
{
    public override CmContractTerminationId Id { get; init; }

    public ContractDraftVendorId ContractDraftVendorId { get; set; }

    public ParameterCode? TerminateType { get; private set; }

    public string? TerminateReasonOther { get; private set; }

    public string? TerminateReason { get; private set; }

    public DateTimeOffset? TerminateDate { get; private set; }

    public DateTimeOffset? DocumentDate { get; private set; }

    public CmContractTerminationStatus Status { get; private set; }

    public bool IsProposedApprover { get; private set; }

    public string? TerminateReasonDetail { get; private set; }

    public virtual CaContractDraftVendor CaContractDraftVendor { get; init; }

    public virtual IReadOnlyCollection<CmContractTerminationAcceptor> Acceptors { get; private set; }

    public virtual IReadOnlyCollection<CmContractTerminationAssignee> Assignees { get; private set; }

    public virtual IReadOnlyCollection<CmContractTerminationDocumentHistory> DocumentHistories { get; private set; }

    public virtual IReadOnlyCollection<CmContractTerminationAttachment> Attachments { get; private set; }

    public virtual SuParameter? TerminateTypeNavigation { get; private set; }

    public CmContractTerminationDocumentHistory? LastedDraftOrRejectedDocument =>
        this.DocumentHistories
            .Where(x => x.StatusState is CmContractTerminationStatus.Draft or CmContractTerminationStatus.Rejected)
            .OrderVersions()
            .FirstOrDefault();

    public CmContractTerminationDocumentHistory? LastedWaitingApprovalDocument =>
        this.DocumentHistories
            .Where(x => x.StatusState == CmContractTerminationStatus.WaitingApproval)
            .Where(x => x.IsReplaced)
            .OrderVersions()
            .FirstOrDefault();

    public CmContractTerminationDocumentHistory? LastedWaitingAssignDocument =>
        this.DocumentHistories
            .Where(x => x.StatusState == CmContractTerminationStatus.WaitingAssign)
            .Where(x => x.IsReplaced)
            .OrderVersions()
            .FirstOrDefault();

    public CmContractTerminationDocumentHistory? LastedWaitingCommentDocument =>
        this.DocumentHistories
            .Where(x => x.StatusState == CmContractTerminationStatus.WaitingComment)
            .OrderVersions()
            .FirstOrDefault();

    public Unit AddDocumentHistory(
        FileId fileId,
        bool isReplace = false,
        bool incrementMajor = false)
    {
        var histories = this.DocumentHistories.ToHashSet();

        var version = this.DocumentHistories
                          .NextVersion(incrementMajor);

        histories.Add(
            CmContractTerminationDocumentHistory.Create(
                CmContractTerminationDocumentType.ContractTermination,
                histories.Any() ? this.Status : CmContractTerminationStatus.Draft,
                version,
                fileId,
                isReplace));

        this.DocumentHistories = histories;

        return unit;
    }

    private static string _defaultTerminateReasonDetail = @"ข้าพเจ้าในฐานะ.................. มีความประสงค์ขอเลิกสัญญา....................และขอให้ท่านชำระเงินจำนวน......................บาท (................................) ซึ่งได้รวมภาษีมูลค่าเพิ่ม ภายในกำหนด ......... (.........) วัน นับแต่วันที่ท่านได้รับหนังสือฉบับนี้ โดยชำระให้แก่.................หากท่านไม่ชำระภายในกำหนดระยะเวลาดังกล่าว ให้ถือเอาหนังสือฉบับนี้เป็นหนังสือบอกเลิกสัญญาและให้สัญญาเช่าสิ้นสุดเมื่อครบกำหนดระยะเวลาดังกล่าว โดยท่านมีหน้าที่ต้องส่งมอบทรัพย์สินคืนให้แก่..........................ในสภาพเรียบร้อยใช้การได้ดี หากไม่ส่งมอบทรัพย์สิน ................................มีสิทธิเข้าครอบครองทรัพย์สินได้ทันที นอกจากนี้ท่านยังต้องรับผิดชำระค่าใช้ทรัพย์ ค่าขาดประโยชน์ในอัตราเดือนละ.................บาท (......................) ค่าเสียหาย และค่าเสื่อมราคาให้แก่.............มิฉะนั้น ข้าพเจ้ามีความจำเป็นที่จะต้องดำเนินการตามกฎหมายกับท่านต่อไป";

    public static CmContractTermination Create(
        ContractDraftVendorId contractDraftVendorId)
    {
        var newData = new CmContractTermination
        {
            Id = CmContractTerminationId.New,
            ContractDraftVendorId = contractDraftVendorId,
            IsProposedApprover = false,
            TerminateReasonDetail = _defaultTerminateReasonDetail,
            DocumentHistories = [],
            Acceptors = [],
            Assignees = [],
            Attachments = [],
        };

        newData.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.Create,
                ActivityLogActionTypeConstant.Create,
                nameof(CmContractTerminationStatus.Draft)));

        return newData;
    }

    public CmContractTermination SetValues(
        ParameterCode? terminateType,
        string? terminateReason,
        DateTimeOffset? terminateDate)
    {
        this.TerminateType = terminateType;
        this.TerminateReason = terminateReason;
        this.TerminateDate = terminateDate;

        return this;
    }

    public CmContractTermination SetTerminateReasonDetail(string? terminateReasonDetail)
    {
        this.TerminateReasonDetail = terminateReasonDetail;

        return this;
    }

    public CmContractTermination SetTerminateReasonOther(string? terminateReasonOther)
    {
        this.TerminateReasonOther = terminateReasonOther;

        return this;
    }

    public CmContractTermination SetIsProposedApprover(bool isProposedApprover)
    {
        this.IsProposedApprover = isProposedApprover;

        return this;
    }

    public CmContractTermination AddAttachment(CmContractTerminationAttachment attachment)
    {
        var attachments = (this.Attachments ?? []).ToHashSet();

        if (attachments.Any(a => a.Id == attachment.Id))
        {
            throw new InvalidOperationException("Attachment with the same Id already exists.");
        }

        attachments.Add(attachment);

        this.Attachments = attachments;

        return this;
    }

    public CmContractTermination RemoveAttachment(CmContractTerminationAttachment attachment)
    {
        if (attachment == null)
        {
            throw new ArgumentNullException(nameof(attachment));
        }

        var attachments = this.Attachments.ToHashSet();

        if (!attachments.Remove(attachment))
        {
            throw new InvalidOperationException("Attachment not found.");
        }

        this.Attachments = attachments;

        return this;
    }

    public CmContractTermination SetStatus(CmContractTerminationStatus status, string? remark = null)
    {
        var oldStatus = this.Status;

        this.Status = status;

        switch (status)
        {
            case CmContractTerminationStatus.WaitingCommitteeApproval:
                this.AddActivity(
                    new ActivityInfo(
                        ActivityLogActionTypeConstant.SendCommitteeApprove,
                        ActivityLogActionTypeConstant.SendCommitteeApprove,
                        this.Status.ToString()));

                break;

            case CmContractTerminationStatus.WaitingAssign:
                this.AddActivity(
                    new ActivityInfo(
                        ActivityLogActionTypeConstant.WaitingAssign,
                        ActivityLogActionTypeConstant.WaitingAssign,
                        this.Status.ToString()));

                break;

            case CmContractTerminationStatus.WaitingComment:
                this.AddActivity(
                    new ActivityInfo(
                        ActivityLogActionTypeConstant.WaitingComment,
                        ActivityLogActionTypeConstant.WaitingComment,
                        this.Status.ToString()));

                if (oldStatus != this.Status)
                {
                    this.Assignees
                        .Iter(r =>
                        {
                            r.ResetAction();
                        });
                }

                break;

            case CmContractTerminationStatus.WaitingApproval:
                this.AddActivity(
                    new ActivityInfo(
                        ActivityLogActionTypeConstant.SendApprove,
                        ActivityLogActionTypeConstant.SendApprove,
                        this.Status.ToString()));

                break;

            case CmContractTerminationStatus.RejectToAssignee:
                this.AddActivity(
                    new ActivityInfo(
                        ActivityLogActionTypeConstant.Reject,
                        ActivityLogActionTypeConstant.Reject,
                        this.Status.ToString(),
                        remark));

                break;
        }

        return this;
    }

    public CmContractTermination AddAcceptor(CmContractTerminationAcceptor acceptor)
    {
        var acceptors = this.Acceptors?.ToList() ?? new List<CmContractTerminationAcceptor>();
        acceptor.SetActive();
        acceptors.Add(acceptor);
        this.Acceptors = acceptors;

        return this;
    }

    public CmContractTermination RemoveAcceptor(CmContractTerminationAcceptor acceptor)
    {
        var list = this.Acceptors?.ToList() ?? new List<CmContractTerminationAcceptor>();
        list.Remove(acceptor);
        this.Acceptors = list;

        return this;
    }

    public CmContractTermination AddAssignee(CmContractTerminationAssignee assignee)
    {
        var assignees = this.Assignees?.ToList() ?? new List<CmContractTerminationAssignee>();
        assignees.Add(assignee);
        this.Assignees = assignees;

        return this;
    }

    public CmContractTermination RemoveAssignee(CmContractTerminationAssignee assign)
    {
        var assignees = this.Assignees?.ToList() ?? new List<CmContractTerminationAssignee>();
        assignees.Remove(assign);
        this.Assignees = assignees;

        return this;
    }

    public CmContractTermination SetStatusAssignee()
    {
        this.Status = CmContractTerminationStatus.WaitingAssign;

        return this;
    }

    public bool HasMajorityRejection()
    {
        if (this.Status != CmContractTerminationStatus.WaitingCommitteeApproval)
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
}