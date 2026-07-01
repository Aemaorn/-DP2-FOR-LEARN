namespace GHB.DP2.Application.Features.SystemUtility.SuSection;

using FluentValidation;
using GHB.DP2.Application.Services;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record CreateSuSectionRequest(
    string NewId,
    string RefBankOrder,
    decimal MaximumBudget,
    string? Remark,
    string? SupplyMethodCode,
    string? SupplyMethodSpecialTypeCode,
    List<CreateSuSectionApproverRequest>? Approvers);

public record CreateSuSectionApproverRequest(
    string InRefCode,
    string PositionName,
    string ShortPosition,
    decimal Budget,
    SectionProcessType ProcessType,
    string CommandText);

public class CreateSuSectionRequestValidator : Validator<CreateSuSectionRequest>
{
    public CreateSuSectionRequestValidator()
    {
        this.RuleFor(x => x.NewId)
            .NotNull()
            .WithMessage("Section ID is required");

        this.RuleFor(x => x.RefBankOrder)
            .NotNull()
            .WithMessage("RefBankOrder is required");

        this.RuleFor(x => x.MaximumBudget)
            .GreaterThan(0)
            .WithMessage("Maximum budget must be greater than 0");

        this.RuleFor(x => x.Remark)
            .MaximumLength(500)
            .WithMessage("Remark must not exceed 500 characters");

        this.RuleForEach(x => x.Approvers)
            .SetValidator(new CreateSuSectionApproverRequestValidator());
    }
}

public class CreateSuSectionApproverRequestValidator : Validator<CreateSuSectionApproverRequest>
{
    public CreateSuSectionApproverRequestValidator()
    {
        this.RuleFor(x => x.ProcessType)
            .IsInEnum();

        this.RuleFor(x => x.InRefCode)
            .NotNull()
            .WithMessage("InRefCode is required");

        this.RuleFor(x => x.PositionName)
            .NotNull()
            .WithMessage("Position name is required")
            .MaximumLength(100)
            .WithMessage("Position name must not exceed 100 characters");

        this.RuleFor(x => x.ShortPosition)
            .NotNull()
            .WithMessage("Short position is required");

        this.RuleFor(x => x.Budget)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Budget must be greater than or equal to 0");

        this.RuleFor(x => x.CommandText)
            .NotNull()
            .WithMessage("Command text is required");
    }
}

public class CreateSuSection : SecureEndpointBase<CreateSuSectionRequest, Results<Ok<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public CreateSuSection(
        Dp2DbContext dbContext,
        IPermissionValidationService permissionValidationService,
        ILogger<CreateSuSection> logger)
        : base(permissionValidationService, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("SuSection"));
        this.Post("/st/sections");
        this.AuditLog("จัดการข้อมูลส่วนงาน", "เพิ่มข้อมูลส่วนงาน");
    }

    protected override async ValueTask<Results<Ok<string>, BadRequest<string>>> HandleRequestAsync(CreateSuSectionRequest req, CancellationToken ct)
    {
        var sectionId = SectionId.From(req.NewId);

        var existingSection = await this.dbContext.SuSections
                                        .SingleOrDefaultAsync(x => x.Id == sectionId, ct);

        if (existingSection is not null)
        {
            return TypedResults.BadRequest("Section ID already exists");
        }

        var supplyMethod = req.SupplyMethodCode is not null
            ? await this.dbContext.SuParameters.FirstOrDefaultAsync(x => x.Code == ParameterCode.From(req.SupplyMethodCode), ct)
            : null;

        var supplyMethodSpecialType = req.SupplyMethodSpecialTypeCode is not null
            ? await this.dbContext.SuParameters.FirstOrDefaultAsync(x => x.Code == ParameterCode.From(req.SupplyMethodSpecialTypeCode), ct)
            : null;

        var section = SuSection.Create(
            sectionId,
            req.RefBankOrder,
            req.SupplyMethodCode is not null ? ParameterCode.From(req.SupplyMethodCode) : null,
            req.SupplyMethodSpecialTypeCode is not null ? ParameterCode.From(req.SupplyMethodSpecialTypeCode) : null,
            req.MaximumBudget,
            req.Remark ?? string.Empty,
            supplyMethod,
            supplyMethodSpecialType);

        var approvers = req.Approvers?.Select(approver =>
            SuSectionApprover.Create(
                sectionId,
                approver.InRefCode,
                approver.PositionName,
                approver.ShortPosition,
                approver.Budget,
                approver.ProcessType,
                approver.CommandText)).ToList() ?? new List<SuSectionApprover>();

        section.UpdateApprovers(approvers);

        this.dbContext.SuSections.Add(section);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok(section.Id.Value);
    }
}