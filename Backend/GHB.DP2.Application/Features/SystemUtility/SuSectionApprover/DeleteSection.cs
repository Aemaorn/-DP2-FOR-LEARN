namespace GHB.DP2.Application.Features.SystemUtility.SuSectionApprover;

using GHB.DP2.Application.Services;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record DeleteSectionRequest(string Id);

public class DeleteSection : SecureEndpointBase<DeleteSectionRequest, Results<NoContent, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public DeleteSection(
        Dp2DbContext dbContext,
        IPermissionValidationService permissionValidationService,
        ILogger<DeleteSection> logger)
        : base(permissionValidationService, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("SuSectionApprover"));
        this.Delete("/st/section-approver/sections/{Id}");
        this.AuditLog("จัดการผู้อนุมัติตามวงเงิน", "ลบข้อมูลวงเงิน");
        this.AllowAnonymous();
    }

    public override Task OnBeforeHandleAsync(DeleteSectionRequest req, CancellationToken ct)
        => Task.CompletedTask;

    protected override async ValueTask<Results<NoContent, NotFound<string>>> HandleRequestAsync(DeleteSectionRequest req, CancellationToken ct)
    {
        var sectionId = SectionId.From(req.Id);

        var section = await this.dbContext.SuSections
            .Include(x => x.Approvers)
            .FirstOrDefaultAsync(x => x.Id == sectionId, ct);

        if (section is null)
        {
            return TypedResults.NotFound($"SuSection with Id {req.Id} not found");
        }

        this.dbContext.SuSections.Remove(section);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }
}
