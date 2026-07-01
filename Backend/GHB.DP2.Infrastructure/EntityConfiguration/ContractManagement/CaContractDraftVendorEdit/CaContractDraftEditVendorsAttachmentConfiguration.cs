namespace GHB.DP2.Infrastructure.EntityConfiguration.ContractManagement.CaContractDraftVendorEdit;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.ContractManagement.CaContractDraftVendorEdit;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class CaContractDraftEditVendorsAttachmentConfiguration : EntityTypeConfigurationBase<CaContractDraftEditVendorsAttachment, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<CaContractDraftEditVendorsAttachment> builder)
    {
        builder.ToTable(nameof(CaContractDraftEditVendorsAttachment), nameof(ContractManagement));

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
               .HasVogenConversion();

        builder.Property(c => c.TypeCode)
               .HasConversion<ParameterCode.EfCoreValueConverter, ParameterCode.EfCoreValueComparer>()
               .IsRequired();

        builder.Property(c => c.PageNumber);

        builder.Property(c => c.Description);

        builder.Property(c => c.FormatOtherName);

        builder.Property(c => c.Sequence)
               .IsRequired();

        builder.OwnsMany(c => c.Files, filesBuilder =>
        {
            filesBuilder.ToTable(nameof(CaContractDraftEditVendorsAttachmentFile), nameof(ContractManagement));

            filesBuilder.HasKey(f => f.Id);

            filesBuilder.Property(f => f.Id)
                        .ValueGeneratedNever();

            filesBuilder.Property(f => f.FileName)
                        .IsRequired()
                        .HasMaxLength(255);

            filesBuilder.Property(f => f.FileType)
                        .IsRequired()
                        .HasMaxLength(100);

            filesBuilder.Property(f => f.Sequence)
                        .IsRequired();
        });

        builder.HasOne(c => c.Type)
               .WithMany()
               .HasForeignKey(c => c.TypeCode)
               .HasPrincipalKey(p => p.Code);

        builder.OwnsAuditInfo();
        builder.HasSoftDelete();
    }
}
