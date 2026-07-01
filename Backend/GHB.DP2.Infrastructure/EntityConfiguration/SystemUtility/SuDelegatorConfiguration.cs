namespace GHB.DP2.Infrastructure.EntityConfiguration.SystemUtility;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class SuDelegatorConfiguration : EntityTypeConfigurationBase<SuDelegator, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<SuDelegator> builder)
    {
        builder.ToTable(nameof(SuDelegator), nameof(SystemUtility));

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id)
               .HasVogenConversion();

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

        builder.Property(d => d.DelegationStartDate)
               .IsRequired();

        builder.Property(d => d.DelegationEndDate)
               .IsRequired();

        builder.Property(d => d.Annotation)
               .IsRequired();

        builder.HasMany(d => d.Delegatees)
               .WithOne(dt => dt.SuDelegator)
               .HasForeignKey(d => d.DelegatorId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(d => d.SuUser)
               .WithMany(u => u.Delegators)
               .HasForeignKey(d => d.SuUserId);

        builder.HasOne(d => d.Position)
               .WithMany()
               .HasForeignKey(d => d.PositionId);

        builder.OwnsAuditInfo();
        builder.HasSoftDelete();
    }
}