namespace GHB.DP2.Infrastructure.EntityConfiguration.ContractAgreement.CaContractInvitation;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.ContractAgreement.CaContractInvitation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class CaContractInvitationVendorsShareholdersConfiguration : EntityTypeConfigurationBase<CaContractInvitationVendorShareholders, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<CaContractInvitationVendorShareholders> builder)
    {
        builder.ToTable(nameof(CaContractInvitationVendorShareholders), nameof(ContractAgreement));

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
               .HasVogenConversion()
               .IsRequired();

        builder.Property(e => e.Sequence)
               .IsRequired();

        builder.Property(e => e.TaxId)
               .HasMaxLength(20);

        builder.Property(e => e.FirstName)
               .HasMaxLength(200);

        builder.Property(e => e.LastName)
               .HasMaxLength(200);

        builder.Property(e => e.IsDirector);

        builder.Property(e => e.IsShareholder);

        builder.Property(e => e.IsJuristic);

        builder.Property(e => e.CheckType);

        builder.Property(e => e.WatchlistResult);

        builder.Property(e => e.WatchlistResultRemark);

        builder.Property(e => e.WatchlistResultAt);

        builder.Property(e => e.CoiResult);

        builder.Property(e => e.CoiResultRemark);

        builder.Property(e => e.CoiResultAt);

        builder.Property(e => e.EgpResult);

        builder.Property(e => e.EgpRemark);

        builder.Property(e => e.EgpResultAt);

        builder.HasMany(e => e.VendorShareholderCheckers)
               .WithOne(c => c.ContractInvitationVendorShareholders)
               .OnDelete(DeleteBehavior.Cascade);

        builder.OwnsAuditInfo();
        builder.HasSoftDelete();
    }
}