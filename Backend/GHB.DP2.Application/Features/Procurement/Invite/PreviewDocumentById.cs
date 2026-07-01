namespace GHB.DP2.Application.Features.Procurement.Invite;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Application.Features.Procurement.Invite.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PInvite;
using GHB.DP2.Domain.Procurement.PJp005;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record PreviewInviteDocumentRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid Id,
    Guid ProcurementId,
    Guid EntrepreneurId);

public class PreviewInviteDocumentEndpoint : InviteEndpointBase<PreviewInviteDocumentRequest, Results<Ok<Guid>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;
    private readonly IFileServiceClient fileServiceClient;

    public PreviewInviteDocumentEndpoint(
        Dp2DbContext dbContext,
        IFileServiceClient fileServiceClient,
        ILogger<PreviewInviteDocumentEndpoint> logger)
        : base(dbContext, logger)
    {
        this.dbContext = dbContext;
        this.fileServiceClient = fileServiceClient;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("Procurement/Invite")
             .WithName("InvitePreviewDocument")
             .Produces<Ok>()
             .Produces<NotFound>());
        this.Get("procurement/{ProcurementId:guid}/invite/{Id:guid}/entrepreneur/{EntrepreneurId:guid}/review-document");
    }

    protected override async ValueTask<Results<Ok<Guid>, NotFound<string>>> HandleRequestAsync(PreviewInviteDocumentRequest req, CancellationToken ct)
    {
        var invite = await this.dbContext.PInvites
                               .Include(x => x.Procurement)
                               .Include(x => x.Acceptors)
                               .ThenInclude(pInviteAcceptors => pInviteAcceptors.CommitteePosition)
                               .Include(x => x.InvitedEntrepreneurs)
                               .ThenInclude(pInvitedEntrepreneurs => pInvitedEntrepreneurs.Vendor)
                               .Include(pInvite => pInvite.InvitedEntrepreneurs)
                               .ThenInclude(pInvitedEntrepreneurs => pInvitedEntrepreneurs.InvitedEntrepreneurShareholders)
                               .Include(pInvite => pInvite.InvitedEntrepreneurs)
                               .ThenInclude(pInvitedEntrepreneurs => pInvitedEntrepreneurs.DocumentHistories)
                               .FirstOrDefaultAsync(x => x.Id == PInviteId.From(req.Id) && x.ProcurementId == ProcurementId.From(req.ProcurementId), ct);

        if (invite is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลคำเชิญที่มีรหัส {req.Id}");
        }

        var entrepreneur = invite.InvitedEntrepreneurs
            .FirstOrDefault(e => e.Id == PInvitedEntrepreneursId.From(req.EntrepreneurId));

        if (entrepreneur is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลผู้ประกอบการ");
        }

        var committees = await this.dbContext.PJp005S
                                   .Where(c => c.ProcurementId == ProcurementId.From(req.ProcurementId))
                                   .SelectMany(s => s.Committees)
                                   .Where(w => w.GroupType == PJp005CommitteeGroupType.ProcurementCommittee)
                                   .ToArrayAsync(ct);

        var operators =
            committees.Select(s => s.SuUserId)
                      .ToArray();

        var response = await this.MapToResponseMappingDtoAsync(invite, operators, req.UserId, false, entrepreneur, ct);

        var getLastedDraftDocumentHistory = entrepreneur.DocumentHistories
                                                        .Where(d => d.StatusState == PInviteStatus.Draft)
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

        var fileContent = OdtDocumentExtensions.ReplaceOdtDocument(file.Contents, response);

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