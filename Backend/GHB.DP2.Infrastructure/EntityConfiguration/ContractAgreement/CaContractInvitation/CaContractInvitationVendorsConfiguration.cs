namespace GHB.DP2.Infrastructure.EntityConfiguration.ContractAgreement.CaContractInvitation;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.ContractAgreement.CaContractInvitation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class CaContractInvitationVendorsConfiguration : EntityTypeConfigurationBase<CaContractInvitationVendors, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<CaContractInvitationVendors> builder)
    {
        builder.ToTable(nameof(CaContractInvitationVendors), nameof(ContractAgreement));

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id)
               .HasVogenConversion();

        builder.Property(i => i.PurchaseOrderApprovalContractId)
               .IsRequired();

        builder.Property(i => i.DocumentId);

        builder.Property(i => i.Email)
               .IsRequired()
               .HasMaxLength(256);

        builder.Property(i => i.ContractName)
               .IsRequired();

        builder.Property(i => i.PoNumber)
               .IsRequired();

        builder.Property(i => i.ContractNumber)
               .IsRequired();

        builder.Property(i => i.AgreedPrice)
               .IsRequired();

        builder.Property(i => i.HasContractGuarantee)
               .IsRequired();

        builder.Property(i => i.ContractGuaranteePercent);

        builder.Property(i => i.GuaranteeAmount);

        builder.HasOne(p => p.DocumentTemplateType)
               .WithMany()
               .HasForeignKey(p => p.DocumentTemplateCode)
               .HasPrincipalKey(p => p.Code);

        builder.Property(i => i.ContractOfficerName)
               .IsRequired();

        builder.Property(i => i.ContractOfficerPhone)
               .IsRequired();

        builder.Property(i => i.EgpResult);

        builder.Property(i => i.EgpRemark);

        builder.Property(i => i.EgpDate);

        builder.Property(i => i.CoiResult);

        builder.Property(i => i.CoiRemark);

        builder.Property(i => i.CoiDate);

        builder.Property(i => i.WatchlistResult);

        builder.Property(i => i.WatchlistRemark);

        builder.Property(i => i.WatchlistDate);

        builder.Property(i => i.DocumentDate);

        builder.Property(i => i.EmailSend);

        builder.Property(i => i.EmailTemplate);

        builder.HasOne(i => i.ContractInvitation)
               .WithMany(i => i.Vendors)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.Shareholders)
               .WithOne(pd => pd.ContractInvitationVendors)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Checkers)
               .WithOne(c => c.ContractInvitationVendors)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Attachments)
               .WithOne()
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.EmailAttachments)
               .WithOne()
               .OnDelete(DeleteBehavior.Cascade);

        builder.OwnDocumentHistory(i => i.DocumentHistories, documentBuilder =>
        {
            documentBuilder.ToTable(nameof(CaContractInvitationVendorsDocumentHistory), nameof(ContractAgreement));

            documentBuilder.HasKey(d => d.Id);

            documentBuilder.Property(d => d.Id)
                           .HasVogenConversion();

            documentBuilder.Property(d => d.DocumentType)
                           .HasConversion(new EnumToStringConverter<CaContractInvitationDocumentType>());

            documentBuilder.Property(d => d.StatusState)
                           .HasConversion(new EnumToStringConverter<ContractInvitationStatus>());
        });

        builder.OwnsAuditInfo();
        builder.HasSoftDelete();
    }
}