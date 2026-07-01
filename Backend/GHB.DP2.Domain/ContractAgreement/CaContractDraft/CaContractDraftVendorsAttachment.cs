namespace GHB.DP2.Domain.ContractAgreement.CaContractDraft;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct ContractDraftVendorsAttachmentId
{
    public static ContractDraftVendorsAttachmentId New() => From(Guid.CreateVersion7());
}

/// <summary>
/// เอกสารอันเป็นส่วนหนึ่งของสัญญา
/// </summary>
public partial class CaContractDraftVendorsAttachment : AuditableEntity<ContractDraftVendorsAttachmentId>, IHasSoftDelete
{
    public override ContractDraftVendorsAttachmentId Id { get; init; }

    public ParameterCode TypeCode { get; private set; }

    public string? Description { get; private set; }

    public int? PageNumber { get; private set; }

    public int Sequence { get; private set; }

    public string? FormatOtherName { get; private set; }

    public virtual CaContractDraftVendor ContractDraftVendor { get; init; }

    public virtual SuParameter Type { get; init; }

    public virtual IReadOnlyCollection<CaContractDraftVendorsAttachmentFile> Files { get; private set; }

    public CaContractDraftVendorsAttachment AddFile(CaContractDraftVendorsAttachmentFile file)
    {
        var files = this.Files.ToHashSet();

        files.Add(file);

        this.Files = files;

        return this;
    }

    public CaContractDraftVendorsAttachment RemoveFile(CaContractDraftVendorsAttachmentFile file)
    {
        var files = this.Files.ToHashSet();

        if (files.Remove(file))
        {
            this.Files = files;
        }

        return this;
    }

    public CaContractDraftVendorsAttachment ClearAllFiles()
    {
        this.Files = new List<CaContractDraftVendorsAttachmentFile>();

        return this;
    }

    public CaContractDraftVendorsAttachment SetTypeCode(ParameterCode typeCode)
    {
        this.TypeCode = typeCode;

        return this;
    }

    public CaContractDraftVendorsAttachment SetDescription(string? description)
    {
        this.Description = description;

        return this;
    }

    public CaContractDraftVendorsAttachment SetPageNumber(int? pageNumber)
    {
        this.PageNumber = pageNumber;

        return this;
    }

    public CaContractDraftVendorsAttachment SetSequence(int sequence)
    {
        this.Sequence = sequence;

        return this;
    }

    public CaContractDraftVendorsAttachment SetFormatOtherName(string? formatOtherName)
    {
        this.FormatOtherName = formatOtherName;

        return this;
    }

    public static CaContractDraftVendorsAttachment Create(
        ParameterCode typeCode,
        string? description,
        int? pageNumber,
        int sequence,
        string? formatOtherName)
    {
        return new CaContractDraftVendorsAttachment
        {
            Id = ContractDraftVendorsAttachmentId.New(),
            TypeCode = typeCode,
            Description = description,
            PageNumber = pageNumber,
            Sequence = sequence,
            Files = [],
            FormatOtherName = formatOtherName,
        };
    }

    public static CaContractDraftVendorsAttachment Create(
        Guid id,
        ParameterCode typeCode,
        string? description,
        int? pageNumber,
        int sequence,
        string? formatOtherName)
    {
        return new CaContractDraftVendorsAttachment
        {
            Id = ContractDraftVendorsAttachmentId.From(id),
            TypeCode = typeCode,
            Description = description,
            PageNumber = pageNumber,
            Sequence = sequence,
            Files = [],
            FormatOtherName = formatOtherName,
        };
    }
}