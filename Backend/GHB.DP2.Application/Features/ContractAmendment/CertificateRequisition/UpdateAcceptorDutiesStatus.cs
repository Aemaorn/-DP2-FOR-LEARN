namespace GHB.DP2.Application.Features.ContractAmendment.CertificateRequisition;

using FluentValidation;
using GHB.DP2.Application.Features.ContractAmendment.CertificateRequisition.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAmendment.CamCertificateRequisition;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record UpdatePeriodAcceptorDutiesStatusRequest(
    Guid Id,
    Guid AcceptorId,
    bool IsUnableToPerformDuties,
    string? Remark);

public class UpdateAcceptorDutiesStatusValidator : Validator<UpdatePeriodAcceptorDutiesStatusRequest>
{
    public UpdateAcceptorDutiesStatusValidator()
    {
        this.RuleFor(x => x.Id)
            .NotNull()
            .NotEmpty()
            .WithMessage("ต้องระบุรหัสการขอใบรับรอง");

        this.RuleFor(x => x.AcceptorId)
            .NotNull()
            .NotEmpty()
            .WithMessage("กรุณาระบุบุคคล/คณะกรรมการตรวจรับพัสดุ");

        this.RuleFor(x => x.IsUnableToPerformDuties)
            .NotNull()
            .WithMessage("กรุณาระบุสถานะการไม่สามารถปฏิบัติหน้าที่ได้");
    }
}

public class UpdatePeriodAcceptorDutiesStatusEndpoint :
    CertificateRequisitionEndpointBase<
        UpdatePeriodAcceptorDutiesStatusRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public UpdatePeriodAcceptorDutiesStatusEndpoint(
        ILogger<UpdatePeriodAcceptorDutiesStatusEndpoint> logger,
        Dp2DbContext dbContext)
        : base(dbContext, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("ContractAmendment/CertificateRequisition")
             .WithName("CertificateRequisitionUpdateAcceptorDutiesStatus")
             .Produces<Ok>()
             .Produces<NotFound>()
             .Accepts<UpdatePeriodAcceptorDutiesStatusRequest>("application/json"));
        this.Put("certificate-requisition/{Id:guid}/acceptor/{AcceptorId:guid}/duties-status");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>>
        HandleRequestAsync(
            UpdatePeriodAcceptorDutiesStatusRequest req,
            CancellationToken ct)
    {
        var certificateRequisitionExisting =
            await this.GetById(
                CamCertificateRequisitionId.From(req.Id),
                ct);

        if (certificateRequisitionExisting is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลการขอใบรับรอง");
        }

        var acceptor =
            certificateRequisitionExisting.Acceptors
                                          .FirstOrDefault(a =>
                                              a.Id == req.AcceptorId &&
                                              a.Type == AcceptorType.AcceptanceCommittee);

        if (acceptor is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลบุคคล/คณะกรรมการตรวจรับพัสดุ");
        }

        acceptor.SetIsUnableToPerformDuties(req.IsUnableToPerformDuties);

        _ = acceptor.IsUnableToPerformDuties switch
        {
            true => acceptor.UnableToPerformDuties(req.Remark),
            false => acceptor.Pending(),
        };

        if (certificateRequisitionExisting.HasMajorityRejection())
        {
            certificateRequisitionExisting.SetRejected();
        }

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }
}