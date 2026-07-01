namespace GHB.DP2.Domain.ContractAmendment.ContractAmendment;

using System.Text.RegularExpressions;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using Vogen;

public enum CamContractAmendmentStatus
{
    /// <summary>
    /// แบบร่าง
    /// </summary>
    Draft,

    InProgress,

    Completed,
}

public enum CmContractAmendmentType
{
    ChangeContractDetails,
    AppendNewPurchaseOrder,
    WaiveOrReducePenalty,
    AdjustContractDuration,
}

public enum CmContractAmendmentPoStep
{
    PoAddendum,
    PoSap,
}

[ValueObject<string>(Conversions.EfCoreValueConverter)]
public partial struct CamContractAmendmentNumber
{
    public static string GetCertificateNumberYearPrefix()
    {
        int buddhistYear = DateTime.UtcNow.Year + 543;

        var yearPrefix = (buddhistYear % 100).ToString("D2");

        return $"CAM{yearPrefix}";
    }

    public static CamContractAmendmentNumber New()
    {
        var prefix = GetCertificateNumberYearPrefix();

        var newCertificateNumber = $"{prefix}00001";

        return From(newCertificateNumber);
    }

    public CamContractAmendmentNumber Next()
    {
        if (string.IsNullOrWhiteSpace(this.Value))
        {
            throw new InvalidOperationException("CamContractAmendmentNumber number cannot be null or empty.");
        }

        if (!Regex.IsMatch(this.Value, @"^CAM\d{7}$", RegexOptions.None, TimeSpan.FromMilliseconds(100)))
        {
            throw new FormatException("Invalid CertificateNumber format.");
        }

        string yearPart = this.Value.Substring(4, 2);
        string numberPart = this.Value.Substring(6);

        if (!int.TryParse(yearPart, out var year) || !int.TryParse(numberPart, out var number))
        {
            throw new FormatException("Invalid CamContractAmendmentNumber format.");
        }

        number++;

        var newCertificateNumber = $"CAM{year}{number:D5}";

        return From(newCertificateNumber);
    }
}

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct CamContractAmendmentId
{
    public static CamContractAmendmentId New() => From(Guid.CreateVersion7());
}

public partial class CamContractAmendment : AuditableEntity<CamContractAmendmentId>, IHasSoftDelete
{
    public override CamContractAmendmentId Id { get; init; }

    public CamContractAmendmentNumber CamContractAmendmentNumber { get; init; }

    public ContractDraftVendorId ContractDraftVendorId { get; init; }

    public CmContractAmendmentType Type { get; init; }

    public CmContractAmendmentPoStep? Step { get; private set; }

    public string? Remark { get; init; }

    public CamContractAmendmentStatus Status { get; private set; }

    public virtual CaContractDraftVendor ContractDraftVendor { get; init; }

    public virtual CamContractAmendmentPoAddendum.CamContractAmendmentPoAddendum PoAddendum { get; init; }

    public virtual CamContractAmendmentPoSap.CamContractAmendmentPoSap PoSap { get; init; }

    public virtual CamContractAmendmentWaiveOrReducePenalty.CamContractAmendmentWaiveOrReducePenalty WaiveOrReducePenalty { get; init; }

    public virtual CamContractAmendmentExtendChange.CamContractAmendmentExtendChange ExtendChange { get; init; }

    public virtual IReadOnlyCollection<CamContractAmendmentAttachment> Attachments { get; private set; }

    public static CamContractAmendment Create(
        ContractDraftVendorId contractDraftVendorId,
        CmContractAmendmentType type,
        string? remark)
    {
        var newData = new CamContractAmendment
        {
            Id = CamContractAmendmentId.New(),
            ContractDraftVendorId = contractDraftVendorId,
            CamContractAmendmentNumber = CamContractAmendmentNumber.New(),
            Type = type,
            Remark = remark,
            Status = CamContractAmendmentStatus.Draft,
            Attachments = [],
        };

        if (type is CmContractAmendmentType.AppendNewPurchaseOrder)
        {
            newData = newData.SetPoStep(CmContractAmendmentPoStep.PoAddendum);
        }

        return newData;
    }

    public CamContractAmendment SetPoStep(CmContractAmendmentPoStep step)
    {
        this.Step = step;

        return this;
    }

    public CamContractAmendment SetStatus(CamContractAmendmentStatus status)
    {
        this.Status = status;

        return this;
    }

    public CamContractAmendment AddAttachment(CamContractAmendmentAttachment attachment)
    {
        var attachments = this.Attachments?.ToHashSet() ?? [];

        attachments.Add(attachment);
        this.Attachments = attachments;

        return this;
    }

    public CamContractAmendment RemoveAttachment(CamContractAmendmentAttachment attachment)
    {
        var attachments = this.Attachments?.ToHashSet() ?? [];

        attachments.Remove(attachment);
        this.Attachments = attachments;

        return this;
    }
}