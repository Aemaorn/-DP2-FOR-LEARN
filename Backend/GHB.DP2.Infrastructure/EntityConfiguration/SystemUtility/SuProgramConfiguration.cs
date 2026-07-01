namespace GHB.DP2.Infrastructure.EntityConfiguration.SystemUtility;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class SuProgramConfiguration : EntityTypeConfigurationBase<SuProgram, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<SuProgram> builder)
    {
        builder.ToTable(nameof(SuProgram), nameof(SystemUtility));

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion();

        builder.Property(p => p.Code)
               .HasMaxLength(50)
               .IsRequired();

        builder.Property(p => p.Label)
               .HasMaxLength(500)
               .IsRequired();

        builder.Property(p => p.Path);

        builder.Property(p => p.Sorting)
               .IsRequired();

        builder.Property(p => p.IsActive)
               .IsRequired()
               .HasDefaultValue(true);

        builder.HasOne(p => p.Parent)
               .WithMany(p => p.Children)
               .HasForeignKey(p => p.ParentId)
               .IsRequired(false);
    }
}