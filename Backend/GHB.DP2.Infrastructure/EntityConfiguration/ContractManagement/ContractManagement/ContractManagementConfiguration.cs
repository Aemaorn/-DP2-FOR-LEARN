namespace GHB.DP2.Infrastructure.EntityConfiguration.ContractManagement.ContractManagement;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractManagement.ContractManagement;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class ContractManagementConfiguration : EntityTypeConfigurationBase<ContractManagement, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<ContractManagement> builder)
    {
        builder.ToTable(nameof(ContractManagement), nameof(Domain.ContractManagement));

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
               .HasVogenConversion();

        builder.Property(c => c.CaContractDraftVendorId)
               .HasVogenConversion()
               .IsRequired();

        builder.Property(c => c.ProcurementId)
               .HasConversion<ProcurementId.EfCoreValueConverter, ProcurementId.EfCoreValueComparer>()
               .IsRequired();

        builder.Property(c => c.Step)
               .HasConversion(new EnumToStringConverter<ContractManagementStep>())
               .IsRequired();

        builder.Property(c => c.Status)
               .HasConversion(new EnumToStringConverter<ContractManagementStatus>())
               .IsRequired();

        builder.Property(c => c.ContractName)
               .IsRequired();

        builder.Property(c => c.SupplyMethodCode)
               .HasConversion<ParameterCode.EfCoreValueConverter, ParameterCode.EfCoreValueComparer>()
               .IsRequired();

        builder.Property(c => c.SupplyMethodTypeCode)
               .HasConversion<ParameterCode.EfCoreValueConverter, ParameterCode.EfCoreValueComparer>();

        builder.Property(c => c.SupplyMethodSpecialTypeCode)
               .HasConversion<ParameterCode.EfCoreValueConverter, ParameterCode.EfCoreValueComparer>();

        builder.Property(c => c.BudgetYear)
               .IsRequired();

        builder.HasOne(c => c.CaContractDraftVendor)
               .WithMany()
               .HasForeignKey(c => c.CaContractDraftVendorId)
               .OnDelete(DeleteBehavior.Restrict)
               .IsRequired();

        builder.HasOne(c => c.Procurement)
               .WithMany()
               .HasForeignKey(c => c.ProcurementId)
               .OnDelete(DeleteBehavior.Restrict)
               .IsRequired();

        builder.HasOne(c => c.Department)
               .WithMany()
               .HasForeignKey(c => c.DepartmentId)
               .HasPrincipalKey(p => p.Id)
               .OnDelete(DeleteBehavior.Restrict)
               .IsRequired();

        builder.HasOne(c => c.SupplyMethod)
               .WithMany()
               .HasForeignKey(c => c.SupplyMethodCode)
               .HasPrincipalKey(p => p.Code)
               .OnDelete(DeleteBehavior.Restrict)
               .IsRequired();

        builder.HasOne(c => c.SupplyMethodType)
               .WithMany()
               .HasForeignKey(c => c.SupplyMethodTypeCode)
               .HasPrincipalKey(p => p.Code)
               .OnDelete(DeleteBehavior.Restrict)
               .IsRequired(false);

        builder.HasOne(c => c.SupplyMethodSpecialType)
               .WithMany()
               .HasForeignKey(c => c.SupplyMethodSpecialTypeCode)
               .HasPrincipalKey(p => p.Code)
               .OnDelete(DeleteBehavior.Restrict)
               .IsRequired(false);

        builder.OwnsAuditInfo();
        builder.HasActivityInfo();
    }
}
