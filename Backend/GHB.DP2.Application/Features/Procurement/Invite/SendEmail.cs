namespace GHB.DP2.Application.Features.Procurement.Invite;

using Codehard.FileService.Client.Abstractions;
using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Features.Procurement.Invite.Abstract;
using GHB.DP2.Application.Features.Procurement.Invite.Templateds;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PInvite;
using GHB.DP2.Infrastructure;
using GHB.DP2.Infrastructure.Services.ChEditor;
using GHB.DP2.Infrastructure.Services.Email;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record SendEmailRequest(
    Guid ProcurementId,
    Guid InviteId,
    Guid EntrepreneursId,
    string Email,
    string EmailTemplate,
    EmailAttachment[] Attachments);

public record EmailAttachment(
    Guid? Id,
    string FileName,
    FileId FileId,
    int Sequence);

public class SendEmailEndpoint : InviteEndpointBase<SendEmailRequest, Results<Accepted, BadRequest<string>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;
    private readonly IEmailServiceFactory emailServiceFactory;
    private readonly IFileServiceClient fileService;
    private readonly IChEditorService chEditorService;

    public SendEmailEndpoint(
        Dp2DbContext dbContext,
        ILogger<SendEmailEndpoint> logger,
        IEmailServiceFactory emailService,
        IFileServiceClient fileService,
        IChEditorService chEditorService)
        : base(dbContext, logger)
    {
        this.dbContext = dbContext;
        this.emailServiceFactory = emailService;
        this.fileService = fileService;
        this.chEditorService = chEditorService;
    }

    public override void Configure()
    {
        this.Post("procurement/{ProcurementId:guid}/invite/{InviteId:guid}/entrepreneurs/{EntrepreneursId:guid}/send-email");
        this.Options(builder =>
            builder.WithTags("Procurement/Invite")
                   .WithName("SendEmailInvite")
                   .Produces<string>(StatusCodes.Status202Accepted)
                   .Produces<string>(StatusCodes.Status400BadRequest)
                   .Produces<string>(StatusCodes.Status404NotFound)
                   .Accepts<SendEmailRequest>());
    }

    protected override async ValueTask<Results<Accepted, BadRequest<string>, NotFound<string>>> HandleRequestAsync(SendEmailRequest req, CancellationToken ct)
    {
        var inviteQuery =
            this.dbContext.PInvites
                .Where(i =>
                    i.ProcurementId == ProcurementId.From(req.ProcurementId) &&
                    i.Id == PInviteId.From(req.InviteId))
                .AsQueryable();

        var invite =
            await inviteQuery
                  .Include(i => i.Procurement)
                  .SingleOrDefaultAsync(ct);

        if (invite is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลหนังสือเชิญชวน");
        }

        if (invite.Status is not PInviteStatus.Approved)
        {
            return TypedResults.BadRequest($"ไม่สามารถส่งอีเมลได้ เนื่องจากสถานะหนังสือเชิญชวนไม่ถูกต้อง");
        }

        var entrepreneurs =
            await inviteQuery
                  .Include(i => i.InvitedEntrepreneurs)
                  .ThenInclude(e => e.Vendor)
                  .Include(i => i.InvitedEntrepreneurs)
                  .ThenInclude(i => i.Attachments)
                  .SelectMany(i => i.InvitedEntrepreneurs)
                  .SingleOrDefaultAsync(
                      i =>
                          i.Id == PInvitedEntrepreneursId.From(req.EntrepreneursId),
                      ct);

        if (entrepreneurs is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลผู้ประกอบการที่ได้รับเชิญ");
        }

        entrepreneurs.SetSendMailInfo(req.Email, req.EmailTemplate);

        var existingAttachments = entrepreneurs.Attachments.ToList();
        var requestIds = req.Attachments.Where(a => a.Id.HasValue).Select(a => a.Id!.Value).ToHashSet();

        foreach (var existingAttachment in existingAttachments)
        {
            if (!requestIds.Contains(existingAttachment.Id.Value))
            {
                entrepreneurs.RemoveAttachment(existingAttachment);
            }
        }

        if (req.Attachments.Any())
        {
            var newAttachments = req.Attachments
                                    .Where(a => !a.Id.HasValue)
                                    .Select(a =>
                                        PInvitedEntrepreneursAttachments.Create(
                                            a.FileId,
                                            a.FileName,
                                            a.Sequence));

            foreach (var attachment in newAttachments)
            {
                entrepreneurs.AddAttachment(attachment);
            }
        }

        if (string.IsNullOrWhiteSpace(entrepreneurs.Email) && string.IsNullOrWhiteSpace(entrepreneurs.Vendor.Email))
        {
            return TypedResults.BadRequest($"ไม่พบข้อมูลอีเมลผู้รับหนังสือเชิญชวน");
        }

        var vendor = entrepreneurs.Vendor;
        var procurement = invite.Procurement;
        var purchaseRequisition =
            await this.dbContext.PpPurchaseRequisitions
                      .Include(ppPurchaseRequisition => ppPurchaseRequisition.TechnicalSpecifications)
                      .Include(ppPurchaseRequisition => ppPurchaseRequisition.TorDraft)
                      .ThenInclude(ppTorDraft => ppTorDraft!.PpTorDraftQualifications)
                      .FirstOrDefaultAsync(
                          p =>
                              p.ProcurementId == procurement.Id,
                          ct);

        if (purchaseRequisition is null)
        {
            return TypedResults.BadRequest($"ไม่พบข้อมูลใบขอซื้อ/ขอจ้าง");
        }

        var template = new InviteEntrepreneursTemplated
        {
            EmailTemplate = entrepreneurs.EmailTemplate ?? "-",
        }.TransformText();

        var emailSetUp =
            this.emailServiceFactory.Create()
                .To(entrepreneurs.Email ?? vendor.Email, vendor.EstablishmentName)
                .Subject($"หนังสือเชิญชวนจัดซื้อจัดจ้าง {procurement.Name}")
                .Html(template);

        // Note: Invite PDF is now attached from frontend so user can see it before sending
        // Backend only attaches files from req.Attachments
        if (req.Attachments.Any())
        {
            foreach (var attachment in req.Attachments)
            {
                var attachmentStream =
                    await this.fileService
                              .DownloadAsStreamAsync(
                                  attachment.FileId,
                                  cancellationToken: ct);

                if (attachmentStream is not null)
                {
                    using var memoryStream = new MemoryStream();
                    await attachmentStream.Stream.CopyToAsync(memoryStream, ct);
                    var fileBytes = memoryStream.ToArray();

                    var contentType = this.GetContentType(attachment.FileName);
                    emailSetUp.Attach(attachment.FileName, fileBytes, contentType);
                }
            }
        }

        await emailSetUp.SendAsync(ct);

        entrepreneurs.MarkEmailAsSent();

        this.dbContext.PInvitedEntrepreneurs.Update(entrepreneurs);

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Accepted(string.Empty);
    }

    private async Task<byte[]> ConvertToPdf(FileId fileId, CancellationToken cancellationToken = default)
    {
        var fileResult =
            await this.fileService
                      .DownloadAsStreamAsync(
                          fileId,
                          cancellationToken: cancellationToken);

        if (fileResult is null)
        {
            this.ThrowError(
                "ไม่สามารถดาวน์โหลดไฟล์เอกสารได้",
                StatusCodes.Status400BadRequest);
        }

        await using var pdfStream =
            await this.chEditorService
                      .ConvertToPdf(fileResult.Stream, cancellationToken);

        using var memoryStream = new MemoryStream();
        await pdfStream.CopyToAsync(memoryStream, cancellationToken);

        return memoryStream.ToArray();
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