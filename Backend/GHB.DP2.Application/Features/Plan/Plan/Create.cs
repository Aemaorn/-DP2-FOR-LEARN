namespace GHB.DP2.Application.Features.Plan.Plan;

using Codehard.FileService.Client.Abstractions;
using FluentValidation;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Validators;
using GHB.DP2.Application.Features.Plan.Plan.Abstract;
using GHB.DP2.Domain.Plan;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record CreatePlanRequest(
    PlanStatus Status,
    string DepartmentCode,
    PlanType Type,
    string SupplyMethodCode,
    string? SupplyMethodTypeCode,
    string? SupplyMethodSpecialTypeCode,
    int BudgetYear,
    string Name,
    decimal Budget,
    DateTimeOffset ExpectingProcurementAt,
    string? Remark,
    string? Telephone,
    bool IsStock,
    string? AssignSegmentCode,
    string? GroupEgpNumber,
    string? EgpNumber,
    bool? IsCommercialMaterial,
    AcceptorRequest[]? Acceptors,
    AttachmentsDtoWithId[]? Attachments);

public class CreatePlanRequestValidator : Validator<CreatePlanRequest>
{
    public CreatePlanRequestValidator()
    {
        this.RuleFor(p => p.DepartmentCode)
            .NotEmpty()
            .WithMessage("กรุณาระบุฝ่าย/ภาคเขต");

        this.RuleFor(p => p.Type)
            .IsInEnum()
            .WithMessage("ประเภทแผนจัดซื้อจัดจ้างไม่ถูกต้อง");

        this.RuleFor(p => p.SupplyMethodCode)
            .NotEmpty()
            .WithMessage("กรุณาระบุวิธีการจัดหา");

        this.RuleFor(p => p.BudgetYear)
            .GreaterThan(0)
            .WithMessage("ปีงบประมาณจะต้องมากกว่าศูนย์");

        this.RuleFor(p => p.Name)
            .NotEmpty()
            .WithMessage("กรุณาระบุชื่อโครงการ");

        this.RuleFor(p => p.Budget)
            .GreaterThan(0)
            .WithMessage("วงเงินงบประมาณจะต้องมากกว่าศูนย์");

        this.RuleFor(p => p.Telephone)
            .MaximumLength(20)
            .WithMessage("รูปแบบเบอร์โทรศัพท์ไม่ถูกต้อง");

        this.RuleForEach(x => x.Acceptors)
            .ChildRules(acceptor =>
            {
                acceptor.RuleFor(a => a.AcceptorType)
                        .IsInEnum()
                        .WithMessage("Invalid acceptor type.");

                acceptor.RuleFor(a => a.UserId)
                        .NotEmpty()
                        .WithMessage("User ID is required.");

                acceptor.RuleFor(a => a.Sequence)
                        .GreaterThanOrEqualTo(0)
                        .WithMessage("Sequence must be a non-negative integer.");
            });

        this.RuleForEach(x => x.Attachments)
            .ChildRules(a => a.RuleForEach(x => x.FileAttachments)
                .ChildRules(f => f.RuleFor(x => x.FileName).MustBeValidFileExtension()))
            .When(x => x.Attachments is not null);
    }
}

public class CreatePlanEndpoint : PlanEndpointBase<CreatePlanRequest, Created<Guid>>
{
    private readonly Dp2DbContext dbContext;

