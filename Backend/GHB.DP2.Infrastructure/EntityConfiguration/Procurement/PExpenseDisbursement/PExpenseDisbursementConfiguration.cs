namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PExpenseDisbursement;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PExpenseDisbursement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class PExpenseDisbursementConfiguration : EntityTypeConfigurationBase<Domain.Procurement.PExpenseDisbursement.PExpenseDisbursement, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<Domain.Procurement.PExpenseDisbursement.PExpenseDisbursement> builder)
    {
        builder.ToTable(nameof(Domain.Procurement.PExpenseDisbursement.PExpenseDisbursement), nameof(Procurement));

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion();

        builder.Property(p => p.SourceId)
               .IsRequired();

        builder.Property(p => p.Date)
               .IsRequired();

        builder.Property(p => p.DocumentDate);

        builder.Property(p => p.Description);

        builder.Property(p => p.IsAdvance)
               .IsRequired();

        builder.Property(p => p.IsInvoiceAmount);

        builder.Property(p => p.InvoiceAmount);

        builder.Property(p => p.AdvanceName);

        builder.HasOne(p => p.AdvancePaymentMethod)
               .WithMany()
               .HasForeignKey(p => p.AdvancePaymentMethodCode)
               .HasPrincipalKey(p => p.Code)
               .IsRequired(false);

        builder.Property(p => p.AdvancePaymentDate)
               .IsRequired(false);

        builder.HasOne(p => p.AdvanceBank)
               .WithMany()
               .HasForeignKey(p => p.AdvanceBankCode)
               .HasPrincipalKey(p => p.Code);

        builder.Property(p => p.AdvanceBankAccount);

        builder.Property(p => p.AdvanceBankCode);

        builder.Property(p => p.AdvanceBankBranch);

        builder.Property(p => p.AdvanceBankAccountName);

        builder.Property(p => p.AdvanceDetail);

        builder.Property(p => p.Status)
               .HasConversion(new EnumToStringConverter<PExpenseDisbursementStatus>())
               .IsRequired();

        builder.Property(p => p.SourceType)
               .HasConversion(new EnumToStringConverter<PExpenseDisbursementSourceType>())
               .IsRequired();

        builder.HasMany(p => p.Acceptors)
               .WithOne(a => a.PExpenseDisbursement)
               .HasForeignKey(p => p.PExpenseDisbursementId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Assignees)
               .WithOne(p => p.PExpenseDisbursement)
               .HasForeignKey(p => p.PExpenseDisbursementId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasActivityInfo();

        builder.HasSoftDelete();
        builder.OwnsAuditInfo();
    }
}