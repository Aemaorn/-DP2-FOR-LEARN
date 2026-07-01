namespace GHB.DP2.Infrastructure.EntityConfiguration.ContractAgreement.CaContractDraft;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class CaContractDraftVendorShareholdersCheckerConfiguration : EntityTypeConfigurationBase<CaContractDraftVendorShareholderChecker>
{
    protected override void EntityConfigure(EntityTypeBuilder<CaContractDraftVendorShareholderChecker> builder)
    {
        builder.ToTable(nameof(CaContractDraftVendorShareholderChecker), nameof(ContractAgreement));

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
               .HasVogenConversion();

        builder.Property(e => e.CheckType)
               .HasConversion(new EnumToStringConverter<QualificationType>());

        builder.Property(e => e.ResultAt);

        builder.Property(e => e.Result)
               .HasConversion(new EnumToStringConverter<QualificationResult>());

        builder.Property(e => e.Remark);

        builder.Property(e => e.CreatedBy)
               .IsRequired();

        builder.Property(e => e.CreatedByName)
               .IsRequired();

        builder.Property(e => e.CreatedAt)
               .IsRequired();
    }
}