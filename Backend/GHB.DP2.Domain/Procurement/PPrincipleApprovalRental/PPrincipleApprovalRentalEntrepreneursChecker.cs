namespace GHB.DP2.Domain.Procurement.PPrincipleApprovalRental;

using GHB.DP2.Domain.Common;
using LanguageExt;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PPrincipleApprovalRentalEntrepreneursCheckerId
{
    public static PPrincipleApprovalRentalEntrepreneursCheckerId New() => From(Guid.CreateVersion7());
}

public class PPrincipleApprovalRentalEntrepreneursChecker :
    IHasVendorQualificationCheckerInfo
{
    public PPrincipleApprovalRentalEntrepreneursCheckerId Id { get; init; }

    public virtual PPrincipleApprovalRentalEntrepreneurs PrincipleApprovalRentalEntrepreneursI { get; init; }

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
        return new PPrincipleApprovalRentalEntrepreneursChecker
        {
            Id = PPrincipleApprovalRentalEntrepreneursCheckerId.New(),
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