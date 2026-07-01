namespace GHB.DP2.Application.Features.ContractManagement.ContractGuaranteeReturn;

using Codehard.FileService.Client.Abstractions;
using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Features.ContractManagement.ContractGuaranteeReturn.Abstract;
using GHB.DP2.Application.Features.ContractManagement.ContractGuaranteeReturn.Templateds;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractManagement.CmContractGuaranteeReturn;
using GHB.DP2.Infrastructure;
using GHB.DP2.Infrastructure.Services.Email;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record SendGuaranteeReturnEmailRequest(
    Guid ContractVendorId,
    Guid Id,
    string Email,
    string EmailTemplate,
    SendGuaranteeReturnEmailAttachment[] EmailAttachments);

public record SendGuaranteeReturnEmailAttachment(
    Guid? Id,
    string FileName,
    FileId FileId,
    int Sequence);

public class SendGuaranteeReturnEmailEndpoint
    : ContractGuaranteeReturnEndpoint<SendGuaranteeReturnEmailRequest, Results<Accepted, BadRequest<string>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;
    private readonly IFileServiceClient fileService;
    private readonly IEmailServiceFactory emailService;

    public SendGuaranteeReturnEmailEndpoint(
        Dp2DbContext dbContext,
        ILogger<SendGuaranteeReturnEmailEndpoint> logger,
        IFileServiceClient fileService,
        IOperationService operationService,
        IEmailServiceFactory emailService)
        : base(logger, dbContext, fileService, operationService)
    {
        this.dbContext = dbContext;
        this.fileService = fileService;
        this.emailService = emailService;
    }

    public override void Configure()
    {
        this.Post("contract/{ContractVendorId:guid}/contract-guarantee-return/{Id:guid}/send-email");
        this.Options(builder =>
            builder.WithTags("ContractManagement/ContractGuaranteeReturn")
                   .WithName("SendGuaranteeReturnEmail")
                   .Produces<string>(StatusCodes.Status202Accepted)
                   .Produces<string>(StatusCodes.Status400BadRequest)
                   .Produces<string>(StatusCodes.Status404NotFound)
                   .Accepts<SendGuaranteeReturnEmailRequest>());
    }

    protected override async ValueTask<Results<Accepted, BadRequest<string>, NotFound<string>>> HandleRequestAsync(
        SendGuaranteeReturnEmailRequest req, CancellationToken ct)
    {
        var entity = await this.GetByIdAsync(ContractDraftVendorId.From(req.ContractVendorId), ct);

        var guarantee = entity.CmContractGuaranteeReturns
                              .SingleOrDefault(t => t.Id.Value == req.Id);

        if (guarantee is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลคืนหลักประกันสัญญา {req.Id}");
        }

        if (string.IsNullOrWhiteSpace(req.Email))
        {
            return TypedResults.BadRequest("กรุณาระบุอีเมล");
        }

        guarantee.SetSendMailInfo(req.Email, req.EmailTemplate);

        var existingAttachments = guarantee.EmailAttachments.ToList();
        var requestIds = req.EmailAttachments
                            .Where(a => a.Id.HasValue)
                            .Select(a => a.Id!.Value)
                            .ToHashSet();

        foreach (var existingAttachment in existingAttachments)
        {
            if (!requestIds.Contains(existingAttachment.Id.Value))
            {
                guarantee.RemoveAttachment(existingAttachment);
            }
        }

        if (req.EmailAttachments.Any())
        {
            var newAttachments = req.EmailAttachments
                                 .Where(a => !a.Id.HasValue)
                                 .Select(a =>
                                     CmContractGuaranteeReturnEmailAttachments.Create(
                                         a.FileId,
                                         a.FileName,
                                         a.Sequence));

            foreach (var attachment in newAttachments)
            {
                guarantee.AddAttachment(attachment);
            }
        }

        var template = new GuaranteeReturnEmailTemplated()
        {
            EmailTemplate = guarantee.EmailTemplate ?? string.Empty,
        }.TransformText();

        var emailSetup =
            this.emailService.Create()
                .To(req.Email)
                .Subject($"แจ้งคืนหลักประกันสัญญา {entity.ContractName} เลขที่ {entity.ContractNumber}")
                .Html(template);

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
