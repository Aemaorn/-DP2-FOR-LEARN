namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PInvite;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PInvite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PInvitedEntrepreneurConfiguration : EntityTypeConfigurationBase<PInvitedEntrepreneurs, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PInvitedEntrepreneurs> builder)
    {
        builder.ToTable(nameof(PInvitedEntrepreneurs), nameof(Procurement));

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id)
               .HasVogenConversion()
               .IsRequired();

        builder.Property(i => i.EmailSend)
               .IsRequired();

        builder.Property(i => i.Sequence)
               .IsRequired();

        builder.Property(i => i.WatchlistResult);

        builder.Property(i => i.WatchlistResultRemark);

        builder.Property(i => i.WatchlistResultAt);

        builder.Property(i => i.CoiResult);

        builder.Property(i => i.CoiResultRemark);

        builder.Property(i => i.CoiResultAt);

        builder.Property(i => i.EgpResult);

        builder.Property(i => i.EgpResultRemark);

        builder.Property(i => i.EgpResultAt);

        builder.Property(i => i.Email);

        builder.Property(i => i.EmailTemplate);

        builder.HasMany(m => m.InvitedEntrepreneurShareholders)
               .WithOne(d => d.InvitedEntrepreneur)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(ie => ie.InvitedEntrepreneurCheckers)
               .WithOne(c => c.InvitedEntrepreneur)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Attachments)
               .WithOne()
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.DocumentHistories)
               .WithOne(d => d.InvitedEntrepreneur)
               .HasForeignKey(d => d.PInvitedEntrepreneursId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.OwnsAuditInfo();
        builder.HasSoftDelete();
    }
}