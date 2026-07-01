namespace GHB.DP2.Infrastructure.EntityConfiguration.ContractManagement.CmDeliveryAcceptance;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.ContractManagement.CmDeliveryAcceptance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class CmDeliveryAcceptancePeriodConfiguration : EntityTypeConfigurationBase<CmDeliveryAcceptancePeriod, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<CmDeliveryAcceptancePeriod> builder)
    {
        builder.ToTable(nameof(CmDeliveryAcceptancePeriod), nameof(ContractManagement));

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion();

        builder.Property(p => p.CmDeliveryAcceptanceId)
               .IsRequired();

        builder.Property(p => p.Status)
               .HasConversion(new EnumToStringConverter<CmDeliveryAcceptancePeriodStatus>())
               .IsRequired();

        builder.Property(p => p.DocumentDate);

        builder.Property(p => p.AcceptanceDate);

        builder.Property(p => p.AcceptanceNumber);

        builder.Property(p => p.Description);

        builder.Property(p => p.AcceptedAmount);

        builder.Property(p => p.HasDeduction)
               .HasDefaultValue(false);

        builder.Property(p => p.DeductionDescription);

        builder.Property(p => p.DeductionAmount);

        builder.Property(p => p.HasInvoiceSlip)
               .IsRequired()
               .HasDefaultValue(false);

        builder.Property(p => p.InvoiceSlipDescription);

        builder.Property(p => p.InvoiceSlipAmount);

        builder.Property(p => p.ObjectiveDescription);

        builder.Property(p => p.ContractBudgetAmount);

        builder.Property(p => p.AccountStatus);

        builder.Property(p => p.DisbursementDate);

        builder.Property(p => p.DisbursementAmount);

        builder.Property(p => p.DisbursementRemark);

        builder.HasOne(p => p.CmDeliveryAcceptance)
               .WithMany(p => p.Periods)
               .HasForeignKey(p => p.CmDeliveryAcceptanceId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.OwnDocumentHistory(p => p.DocumentHistories, documentBuilder =>
        {
            documentBuilder.ToTable(nameof(CmDeliveryAcceptancePeriodDocumentHistory), nameof(ContractManagement));

            documentBuilder.HasKey(d => d.Id);

            documentBuilder.Property(d => d.Id)
                           .HasVogenConversion();

            documentBuilder.Property(d => d.DocumentType)
                           .HasConversion(new EnumToStringConverter<CmDeliveryAcceptanceDocumentType>());

            documentBuilder.Property(d => d.StatusState)
                           .HasConversion(new EnumToStringConverter<CmDeliveryAcceptancePeriodStatus>());
        });

        builder.HasMany(p => p.Budgets)
               .WithOne(p => p.Period)
               .HasForeignKey(p => p.CmDeliveryAcceptancePeriodId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.PaymentTerms)
               .WithOne(p => p.Period)
               .HasForeignKey(p => p.DeliveryAcceptancePeriodId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasActivityInfo();

        builder.OwnsAuditInfo();
        builder.HasSoftDelete();
    }
}