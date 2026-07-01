namespace GHB.DP2.Application.Features.Procurement.Procurement;

using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Threading;
using Codehard.FileService.Contracts.ValueObjects;
using FluentValidation;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Validators;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Plan;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class CreateProcurementRequest
{
    [FromClaim(JwtRegisteredClaimNames.Sub)]
    public Guid UserId { get; init; }

    public PlanId? PlanId { get; set; }

    public ProcurementType ProcurementType { get; set; }

    public ProcurementStep ProcurementStep { get; set; }

    public string DepartmentCode { get; set; } = string.Empty;

    public string SupplyMethodCode { get; set; } = string.Empty;

    public string? SupplyMethodTypeCode { get; set; }

    public string? SupplyMethodSpecialTypeCode { get; set; }

    public string PlanName { get; set; } = string.Empty;

    public decimal Budget { get; set; }

    public int BudgetYear { get; set; }

    public DateTimeOffset? ExpectingProcurementAt { get; set; }

    public bool IsStock { get; set; }

    public bool IsCommercialMaterial { get; set; }

    public ProcurementStatus Status { get; init; }

    public IEnumerable<ProcurementAttachmentsDto> Attachments { get; set; }
}

public record ProcurementAttachmentsDto(
    string DocumentTypeCode,
    int Sequence,
    string? Remark,
    IEnumerable<ProcurementFileAttachments> FileAttachments);

public record ProcurementFileAttachments(
    Guid FileId,
    string FileName,
    int Sequence,
    bool IsPublic);

public class CreatePlanRequestValidator : Validator<CreateProcurementRequest>
{
    public CreatePlanRequestValidator()
    {
        this.RuleFor(p => p.ProcurementType)
            .IsInEnum()
            .WithMessage("Procurement type is invalid.");

        this.RuleFor(r => r.Status)
            .IsInEnum()
            .WithMessage("สถานะอยู่นอกเหนือที่มีอยู่");

        this.RuleFor(p => p.ProcurementStep)
            .NotNull()
            .WithMessage("Procurement step is required.");

        this.RuleFor(p => p.DepartmentCode)
            .NotEmpty()
            .WithMessage("Department code is required.");

        this.RuleFor(p => p.SupplyMethodCode)
            .NotEmpty()
            .WithMessage("Supply method code is required.");

        this.RuleFor(p => p.PlanName)
            .NotEmpty()
            .WithMessage("Name is required.");
        this.When(p => p.ProcurementType == ProcurementType.Procurement, () =>
        {
            this.RuleFor(p => p.Budget)
                .GreaterThan(0)
                .WithMessage("Budget must be greater than 0.");

            this.RuleFor(p => p.BudgetYear)
                .GreaterThan(0)
                .WithMessage("Budget year must be greater than 0.");
        });

        this.RuleForEach(x => x.Attachments)
            .ChildRules(a => a.RuleForEach(x => x.FileAttachments)
                .ChildRules(f => f.RuleFor(x => x.FileName).MustBeValidFileExtension()));
    }
}

public class CreateProcurementEndpoint : EndpointBase<CreateProcurementRequest, Created<Guid>>
{
    private readonly Dp2DbContext dbContext;
    private readonly IOperationService operationService;

