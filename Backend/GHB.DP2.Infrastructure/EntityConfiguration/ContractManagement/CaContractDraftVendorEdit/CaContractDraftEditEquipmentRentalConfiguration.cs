namespace GHB.DP2.Infrastructure.EntityConfiguration.ContractManagement.CaContractDraftVendorEdit;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.ContractManagement.CaContractDraftVendorEdit;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class CaContractDraftEditEquipmentRentalConfiguration : EntityTypeConfigurationBase<CaContractDraftEditEquipmentRental, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<CaContractDraftEditEquipmentRental> builder)
    {
        builder.ToTable(nameof(CaContractDraftEditEquipmentRental), nameof(ContractManagement));

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
               .HasVogenConversion();

        builder.OwnsOne(
            c => c.CopierLease,
            copierLeaseBuilder =>
            {
                copierLeaseBuilder.Property(c => c.NumberOfMachines)
                                  .HasColumnName("CopierNumberOfMachines");

                copierLeaseBuilder.Property(c => c.RentalQuantity)
                                  .HasColumnName("CopierRentalQuantity");

                copierLeaseBuilder.Property(c => c.MonthlyRentalRate)
                                  .HasColumnName("CopierMonthlyRentalRate");

                copierLeaseBuilder.Property(c => c.EstimatedMonthlyCopyVolume)
                                  .HasColumnName("CopierEstimatedMonthlyCopyVolume");

                copierLeaseBuilder.Property(c => c.ActualMonthlyCopyVolume)
                                  .HasColumnName("CopierActualMonthlyCopyVolume");

                copierLeaseBuilder.Property(c => c.CopyRatePerPage)
                                  .HasColumnName("CopierCopyRatePerPage");
            });

        builder.OwnsOne(
            c => c.CarLease,
            carLeaseBuilder =>
            {
                carLeaseBuilder.Property(c => c.RentPerVehicle)
                               .HasColumnName("CarRentPerVehicle");

                carLeaseBuilder.Property(c => c.UnitCode)
                               .HasConversion<ParameterCode.EfCoreValueConverter, ParameterCode.EfCoreValueComparer>()
                               .HasColumnName("CarUnitCode");

                carLeaseBuilder.HasOne(c => c.Unit)
                               .WithMany()
                               .HasForeignKey(c => c.UnitCode)
                               .HasPrincipalKey(p => p.Code)
                               .OnDelete(DeleteBehavior.Restrict);
            });

        builder.OwnsOne(c => c.LeaseDuration, leaseDurationBuilder =>
        {
            leaseDurationBuilder.OwnsOne(
                c => c.Duration,
                durationBuilder =>
                {
                    durationBuilder.Property(c => c.Day)
                                   .HasColumnName("LeaseDurationDay");

                    durationBuilder.Property(c => c.Month)
                                   .HasColumnName("LeaseDurationMonth");

                    durationBuilder.Property(c => c.Year)
                                   .HasColumnName("LeaseDurationYear");
                });

            leaseDurationBuilder.Property(c => c.CountFromConditionCode)
                                .HasConversion<ParameterCode.EfCoreValueConverter, ParameterCode.EfCoreValueComparer>()
                                .HasColumnName("LeaseCountFromCondition");

            leaseDurationBuilder.HasOne(c => c.CountFromCondition)
                                .WithMany()
                                .HasForeignKey(c => c.CountFromConditionCode)
                                .HasPrincipalKey(p => p.Code)
                                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.OwnsAuditInfo();
    }
}
