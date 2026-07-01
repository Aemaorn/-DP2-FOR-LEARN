namespace GHB.DP2.Application.Features.SystemUtility.SuSection;

using FluentValidation;
using GHB.DP2.Application.Services;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record UpdateSuSectionRequest(
    string Id,
    string? NewId,
    string RefBankOrder,
    decimal MaximumBudget,
    string? Remark,
    string? SupplyMethodCode,
    string? SupplyMethodSpecialTypeCode,
    List<UpdateSuSectionApproverRequest>? Approvers);

public record UpdateSuSectionApproverRequest(
    string InRefCode,
    string PositionName,
    string ShortPosition,
    decimal Budget,
    SectionProcessType ProcessType,
    string CommandText);

public class UpdateSuSectionRequestValidator : Validator<UpdateSuSectionRequest>
{
    public UpdateSuSectionRequestValidator()
    {
        this.RuleFor(x => x.Id)
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
            .SetValidator(new UpdateSuSectionApproverRequestValidator());
    }
}

public class UpdateSuSectionApproverRequestValidator : Validator<UpdateSuSectionApproverRequest>
{
    public UpdateSuSectionApproverRequestValidator()
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
            .WithMessage("งบประมาณต้องมากกว่า 0");

        this.RuleFor(x => x.CommandText)
            .NotNull()
            .WithMessage("Command text is required");
    }
}

public class UpdateSuSection : SecureEndpointBase<UpdateSuSectionRequest, Results<Ok<string>, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public UpdateSuSection(
        Dp2DbContext dbContext,
        IPermissionValidationService permissionValidationService,
        ILogger<UpdateSuSection> logger)
        : base(permissionValidationService, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("SuSection"));
        this.Put("/st/sections/{Id}");
        this.AuditLog("จัดการข้อมูลส่วนงาน", "แก้ไขข้อมูลส่วนงาน");
    }

    protected override async ValueTask<Results<Ok<string>, NotFound<string>, BadRequest<string>>> HandleRequestAsync(UpdateSuSectionRequest req, CancellationToken ct)
    {
        var sectionId = SectionId.From(req.Id);

        var section = await this.GetSuSectionById(sectionId, ct);

        if (section is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลส่วนงาน");
        }

        if (req.NewId is not null && req.NewId != req.Id)
        {
            var newSectionId = SectionId.From(req.NewId);

            var existSection = await this.GetSuSectionById(newSectionId, ct);

            if (existSection is not null)
            {
                return TypedResults.BadRequest("Section ID already exist");
            }

            var newSection = await this.CreateSectionWithApprovers(req, newSectionId, ct);

            await this.DeleteOldSection(section, ct);

            return TypedResults.Ok(newSection.Id.Value);
        }

        await this.UpdateSectionWithApprovers(section, req, ct);

        return TypedResults.Ok(section.Id.Value);
    }

    private async Task<SuSection?> GetSuSectionById(SectionId id, CancellationToken ct)
    {
        return await this.dbContext.SuSections
                         .Include(x => x.Approvers)
                         .SingleOrDefaultAsync(x => x.Id == id, ct);
    }

    private async Task<SuSection> CreateSectionWithApprovers(UpdateSuSectionRequest req, SectionId newSectionId, CancellationToken ct)
    {
        var supplyMethod = req.SupplyMethodCode is not null
            ? await this.dbContext.SuParameters.FirstOrDefaultAsync(x => x.Code == ParameterCode.From(req.SupplyMethodCode), ct)
            : null;

        var supplyMethodSpecialType = req.SupplyMethodSpecialTypeCode is not null
            ? await this.dbContext.SuParameters.FirstOrDefaultAsync(x => x.Code == ParameterCode.From(req.SupplyMethodSpecialTypeCode), ct)
            : null;

        var newSection = SuSection.Create(
            newSectionId,
            req.RefBankOrder,
            req.SupplyMethodCode is not null ? ParameterCode.From(req.SupplyMethodCode) : null,
            req.SupplyMethodSpecialTypeCode is not null ? ParameterCode.From(req.SupplyMethodSpecialTypeCode) : null,
            req.MaximumBudget,
            req.Remark ?? string.Empty,
            supplyMethod,
            supplyMethodSpecialType);

        var approvers = req.Approvers?.Select(approver =>
            SuSectionApprover.Create(
                newSectionId,
                approver.InRefCode,
                approver.PositionName,
                approver.ShortPosition,
                approver.Budget,
                approver.ProcessType,
                approver.CommandText)).ToList() ?? new List<SuSectionApprover>();

        newSection.UpdateApprovers(approvers);

        this.dbContext.SuSections.Add(newSection);
        await this.dbContext.SaveChangesAsync(ct);

        return newSection;
    }

    private async Task UpdateSectionWithApprovers(SuSection section, UpdateSuSectionRequest req, CancellationToken ct)
    {
        var supplyMethod = req.SupplyMethodCode is not null
            ? await this.dbContext.SuParameters.FirstOrDefaultAsync(x => x.Code == ParameterCode.From(req.SupplyMethodCode), ct)
            : null;

        var supplyMethodSpecialType = req.SupplyMethodSpecialTypeCode is not null
            ? await this.dbContext.SuParameters.FirstOrDefaultAsync(x => x.Code == ParameterCode.From(req.SupplyMethodSpecialTypeCode), ct)
            : null;

        section.Update(
            req.RefBankOrder,
            req.SupplyMethodCode is not null ? ParameterCode.From(req.SupplyMethodCode) : null,
            req.SupplyMethodSpecialTypeCode is not null ? ParameterCode.From(req.SupplyMethodSpecialTypeCode) : null,
            req.MaximumBudget,
            req.Remark ?? string.Empty,
            supplyMethod,
            supplyMethodSpecialType);

        this.dbContext.SuSectionApprovers.RemoveRange(section.Approvers);

        var updatedApprovers = req.Approvers?.Select(approver =>
            SuSectionApprover.Create(
                section.Id,
                approver.InRefCode,
                approver.PositionName,
                approver.ShortPosition,
                approver.Budget,
                approver.ProcessType,
                approver.CommandText)).ToList() ?? new List<SuSectionApprover>();

        section.UpdateApprovers(updatedApprovers);

        this.dbContext.SuSections.Update(section);
        await this.dbContext.SaveChangesAsync(ct);
    }

    private async Task DeleteOldSection(SuSection section, CancellationToken ct)
    {
        this.dbContext.SuSections.Remove(section);
        await this.dbContext.SaveChangesAsync(ct);
    }
}