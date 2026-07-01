namespace GHB.DP2.Application.Features.Procurement.Jp005;

using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.Jp005.Abstract;
using GHB.DP2.Application.Services.Document;
using System.IdentityModel.Tokens.Jwt;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PJp005;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record ResetJp005DocumentRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid ProcurementId,
    Guid Id,
    PJp005DocumentType DocumentType);

public class ResetJp005DocumentEndpoint : Jp005EndpointBase<ResetJp005DocumentRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public ResetJp005DocumentEndpoint(
        Dp2DbContext dbContext,
        IOperationService operationService,
        ICommandTextService commandTextService,
        ILogger<ResetJp005DocumentEndpoint> logger)
        : base(dbContext, operationService, commandTextService, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Post("procurement/{ProcurementId:guid}/jp005/{Id:guid}/reset-document");
        this.Description(b =>
            b.WithTags("Procurement/JorPor005")
             .WithName("ResetJp005Document"));
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(
        ResetJp005DocumentRequest req,
        CancellationToken ct)
    {
        var jp005 = await this.dbContext.PJp005S
                              .Include(j => j.DocumentHistories)
                              .Include(j => j.Procurement)
                              .ThenInclude(p => p.Department)
                              .Include(j => j.Procurement)
                              .ThenInclude(p => p.SupplyMethod)
                              .Include(j => j.Procurement)
                              .ThenInclude(p => p.SupplyMethodType)
                              .Include(j => j.Procurement)
                              .ThenInclude(p => p.SupplyMethodSpecialType)
                              .Include(j => j.Procurement)
                              .ThenInclude(p => p.Plan)
                              .Include(j => j.Committees)
                              .ThenInclude(c => c.User)
                              .ThenInclude(u => u.Employee)
                              .ThenInclude(e => e.View)
                              .Include(j => j.CommitteeDuties)
                              .Include(j => j.ProcurementSuppliesDivisions)
                              .Include(j => j.Acceptors)
                              .ThenInclude(a => a.User)
                              .ThenInclude(u => u.Employee)
                              .ThenInclude(e => e.View)
                              .FirstOrDefaultAsync(
                                  j => j.Id == PJp005Id.From(req.Id) &&
                                       j.ProcurementId == ProcurementId.From(req.ProcurementId),
                                  ct);

        if (jp005 == null)
        {
            return TypedResults.NotFound(DocumentErrorMessages.DocumentNotFound);
        }

        // Get procurement for mapping
        var procurement = jp005.Procurement;

        // Get template from SuDocumentTemplates (using base class method)
        var templateFileId = await this.GetDocumentTemplateForResetAsync(
            jp005,
            req.DocumentType,
            procurement.SupplyMethodCode,
            ct);

        // Create replace DTO - user who reset becomes the creator
        var replaceDto = await this.GetJp005MapToResponseMappingDtoAsync(
            jp005,
            procurement,
            req.UserId,
            hasCreator: true,
            hasAcceptor: false,
            hasPublisher: false,
            cancellationToken: ct);

        // Copy template and replace placeholders in one step
        var documentService = this.Resolve<IDocumentService>();
        var newFileId = await documentService.CopyDocumentTemplateAsync(
            templateFileId,
            contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto),
            parentDirectory: $"{DocumentTemplateGroups.Jp05}/{jp005.Id}_{req.DocumentType}_Reset_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
            cancellationToken: ct);

        if (newFileId is null)
        {
            return TypedResults.BadRequest(DocumentErrorMessages.CopyDocumentFailed);
        }

        jp005.AddDocumentHistory(req.DocumentType, newFileId.Value, false, incrementMajor: true);

        this.dbContext.PJp005S.Update(jp005);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }
}