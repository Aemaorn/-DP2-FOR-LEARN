namespace GHB.DP2.Infrastructure.EntityConfiguration.ContractManagement.CaContractDraftVendorAmendment;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.ContractManagement.CaContractDraftVendorAmendment;
using GHB.DP2.Domain.ContractManagement.CaContractDraftVendorEdit;
using GHB.DP2.Domain.ContractManagement.ContractManagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class CaContractDraftVendorAmendmentConfiguration : EntityTypeConfigurationBase<CaContractDraftVendorAmendment, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<CaContractDraftVendorAmendment> builder)
    {
        builder.ToTable(nameof(CaContractDraftVendorAmendment), nameof(Domain.ContractManagement));

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
               .HasVogenConversion();

        builder.Property(c => c.ContractManagementId)
               .HasConversion<ContractManagementId.EfCoreValueConverter, ContractManagementId.EfCoreValueComparer>();

        builder.Property(c => c.CaContractDraftVendorEditId)
               .HasConversion<ContractDraftVendorEditId.EfCoreValueConverter, ContractDraftVendorEditId.EfCoreValueComparer>();

        builder.Property(c => c.Status)
               .HasConversion(new EnumToStringConverter<CaContractDraftVendorAmendmentStatus>())
               .IsRequired();

        builder.HasOne(c => c.ContractManagement)
               .WithMany()
               .HasForeignKey(c => c.ContractManagementId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.CaContractDraftVendorEdit)
               .WithMany()
               .HasForeignKey(c => c.CaContractDraftVendorEditId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.Acceptors)
               .WithOne(a => a.CaContractDraftVendorAmendment)
               .OnDelete(DeleteBehavior.Cascade);

        builder.OwnDocumentHistory(c => c.DocumentHistories, documentBuilder =>
        {
            documentBuilder.ToTable(nameof(CaContractDraftVendorAmendmentDocumentHistory), nameof(Domain.ContractManagement));

            documentBuilder.HasKey(d => d.Id);

            documentBuilder.Property(d => d.Id)
                           .HasVogenConversion();

            documentBuilder.Property(d => d.DocumentType)
                           .HasConversion(new EnumToStringConverter<CaContractDraftVendorAmendmentDocumentType>());

            documentBuilder.Property(d => d.StatusState)
                           .HasConversion(new EnumToStringConverter<CaContractDraftVendorAmendmentStatus>());
        });

        builder.OwnsAuditInfo();
        builder.HasActivityInfo();
    }
}
