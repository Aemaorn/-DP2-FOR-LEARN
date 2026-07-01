namespace GHB.DP2.Infrastructure.EntityConfiguration.ContractAmendment.CamContractAmendment.CamContractAmendmentPoSap;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment.CamContractAmendmentPoSap;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class CamContractAmendmentPoSapConfiguration : EntityTypeConfigurationBase<CamContractAmendmentPoSap, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<CamContractAmendmentPoSap> builder)
    {
        builder.ToTable(nameof(CamContractAmendmentPoSap), nameof(Domain.ContractAmendment.ContractAmendment));

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
               .HasVogenConversion();

        builder.Property(p => p.CamContractAmendmentId)
               .HasVogenConversion()
               .IsRequired();

        builder.Property(p => p.PoSapNumber);

        builder.Property(p => p.Status)
               .HasConversion(new EnumToStringConverter<CamContractAmendmentPoSapStatus>())
               .IsRequired();

        builder.HasOne(x => x.CamContractAmendment)
               .WithOne(x => x.PoSap)
               .HasForeignKey<CamContractAmendmentPoSap>(x => x.CamContractAmendmentId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Acceptors)
               .WithOne(x => x.CamContractAmendmentPoSap)
               .OnDelete(DeleteBehavior.Cascade);

        builder.OwnsAuditInfo();
        builder.HasActivityInfo();
    }
}