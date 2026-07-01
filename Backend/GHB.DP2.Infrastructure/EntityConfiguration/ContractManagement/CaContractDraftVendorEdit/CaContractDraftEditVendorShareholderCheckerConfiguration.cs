namespace GHB.DP2.Infrastructure.EntityConfiguration.ContractManagement.CaContractDraftVendorEdit;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractManagement.CaContractDraftVendorEdit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class CaContractDraftEditVendorShareholderCheckerConfiguration : EntityTypeConfigurationBase<CaContractDraftEditVendorShareholderChecker>
{
    protected override void EntityConfigure(EntityTypeBuilder<CaContractDraftEditVendorShareholderChecker> builder)
    {
        builder.ToTable(nameof(CaContractDraftEditVendorShareholderChecker), nameof(ContractManagement));

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
