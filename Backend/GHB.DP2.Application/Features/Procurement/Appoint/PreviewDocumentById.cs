namespace GHB.DP2.Application.Features.Procurement.Appoint;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.Appoint.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.PpAppoint;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record PreviewAppointDocumentRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid Id);

public class PreviewAppointDocumentEndpoint : AppointEndpointBase<PreviewAppointDocumentRequest, Results<Ok<Guid>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;
    private readonly IFileServiceClient fileServiceClient;

    public PreviewAppointDocumentEndpoint(
        Dp2DbContext dbContext,
        IOperationService operationService,
        ICommandTextService commandTextService,
        IFileServiceClient fileServiceClient,
        ILogger<PreviewAppointDocumentEndpoint> logger)
        : base(dbContext, logger, operationService, commandTextService)
    {
        this.dbContext = dbContext;
        this.fileServiceClient = fileServiceClient;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("Procurement/Appoint")
             .WithName("AppointPreviewDocument")
             .Produces<Ok>()
             .Produces<NotFound>());
        this.Get("appointments/{Id:guid}/review-document");
    }

    protected override async ValueTask<Results<Ok<Guid>, NotFound<string>>> HandleRequestAsync(PreviewAppointDocumentRequest req, CancellationToken ct)
    {
        var appointId = PpAppointId.From(req.Id);

        var appoint = await this.dbContext.PpAppoints
                                .AsNoTracking()
                                .Include(x => x.TorDraftCommittees)
                                .ThenInclude(ppAppointTorDraftCommittee => ppAppointTorDraftCommittee.User)
                                .ThenInclude(suUser => suUser.Employee)
                                .ThenInclude(rawEmployee => rawEmployee.View)
                                .Include(x => x.TorDraftCommitteeDuties)
                                .Include(x => x.MedianPriceCommittees)
                                .ThenInclude(ppAppointMedianPriceCommittee => ppAppointMedianPriceCommittee.User)
                                .ThenInclude(suUser => suUser.Employee)
                                .ThenInclude(rawEmployee => rawEmployee.View)
                                .Include(x => x.MedianPriceCommitteeDuties)
                                .Include(x => x.Acceptors)
                                .Include(ppAppoint => ppAppoint.DocumentHistories)
                                .Include(auditableEntity => auditableEntity.AuditInfo)
                                .FirstOrDefaultAsync(x => x.Id == appointId, ct);

        if (appoint is null)
        {
            return TypedResults.NotFound($"Appointment with ID {req.Id} not found.");
        }

        var dto = await this.MapToReplaceDto(appoint, ct, isPreview: true);

        var getLastedDraftDocumentHistory = appoint.DocumentHistories
                                                   .Where(d => d.StatusState == AppointStatus.Draft)
                                                   .OrderVersions()
                                                   .FirstOrDefault();

        if (getLastedDraftDocumentHistory is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลแผนที่ร่าง");
        }

        var file = await this.fileServiceClient.DownloadAsync(getLastedDraftDocumentHistory.FileId, cancellationToken: ct);

        if (file == null)
        {
            return TypedResults.NotFound("ไม่พบไฟล์แผนที่ร่าง");
        }

        var fileContent = OdtDocumentExtensions.ReplaceOdtDocument(file.Contents, dto);

        var odt = DocumentService.DetectContentType(fileContent);
        var unixTimeOneDay = DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds();
        var fileResult = await this.fileServiceClient.UploadFileAsync(
            fileContent,
            contentType: odt,
            expirationUnixSeconds: unixTimeOneDay,
            cancellationToken: ct);

        return TypedResults.Ok(fileResult.Id.Value);
    }
}