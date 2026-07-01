namespace GHB.DP2.Infrastructure.EntityConfiguration.AnnouncementInfo;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.AnnouncementInfo;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class AnnouncementInfoConfiguration : EntityTypeConfigurationBase<AnnouncementInfo, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<AnnouncementInfo> builder)
    {
        builder.ToTable(nameof(AnnouncementInfo), nameof(AnnouncementInfo));

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
               .HasVogenConversion();

        builder.Property(a => a.OldId);

        builder.Property(a => a.AnnouncementName);

        builder.Property(a => a.AnnouncementTitle);

        builder.Property(a => a.Email)
               .HasMaxLength(500);

        builder.Property(a => a.Status)
               .HasConversion(new EnumToStringConverter<AnnouncementInfoStatus>())
               .IsRequired();

        builder.Property(a => a.AnnouncementDate);

        builder.Property(a => a.SupplyMethodCode)
               .HasConversion<ParameterCode.EfCoreValueConverter, ParameterCode.EfCoreValueComparer>();

        builder.Property(a => a.BudgetAmount);

        builder.Property(a => a.AnnouncementCategoryCode)
               .HasConversion<ParameterCode.EfCoreValueConverter, ParameterCode.EfCoreValueComparer>();

        builder.Property(a => a.Description);

        builder.Property(a => a.DocumentId);

        builder.Property(a => a.DocumentName);

        builder.Property(a => a.DocumentUrl);

        builder.Property(a => a.ExpectedDate);

        builder.Property(a => a.StartDate);

        builder.Property(a => a.EndDate);

        builder.Property(a => a.ReferencePrice);

        builder.Property(a => a.BudgetYear);

        builder.Property(a => a.Remark);

        builder.Property(a => a.IsDp);

        builder.Property(a => a.IsActive);

        builder.HasOne(a => a.SupplyMethod)
               .WithMany()
               .HasForeignKey(a => a.SupplyMethodCode)
               .HasPrincipalKey(p => p.Code)
               .OnDelete(DeleteBehavior.Restrict)
               .IsRequired(false);

        builder.HasOne(a => a.AnnouncementCategory)
               .WithMany()
               .HasForeignKey(a => a.AnnouncementCategoryCode)
               .HasPrincipalKey(p => p.Code)
               .OnDelete(DeleteBehavior.Restrict)
               .IsRequired(false);

        builder.HasActivityInfo();
        builder.HasSoftDelete();
        builder.OwnsAuditInfo();
    }
}
