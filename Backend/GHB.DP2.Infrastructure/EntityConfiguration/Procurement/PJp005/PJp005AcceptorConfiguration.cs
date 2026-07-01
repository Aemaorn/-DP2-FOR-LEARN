namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PJp005;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PJp005;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PpTorDraftAcceptorConfiguration : EntityTypeConfigurationBase<PJp005Acceptors, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PJp005Acceptors> builder)
    {
        builder.ToTable(nameof(PJp005Acceptors), nameof(Procurement));

        builder.Property(m => m.PJp005Id)
               .HasVogenConversion()
               .IsRequired();

        builder.HasOne(m => m.PJp005)
               .WithMany(m => m.Acceptors)
               .HasForeignKey(m => m.PJp005Id)
               .OnDelete(DeleteBehavior.Cascade);

        builder.AcceptorInfo();
        builder.OwnsAuditInfo();
        builder.HasSoftDelete();
    }
}