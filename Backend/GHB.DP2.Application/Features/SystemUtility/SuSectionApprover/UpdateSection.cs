namespace GHB.DP2.Application.Features.SystemUtility.SuSectionApprover;

using FluentValidation;
using GHB.DP2.Application.Services;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record UpdateSectionRequest(
    string Id,
    string RefBankOrder,
    decimal MaximumBudget,
    string? Remark,
    string? SupplyMethodCode,
    string? SupplyMethodSpecialTypeCode);

public class UpdateSectionRequestValidator : Validator<UpdateSectionRequest>
{
    public UpdateSectionRequestValidator()
    {
        this.RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Section ID is required");

        this.RuleFor(x => x.RefBankOrder)
            .NotEmpty()
            .WithMessage("RefBankOrder is required");
    }
}

public class UpdateSection : SecureEndpointBase<UpdateSectionRequest, Results<Ok, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public UpdateSection(
        Dp2DbContext dbContext,
        IPermissionValidationService permissionValidationService,
        ILogger<UpdateSection> logger)
        : base(permissionValidationService, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("SuSectionApprover"));
        this.Put("/st/section-approver/sections/{Id}");
        this.AuditLog("จัดการผู้อนุมัติตามวงเงิน", "แก้ไขข้อมูลวงเงิน");
        this.AllowAnonymous();
    }

    public override Task OnBeforeHandleAsync(UpdateSectionRequest req, CancellationToken ct)
        => Task.CompletedTask;

    protected override async ValueTask<Results<Ok, NotFound<string>>> HandleRequestAsync(UpdateSectionRequest req, CancellationToken ct)
    {
        var sectionId = SectionId.From(req.Id);

        var section = await this.dbContext.SuSections
            .FirstOrDefaultAsync(x => x.Id == sectionId, ct);

        if (section is null)
        {
            return TypedResults.NotFound($"SuSection with Id {req.Id} not found");
        }

        SuParameter? supplyMethod = null;
        if (req.SupplyMethodCode is not null)
        {
            supplyMethod = await this.dbContext.SuParameters.FirstOrDefaultAsync(x => x.Code == ParameterCode.From(req.SupplyMethodCode), ct);
            if (supplyMethod is null)
            {
                return TypedResults.NotFound($"SupplyMethodCode '{req.SupplyMethodCode}' not found in SuParameter");
            }
        }

        SuParameter? supplyMethodSpecialType = null;
        if (req.SupplyMethodSpecialTypeCode is not null)
        {
            supplyMethodSpecialType = await this.dbContext.SuParameters.FirstOrDefaultAsync(x => x.Code == ParameterCode.From(req.SupplyMethodSpecialTypeCode), ct);
            if (supplyMethodSpecialType is null)
            {
                return TypedResults.NotFound($"SupplyMethodSpecialTypeCode '{req.SupplyMethodSpecialTypeCode}' not found in SuParameter");
            }
        }

        section.Update(
            req.RefBankOrder,
            req.SupplyMethodCode is not null ? ParameterCode.From(req.SupplyMethodCode) : null,
            req.SupplyMethodSpecialTypeCode is not null ? ParameterCode.From(req.SupplyMethodSpecialTypeCode) : null,
            req.MaximumBudget,
            req.Remark ?? string.Empty,
            supplyMethod,
            supplyMethodSpecialType);

        this.dbContext.SuSections.Update(section);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }
}
