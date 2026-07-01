namespace GHB.DP2.Domain.AnnouncementInfo;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct AnnouncementReportId
{
    public static AnnouncementReportId New() => From(Guid.CreateVersion7());
}

public partial class AnnouncementReport : AuditableEntity<AnnouncementReportId>, IHasSoftDelete, IHasActivityInfo
{
    public override AnnouncementReportId Id { get; init; }

    public int? OldId { get; protected set; }

    public int? Year { get; protected set; }

    public string? Discretion { get; protected set; }

    public ParameterCode? AnnouncementReportTypeCode { get; protected set; }

    public virtual SuParameter? AnnouncementCategory { get; init; }

    public bool? IsActive { get; protected set; }

    public bool? IsDp { get; protected set; }

    public FileId? DocumentId { get; protected set; }

    public string? DocumentName { get; protected set; }

    public string? DocumentUrl { get; protected set; }

    public static AnnouncementReport Create(
        int? year,
        string? discretion,
        string? reportTypeCode,
        Guid? documentId = null,
        string? documentName = null,
        string? documentUrl = null)
    {
        return new AnnouncementReport
        {
            Id = AnnouncementReportId.New(),
            Year = year,
            Discretion = discretion,
            AnnouncementReportTypeCode = reportTypeCode is not null ? ParameterCode.From(reportTypeCode) : null,
            IsActive = true,
            IsDp = true,
            DocumentId = documentId.HasValue ? FileId.From(documentId.Value) : null,
            DocumentName = documentName,
            DocumentUrl = documentUrl,
        };
    }

    public static AnnouncementReport Import(
        int? oldId,
        int? year,
        string? discretion,
        string? reportTypeCode,
        string? documentUrl = null)
    {
        return new AnnouncementReport
        {
            Id = AnnouncementReportId.New(),
            OldId = oldId,
            Year = year,
            Discretion = discretion,
            AnnouncementReportTypeCode = reportTypeCode is not null ? ParameterCode.From(reportTypeCode) : null,
            IsActive = true,
            IsDp = true,
            DocumentUrl = documentUrl,
        };
    }

    public AnnouncementReport ImportUpdate(
        int? year,
        string? discretion,
        string? reportTypeCode,
        string? documentUrl = null)
    {
        this.Year = year;
        this.Discretion = discretion;
        this.AnnouncementReportTypeCode = reportTypeCode is not null ? ParameterCode.From(reportTypeCode) : null;
        this.DocumentUrl = documentUrl;
        return this;
    }

    public AnnouncementReport Update(
        string? discretion,
        Guid? documentId = null,
        string? documentName = null,
        string? documentUrl = null)
    {
        this.Discretion = discretion;
        this.DocumentId = documentId.HasValue ? FileId.From(documentId.Value) : null;
        this.DocumentName = documentName;
        this.DocumentUrl = documentUrl;
        return this;
    }
}
