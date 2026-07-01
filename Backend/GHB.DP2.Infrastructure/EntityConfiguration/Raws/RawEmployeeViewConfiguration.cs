namespace GHB.DP2.Infrastructure.EntityConfiguration.Raws;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Raws;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class RawEmployeeViewConfiguration : EntityTypeConfigurationBase<RawEmployeeView, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<RawEmployeeView> builder)
    {
        builder.ToView("raw_employee_view", nameof(Raws));

        builder.HasKey(e => e.EmployeeCode);

        builder.Property(e => e.EmployeeCode)
               .HasVogenConversion();

        builder.Property(e => e.FullName);

        builder.Property(e => e.PositionId)
               .HasVogenConversion();

        builder.Property(e => e.FullPositionName);

        builder.Property(e => e.BusinessUnitId)
               .HasColumnName("RawBusinessUnitId")
               .HasVogenConversion();

        builder.Property(e => e.BusinessUnitName)
               .HasColumnName("RawBusinessUnitName");
    }
}