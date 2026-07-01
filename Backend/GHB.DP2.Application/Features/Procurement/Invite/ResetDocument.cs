namespace GHB.DP2.Application.Features.Procurement.Invite;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.Procurement.Invite.Abstract;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PInvite;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record ResetInviteDocumentRequest(
    Guid UserId,
    Guid ProcurementId,
    Guid InviteId,
    Guid EntrepreneurId);

public class ResetInviteDocumentEndpoint : InviteEndpointBase<ResetInviteDocumentRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public ResetInviteDocumentEndpoint(
        Dp2DbContext dbContext,
        ILogger<ResetInviteDocumentEndpoint> logger)
        : base(dbContext, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("Procurement/Invite")
             .WithName("ResetInviteDocument")
             .Accepts<ResetInviteDocumentRequest>("application/json"));
        this.Post("procurement/{ProcurementId:guid}/invite/{InviteId:guid}/entrepreneur/{EntrepreneurId:guid}/reset-document");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(
        ResetInviteDocumentRequest req,
        CancellationToken ct)
    {
        var invite = await this.GetPInviteById(
            PInviteId.From(req.InviteId),
            ProcurementId.From(req.ProcurementId),
            ct);

        // Get Entrepreneur
        var entrepreneur = invite.InvitedEntrepreneurs
            .FirstOrDefault(e => e.Id == PInvitedEntrepreneursId.From(req.EntrepreneurId));

        if (entrepreneur is null)
        {
            this.ThrowError(
                $"ไม่พบข้อมูลผู้ประกอบการ",
                StatusCodes.Status404NotFound);
        }

        // Get latest template from SuDocumentTemplates
        var templateFileId = await this.GetDocumentTemplateForResetAsync(ct);

        // Get operators for mapping
        var jp005Committees = await this.dbContext.PJp005S
            .Where(c => c.ProcurementId == invite.ProcurementId)
            .SelectMany(s => s.Committees)
            .Where(w => w.GroupType == Domain.Procurement.PJp005.PJp005CommitteeGroupType.ProcurementCommittee)
            .ToArrayAsync(ct);

        var operators = jp005Committees.Select(s => s.SuUserId);

        // Create replace DTO
        var replaceDto = await this.MapToResponseMappingDtoAsync(invite, operators, req.UserId, hasAcceptor: false, entrepreneur, ct);

        // Use CopyDocumentTemplateAsync with ReplaceOdtDocument
        var documentService = this.Resolve<IDocumentService>();
        var newFileId = await documentService.CopyDocumentTemplateAsync(
            templateFileId,
            contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto),
            parentDirectory: $"{DocumentTemplateGroups.INV}/{entrepreneur.Id}_ResetFromTemplate_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
            cancellationToken: ct);

        if (newFileId is null)
        {
            this.ThrowError(
                "ไม่สามารถดึง template เอกสารได้",
                StatusCodes.Status500InternalServerError);
        }

        // ใช้ NextVersion กับ incrementMajor: true เพื่อขึ้น version 2.0, 3.0
        var newVersion = entrepreneur.DocumentHistories.NextVersion(incrementMajor: true);

        var newHistory = PInvitedEntrepreneursDocumentHistory.Create(
            entrepreneur.Id,
            invite.Status,
            newVersion,
            newFileId!.Value);

        entrepreneur.AddDocumentHistory(newHistory);

        this.dbContext.PInvitedEntrepreneurs.Update(entrepreneur);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private async Task<FileId> GetDocumentTemplateForResetAsync(CancellationToken ct)
    {
        var documentService = this.Resolve<IDocumentService>();

        var templateFileId = await documentService.GetDocumentTemplateAsync(
            dt =>
                dt.Group == DocumentTemplateGroups.INV &&
                dt.IsActive,
            ct);

        if (templateFileId is null)
        {
            this.ThrowError(
                DocumentErrorMessages.TemplateNotFoundForReset,
                StatusCodes.Status404NotFound);
        }

        return templateFileId.Value;
    }
}