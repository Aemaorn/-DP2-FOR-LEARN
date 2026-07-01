namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PJp005;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PJp005;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class PJp005CommitteeDutiesConfiguration : EntityTypeConfigurationBase<PJp005CommitteeDuties, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PJp005CommitteeDuties> builder)
    {
        builder.ToTable(
            nameof(PJp005CommitteeDuties),
            nameof(Procurement));

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion()
               .ValueGeneratedNever();

        builder.Property(p => p.GroupType)
               .HasConversion(new EnumToStringConverter<PJp005CommitteeGroupType>())
               .IsRequired();

        builder.Property(p => p.Description)
               .IsRequired();

        builder.Property(p => p.Sequence)
               .IsRequired();

        builder.OwnsAuditInfo();
    }
}