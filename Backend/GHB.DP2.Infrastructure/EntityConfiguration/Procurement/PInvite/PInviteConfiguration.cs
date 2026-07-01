namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PInvite;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PInvite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class PInviteConfiguration : EntityTypeConfigurationBase<PInvite, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PInvite> builder)
    {
        builder.ToTable(nameof(PInvite), nameof(Procurement));

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id)
               .HasVogenConversion()
               .IsRequired();

        builder.Property(i => i.ProcurementId)
               .HasVogenConversion()
               .IsRequired();

        builder.Property(i => i.InviteNumber)
               .HasVogenConversion()
               .IsRequired();

        builder.Property(i => i.IsInvite)
               .IsRequired();

        builder.Property(i => i.IsMigration)
               .HasDefaultValue(false);

        builder.Property(i => i.SubmitProposalStartDate);

        builder.Property(i => i.SubmitProposalEndDate);

        builder.Property(i => i.SubmitProposalStartTime);

        builder.Property(i => i.SubmitProposalEndTime);

        builder.Property(i => i.NeedToKnowWithinDate);

        builder.Property(i => i.ClarifyDetailViaDate);

        builder.Property(i => i.DocumentDate);

        builder.Property(i => i.PhoneNumber);

        builder.Property(i => i.Status)
               .HasConversion(new EnumToStringConverter<PInviteStatus>())
               .IsRequired();

        builder.HasMany(i => i.Acceptors)
               .WithOne(a => a.Invite)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(i => i.InvitedEntrepreneurs)
               .WithOne(a => a.Invite)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Procurement)
               .WithMany(p => p.Invites)
               .HasForeignKey(m => m.ProcurementId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasActivityInfo();
        builder.OwnsAuditInfo();
        builder.HasSoftDelete();
    }
}