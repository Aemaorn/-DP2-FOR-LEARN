namespace GHB.DP2.Infrastructure.EntityConfiguration.ContractManagement.CmDeliveryAcceptance;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.ContractManagement.CmDeliveryAcceptance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class CmDeliveryAcceptanceConfiguration : EntityTypeConfigurationBase<CmDeliveryAcceptance, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<CmDeliveryAcceptance> builder)
    {
        builder.ToTable(nameof(CmDeliveryAcceptance), nameof(ContractManagement));

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion();

        builder.Property(p => p.ContractType)
               .IsRequired(false);

        builder.Property(p => p.Status)
               .HasConversion(new EnumToStringConverter<CmDeliveryAcceptanceStatus>())
               .IsRequired();

        builder.Property(p => p.SourceType)
               .HasConversion(new EnumToStringConverter<SourceType>())
               .IsRequired();

        builder.Property(p => p.RefId)
               .IsRequired(false);

        builder.Property(p => p.Number)
               .IsRequired(false);

        builder.Property(p => p.Name)
               .IsRequired(false);

        builder.Property(p => p.Budget)
               .IsRequired(false);

        builder.Property(p => p.IsCommercialMaterial)
               .IsRequired(false);

        builder.HasOne(p => p.Department)
               .WithMany()
               .HasForeignKey(p => p.DepartmentId)
               .HasPrincipalKey(p => p.Id)
               .OnDelete(DeleteBehavior.Restrict)
               .IsRequired(false);

        builder.HasOne(p => p.SupplyMethod)
               .WithMany()
               .HasForeignKey(p => p.SupplyMethodCode)
               .HasPrincipalKey(p => p.Code)
               .IsRequired(false);

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

        builder.OwnsAuditInfo();
        builder.HasSoftDelete();
    }
}
