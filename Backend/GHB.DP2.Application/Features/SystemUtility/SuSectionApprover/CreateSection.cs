namespace GHB.DP2.Application.Features.SystemUtility.SuSectionApprover;

using FluentValidation;
using GHB.DP2.Application.Services;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record CreateSectionRequest(
    string NewId,
    string RefBankOrder,
    decimal MaximumBudget,
    string? Remark,
    string? SupplyMethodCode,
    string? SupplyMethodSpecialTypeCode);

public class CreateSectionRequestValidator : Validator<CreateSectionRequest>
{
    public CreateSectionRequestValidator()
    {
        this.RuleFor(x => x.NewId)
            .NotEmpty()
            .WithMessage("Section ID is required");

        this.RuleFor(x => x.RefBankOrder)
            .NotEmpty()
            .WithMessage("RefBankOrder is required");
    }
}

public class CreateSection : SecureEndpointBase<CreateSectionRequest, Results<Ok<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public CreateSection(
        Dp2DbContext dbContext,
        IPermissionValidationService permissionValidationService,
        ILogger<CreateSection> logger)
        : base(permissionValidationService, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("SuSectionApprover"));
        this.Post("/st/section-approver/sections");
        this.AuditLog("จัดการผู้อนุมัติตามวงเงิน", "เพิ่มข้อมูลวงเงิน");
        this.AllowAnonymous();
    }

    public override Task OnBeforeHandleAsync(CreateSectionRequest req, CancellationToken ct)
        => Task.CompletedTask;

    protected override async ValueTask<Results<Ok<string>, BadRequest<string>>> HandleRequestAsync(CreateSectionRequest req, CancellationToken ct)
    {
        var sectionId = SectionId.From(req.NewId);

        var exists = await this.dbContext.SuSections.AnyAsync(x => x.Id == sectionId, ct);
        if (exists)
        {
            return TypedResults.BadRequest("Section ID already exists");
        }

        SuParameter? supplyMethod = null;
        if (req.SupplyMethodCode is not null)
        {
            supplyMethod = await this.dbContext.SuParameters.FirstOrDefaultAsync(x => x.Code == ParameterCode.From(req.SupplyMethodCode), ct);
            if (supplyMethod is null)
            {
                return TypedResults.BadRequest($"SupplyMethodCode '{req.SupplyMethodCode}' not found in SuParameter");
            }
        }

        SuParameter? supplyMethodSpecialType = null;
        if (req.SupplyMethodSpecialTypeCode is not null)
        {
            supplyMethodSpecialType = await this.dbContext.SuParameters.FirstOrDefaultAsync(x => x.Code == ParameterCode.From(req.SupplyMethodSpecialTypeCode), ct);
            if (supplyMethodSpecialType is null)
            {
                return TypedResults.BadRequest($"SupplyMethodSpecialTypeCode '{req.SupplyMethodSpecialTypeCode}' not found in SuParameter");
            }
        }

        var section = SuSection.Create(
            sectionId,
            req.RefBankOrder,
            req.SupplyMethodCode is not null ? ParameterCode.From(req.SupplyMethodCode) : null,
            req.SupplyMethodSpecialTypeCode is not null ? ParameterCode.From(req.SupplyMethodSpecialTypeCode) : null,
            req.MaximumBudget,
            req.Remark,
            supplyMethod,
            supplyMethodSpecialType);

        this.dbContext.SuSections.Add(section);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok(section.Id.Value);
    }
}
