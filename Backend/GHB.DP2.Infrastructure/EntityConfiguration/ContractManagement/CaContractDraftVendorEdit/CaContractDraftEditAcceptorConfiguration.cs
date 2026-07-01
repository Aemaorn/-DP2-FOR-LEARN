namespace GHB.DP2.Infrastructure.EntityConfiguration.ContractManagement.CaContractDraftVendorEdit;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.ContractManagement.CaContractDraftVendorEdit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class CaContractDraftEditAcceptorConfiguration : EntityTypeConfigurationBase<CaContractDraftEditAcceptor, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<CaContractDraftEditAcceptor> builder)
    {
        builder.ToTable(nameof(CaContractDraftEditAcceptor), nameof(ContractManagement));

        builder.Property(m => m.IsUnableToPerformDuties)
               .IsRequired();

        builder.HasOne(m => m.CommitteePosition)
               .WithMany()
               .HasForeignKey(m => m.CommitteePositionsCode)
               .HasPrincipalKey(m => m.Code)
               .OnDelete(DeleteBehavior.Restrict);

        builder.AcceptorInfo();
        builder.HasSoftDelete();
        builder.OwnsAuditInfo();

        builder.HasOne(c => c.CaContractDraftVendorEdit)
               .WithMany(c => c.Acceptors);
    }
}
