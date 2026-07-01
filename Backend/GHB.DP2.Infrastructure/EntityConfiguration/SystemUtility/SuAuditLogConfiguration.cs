namespace GHB.DP2.Infrastructure.EntityConfiguration.SystemUtility;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class SuAuditLogConfiguration : EntityTypeConfigurationBase<SuAuditLog, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<SuAuditLog> builder)
    {
        builder.ToTable(nameof(SuAuditLog), nameof(SystemUtility));

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
               .HasVogenConversion();

        builder.Property(x => x.Message)
               .IsRequired();

        builder.Property(x => x.UserId)
               .IsRequired();

        builder.Property(x => x.Program)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(x => x.IpAddress)
               .IsRequired()
               .HasMaxLength(45); // IPv6 max length

        builder.Property(x => x.CreatedAt)
               .IsRequired();

        builder.HasIndex(x => x.CreatedAt)
               .IsDescending(true);

        builder.HasIndex(x => x.User);

        builder.HasIndex(x => x.Program);

        builder.HasIndex(x => x.Message);
    }
}