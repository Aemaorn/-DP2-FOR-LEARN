namespace GHB.DP2.Application.Features.ContractAgreement.ContractInvitation;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.ContractAgreement.ContractInvitation.Abstract;
using GHB.DP2.Application.Features.ContractAgreement.ContractInvitation.Templateds;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractInvitation;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using GHB.DP2.Infrastructure.Services.ChEditor;
using GHB.DP2.Infrastructure.Services.Email;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class ApproveContractInvitationRequest
{
    public Guid ProcurementId { get; init; }

    public Guid Id { get; init; }

    [FromClaim(JwtRegisteredClaimNames.Sub)]
    public Guid UserId { get; init; }

    public string? Remark { get; init; }
}

public class ApproveContractInvitationEndpoint
    : ContractInvitationEndpointBase<ApproveContractInvitationRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;
    private readonly IFileServiceClient fileService;
    private readonly IEmailServiceFactory emailService;
    private readonly IChEditorService chEditorService;

    public ApproveContractInvitationEndpoint(
        Dp2DbContext dbContext,
        IOperationService operationService,
        IFileServiceClient fileServiceClient,
        IEmailServiceFactory emailService,
        IChEditorService chEditorService,
        ILogger<UpsertAttachmentsEndpoint> logger)
        : base(dbContext, operationService, fileServiceClient, logger)
    {
        this.dbContext = dbContext;
        this.fileService = fileServiceClient;
        this.emailService = emailService;
        this.chEditorService = chEditorService;
    }

    public override void Configure()
    {
        this.Put("procurement/{ProcurementId:guid}/contractInvitation/{Id:guid}/approve");
        this.Options(b =>
            b.WithTags("ContractAgreement/ContractInvitation")
             .WithName("ApproveContractInvitation")
             .Produces<Ok>()
             .ProducesProblem(StatusCodes.Status400BadRequest)
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status500InternalServerError));
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(
        ApproveContractInvitationRequest req,
        CancellationToken ct)
    {
        var contractInvitationExisting = await this.ValidateRequestAsync(req, ct);

        this.ApproverApprove(
            contractInvitationExisting,
            req);

        UpdateSequentialCurrents(contractInvitationExisting, AcceptorType.Approver);

        contractInvitationExisting.EvaluateAcceptorApproval();

        if (contractInvitationExisting.Status == ContractInvitationStatus.Approved)
        {
            foreach (var vendor in contractInvitationExisting.Vendors)
            {
                await this.UpdateDocumentAsync(vendor, true, true, ContractInvitationStatus.WaitingApproval, ct);
            }

            var programName = contractInvitationExisting.Procurement.Type == ProcurementType.Rent
                ? ProgramConstant.BranchSpaceRent.Name
                : ProgramConstant.ContractInvitation.Name;

            _ = SendNotificationAsync(
                contractInvitationExisting,
                UserId.From(contractInvitationExisting.AuditInfo.CreatedBy),
                NotificationConstant.InformCommittee.Title,
                string.Format(
                    NotificationConstant.InformCommittee.Message,
                    programName,
                    contractInvitationExisting.Procurement.ProcurementNumber));
        }

        await this.dbContext.SaveChangesAsync(ct);

        if (contractInvitationExisting.Status == ContractInvitationStatus.Approved)
        {
            var assigneeEmails = await this.dbContext.PPurchaseOrderApprovals
                                           .Where(w => w.ProcurementId == contractInvitationExisting.ProcurementId)
                                           .SelectMany(s => s.Assignees)
                                           .Where(a => a.Type == AssigneeType.Assignee && !a.IsDeleted)
                                           .Include(a => a.User)
                                           .ThenInclude(u => u.Employee)
                                           .Select(a => a.User.Employee.Email)
                                           .Where(email => !string.IsNullOrWhiteSpace(email))
                                           .Distinct()
                                           .ToListAsync(ct);

            _ = this.SendEmailToAllVendorsAsync(contractInvitationExisting, assigneeEmails, CancellationToken.None);
        }

        return TypedResults.Ok();
    }

    private async Task<CaContractInvitation> ValidateRequestAsync(
        ApproveContractInvitationRequest req,
        CancellationToken ct)
    {
        var contractInvitationExisting =
            await this.GetById(
                ContractInvitationId.From(req.Id),
                ProcurementId.From(req.ProcurementId),
                ct);

        var canApprove =
            contractInvitationExisting.Status is ContractInvitationStatus.WaitingApproval;

        if (!canApprove)
        {
            this.ThrowError(
                r =>
                    req.Id,
                $"หนังสือเชิญชวนทำสัญญาที่ระบุไม่อยู่ในสถานะที่สามารถอนุมัติได้ (สถานะปัจจุบัน: {contractInvitationExisting.Status})",
                StatusCodes.Status404NotFound);
        }

        return contractInvitationExisting;
    }

    private void ApproverApprove(
        CaContractInvitation contractInvitationExisting,
        ApproveContractInvitationRequest req)
    {
        var acceptorsPendingExisting =
            contractInvitationExisting.Acceptors
                                      .Where(a =>
                                          a is
                                          {
                                              Type: AcceptorType.Approver,
                                              Status: AcceptorStatus.Pending,
                                              IsActive: true
                                          })
                                      .Map(DelegatorExtensions.DelegatorToAcceptor)
                                      .OrderBy(a => a.Sequence)
                                      .ToList();

        var acceptorExisting =
            acceptorsPendingExisting.FirstOrDefault(a => a.Delegatee?.SuUserId == null
                ? a.UserId == UserId.From(req.UserId)
                : a.Delegatee?.SuUserId == UserId.From(req.UserId));

        if (acceptorExisting is null)
        {
            this.ThrowError(
                "ไม่พบผู้อนุมัติที่ใช้งานได้",
                StatusCodes.Status400BadRequest);
        }

        if (!acceptorExisting.ArePreviousAcceptorsApproved(contractInvitationExisting.Acceptors))
        {
            this.ThrowError(
                "ต้องได้รับการอนุมัติจากลำดับก่อนหน้าก่อน",
                StatusCodes.Status400BadRequest);
        }

        var currentAcceptorUser =
            contractInvitationExisting.Acceptors
                                      .FirstOrDefault(a => a.Id == acceptorExisting.Id);

        if (currentAcceptorUser is null)
        {
            this.ThrowError(
                "ไม่พบผู้อนุมัติที่ใช้งานได้",
                StatusCodes.Status400BadRequest);
        }

        currentAcceptorUser
            .SetDelegatee(acceptorExisting.DelegateeId)
            .Approve(remark: req.Remark);

        currentAcceptorUser.ContractInvitation.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.Approved,
                $"ผู้เห็นชอบ/อนุมัติ \"เห็นชอบ/อนุมัติ\" หนังสือเชิญชวนทำสัญญา",
                nameof(ContractInvitationStatus.WaitingApproval),
                req.Remark));
    }

    private async Task SendEmailToAllVendorsAsync(
        CaContractInvitation contractInvitation,
        List<string> assigneeEmails,
        CancellationToken ct)
    {
        foreach (var vendor in contractInvitation.Vendors)
        {
            try
            {
                var recipientEmail = vendor.EmailSend ?? vendor.Email;

                if (string.IsNullOrWhiteSpace(recipientEmail))
                {
                    continue;
                }

                var entrepreneur = vendor.PurchaseOrderApprovalContract.Entrepreneur;
                var suVendor = entrepreneur?.SuVendor;

                var establishmentName = suVendor?.Type switch
                {
                    SuVendorType.Consortium => $"กรรมการผู้จัดการ/ผู้จัดการ {suVendor.EstablishmentName}",
                    SuVendorType.Individual => $"คุณ {suVendor.EstablishmentName}",
                    _ => suVendor?.EstablishmentName ?? string.Empty,
                };

                var emailTemplateContent = vendor.EmailTemplate
                                           ??
                                           $"<p>{establishmentName}</p><p></p><p>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;ด้วย&nbsp;ธนาคารอาคารสงเคราะห์&nbsp;มีความประสงค์จะขอเรียนเชิญท่าน&nbsp;ให้มาจัดทำสัญญา&nbsp;ดังรายละเอียดตามเอกสารแบบฉบับนี้</p><p></p><p>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;ทั้งนี้&nbsp;ขอให้ท่านจัดส่งเอกสารมาที่&nbsp;ส่วนบริหารสัญญา&nbsp;ฝ่ายจัดหาและการพัสดุ&nbsp;อาคาร&nbsp;2&nbsp;ชั้น&nbsp;4&nbsp;ธนาคารอาคารสงเคราะห์&nbsp;สำนักงานใหญ่&nbsp;เลขที่&nbsp;63&nbsp;ถนนพระราม&nbsp;9&nbsp;แขวงห้วยขวาง&nbsp;กรุงเทพมหานคร&nbsp;10310&nbsp;ภายใน&nbsp;7&nbsp;วัน&nbsp;นับตั้งแต่วันที่ได้รับหนังสือฉบับนี้&nbsp;หลังจากนั้นธนาคารจะแจ้งนัดหมายมาลงนามสัญญาต่อไป</p>";

                vendor.SetSendMailInfo(recipientEmail, emailTemplateContent);

                var template = new InvitedContractTemplated()
                {
                    EmailTemplate = emailTemplateContent,
                }.TransformText();

                var emailSetup = this.emailService.Create()
                                     .To(recipientEmail, establishmentName)
                                     .Subject($"หนังสือเชิญให้ทำสัญญา {vendor.ContractName} เลขที่ {vendor.ContractNumber} ")
                                     .Html(template);

                foreach (var ccEmail in assigneeEmails)
                {
                    emailSetup.Cc(ccEmail);
                }

                var lastedDocument = vendor.LastedDocument;

                if (lastedDocument is not null)
                {
                    try
                    {
                        var fileResult = await this.fileService.DownloadAsStreamAsync(
                            lastedDocument.FileId,
                            cancellationToken: ct);

                        if (fileResult is not null)
                        {
                            using (fileResult.Stream)
                            {
                                await using var pdfStream = await this.chEditorService.ConvertToPdf(fileResult.Stream, ct);
                                using var memoryStream = new MemoryStream();
                                await pdfStream.CopyToAsync(memoryStream, ct);
                                emailSetup.Attach("หนังสือเชิญชวนทำสัญญา.pdf", memoryStream.ToArray(), "application/pdf");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        this.Logger.LogError(ex, "Failed to attach PDF for vendor {VendorId}, sending email without attachment", vendor.Id);
                    }
                }

                await emailSetup.SendAsync(ct);
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "Failed to send email to vendor {VendorId}", vendor.Id);
            }
        }
    }

    private static void UpdateSequentialCurrents(CaContractInvitation contractInvitation, AcceptorType type)
    {
        var approvers = contractInvitation.Acceptors
                                          .Where(a => a.Type == type && a.IsActive)
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

        var next = approvers.Select(DelegatorExtensions.DelegatorToAcceptor).FirstOrDefault(a =>
            a.Status == AcceptorStatus.Pending);

        if (next is null)
        {
            return;
        }

        var pendingOfType = approvers.Where(a => a.Status == AcceptorStatus.Pending).ToList();
        var isLastPending = pendingOfType.Count == 1;

        next.SetCurrent(true);

        var programName = contractInvitation.Procurement.Type == ProcurementType.Rent
            ? ProgramConstant.BranchSpaceRent.Name
            : ProgramConstant.ContractInvitation.Name;

        if (next.Type == AcceptorType.Approver && !isLastPending)
        {
            foreach (var targetUserId in next.GetNotificationTargets())
            {
                _ = SendNotificationAsync(
                    contractInvitation,
                    targetUserId,
                    NotificationConstant.WaitForLike.Title,
                    string.Format(NotificationConstant.WaitForLike.Message, programName, contractInvitation.Procurement.ProcurementNumber));
            }
        }
        else if (next.Type == AcceptorType.Approver && isLastPending)
        {
            foreach (var targetUserId in next.GetNotificationTargets())
            {
                _ = SendNotificationAsync(
                    contractInvitation,
                    targetUserId,
                    NotificationConstant.WaitForApprove.Title,
                    string.Format(NotificationConstant.WaitForApprove.Message, programName, contractInvitation.Procurement.ProcurementNumber));
            }
        }
    }

    private static async Task SendNotificationAsync(CaContractInvitation contractInvitation, UserId userId, string title, string message)
    {
        var notificationProgram = NotificationProgram.ContractAgreement;

        var programUrl = contractInvitation.Procurement.Type == ProcurementType.Rent
            ? ProgramConstant.BranchSpaceRent.Url
            : ProgramConstant.Procurement.Url;

        await Notification
              .Crate(
                  userId,
                  title,
                  message,
                  notificationProgram)
              .SetReferenceId(contractInvitation.Id.Value)
              .SetLinkUrl(string.Format(programUrl, contractInvitation.Procurement.Id), "ดูรายละเอียด")
              .PublishAsync(CancellationToken.None);
    }
}