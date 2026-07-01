namespace GHB.DP2.Infrastructure.EntityConfiguration.ContractAgreement.CaContractDraft;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class CaContractDraftVendorCheckerConfiguration : EntityTypeConfigurationBase<CaContractDraftVendorChecker>
{
    protected override void EntityConfigure(EntityTypeBuilder<CaContractDraftVendorChecker> builder)
    {
        builder.ToTable(nameof(CaContractDraftVendorChecker), nameof(ContractAgreement));

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
               .HasVogenConversion()
               .IsRequired();

        builder.Property(e => e.CheckType)
               .HasConversion(new EnumToStringConverter<QualificationType>())
               .IsRequired();

        builder.Property(e => e.ResultAt)
               .IsRequired();

        builder.Property(e => e.Result)
               .HasConversion(new EnumToStringConverter<QualificationResult>())
               .IsRequired();

        builder.Property(e => e.Remark);

        builder.Property(e => e.CreatedBy)
               .IsRequired();

        builder.Property(e => e.CreatedByName)
               .IsRequired();

        builder.Property(e => e.CreatedAt)
               .IsRequired();
    }
}