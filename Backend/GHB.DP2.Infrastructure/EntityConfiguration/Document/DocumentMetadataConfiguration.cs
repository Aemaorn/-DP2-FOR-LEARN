namespace GHB.DP2.Infrastructure.EntityConfiguration.Document;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Common;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class DocumentMetadataConfiguration : EntityTypeConfigurationBase<DocumentMetadata, Dp2DbContext>
{
    /// <summary>Configures the entity of type TEntity.</summary>
    /// <param name="builder"></param>
    protected override void EntityConfigure(EntityTypeBuilder<DocumentMetadata> builder)
    {
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id)
               .ValueGeneratedNever();

        builder.Property(d => d.State)
               .IsRequired();

        builder.OwnsAuditInfo();
    }
}