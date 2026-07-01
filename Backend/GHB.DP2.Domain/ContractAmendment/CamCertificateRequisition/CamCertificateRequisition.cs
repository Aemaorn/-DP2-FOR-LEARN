namespace GHB.DP2.Domain.ContractAmendment.CamCertificateRequisition;

using System.Text.RegularExpressions;
using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.SystemUtility;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct CamCertificateRequisitionId
{
    public static CamCertificateRequisitionId New() => From(Guid.CreateVersion7());
}

public enum CamCertificateRequisitionStatus
{
    /// <summary>
    /// แบบร่าง
    /// </summary>
    Draft,

    /// <summary>
    /// รอคณะกรรมการตรวจรับอนุมัติ
    /// </summary>
    WaitingForCommitteeApproval,

    /// <summary>
    /// อนุมัติแล้ว
    /// </summary>
    Approved,

    /// <summary>
    /// ตีกลับ
    /// </summary>
    Rejected,

    /// <summary>
    /// เรียกคืนแก้ไข
    /// </summary>
    Edit,

    /// <summary>
    /// ยกเลิก
    /// </summary>
    Cancelled,
}

[ValueObject<string>(Conversions.EfCoreValueConverter)]
public partial struct CertificateNumber
{
    public static string GetCertificateNumberYearPrefix()
    {
        int buddhistYear = DateTime.UtcNow.Year + 543;

        var yearPrefix = (buddhistYear % 100).ToString("D2");

        return $"CERT{yearPrefix}";
    }

    public static CertificateNumber New()
    {
        var prefix = GetCertificateNumberYearPrefix();

        var newCertificateNumber = $"{prefix}00001";

        return From(newCertificateNumber);
    }

    public CertificateNumber Next()
    {
        if (string.IsNullOrWhiteSpace(this.Value))
        {
            throw new InvalidOperationException("CertificateNumber number cannot be null or empty.");
        }

        if (!Regex.IsMatch(this.Value, @"^CERT\d{7}$", RegexOptions.None, TimeSpan.FromMilliseconds(100)))
        {
            throw new FormatException("Invalid CertificateNumber format.");
        }

        string yearPart = this.Value.Substring(4, 2);
        string numberPart = this.Value.Substring(6);

        if (!int.TryParse(yearPart, out var year) || !int.TryParse(numberPart, out var number))
        {
            throw new FormatException("Invalid CertificateNumber format.");
        }

        number++;

        var newCertificateNumber = $"CERT{year}{number:D5}";

        return From(newCertificateNumber);
    }
}

public record CertificateRequisitionInfo(
    CertificateNumber CertificateNo,
    DateTime? ReceiveDate,
    string? SbsDocumentNo,
    DateTimeOffset? DocumentDate,
    DateTimeOffset? IssuedDate,
    string? RequestReason,
    string? EntrepreneurName,
    SuVendorId? EntrepreneurId,
    string? EntrepreneurEmail,
    string? ContractNumber,
    string? PoNumber,
    decimal? Budget,
    string? ContractName,
    DateTimeOffset? ContractSignedDate,
    DateTimeOffset? DeliveryDate,
    DateTimeOffset? ContractEndDate,
    bool? IsManual,
    ParameterCode? SupplyMethodCode = null,
    ParameterCode? SupplyMethodTypeCode = null,
    ParameterCode? SupplyMethodSpecialTypeCode = null);

public partial class CamCertificateRequisition : AuditableEntity<CamCertificateRequisitionId>, IHasSoftDelete, IHasActivityInfo
{
    public override CamCertificateRequisitionId Id { get; init; }

    public ContractDraftVendorId? ContractDraftVendorId { get; init; }

    public CertificateNumber CertificateNo { get; init; }

    public DateTimeOffset? ReceiveDate { get; init; }

    public string? SbsDocumentNo { get; private set; }

    public DateTimeOffset? DocumentDate { get; private set; }

    public DateTimeOffset? IssuedDate { get; private set; }

    public string? RequestReason { get; private set; }

    public string? EntrepreneurName { get; private set; }

    public SuVendorId? EntrepreneurId { get; private set; }

