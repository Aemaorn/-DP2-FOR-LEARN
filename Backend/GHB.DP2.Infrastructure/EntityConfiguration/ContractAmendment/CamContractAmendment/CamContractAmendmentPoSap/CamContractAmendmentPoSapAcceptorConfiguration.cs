namespace GHB.DP2.Infrastructure.EntityConfiguration.ContractAmendment.CamContractAmendment.CamContractAmendmentPoSap;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment.CamContractAmendmentPoSap;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class CamContractAmendmentPoSapAcceptorConfiguration : EntityTypeConfigurationBase<CamContractAmendmentPoSapAcceptor, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<CamContractAmendmentPoSapAcceptor> builder)
    {
        builder.ToTable(nameof(CamContractAmendmentPoSapAcceptor), nameof(Domain.ContractAmendment.ContractAmendment));

        builder.AcceptorInfo();
        builder.OwnsAuditInfo();
    }
}