    public CreateProcurementEndpoint(ILogger<CreateProcurementEndpoint> logger, Dp2DbContext dbContext, IOperationService operationService)
        : base(logger)
    {
        this.dbContext = dbContext;
        this.operationService = operationService;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("Procurement")
             .WithName("CreateProcurement")
             .Produces<Ok>()
             .Produces<NotFound>()
             .Accepts<CreateProcurementRequest>("application/json"));
        this.Post("procurement");
    }

    protected override async ValueTask<Created<Guid>> HandleRequestAsync(
        CreateProcurementRequest req,
        CancellationToken ct)
    {
        await this.ValidateRequestAsync(req, ct);

        var procurementNumber = await this.GenerateProcurementNumberAsync(req.ProcurementType, ct);

        if (procurementNumber is null)
        {
            this.ThrowError("เกิดข้อผิดพลาด การสร้างรหัสจัดซื้อจัดจ้าง.", StatusCodes.Status500InternalServerError);
        }

        var procurement = await this.CreateProcurementFromRequest(req, procurementNumber.Value, ct);

        AddAttachments(procurement, req.Attachments);

        this.dbContext.Procurements.Add(procurement);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Created(string.Empty, procurement.Id.Value);
    }

    private async Task ValidateRequestAsync(CreateProcurementRequest req, CancellationToken ct)
    {
        // Check if the department exists
        var department = await this.dbContext.RawBusinessUnits
                                   .FirstOrDefaultAsync(d => d.Id == BusinessUnitId.From(req.DepartmentCode), ct);

        if (department is null)
        {
            this.ThrowError(
                r => r.DepartmentCode,
                $"ไม่พบหน่วยงานที่มีรหัส {req.DepartmentCode}");
        }

        // Validate supply method parameter
        var supplyMethod = await this.dbContext.SuParameters
                                     .FirstOrDefaultAsync(p => p.Code == ParameterCode.From(req.SupplyMethodCode), ct);

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
            var supplyMethodType = await this.dbContext.SuParameters
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
            var supplyMethodSpecialType = await this.dbContext.SuParameters
                                                    .FirstOrDefaultAsync(p => p.Code == ParameterCode.From(req.SupplyMethodSpecialTypeCode), ct);

            if (supplyMethodSpecialType is null)
            {
                this.ThrowError(
                    r => r.SupplyMethodSpecialTypeCode,
                    $"Supply method special type with code {req.SupplyMethodSpecialTypeCode} not found.",
                    StatusCodes.Status404NotFound);
            }
        }
    }

    private async Task<Procurement> CreateProcurementFromRequest(CreateProcurementRequest req, ProcurementNumber procurementNumber, CancellationToken ct)
    {
        var managers = await this.operationService.GetDefaultAcceptorPositionAsync(
            SectionProcessType.TOR,
            req.UserId,
            req.Budget,
            req.SupplyMethodCode,
            req.SupplyMethodCode is SupplyMethodConstant.Eighty ? default : req.SupplyMethodSpecialTypeCode,
            ct);

        var hasMd = managers.Any(m => m.InRefCode == InRefCodeConstant.Bp002);

        var procurement = Procurement.Create(req.PlanId, hasMd)
                                     .SetProcurementNumber(procurementNumber)
                                     .SetProcurementInfo(
                                         req.ProcurementType,
                                         req.ProcurementStep,
                                         BusinessUnitId.From(req.DepartmentCode),
                                         req.PlanName,
                                         req.Budget,
                                         req.BudgetYear,
                                         req.ExpectingProcurementAt)
                                     .SetProcurementStep(
                                         req.ProcurementType,
                                         req.ProcurementStep)
                                     .SetSupplyMethod(
                                         ParameterCode.From(req.SupplyMethodCode),
                                         req.SupplyMethodTypeCode is not null ? ParameterCode.From(req.SupplyMethodTypeCode) : null,
                                         req.SupplyMethodSpecialTypeCode is not null ? ParameterCode.From(req.SupplyMethodSpecialTypeCode) : null)
                                     .SetMaterialType(
                                         req.IsStock,
                                         req.IsCommercialMaterial)
                                     .SetStatus(req.Status);

        if (req.ProcurementType == ProcurementType.Rent)
        {
            procurement.SetProcessType(ProcessType.PrincipleApproval);
        }

        return procurement;
    }

    private async Task<ProcurementNumber?> GenerateProcurementNumberAsync(ProcurementType procurementType, CancellationToken ct)
    {
        var yearString = DateTimeOffset.UtcNow.ToString("yy", new CultureInfo("th-TH"));
        var prefix = procurementType switch
        {
            ProcurementType.Procurement => $"{RunningPrefixConstant.Procurement}{yearString}",
            ProcurementType.Rent => $"{RunningPrefixConstant.Rental}{yearString}",
            _ => throw new InvalidOperationException("Invalid procurement type for generating procurement number."),
        };

        var lastProcurement = await this.dbContext.Procurements
                                        .Where(p =>
                                            p.ProcurementNumber != null &&
                                            ((string)p.ProcurementNumber).StartsWith(prefix))
                                        .IgnoreQueryFilters()
                                        .OrderByDescending(p => p.ProcurementNumber)
                                        .FirstOrDefaultAsync(ct);

        if (lastProcurement is null)
        {
            return ProcurementNumber.New(procurementType);
        }

        if (lastProcurement.ProcurementNumber is null)
        {
            return ProcurementNumber.New(procurementType);
        }

        return lastProcurement.ProcurementNumber?.Next(procurementType);
    }

    private static void AddAttachments(
        Procurement procurement,
        IEnumerable<ProcurementAttachmentsDto> attachments)
    {
        if (attachments == null || !attachments.Any())
        {
            return;
        }

        foreach (var attachment in attachments)
        {
            var attachmentEntity = ProcurementAttachment.Create(
                procurement.Id,
                attachment.Sequence,
                ParameterCode.From(attachment.DocumentTypeCode),
                attachment.Remark);

            foreach (var info in attachment.FileAttachments)
            {
                attachmentEntity.AddAttachmentInfos(
                    ProcurementAttachmentInfo.Create(
                        attachmentEntity.Id,
                        info.Sequence,
                        FileId.From(info.FileId),
                        info.FileName,
                        info.IsPublic));
            }

            procurement.AddAttachment(attachmentEntity);
        }
    }
}