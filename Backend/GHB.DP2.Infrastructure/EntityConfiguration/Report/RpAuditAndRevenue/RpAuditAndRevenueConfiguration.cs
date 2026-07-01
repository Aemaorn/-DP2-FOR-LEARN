namespace GHB.DP2.Infrastructure.EntityConfiguration.Report.RpAuditAndRevenue;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Report.RpAuditAndRevenue;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class RpAuditAndRevenueConfiguration : EntityTypeConfigurationBase<Domain.Report.RpAuditAndRevenue.RpAuditAndRevenue, Dp2DbContext>
{
    protected override void EntityConfigure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Domain.Report.RpAuditAndRevenue.RpAuditAndRevenue> builder)
    {
        builder.ToTable(nameof(Domain.Report.RpAuditAndRevenue.RpAuditAndRevenue), nameof(Report));

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion();

        builder.Property(p => p.DocumentNumber)
               .HasMaxLength(100)
               .IsRequired();

        builder.Property(p => p.DocumentDate)
               .IsRequired();

        builder.Property(p => p.SignStartDate)
               .IsRequired();

        builder.Property(p => p.SignEndDate)
               .IsRequired();

        builder.Property(p => p.DeliveryDate)
               .IsRequired();

        builder.Property(p => p.Status)
               .HasConversion(new EnumToStringConverter<RpAuditAndRevenueStatus>())
               .IsRequired();

        builder.HasActivityInfo();

        builder.OwnDocumentHistory(p => p.DocumentHistories, documentBuilder =>
        {
            documentBuilder.ToTable(nameof(Domain.Report.RpAuditAndRevenue.RpAuditAndRevenueDocumentHistory), nameof(Report));

            documentBuilder.HasKey(d => d.Id);

            documentBuilder.Property(d => d.Id)
                           .HasVogenConversion();

            documentBuilder.Property(d => d.DocumentType)
                           .HasConversion(new EnumToStringConverter<RpAuditAndRevenueDocumentType>());

            documentBuilder.Property(d => d.StatusState)
                           .HasConversion(new EnumToStringConverter<RpAuditAndRevenueStatus>());
        });

        builder.HasSoftDelete();
        builder.OwnsAuditInfo();
    }
}