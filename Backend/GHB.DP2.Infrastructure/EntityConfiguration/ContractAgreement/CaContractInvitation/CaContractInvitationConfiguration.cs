namespace GHB.DP2.Infrastructure.EntityConfiguration.ContractAgreement.CaContractInvitation;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.ContractAgreement.CaContractInvitation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class CaContractInvitationConfiguration : EntityTypeConfigurationBase<CaContractInvitation, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<CaContractInvitation> builder)
    {
        builder.ToTable(nameof(CaContractInvitation), nameof(ContractAgreement));

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id)
               .HasVogenConversion();

        builder.Property(i => i.ProcurementId)
               .IsRequired();

        builder.Property(i => i.Status)
               .HasConversion(new EnumToStringConverter<ContractInvitationStatus>())
               .IsRequired();

        builder.HasOne(i => i.Procurement)
               .WithMany(i => i.ContractInvitations)
               .HasForeignKey(i => i.ProcurementId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.OwnsAuditInfo();
        builder.HasSoftDelete();
        builder.HasActivityInfo();
    }
}