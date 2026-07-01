namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PPrincipleApprovalRental;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PPrincipleApprovalRental;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PPrincipleApprovalRentalEntrepreneursConfiguration : EntityTypeConfigurationBase<PPrincipleApprovalRentalEntrepreneurs, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PPrincipleApprovalRentalEntrepreneurs> builder)
    {
        builder.ToTable(nameof(PPrincipleApprovalRentalEntrepreneurs), nameof(Procurement));

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id)
               .HasVogenConversion()
               .IsRequired();

        builder.Property(i => i.EmailSend)
               .IsRequired();

        builder.Property(i => i.Sequence)
               .IsRequired();

        builder.Property(i => i.WatchlistResult);

        builder.Property(i => i.WatchlistResultRemark);

        builder.Property(i => i.WatchlistResultAt);

        builder.Property(i => i.CoiResult);

        builder.Property(i => i.CoiResultRemark);

        builder.Property(i => i.CoiResultAt);

        builder.Property(i => i.EgpResult);

        builder.Property(i => i.EgpResultRemark)
               .HasMaxLength(2000);

        builder.Property(i => i.EgpResultAt);

        builder.HasMany(m => m.EntrepreneursShareholders)
               .WithOne(d => d.Entrepreneurs)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.EntrepreneursPriceDetails)
               .WithOne(d => d.Entrepreneurs)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Checkers)
               .WithOne(c => c.PrincipleApprovalRentalEntrepreneursI)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Attachments)
               .WithOne()
               .OnDelete(DeleteBehavior.Cascade);

        builder.OwnsAuditInfo();
        builder.HasSoftDelete();
    }
}