namespace GHB.DP2.Infrastructure.EntityConfiguration.Raws;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Raws;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class RawEmployeeConfiguration : EntityTypeConfigurationBase<RawEmployee, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<RawEmployee> builder)
    {
        builder.ToTable(nameof(RawEmployee), nameof(Raws));

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
               .HasVogenConversion();

        builder.Property(e => e.Title)
               .IsRequired();

        builder.Property(e => e.FirstName)
               .IsRequired();

        builder.Property(e => e.LastName)
               .IsRequired();

        builder.Property(e => e.CitizenCardId)
               .IsRequired();

        builder.Property(e => e.BirthDate);

        builder.Property(e => e.Email)
               .IsRequired();

        builder.Property(e => e.Remark);

        builder.Property(e => e.CreatedAt)
               .IsRequired();

        builder.Property(e => e.UpdatedAt);

        builder.HasOne(e => e.View)
               .WithMany()
               .HasForeignKey(e => e.Id)
               .IsRequired(false);
    }
}