namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PJp005;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PJp005;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PJp005ProcurementSuppliesDivision : EntityTypeConfigurationBase<Domain.Procurement.PJp005.PJp005ProcurementSuppliesDivision, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<Domain.Procurement.PJp005.PJp005ProcurementSuppliesDivision> builder)
    {
        builder.ToTable(nameof(PJp005ProcurementSuppliesDivision), nameof(Domain.Procurement.Procurement));

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion();

        builder.Property(p => p.SuUserId)
               .HasVogenConversion()
               .IsRequired();

        builder.Property(p => p.FullName)
               .IsRequired();

        builder.Property(p => p.FullPositionName)
               .IsRequired();

        builder.Property(p => p.Sequence)
               .IsRequired();

        builder.OwnsAuditInfo();
    }
}