    public string? EntrepreneurEmail { get; private set; }

    public string? ContractNumber { get; private set; }

    public string? PoNumber { get; private set; }

    public decimal? Budget { get; private set; }

    public string? ContractName { get; private set; }

    public DateTimeOffset? ContractSignedDate { get; private set; }

    public DateTimeOffset? DeliveryDate { get; private set; }

    public DateTimeOffset? ContractEndDate { get; private set; }

    public bool? IsManual { get; private set; }

    /// <summary>วิธีจัดหา</summary>
    public ParameterCode? SupplyMethodCode { get; private set; }

    public ParameterCode? SupplyMethodTypeCode { get; private set; }

    public ParameterCode? SupplyMethodSpecialTypeCode { get; private set; }

    public virtual SuParameter? SupplyMethod { get; init; }

    public virtual SuParameter? SupplyMethodType { get; init; }

    public virtual SuParameter? SupplyMethodSpecialType { get; init; }

    public CamCertificateRequisitionStatus Status { get; private set; }

    public virtual CaContractDraftVendor? ContractDraftVendor { get; init; }

    public virtual IReadOnlyCollection<CamCertificateRequisitionAcceptor> Acceptors { get; set; }

    public virtual IReadOnlyCollection<CamCertificateRequisitionDocumentHistory> DocumentHistories { get; private set; }

    public virtual IReadOnlyCollection<CamCertificateRequisitionAttachment> Attachments { get; private set; }

    public CamCertificateRequisitionDocumentHistory? LastedDocumentHistory =>
        this.DocumentHistories
            .OrderVersions()
            .FirstOrDefault();

    public CamCertificateRequisitionDocumentHistory? LastedReplacedDocument =>
        this.DocumentHistories
            .Where(d => d.IsReplaced)
            .OrderVersions()
            .FirstOrDefault();

