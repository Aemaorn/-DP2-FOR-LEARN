namespace GHB.DP2.Application.Features.SystemUtility.SuDocumentTemplate;

using GHB.DP2.Application.Services;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class DeleteSuDocumentTemplateRequest
{
    public Guid Id { get; init; }
}

public class DeleteSuDocumentTemplate :
    SecureEndpointBase<DeleteSuDocumentTemplateRequest,
                       Results<NoContent, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public DeleteSuDocumentTemplate(
        Dp2DbContext dbContext,
        IPermissionValidationService permissionValidationService,
        ILogger<DeleteSuDocumentTemplate> logger)
        : base(permissionValidationService, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("SuDocumentTemplate"));
        this.Delete("/st/st007/{Id:guid}");
        this.AuditLog("จัดการรูปแบบเอกสาร", "ลบรูปแบบเอกสาร");
    }

    protected override async ValueTask<Results<NoContent, NotFound<string>>> HandleRequestAsync(DeleteSuDocumentTemplateRequest req, CancellationToken ct)
    {
        var data = await this.dbContext.SuDocumentTemplates
                             .Include(i => i.BudgetForDocument)
                             .FirstOrDefaultAsync(x => x.Id == SuDocumentTemplateId.From(req.Id), ct);

        if (data is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลรูปแบบเอกสาร");
        }

        this.dbContext.SuDocumentTemplates.Remove(data);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }
}