namespace GHB.DP2.Infrastructure.EntityConfiguration.ContractAgreement.CaContractDraft;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class CaContractDraftAcceptorConfiguration : EntityTypeConfigurationBase<CaContractDraftAcceptor, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<CaContractDraftAcceptor> builder)
    {
        builder.ToTable(nameof(CaContractDraftAcceptor), nameof(ContractAgreement));

        builder.AcceptorInfo();
        builder.HasSoftDelete();
        builder.OwnsAuditInfo();

        builder.HasOne(c => c.ContractDraftVendor)
               .WithMany(c => c.Acceptors);
    }
}