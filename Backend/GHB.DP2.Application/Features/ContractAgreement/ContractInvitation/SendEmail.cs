namespace GHB.DP2.Application.Features.ContractAgreement.ContractInvitation;

using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Features.ContractAgreement.ContractInvitation.Abstract;
using GHB.DP2.Application.Features.ContractAgreement.ContractInvitation.Templateds;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.Invite;
using GHB.DP2.Domain.ContractAgreement.CaContractInvitation;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using GHB.DP2.Infrastructure.Services.Email;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record SendEmailRequest(
    Guid ProcurementId,
    Guid ContractInvitationId,
    Guid VendorsId,
    string Email,
    string EmailTemplate,
    EmailAttachment[] EmailAttachments);

public class SendEmailEndpoint : ContractInvitationEndpointBase<SendEmailRequest, Results<Accepted, BadRequest<string>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;
    private readonly IFileServiceClient fileService;
    private readonly IEmailServiceFactory emailService;

    public SendEmailEndpoint(
        Dp2DbContext dbContext,
        ILogger<SendEmailEndpoint> logger,
        IFileServiceClient fileService,
        IOperationService operationService,
        IEmailServiceFactory emailService)
        : base(dbContext, operationService, fileService, logger)
    {
        this.dbContext = dbContext;
        this.fileService = fileService;
        this.emailService = emailService;
    }

    public override void Configure()
    {
        this.Post("procurement/{ProcurementId:guid}/contract-invitation/{ContractInvitationId:guid}/vendor/{VendorsId:guid}/send-email");
        this.Options(builder =>
            builder.WithTags("ContractAgreement/ContractDraft")
                   .WithName("SendEmailContractDraft")
                   .Produces<string>(StatusCodes.Status202Accepted)
                   .Produces<string>(StatusCodes.Status400BadRequest)
                   .Produces<string>(StatusCodes.Status404NotFound)
                   .Accepts<SendEmailRequest>());
    }

    protected override async ValueTask<Results<Accepted, BadRequest<string>, NotFound<string>>> HandleRequestAsync(SendEmailRequest req, CancellationToken ct)
    {
        var contractInvitationVendor =
            await this.dbContext.CaContractInvitations
                      .Include(ci => ci.Procurement)
                      .ThenInclude(p => p.SupplyMethodType)
                      .Include(ci => ci.Procurement)
                      .ThenInclude(p => p.SupplyMethodSpecialType)
                      .Where(ci =>
                          ci.ProcurementId == ProcurementId.From(req.ProcurementId) &&
                          ci.Id == ContractInvitationId.From(req.ContractInvitationId))
                      .SelectMany(ci => ci.Vendors)
                      .Include(caContractInvitationVendors => caContractInvitationVendors.EmailAttachments)
                      .Include(caContractInvitationVendors => caContractInvitationVendors.PurchaseOrderApprovalContract)
                      .ThenInclude(pPurchaseOrderApprovalContract => pPurchaseOrderApprovalContract.Entrepreneur)
                      .ThenInclude(pPurchaseOrderEntrepreneur => pPurchaseOrderEntrepreneur!.SuVendor)
                      .FirstOrDefaultAsync(
                          v =>
                              v.Id == ContractInvitationVendorsId.From(req.VendorsId),
                          ct);

        if (contractInvitationVendor is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลผู้ขายในคำเชิญสัญญา");
        }

        if (contractInvitationVendor.ContractInvitation.Status is not ContractInvitationStatus.Approved)
        {
            return TypedResults.BadRequest("สถานะคำเชิญสัญญาไม่ถูกต้อง");
        }

        if (string.IsNullOrWhiteSpace(contractInvitationVendor.Email))
        {
            return TypedResults.BadRequest("ไม่พบอีเมลผู้ขาย");
        }

        var entrepreneur =
            contractInvitationVendor.PurchaseOrderApprovalContract.Entrepreneur;

        if (entrepreneur is null)
        {
            return TypedResults.BadRequest("ไม่พบข้อมูลผู้ขาย");
        }

        contractInvitationVendor.SetSendMailInfo(req.Email, req.EmailTemplate);

        var existingAttachments = contractInvitationVendor.EmailAttachments.ToList();
        var requestIds = req.EmailAttachments.Where(a => a.Id.HasValue).Select(a => a.Id!.Value).ToHashSet();

        foreach (var existingAttachment in existingAttachments)
        {
            if (!requestIds.Contains(existingAttachment.Id.Value))
            {
                contractInvitationVendor.RemoveAttachment(existingAttachment);
            }
        }

        if (req.EmailAttachments.Any())
        {
            var newAttachments = req.EmailAttachments
                                 .Where(a => !a.Id.HasValue)
                                 .Select(a =>
                                     CaContractInvitationVendorEmailAttachments.Create(
                                         a.FileId,
                                         a.FileName,
                                         a.Sequence));

            foreach (var attachment in newAttachments)
            {
                contractInvitationVendor.AddAttachment(attachment);
            }
        }

        var vendor = entrepreneur.SuVendor;

        var establishmentName =
            vendor.Type switch
            {
                SuVendorType.Consortium => $"กรรมการผู้จัดการ/ผู้จัดการ {vendor.EstablishmentName}",
                SuVendorType.Individual => $"คุณ {vendor.EstablishmentName}",
                _ => vendor.EstablishmentName,
            };

        var template = new InvitedContractTemplated()
        {
            EmailTemplate = contractInvitationVendor.EmailTemplate ?? string.Empty,
        }.TransformText();

        var emailSetup =
            this.emailService.Create()
                .To(contractInvitationVendor.EmailSend ?? contractInvitationVendor.Email, establishmentName)
                .Subject($"หนังสือเชิญให้ทำสัญญา {contractInvitationVendor.ContractName} เลขที่ {contractInvitationVendor.ContractNumber} ")
                .Html(template);

        // Note: Contract invitation PDF is now attached by frontend via EmailAttachments
        // to allow user to see and manage the attachment before sending
        if (req.EmailAttachments.Any())
        {
            foreach (var attachment in req.EmailAttachments)
            {
                var attachmentStream =
                    await this.fileService
                              .DownloadAsStreamAsync(
                                  attachment.FileId,
                                  cancellationToken: ct);

                if (attachmentStream is not null)
                {
                    using (attachmentStream)
                    {
                        using var memoryStream = new MemoryStream();
                        await attachmentStream.Stream.CopyToAsync(memoryStream, ct);
                        var fileBytes = memoryStream.ToArray();

                        var contentType = this.GetContentType(attachment.FileName);
                        emailSetup.Attach(attachment.FileName, fileBytes, contentType);
                    }
                }
            }
        }

        await emailSetup.SendAsync(ct);

        this.dbContext.CaContractInvitationVendors.Update(contractInvitationVendor);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Accepted(string.Empty);
    }

    private string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        return extension switch
        {
            ".pdf" => "application/pdf",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".txt" => "text/plain",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".ppt" => "application/vnd.ms-powerpoint",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            _ => "application/octet-stream",
        };
    }
}