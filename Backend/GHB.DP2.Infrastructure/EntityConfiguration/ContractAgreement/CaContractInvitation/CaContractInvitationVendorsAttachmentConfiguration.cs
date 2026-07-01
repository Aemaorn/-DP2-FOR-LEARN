namespace GHB.DP2.Infrastructure.EntityConfiguration.ContractAgreement.CaContractInvitation;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.ContractAgreement.CaContractInvitation;
using GHB.DP2.Domain.Procurement.PPurchaseOrder;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class CaContractInvitationVendorsAttachmentConfiguration : EntityTypeConfigurationBase<CaContractInvitationVendorAttachments>
{
    protected override void EntityConfigure(EntityTypeBuilder<CaContractInvitationVendorAttachments> builder)
    {
        builder.ToTable(nameof(CaContractInvitationVendorAttachments), nameof(Domain.ContractAgreement));

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
               .HasVogenConversion()
               .ValueGeneratedNever();

        builder.Property(a => a.Type)
               .HasConversion(new EnumToStringConverter<EntrepreneurAttachmentType>())
               .IsRequired();

        builder.Property(a => a.FileId)
               .IsRequired();

        builder.Property(a => a.FileName)
               .IsRequired();

        builder.Property(a => a.IsPublic)
               .IsRequired();

        builder.Property(a => a.Sequence)
               .HasDefaultValue(1)
               .IsRequired();

        builder.HasOne(a => a.DocumentType)
               .WithMany()
               .HasForeignKey(a => a.DocumentTypeCode)
               .HasPrincipalKey(p => p.Code)
               .OnDelete(DeleteBehavior.Restrict);

        builder.OwnsAuditInfo();
    }
}