namespace GHB.DP2.Domain.Common;

using System.ComponentModel.DataAnnotations;
using LanguageExt;

public enum QualificationType
{
    [Display(Name = "COI")]
    COI,

    [Display(Name = "Watchlist")]
    Watchlist,
}

public enum QualificationResult
{
    [Display(Name = "Pass")]
    Pass,

    [Display(Name = "Fail")]
    Fail,

    [Display(Name = "UnKnow")]
    UnKnow,
}

public interface IHasVendorQualificationCheckerInfo
{
    public QualificationType CheckType { get; init; }

    public QualificationResult Result { get; set; }

    public DateTimeOffset ResultAt { get; set; }

    [MaxLength(2000)]
    public string? Remark { get; set; }

    public Guid CreatedBy { get; set; }

    public string CreatedByName { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public static abstract IHasVendorQualificationCheckerInfo Create(
        QualificationType checkType,
        QualificationResult result,
        DateTimeOffset resultAt,
        string? remark);

    public Unit Create(Guid userId, string name);
}