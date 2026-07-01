namespace GHB.DP2.Infrastructure.EntityConfiguration.ContractManagement.CmContractGuaranteeReturn;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.ContractManagement.CmContractGuaranteeReturn;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class CmContractGuaranteeReturnAcceptorConfiguration : EntityTypeConfigurationBase<CmContractGuaranteeReturnAcceptor, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<CmContractGuaranteeReturnAcceptor> builder)
    {
        builder.ToTable(nameof(CmContractGuaranteeReturnAcceptor), nameof(ContractManagement));

        builder.Property(m => m.IsUnableToPerformDuties)
               .IsRequired();

        builder.HasOne(m => m.CommitteePosition)
               .WithMany()
               .HasForeignKey(m => m.CommitteePositionsCode)
               .HasPrincipalKey(m => m.Code)
               .OnDelete(DeleteBehavior.Restrict);

        builder.AcceptorInfo();
        builder.OwnsAuditInfo();
        builder.HasSoftDelete();
    }
}