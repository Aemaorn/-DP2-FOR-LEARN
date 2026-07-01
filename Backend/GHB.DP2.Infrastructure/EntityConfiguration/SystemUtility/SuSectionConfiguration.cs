namespace GHB.DP2.Infrastructure.EntityConfiguration.SystemUtility;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class SuSectionConfiguration : EntityTypeConfigurationBase<SuSection, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<SuSection> builder)
    {
        builder.ToTable(nameof(SuSection), nameof(SystemUtility));

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
               .HasVogenConversion();

        builder.Property(s => s.RefBankOrder)
               .IsRequired();

        builder.Property(s => s.SupplyMethodCode)
               .HasConversion<ParameterCode.EfCoreValueConverter, ParameterCode.EfCoreValueComparer>();

        builder.Property(s => s.SupplyMethodSpecialTypeCode)
               .HasConversion<ParameterCode.EfCoreValueConverter, ParameterCode.EfCoreValueComparer>();

        builder.Property(s => s.MaximumBudget)
               .IsRequired();

        builder.Property(s => s.Remark);

        builder.HasOne(s => s.SupplyMethod)
               .WithMany()
               .HasForeignKey(s => s.SupplyMethodCode)
               .HasPrincipalKey(p => p.Code)
               .OnDelete(DeleteBehavior.Restrict)
               .IsRequired(false);

        builder.HasOne(s => s.SupplyMethodSpecialType)
               .WithMany()
               .HasForeignKey(s => s.SupplyMethodSpecialTypeCode)
               .HasPrincipalKey(p => p.Code)
               .OnDelete(DeleteBehavior.Restrict)
               .IsRequired(false);

        builder.OwnsMany(s => s.Approvers, approvers =>
        {
            approvers.ToTable(nameof(SuSectionApprover), nameof(SystemUtility));

            approvers.HasKey(a => a.Id);

            approvers.Property(a => a.Id)
                     .HasVogenConversion();

            approvers.Property(a => a.InRefCode)
                     .IsRequired();

            approvers.Property(a => a.PositionName)
                     .IsRequired();

            approvers.Property(a => a.ShortPosition)
                     .IsRequired();

            approvers.Property(a => a.Budget)
                     .IsRequired();

            approvers.Property(a => a.CommandBudget);

            approvers.Property(a => a.ProcessType)
                     .HasConversion(new EnumToStringConverter<SectionProcessType>())
                     .IsRequired();

            approvers.WithOwner(a => a.Section)
                     .HasForeignKey(s => s.SuSectionId);
        });
    }
}