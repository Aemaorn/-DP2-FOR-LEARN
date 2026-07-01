namespace GHB.DP2.Infrastructure.EntityConfiguration.ContractAmendment.CamCertificateRequisition;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.ContractAmendment.CamCertificateRequisition;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class CamCertificateRequisitionAcceptorConfiguration : EntityTypeConfigurationBase<CamCertificateRequisitionAcceptor, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<CamCertificateRequisitionAcceptor> builder)
    {
        builder.ToTable(nameof(CamCertificateRequisitionAcceptor), nameof(ContractAmendment));

        builder.Property(c => c.IsUnableToPerformDuties)
               .IsRequired();

        builder.Property(c => c.CertificateRequisitionId)
               .HasVogenConversion()
               .IsRequired();

        builder.HasOne(m => m.CommitteePosition)
               .WithMany()
               .HasForeignKey(m => m.CommitteePositionsCode)
               .HasPrincipalKey(m => m.Code)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.CertificateRequisition)
               .WithMany(c => c.Acceptors)
               .HasForeignKey(c => c.CertificateRequisitionId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.AcceptorInfo();
        builder.HasSoftDelete();
        builder.OwnsAuditInfo();
    }
}