namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PpTorDraft;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PpTorDraft;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class PpTorDraftConfiguration : EntityTypeConfigurationBase<Domain.Procurement.PpTorDraft.PpTorDraft, Dp2DbContext>
{
    protected override void EntityConfigure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Domain.Procurement.PpTorDraft.PpTorDraft> builder)
    {
        builder.ToTable(nameof(Domain.Procurement.PpTorDraft.PpTorDraft), nameof(Domain.Procurement.Procurement));

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion();

        builder.Property(p => p.ReferenceId)
               .HasConversion<PpTorDraftId.EfCoreValueConverter, PpTorDraftId.EfCoreValueComparer>();

        builder.Property(p => p.ProcurementId)
               .HasConversion<ProcurementId.EfCoreValueConverter, ProcurementId.EfCoreValueComparer>();

        builder.Property(p => p.ReferenceNumber)
               .HasVogenConversion()
               .HasMaxLength(100);

        builder.Property(p => p.BidGuarantee);

        builder.Property(p => p.IsStock)
               .IsRequired();

        builder.Property(p => p.Reason);

        builder.Property(p => p.EvaluationCriteria);

        builder.Property(p => p.IsChange)
               .IsRequired();

        builder.Property(p => p.IsCancel)
               .IsRequired();

        builder.Property(p => p.IsActive)
               .IsRequired();

        builder.Property(p => p.IsMigration)
               .HasDefaultValue(false);

        builder.Property(p => p.IsMA);

        builder.Property(p => p.CancelReason);

        builder.Property(p => p.ChangeReason);

        builder.HasOne(p => p.DocumentTemplate)
               .WithMany()
               .HasForeignKey(p => p.DocumentTemplateId)
               .IsRequired(false)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.PpTorDraftObjects)
               .WithOne(p => p.PpTorDraft)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.PpTorDraftQualifications)
               .WithOne(p => p.PpTorDraft)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.PpTorDraftTechnicalPeriods)
               .WithOne(p => p.PpTorDraft)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.PpTorDraftTechnicalSpecifications)
               .WithOne(p => p.PpTorDraft)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.PpTorDraftBudgets)
               .WithOne(p => p.PpTorDraft)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.PpTorDraftWarranties)
               .WithOne(p => p.PpTorDraft)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.PpTorDraftPaymentTerms)
               .WithOne(p => p.PpTorDraft)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.PpTorPaymentTermPeriods)
               .WithOne(p => p.PpTorDraft)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.PpTorDraftFineRates)
               .WithOne(p => p.PpTorDraft)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.PpTorDraftAcceptors)
               .WithOne(a => a.PpTorDraft)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.Procurement)
               .WithMany(p => p.TorDrafts)
               .HasForeignKey(p => p.ProcurementId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.PpTorTrainingItems)
               .WithOne(p => p.PpTorDraft)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.PpTorImpediments)
               .WithOne(p => p.PpTorDraft)
               .OnDelete(DeleteBehavior.Cascade);

        builder.Property(p => p.Status)
               .HasConversion(new EnumToStringConverter<TorDraftStatus>())
               .IsRequired();

        builder.Property(p => p.Telephone)
               .HasMaxLength(100);

        builder.Property(p => p.DocumentDate);

        builder.OwnDocumentHistory(p => p.DocumentHistories, documentBuilder =>
        {
            documentBuilder.ToTable(nameof(PpTorDraftDocumentHistory), nameof(Procurement));

            documentBuilder.HasKey(d => d.Id);

            documentBuilder.Property(d => d.Id)
                           .HasVogenConversion();

            documentBuilder.Property(d => d.DocumentType)
                           .HasConversion(new EnumToStringConverter<PpTorDraftDocumentType>());

            documentBuilder.Property(d => d.StatusState)
                           .HasConversion(new EnumToStringConverter<TorDraftStatus>());
        });

        builder.Property(p => p.IsContractGuarantee);

        builder.Property(p => p.PercentageContract);

        builder.Property(p => p.IsCM);

        builder.Property(p => p.IsPM);

        builder.Property(p => p.IsTraining);

        builder.Property(p => p.IsImpediment);

        builder.HasActivityInfo();
        builder.HasSoftDelete();
        builder.OwnsAuditInfo();
    }
}