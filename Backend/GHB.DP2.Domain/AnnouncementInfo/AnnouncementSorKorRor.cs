namespace GHB.DP2.Domain.AnnouncementInfo;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct AnnouncementSorKorRorId
{
    public static AnnouncementSorKorRorId New() => From(Guid.CreateVersion7());
}

public partial class AnnouncementSorKorRor : AuditableEntity<AnnouncementSorKorRorId>, IHasSoftDelete, IHasActivityInfo
{
    public override AnnouncementSorKorRorId Id { get; init; }

    public int? OldId { get; protected set; }

    public int? Year { get; protected set; }

    public int? Month { get; protected set; }

    public decimal? Amount { get; protected set; }

    public ParameterCode? DepartmentTypeCode { get; protected set; }

    public virtual SuParameter? DepartmentType { get; init; }

    public bool? IsDp { get; protected set; }

    public bool? IsActive { get; protected set; }

    public FileId? DocumentId { get; protected set; }

    public string? DocumentName { get; protected set; }

    public string? DocumentUrl { get; protected set; }

    public static AnnouncementSorKorRor Create(
        int? year,
        int? month,
        decimal? amount,
        string? departmentTypeCode,
        Guid? documentId = null,
        string? documentName = null,
        string? documentUrl = null)
    {
        return new AnnouncementSorKorRor
        {
            Id = AnnouncementSorKorRorId.New(),
            Year = year,
            Month = month,
            Amount = amount,
            DepartmentTypeCode = departmentTypeCode is not null ? ParameterCode.From(departmentTypeCode) : null,
            IsActive = true,
            IsDp = true,
            DocumentId = documentId.HasValue ? FileId.From(documentId.Value) : null,
            DocumentName = documentName,
            DocumentUrl = documentUrl,
        };
    }

    public static AnnouncementSorKorRor Import(
        int? oldId,
        int? year,
        int? month,
        decimal? amount,
        string? departmentTypeCode,
        string? documentUrl = null)
    {
        return new AnnouncementSorKorRor
        {
            Id = AnnouncementSorKorRorId.New(),
            OldId = oldId,
            Year = year,
            Month = month,
            Amount = amount,
            DepartmentTypeCode = departmentTypeCode is not null ? ParameterCode.From(departmentTypeCode) : null,
            IsActive = true,
            IsDp = true,
            DocumentUrl = documentUrl,
        };
    }

    public AnnouncementSorKorRor ImportUpdate(
        int? year,
        int? month,
        decimal? amount,
        string? departmentTypeCode,
        string? documentUrl = null)
    {
        this.Year = year;
        this.Month = month;
        this.Amount = amount;
        this.DepartmentTypeCode = departmentTypeCode is not null ? ParameterCode.From(departmentTypeCode) : null;
        this.DocumentUrl = documentUrl;
        return this;
    }

    public AnnouncementSorKorRor Update(
        int? year,
        int? month,
        decimal? amount,
        string? departmentTypeCode,
        Guid? documentId = null,
        string? documentName = null,
        string? documentUrl = null)
    {
        this.Year = year;
        this.Month = month;
        this.Amount = amount;
        this.DepartmentTypeCode = departmentTypeCode is not null ? ParameterCode.From(departmentTypeCode) : null;
        this.DocumentId = documentId.HasValue ? FileId.From(documentId.Value) : null;
        this.DocumentName = documentName;
        this.DocumentUrl = documentUrl;
        return this;
    }
}