    public static CamCertificateRequisition Create(
        ContractDraftVendorId? contractDraftVendorId,
        CertificateRequisitionInfo req)
    {
        var entity = new CamCertificateRequisition
        {
            Id = CamCertificateRequisitionId.New(),
            ContractDraftVendorId = contractDraftVendorId,
            CertificateNo = req.CertificateNo,
            ReceiveDate = req.ReceiveDate,
            SbsDocumentNo = req.SbsDocumentNo,
            DocumentDate = req.DocumentDate,
            IssuedDate = req.IssuedDate,
            RequestReason = req.RequestReason,
            IsManual = req.IsManual,
            SupplyMethodCode = req.SupplyMethodCode,
            SupplyMethodTypeCode = req.SupplyMethodTypeCode,
            SupplyMethodSpecialTypeCode = req.SupplyMethodSpecialTypeCode,
            EntrepreneurName = req.EntrepreneurName,
            EntrepreneurId = req.EntrepreneurId,
            EntrepreneurEmail = req.EntrepreneurEmail,
            ContractNumber = req.ContractNumber,
            PoNumber = req.PoNumber,
            Budget = req.Budget,
            ContractName = req.ContractName,
            ContractSignedDate = req.ContractSignedDate,
            DeliveryDate = req.DeliveryDate,
            ContractEndDate = req.ContractEndDate,
            DocumentHistories = [],
            Attachments = [],
        };

        entity.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.Create,
                $"สร้างข้อมูลใบรองรับผลงาน",
                nameof(CamCertificateRequisitionStatus.Draft)));

        return entity;
    }

    public CamCertificateRequisition UpdateInfo(CertificateRequisitionInfo req)
    {
        this.SbsDocumentNo = req.SbsDocumentNo;
        this.DocumentDate = req.DocumentDate;
        this.IssuedDate = req.IssuedDate;
        this.RequestReason = req.RequestReason;
        this.EntrepreneurName = req.EntrepreneurName;
        this.EntrepreneurId = req.EntrepreneurId;
        this.EntrepreneurEmail = req.EntrepreneurEmail;
        this.ContractNumber = req.ContractNumber;
        this.PoNumber = req.PoNumber;
        this.Budget = req.Budget;
        this.ContractName = req.ContractName;
        this.ContractSignedDate = req.ContractSignedDate;
        this.DeliveryDate = req.DeliveryDate;
        this.ContractEndDate = req.ContractEndDate;
        this.SupplyMethodCode = req.SupplyMethodCode;
        this.SupplyMethodTypeCode = req.SupplyMethodTypeCode;
        this.SupplyMethodSpecialTypeCode = req.SupplyMethodSpecialTypeCode;

        this.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.Update,
                $"อัปเดตข้อมูลใบรองรับผลงาน",
                this.Status.ToString()));

        return this;
    }

    public CamCertificateRequisition AddAcceptor(CamCertificateRequisitionAcceptor acceptor)
    {
        var acceptors = (this.Acceptors ?? []).ToHashSet();

        acceptors.Add(acceptor);

        this.Acceptors = acceptors;

        return this;
    }

    public CamCertificateRequisition RemoveAcceptor(CamCertificateRequisitionAcceptor acceptor)
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

    public CamCertificateRequisition AddAttachment(CamCertificateRequisitionAttachment attachment)
    {
        var attachments = (this.Attachments ?? []).ToHashSet();

        attachments.Add(attachment);

        this.Attachments = attachments;

        return this;
    }

    public CamCertificateRequisition RemoveAttachment(CamCertificateRequisitionAttachment attachment)
    {
        var attachments = this.Attachments.ToHashSet();

        attachments.Remove(attachment);

        this.Attachments = attachments;

        return this;
    }

    public CamCertificateRequisition AddDocumentHistory(FileId fileId, bool isReplace = false)
    {
        var documentHistories = this.DocumentHistories.ToHashSet();

        var version =
            this.DocumentHistories.NextVersion();

        var newDocumentHistory =
            CamCertificateRequisitionDocumentHistory.Create(
                this.Status,
                version,
                fileId,
                isReplace);

        documentHistories.Add(newDocumentHistory);

        this.DocumentHistories = documentHistories;

        return this;
    }

    public CamCertificateRequisition UpdateStatus(CamCertificateRequisitionStatus status)
    {
        if (this.Status == status)
        {
            this.AddActivity(
                new ActivityInfo(
                    ActivityLogActionTypeConstant.Update,
                    ActivityLogActionTypeConstant.Update,
                    this.Status.ToString()));
        }

        this.Status = status;

        switch (status)
        {
            case CamCertificateRequisitionStatus.WaitingForCommitteeApproval:
                this.AddActivity(
                    new ActivityInfo(
                        ActivityLogActionTypeConstant.SendCommitteeApprove,
                        $"ส่งฝ่ายเห็นชอบ เห็นชอบ/อนุมัติ",
                        nameof(CamCertificateRequisitionStatus.WaitingForCommitteeApproval)));

                this.SetAcceptorsToPending();

                break;

            case CamCertificateRequisitionStatus.Edit:
                this.SetAcceptorsToDraft();

                this.AddActivity(
                    new ActivityInfo(
                        ActivityLogActionTypeConstant.Recall,
                        $"เรียกคืนแก้ไขข้อมูลใบรองรับผลงาน",
                        nameof(CamCertificateRequisitionStatus.Edit)));

                break;
        }

        return this;
    }

    private void SetAcceptorsToPending()
    {
        _ = this.Acceptors
                .Where(a => a is
                {
                    IsActive: true,
                    IsUnableToPerformDuties: false,
                })
                .Iter(a => a.Pending());
    }

    private void SetAcceptorsToDraft()
    {
        _ = this.Acceptors
                .Where(a => a is
                {
                    IsActive: true,
                    IsUnableToPerformDuties: false,
                })
                .Iter(a => a.Draft());
    }

    public CamCertificateRequisition SetRejected()
    {
        this.Status = CamCertificateRequisitionStatus.Rejected;

        return this;
    }

    public CamCertificateRequisition SetDeleted()
    {
        this.IsDeleted = true;

        return this;
    }

    public bool HasMajorityRejection()
    {
        if (this.Status != CamCertificateRequisitionStatus.WaitingForCommitteeApproval)
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