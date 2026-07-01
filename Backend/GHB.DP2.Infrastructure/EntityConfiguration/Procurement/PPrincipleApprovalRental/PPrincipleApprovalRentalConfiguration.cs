namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PPrincipleApprovalRental;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PPrincipleApprovalRental;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class PPrincipleApprovalRentalConfiguration : EntityTypeConfigurationBase<PPrincipleApprovalRental, Dp2DbContext>
{
    protected override void EntityConfigure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<PPrincipleApprovalRental> builder)
    {
        builder.ToTable(nameof(PPrincipleApprovalRental), nameof(Domain.Procurement.Procurement));

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion();

        builder.Property(p => p.ProcurementId)
               .HasConversion<ProcurementId.EfCoreValueConverter, ProcurementId.EfCoreValueComparer>();

        builder.Property(p => p.UseContract)
               .HasConversion(new EnumToStringConverter<UseContractType>())
               .IsRequired();

        builder.Property(p => p.BranchLocation);

        builder.HasOne(p => p.RentTypeCodeInfo)
               .WithMany()
               .HasForeignKey(p => p.RentTypeCode)
               .HasPrincipalKey(p => p.Code)
               .IsRequired();

        builder.Property(p => p.RentalStartDate);

        builder.Property(p => p.RentalEndDate);

        builder.Property(p => p.RentalDurationYear);

        builder.Property(p => p.RentalDurationMonth);

        builder.Property(p => p.RentalDurationDay);

        builder.Property(p => p.MaxMonthlyRent);

        builder.Property(p => p.TotalRentalAmount);

        builder.Property(p => p.ExpectedContractDate);

        builder.Property(p => p.RentalLocationDetails);

        builder.Property(p => p.SubDistrictCode);

        builder.Property(p => p.SubDistrictName);

        builder.Property(p => p.DistrictCode);

        builder.Property(p => p.DistrictName);

        builder.Property(p => p.ProvinceCode);

        builder.Property(p => p.ProvinceName);

        builder.Property(p => p.ReferencePriceAmount);

        builder.Property(p => p.AnalysisSummaryNpv);

        builder.Property(p => p.AnalysisSummaryPaybackYearPeriod);

        builder.Property(p => p.AnalysisSummaryDiscountedPaybackYearPeriod);

        builder.Property(p => p.PhoneNumber);

        builder.Property(p => p.Status)
               .HasConversion(new EnumToStringConverter<PPrincipleApprovalRentalStatus>())
               .IsRequired();

        builder.Property(p => p.DocumentDate);

        builder.HasOne(p => p.Procurement)
               .WithMany(p => p.PrincipleApprovalRentals)
               .HasForeignKey(p => p.ProcurementId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.RentTypeCodeInfo)
               .WithMany()
               .HasForeignKey(p => p.RentTypeCode)
               .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(m => m.Acceptors)
               .WithOne(a => a.PPrincipleApprovalRental)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.Assignees)
               .WithOne(a => a.PPrincipleApprovalRental)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.PerfSupportData)
               .WithOne(a => a.PPrincipleApprovalRental)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.PerfSupportDataDetails)
               .WithOne(a => a.PPrincipleApprovalRental)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.RoiLoanAndDepositSummaries)
               .WithOne(a => a.PPrincipleApprovalRental)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.RoiPerfResults)
               .WithOne(a => a.PPrincipleApprovalRental)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.Budgets)
               .WithOne(a => a.PPrincipleApprovalRental)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.RentalAnalyses)
               .WithOne(a => a.PPrincipleApprovalRental)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.Entrepreneurs)
               .WithOne(a => a.PPrincipleApprovalRental)
               .OnDelete(DeleteBehavior.Cascade);

        builder.OwnDocumentHistory(p => p.DocumentHistories, documentBuilder =>
        {
            documentBuilder.ToTable(nameof(PPrincipleApprovalRentalDocumentHistory), nameof(Procurement));

            documentBuilder.HasKey(d => d.Id);

            documentBuilder.Property(d => d.Id)
                           .HasVogenConversion();

            documentBuilder.Property(d => d.DocumentType)
                           .HasConversion(new EnumToStringConverter<PPrincipleApprovalRentalDocumentType>());

            documentBuilder.Property(d => d.StatusState)
                           .HasConversion(new EnumToStringConverter<PPrincipleApprovalRentalStatus>());
        });

        builder.HasActivityInfo();
        builder.HasSoftDelete();
        builder.OwnsAuditInfo();
    }
}