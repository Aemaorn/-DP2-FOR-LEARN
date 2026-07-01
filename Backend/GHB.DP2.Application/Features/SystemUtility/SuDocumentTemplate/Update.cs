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

public class UpdateSuDocumentTemplateRequest
{
    public Guid Id { get; init; }

    [FromForm]
    public UpdateSuDocumentTemplateRequestDto Request { get; init; }
}

public record UpdateSuDocumentTemplateRequestDto(
    string Group,
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

public class UpdateSuDocumentTemplateValidation : Validator<UpdateSuDocumentTemplateRequest>
{
    public UpdateSuDocumentTemplateValidation()
    {
        this.RuleFor(x => x.Request.Group)
            .NotNull()
            .NotEmpty()
            .WithMessage("Group is required");

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

        // Document templates are .odt files consumed by OnlyOffice/ChEditor — they are
        // intentionally outside the general attachment whitelist.
        this.RuleFor(x => x.Request)
            .MustBeValidTemplateFile(isRequired: false);
    }
}

public class UpdateSuDocumentTemplate : SecureEndpointBase<UpdateSuDocumentTemplateRequest, Results<Ok, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;
    private readonly IFileServiceClient fileServiceClient;

    public UpdateSuDocumentTemplate(
        Dp2DbContext dbContext,
        IFileServiceClient fileServiceClient,
        IPermissionValidationService permissionValidationService,
        ILogger<UpdateSuDocumentTemplate> logger)
        : base(permissionValidationService, logger)
    {
        this.dbContext = dbContext;
        this.fileServiceClient = fileServiceClient;
    }

    public override void Configure()
    {
        this.Description(x => x.Accepts<UpdateSuDocumentTemplateRequest>("multipart/form-data"));
        this.Options(x => x.WithTags("SuDocumentTemplate"));
        this.Put("/st/st007/{Id:guid}");
        this.AuditLog("จัดการรูปแบบเอกสาร", "แก้ไขรูปแบบเอกสาร");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>>> HandleRequestAsync(UpdateSuDocumentTemplateRequest req, CancellationToken ct)
    {
        var data = await this.dbContext.SuDocumentTemplates
                             .FirstOrDefaultAsync(x => x.Id == SuDocumentTemplateId.From(req.Id), ct);

        if (data is null)
        {
            return TypedResults.NotFound($"SuDocumentTemplate with Id {req.Id} not found");
        }

        await this.ValidateParameterAsync(req.Request, ct);

        data.Update(req.Request.Group, req.Request.Name, req.Request.IsActive);

        if (req.Request.File is not null)
        {
            var contents = await req.Request.File.ReadFileAsync(CancellationToken.None);

            var uploadFile = await this.fileServiceClient.UploadFileAsync(contents, req.Request.File.FileName, cancellationToken: CancellationToken.None);

            data.UpdatePreviewPfdFile(uploadFile.Id, uploadFile.FileName);
        }

        data.UpdateFileId(FileId.From(req.Request.FileId));

        data
            .SetSupplyMethodCode(
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

        this.dbContext.SuDocumentTemplates.Update(data);
        await this.dbContext.SaveChangesAsync(CancellationToken.None);

        return TypedResults.Ok();
    }

    private async Task ValidateParameterAsync(UpdateSuDocumentTemplateRequestDto request, CancellationToken ct)
    {
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
                    $"Supply method with code {request.SupplyMethodCode} not found",
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
                    $"Contract template with code {request.ContractTemplateCode} not found",
                    StatusCodes.Status404NotFound);
            }
        }
    }
}