namespace GHB.DP2.Infrastructure.EntityConfiguration.ContractAmendment.CamContractAmendment.CamContractAmendmentPoAddendum;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment.CamContractAmendmentPoAddendum;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class CamContractAmendmentPoAddendumConfiguration : EntityTypeConfigurationBase<CamContractAmendmentPoAddendum, Dp2DbContext>
{
    protected override void EntityConfigure(
        Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<CamContractAmendmentPoAddendum> builder)
    {
        builder.ToTable(nameof(CamContractAmendmentPoAddendum), nameof(Domain.ContractAmendment.ContractAmendment));

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
               .HasVogenConversion();

        builder.Property(p => p.ContractNumber);

        builder.Property(p => p.SapNumber);

        builder.Property(p => p.SapNumber);

        builder.Property(p => p.Status)
               .HasConversion(new EnumToStringConverter<CamContractAmendmentPoAddendumStatus>())
               .IsRequired();

        builder.HasMany(m => m.Acceptors)
               .WithOne(a => a.CamContractAmendmentPoAddendum)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.Assignees)
               .WithOne(a => a.CamContractAmendmentPoAddendum)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.PaymentTerms)
               .WithOne(a => a.CamContractAmendmentPoAddendum)
               .OnDelete(DeleteBehavior.Cascade);

        builder.OwnDocumentHistory(p => p.DocumentHistories, documentBuilder =>
        {
            documentBuilder.ToTable(nameof(CamContractAmendmentPoAddendumDocumentHistory), nameof(Domain.ContractAmendment.ContractAmendment));

            documentBuilder.HasKey(d => d.Id);

            documentBuilder.Property(d => d.Id)
                           .HasVogenConversion();

            documentBuilder.Property(d => d.DocumentType)
                           .HasConversion(new EnumToStringConverter<CamContractAmendmentPoAddendumDocumentType>());

            documentBuilder.Property(d => d.StatusState)
                           .HasConversion(new EnumToStringConverter<CamContractAmendmentPoAddendumStatus>());
        });

        builder.OwnsAuditInfo();
        builder.HasActivityInfo();
    }
}