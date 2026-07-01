namespace GHB.DP2.Infrastructure.EntityConfiguration.SystemUtility;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class SuDocumentTemplateConfiguration : EntityTypeConfigurationBase<SuDocumentTemplate, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<SuDocumentTemplate> builder)
    {
        builder.ToTable(nameof(SuDocumentTemplate), nameof(SystemUtility));

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
               .HasVogenConversion();

        builder.Property(e => e.Group)
               .HasMaxLength(50)
               .IsRequired();

        builder.Property(e => e.Code)
               .IsRequired();

        builder.Property(e => e.Name)
               .IsRequired();

        builder.Property(e => e.PreviewPfdFileName)
               .IsRequired(false);

        builder.Property(e => e.PreviewPfdFileId)
               .IsRequired();

        builder.Property(e => e.FileId)
               .IsRequired();

        builder.Property(e => e.IsActive)
               .IsRequired();

        builder.OwnsOne(
            e => e.BudgetForDocument,
            budget =>
            {
                budget.Property(b => b.Min)
                      .HasColumnName("BudgetMin")
                      .HasDefaultValue(decimal.Zero);

                budget.Property(b => b.Max)
                      .HasColumnName("BudgetMax");
            });

        builder.HasOne(p => p.SupplyMethodCodeInfo)
               .WithMany()
               .HasForeignKey(p => p.SupplyMethodCode)
               .HasPrincipalKey(p => p.Code);

        builder.Property(p => p.AdditionalInfo)
               .HasColumnType("jsonb")
               .IsRequired(false);

        builder.Property(p => p.IsCancel);

        builder.Property(p => p.IsChange);

        builder.OwnsAuditInfo();
        builder.HasSoftDelete();

        builder.HasIndex(p => p.Code)
               .IsUnique();
    }
}