namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PPurchaseOrderApproval;

using GHB.DP2.Domain.Procurement.PPurchaseOrderApproval;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class PPurchaseOrderApprovalCommitteeConfiguration : IEntityTypeConfiguration<PPurchaseOrderApprovalCommittee>
{
    public void Configure(EntityTypeBuilder<PPurchaseOrderApprovalCommittee> builder)
    {
        builder.ToTable(nameof(PPurchaseOrderApprovalCommittee), nameof(Procurement));

        builder.HasKey(x => x.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion();

        builder.Property(p => p.PurchaseOrderApprovalId)
               .HasConversion<PurchaseOrderApprovalId.EfCoreValueConverter, PurchaseOrderApprovalId.EfCoreValueComparer>()
               .IsRequired();

        builder.Property(x => x.GroupType)
               .HasConversion(new EnumToStringConverter<GroupType>())
               .IsRequired();

        builder.Property(x => x.SuUserId)
               .IsRequired();

        builder.Property(x => x.FullName)
               .IsRequired();

        builder.Property(p => p.FullPositionName)
              .IsRequired();

        builder.HasOne(m => m.CommitteePositions)
               .WithMany()
               .HasForeignKey(m => m.CommitteePositionsCode)
               .HasPrincipalKey(m => m.Code)
               .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.CommitteePositionsName)
               .IsRequired();

        builder.Property(x => x.Sequence).IsRequired();

        builder.HasOne(x => x.PPurchaseOrderApproval)
            .WithMany(x => x.Committees)
            .HasForeignKey(x => x.PurchaseOrderApprovalId)
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
