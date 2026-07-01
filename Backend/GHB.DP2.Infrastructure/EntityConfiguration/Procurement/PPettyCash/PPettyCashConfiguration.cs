namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PPettyCash;

using GHB.DP2.Domain.Procurement.PPettyCash;
using Codehard.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class PPettyCashConfiguration : EntityTypeConfigurationBase<PPettyCash, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PPettyCash> builder)
    {
        builder.ToTable(nameof(PPettyCash), nameof(Domain.Procurement.Procurement));

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion();

        builder.Property(p => p.PettyCashNumber)
               .HasVogenConversion()
               .IsRequired();

        builder.Property(p => p.PettyCashDate)
               .IsRequired();

        builder.HasOne(p => p.Department)
               .WithMany()
               .HasForeignKey(p => p.DepartmentId)
               .HasPrincipalKey(p => p.Id)
               .OnDelete(DeleteBehavior.Restrict)
               .IsRequired();

        builder.Property(p => p.BudgetYear)
               .IsRequired();

        builder.HasOne(p => p.SupplyMethod)
               .WithMany()
               .HasForeignKey(p => p.SupplyMethodCode)
               .HasPrincipalKey(p => p.Code)
               .IsRequired();

        builder.HasOne(p => p.SupplyMethodType)
               .WithMany()
               .HasForeignKey(p => p.SupplyMethodTypeCode)
               .HasPrincipalKey(p => p.Code)
               .IsRequired();

        builder.HasOne(p => p.SupplyMethodSpecialType)
               .WithMany()
               .HasForeignKey(p => p.SupplyMethodSpecialTypeCode)
               .HasPrincipalKey(p => p.Code)
               .IsRequired(false);

        builder.Property(p => p.Subject)
               .IsRequired();

        builder.Property(p => p.Source)
               .IsRequired();

        builder.Property(p => p.Reasons);

        builder.Property(p => p.DeliveryDate);

        builder.Property(p => p.DocumentDate);

        builder.Property(p => p.PettyCaseDepartmentCode);

        builder.Property(p => p.Budget)
               .IsRequired();

        builder.Property(p => p.DeliveryPeriod);

        builder.Property(p => p.DeliveryPeriodTypeCode);

        builder.HasOne(p => p.DeliveryPeriodType)
               .WithMany()
               .HasForeignKey(p => p.DeliveryPeriodTypeCode)
               .HasPrincipalKey(p => p.Code);

        builder.Property(p => p.DeliveryConditionCode);

        builder.HasOne(p => p.DeliveryCondition)
               .WithMany()
               .HasForeignKey(p => p.DeliveryConditionCode)
               .HasPrincipalKey(p => p.Code);

        builder.Property(p => p.IsAdvance)
               .IsRequired();

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
               .HasConversion(new EnumToStringConverter<PettyCashStatus>())
               .IsRequired();

        builder.Property(p => p.IsActive)
               .IsRequired();

        builder.Property(p => p.DocumentId);

        builder.Property(p => p.DisbursementDate);

        builder.Property(p => p.AnnouncementDocumentId);

        builder.Property(p => p.Telephone);

        builder.Property(p => p.CashType)
               .HasConversion(new EnumToStringConverter<CashType>())
               .HasDefaultValue(CashType.Standard)
               .IsRequired();

        builder.Property(p => p.IsFromJorPor001)
               .IsRequired(false);

        builder.HasActivityInfo();

        builder.HasMany(p => p.Categories)
               .WithOne(p => p.PettyCash)
               .HasForeignKey(p => p.PettyCashId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Vendors)
               .WithOne(p => p.PettyCash)
               .HasForeignKey(p => p.PettyCashId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.GLAccounts)
               .WithOne(p => p.PettyCash)
               .HasForeignKey(p => p.PettyCashId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Committees)
               .WithOne(p => p.PettyCash)
               .HasForeignKey(p => p.PettyCashId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Acceptors)
               .WithOne(a => a.PettyCash)
               .HasForeignKey(p => p.PettyCashId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Assignees)
               .WithOne(p => p.PettyCash)
               .HasForeignKey(p => p.PettyCashId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Attachments)
               .WithOne();

        builder.OwnDocumentHistory(p => p.DocumentHistories, documentBuilder =>
        {
            documentBuilder.ToTable(nameof(PPettyCashDocumentHistory), nameof(Procurement));

            documentBuilder.HasKey(d => d.Id);

            documentBuilder.Property(d => d.Id)
                           .HasVogenConversion();

            documentBuilder.Property(d => d.StatusState)
                           .HasConversion(new EnumToStringConverter<PettyCashStatus>());
        });

        builder.HasActivityInfo();
        builder.HasSoftDelete();
        builder.OwnsAuditInfo();
    }
}