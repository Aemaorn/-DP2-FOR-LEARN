namespace GHB.DP2.Infrastructure.EntityConfiguration.ContractAmendment.CamContractAmendment;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class CamContractAmendmentConfiguration : EntityTypeConfigurationBase<CamContractAmendment, Dp2DbContext>
{
    protected override void EntityConfigure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<CamContractAmendment> builder)
    {
        builder.ToTable(nameof(CamContractAmendment), nameof(Domain.ContractAmendment.ContractAmendment));

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
               .HasVogenConversion();

        builder.Property(p => p.CamContractAmendmentNumber)
               .IsRequired()
               .HasVogenConversion();

        builder.Property(p => p.Type)
               .HasConversion(new EnumToStringConverter<CmContractAmendmentType>())
               .IsRequired();

        builder.Property(p => p.Remark);

        builder.Property(p => p.Status)
               .HasConversion(new EnumToStringConverter<CamContractAmendmentStatus>())
               .IsRequired();

        builder.Property(p => p.Step)
               .HasConversion(new EnumToStringConverter<CmContractAmendmentPoStep>());

        builder.HasOne(x => x.PoAddendum)
               .WithOne(x => x.CamContractAmendment)
               .HasForeignKey<Domain.ContractAmendment.ContractAmendment.CamContractAmendmentPoAddendum.CamContractAmendmentPoAddendum>(x => x.CamContractAmendmentId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ContractDraftVendor)
               .WithMany()
               .HasForeignKey(x => x.ContractDraftVendorId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.OwnsAuditInfo();
        builder.HasSoftDelete();
    }
}