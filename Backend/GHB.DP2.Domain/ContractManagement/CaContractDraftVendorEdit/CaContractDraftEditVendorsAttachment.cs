namespace GHB.DP2.Domain.ContractManagement.CaContractDraftVendorEdit;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct ContractDraftEditVendorsAttachmentId
{
    public static ContractDraftEditVendorsAttachmentId New() => From(Guid.CreateVersion7());
}

/// <summary>
/// เอกสารอันเป็นส่วนหนึ่งของสัญญา (Edit)
/// </summary>
public partial class CaContractDraftEditVendorsAttachment : AuditableEntity<ContractDraftEditVendorsAttachmentId>, IHasSoftDelete
{
    public override ContractDraftEditVendorsAttachmentId Id { get; init; }

    public ParameterCode TypeCode { get; private set; }

    public string? Description { get; private set; }

    public int? PageNumber { get; private set; }

    public int Sequence { get; private set; }

    public string? FormatOtherName { get; private set; }

    public virtual CaContractDraftVendorEdit CaContractDraftVendorEdit { get; init; }

    public virtual SuParameter Type { get; init; }

    public virtual IReadOnlyCollection<CaContractDraftEditVendorsAttachmentFile> Files { get; private set; }

    public CaContractDraftEditVendorsAttachment AddFile(CaContractDraftEditVendorsAttachmentFile file)
    {
        var files = this.Files.ToHashSet();

        files.Add(file);

        this.Files = files;

        return this;
    }

    public CaContractDraftEditVendorsAttachment RemoveFile(CaContractDraftEditVendorsAttachmentFile file)
    {
        var files = this.Files.ToHashSet();

        if (files.Remove(file))
        {
            this.Files = files;
        }

        return this;
    }

    public CaContractDraftEditVendorsAttachment ClearAllFiles()
    {
        this.Files = new List<CaContractDraftEditVendorsAttachmentFile>();

        return this;
    }

    public CaContractDraftEditVendorsAttachment SetTypeCode(ParameterCode typeCode)
    {
        this.TypeCode = typeCode;

        return this;
    }

    public CaContractDraftEditVendorsAttachment SetDescription(string? description)
    {
        this.Description = description;

        return this;
    }

    public CaContractDraftEditVendorsAttachment SetPageNumber(int? pageNumber)
    {
        this.PageNumber = pageNumber;

        return this;
    }

    public CaContractDraftEditVendorsAttachment SetSequence(int sequence)
    {
        this.Sequence = sequence;

        return this;
    }

    public CaContractDraftEditVendorsAttachment SetFormatOtherName(string? formatOtherName)
    {
        this.FormatOtherName = formatOtherName;

        return this;
    }

    public static CaContractDraftEditVendorsAttachment Create(
        ParameterCode typeCode,
        string? description,
        int? pageNumber,
        int sequence,
        string? formatOtherName)
    {
        return new CaContractDraftEditVendorsAttachment
        {
            Id = ContractDraftEditVendorsAttachmentId.New(),
            TypeCode = typeCode,
            Description = description,
            PageNumber = pageNumber,
            Sequence = sequence,
            Files = [],
            FormatOtherName = formatOtherName,
        };
    }

    public static CaContractDraftEditVendorsAttachment Create(
        Guid id,
        ParameterCode typeCode,
        string? description,
        int? pageNumber,
        int sequence,
        string? formatOtherName)
    {
        return new CaContractDraftEditVendorsAttachment
        {
            Id = ContractDraftEditVendorsAttachmentId.From(id),
            TypeCode = typeCode,
            Description = description,
            PageNumber = pageNumber,
            Sequence = sequence,
            Files = [],
            FormatOtherName = formatOtherName,
        };
    }
}
