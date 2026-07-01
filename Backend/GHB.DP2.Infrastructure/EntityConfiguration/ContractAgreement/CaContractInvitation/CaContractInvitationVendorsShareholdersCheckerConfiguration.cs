namespace GHB.DP2.Infrastructure.EntityConfiguration.ContractAgreement.CaContractInvitation;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractInvitation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class CaContractInvitationVendorsShareholdersCheckerConfiguration : EntityTypeConfigurationBase<CaContractInvitationVendorShareholderChecker>
{
    protected override void EntityConfigure(EntityTypeBuilder<CaContractInvitationVendorShareholderChecker> builder)
    {
        builder.ToTable(nameof(CaContractInvitationVendorShareholderChecker), nameof(ContractAgreement));

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