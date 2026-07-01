namespace GHB.DP2.Domain.ContractAmendment.ContractAmendment.CamContractAmendmentPoAddendum;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using LanguageExt;
using Vogen;

public enum CamContractAmendmentPoAddendumStatus
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

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct CamContractAmendmentPoAddendumId
{
    public static CamContractAmendmentPoAddendumId New() => From(Guid.CreateVersion7());
}

public partial class CamContractAmendmentPoAddendum : AuditableEntity<CamContractAmendmentPoAddendumId>, IHasActivityInfo
{
    public override CamContractAmendmentPoAddendumId Id { get; init; }

    public CamContractAmendmentId CamContractAmendmentId { get; private set; }

    public string ContractNumber { get; private set; }

    public string? PoNumber { get; private set; }

    public string SapNumber { get; private set; }

    public CamContractAmendmentPoAddendumStatus Status { get; private set; }

    public virtual SuVendor Vendor { get; private set; }

    public virtual CamContractAmendment CamContractAmendment { get; init; }

    public virtual IReadOnlyCollection<CamContractAmendmentPoAddendumDocumentHistory> DocumentHistories { get; private set; }

    public virtual IReadOnlyCollection<CamContractAmendmentPoAddendumAcceptor> Acceptors { get; private set; }

    public virtual IReadOnlyCollection<CamContractAmendmentPoAddendumAssignee> Assignees { get; private set; }

    public virtual IReadOnlyCollection<CamContractAmendmentPoAddendumPaymentTerm> PaymentTerms { get; private set; }

    public CamContractAmendmentPoAddendumDocumentHistory? LastedContractAddendumDocument =>
        this.DocumentHistories
            .Where(d => d.DocumentType == CamContractAmendmentPoAddendumDocumentType.ContractAddendum)
            .OrderVersions()
            .FirstOrDefault();

    public CamContractAmendmentPoAddendumDocumentHistory? LastedContractAmendmentRequestDocument =>
        this.DocumentHistories
            .Where(d => d.DocumentType == CamContractAmendmentPoAddendumDocumentType.ContractAmendmentRequest)
            .OrderVersions()
            .FirstOrDefault();

    public CamContractAmendmentPoAddendumDocumentHistory? LastedContractAddendumDraftDocument =>
        this.DocumentHistories
            .Where(d => d.StatusState == CamContractAmendmentPoAddendumStatus.Draft)
            .Where(d => d.DocumentType == CamContractAmendmentPoAddendumDocumentType.ContractAddendum)
            .OrderVersions()
            .FirstOrDefault();

    public CamContractAmendmentPoAddendumDocumentHistory? LastedContractAddendumWaitingCommitteeApprovalDocument =>
        this.DocumentHistories
            .Where(d => d.StatusState == CamContractAmendmentPoAddendumStatus.WaitingCommitteeApproval)
            .Where(d => d.DocumentType == CamContractAmendmentPoAddendumDocumentType.ContractAddendum)
            .Where(x => !x.IsReplaced)
            .OrderVersions()
            .FirstOrDefault();

    public CamContractAmendmentPoAddendumDocumentHistory? LastedContractAmendmentRequestDraftDocument =>
        this.DocumentHistories
            .Where(d => d.StatusState == CamContractAmendmentPoAddendumStatus.Draft)
            .Where(d => d.DocumentType == CamContractAmendmentPoAddendumDocumentType.ContractAmendmentRequest)
            .OrderVersions()
            .FirstOrDefault();

    public CamContractAmendmentPoAddendumDocumentHistory? LastedContractAmendmentRequestWaitingCommitteeApprovalDocument =>
        this.DocumentHistories
            .Where(d => d.StatusState == CamContractAmendmentPoAddendumStatus.WaitingCommitteeApproval)
            .Where(d => d.DocumentType == CamContractAmendmentPoAddendumDocumentType.ContractAmendmentRequest)
            .Where(x => !x.IsReplaced)
            .OrderVersions()
            .FirstOrDefault();

    public Unit AddDocumentHistory(
        CamContractAmendmentPoAddendumDocumentType documentType,
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
            CamContractAmendmentPoAddendumDocumentHistory.Create(
                documentType,
                this.Status,
                version,
                fileId,
                isReplace));

        this.DocumentHistories = histories;

