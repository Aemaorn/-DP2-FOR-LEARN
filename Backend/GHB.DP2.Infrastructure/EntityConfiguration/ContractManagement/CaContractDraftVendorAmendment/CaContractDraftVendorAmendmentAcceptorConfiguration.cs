namespace GHB.DP2.Infrastructure.EntityConfiguration.ContractManagement.CaContractDraftVendorAmendment;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.ContractManagement.CaContractDraftVendorAmendment;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class CaContractDraftVendorAmendmentAcceptorConfiguration : EntityTypeConfigurationBase<CaContractDraftVendorAmendmentAcceptor, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<CaContractDraftVendorAmendmentAcceptor> builder)
    {
        builder.ToTable(nameof(CaContractDraftVendorAmendmentAcceptor), nameof(Domain.ContractManagement));

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

        builder.HasOne(c => c.CaContractDraftVendorAmendment)
               .WithMany(c => c.Acceptors);
    }
}
