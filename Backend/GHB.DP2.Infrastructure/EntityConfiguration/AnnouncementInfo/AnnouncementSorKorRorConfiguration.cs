namespace GHB.DP2.Infrastructure.EntityConfiguration.AnnouncementInfo;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.AnnouncementInfo;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class AnnouncementSorKorRorConfiguration : EntityTypeConfigurationBase<AnnouncementSorKorRor, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<AnnouncementSorKorRor> builder)
    {
        builder.ToTable(nameof(AnnouncementSorKorRor), nameof(AnnouncementInfo));

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
               .HasVogenConversion();

        builder.Property(a => a.OldId);

        builder.Property(a => a.Year);

        builder.Property(a => a.Month);

        builder.Property(a => a.Amount);

        builder.Property(a => a.DepartmentTypeCode)
               .HasConversion<ParameterCode.EfCoreValueConverter, ParameterCode.EfCoreValueComparer>();

        builder.HasOne(a => a.DepartmentType)
               .WithMany()
               .HasForeignKey(a => a.DepartmentTypeCode)
               .HasPrincipalKey(p => p.Code)
               .OnDelete(DeleteBehavior.Restrict)
               .IsRequired(false);

        builder.Property(a => a.IsDp);

        builder.Property(a => a.IsActive);

        builder.Property(a => a.DocumentId);

        builder.Property(a => a.DocumentName);

        builder.Property(a => a.DocumentUrl);

        builder.HasActivityInfo();
        builder.HasSoftDelete();
        builder.OwnsAuditInfo();
    }
}
