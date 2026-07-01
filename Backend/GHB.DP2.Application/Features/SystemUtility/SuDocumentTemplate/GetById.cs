namespace GHB.DP2.Application.Features.SystemUtility.SuDocumentTemplate;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Services;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class GetSuDocumentTemplateByIdRequest
{
    public Guid Id { get; init; }
}

public record GetSuDocumentTemplateByIdResponse(
    Guid Id,
    string Group,
    string Code,
    string Name,
    FileResponse PreviewPdfFile,
    FileResponse File,
    bool IsActive,
    string SupplyMethodCode,
    decimal? BudgetMin,
    decimal? BudgetMax,
    bool? IsCancel,
    bool? IsChange,
    bool? IsJorPorComment,
    bool? IsFine,
    string? ContractTemplateType,
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
    string? ContractAmendmentDocumentType);

public record FileResponse(FileId Id, string? FileName);

public class GetSuDocumentTemplateById :
    SecureEndpointBase<GetSuDocumentTemplateByIdRequest,
                       Results<Ok<GetSuDocumentTemplateByIdResponse>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public GetSuDocumentTemplateById(
        Dp2DbContext dbContext,
        IPermissionValidationService permissionValidationService,
        ILogger<GetSuDocumentTemplateById> logger)
        : base(permissionValidationService, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("SuDocumentTemplate"));
        this.Get("/st/st007/{Id:guid}");
    }

    protected override async ValueTask<Results<Ok<GetSuDocumentTemplateByIdResponse>, NotFound<string>>> HandleRequestAsync(GetSuDocumentTemplateByIdRequest req, CancellationToken ct)
    {
        var data = await this.dbContext.SuDocumentTemplates
                             .Include(suDocumentTemplate => suDocumentTemplate.BudgetForDocument)
                             .FirstOrDefaultAsync(x => x.Id == SuDocumentTemplateId.From(req.Id), ct);

        if (data is null)
        {
            return TypedResults.NotFound($"SuDocumentTemplate with Id {req.Id} not found");
        }

        var contractTemplateType =
            await Optional(data.ContractTemplateCode)
                  .Map(ParameterCode.From)
                  .MatchUnsafeAsync(
                      Some: async code =>
                      {
                          var contractTemplate =
                              await this.dbContext.SuParameters
                                        .Include(p => p.Parent)
                                        .AsNoTracking()
                                        .FirstOrDefaultAsync(p => p.Code == code, CancellationToken.None);

                          return (string?)contractTemplate?.Parent?.Code ?? string.Empty;
                      },
                      None: () => Task.FromResult<string?>(null));

        var response = new GetSuDocumentTemplateByIdResponse(
            data.Id.Value,
            data.Group,
            data.Code,
            data.Name,
            new FileResponse(data.PreviewPfdFileId, data.PreviewPfdFileName),
            new FileResponse(data.FileId, string.Empty),
            data.IsActive,
            data.SupplyMethodCode?.Value ?? string.Empty,
            data.BudgetForDocument.Min,
            data.BudgetForDocument.Max,
            data.IsCancel,
            data.IsChange,
            data.IsJorPorComment,
            data.IsFine,
            contractTemplateType,
            data.ContractTemplateCode,
            data.IsWinnerAnnouncement,
            data.IsEvaluationReport,
            data.IsAppointmentOrdered,
            data.IsApproval,
            data.IsInYear,
            data.IsPublished,
            data.SupplyMethodTypeCode,
            data.HasGuarantee,
            data.IsConfidential,
            data.PrincipleApprovalTemplateCode,
            data.ContractAmendmentDocumentType);

        return TypedResults.Ok(response);
    }
}