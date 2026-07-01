namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PJp005;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PJp005;
using GHB.DP2.Domain.Procurement.PpPurchaseRequisition;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class PJp005Configuration : EntityTypeConfigurationBase<PJp005, Dp2DbContext>
{
    protected override void EntityConfigure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<PJp005> builder)
    {
        builder.ToTable(
            nameof(PJp005),
            nameof(Procurement));

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion();

        builder.Property(p => p.PJp005Number)
               .HasVogenConversion()
               .IsRequired();

        builder.Property(p => p.ProcurementId)
               .HasConversion<
                   ProcurementId.EfCoreValueConverter,
                   ProcurementId.EfCoreValueComparer>();

        builder.Property(p => p.PpPurchaseRequisitionId)
               .HasConversion<
                   PpPurchaseRequisitionId.EfCoreValueConverter,
                   PpPurchaseRequisitionId.EfCoreValueComparer>();

        builder.Property(jp => jp.EvaluationDueDate)
               .IsRequired();

        builder.Property(jp => jp.EvaluationPeriodTypeCode)
               .IsRequired();

        builder.Property(jp => jp.EvaluationPeriodConditionCode)
               .IsRequired();

        builder.Property(jp => jp.IsActive)
               .IsRequired();

        builder.Property(jp => jp.EgpProjectNumber);

        builder.Property(jp => jp.JorPorNumber);

        builder.Property(p => p.Status)
               .HasConversion(new EnumToStringConverter<PJp005Status>())
               .IsRequired();

        builder.Property(p => p.IsMigration)
               .HasDefaultValue(false);

        builder.Property(p => p.DocumentDate);

        builder.HasMany(p => p.Committees)
               .WithOne(p => p.PJp005)
               .HasForeignKey(p => p.PJp005Id)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.CommitteeDuties)
               .WithOne(p => p.PJp005)
               .HasForeignKey(p => p.PJp005Id)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Acceptors)
               .WithOne(p => p.PJp005)
               .HasForeignKey(x => x.PJp005Id)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.ProcurementSuppliesDivisions)
               .WithOne(p => p.PJp005)
               .HasForeignKey(p => p.PJp005Id)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.Procurement)
               .WithMany(p => p.Jp005)
               .HasForeignKey(m => m.ProcurementId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.OwnDocumentHistory(p => p.DocumentHistories, documentBuilder =>
        {
            documentBuilder.ToTable(nameof(PJp005DocumentHistory), nameof(Procurement));

            documentBuilder.HasKey(d => d.Id);

            documentBuilder.Property(d => d.Id)
                           .HasVogenConversion();

            documentBuilder.Property(d => d.DocumentType)
                           .HasConversion(new EnumToStringConverter<PJp005DocumentType>());

            documentBuilder.Property(d => d.StatusState)
                           .HasConversion(new EnumToStringConverter<PJp005Status>());
        });

        builder.HasActivityInfo();
        builder.HasSoftDelete();
        builder.OwnsAuditInfo();
    }
}