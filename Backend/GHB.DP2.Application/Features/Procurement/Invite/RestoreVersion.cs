namespace GHB.DP2.Application.Features.Procurement.Invite;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.Procurement.Invite.Abstract;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PInvite;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record RestoreInviteVersionRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid ProcurementId,
    Guid InviteId,
    Guid EntrepreneurId,
    Guid SourceFileId);

public record RestoreInviteVersionResponse(
    Guid FileId,
    string Version);

public class RestoreInviteVersionEndpoint
    : InviteEndpointBase<RestoreInviteVersionRequest, Results<Ok<RestoreInviteVersionResponse>, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public RestoreInviteVersionEndpoint(
        Dp2DbContext dbContext,
        ILogger<RestoreInviteVersionEndpoint> logger)
        : base(dbContext, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b => b.WithTags("Procurement/Invite"));
        this.Post("procurement/{ProcurementId:guid}/invite/{InviteId:guid}/entrepreneur/{EntrepreneurId:guid}/restore-version/{SourceFileId:guid}");
    }

    protected override async ValueTask<Results<Ok<RestoreInviteVersionResponse>, NotFound<string>, BadRequest<string>>> HandleRequestAsync(
        RestoreInviteVersionRequest req,
        CancellationToken ct)
    {
        // 1. Get Invite
        var invite = await this.GetPInviteById(
            PInviteId.From(req.InviteId),
            ProcurementId.From(req.ProcurementId),
            ct);

        // 2. Get Entrepreneur
        var entrepreneur = invite.InvitedEntrepreneurs
            .FirstOrDefault(e => e.Id == PInvitedEntrepreneursId.From(req.EntrepreneurId));

        if (entrepreneur is null)
        {
            this.ThrowError(
                $"ไม่พบข้อมูลผู้ประกอบการ",
                StatusCodes.Status404NotFound);
        }

        // 3. Validate SourceFileId exists in document history
        var sourceHistory = entrepreneur.DocumentHistories
            .FirstOrDefault(h => h.FileId == FileId.From(req.SourceFileId));

        if (sourceHistory is null)
        {
            this.ThrowError(
                $"ไม่พบเอกสาร version ที่ต้องการดึง",
                StatusCodes.Status404NotFound);
        }

        // 4. Copy document using DocumentService
        var documentService = this.Resolve<IDocumentService>();

        var parentDirectory =
            $"Invite/{entrepreneur.Id}_RestoreFrom_{sourceHistory.Version}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt";

        var newFileId = await documentService.CopyDocumentTemplateAsync(
            FileId.From(req.SourceFileId),
            parentDirectory: parentDirectory,
            cancellationToken: ct);

        if (newFileId is null)
        {
            this.ThrowError(
                DocumentErrorMessages.CopyDocumentFailed,
                StatusCodes.Status500InternalServerError);
        }

        // 5. Calculate new version
        var lastHistory = entrepreneur.DocumentHistories
            .OrderVersions()
            .FirstOrDefault();

        var newVersion = lastHistory is not null
            ? RunningDocumentVersion.IncrementDocumentVersion(
                lastHistory.Version,
                lastHistory.StatusState.ToString(),
                invite.Status.ToString())
            : "1.0";

        // 6. Create new document history entry
        var newHistory = PInvitedEntrepreneursDocumentHistory.Create(
            entrepreneur.Id,
            invite.Status,
            newVersion,
            newFileId.Value,
            isReplace: true);

        entrepreneur.AddDocumentHistory(newHistory);

        // 7. Save changes
        this.dbContext.PInvitedEntrepreneurs.Update(entrepreneur);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok(new RestoreInviteVersionResponse(
            newFileId.Value.Value,
            newVersion));
    }
}
