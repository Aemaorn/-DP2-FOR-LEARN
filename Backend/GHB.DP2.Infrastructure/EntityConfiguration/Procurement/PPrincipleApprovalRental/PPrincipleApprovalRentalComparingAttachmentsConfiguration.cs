namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PPrincipleApprovalRental;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PPrincipleApprovalRental;
using Microsoft.EntityFrameworkCore;

public class PPrincipleApprovalRentalComparingAttachmentsConfiguration : EntityTypeConfigurationBase<PPrincipleApprovalRentalComparingAttachments, Dp2DbContext>
{
    protected override void EntityConfigure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<PPrincipleApprovalRentalComparingAttachments> builder)
    {
        builder.ToTable(nameof(PPrincipleApprovalRentalComparingAttachments), nameof(Domain.Procurement.Procurement));

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
               .ValueGeneratedNever();

        builder.Property(a => a.FileName)
               .IsRequired();

        builder.Property(a => a.IsPublic)
               .IsRequired();

        builder.Property(a => a.Sequence)
               .HasDefaultValue(1)
               .IsRequired();

        builder.HasOne(p => p.PPrincipleApprovalRental)
               .WithMany(p => p.ComparingAttachments);

        builder.OwnsAuditInfo();
    }
}