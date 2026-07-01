namespace GHB.DP2.Infrastructure.EntityConfiguration.SystemUtility;

using System.Text.Json;
using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class SuNotificationConfiguration : EntityTypeConfigurationBase<SuNotification, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<SuNotification> builder)
    {
        builder.ToTable(nameof(SuNotification), nameof(SystemUtility));

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Id)
               .HasVogenConversion();

        builder.Property(n => n.UserId)
               .HasVogenConversion();

        builder.Property(n => n.Title)
               .IsRequired()
               .HasMaxLength(200);

        builder.Property(n => n.Message)
               .IsRequired();

        builder.Property(n => n.AdditionalData)
               .HasConversion(
                   v => JsonSerializer.Serialize(
                       v, JsonSerializerOptions.Default),
                   v => JsonSerializer.Deserialize<IDictionary<string, string>>(
                       v, JsonSerializerOptions.Default) ?? new Dictionary<string, string>())
               .HasColumnType("jsonb");

        builder.Property(n => n.CreatedAt)
               .IsRequired();

        builder.Property(n => n.ReadAt);

        builder.Ignore(n => n.Program);
        builder.Ignore(n => n.ReferenceId);
        builder.Ignore(n => n.LinkUrl);
        builder.Ignore(n => n.LinkButtonText);
    }
}