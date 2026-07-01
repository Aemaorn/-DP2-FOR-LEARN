namespace GHB.DP2.Infrastructure.EntityConfiguration.ContractManagement.CaContractDraftVendorEdit;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.ContractManagement.CaContractDraftVendorEdit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class CaContractDraftVendorEditComponentConfiguration : EntityTypeConfigurationBase<CaContractDraftVendorEditComponent, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<CaContractDraftVendorEditComponent> builder)
    {
        builder.ToTable(nameof(CaContractDraftVendorEditComponent), nameof(ContractManagement));

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
               .HasVogenConversion();

        builder.Property(c => c.ContractDraftVendorEditId)
               .HasVogenConversion();

        builder.Property(c => c.ComponentCode)
               .IsRequired();

        builder.Property(c => c.ComponentName)
               .IsRequired();

        builder.Property(c => c.IsEdited);

        builder.OwnsAuditInfo();
    }
}
