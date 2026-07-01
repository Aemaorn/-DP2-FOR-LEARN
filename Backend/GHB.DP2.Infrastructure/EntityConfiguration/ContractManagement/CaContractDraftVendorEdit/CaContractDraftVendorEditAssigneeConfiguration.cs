namespace GHB.DP2.Infrastructure.EntityConfiguration.ContractManagement.CaContractDraftVendorEdit;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.ContractManagement.CaContractDraftVendorEdit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class CaContractDraftVendorEditAssigneeConfiguration : EntityTypeConfigurationBase<CaContractDraftVendorEditAssignee, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<CaContractDraftVendorEditAssignee> builder)
    {
        builder.ToTable(nameof(CaContractDraftVendorEditAssignee), nameof(ContractManagement));

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
               .HasVogenConversion();

        builder.AssigneeInfo();
        builder.OwnsAuditInfo();
        builder.HasSoftDelete();
    }
}
