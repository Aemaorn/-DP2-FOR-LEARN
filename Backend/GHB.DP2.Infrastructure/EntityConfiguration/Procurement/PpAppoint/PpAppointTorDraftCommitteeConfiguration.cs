namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PpAppoint;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PpAppoint;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PpAppointTorDraftCommitteeConfiguration : EntityTypeConfigurationBase<PpAppointTorDraftCommittee, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PpAppointTorDraftCommittee> builder)
    {
        builder.ToTable(nameof(PpAppointTorDraftCommittee), nameof(Procurement));

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion()
               .ValueGeneratedNever();

        builder.Property(p => p.SuUserId)
               .HasVogenConversion()
               .IsRequired();

        builder.Property(p => p.FullName)
               .IsRequired();

        builder.Property(p => p.FullPositionName)
               .IsRequired();

        builder.HasOne(m => m.CommitteePositions)
               .WithMany()
               .HasForeignKey(m => m.CommitteePositionsCode)
               .HasPrincipalKey(m => m.Code)
               .OnDelete(DeleteBehavior.Restrict);

        builder.Property(p => p.CommitteePositionsName)
               .IsRequired();

        builder.Property(p => p.Sequence)
               .IsRequired();

        builder.HasSoftDelete();
        builder.OwnsAuditInfo();
    }
}