namespace GHB.DP2.Infrastructure.EntityConfiguration.ContractManagement.CmContractTermination;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.ContractManagement.CmContractTermination;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class CmContractTerminationConfiguration : EntityTypeConfigurationBase<CmContractTermination, Dp2DbContext>
{
    protected override void EntityConfigure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<CmContractTermination> builder)
    {
        builder.ToTable(nameof(CmContractTermination), nameof(ContractManagement));

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
               .HasVogenConversion();

        builder.Property(p => p.ContractDraftVendorId)
               .IsRequired();

        builder.Property(p => p.TerminateReason)
               .HasMaxLength(5000);

        builder.Property(p => p.TerminateDate);

        builder.Property(p => p.DocumentDate);

        builder.Property(p => p.Status)
               .HasConversion(new EnumToStringConverter<CmContractTerminationStatus>())
               .IsRequired();

        builder.Property(p => p.TerminateReasonOther)
               .HasMaxLength(5000);

        builder.Property(p => p.TerminateReasonDetail);

        builder.Property(r => r.IsProposedApprover)
               .HasDefaultValue(true)
               .IsRequired();

        builder.HasOne(t => t.TerminateTypeNavigation)
               .WithMany()
               .HasForeignKey(r => r.TerminateType)
               .HasPrincipalKey(c => c.Code)
               .OnDelete(DeleteBehavior.Restrict)
               .IsRequired(false);

        builder.HasMany(m => m.Acceptors)
               .WithOne(a => a.CmContractTermination)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.Assignees)
               .WithOne(a => a.CmContractTermination)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.CaContractDraftVendor)
               .WithMany(p => p.CmContractTerminations)
               .HasForeignKey(p => p.ContractDraftVendorId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.OwnDocumentHistory(p => p.DocumentHistories, documentBuilder =>
        {
            documentBuilder.ToTable(nameof(CmContractTerminationDocumentHistory), nameof(ContractManagement));

            documentBuilder.HasKey(d => d.Id);

            documentBuilder.Property(d => d.Id)
                           .HasVogenConversion();

            documentBuilder.Property(d => d.DocumentType)
                           .HasConversion(new EnumToStringConverter<CmContractTerminationDocumentType>());

            documentBuilder.Property(d => d.StatusState)
                           .HasConversion(new EnumToStringConverter<CmContractTerminationStatus>());
        });

        builder.OwnsAuditInfo();
        builder.HasSoftDelete();
        builder.HasActivityInfo();
    }
}