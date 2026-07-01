namespace GHB.DP2.Application.Features.Procurement.Jp006;

using GHB.DP2.Application.Constants;
using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.Jp006.Abstract;
using GHB.DP2.Application.Services.Document;
using System.IdentityModel.Tokens.Jwt;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PPurchaseOrder;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record ResetJp006DocumentRequest(Guid ProcurementId, Guid Jp006Id, PurchaseOrderDocumentType DocumentType);

public class ResetJp006DocumentEndpoint : Jp006EndpointBase<ResetJp006DocumentRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public ResetJp006DocumentEndpoint(
        ILogger<ResetJp006DocumentEndpoint> logger,
        IOperationService operationService,
        ICommandTextService commandTextService,
        IFileServiceClient fileServiceClient,
        Dp2DbContext dbContext)
        : base(logger, operationService, commandTextService, fileServiceClient, dbContext)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Post("procurement/{ProcurementId:guid}/jp006/{Jp006Id:guid}/reset-document");
        this.Description(b =>
            b.WithTags(nameof(Jp006))
             .WithName("ResetJp006Document"));
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(
        ResetJp006DocumentRequest req,
        CancellationToken ct)
    {
        var jp006 = await this.dbContext.PJp006S
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
                              .Include(j => j.Procurement)
                              .ThenInclude(p => p.PurchaseRequisitions)
                              .ThenInclude(pr => pr.Budgets)
                              .ThenInclude(b => b.PpPurchaseRequisitionBudgetDetails)
                              .Include(j => j.Procurement)
                              .ThenInclude(p => p.PurchaseRequisitions)
                              .ThenInclude(pr => pr.Assignees)
                              .ThenInclude(a => a.User)
                              .ThenInclude(u => u.Employee)
                              .ThenInclude(e => e.View)
                              .Include(j => j.Procurement)
                              .ThenInclude(p => p.Jp005)
                              .ThenInclude(jp005 => jp005.Acceptors)
                              .ThenInclude(a => a.User)
                              .ThenInclude(u => u.Employee)
                              .ThenInclude(e => e.View)
                              .Include(j => j.Procurement)
                              .ThenInclude(p => p.Invites)
                              .Include(j => j.Entrepreneurs)
                              .ThenInclude(e => e.SuVendor)
                              .Include(j => j.Entrepreneurs)
                              .ThenInclude(e => e.PJp006PriceDetails)
                              .Include(j => j.Entrepreneurs)
                              .ThenInclude(e => e.PurchaseOrderShareholders)
                              .Include(j => j.Acceptors)
                              .ThenInclude(a => a.User)
                              .ThenInclude(u => u.Employee)
                              .ThenInclude(e => e.View)
                              .Include(j => j.Assignees)
                              .ThenInclude(a => a.User)
                              .ThenInclude(u => u.Employee)
                              .ThenInclude(e => e.View)
                              .AsSplitQuery()
                              .FirstOrDefaultAsync(
                                  j => j.Id == PurchaseOrderId.From(req.Jp006Id) &&
                                       j.ProcurementId == ProcurementId.From(req.ProcurementId),
                                  ct);

        if (jp006 == null)
        {
            return TypedResults.NotFound(DocumentErrorMessages.DocumentNotFound);
        }

        // Get procurement for mapping
        var procurement = jp006.Procurement;

        // Get template from SuDocumentTemplates instead of version 1.0
        var winners = jp006.Entrepreneurs.Where(e => e.IsWinner);

        var sumAgreePrice = winners.Any()
            ? winners.Sum(e => e.PJp006PriceDetails.Sum(pd => pd.AgreedPrice * pd.ParcelQuantity))
            : 0m;

        var templateFileId = await this.GetDocumentTemplateForResetAsync(
            jp006,
            req.DocumentType,
            procurement.SupplyMethodCode,
            sumAgreePrice,
            ct);

        // Create replace DTO - user who reset becomes the creator
        var replaceDto = await this.MapPJp006Replace(
            jp006,
            Guid.TryParse(this.HttpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var userId) ? UserId.From(userId) : UserId.From(Guid.Empty),
            hasCreator: true,
            hasCommittee: false,
            hasAcceptor: false,
            hasPublisher: false,
            ct);

        if (replaceDto is null)
        {
            return TypedResults.BadRequest("ไม่สามารถสร้าง Replace DTO ได้");
        }

        // Use CopyDocumentTemplateAsync with ReplaceOdtDocument using template file
        var documentService = this.Resolve<IDocumentService>();
        var newFileId = await documentService.CopyDocumentTemplateAsync(
            templateFileId,
            contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto),
            parentDirectory: $"{DocumentTemplateGroups.Jp06}/{jp006.Id}_{req.DocumentType}_Reset_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
            cancellationToken: ct);

        if (newFileId is null)
        {
            return TypedResults.BadRequest(DocumentErrorMessages.CopyDocumentFailed);
        }

        jp006.AddDocumentHistory(req.DocumentType, newFileId.Value, incrementMajor: true);

        this.dbContext.PJp006S.Update(jp006);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }
}
