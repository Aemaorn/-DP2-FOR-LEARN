namespace GHB.DP2.Infrastructure.EntityConfiguration.SystemUtility;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class SuDelegateeConfiguration : EntityTypeConfigurationBase<SuDelegatee, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<SuDelegatee> builder)
    {
        builder.ToTable(nameof(SuDelegatee), nameof(SystemUtility));

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id)
               .HasVogenConversion();

        builder.Property(d => d.DelegatorId)
               .HasVogenConversion();

        builder.Property(d => d.Sequence)
               .IsRequired()
               .HasDefaultValue(1);

        builder.Property(d => d.DelegatorPositionId)
               .HasVogenConversion();

        builder.Property(d => d.DelegatorBusinessUnitId)
               .HasVogenConversion();

        builder.Property(d => d.DelegatorPositionName)
               .IsRequired();

        builder.Property(d => d.Acting)
               .IsRequired();

        builder.Property(d => d.SuUserId)
               .HasVogenConversion()
               .IsRequired();

        builder.Property(d => d.EmployeeCode)
               .HasVogenConversion()
               .IsRequired();

        builder.Property(d => d.UserFullName)
               .IsRequired();

        builder.Property(d => d.PositionId)
               .HasVogenConversion()
               .IsRequired();

        builder.Property(d => d.FullPositionName)
               .IsRequired();

        builder.Property(d => d.BusinessUnitId)
               .HasConversion<BusinessUnitId.EfCoreValueConverter, BusinessUnitId.EfCoreValueComparer>();

        builder.Property(d => d.Active)
               .IsRequired();

        builder.Property(p => p.ParentBusinessUnitId)
               .HasConversion<BusinessUnitId.EfCoreValueConverter, BusinessUnitId.EfCoreValueComparer>();

        builder.Property(p => p.SubBusinessUnitId)
               .HasConversion<BusinessUnitId.EfCoreValueConverter, BusinessUnitId.EfCoreValueComparer>();

        builder.HasOne(d => d.SuUser)
               .WithMany(u => u.Delegatees)
               .HasForeignKey(d => d.SuUserId);

        builder.HasOne(d => d.Position)
               .WithMany()
               .HasForeignKey(d => d.PositionId);

        builder.HasOne(d => d.RawBusinessUnit)
               .WithMany()
               .HasForeignKey(d => d.BusinessUnitId)
               .HasPrincipalKey(p => p.Id)
               .IsRequired(false);

        builder.HasOne(d => d.ParentBusinessUnit)
               .WithMany()
               .HasForeignKey(d => d.ParentBusinessUnitId)
               .HasPrincipalKey(p => p.Id)
               .IsRequired(false);

        builder.HasOne(d => d.SubBusinessUnit)
               .WithMany()
               .HasForeignKey(d => d.SubBusinessUnitId)
               .HasPrincipalKey(p => p.Id)
               .IsRequired(false);

        builder.HasMany(d => d.DelegateeHistories)
               .WithOne(dh => dh.SuDelegatee)
               .HasForeignKey(dh => dh.SuDelegateeId);

        builder.OwnsAuditInfo();
    }
}