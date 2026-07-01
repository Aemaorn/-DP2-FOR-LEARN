namespace GHB.DP2.Application.Features.Report.ContractCompletionByQuarter;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client.Abstractions;
using FluentValidation;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.Report.ContractCompletionByQuarter.Abstract;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Report.RpContractCompletionByQuarter;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record CreateReportContractCompletionByQuarterRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)] Guid UserId,
    RpContractCompletionByQuarterStatus Status,
    DateTimeOffset DocumentDate,
    int Year,
    int Quarter,
    DateTimeOffset SignStartDate,
    DateTimeOffset SignEndDate,
    IEnumerable<RpContractCompletionByQuarterDetailDto>? Detail,
    IEnumerable<AcceptorRequest>? Acceptors);

public class CreateReportContractCompletionByQuarterValidator : Validator<CreateReportContractCompletionByQuarterRequest>
{
    public CreateReportContractCompletionByQuarterValidator()
    {
        this.RuleFor(x => x.DocumentDate)
            .NotNull()
            .WithMessage("กรุณาระบุข้อมูลวันที่เอกสาร");

        this.RuleFor(x => x.Year)
            .NotNull()
            .WithMessage("กรุณาระบุข้อมูลปีงบประมาณ");

        this.RuleFor(x => x.Quarter)
            .NotNull()
            .WithMessage("กรุณาระบุข้อมูลไตรมาส");

        this.RuleFor(x => x.SignStartDate)
            .NotNull()
            .WithMessage("กรุณาระบุข้อมูลวันที่ลงนามในสัญญาเริ่มต้น");

        this.RuleFor(x => x.SignEndDate)
            .NotNull()
            .WithMessage("กรุณาระบุข้อมูลวันที่ลงนามในสัญญาสิ้นสุด");

        this.When(x => x.Status == RpContractCompletionByQuarterStatus.WaitingApproval, () =>
        {
            this.RuleFor(x => x.Acceptors)
                .NotNull().WithMessage("ต้องระบุผู้มีอำนาจเห็นชอบ/อนุมัติอย่างน้อย 1 คน")
                .Must(a => a != null && a.Any())
                .WithMessage("ต้องระบุผู้มีอำนาจเห็นชอบ/อนุมัติอย่างน้อย 1 คน");

            this.RuleFor(x => x.Detail)
                .NotNull().WithMessage("ต้องมีข้อมูลรายการสัญญาอย้างน้อย 1 รายการ")
                .Must(a => a != null && a.Any())
                .WithMessage("ต้องมีข้อมูลรายการสัญญาอย้างน้อย 1 รายการ");
        });
    }
}

public class CreateReportContractCompletionByQuarterEndpoint : ContractCompletionByQuarterEndpoint<CreateReportContractCompletionByQuarterRequest, Created<Guid>>
{
    private readonly Dp2DbContext dbContext;

    public CreateReportContractCompletionByQuarterEndpoint(ILogger<CreateReportContractCompletionByQuarterEndpoint> logger, Dp2DbContext dbContext, IFileServiceClient fileServiceClient)
        : base(logger, dbContext, fileServiceClient)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Post("report/contract-completion-by-quarter");
        this.Description(b => b
                              .WithTags("Report/ContractCompletionByQuarter")
                              .WithName("CreateReportContractCompletionByQuarter")
                              .WithSummary("สร้างรายงานการสรุปผลสัญญาตามไตรมาส")
                              .AllowAnonymous()
                              .Produces<Created<Guid>>(StatusCodes.Status201Created)
                              .ProducesProblem(StatusCodes.Status400BadRequest)
                              .ProducesProblem(StatusCodes.Status500InternalServerError));
    }

    private async Task<string> GenerateRunningNumberAsync(DateTimeOffset documentDate, CancellationToken ct)
    {
        var year = (documentDate.Year + 543) % 100;
        var yearStr = year.ToString("D2");
        var prefix = $"CAMR{yearStr}";

        var latestDoc = await this.dbContext.RpContractCompletionByQuarters
            .Where(x => x.DocumentNumber.StartsWith(prefix))
            .OrderByDescending(x => x.DocumentNumber)
            .FirstOrDefaultAsync(ct);

        int nextSeq = 1;
        if (latestDoc != null && latestDoc.DocumentNumber.Length >= prefix.Length + 5)
        {
            var seqStr = latestDoc.DocumentNumber.Substring(prefix.Length, 5);
            if (int.TryParse(seqStr, out var lastSeq))
            {
                nextSeq = lastSeq + 1;
            }
        }

        return $"CAMR{yearStr}{nextSeq:00000}";
    }

    protected override async ValueTask<Created<Guid>> HandleRequestAsync(CreateReportContractCompletionByQuarterRequest req, CancellationToken ct)
    {
        var runningNumber = await this.GenerateRunningNumberAsync(req.DocumentDate, ct);

        var entity = RpContractCompletionByQuarter.Create()
                                      .SetValues(
                                          runningNumber,
                                          req.Year,
                                          req.Quarter,
                                          req.DocumentDate,
                                          req.SignStartDate,
                                          req.SignEndDate);

        if (req.Acceptors != null)
        {
            this.UpsertAcceptors(entity, req.Acceptors, req.Status, UserId.From(req.UserId));
        }

        if (req.Detail != null)
        {
            this.UpsertDetails(entity, req.Detail);
        }

        entity.SetStatus(req.Status);

        await this.SetDefaultDocumentTemplate(entity, ct);

        this.dbContext.RpContractCompletionByQuarters.Add(entity);

        await this.dbContext.SaveChangesAsync(CancellationToken.None);

        // Re-fetch entity with full navigation properties for document replacement
        var reloaded = await this.GetById(entity.Id, CancellationToken.None);

        if (reloaded != null)
        {
            // Get latest version document (template from SetDefaultDocumentTemplate)
            var latestDocument = reloaded.DocumentHistories
                .OrderVersions()
                .FirstOrDefault();

            if (latestDocument != null)
            {
                var documentService = this.Resolve<IDocumentService>();

                var replaceDto = await this.MapToReplaceDtoAsync(
                    reloaded,
                    UserId.From(Guid.Empty),
                    hasCreator: false,
                    hasAcceptor: false,
                    CancellationToken.None);

                var parentDirectory =
                    $"{DocumentTemplateGroups.QuarterlyCompletion}/{reloaded.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt";

                var newFileId = await documentService.CopyDocumentTemplateAsync(
                    latestDocument.FileId,
                    contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto),
                    parentDirectory: parentDirectory,
                    cancellationToken: CancellationToken.None);

                if (newFileId != null)
                {
                    reloaded.AddDocumentHistory(
                        RpContractCompletionByQuarterDocumentType.Completion,
                        newFileId.Value);

                    await this.dbContext.SaveChangesAsync(CancellationToken.None);
                }
            }
        }

        return TypedResults.Created(string.Empty, entity.Id.Value);
    }
}