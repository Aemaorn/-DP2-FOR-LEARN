namespace GHB.DP2.Infrastructure.EntityConfiguration.ContractManagement.CmContractGuaranteeReturn;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.ContractManagement.CmContractGuaranteeReturn;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class CmContractGuaranteeReturnConfiguration : EntityTypeConfigurationBase<Domain.ContractManagement.CmContractGuaranteeReturn.CmContractGuaranteeReturn, Dp2DbContext>
{
    protected override void EntityConfigure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<CmContractGuaranteeReturn> builder)
    {
        builder.ToTable(nameof(CmContractGuaranteeReturn), nameof(ContractManagement));

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
               .HasVogenConversion();

        builder.Property(p => p.ContractDraftVendorId)
               .IsRequired();

        builder.Property(p => p.GuaranteeReturnDate)
               .IsRequired();

        builder.Property(p => p.ReturnAmount)
               .IsRequired();

        builder.Property(p => p.IsDeducted)
               .IsRequired();

        builder.Property(p => p.DeductedAmount);

        builder.Property(p => p.NetReturnAmount)
               .IsRequired();

        builder.Property(p => p.AdditionalComment)
               .HasMaxLength(5000)
               .IsRequired(false);

        builder.Property(p => p.ContractDescription);

        builder.Property(p => p.GuranteeDescription);

        builder.Property(p => p.ProofOfPaymentDescription);

        builder.Property(p => p.GuaranteeNumber)
               .HasVogenConversion()
               .IsRequired()
               .HasDefaultValue(GuaranteeReturnNumber.From(string.Empty));

        builder.Property(p => p.DisbursementDate);

        builder.Property(p => p.DisbursementAmount);

        builder.Property(p => p.DisbursementRemark);

        builder.Property(p => p.Status)
               .HasConversion(new EnumToStringConverter<CmContractGuaranteeReturnStatus>())
               .IsRequired();

        builder.Property(p => p.DocumentDate);

        builder.HasMany(m => m.Acceptors)
               .WithOne(a => a.CmContractGuaranteeReturn)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.Assignees)
               .WithOne(a => a.CmContractGuaranteeReturn)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.Conditions)
               .WithOne(a => a.CmContractGuaranteeReturn)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.RequiredDocuments)
               .WithOne(a => a.CmContractGuaranteeReturn)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.CaContractDraftVendor)
               .WithMany(p => p.CmContractGuaranteeReturns)
               .HasForeignKey(p => p.ContractDraftVendorId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.OwnDocumentHistory(p => p.DocumentHistories, documentBuilder =>
        {
            documentBuilder.ToTable(nameof(CmContractGuaranteeReturnDocumentHistory), nameof(ContractManagement));

            documentBuilder.HasKey(d => d.Id);

            documentBuilder.Property(d => d.Id)
                           .HasVogenConversion();

            documentBuilder.Property(d => d.DocumentType)
                           .HasConversion(new EnumToStringConverter<CmContractGuaranteeReturnDocumentType>());

            documentBuilder.Property(d => d.StatusState)
                           .HasConversion(new EnumToStringConverter<CmContractGuaranteeReturnStatus>());
        });

        builder.HasMany(p => p.Attachments)
               .WithOne(p => p.CmContractGuaranteeReturn);

        builder.Property(i => i.IsSendMail)
               .HasDefaultValue(false);

        builder.Property(i => i.EmailSend);

        builder.Property(i => i.EmailTemplate);

        builder.HasMany(p => p.EmailAttachments)
               .WithOne()
               .OnDelete(DeleteBehavior.Cascade);

        builder.OwnsAuditInfo();
        builder.HasSoftDelete();
        builder.HasActivityInfo();
    }
}