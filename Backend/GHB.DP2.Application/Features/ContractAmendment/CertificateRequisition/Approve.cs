namespace GHB.DP2.Application.Features.ContractAmendment.CertificateRequisition;

using System.IdentityModel.Tokens.Jwt;
using FluentValidation;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.ContractAmendment.CertificateRequisition.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAmendment.CamCertificateRequisition;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record ApproveCertificateRequisitionRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid Id,
    AcceptorType Group,
    string? Remark);

public class ApproveCertificateRequisitionValidator : Validator<ApproveCertificateRequisitionRequest>
{
    public ApproveCertificateRequisitionValidator()
    {
        this.RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("ต้องระบุผู้ใช้งาน");

        this.RuleFor(x => x.Id)
            .NotEmpty().WithMessage("ต้องระบุรหัสการขอใบรับรอง");

        this.RuleFor(x => x.Group)
            .IsInEnum()
            .WithMessage("กลุ่มผู้อนุมัติไม่ถูกต้อง")
            .Must(x => x == AcceptorType.AcceptanceCommittee)
            .WithMessage("กลุ่มผู้อนุมัติต้องเป็น บุคคล/คณะกรรมการตรวจรับพัสดุ เท่านั้น");
    }
}

public class ApproveCertificateRequisitionEndpoint
    : CertificateRequisitionEndpointBase<
        ApproveCertificateRequisitionRequest,
        Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public ApproveCertificateRequisitionEndpoint(
        Dp2DbContext dbContext,
        ILogger<ApproveCertificateRequisitionEndpoint> logger)
        : base(dbContext, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("ContractAmendment/CertificateRequisition")
             .WithName("ApproveCertificateRequisition")
             .Accepts<ApproveCertificateRequisitionRequest>("application/json"));
        this.Put("certificate-requisition/{Id:guid}/approve");
    }

    protected override async ValueTask<
        Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(
        ApproveCertificateRequisitionRequest req,
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

        var allowApproveStatus =
            certificateRequisitionExisting.Status == CamCertificateRequisitionStatus.WaitingForCommitteeApproval;

        if (!allowApproveStatus)
        {
            return TypedResults.BadRequest("ไม่สามารถอนุมัติในสถานะนี้ได้");
        }

        var acceptors =
            certificateRequisitionExisting
                .Acceptors
                .Where(a =>
                    a.Type == req.Group &&
                    !a.IsUnableToPerformDuties &&
                    a.IsActive)
                .OrderBy(a => a.Sequence)
                .ToList();

        var currentAcceptor =
            acceptors.FirstOrDefault(a => a.UserId == req.UserId);

        if (currentAcceptor == null)
        {
            return TypedResults.BadRequest("ไม่พบผู้อนุมัติในกลุ่มหรือขั้นตอนนี้");
        }

        currentAcceptor.Approve(req.Remark);
        currentAcceptor.SetCurrent(false);

        UpdateSequentialCurrents(certificateRequisitionExisting, req.Group);

        var allAcceptorsInGroupApproved =
            acceptors
                .Where(a =>
                    a.Type == req.Group &&
                    !a.IsUnableToPerformDuties)
                .All(a => a.Status == AcceptorStatus.Approved);

        if (allAcceptorsInGroupApproved || currentAcceptor.IsBoardChairman())
        {
            certificateRequisitionExisting.UpdateStatus(CamCertificateRequisitionStatus.Approved);
        }

        certificateRequisitionExisting.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.CommitteeApproved,
                ActivityLogActionTypeConstant.CommitteeApproved,
                certificateRequisitionExisting.Status.ToString(),
                req.Remark));

        await this.UpdateDocumentTemplateAsync(certificateRequisitionExisting, ct);
        this.dbContext.CamCertificateRequisitions.Update(certificateRequisitionExisting);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private static void UpdateSequentialCurrents(CamCertificateRequisition certificateRequisition, AcceptorType type)
    {
        var approvers = certificateRequisition.Acceptors
                                              .Where(a => a.Type == type && a.IsActive && !a.IsUnableToPerformDuties)
                                              .OrderBy(a => a.Sequence)
                                              .ToList();

        if (approvers.Count == 0)
        {
            return;
        }

        foreach (var a in approvers)
        {
            a.SetCurrent(false);
        }

        var next = approvers.FirstOrDefault(a =>
            a.Status == AcceptorStatus.Pending);

        if (next is null)
        {
            return;
        }

        next.SetCurrent(true);

        var pendingOfType = approvers.Where(a => a.Status == AcceptorStatus.Pending).ToList();
        var isLastPending = pendingOfType.Count == 1;

        if (next.Type == AcceptorType.DepartmentDirectorAgree ||
            (next.Type == AcceptorType.Approver && !isLastPending))
        {
            foreach (var targetUserId in next.GetNotificationTargets())
            {
                _ = SendNotificationAsync(
                    certificateRequisition,
                    targetUserId,
                    NotificationConstant.WaitForLike.Title,
                    string.Format(NotificationConstant.WaitForLike.Message, ProgramConstant.CertificatesRequisition.Name, certificateRequisition.ContractDraftVendor?.ContractNumber ?? certificateRequisition.ContractNumber));
            }
        }
        else if (next.Type == AcceptorType.Approver && isLastPending)
        {
            foreach (var targetUserId in next.GetNotificationTargets())
            {
                _ = SendNotificationAsync(
                    certificateRequisition,
                    targetUserId,
                    NotificationConstant.WaitForApprove.Title,
                    string.Format(NotificationConstant.WaitForApprove.Message, ProgramConstant.CertificatesRequisition.Name, certificateRequisition.ContractDraftVendor?.ContractNumber ?? certificateRequisition.ContractNumber));
            }
        }
    }

    private static async Task SendNotificationAsync(CamCertificateRequisition certificateRequisition, UserId userId, string title, string message)
    {
        await Notification
              .Crate(
                  userId,
                  title,
                  message,
                  NotificationProgram.Procurement)
              .SetReferenceId(certificateRequisition.Id.Value)
              .SetLinkUrl(string.Format(ProgramConstant.CertificatesRequisition.Url, certificateRequisition.Id), "ดูรายละเอียด")
              .PublishAsync(CancellationToken.None);
    }
}