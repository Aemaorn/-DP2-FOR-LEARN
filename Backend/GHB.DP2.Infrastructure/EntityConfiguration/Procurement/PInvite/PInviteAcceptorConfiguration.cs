namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PInvite;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PInvite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PInviteAcceptorConfiguration : EntityTypeConfigurationBase<PInviteAcceptors, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PInviteAcceptors> builder)
    {
        builder.ToTable(nameof(PInviteAcceptors), nameof(Procurement));

        builder.Property(m => m.IsUnableToPerformDuties)
               .IsRequired();

        builder.HasOne(m => m.CommitteePosition)
               .WithMany()
               .HasForeignKey(m => m.CommitteePositionsCode)
               .HasPrincipalKey(m => m.Code)
               .OnDelete(DeleteBehavior.Restrict);

        builder.AcceptorInfo();
        builder.OwnsAuditInfo();
        builder.HasSoftDelete();
    }
}