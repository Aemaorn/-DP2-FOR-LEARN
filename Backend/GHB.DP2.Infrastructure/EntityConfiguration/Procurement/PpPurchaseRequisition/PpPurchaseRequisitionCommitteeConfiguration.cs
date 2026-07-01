namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PpPurchaseRequisition;

using GHB.DP2.Domain.Procurement.PpPurchaseRequisition;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class PpPurchaseRequisitionCommitteeConfiguration : IEntityTypeConfiguration<PpPurchaseRequisitionCommittee>
{
    public void Configure(EntityTypeBuilder<PpPurchaseRequisitionCommittee> builder)
    {
        builder.ToTable(nameof(PpPurchaseRequisitionCommittee), nameof(Procurement));

        builder.HasKey(x => x.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion();

        builder.Property(p => p.PpPurchaseRequisitionId)
               .HasConversion<PpPurchaseRequisitionId.EfCoreValueConverter, PpPurchaseRequisitionId.EfCoreValueComparer>()
               .IsRequired();

        builder.Property(x => x.GroupType)
               .HasConversion(new EnumToStringConverter<GroupType>())
               .IsRequired();

        builder.Property(x => x.SuUserId)
               .IsRequired();

        builder.Property(x => x.FullName)
               .HasMaxLength(2000)
               .IsRequired();

        builder.Property(x => x.FullPositionName)
               .HasMaxLength(2000);

        builder.HasOne(m => m.CommitteePositions)
               .WithMany()
               .HasForeignKey(m => m.CommitteePositionsCode)
               .HasPrincipalKey(m => m.Code)
               .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.CommitteePositionsName)
               .HasMaxLength(2000)
               .IsRequired();

        builder.Property(x => x.Sequence).IsRequired();

        builder.HasOne(x => x.PpPurchaseRequisition)
            .WithMany(x => x.Committees)
            .HasForeignKey(x => x.PpPurchaseRequisitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.SuUserId);

        builder.HasOne(x => x.CommitteePositions)
            .WithMany()
            .HasForeignKey(x => x.CommitteePositionsCode)
            .HasPrincipalKey(x => x.Code);

        builder.OwnsAuditInfo();
    }
}