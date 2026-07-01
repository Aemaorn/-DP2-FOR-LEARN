namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PInvite;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PInvite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class PInvitedEntrepreneursDocumentHistoryConfiguration : EntityTypeConfigurationBase<PInvitedEntrepreneursDocumentHistory, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PInvitedEntrepreneursDocumentHistory> builder)
    {
        builder.ToTable(nameof(PInvitedEntrepreneursDocumentHistory), nameof(Procurement));

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id)
               .HasVogenConversion()
               .IsRequired();

        builder.Property(d => d.PInvitedEntrepreneursId)
               .HasVogenConversion()
               .IsRequired();

        builder.Property(d => d.StatusState)
               .HasConversion(new EnumToStringConverter<PInviteStatus>())
               .IsRequired();

        builder.Property(d => d.FileId)
               .IsRequired();

        builder.Property(d => d.Version)
               .IsRequired();

        builder.Property(d => d.CreatedAt)
               .IsRequired();

        builder.Property(d => d.CreatedBy)
               .IsRequired();

        builder.Property(d => d.CreatedByName)
               .IsRequired();

        builder.Property(d => d.IsReplaced)
               .IsRequired()
               .HasDefaultValue(false);

        builder.Property(d => d.Remark);
    }
}
