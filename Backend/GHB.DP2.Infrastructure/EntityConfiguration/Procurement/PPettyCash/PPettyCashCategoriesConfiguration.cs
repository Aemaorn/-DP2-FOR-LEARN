namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PPettyCash;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PPettyCash;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PPettyCashCategoriesConfiguration : EntityTypeConfigurationBase<PPettyCashCategories, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PPettyCashCategories> builder)
    {
        builder.ToTable(nameof(PPettyCashCategories), nameof(Domain.Procurement.Procurement));
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .ValueGeneratedNever()
               .HasVogenConversion();

        builder.Property(p => p.PettyCashId)
               .HasConversion<PettyCashId.EfCoreValueConverter, PettyCashId.EfCoreValueComparer>()
               .IsRequired();

        builder.HasOne(p => p.PettyCash)
               .WithMany(p => p.Categories)
               .HasForeignKey(p => p.PettyCashId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.Property(p => p.CategoryTypeCode)
               .IsRequired();

        builder.HasOne(p => p.CategoryType)
               .WithMany()
               .HasForeignKey(p => p.CategoryTypeCode)
               .HasPrincipalKey(p => p.Code)
               .IsRequired();

        builder.OwnsAuditInfo();
    }
}