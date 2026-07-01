namespace GHB.DP2.Infrastructure.EntityConfiguration.SystemUtility;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class SuUserConfiguration : EntityTypeConfigurationBase<SuUser, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<SuUser> builder)
    {
        builder.ToTable(nameof(SuUser), nameof(SystemUtility));

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
               .HasVogenConversion();

        builder.Property(u => u.SignatureImageId);

        builder.Property(u => u.IsActive)
               .IsRequired();

        builder.Property(u => u.FailedLoginAttempts)
               .HasDefaultValue(0)
               .IsRequired();

        builder.Property(u => u.LockoutEnd);

        builder.OwnsOne(u => u.OtherInfo)
               .ToJson();

        builder.HasOne(u => u.Employee)
               .WithMany(e => e.Users)
               .HasForeignKey(u => u.EmployeeCode);

        builder.HasMany(u => u.Roles)
               .WithMany(r => r.Users)
               .UsingEntity("SuUserRole");

        builder.HasMany(u => u.Notifications)
               .WithOne(n => n.User)
               .HasForeignKey(n => n.UserId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.OwnsAuditInfo();
    }
}