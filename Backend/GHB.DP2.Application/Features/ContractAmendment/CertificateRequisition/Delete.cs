namespace GHB.DP2.Application.Features.ContractAmendment.CertificateRequisition;

using GHB.DP2.Application.Features.ContractAmendment.CertificateRequisition.Abstract;
using GHB.DP2.Domain.ContractAmendment.CamCertificateRequisition;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class DeleteById
{
    public record DeleteCertificateRequisitionByIdRequest(
        Guid Id);

    public class DeleteCamCertificateRequisitionById
        : CertificateRequisitionEndpointBase<
            DeleteCertificateRequisitionByIdRequest,
            Results<NoContent, NotFound<string>>>
    {
        private readonly Dp2DbContext dbContext;

        public DeleteCamCertificateRequisitionById(
            Dp2DbContext dbContext,
            ILogger<DeleteCertificateRequisitionByIdRequest> logger)
            : base(dbContext, logger)
        {
            this.dbContext = dbContext;
        }

        public override void Configure()
        {
            this.Delete("certificate-requisition/{Id:guid}");
            this.Description(
                b => b
                     .WithTags("ContractAmendment/CertificateRequisition")
                     .Produces<string>(StatusCodes.Status204NoContent)
                     .Produces<string>(StatusCodes.Status404NotFound));
        }

        protected override async ValueTask<Results<
                NoContent,
                NotFound<string>>>
            HandleRequestAsync(DeleteCertificateRequisitionByIdRequest req, CancellationToken ct)
        {
            var certReqExisting =
                await this.dbContext.CamCertificateRequisitions
                          .FirstOrDefaultAsync(
                              cr => cr.Id == CamCertificateRequisitionId.From(req.Id),
                              ct);

            if (certReqExisting is null)
            {
                this.ThrowError(
                    r => req.Id,
                    $"ไม่พบข้อมูลการขอใบรับรอง {req.Id}",
                    StatusCodes.Status404NotFound);
            }

            var canDelete =
                certReqExisting.Status == CamCertificateRequisitionStatus.Draft ||
                certReqExisting.Status == CamCertificateRequisitionStatus.Edit ||
                certReqExisting.Status == CamCertificateRequisitionStatus.Rejected;

            if (!canDelete)
            {
                this.ThrowError(
                    r => req.Id,
                    $"ไม่สามารถลบการขอใบรับรองที่มีสถานะ {certReqExisting.Status} ได้",
                    StatusCodes.Status409Conflict);
            }

            certReqExisting.SetDeleted();

            await this.dbContext.SaveChangesAsync(ct);

            return TypedResults.NoContent();
        }
    }
}