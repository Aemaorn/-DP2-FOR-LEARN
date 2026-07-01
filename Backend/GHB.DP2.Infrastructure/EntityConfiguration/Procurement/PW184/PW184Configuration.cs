namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.Pw184;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.Pw184;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class Pw184Configuration : EntityTypeConfigurationBase<Pw184, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<Pw184> builder)
    {
        builder.ToTable(nameof(Pw184), nameof(Domain.Procurement.Procurement));

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion();

        builder.Property(p => p.Pw184Number)
               .HasVogenConversion()
               .IsRequired();

        builder.Property(p => p.Pw184Date)
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

        builder.HasOne(p => p.SupplyMethodSpecialType)
               .WithMany()
               .HasForeignKey(p => p.SupplyMethodSpecialTypeCode)
               .HasPrincipalKey(p => p.Code)
               .IsRequired(false);

        builder.Property(p => p.Subject)
               .IsRequired();

        builder.Property(p => p.Source)
               .IsRequired();

        builder.Property(p => p.Reason);

        builder.Property(p => p.Budget)
               .IsRequired();

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
               .HasPrincipalKey(p => p.Code)
               .IsRequired(false);

        builder.Property(p => p.AdvanceBankAccount);
        builder.Property(p => p.AdvanceBankBranch);
        builder.Property(p => p.AdvanceBankAccountName);
        builder.Property(p => p.AdvanceDetail);

        builder.Property(p => p.DisbursementDate);
        builder.Property(p => p.DisbursementAmount);
        builder.Property(p => p.DisbursementDescription);

        builder.Property(p => p.CurrentCommitteeSequence)
               .IsRequired();

        builder.Property(p => p.Status)
               .HasConversion(new EnumToStringConverter<Pw184Status>())
               .IsRequired();

        builder.Property(p => p.IsActive)
               .IsRequired();

        builder.Property(p => p.DocumentDate);

        builder.HasMany(p => p.Acceptors)
               .WithOne(a => a.Pw184)
               .HasForeignKey(p => p.Pw184Id)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Committees)
               .WithOne(c => c.Pw184)
               .HasForeignKey(c => c.Pw184Id)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Vendors)
               .WithOne(p => p.Pw184)
               .HasForeignKey(p => p.Pw184Id)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.GLAccounts)
               .WithOne(p => p.Pw184)
               .HasForeignKey(p => p.Pw184Id)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Attachments)
               .WithOne(p => p.Pw184);

        builder.HasActivityInfo();
        builder.HasSoftDelete();
        builder.OwnsAuditInfo();
    }
}
