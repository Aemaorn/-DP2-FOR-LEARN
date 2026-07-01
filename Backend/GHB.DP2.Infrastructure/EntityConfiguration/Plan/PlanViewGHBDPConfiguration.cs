namespace GHB.DP2.Infrastructure.EntityConfiguration.Plan;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Plan;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class PlanViewGhbdpConfiguration : EntityTypeConfigurationBase<PlanViewGHBDP, Dp1DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PlanViewGHBDP> builder)
    {
        builder.ToView("list_plan_view", "Plan");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.PlanNumber);

        builder.Property(e => e.Name);

        builder.Property(e => e.BudgetYear);

        builder.Property(e => e.Budget);

        builder.Property(p => p.Type)
               .HasConversion(new EnumToStringConverter<PlanType>())
               .IsRequired();

        builder.Property(e => e.DepartmentName);

        builder.Property(e => e.SupplyMethod);

        builder.Property(e => e.IsChange);

        builder.Property(e => e.IsCancel);

        builder.Property(e => e.Status)
               .HasConversion(new EnumToStringConverter<PlanStatus>())
               .IsRequired();
    }
}