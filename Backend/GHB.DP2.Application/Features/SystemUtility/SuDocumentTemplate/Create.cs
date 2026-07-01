namespace GHB.DP2.Application.Features.SystemUtility.SuDocumentTemplate;

using Codehard.FileService.Client.Abstractions;
using Codehard.FileService.Contracts.ValueObjects;
using FluentValidation;
using GHB.DP2.Application.Contracts;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Services;
using GHB.DP2.Application.Validators;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class CreateSuDocumentTemplateRequest
{
    [FromForm]
    public CreateSuDocumentTemplateRequestDto Request { get; init; }
}

public record CreateSuDocumentTemplateRequestDto(
    string Group,
    string Code,
    string Name,
    IFormFile? File,
    Guid FileId,
    bool IsActive,
    string? SupplyMethodCode,
    decimal? BudgetMin,
    decimal? BudgetMax,
    bool? IsCancel,
    bool? IsChange,
    bool? IsJorPorComment,
    bool? IsFine,
    string? ContractTemplateCode,
    bool? IsWinnerAnnounced,
    bool? IsEvaluationReport,
    bool? IsAppointmentOrdered,
    bool? IsApproval,
    bool? IsInYear,
    bool? IsPublished,
    string? SupplyMethodTypeCode,
    bool? HasGuarantee,
    bool? IsConfidential,
    string? PrincipleApprovalTemplateCode,
    string? ContractAmendmentDocumentType) : IHasFile;

public record CreateSuDocumentTemplateResponse(Guid Id);

public class CreateSuDocumentTemplateValidation : Validator<CreateSuDocumentTemplateRequest>
{
    public CreateSuDocumentTemplateValidation()
    {
        this.RuleFor(x => x.Request)
            .NotNull()
            .NotEmpty()
            .WithMessage("SuDocumentTemplate are required");

        this.RuleFor(x => x.Request.Group)
            .NotNull()
            .NotEmpty()
            .WithMessage("Group is required");

        this.RuleFor(x => x.Request.Code)
            .NotNull()
            .NotEmpty()
            .WithMessage("Code is required");

        this.RuleFor(x => x.Request.Name)
            .NotNull()
            .NotEmpty()
            .WithMessage("Name is required");

        this.RuleFor(x => x.Request.IsActive)
            .NotNull()
            .WithMessage("IsActive is required");

        this.RuleFor(x => x.Request.FileId)
            .NotNull()
            .WithMessage("FileId is required");

        this.RuleFor(x => x.Request)
            .MustBeValidPdfFile();
    }
}

public class CreateSuDocumentTemplate :
    SecureEndpointBase<CreateSuDocumentTemplateRequest,
                       Results<Ok<CreateSuDocumentTemplateResponse>, NotFound<string>, Conflict<string>>>
{
    private readonly Dp2DbContext dbContext;
    private readonly IFileServiceClient fileServiceClient;

    public CreateSuDocumentTemplate(
        Dp2DbContext dbContext,
        IFileServiceClient fileServiceClient,
        IPermissionValidationService permissionValidationService,
        ILogger<CreateSuDocumentTemplate> logger)
        : base(permissionValidationService, logger)
    {
        this.dbContext = dbContext;
        this.fileServiceClient = fileServiceClient;
    }

    public override void Configure()
    {
        this.Description(x => x.Accepts<CreateSuDocumentTemplateRequest>("multipart/form-data"));
        this.Options(x => x.WithTags("SuDocumentTemplate"));
        this.Post("/st/st007");
        this.AuditLog("จัดการรูปแบบเอกสาร", "สร้างรูปแบบเอกสาร");
    }

    protected override async ValueTask<Results<Ok<CreateSuDocumentTemplateResponse>, NotFound<string>, Conflict<string>>> HandleRequestAsync(CreateSuDocumentTemplateRequest req, CancellationToken ct)
    {
        await this.ValidateParameterAsync(req.Request, ct);

        var createModel = SuDocumentTemplate.Create(req.Request.Group, req.Request.Code, req.Request.Name, req.Request.IsActive);

        if (req.Request.File is not null)
        {
            var contents = await req.Request.File.ReadFileAsync(CancellationToken.None);

            var uploadFile = await this.fileServiceClient.UploadFileAsync(contents, req.Request.File.FileName, cancellationToken: CancellationToken.None);

            createModel.UpdatePreviewPfdFile(uploadFile.Id, uploadFile.FileName);
        }

        createModel.UpdateFileId(FileId.From(req.Request.FileId));

        createModel.SetSupplyMethodCode(
                       req.Request.SupplyMethodCode is null ? null : ParameterCode.From(req.Request.SupplyMethodCode))
                   .SetBudgetForDocument(
                       req.Request.BudgetMin ?? decimal.Zero,
                       req.Request.BudgetMax)
                   .SetIsCancel(req.Request.IsCancel)
                   .SetIsChange(req.Request.IsChange)
                   .SetIsJorPorComment(req.Request.IsJorPorComment)
                   .SetIsFine(req.Request.IsFine)
                   .SetContractTemplateCode(req.Request.ContractTemplateCode)
                   .SetIsWinnerAnnouncement(req.Request.IsWinnerAnnounced)
                   .SetIsEvaluationReport(req.Request.IsEvaluationReport)
                   .SetIsAppointmentOrdered(req.Request.IsAppointmentOrdered)
                   .SetIsApproval(req.Request.IsApproval)
                   .SetIsInYear(req.Request.IsInYear)
                   .SetIsPublished(req.Request.IsPublished)
                   .SetSupplyMethodTypeCode(req.Request.SupplyMethodTypeCode)
                   .SetHasGuarantee(req.Request.HasGuarantee)
                   .SetIsConfidential(req.Request.IsConfidential)
                   .SetPrincipleApprovalTemplateCode(req.Request.PrincipleApprovalTemplateCode)
                   .SetContractAmendmentDocumentType(req.Request.ContractAmendmentDocumentType);

        this.dbContext.SuDocumentTemplates.Add(createModel);
        await this.dbContext.SaveChangesAsync(CancellationToken.None);

        var response = new CreateSuDocumentTemplateResponse(createModel.Id.Value);

        return TypedResults.Ok(response);
    }

    private async Task ValidateParameterAsync(CreateSuDocumentTemplateRequestDto request, CancellationToken ct)
    {
        var isCodeExist = await this.dbContext.SuDocumentTemplates
                                    .AnyAsync(x => x.Code == request.Code, ct);

        if (isCodeExist)
        {
            this.ThrowError(
                "รหัสซ้ำ",
                StatusCodes.Status409Conflict);
        }

        if (!string.IsNullOrWhiteSpace(request.SupplyMethodCode))
        {
            var supplyMethodExists =
                await this.dbContext.SuParameters
                          .AnyAsync(
                              p => p.Code == ParameterCode.From(request.SupplyMethodCode),
                              ct);

            if (!supplyMethodExists)
            {
                this.ThrowError(
                    $"ไม่พบวิธีการจัดหา",
                    StatusCodes.Status404NotFound);
            }
        }

        if (!string.IsNullOrWhiteSpace(request.ContractTemplateCode))
        {
            var contractTemplateExists =
                await this.dbContext.SuParameters
                          .AnyAsync(
                              p => p.Code == ParameterCode.From(request.ContractTemplateCode),
                              ct);

            if (!contractTemplateExists)
            {
                this.ThrowError(
                    $"ไม่พบข้อมูล",
                    StatusCodes.Status404NotFound);
            }
        }
    }
}