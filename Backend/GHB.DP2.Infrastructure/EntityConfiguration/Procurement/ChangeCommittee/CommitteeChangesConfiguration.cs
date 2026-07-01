namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.ChangeCommittee;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.ChangeCommittee;
using GHB.DP2.Domain.Procurement.PpAppoint;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class CommitteeChangesConfiguration : EntityTypeConfigurationBase<CommitteeChanges, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<CommitteeChanges> builder)
    {
        builder.ToTable(nameof(CommitteeChanges), "Procurement");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
               .HasVogenConversion();

        builder.Property(c => c.ProcurementId)
               .HasConversion<ProcurementId.EfCoreValueConverter, ProcurementId.EfCoreValueComparer>();

        builder.Property(c => c.SourceType)
               .HasConversion(new EnumToStringConverter<SourceType>());

        builder.Property(c => c.SourceId)
               .IsRequired();

        builder.Property(c => c.CommitteeType)
               .HasConversion(new EnumToStringConverter<CommitteeType>());

        builder.Property(c => c.Status)
               .HasConversion(new EnumToStringConverter<CommitteeChangeStatus>());

        builder.Property(c => c.Remark)
               .IsRequired(false);

        builder.Property(c => c.IsJorPorComment)
               .IsRequired()
               .HasDefaultValue(false);

        builder.Property(c => c.DocumentDate);

        // JsonElement properties for committee data - no conversion needed
        builder.OwnsMany(c => c.OldCommittees, b => { b.ToJson(); });

        builder.OwnsMany(c => c.NewCommittees, b => { b.ToJson(); });

        builder.HasOne(c => c.Procurement)
               .WithMany()
               .HasForeignKey(c => c.ProcurementId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.Acceptors)
               .WithOne(a => a.CommitteeChange)
               .HasForeignKey(a => a.CommitteeChangeId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Attachments)
               .WithOne(a => a.CommitteeChanges)
               .HasForeignKey(a => a.CommitteeChangeId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Assignees)
               .WithOne(a => a.CommitteeChanges)
               .HasForeignKey(a => a.CommitteeChangeId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.OwnDocumentHistory(c => c.DocumentHistories, documentBuilder =>
        {
            documentBuilder.ToTable(nameof(CommitteeChangeDocumentHistory), nameof(Procurement));

            documentBuilder.HasKey(d => d.Id);

            documentBuilder.Property(d => d.Id)
                           .HasVogenConversion();

            documentBuilder.Property(d => d.StatusState)
                           .HasConversion(new EnumToStringConverter<CommitteeChangeStatus>());
        });

        builder.HasActivityInfo();
        builder.HasSoftDelete();
        builder.OwnsAuditInfo();
    }
}