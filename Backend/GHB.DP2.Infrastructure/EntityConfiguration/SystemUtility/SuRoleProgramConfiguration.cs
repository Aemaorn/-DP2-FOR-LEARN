namespace GHB.DP2.Infrastructure.EntityConfiguration.SystemUtility;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class SuRoleProgramConfiguration : EntityTypeConfigurationBase<SuRoleProgram, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<SuRoleProgram> builder)
    {
        builder.ToTable(nameof(SuRoleProgram), nameof(SystemUtility));

        builder.HasKey(rp => rp.Id);

        builder.Property(rp => rp.Id)
               .HasVogenConversion();

        builder.Property(rp => rp.IsView)
               .IsRequired();

        builder.Property(rp => rp.IsManage)
               .IsRequired();

        builder.Ignore(rp => rp.Permission);

        builder.HasOne(rp => rp.Role)
               .WithMany(r => r.RolePrograms)
               .IsRequired();

        builder.HasOne(rp => rp.Program)
               .WithMany()
               .HasForeignKey(rp => rp.ProgramId)
               .IsRequired();

        builder.OwnsAuditInfo();
    }
}