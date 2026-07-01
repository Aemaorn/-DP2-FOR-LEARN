namespace GHB.DP2.Application.Features.SystemUtility.SuSectionApprover;

using FluentValidation;
using GHB.DP2.Application.Services;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record UpdateSectionApproverRequest(
    string SuSectionId,
    string ApproverId,
    string InRefCode,
    string PositionName,
    string ShortPosition,
    decimal Budget,
    SectionProcessType ProcessType,
    string CommandText,
    decimal? CommandBudget);

public class UpdateSectionApproverRequestValidator : Validator<UpdateSectionApproverRequest>
{
    public UpdateSectionApproverRequestValidator()
    {
        this.RuleFor(x => x.ProcessType)
            .IsInEnum();

        this.RuleFor(x => x.InRefCode)
            .NotEmpty()
            .WithMessage("InRefCode is required");

        this.RuleFor(x => x.PositionName)
            .NotEmpty()
            .WithMessage("Position name is required");
    }
}

public class UpdateSectionApprover : SecureEndpointBase<UpdateSectionApproverRequest, Results<Ok, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public UpdateSectionApprover(
        Dp2DbContext dbContext,
        IPermissionValidationService permissionValidationService,
        ILogger<UpdateSectionApprover> logger)
        : base(permissionValidationService, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("SuSectionApprover"));
        this.Put("/st/section-approver/{SuSectionId}/approvers/{ApproverId}");
        this.AuditLog("จัดการผู้อนุมัติตามวงเงิน", "แก้ไขผู้อนุมัติตามวงเงิน");
        this.AllowAnonymous();
    }

    public override Task OnBeforeHandleAsync(UpdateSectionApproverRequest req, CancellationToken ct)
        => Task.CompletedTask;

    protected override async ValueTask<Results<Ok, NotFound<string>>> HandleRequestAsync(UpdateSectionApproverRequest req, CancellationToken ct)
    {
        var approverId = SectionApproverId.From(Guid.Parse(req.ApproverId));
        var sectionId = SectionId.From(req.SuSectionId);

        var section = await this.dbContext.SuSections
            .Include(x => x.Approvers)
            .FirstOrDefaultAsync(x => x.Id == sectionId, ct);

        var approver = section?.Approvers.FirstOrDefault(x => x.Id == approverId);

        if (approver is null)
        {
            return TypedResults.NotFound($"Approver with Id {req.ApproverId} not found");
        }

        approver.Update(
            req.InRefCode,
            req.PositionName,
            req.ShortPosition,
            req.Budget,
            req.ProcessType,
            req.CommandText,
            req.CommandBudget);

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }
}
