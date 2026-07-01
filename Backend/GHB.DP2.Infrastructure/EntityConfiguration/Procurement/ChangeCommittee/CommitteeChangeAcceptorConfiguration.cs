namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.ChangeCommittee;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.ChangeCommittee;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class CommitteeChangeAcceptorConfiguration : EntityTypeConfigurationBase<CommitteeChangeAcceptor, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<CommitteeChangeAcceptor> builder)
    {
        builder.ToTable(nameof(CommitteeChangeAcceptor), "Procurement");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.CommitteeChangeId)
               .HasVogenConversion();

        builder.Property(m => m.IsUnableToPerformDuties)
               .IsRequired();

        builder.HasOne(m => m.CommitteePosition)
               .WithMany()
               .HasForeignKey(m => m.CommitteePositionsCode)
               .HasPrincipalKey(m => m.Code)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.CommitteeChange)
               .WithMany(c => c.Acceptors)
               .HasForeignKey(a => a.CommitteeChangeId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.AcceptorInfo();
        builder.OwnsAuditInfo();
        builder.HasSoftDelete();
    }
}