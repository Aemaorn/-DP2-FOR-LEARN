namespace GHB.DP2.Domain.Procurement.PInvite;

using GHB.DP2.Domain.Common;
using LanguageExt;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PInvitedEntrepreneurShareholderCheckerId
{
    public static PInvitedEntrepreneurShareholderCheckerId New() => From(Guid.CreateVersion7());
}

public class PInvitedEntrepreneurShareholderChecker :
    IHasVendorQualificationCheckerInfo
{
    public PInvitedEntrepreneurShareholderCheckerId Id { get; init; }

    public virtual PInvitedEntrepreneurShareholders InvitedEntrepreneurShareholder { get; init; }

    public QualificationType CheckType { get; init; }

    public QualificationResult Result { get; set; }

    public DateTimeOffset ResultAt { get; set; }

    public string? Remark { get; set; }

    public Guid CreatedBy { get; set; }

    public string CreatedByName { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public static IHasVendorQualificationCheckerInfo Create(
        QualificationType checkType,
        QualificationResult result,
        DateTimeOffset resultAt,
        string? remark)
    {
        return new PInvitedEntrepreneurShareholderChecker
        {
            Id = PInvitedEntrepreneurShareholderCheckerId.New(),
            CheckType = checkType,
            Result = result,
            ResultAt = resultAt,
            Remark = remark,
        };
    }

    public Unit Create(Guid userId, string name)
    {
        this.CreatedByName = name;
        this.CreatedBy = userId;
        this.CreatedAt = DateTime.UtcNow;

        return unit;
    }
}