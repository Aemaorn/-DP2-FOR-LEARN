namespace GHB.DP2.Application.Features.SystemUtility.SuSectionApprover;

using GHB.DP2.Application.Services;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record DeleteSectionApproverRequest(string SuSectionId, Guid ApproverId);

public class DeleteSectionApprover : SecureEndpointBase<DeleteSectionApproverRequest, Results<NoContent, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public DeleteSectionApprover(
        Dp2DbContext dbContext,
        IPermissionValidationService permissionValidationService,
        ILogger<DeleteSectionApprover> logger)
        : base(permissionValidationService, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("SuSectionApprover"));
        this.Delete("/st/section-approver/{SuSectionId}/approvers/{ApproverId:guid}");
        this.AuditLog("จัดการผู้อนุมัติตามวงเงิน", "ลบผู้อนุมัติ");
        this.AllowAnonymous();
    }

    public override Task OnBeforeHandleAsync(DeleteSectionApproverRequest req, CancellationToken ct)
        => Task.CompletedTask;

    protected override async ValueTask<Results<NoContent, NotFound<string>>> HandleRequestAsync(DeleteSectionApproverRequest req, CancellationToken ct)
    {
        var sectionId = SectionId.From(req.SuSectionId);
        var approverId = SectionApproverId.From(req.ApproverId);

        var section = await this.dbContext.SuSections
            .Include(x => x.Approvers)
            .FirstOrDefaultAsync(x => x.Id == sectionId, ct);

        if (section is null)
        {
            return TypedResults.NotFound($"SuSection with Id {req.SuSectionId} not found");
        }

        var approver = section.Approvers.FirstOrDefault(x => x.Id == approverId);

        if (approver is null)
        {
            return TypedResults.NotFound($"Approver with Id {req.ApproverId} not found");
        }

        section.UpdateApprovers(section.Approvers.Where(x => x.Id != approverId).ToList());
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }
}
