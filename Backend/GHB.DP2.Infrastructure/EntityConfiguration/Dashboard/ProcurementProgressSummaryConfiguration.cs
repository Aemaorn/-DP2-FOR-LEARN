namespace GHB.DP2.Infrastructure.EntityConfiguration.Dashboard;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Dashboard;
using GHB.DP2.Domain.Plan;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class ProcurementProgressSummaryConfiguration
    : EntityTypeConfigurationBase<ProcurementProgressSummary, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<ProcurementProgressSummary> builder)
    {
        builder.ToTable(nameof(ProcurementProgressSummary), "Dashboard");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion();

        builder.Property(p => p.PlanId)
               .HasVogenConversion()
               .IsRequired();

        builder.Property(p => p.Status)
               .HasConversion(new EnumToStringConverter<ProcurementProgressStatus>())
               .HasMaxLength(50);

        builder.HasOne(p => p.Plan)
               .WithMany()
               .HasForeignKey(p => p.PlanId)
               .HasPrincipalKey(p => p.Id)
               .OnDelete(DeleteBehavior.Restrict)
               .IsRequired();

        builder.OwnsAuditInfo();
    }
}
