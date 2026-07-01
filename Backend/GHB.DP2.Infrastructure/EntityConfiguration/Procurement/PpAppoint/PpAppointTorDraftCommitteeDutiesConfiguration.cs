namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PpAppoint;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PpAppoint;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PpAppointTorDraftCommitteeDutiesConfiguration : EntityTypeConfigurationBase<PpAppointTorDraftCommitteeDuties, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PpAppointTorDraftCommitteeDuties> builder)
    {
        builder.ToTable(nameof(PpAppointTorDraftCommitteeDuties), nameof(Procurement));

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion()
               .ValueGeneratedNever();

        builder.Property(p => p.Description)
               .IsRequired();

        builder.Property(p => p.Sequence);

        builder.HasSoftDelete();
        builder.OwnsAuditInfo();
    }
}