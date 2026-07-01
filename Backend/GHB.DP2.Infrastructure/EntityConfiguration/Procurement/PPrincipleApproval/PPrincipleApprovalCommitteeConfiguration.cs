namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PPrincipleApproval;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PPrincipleApproval;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PPrincipleApprovalCommitteeConfiguration : EntityTypeConfigurationBase<PPrincipleApprovalCommittee, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PPrincipleApprovalCommittee> builder)
    {
        builder.ToTable(nameof(PPrincipleApprovalCommittee), nameof(Procurement));

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion()
               .ValueGeneratedNever();

        builder.Property(p => p.SuUserId)
               .HasVogenConversion()
               .IsRequired();

        builder.Property(p => p.FullName)
               .IsRequired();

        builder.Property(p => p.FullPositionName)
               .IsRequired();

        builder.Property(p => p.CommitteePositionsCode)
               .HasConversion<ParameterCode.EfCoreValueConverter, ParameterCode.EfCoreValueComparer>()
               .IsRequired();

        builder.Property(p => p.CommitteePositionsName)
               .IsRequired();

        builder.Property(p => p.Sequence)
               .IsRequired();

        builder.HasSoftDelete();
        builder.OwnsAuditInfo();
    }
}