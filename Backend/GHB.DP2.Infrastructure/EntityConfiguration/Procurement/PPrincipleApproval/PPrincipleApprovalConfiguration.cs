namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PPrincipleApproval;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PPrincipleApproval;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class PPrincipleApprovalConfiguration : EntityTypeConfigurationBase<Domain.Procurement.PPrincipleApproval.PPrincipleApproval, Dp2DbContext>
{
    protected override void EntityConfigure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Domain.Procurement.PPrincipleApproval.PPrincipleApproval> builder)
    {
        builder.ToTable(nameof(Domain.Procurement.PPrincipleApproval.PPrincipleApproval), nameof(Domain.Procurement.Procurement));

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion();

        builder.Property(p => p.ProcurementId)
               .HasConversion<ProcurementId.EfCoreValueConverter, ProcurementId.EfCoreValueComparer>();

        builder.Property(p => p.BranchLocation)
               .HasMaxLength(2000);

        builder.Property(p => p.RentalStartDate)
               .IsRequired();

        builder.Property(p => p.RentalEndDate)
               .IsRequired();

        builder.Property(p => p.RentalDurationYear)
               .IsRequired();

        builder.Property(p => p.RentalDurationMonth)
               .IsRequired();

        builder.Property(p => p.RentalDurationDay)
               .IsRequired();

        builder.Property(p => p.MaxMonthlyRent)
               .IsRequired();

        builder.Property(p => p.TotalRentalAmount)
               .IsRequired();

        builder.Property(p => p.ExpectedContractDate)
               .IsRequired();

        builder.Property(p => p.RentalLocationDetails)
               .IsRequired();

        builder.Property(p => p.SubDistrictCode)
               .IsRequired();

        builder.Property(p => p.SubDistrictName)
               .IsRequired();

        builder.Property(p => p.DistrictCode)
               .IsRequired();

        builder.Property(p => p.DistrictName)
               .IsRequired();

        builder.Property(p => p.ProvinceCode)
               .IsRequired();

        builder.Property(p => p.ProvinceName)
               .IsRequired();

        builder.Property(p => p.ReferencePriceAmount);

        builder.Property(p => p.AnalysisSummaryNpv);

        builder.Property(p => p.AnalysisSummaryPaybackYearPeriod);

        builder.Property(p => p.AnalysisSummaryDiscountedPaybackYearPeriod);

        builder.Property(p => p.PhoneNumber);

        builder.Property(p => p.IsRentCommittee)
               .IsRequired();

        builder.Property(p => p.IsAcceptanceCommittee)
               .IsRequired();

        builder.HasOne(p => p.RentTypeCodeInfo)
               .WithMany()
               .HasForeignKey(p => p.RentTypeCode)
               .HasPrincipalKey(p => p.Code)
               .IsRequired();

        builder.Property(p => p.Status)
               .HasConversion(new EnumToStringConverter<PPrincipleApprovalStatus>())
               .IsRequired();

        builder.Property(p => p.DocumentDate);

        builder.HasOne(p => p.Procurement)
               .WithMany(p => p.PrincipleApprovals)
               .HasForeignKey(p => p.ProcurementId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.PrincipleApprovalAcceptors)
               .WithOne(a => a.PPrincipleApproval)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.PrincipleApprovalAssignees)
               .WithOne(a => a.PPrincipleApproval)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.PrincipleApprovalCommittees)
               .WithOne(a => a.PPrincipleApproval)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.PerfSupportData)
               .WithOne(a => a.PPrincipleApproval)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.PerfSupportDataDetails)
               .WithOne(a => a.PPrincipleApproval)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.RoiLoanAndDepositSummaries)
               .WithOne(a => a.PPrincipleApproval)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.RoiPerfResults)
               .WithOne(a => a.PPrincipleApproval)
               .OnDelete(DeleteBehavior.Cascade)
               .IsRequired(false);

        builder.HasMany(m => m.PrincipleApprovalBudgets)
               .WithOne(a => a.PPrincipleApproval)
               .OnDelete(DeleteBehavior.Cascade)
               .IsRequired(false);

        builder.OwnDocumentHistory(p => p.DocumentHistories, documentBuilder =>
        {
            documentBuilder.ToTable(nameof(PPrincipleApprovalDocumentHistory), nameof(Procurement));

            documentBuilder.HasKey(d => d.Id);

            documentBuilder.Property(d => d.Id)
                           .HasVogenConversion();

            documentBuilder.Property(d => d.StatusState)
                           .HasConversion(new EnumToStringConverter<PPrincipleApprovalStatus>());
        });

        builder.HasOne(m => m.DocumentTemplate)
               .WithMany()
               .HasForeignKey(m => m.DocumentTemplateId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Attachments)
               .WithOne()
               .HasForeignKey(a => a.PPrincipleApprovalId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasActivityInfo();
        builder.HasSoftDelete();
        builder.OwnsAuditInfo();
    }
}