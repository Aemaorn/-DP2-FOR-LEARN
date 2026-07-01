namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PPettyCashReimbursement;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PPettyCashReimbursement;
using GHB.DP2.Domain.Raws;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class PPettyCashReimbursementConfiguration : EntityTypeConfigurationBase<Domain.Procurement.PPettyCashReimbursement.PPettyCashReimbursement, Dp2DbContext>
{
    protected override void EntityConfigure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Domain.Procurement.PPettyCashReimbursement.PPettyCashReimbursement> builder)
    {
        builder.ToTable(nameof(PPettyCashReimbursement), nameof(Procurement));

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion();

        builder.Property(p => p.Number)
               .HasMaxLength(100)
               .IsRequired();

        builder.Property(p => p.Status)
               .HasConversion(new EnumToStringConverter<PPettyCashReimbursementStatus>())
               .IsRequired();

        builder.Property(d => d.DepartmentId)
               .HasConversion<BusinessUnitId.EfCoreValueConverter, BusinessUnitId.EfCoreValueComparer>();

        builder.Property(p => p.ReimbursementDate)
               .IsRequired();

        builder.Property(p => p.Subject)
               .IsRequired();

        builder.Property(p => p.Description);

        builder.Property(p => p.ReferredTo);

        builder.Property(p => p.BankAccountName)
               .IsRequired();

        builder.Property(p => p.BankAccountNumber)
               .IsRequired();

        builder.Property(p => p.DisbursementDate);

        builder.Property(p => p.DisbursementAmount);

        builder.Property(p => p.DisbursementDescription);

        builder.Property(p => p.DocumentDate);

        builder.HasOne(d => d.Department)
               .WithMany()
               .HasForeignKey(d => d.DepartmentId)
               .HasPrincipalKey(p => p.Id)
               .IsRequired(false);

        builder.HasMany(p => p.Attachments)
               .WithOne(p => p.PPettyCashReimbursement);

        builder.HasActivityInfo();

        builder.HasSoftDelete();
        builder.OwnsAuditInfo();
    }
}