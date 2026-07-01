namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PPurchaseOrderApproval;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PPurchaseOrderApproval;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PPurchaseOrderApprovalContractConfiguration : EntityTypeConfigurationBase<PPurchaseOrderApprovalContract, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PPurchaseOrderApprovalContract> builder)
    {
        builder.ToTable(nameof(PPurchaseOrderApprovalContract), nameof(Procurement));

        builder.HasKey(poa => poa.Id);

        builder.Property(poa => poa.Id)
               .HasVogenConversion();

        builder.Property(poa => poa.Sequence)
               .IsRequired();

        builder.Property(poa => poa.PurchaseOrderEntrepreneurId);

        builder.Property(poa => poa.PrincipleApprovalRentalEntrepreneursId);

        builder.Property(poa => poa.PPurchaseOrderApprovalBudgetId);

        builder.Property(poa => poa.PPurchaseOrderApprovalEntrepreneursId);

        builder.Property(poa => poa.ContractNumber)
               .HasMaxLength(100);

        builder.Property(poa => poa.AgreedPrice)
               .IsRequired();

        builder.Property(poa => poa.PoNumber)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(poa => poa.HasEditContractNumber)
               .IsRequired();

        builder.Property(poa => poa.CommitteeType)
               .IsRequired();

        builder.HasOne(poa => poa.Entrepreneur)
               .WithMany()
               .HasForeignKey(poa => poa.PurchaseOrderEntrepreneurId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(poa => poa.PrincipleApprovalRentalEntrepreneurs)
               .WithMany()
               .HasForeignKey(poa => poa.PrincipleApprovalRentalEntrepreneursId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(poa => poa.PPurchaseOrderApprovalEntrepreneurs)
               .WithMany()
               .HasForeignKey(poa => poa.PPurchaseOrderApprovalEntrepreneursId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(poa => poa.Approval)
               .WithMany(poa => poa.Contracts)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(poa => poa.Budget)
               .WithMany()
               .HasForeignKey(poa => poa.TorDraftBudgetId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(poa => poa.PpPurchaseRequisitionBudget)
               .WithMany()
               .HasForeignKey(poa => poa.PpPurchaseRequisitionBudgetId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(poa => poa.PrincipleApprovalRentalBudget)
               .WithMany()
               .HasForeignKey(poa => poa.PrincipleApprovalRentalBudgetId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(poa => poa.PPurchaseOrderApprovalBudget)
                .WithMany()
                .HasForeignKey(poa => poa.PPurchaseOrderApprovalBudgetId)
                .OnDelete(DeleteBehavior.Cascade);

        builder.OwnsAuditInfo();
        builder.HasSoftDelete();
    }
}