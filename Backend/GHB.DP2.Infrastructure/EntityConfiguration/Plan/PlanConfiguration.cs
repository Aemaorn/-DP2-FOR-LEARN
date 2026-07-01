namespace GHB.DP2.Infrastructure.EntityConfiguration.Plan;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Plan;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class PlanConfiguration : EntityTypeConfigurationBase<Plan, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<Plan> builder)
    {
        builder.ToTable(nameof(Plan), nameof(Plan));

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion();

        builder.Property(p => p.PlanNumber)
               .HasVogenConversion()
               .IsRequired();

        builder.Property(p => p.Type)
               .HasConversion(new EnumToStringConverter<PlanType>())
               .IsRequired();

        builder.Property(p => p.Name)
               .IsRequired();

        builder.Property(p => p.BudgetYear)
               .IsRequired();

        builder.Property(p => p.Budget)
               .IsRequired();

        builder.Property(p => p.ExpectingProcurementAt)
               .IsRequired();

        builder.Property(p => p.IsStock)
               .IsRequired();

        builder.Property(p => p.IsCommercialMaterial);

        builder.Property(p => p.Remark);

        builder.Property(p => p.RemarkClosed);

        builder.Property(p => p.LastStatusBeforeClosed)
               .HasConversion(new EnumToStringConverter<PlanStatus>());

        builder.Property(p => p.Telephone);

        builder.Property(p => p.GroupEgpNumber);

        builder.Property(p => p.EgpNumber);

        builder.Property(p => p.DocumentDate);

        builder.Property(p => p.IsChange)
               .IsRequired();

        builder.Property(p => p.IsCancel)
               .IsRequired();

        builder.Property(p => p.Status)
               .HasConversion(new EnumToStringConverter<PlanStatus>())
               .IsRequired();

        builder.Property(p => p.IsActive)
               .IsRequired();

        builder.Property(p => p.CancelReason);

        builder.Property(p => p.ChangeReason);

        builder.Property(p => p.ReferenceId)
               .HasConversion<PlanId.EfCoreValueConverter, PlanId.EfCoreValueComparer>();

        builder.Property(p => p.Sequence)
               .IsRequired();

        builder.HasOne(p => p.Department)
               .WithMany()
               .HasForeignKey(p => p.DepartmentId)
               .HasPrincipalKey(p => p.Id)
               .OnDelete(DeleteBehavior.Restrict)
               .IsRequired();

        builder.HasOne(p => p.SupplyMethod)
               .WithMany()
               .HasForeignKey(p => p.SupplyMethodCode)
               .HasPrincipalKey(p => p.Code)
               .IsRequired();

        builder.HasOne(p => p.SupplyMethodType)
               .WithMany()
               .HasForeignKey(p => p.SupplyMethodTypeCode)
               .HasPrincipalKey(p => p.Code)
               .IsRequired(false);

        builder.HasOne(p => p.SupplyMethodSpecialType)
               .WithMany()
               .HasForeignKey(p => p.SupplyMethodSpecialTypeCode)
               .HasPrincipalKey(p => p.Code)
               .IsRequired(false);

        builder.HasOne(p => p.AssignSegment)
               .WithMany()
               .HasForeignKey(p => p.AssignSegmentCode)
               .HasPrincipalKey(p => p.Code);

        builder.HasMany(p => p.Acceptors)
               .WithOne(a => a.Plan)
               .HasForeignKey(p => p.PlanId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Assignees)
               .WithOne(p => p.Plan)
               .HasForeignKey(p => p.PlanId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Attachments)
               .WithOne();

        builder.OwnDocumentHistory(p => p.DocumentHistories, documentBuilder =>
        {
            documentBuilder.ToTable(nameof(PlanDocumentHistory), nameof(Plan));

            documentBuilder.HasKey(d => d.Id);

            documentBuilder.Property(d => d.Id)
                           .HasVogenConversion();

            documentBuilder.Property(d => d.DocumentType)
                           .HasConversion(new EnumToStringConverter<PlanDocumentType>());

            documentBuilder.Property(d => d.StatusState)
                           .HasConversion(new EnumToStringConverter<PlanStatus>());
        });

        builder.HasActivityInfo();

        builder.HasSoftDelete();
        builder.OwnsAuditInfo();
    }
}