        return unit;
    }

    public static CamContractAmendmentPoAddendum Create(
        CamContractAmendmentId camContractAmendmentId,
        string contractNumber,
        string sapNumber,
        string? poNumber,
        SuVendor vendor)
    {
        var entity = new CamContractAmendmentPoAddendum
        {
            Id = CamContractAmendmentPoAddendumId.New(),
            Status = CamContractAmendmentPoAddendumStatus.Draft,
            SapNumber = sapNumber,
            ContractNumber = contractNumber,
            PoNumber = poNumber,
            Vendor = vendor,
            CamContractAmendmentId = camContractAmendmentId,
            DocumentHistories = [],
            PaymentTerms = [],
            Acceptors = [],
            Assignees = [],
        };

        entity.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.Create,
                "สร้างข้อมูล",
                CamContractAmendmentPoAddendumStatus.Draft.ToString()));

        return entity;
    }

    public CamContractAmendmentPoAddendum AddAcceptor(CamContractAmendmentPoAddendumAcceptor acceptor)
    {
        var acceptors = this.Acceptors?.ToList() ?? new List<CamContractAmendmentPoAddendumAcceptor>();
        acceptors.Add(acceptor);
        this.Acceptors = acceptors;

        return this;
    }

    public CamContractAmendmentPoAddendum RemoveAcceptor(CamContractAmendmentPoAddendumAcceptor acceptor)
    {
        var list = this.Acceptors?.ToList() ?? new List<CamContractAmendmentPoAddendumAcceptor>();
        list.Remove(acceptor);
        this.Acceptors = list;

        return this;
    }

    public CamContractAmendmentPoAddendum AddAssignee(CamContractAmendmentPoAddendumAssignee assignee)
    {
        var assignees = this.Assignees?.ToList() ?? new List<CamContractAmendmentPoAddendumAssignee>();
        assignees.Add(assignee);
        this.Assignees = assignees;

        return this;
    }

    public CamContractAmendmentPoAddendum RemoveAssignee(CamContractAmendmentPoAddendumAssignee assign)
    {
        var assignees = this.Assignees?.ToList() ?? new List<CamContractAmendmentPoAddendumAssignee>();
        assignees.Remove(assign);
        this.Assignees = assignees;

        return this;
    }

    public CamContractAmendmentPoAddendum AddPaymentTerm(CamContractAmendmentPoAddendumPaymentTerm paymentTerm)
    {
        var paymentTerms = this.PaymentTerms?.ToList() ?? new List<CamContractAmendmentPoAddendumPaymentTerm>();
        paymentTerms.Add(paymentTerm);
        this.PaymentTerms = paymentTerms;

        return this;
    }

    public CamContractAmendmentPoAddendum RemovePaymentTerm(CamContractAmendmentPoAddendumPaymentTerm paymentTerm)
    {
        var paymentTerms = this.PaymentTerms?.ToList() ?? new List<CamContractAmendmentPoAddendumPaymentTerm>();
        paymentTerms.Remove(paymentTerm);
        this.PaymentTerms = paymentTerms;

        return this;
    }

    public CamContractAmendmentPoAddendum SetStatus(CamContractAmendmentPoAddendumStatus status)
    {
        this.Status = status;

        return this;
    }

    public CamContractAmendmentPoAddendum SetValues(string contractNumber, string sapNumber, string? poNumber, SuVendor vendor)
    {
        this.ContractNumber = contractNumber;
        this.SapNumber = sapNumber;
        this.PoNumber = poNumber;
        this.Vendor = vendor;

        return this;
    }

    public CamContractAmendmentPoAddendum SetEdit()
    {
        this.Status = CamContractAmendmentPoAddendumStatus.Edit;

        this.Acceptors
            .Iter(a => a.Draft());

        return this;
    }

    public CamContractAmendmentPoAddendum SetWaitingCommitteeApproval()
    {
        this.Assignees
            .Iter(a => a.Pending());

        this.Status = CamContractAmendmentPoAddendumStatus.WaitingCommitteeApproval;

        this.Acceptors
            .Where(p => p.Type != AcceptorType.AcceptanceCommittee)
            .Iter(a => a.Draft());

        this.Acceptors
            .Where(p => p.Type == AcceptorType.AcceptanceCommittee && !p.IsUnableToPerformDuties)
            .Iter(a => a.Pending());

        return this;
    }

    public CamContractAmendmentPoAddendum SetWaitingAssigned()
    {
        this.Status = CamContractAmendmentPoAddendumStatus.WaitingAssigned;
        this.Assignees.Iter(r => r.Pending());

        return this;
    }

    public CamContractAmendmentPoAddendum SetWaitingComment()
    {
        this.Status = CamContractAmendmentPoAddendumStatus.WaitingComment;

        _ = this.Assignees
                .Where(p => p.Type == AssigneeType.Director)
                .Iter(p => p.Assigned());

        return this;
    }

    public CamContractAmendmentPoAddendum SetWaitingApproval()
    {
        this.Status = CamContractAmendmentPoAddendumStatus.WaitingApproval;

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

    public CamContractAmendmentPoAddendum SetApproval()
    {
        this.Status = CamContractAmendmentPoAddendumStatus.Approved;
        this.CamContractAmendment.SetPoStep(CmContractAmendmentPoStep.PoSap);

        return this;
    }

    public bool HasMajorityRejection()
    {
        if (this.Status != CamContractAmendmentPoAddendumStatus.WaitingCommitteeApproval)
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

    public CamContractAmendmentPoAddendum SetRejected(string? remark)
    {
        this.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Reject,
            $"ส่งกลับแก้ไข",
            CamContractAmendmentPoAddendumStatus.Rejected.ToString(),
            remark));

        this.Status = CamContractAmendmentPoAddendumStatus.Rejected;

        return this;
    }
}