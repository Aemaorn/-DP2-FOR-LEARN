namespace GHB.DP2.Infrastructure.EntityConfiguration.Raws;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Raws;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class RawEmployeePositionConfiguration : EntityTypeConfigurationBase<RawEmployeePosition, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<RawEmployeePosition> builder)
    {
        builder.ToTable(nameof(RawEmployeePosition), nameof(Raws));

        builder.HasKey(ep => new
        {
            ep.EmployeeCode,
            ep.PositionId,
            ep.BusinessUnitId,
            ep.Acting,
        });

        builder.Property(ep => ep.EmployeeType)
               .IsRequired();

        builder.Property(ep => ep.Remark);

        builder.HasOne(ep => ep.Employee)
               .WithMany(e => e.Positions)
               .HasForeignKey(ep => ep.EmployeeCode);

        builder.HasOne(ep => ep.Manager)
               .WithMany()
               .HasForeignKey(ep => ep.ManagerEmployeeCode)
               .IsRequired(false);

        builder.HasOne(ep => ep.Position)
               .WithMany()
               .HasForeignKey(ep => ep.PositionId);

        builder.HasOne(ep => ep.BusinessUnit)
               .WithMany()
               .HasForeignKey(ep => ep.BusinessUnitId);
    }
}