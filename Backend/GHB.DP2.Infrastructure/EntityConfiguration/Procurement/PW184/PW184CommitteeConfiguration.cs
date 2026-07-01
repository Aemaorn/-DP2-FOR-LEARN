namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.Pw184;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.Pw184;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class Pw184CommitteeConfiguration : EntityTypeConfigurationBase<Pw184Committee, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<Pw184Committee> builder)
    {
        builder.ToTable(nameof(Pw184Committee), nameof(Domain.Procurement.Procurement));

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion()
               .ValueGeneratedNever();

        builder.Property(p => p.Pw184Id)
               .HasConversion<Pw184Id.EfCoreValueConverter, Pw184Id.EfCoreValueComparer>()
               .IsRequired();

        builder.Property(p => p.SuUserId)
               .HasConversion<UserId.EfCoreValueConverter, UserId.EfCoreValueComparer>()
               .IsRequired();

        builder.HasOne(p => p.User)
               .WithMany()
               .HasForeignKey(p => p.SuUserId)
               .HasPrincipalKey(p => p.Id);

        builder.Property(p => p.GroupType)
               .HasConversion(new EnumToStringConverter<Pw184CommitteeGroupType>())
               .IsRequired();

        builder.Property(p => p.FullName)
               .IsRequired();

        builder.Property(p => p.FullPositionName)
               .IsRequired();

        builder.Property(p => p.CommitteePositionsCode)
               .HasConversion<ParameterCode.EfCoreValueConverter, ParameterCode.EfCoreValueComparer>()
               .IsRequired();

        builder.HasOne(p => p.CommitteePositions)
               .WithMany()
               .HasForeignKey(p => p.CommitteePositionsCode)
               .HasPrincipalKey(p => p.Code);

        builder.Property(p => p.CommitteePositionsName)
               .IsRequired();

        builder.Property(p => p.Sequence)
               .IsRequired();

        builder.OwnsAuditInfo();
    }
}
