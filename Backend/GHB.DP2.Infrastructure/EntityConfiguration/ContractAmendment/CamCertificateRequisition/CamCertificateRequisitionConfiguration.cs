namespace GHB.DP2.Infrastructure.EntityConfiguration.ContractAmendment.CamCertificateRequisition;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.ContractAmendment.CamCertificateRequisition;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class CamCertificateRequisitionConfiguration : EntityTypeConfigurationBase<CamCertificateRequisition, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<CamCertificateRequisition> builder)
    {
        builder.ToTable(nameof(CamCertificateRequisition), nameof(ContractAmendment));

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion();

        builder.Property(p => p.ContractDraftVendorId);

        builder.Property(p => p.CertificateNo)
               .HasVogenConversion()
               .IsRequired();

        builder.Property(p => p.ReceiveDate);

        builder.Property(p => p.SbsDocumentNo);

        builder.Property(p => p.DocumentDate);

        builder.Property(p => p.IssuedDate);

        builder.Property(p => p.RequestReason);

        builder.Property(p => p.EntrepreneurName);

        builder.Property(p => p.EntrepreneurId)
               .HasConversion(
                   (SuVendorId? v) => v!.Value.Value,
                   (Guid v) => SuVendorId.From(v));

        builder.Property(p => p.EntrepreneurEmail);

        builder.Property(p => p.ContractNumber);

        builder.Property(p => p.PoNumber);

        builder.Property(p => p.Budget);

        builder.Property(p => p.ContractName);

        builder.Property(p => p.ContractSignedDate);

        builder.Property(p => p.DeliveryDate);

        builder.Property(p => p.ContractEndDate);

        builder.Property(p => p.IsManual);

        builder.Property(p => p.SupplyMethodCode)
               .HasConversion<ParameterCode.EfCoreValueConverter, ParameterCode.EfCoreValueComparer>();

        builder.Property(p => p.SupplyMethodTypeCode)
               .HasConversion<ParameterCode.EfCoreValueConverter, ParameterCode.EfCoreValueComparer>();

        builder.Property(p => p.SupplyMethodSpecialTypeCode)
               .HasConversion<ParameterCode.EfCoreValueConverter, ParameterCode.EfCoreValueComparer>();

        builder.HasOne(p => p.SupplyMethod)
               .WithMany()
               .HasForeignKey(p => p.SupplyMethodCode)
               .HasPrincipalKey(p => p.Code)
               .IsRequired(false);

        builder.HasOne(p => p.SupplyMethodType)
               .WithMany()
               .HasForeignKey(p => p.SupplyMethodTypeCode)
               .HasPrincipalKey(p => p.Code)
               .IsRequired(false);

        builder.HasOne(p => p.SupplyMethodSpecialType)
               .WithMany()
               .HasForeignKey(p => p.SupplyMethodSpecialTypeCode)
               .HasPrincipalKey(p => p.Code)
               .IsRequired(false);

        builder.Property(p => p.Status)
               .HasConversion(new EnumToStringConverter<CamCertificateRequisitionStatus>())
               .IsRequired();

        builder.HasOne(p => p.ContractDraftVendor)
               .WithMany(p => p.CamCertificateRequisitions)
               .HasForeignKey(p => p.ContractDraftVendorId)
               .IsRequired(false)
               .OnDelete(DeleteBehavior.Restrict);

        builder.OwnDocumentHistory(p => p.DocumentHistories, documentBuilder =>
        {
            documentBuilder.ToTable(nameof(CamCertificateRequisitionDocumentHistory), nameof(ContractAmendment));

            documentBuilder.HasKey(d => d.Id);

            documentBuilder.Property(d => d.Id)
                           .HasVogenConversion();

            documentBuilder.Property(d => d.StatusState)
                           .HasConversion(new EnumToStringConverter<CamCertificateRequisitionStatus>());
        });

        builder.OwnsAuditInfo();
        builder.HasSoftDelete();
        builder.HasActivityInfo();
    }
}