namespace GHB.DP2.Application.Features.SystemUtility.SuSectionApprover;

using FluentValidation;
using GHB.DP2.Application.Services;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record CreateSectionApproverRequest(
    string SuSectionId,
    string InRefCode,
    string PositionName,
    string ShortPosition,
    decimal Budget,
    SectionProcessType ProcessType,
    string CommandText,
    decimal? CommandBudget);

public class CreateSectionApproverRequestValidator : Validator<CreateSectionApproverRequest>
{
    public CreateSectionApproverRequestValidator()
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

public class CreateSectionApprover : SecureEndpointBase<CreateSectionApproverRequest, Results<Created<string>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public CreateSectionApprover(
        Dp2DbContext dbContext,
        IPermissionValidationService permissionValidationService,
        ILogger<CreateSectionApprover> logger)
        : base(permissionValidationService, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("SuSectionApprover"));
        this.Post("/st/section-approver/{SuSectionId}/approvers");
        this.AuditLog("จัดการผู้อนุมัติตามวงเงิน", "เพิ่มผู้อนุมัติ");
        this.AllowAnonymous();
    }

    public override Task OnBeforeHandleAsync(CreateSectionApproverRequest req, CancellationToken ct)
        => Task.CompletedTask;

    protected override async ValueTask<Results<Created<string>, NotFound<string>>> HandleRequestAsync(CreateSectionApproverRequest req, CancellationToken ct)
    {
        var sectionId = SectionId.From(req.SuSectionId);

        var section = await this.dbContext.SuSections
            .Include(x => x.Approvers)
            .FirstOrDefaultAsync(x => x.Id == sectionId, ct);

        if (section is null)
        {
            return TypedResults.NotFound($"SuSection with Id {req.SuSectionId} not found");
        }

        var approver = SuSectionApprover.Create(
            sectionId,
            req.InRefCode,
            req.PositionName,
            req.ShortPosition,
            req.Budget,
            req.ProcessType,
            req.CommandText,
            req.CommandBudget);

        section.UpdateApprovers([.. section.Approvers, approver]);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Created(
            $"/st/section-approver/{req.SuSectionId}/approvers/{approver.Id.Value}",
            approver.Id.Value.ToString());
    }
}
