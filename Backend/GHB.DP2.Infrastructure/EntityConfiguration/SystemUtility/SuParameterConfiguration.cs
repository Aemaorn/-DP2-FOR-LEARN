namespace GHB.DP2.Infrastructure.EntityConfiguration.SystemUtility;

using System.Text.Json;
using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class SuParameterConfiguration : EntityTypeConfigurationBase<SuParameter, Dp2DbContext>
{
    private static readonly JsonSerializerOptions ValuesJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static Dictionary<string, ParameterValue> DeserializeValues(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, ParameterValue>();
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, ParameterValue>>(json, ValuesJsonOptions)
                   ?? new Dictionary<string, ParameterValue>();
        }
        catch (JsonException)
        {
            return new Dictionary<string, ParameterValue>();
        }
    }

    protected override void EntityConfigure(EntityTypeBuilder<SuParameter> builder)
    {
        builder.ToTable(nameof(SuParameter), nameof(SystemUtility));

        builder.HasKey(p => p.Id);

        builder.Property(u => u.Id)
               .HasVogenConversion();

        builder.Property(u => u.ParentId)
               .HasConversion<ParameterId.EfCoreValueConverter, ParameterId.EfCoreValueComparer>();

        builder.HasOne(p => p.Parent)
               .WithMany(p => p.Children)
               .HasForeignKey(p => p.ParentId)
               .IsRequired(false);

        builder.HasOne(p => p.Group)
               .WithMany(g => g.Parameters)
               .HasForeignKey(p => p.GroupCode)
               .HasPrincipalKey(g => g.Code)
               .IsRequired();

        builder.Property(u => u.Code)
               .HasVogenConversion()
               .HasMaxLength(50)
               .IsRequired();

        builder.Property(u => u.Label)
               .HasMaxLength(1000)
               .IsRequired();

        builder.Property(u => u.Sequence)
               .IsRequired();

        builder.Property(u => u.Values)
               .HasConversion(
                   v => JsonSerializer.Serialize(v, ValuesJsonOptions),
                   v => DeserializeValues(v))
               .HasColumnType("jsonb")
               .IsRequired();

        builder.Property(u => u.IsActive)
               .IsRequired();

        builder.OwnsAuditInfo();
        builder.HasSoftDelete();
    }
}