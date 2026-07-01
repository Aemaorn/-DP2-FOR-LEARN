namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.P79Clause2;

using GHB.DP2.Domain.Procurement.P79Clause2;
using Codehard.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class P79Clause2Configuration : EntityTypeConfigurationBase<P79Clause2, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<P79Clause2> builder)
    {
        builder.ToTable(nameof(P79Clause2), nameof(Domain.Procurement.Procurement));

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion();

        builder.Property(p => p.P79Clause2Number)
               .HasVogenConversion()
               .IsRequired();

        builder.Property(p => p.P79Clause2Date)
               .IsRequired();

        builder.Property(p => p.DocumentDate);

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

        builder.Property(p => p.Budget)
               .IsRequired();

        builder.Property(p => p.MedianPrice);

        builder.Property(p => p.ReasonItem1);

        builder.Property(p => p.ReasonItem2);

        builder.Property(p => p.ReasonItem3);

        builder.Property(p => p.IsAdvance)
               .IsRequired();

        builder.Property(p => p.DeliveryDate);

        builder.Property(p => p.ProcurementReasonItem1);

        builder.Property(p => p.ProcurementReasonItem2);

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

        builder.Property(p => p.Telephone);

        builder.Property(p => p.AdvanceBankBranch);

        builder.Property(p => p.AdvanceBankAccountName);

        builder.Property(p => p.AdvanceDetail);

        builder.Property(p => p.DisbursementDate);

        builder.Property(p => p.DisbursementAmount);

        builder.Property(p => p.DisbursementDescription);

        builder.Property(p => p.Status)
               .HasConversion(new EnumToStringConverter<P79Clause2Status>())
               .IsRequired();

        builder.Property(p => p.IsActive)
               .IsRequired();

        builder.Property(p => p.DocumentId);

        builder.Property(p => p.AnnouncementDocumentId);

        builder.HasActivityInfo();

        builder.HasMany(p => p.Acceptors)
               .WithOne(a => a.P79Clause2)
               .HasForeignKey(p => p.P79Clause2Id)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Vendors)
               .WithOne(p => p.P79Clause2)
               .HasForeignKey(p => p.P79Clause2Id)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.GLAccounts)
               .WithOne(p => p.P79Clause2)
               .HasForeignKey(p => p.P79Clause2Id)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Attachments)
               .WithOne(p => p.P79Clause2);

        builder.OwnDocumentHistory(p => p.DocumentHistories, documentBuilder =>
        {
            documentBuilder.ToTable(nameof(P79Clause2DocumentHistory), nameof(Procurement));

            documentBuilder.HasKey(d => d.Id);

            documentBuilder.Property(d => d.Id)
                           .HasVogenConversion();

            documentBuilder.Property(d => d.DocumentType)
                           .HasConversion(new EnumToStringConverter<P79Clause2DocumentType>());

            documentBuilder.Property(d => d.StatusState)
                           .HasConversion(new EnumToStringConverter<P79Clause2Status>());
        });

        builder.HasActivityInfo();
        builder.HasSoftDelete();
        builder.OwnsAuditInfo();
    }
}