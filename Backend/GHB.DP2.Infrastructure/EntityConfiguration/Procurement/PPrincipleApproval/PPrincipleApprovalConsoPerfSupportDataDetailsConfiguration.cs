namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PPrincipleApproval;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PPrincipleApproval;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PPrincipleApprovalConsoPerfSupportDataDetailsConfiguration : EntityTypeConfigurationBase<PPrincipleApprovalConsoPerfSupportDataDetails, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PPrincipleApprovalConsoPerfSupportDataDetails> builder)
    {
        builder.ToTable(nameof(PPrincipleApprovalConsoPerfSupportDataDetails), nameof(Procurement));

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