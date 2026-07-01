namespace GHB.DP2.Application.Features.ContractManagement.DeliveryAcceptance;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client.Abstractions;
using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Features.ContractManagement.DeliveryAcceptance.Abstract;
using GHB.DP2.Application.Features.ContractManagement.DeliveryAcceptance.Templateds;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractManagement.CmDeliveryAcceptance;
using GHB.DP2.Infrastructure;
using GHB.DP2.Infrastructure.Services.Email;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record SendWarrantyPeriodEmailRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid Id,
    string Email,
    string EmailTemplate,
    SendWarrantyPeriodEmailAttachment[] EmailAttachments);

public record SendWarrantyPeriodEmailAttachment(
    string FileName,
    FileId FileId,
    int Sequence);

public class SendWarrantyPeriodEmailEndpoint
    : DeliveryAcceptanceEndpointBase<SendWarrantyPeriodEmailRequest, Results<Accepted, BadRequest<string>, NotFound<string>>>
{
    private readonly IFileServiceClient fileService;
    private readonly IEmailServiceFactory emailService;

    public SendWarrantyPeriodEmailEndpoint(
        Dp2DbContext dbContext,
        ILogger<SendWarrantyPeriodEmailEndpoint> logger,
        IFileServiceClient fileService,
        IEmailServiceFactory emailService)
        : base(dbContext, logger)
    {
        this.fileService = fileService;
        this.emailService = emailService;
    }

    public override void Configure()
    {
        this.Post("delivery-acceptance/{Id:guid}/send-warranty-period-email");
        this.Options(builder =>
            builder.WithTags("ContractManagement/DeliveryAcceptance")
                   .WithName("SendWarrantyPeriodEmail")
                   .Produces<string>(StatusCodes.Status202Accepted)
                   .Produces<string>(StatusCodes.Status400BadRequest)
                   .Produces<string>(StatusCodes.Status404NotFound)
                   .Accepts<SendWarrantyPeriodEmailRequest>());
    }

    protected override async ValueTask<Results<Accepted, BadRequest<string>, NotFound<string>>> HandleRequestAsync(
        SendWarrantyPeriodEmailRequest req, CancellationToken ct)
    {
        var deliveryAcceptance = await this.GetById(CmDeliveryAcceptanceId.From(req.Id), ct);

        if (string.IsNullOrWhiteSpace(req.Email))
        {
            return TypedResults.BadRequest("กรุณาระบุอีเมล");
        }

        var template = new WarrantyPeriodEmailTemplated()
        {
            EmailTemplate = req.EmailTemplate,
        }.TransformText();

        var emailSetup =
            this.emailService.Create()
                .To(req.Email)
                .Subject("แจ้งระยะเวลารับประกัน")
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

        var userName = this.HttpContext.User.FindFirst(JwtRegisteredClaimNames.Name)?.Value ?? string.Empty;

        this.dbContext.ActivityLogs.Add(new ActivityLog(
            deliveryAcceptance.Id.Value.ToString(),
            new ActivityInfo(
                ActivityLogActionTypeConstant.SendEmail,
                ActivityLogActionTypeConstant.SendEmail,
                deliveryAcceptance.Status.ToString(),
                $"ส่งอีเมลแจ้งระยะเวลารับประกันถึง {req.Email}"),
            new AuditInfo(req.UserId, DateTimeOffset.UtcNow, userName)));

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
