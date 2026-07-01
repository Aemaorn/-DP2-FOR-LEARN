namespace GHB.DP2.Domain.AnnouncementInfo;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct AnnouncementInfoId
{
    public static AnnouncementInfoId New() => From(Guid.CreateVersion7());
}

public enum AnnouncementInfoStatus
{
    /// <summary>
    /// Publish
    /// </summary>
    Publish,
}

public partial class AnnouncementInfo : AuditableEntity<AnnouncementInfoId>, IHasSoftDelete, IHasActivityInfo
{
    public override AnnouncementInfoId Id { get; init; }

    public int? OldId { get; protected init; }

    public string? AnnouncementName { get; protected set; }

    public string? AnnouncementTitle { get; protected set; }

    public string? Email { get; protected set; }

    public AnnouncementInfoStatus Status { get; private set; }

    public DateTimeOffset? AnnouncementDate { get; protected set; }

    public ParameterCode? SupplyMethodCode { get; private set; }

    public virtual SuParameter? SupplyMethod { get; init; }

    public decimal? BudgetAmount { get; protected set; }

    public ParameterCode? AnnouncementCategoryCode { get; protected set; }

    public virtual SuParameter? AnnouncementCategory { get; init; }

    public string? Description { get; protected set; }

    public FileId? DocumentId { get; protected set; }

    public string? DocumentName { get; protected set; }

    public string? DocumentUrl { get; protected set; }

    public DateTimeOffset? ExpectedDate { get; protected set; }

    public DateTimeOffset? StartDate { get; protected set; }

    public DateTimeOffset? EndDate { get; protected set; }

    public decimal? ReferencePrice { get; protected set; }

    public int? BudgetYear { get; private set; }

    public string? Remark { get; protected set; }

    public bool? IsDp { get; protected set; }

    public bool? IsActive { get; protected set; }

    public AnnouncementInfo Update(
        string? announcementTitle,
        string? announcementName,
        DateTimeOffset announcementDate,
        decimal budgetAmount,
        string? announcementCategoryCode = null,
        string? supplyMethodCode = null,
        int? budgetYear = null,
        string? remark = null,
        string? description = null,
        DateTimeOffset? expectedDate = null,
        decimal? referencePrice = null,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null,
        Guid? documentId = null,
        string? documentName = null)
    {
        this.AnnouncementTitle = announcementTitle;
        this.AnnouncementName = announcementName;
        this.AnnouncementDate = announcementDate;
        this.BudgetAmount = budgetAmount;
        this.AnnouncementCategoryCode = announcementCategoryCode is not null ? ParameterCode.From(announcementCategoryCode) : null;
        this.SupplyMethodCode = supplyMethodCode is not null ? ParameterCode.From(supplyMethodCode) : null;
        this.BudgetYear = budgetYear;
        this.Remark = remark;
        this.Description = description;
        this.ExpectedDate = expectedDate;
        this.ReferencePrice = referencePrice;
        this.StartDate = startDate;
        this.EndDate = endDate;
        this.DocumentId = documentId.HasValue ? FileId.From(documentId.Value) : null;
        this.DocumentName = documentName;
        return this;
    }

    public static AnnouncementInfo Create(
        string? announcementTitle,
        string? announcementName,
        DateTimeOffset announcementDate,
        decimal budgetAmount,
        string? announcementCategoryCode = null,
        string? supplyMethodCode = null,
        int? budgetYear = null,
        string? remark = null,
        string? description = null,
        DateTimeOffset? expectedDate = null,
        decimal? referencePrice = null,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null,
        Guid? documentId = null,
        string? documentName = null)
    {
        return new AnnouncementInfo
        {
            Id = AnnouncementInfoId.New(),
            AnnouncementTitle = announcementTitle,
            AnnouncementName = announcementName,
            AnnouncementDate = announcementDate,
            BudgetAmount = budgetAmount,
            AnnouncementCategoryCode = announcementCategoryCode is not null ? ParameterCode.From(announcementCategoryCode) : null,
            SupplyMethodCode = supplyMethodCode is not null ? ParameterCode.From(supplyMethodCode) : null,
            BudgetYear = budgetYear,
            Remark = remark,
            Description = description,
            ExpectedDate = expectedDate,
            ReferencePrice = referencePrice,
            StartDate = startDate,
            EndDate = endDate,
            IsActive = true,
            IsDp = true,
            DocumentId = documentId.HasValue ? FileId.From(documentId.Value) : null,
            DocumentName = documentName,
        };
    }

    public AnnouncementInfo ImportUpdate(
        string? announcementTitle,
        string? announcementName,
        DateTimeOffset? announcementDate,
        decimal? budgetAmount,
        AnnouncementInfoStatus status,
        string? supplyMethodCode,
        string? email,
        string? announcementCategoryCode = null,
        string? documentUrl = null,
        int? budgetYear = null,
        string? remark = null,
        string? description = null,
        DateTimeOffset? expectedDate = null,
        decimal? referencePrice = null,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null,
        string? documentName = null)
    {
        this.AnnouncementTitle = announcementTitle;
        this.AnnouncementName = announcementName;
        this.AnnouncementDate = announcementDate;
        this.BudgetAmount = budgetAmount;
        this.Status = status;
        this.AnnouncementCategoryCode = announcementCategoryCode is not null ? ParameterCode.From(announcementCategoryCode) : null;
        this.SupplyMethodCode = supplyMethodCode is not null ? ParameterCode.From(supplyMethodCode) : null;
        this.Email = email;
        this.DocumentUrl = documentUrl;
        this.DocumentName = documentName;
        this.BudgetYear = budgetYear;
        this.Remark = remark;
        this.Description = description;
        this.ExpectedDate = expectedDate;
        this.ReferencePrice = referencePrice;
        this.StartDate = startDate;
        this.EndDate = endDate;
        return this;
    }

    public static AnnouncementInfo Import(
        int? oldId,
        string? announcementTitle,
        string? announcementName,
        DateTimeOffset? announcementDate,
        decimal? budgetAmount,
        AnnouncementInfoStatus status,
        string? supplyMethodCode,
        string? email,
        string? announcementCategoryCode = null,
        Guid? documentId = null,
        string? documentUrl = null,
        int? budgetYear = null,
        string? remark = null,
        string? description = null,
        DateTimeOffset? expectedDate = null,
        decimal? referencePrice = null,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null,
        string? documentName = null)
    {
        return new AnnouncementInfo
        {
            Id = AnnouncementInfoId.New(),
            OldId = oldId,
            AnnouncementTitle = announcementTitle,
            AnnouncementName = announcementName,
            AnnouncementDate = announcementDate,
            AnnouncementCategoryCode = announcementCategoryCode is not null ? ParameterCode.From(announcementCategoryCode) : null,
            BudgetAmount = budgetAmount,
            Status = status,
            SupplyMethodCode = supplyMethodCode is not null ? ParameterCode.From(supplyMethodCode) : null,
            Email = email,
            DocumentId = documentId.HasValue ? FileId.From(documentId.Value) : null,
            DocumentUrl = documentUrl,
            DocumentName = documentName,
            BudgetYear = budgetYear,
            Remark = remark,
            Description = description,
            ExpectedDate = expectedDate,
            ReferencePrice = referencePrice,
            StartDate = startDate,
            EndDate = endDate,
            IsActive = true,
            IsDp = false,
        };
    }
}
