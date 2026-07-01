namespace GHB.DP2.Infrastructure.EntityConfiguration.ContractAgreement.CaContractInvitation;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.ContractAgreement.CaContractInvitation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class CaContractInvitationAcceptorConfiguration : EntityTypeConfigurationBase<CaContractInvitationAcceptor, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<CaContractInvitationAcceptor> builder)
    {
        builder.ToTable(nameof(CaContractInvitationAcceptor), nameof(ContractAgreement));

        builder.AcceptorInfo();
        builder.HasSoftDelete();
        builder.OwnsAuditInfo();

        builder.Property(c => c.ContractInvitationId)
               .HasVogenConversion()
               .IsRequired();

        builder.HasOne(c => c.ContractInvitation)
               .WithMany(c => c.Acceptors)
               .HasForeignKey(c => c.ContractInvitationId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}