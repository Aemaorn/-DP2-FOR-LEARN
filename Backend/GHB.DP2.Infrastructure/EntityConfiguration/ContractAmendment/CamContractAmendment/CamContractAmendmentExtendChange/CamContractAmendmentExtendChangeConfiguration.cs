namespace GHB.DP2.Infrastructure.EntityConfiguration.ContractAmendment.CamContractAmendment.CamContractAmendmentExtendChange;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment.CamContractAmendmentExtendChange;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class CamContractAmendmentExtendChangeConfiguration : EntityTypeConfigurationBase<Domain.ContractAmendment.ContractAmendment.CamContractAmendmentExtendChange.CamContractAmendmentExtendChange, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<Domain.ContractAmendment.ContractAmendment.CamContractAmendmentExtendChange.CamContractAmendmentExtendChange> builder)
    {
        builder.ToTable(nameof(Domain.ContractAmendment.ContractAmendment.CamContractAmendmentExtendChange.CamContractAmendmentExtendChange), nameof(ContractAmendment));

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
               .HasVogenConversion();

        builder.Property(e => e.CamContractAmendmentId)
               .IsRequired();

        builder.Property(e => e.ChangeType)
               .HasConversion(new EnumToStringConverter<ContractAmendmentExtendChangeType>())
               .IsRequired();

        builder.Property(e => e.PaymentTypeCode)
               .HasConversion<ParameterCode.EfCoreValueConverter, ParameterCode.EfCoreValueComparer>();

        builder.Property(e => e.WorkStartDate)
               .IsRequired();

        builder.Property(e => e.NewEndDate)
               .IsRequired();

        builder.Property(e => e.Status)
               .HasConversion(new EnumToStringConverter<ContractAmendmentExtendChangeStatus>())
               .IsRequired();

        builder.HasOne(e => e.CamContractAmendment)
               .WithOne(e => e.ExtendChange)
               .HasForeignKey<Domain.ContractAmendment.ContractAmendment.CamContractAmendmentExtendChange.CamContractAmendmentExtendChange>(e => e.CamContractAmendmentId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.PaymentType)
               .WithMany()
               .HasForeignKey(e => e.PaymentTypeCode)
               .HasPrincipalKey(e => e.Code)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Acceptors)
               .WithOne(a => a.CamContractAmendmentExtendChange)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Assignees)
               .WithOne(a => a.CamContractAmendmentExtendChange)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.PaymentTerms)
               .WithOne(p => p.CamContractAmendmentExtendChange)
               .OnDelete(DeleteBehavior.Cascade);

        builder.OwnDocumentHistory(e => e.DocumentHistories, documentBuilder =>
        {
            documentBuilder.ToTable(nameof(CamContractAmendmentExtendChangeDocumentHistory), nameof(ContractAmendment));

            documentBuilder.HasKey(d => d.Id);

            documentBuilder.Property(d => d.Id)
                           .HasVogenConversion();

            documentBuilder.Property(d => d.DocumentType)
                           .HasConversion(new EnumToStringConverter<ExtendChangeAcceptorDocumentType>())
                           .IsRequired();

            documentBuilder.Property(d => d.StatusState)
                           .HasConversion(new EnumToStringConverter<ContractAmendmentExtendChangeStatus>())
                           .IsRequired();
        });

        builder.OwnsAuditInfo();
        builder.HasActivityInfo();
    }
}