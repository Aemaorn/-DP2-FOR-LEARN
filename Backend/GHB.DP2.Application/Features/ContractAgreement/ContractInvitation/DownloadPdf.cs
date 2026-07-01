namespace GHB.DP2.Application.Features.ContractAgreement.ContractInvitation;

using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Features.ContractAgreement.ContractInvitation.Abstract;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Domain.ContractAgreement.CaContractInvitation;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Infrastructure;
using GHB.DP2.Infrastructure.Services.ChEditor;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record DownloadContractInvitationPdfRequest(
    Guid ProcurementId,
    Guid ContractInvitationId,
    Guid VendorId);

public class DownloadContractInvitationPdfEndpoint : ContractInvitationEndpointBase<DownloadContractInvitationPdfRequest, Results<FileContentHttpResult, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;
    private readonly IFileServiceClient fileService;
    private readonly IChEditorService chEditorService;

    public DownloadContractInvitationPdfEndpoint(
        Dp2DbContext dbContext,
        IOperationService operationService,
        IFileServiceClient fileService,
        IChEditorService chEditorService,
        ILogger<DownloadContractInvitationPdfEndpoint> logger)
        : base(dbContext, operationService, fileService, logger)
    {
        this.dbContext = dbContext;
        this.fileService = fileService;
        this.chEditorService = chEditorService;
    }

    public override void Configure()
    {
        this.Get("procurement/{ProcurementId:guid}/contract-invitation/{ContractInvitationId:guid}/vendor/{VendorId:guid}/download-pdf");
        this.Description(b => b
            .WithTags("ContractAgreement/ContractInvitation")
            .WithName("DownloadContractInvitationPdf")
            .Produces<FileContentHttpResult>(StatusCodes.Status200OK, contentType: "application/pdf")
            .Produces<string>(StatusCodes.Status404NotFound));
    }

    protected override async ValueTask<Results<FileContentHttpResult, NotFound<string>>> HandleRequestAsync(
        DownloadContractInvitationPdfRequest req,
        CancellationToken ct)
    {
        var contractInvitationVendor =
            await this.dbContext.CaContractInvitations
                .Where(ci =>
                    ci.ProcurementId == ProcurementId.From(req.ProcurementId) &&
                    ci.Id == ContractInvitationId.From(req.ContractInvitationId))
                .SelectMany(ci => ci.Vendors)
                .Include(v => v.DocumentHistories)
                .FirstOrDefaultAsync(
                    v => v.Id == ContractInvitationVendorsId.From(req.VendorId),
                    ct);

        if (contractInvitationVendor is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลผู้ขายในคำเชิญสัญญา");
        }

        var lastedDocument = contractInvitationVendor.LastedDocument;

        if (lastedDocument is null)
        {
            return TypedResults.NotFound("ไม่พบเอกสารหนังสือเชิญชวนทำสัญญา");
        }

        var fileResult = await this.fileService.DownloadAsStreamAsync(
            lastedDocument.FileId,
            cancellationToken: ct);

        if (fileResult is null)
        {
            return TypedResults.NotFound("ไม่สามารถดาวน์โหลดไฟล์เอกสารได้");
        }

        using (fileResult)
        {
            try
            {
                await using var pdfStream = await this.chEditorService.ConvertToPdf(fileResult.Stream, ct);
                using var memoryStream = new MemoryStream();
                await pdfStream.CopyToAsync(memoryStream, ct);
                var pdfBytes = memoryStream.ToArray();

                return TypedResults.File(
                    pdfBytes,
                    contentType: "application/pdf",
                    fileDownloadName: "หนังสือเชิญชวนทำสัญญา.pdf");
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "Failed to convert document to PDF for ContractInvitationId: {ContractInvitationId}", req.ContractInvitationId);
                return TypedResults.NotFound("ไม่สามารถแปลงเอกสารเป็น PDF ได้");
            }
        }
    }
}
