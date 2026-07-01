namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.Pw119;

using GHB.DP2.Domain.Procurement.Pw119;
using Codehard.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class Pw119Configuration : EntityTypeConfigurationBase<Pw119, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<Pw119> builder)
    {
        builder.ToTable(nameof(Pw119), nameof(Domain.Procurement.Procurement));

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion();

        builder.Property(p => p.Pw119Number)
               .HasVogenConversion()
               .IsRequired();

        builder.Property(p => p.Pw119Date)
               .IsRequired();

        builder.HasOne(p => p.Department)
               .WithMany()
               .HasForeignKey(p => p.DepartmentId)
               .HasPrincipalKey(p => p.Id)
               .OnDelete(DeleteBehavior.Restrict)
               .IsRequired();

        builder.Property(p => p.BudgetYear)
               .IsRequired();

        builder.HasOne(p => p.AssignSegment)
               .WithMany()
               .HasForeignKey(p => p.AssignSegmentCode)
               .HasPrincipalKey(p => p.Code);

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

        builder.Property(p => p.Budget)
               .IsRequired();

        builder.Property(p => p.MedianPrice);

        builder.HasOne(p => p.W119Categories)
               .WithMany()
               .HasForeignKey(p => p.W119CategoriesCode)
               .HasPrincipalKey(p => p.Code)
               .IsRequired();

        builder.Property(p => p.Reason);

        builder.Property(p => p.IsAdvance)
               .IsRequired();

        builder.Property(p => p.AdvanceName);

        builder.Property(p => p.Telephone);

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

        builder.Property(p => p.DisbursementDate);

        builder.Property(p => p.DisbursementAmount);

        builder.Property(p => p.DisbursementDescription);

        builder.Property(p => p.Status)
               .HasConversion(new EnumToStringConverter<Pw119Status>())
               .IsRequired();

        builder.Property(p => p.IsActive)
               .IsRequired();

        builder.Property(p => p.DocumentDate);

        builder.Property(p => p.DocumentId);

        builder.Property(p => p.AnnouncementDocumentId);

        builder.HasActivityInfo();

        builder.HasMany(p => p.Acceptors)
               .WithOne(a => a.Pw119)
               .HasForeignKey(p => p.Pw119Id)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Vendors)
               .WithOne(p => p.Pw119)
               .HasForeignKey(p => p.Pw119Id)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Vendors)
               .WithOne(p => p.Pw119)
               .HasForeignKey(p => p.Pw119Id)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.GLAccounts)
               .WithOne(p => p.Pw119)
               .HasForeignKey(p => p.Pw119Id)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Attachments)
               .WithOne(p => p.Pw119);

        builder.OwnDocumentHistory(p => p.DocumentHistories, documentBuilder =>
        {
            documentBuilder.ToTable(nameof(Pw119DocumentHistory), nameof(Procurement));

            documentBuilder.HasKey(d => d.Id);

            documentBuilder.Property(d => d.Id)
                           .HasVogenConversion();

            documentBuilder.Property(d => d.DocumentType)
                           .HasConversion(new EnumToStringConverter<Pw119DocumentType>());

            documentBuilder.Property(d => d.StatusState)
                           .HasConversion(new EnumToStringConverter<Pw119Status>());
        });

        builder.HasActivityInfo();
        builder.HasSoftDelete();
        builder.OwnsAuditInfo();
    }
}