namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PPrincipleApprovalRental;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PPrincipleApprovalRental;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PPrincipleApprovalRentalConsoPerfSupportDataDetailsConfiguration : EntityTypeConfigurationBase<PPrincipleApprovalRentalConsoPerfSupportDataDetails, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PPrincipleApprovalRentalConsoPerfSupportDataDetails> builder)
    {
        builder.ToTable(nameof(PPrincipleApprovalRentalConsoPerfSupportDataDetails), nameof(Procurement));

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion();

        builder.Property(p => p.ActivityDescription)
               .IsRequired();

        builder.Property(p => p.AccountCountYear1)
               .IsRequired();

        builder.Property(p => p.AmountYear1)
               .IsRequired();

        builder.Property(p => p.AccountCountYear2)
               .IsRequired();

        builder.Property(p => p.AmountYear2)
               .IsRequired();

        builder.OwnsAuditInfo();
    }
}