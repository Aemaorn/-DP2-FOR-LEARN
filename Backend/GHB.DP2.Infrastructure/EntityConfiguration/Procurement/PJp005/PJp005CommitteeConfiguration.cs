namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PJp005;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PJp005;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class PJp005CommitteeConfiguration
       : EntityTypeConfigurationBase<PJp005Committee, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PJp005Committee> builder)
    {
        builder.ToTable(
               nameof(PJp005Committee),
               nameof(Procurement));

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion()
               .ValueGeneratedNever();

        builder.Property(p => p.GroupType)
               .HasConversion(new EnumToStringConverter<PJp005CommitteeGroupType>())
               .IsRequired();

        builder.Property(p => p.SuUserId)
               .HasVogenConversion()
               .IsRequired();

        builder.Property(p => p.FullName)
               .IsRequired();

        builder.Property(p => p.FullPositionName)
               .IsRequired();

        builder.HasOne(m => m.CommitteePositions)
               .WithMany()
               .HasForeignKey(m => m.CommitteePositionsCode)
               .HasPrincipalKey(m => m.Code)
               .OnDelete(DeleteBehavior.Restrict);

        builder.Property(p => p.CommitteePositionsName)
               .IsRequired();

        builder.Property(p => p.Sequence)
               .IsRequired();

        builder.OwnsAuditInfo();
    }
}