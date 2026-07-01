namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PPurchaseOrder;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.PPurchaseOrder;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class PPurchaseOrderEntrepreneurShareholderCheckerConfiguration : EntityTypeConfigurationBase<PPurchaseOrderEntrepreneurShareholderChecker, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PPurchaseOrderEntrepreneurShareholderChecker> builder)
    {
        builder.ToTable(nameof(PPurchaseOrderEntrepreneurShareholderChecker), nameof(Procurement));

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
               .HasVogenConversion()
               .IsRequired();

        builder.Property(e => e.CheckType)
               .HasConversion(new EnumToStringConverter<QualificationType>())
               .IsRequired();

        builder.Property(e => e.ResultAt)
               .IsRequired();

        builder.Property(e => e.Result)
               .HasConversion(new EnumToStringConverter<QualificationResult>())
               .IsRequired();

        builder.Property(e => e.Remark);

        builder.Property(e => e.CreatedBy)
               .IsRequired();

        builder.Property(e => e.CreatedByName)
               .IsRequired();

        builder.Property(e => e.CreatedAt)
               .IsRequired();
    }
}