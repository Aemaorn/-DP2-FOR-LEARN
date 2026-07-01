namespace GHB.DP2.Application.Features.ContractAmendment.CertificateRequisition;

using GHB.DP2.Application.Features.ContractAmendment.CertificateRequisition.Abstract;
using GHB.DP2.Domain.ContractAmendment.CamCertificateRequisition;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record RecallCertificateRequisitionRequest(
    Guid Id);

public class RecallCertificateRequisitionRequestEndpoint : CertificateRequisitionEndpointBase<RecallCertificateRequisitionRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public RecallCertificateRequisitionRequestEndpoint(Dp2DbContext dbContext, ILogger<RecallCertificateRequisitionRequestEndpoint> logger)
        : base(dbContext, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("ContractAmendment/CertificateRequisition")
             .WithName("RecallCertificateRequisitionRequest")
             .Accepts<RecallCertificateRequisitionRequest>());
        this.Put("certificate-requisition/{Id:guid}/recall");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(RecallCertificateRequisitionRequest req, CancellationToken ct)
    {
        var certificateRequisitionExisting =
            await this.GetById(
                CamCertificateRequisitionId.From(req.Id),
                ct);

        if (certificateRequisitionExisting == null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลการขอใบรับรอง");
        }

        if (certificateRequisitionExisting.Status is not CamCertificateRequisitionStatus.WaitingForCommitteeApproval)
        {
            return TypedResults.BadRequest("ไม่สามารถเรียกคืนการขอใบรับรองได้");
        }

        certificateRequisitionExisting.UpdateStatus(CamCertificateRequisitionStatus.Edit);
        this.RevertDocumentTemplateSectionAsync(certificateRequisitionExisting);

        this.dbContext.CamCertificateRequisitions.Update(certificateRequisitionExisting);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }
}