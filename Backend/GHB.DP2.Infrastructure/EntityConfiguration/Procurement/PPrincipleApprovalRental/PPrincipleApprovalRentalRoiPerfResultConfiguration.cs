namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PPrincipleApprovalRental;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PPrincipleApproval;
using GHB.DP2.Domain.Procurement.PPrincipleApprovalRental;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class PPrincipleApprovalRentalRoiPerfResultConfiguration : EntityTypeConfigurationBase<PPrincipleApprovalRentalRoiPerfResult, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PPrincipleApprovalRentalRoiPerfResult> builder)
    {
        builder.ToTable(nameof(PPrincipleApprovalRentalRoiPerfResult), nameof(Procurement));

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion();

        builder.Property(p => p.Sequence)
               .IsRequired();

        builder.Property(p => p.PerformanceResultGroup)
               .HasConversion(new EnumToStringConverter<PerformanceResultGroup>())
               .IsRequired();

        builder.Property(p => p.Year)
               .IsRequired();

        builder.Property(p => p.AccountActual)
               .IsRequired();

        builder.Property(p => p.AccountGrowth)
               .IsRequired();

        builder.Property(p => p.AmountTarget)
               .IsRequired();

        builder.Property(p => p.AmountActual)
               .IsRequired();

        builder.Property(p => p.AmountRate)
               .IsRequired();

        builder.Property(p => p.AmountGrowth)
               .IsRequired();

        builder.OwnsAuditInfo();
    }
}