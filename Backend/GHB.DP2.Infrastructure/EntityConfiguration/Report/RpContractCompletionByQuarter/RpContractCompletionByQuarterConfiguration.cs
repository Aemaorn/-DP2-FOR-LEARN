namespace GHB.DP2.Infrastructure.EntityConfiguration.Report.RpContractCompletionByQuarter;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Report.RpContractCompletionByQuarter;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class RpContractCompletionByQuarterConfiguration : EntityTypeConfigurationBase<RpContractCompletionByQuarter, Dp2DbContext>
{
    protected override void EntityConfigure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<RpContractCompletionByQuarter> builder)
    {
        builder.ToTable(nameof(RpContractCompletionByQuarter), nameof(Report));

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion();

        builder.Property(p => p.DocumentNumber)
               .HasMaxLength(100)
               .IsRequired();

        builder.Property(p => p.Year)
               .IsRequired();

        builder.Property(p => p.Quarter)
               .IsRequired();

        builder.Property(p => p.DocumentDate)
               .IsRequired();

        builder.Property(p => p.SignStartDate)
               .IsRequired();

        builder.Property(p => p.SignEndDate)
               .IsRequired();

        builder.Property(p => p.Status)
               .HasConversion(new EnumToStringConverter<RpContractCompletionByQuarterStatus>())
               .IsRequired();

        builder.Property(p => p.DocumentId);

        builder.OwnDocumentHistory(p => p.DocumentHistories, documentBuilder =>
        {
            documentBuilder.ToTable(nameof(RpContractCompletionByQuarterDocumentHistory), nameof(Report));

            documentBuilder.HasKey(d => d.Id);

            documentBuilder.Property(d => d.Id)
                           .HasVogenConversion();

            documentBuilder.Property(d => d.DocumentType)
                           .HasConversion(new EnumToStringConverter<RpContractCompletionByQuarterDocumentType>());

            documentBuilder.Property(d => d.StatusState)
                           .HasConversion(new EnumToStringConverter<RpContractCompletionByQuarterStatus>());
        });

        builder.HasActivityInfo();
        builder.HasSoftDelete();
        builder.OwnsAuditInfo();
    }
}