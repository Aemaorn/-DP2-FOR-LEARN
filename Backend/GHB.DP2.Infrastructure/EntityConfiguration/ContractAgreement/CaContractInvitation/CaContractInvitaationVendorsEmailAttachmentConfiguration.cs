namespace GHB.DP2.Infrastructure.EntityConfiguration.ContractAgreement.CaContractInvitation;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.ContractAgreement.CaContractInvitation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class CaContractInvitaationVendorsEmailAttachmentConfiguration : EntityTypeConfigurationBase<CaContractInvitationVendorEmailAttachments>
{
    protected override void EntityConfigure(EntityTypeBuilder<CaContractInvitationVendorEmailAttachments> builder)
    {
        builder.ToTable(nameof(CaContractInvitationVendorEmailAttachments), nameof(ContractAgreement));

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
               .HasVogenConversion()
               .ValueGeneratedNever();

        builder.Property(a => a.FileId)
               .IsRequired();

        builder.Property(a => a.FileName)
               .IsRequired();

        builder.Property(a => a.Sequence)
               .HasDefaultValue(1)
               .IsRequired();

        builder.OwnsAuditInfo();
    }
}