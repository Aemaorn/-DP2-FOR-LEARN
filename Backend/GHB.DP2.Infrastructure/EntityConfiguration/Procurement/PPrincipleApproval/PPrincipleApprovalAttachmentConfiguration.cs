namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PPrincipleApproval;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PPrincipleApproval;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PPrincipleApprovalAttachmentConfiguration : EntityTypeConfigurationBase<PPrincipleApprovalAttachment>
{
    protected override void EntityConfigure(EntityTypeBuilder<PPrincipleApprovalAttachment> builder)
    {
        builder.ToTable("PPrincipleApprovalAttachments", nameof(Domain.Procurement));

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
               .HasVogenConversion()
               .ValueGeneratedNever();

        builder.Property(a => a.FileId)
               .IsRequired();

        builder.Property(a => a.FileName)
               .IsRequired();

        builder.Property(a => a.Sequence)
               .HasDefaultValue(1)
               .IsRequired();

        builder.OwnsAuditInfo();
    }
}