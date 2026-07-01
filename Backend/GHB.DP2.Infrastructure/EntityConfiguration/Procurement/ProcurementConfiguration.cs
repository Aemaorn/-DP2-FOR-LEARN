namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Plan;
using GHB.DP2.Domain.Procurement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class ProcurementConfiguration : EntityTypeConfigurationBase<Procurement, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<Procurement> builder)
    {
        builder.ToTable(nameof(Procurement), nameof(Procurement));

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion();

        builder.Property(p => p.PlanId)
               .HasConversion<PlanId.EfCoreValueConverter, PlanId.EfCoreValueComparer>();

        builder.Property(p => p.Type)
               .HasConversion(new EnumToStringConverter<ProcurementType>())
               .IsRequired();

        builder.Property(p => p.Step)
               .HasConversion(new EnumToStringConverter<ProcurementStep>())
               .IsRequired();

        builder.Property(p => p.ProcessType)
               .HasConversion(new EnumToStringConverter<ProcessType>());

        builder.Property(p => p.Status)
               .HasConversion(new EnumToStringConverter<ProcurementStatus>())
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

        builder.Property(p => p.Name)
               .IsRequired();

        builder.Property(p => p.Budget);

        builder.Property(p => p.BudgetYear);

        builder.Property(p => p.ExpectingProcurementAt);

        builder.Property(p => p.IsCancelled);

        builder.Property(p => p.IsStock)
               .IsRequired();

        builder.Property(p => p.IsCommercialMaterial)
               .IsRequired();

        builder.Property(p => p.HasMd);

        builder.Property(p => p.RemarkClosed);

        builder.Property(p => p.LastStatusBeforeClosed)
               .HasConversion(new EnumToStringConverter<ProcurementStatus>());

        builder.Property(p => p.ProcurementNumber)
               .HasConversion<ProcurementNumber.EfCoreValueConverter, ProcurementNumber.EfCoreValueComparer>();

        builder.HasActivityInfo();

        builder.HasSoftDelete();
        builder.OwnsAuditInfo();
    }
}