    public CreatePlanEndpoint(
        ILogger<CreatePlanEndpoint> logger,
        Dp2DbContext dbContext,
        IFileServiceClient fileServiceClient)
        : base(logger, dbContext, fileServiceClient)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("Plan")
             .WithName("CreatePlan")
             .Produces<Ok>()
             .Produces<NotFound>()
             .Accepts<CreatePlanRequest>("application/json"));
        this.Post("plan");
        this.AuditLog("รายการจัดซื้อจัดจ้าง", "สร้างแผนจัดซื้อจัดจ้าง");
    }

    protected override async ValueTask<Created<Guid>> HandleRequestAsync(
        CreatePlanRequest req,
        CancellationToken ct)
    {
        await this.ValidateRequestAsync(req, ct);

        var planNumber = await this.GeneratePlanNumberAsync(req.BudgetYear, ct);
        var plan = CreatePlanFromRequest(req, planNumber);

        if (req is { Acceptors: not null })
        {
            await this.UpsertAcceptors(plan, req.Acceptors, plan.Status, ct);
        }

        if (req.Attachments != null)
        {
            await this.ValidateDocumentTypeCode(req.Attachments, ct);
            await this.UpsertAttachments(plan, req.Attachments);
        }

        this.dbContext.Plans.Add(plan);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Created(string.Empty, plan.Id.Value);
    }

    private async Task ValidateRequestAsync(CreatePlanRequest req, CancellationToken ct)
    {
        // Check if the department exists
        var department =
            await this.dbContext.RawBusinessUnits
                      .FirstOrDefaultAsync(
                          d => d.Id == BusinessUnitId.From(req.DepartmentCode), ct);

        if (department is null)
        {
            this.ThrowError(
                r => r.DepartmentCode,
                $"Department with code {req.DepartmentCode} not found.",
                StatusCodes.Status404NotFound);
        }

        // Validate supply method parameter
        var supplyMethod =
            await this.dbContext.SuParameters
                      .FirstOrDefaultAsync(
                          p => p.Code == ParameterCode.From(req.SupplyMethodCode), ct);

        if (supplyMethod is null)
        {
            this.ThrowError(
                r => r.SupplyMethodCode,
                $"Supply method with code {req.SupplyMethodCode} not found.",
                StatusCodes.Status404NotFound);
        }

        // Validate optional parameter codes
        if (req.SupplyMethodTypeCode is not null)
        {
            var supplyMethodType =
                await this.dbContext.SuParameters
                          .FirstOrDefaultAsync(p => p.Code == ParameterCode.From(req.SupplyMethodTypeCode), ct);

            if (supplyMethodType is null)
            {
                this.ThrowError(
                    r => r.SupplyMethodTypeCode,
                    $"Supply method type with code {req.SupplyMethodTypeCode} not found.",
                    StatusCodes.Status404NotFound);
            }
        }

        if (req.SupplyMethodSpecialTypeCode is not null)
        {
            var supplyMethodSpecialType =
                await this.dbContext.SuParameters
                          .FirstOrDefaultAsync(p => p.Code == ParameterCode.From(req.SupplyMethodSpecialTypeCode), ct);

            if (supplyMethodSpecialType is null)
            {
                this.ThrowError(
                    r => r.SupplyMethodSpecialTypeCode,
                    $"Supply method special type with code {req.SupplyMethodSpecialTypeCode} not found.",
                    StatusCodes.Status404NotFound);
            }
        }

        if (req.AssignSegmentCode is not null)
        {
            // Validate assign segment
            var assignSegment =
                await this.dbContext.SuParameters
                          .FirstOrDefaultAsync(p => p.Code == ParameterCode.From(req.AssignSegmentCode), ct);

            if (assignSegment is null)
            {
                this.ThrowError(
                    r => r.AssignSegmentCode,
                    $"Assign segment with code {req.AssignSegmentCode} not found.",
                    StatusCodes.Status404NotFound);
            }
        }
    }

    private async Task<PlanNumber> GeneratePlanNumberAsync(int budgetYear, CancellationToken ct)
    {
        var yearSuffix = (budgetYear % 100).ToString("D2");
        var planPrefix = $"DP{yearSuffix}";

        var plans = await this.dbContext.Plans
                                 .Where(p => p.BudgetYear == budgetYear)
                                 .IgnoreQueryFilters()
                                 .ToListAsync(ct);

        var lastPlan = plans
                      .Where(p => p.PlanNumber.Value.StartsWith(planPrefix))
                      .OrderByDescending(p => p.PlanNumber.Value)
                      .FirstOrDefault();

        return lastPlan is null
            ? PlanNumber.New(budgetYear)
            : lastPlan.PlanNumber.Next();
    }

    private static Plan CreatePlanFromRequest(CreatePlanRequest req, PlanNumber planNumber)
    {
        var plan = Plan.Create(req.Type, req.BudgetYear, planNumber)
                       .SetStatus(req.Status)
                       .SetDepartment(BusinessUnitId.From(req.DepartmentCode))
                       .SetSupplyMethod(ParameterCode.From(req.SupplyMethodCode))
                       .SetName(req.Name)
                       .SetBudget(req.Budget)
                       .SetExpectingProcurementAt(req.ExpectingProcurementAt)
                       .SetRemark(req.Remark)
                       .SetTelephone(req.Telephone)
                       .SetIsStock(req.IsStock)
                       .SetIsCommercialMaterial(req.IsCommercialMaterial);

        SetOptionalProperties(plan, req);

        return plan;
    }

    private static void SetOptionalProperties(Plan plan, CreatePlanRequest req)
    {
        if (req.SupplyMethodTypeCode is not null)
        {
            plan.SetSupplyMethodType(ParameterCode.From(req.SupplyMethodTypeCode));
        }

        if (req.SupplyMethodSpecialTypeCode is not null)
        {
            plan.SetSupplyMethodSpecialType(ParameterCode.From(req.SupplyMethodSpecialTypeCode));
        }

        if (req.GroupEgpNumber is not null)
        {
            plan.SetGroupEgpNumber(req.GroupEgpNumber);
        }

        if (req.EgpNumber is not null)
        {
            plan.SetEgpNumber(req.EgpNumber);
        }

        if (req.AssignSegmentCode is not null)
        {
            plan.SetAssignSegment(ParameterCode.From(req.AssignSegmentCode));
        }
    }
}
