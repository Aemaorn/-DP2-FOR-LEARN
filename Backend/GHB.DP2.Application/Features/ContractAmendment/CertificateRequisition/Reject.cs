namespace GHB.DP2.Application.Features.ContractAmendment.CertificateRequisition;

using System;
using System.IdentityModel.Tokens.Jwt;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using GHB.DP2.Application.Features.ContractAmendment.CertificateRequisition.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAmendment.CamCertificateRequisition;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record RejectCertificateRequisitionRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid Id,
    AcceptorType Group,
    string? Remark
);

public class RejectCertificateRequisitionValidator : Validator<RejectCertificateRequisitionRequest>
{
    public RejectCertificateRequisitionValidator()
    {
        this.RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("ต้องระบุผู้ใช้งาน");

        this.RuleFor(x => x.Id)
            .NotEmpty().WithMessage("ต้องระบุรหัสการขอใบรับรอง ");

        this.RuleFor(x => x.Group)
            .IsInEnum()
            .WithMessage("กลุ่มผู้อนุมัติไม่ถูกต้อง")
            .Must(x => x == AcceptorType.AcceptanceCommittee)
            .WithMessage("กลุ่มผู้อนุมัติต้องเป็น บุคคล/คณะกรรมการตรวจรับพัสดุ เท่านั้น");
    }
}

public class RejectCertificateRequisitionEndpoint :
    CertificateRequisitionEndpointBase<
        RejectCertificateRequisitionRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public RejectCertificateRequisitionEndpoint(
        Dp2DbContext dbContext,
        ILogger<RejectCertificateRequisitionEndpoint> logger)
        : base(dbContext, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("ContractAmendment/CertificateRequisition")
             .WithName("RejectCertificateRequisition")
             .Accepts<RejectCertificateRequisitionRequest>("application/json"));
        this.Put("certificate-requisition/{Id:guid}/reject");
    }

    protected override async ValueTask
        <Results<Ok, NotFound<string>, BadRequest<string>>>
        HandleRequestAsync(
            RejectCertificateRequisitionRequest req,
            CancellationToken ct)
    {
        var certificateRequisitionExisting =
            await this.GetById(
                CamCertificateRequisitionId.From(req.Id),
                ct);

        if (certificateRequisitionExisting == null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลการขอใบรับรอง");
        }

        var allowReject = certificateRequisitionExisting.Status == CamCertificateRequisitionStatus.WaitingForCommitteeApproval;

        if (!allowReject)
        {
            TypedResults.BadRequest("ไม่สามารถปฏิเสธเอกสารที่อยู่ในสถานะนี้ได้");
        }

        var committeeAcceptor =
            certificateRequisitionExisting
                .Acceptors
                .Where(a => a is
                {
                    Type: AcceptorType.AcceptanceCommittee,
                    IsActive: true,
                    IsUnableToPerformDuties: false,
                    Status: AcceptorStatus.Pending
                })
                .ToArray();

        var acceptor =
            committeeAcceptor.FirstOrDefault(a => a.UserId == UserId.From(req.UserId));

        if (acceptor is null)
        {
            this.ThrowError(
                "ไม่พบบุคคล/คณะกรรมการตรวจรับพัสดุที่ใช้งานได้",
                StatusCodes.Status400BadRequest);
        }

        acceptor.Reject(req.Remark);

        if (certificateRequisitionExisting.HasMajorityRejection() || acceptor.IsBoardChairman())
        {
            certificateRequisitionExisting.SetRejected();
            this.RevertDocumentTemplateSectionAsync(certificateRequisitionExisting);
        }

        certificateRequisitionExisting.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.CommitteeReject,
                ActivityLogActionTypeConstant.CommitteeReject,
                certificateRequisitionExisting.Status.ToString(),
                req.Remark));

        this.dbContext.CamCertificateRequisitions.Update(certificateRequisitionExisting);

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }
}