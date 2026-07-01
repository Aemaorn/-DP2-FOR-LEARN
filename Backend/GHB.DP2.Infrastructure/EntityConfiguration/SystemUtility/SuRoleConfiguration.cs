namespace GHB.DP2.Infrastructure.EntityConfiguration.SystemUtility;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class SuRoleConfiguration : EntityTypeConfigurationBase<SuRole, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<SuRole> builder)
    {
        builder.ToTable(nameof(SuRole), nameof(SystemUtility));

        builder.HasKey(r => r.Code);

        builder.Property(p => p.Code)
               .HasMaxLength(50)
               .HasVogenConversion();

        builder.Property(r => r.Name)
               .HasMaxLength(500)
               .IsRequired();

        builder.Property(r => r.IsActive)
               .IsRequired()
               .HasDefaultValue(true);

        builder.HasMany(r => r.RolePrograms)
               .WithOne(rp => rp.Role)
               .IsRequired();

        builder.HasMany(r => r.Users)
               .WithMany(u => u.Roles)
               .UsingEntity(
                      "SuUserRole");

        builder.OwnsAuditInfo();
    }
}