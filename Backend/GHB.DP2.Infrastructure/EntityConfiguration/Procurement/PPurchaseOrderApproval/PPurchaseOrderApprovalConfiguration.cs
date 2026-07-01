namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PPurchaseOrderApproval;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PPurchaseOrderApproval;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PPurchaseOrderApprovalConfiguration : EntityTypeConfigurationBase<Domain.Procurement.PPurchaseOrderApproval.PPurchaseOrderApproval, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<Domain.Procurement.PPurchaseOrderApproval.PPurchaseOrderApproval> builder)
    {
        builder.ToTable(nameof(PPurchaseOrderApproval), nameof(Procurement));

        builder.HasKey(poa => poa.Id);

        builder.Property(poa => poa.Id)
               .HasVogenConversion();

        builder.Property(poa => poa.ProcurementId)
               .IsRequired();

        builder.Property(poa => poa.ContractType)
               .HasConversion<string>();

        builder.Property(poa => poa.Status)
               .HasConversion<string>();

        builder.Property(poa => poa.DocumentDate);

        builder.HasOne(poa => poa.Procurement)
               .WithMany(p => p.PurchaseOrderApprovals)
               .HasForeignKey(x => x.ProcurementId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasActivityInfo();
        builder.OwnsAuditInfo();
        builder.HasSoftDelete();
    }
}