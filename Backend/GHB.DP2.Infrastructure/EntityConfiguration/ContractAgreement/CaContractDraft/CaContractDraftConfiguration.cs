namespace GHB.DP2.Infrastructure.EntityConfiguration.ContractAgreement.CaContractDraft;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class CaContractDraftConfiguration : EntityTypeConfigurationBase<CaContractDraft, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<CaContractDraft> builder)
    {
        builder.ToTable(nameof(CaContractDraft), nameof(ContractAgreement));

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
               .HasVogenConversion();

        builder.Property(c => c.Status)
               .HasConversion(new EnumToStringConverter<ContractDraftStatus>())
               .IsRequired();

        builder.HasOne(c => c.Procurement)
               .WithMany(p => p.ContractDrafts)
               .HasForeignKey(c => c.ProcurementId);

        builder.OwnsAuditInfo();
        builder.HasSoftDelete();
        builder.HasActivityInfo();
    }
}