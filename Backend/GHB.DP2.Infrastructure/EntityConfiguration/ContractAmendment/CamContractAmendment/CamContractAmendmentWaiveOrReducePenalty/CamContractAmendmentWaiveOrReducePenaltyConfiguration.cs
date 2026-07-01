namespace GHB.DP2.Infrastructure.EntityConfiguration.ContractAmendment.CamContractAmendment.CamContractAmendmentWaiveOrReducePenalty;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment.CamContractAmendmentWaiveOrReducePenalty;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class CamContractAmendmentWaiveOrReducePenaltyConfiguration : EntityTypeConfigurationBase<
    Domain.ContractAmendment.ContractAmendment.CamContractAmendmentWaiveOrReducePenalty.CamContractAmendmentWaiveOrReducePenalty, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<Domain.ContractAmendment.ContractAmendment.CamContractAmendmentWaiveOrReducePenalty.CamContractAmendmentWaiveOrReducePenalty> builder)
    {
        builder.ToTable(nameof(Domain.ContractAmendment.ContractAmendment.CamContractAmendmentWaiveOrReducePenalty.CamContractAmendmentWaiveOrReducePenalty), nameof(ContractAmendment));

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
               .HasVogenConversion();

        builder.Property(e => e.CamContractAmendmentId)
               .IsRequired();

        builder.Property(e => e.PenaltyTypeCode)
               .HasConversion<ParameterCode.EfCoreValueConverter, ParameterCode.EfCoreValueComparer>();

        builder.Property(e => e.Rate);

        builder.Property(e => e.Amount);

        builder.Property(e => e.RateTypeCode)
               .HasConversion<ParameterCode.EfCoreValueConverter, ParameterCode.EfCoreValueComparer>();

        builder.Property(e => e.Status)
               .HasConversion(new EnumToStringConverter<CamContractAmendmentWaiveOrReducePenaltyStatus>())
               .IsRequired();

        builder.HasMany(e => e.Acceptors)
               .WithOne(a => a.CamContractAmendmentWaiveOrReducePenalty)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Assignees)
               .WithOne(a => a.CamContractAmendmentWaiveOrReducePenalty)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.CamContractAmendment)
               .WithOne(e => e.WaiveOrReducePenalty)
               .HasForeignKey<Domain.ContractAmendment.ContractAmendment.CamContractAmendmentWaiveOrReducePenalty.CamContractAmendmentWaiveOrReducePenalty>(e => e.CamContractAmendmentId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.OwnDocumentHistory(e => e.DocumentHistories, documentBuilder =>
        {
            documentBuilder.ToTable(nameof(CamContractAmendmentWaiveOrReducePenaltyDocumentHistory), nameof(ContractAmendment));

            documentBuilder.HasKey(d => d.Id);

            documentBuilder.Property(d => d.Id)
                           .HasVogenConversion();

            documentBuilder.Property(d => d.DocumentType)
                           .HasConversion(new EnumToStringConverter<WaiveOrReducePenaltyDocumentType>())
                           .IsRequired();

            documentBuilder.Property(d => d.StatusState)
                           .HasConversion(new EnumToStringConverter<CamContractAmendmentWaiveOrReducePenaltyStatus>())
                           .IsRequired();
        });

        builder.OwnsAuditInfo();
        builder.HasActivityInfo();
    }
}