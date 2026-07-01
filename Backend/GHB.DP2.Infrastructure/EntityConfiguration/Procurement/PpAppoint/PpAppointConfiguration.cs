namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PpAppoint;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PpAppoint;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class PpAppointConfiguration : EntityTypeConfigurationBase<Domain.Procurement.PpAppoint.PpAppoint, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<Domain.Procurement.PpAppoint.PpAppoint> builder)
    {
        builder.ToTable(nameof(Domain.Procurement.PpAppoint.PpAppoint), nameof(Domain.Procurement.Procurement));

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion();

        builder.Property(p => p.ReferenceId)
               .HasConversion<PpAppointId.EfCoreValueConverter, PpAppointId.EfCoreValueComparer>();

        builder.Property(p => p.ProcurementId)
               .HasConversion<ProcurementId.EfCoreValueConverter, ProcurementId.EfCoreValueComparer>();

        builder.Property(p => p.AppointNumber)
               .HasVogenConversion()
               .IsRequired();

        builder.Property(p => p.MemorandumDate)
               .IsRequired();

        builder.Property(p => p.DocumentDate);

        builder.Property(p => p.MemorandumNumber)
               .HasMaxLength(100);

        builder.Property(p => p.Telephone)
               .HasMaxLength(100);

        builder.Property(p => p.Reason);

        builder.Property(p => p.Status)
               .HasConversion(new EnumToStringConverter<AppointStatus>())
               .IsRequired();

        builder.Property(p => p.IsChange)
               .IsRequired();

        builder.Property(p => p.IsCancel)
               .IsRequired();

        builder.Property(p => p.IsActive)
               .IsRequired();

        builder.Property(p => p.IsMigration)
               .HasDefaultValue(false);

        builder.Property(p => p.CancelReason);

        builder.Property(p => p.ChangeReason);

        builder.HasMany(p => p.Acceptors)
               .WithOne(a => a.PpAppoint)
               .HasForeignKey(p => p.PpAppointId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.MedianPriceCommittees)
               .WithOne(p => p.PpAppoint)
               .HasForeignKey(p => p.PpAppointId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.MedianPriceCommitteeDuties)
               .WithOne(p => p.PpAppoint)
               .HasForeignKey(p => p.PpAppointId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.TorDraftCommittees)
               .WithOne(p => p.PpAppoint)
               .HasForeignKey(p => p.PpAppointId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.TorDraftCommitteeDuties)
               .WithOne(p => p.PpAppoint)
               .HasForeignKey(p => p.PpAppointId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.Procurement)
               .WithMany(p => p.Appoints)
               .HasForeignKey(p => p.ProcurementId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.OwnDocumentHistory(p => p.DocumentHistories, documentBuilder =>
        {
            documentBuilder.ToTable(nameof(PpAppointDocumentHistory), nameof(Procurement));

            documentBuilder.HasKey(d => d.Id);

            documentBuilder.Property(d => d.Id)
                           .HasVogenConversion();

            documentBuilder.Property(d => d.StatusState)
                           .HasConversion(new EnumToStringConverter<AppointStatus>());
        });

        builder.HasActivityInfo();
        builder.HasSoftDelete();
        builder.OwnsAuditInfo();
    }
}