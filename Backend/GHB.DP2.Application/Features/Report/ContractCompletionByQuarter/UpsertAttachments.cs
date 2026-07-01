namespace GHB.DP2.Application.Features.Report.ContractCompletionByQuarter;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Contracts;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Features.Report.ContractCompletionByQuarter.Abstract;
using GHB.DP2.Application.Validators;
using GHB.DP2.Domain.Report.RpContractCompletionByQuarter;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class UpsertAttachmentsRequest : IHasAttachmentsWithId
{
    [FromClaim(JwtRegisteredClaimNames.Sub)]
    public Guid UserId { get; init; }

    public Guid ContractCompletionByQuarterId { get; init; }

    public AttachmentsDtoWithId[] Attachments { get; init; }
}

public class UpsertAttachmentsRequestValidator : Validator<UpsertAttachmentsRequest>
{
    public UpsertAttachmentsRequestValidator()
    {
        this.AddAttachmentsRules();
    }
}

public class UpsertAttachmentsEndpoint : ContractCompletionByQuarterEndpoint<UpsertAttachmentsRequest, Results<Ok, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public UpsertAttachmentsEndpoint(ILogger<UpdateReportContractCompletionByQuarterEndpoint> logger, Dp2DbContext dbContext, IFileServiceClient fileServiceClient)
        : base(logger, dbContext, fileServiceClient)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Put("report/contract-completion-by-quarter/{contractCompletionByQuarterId:guid}/attachment");
        this.Description(b => b
                              .WithTags("Report/ContractCompletionByQuarter")
                              .WithName("UpdateReportContractCompletionByQuarterAttachment")
                              .WithSummary("แก้ไขรายงานการสรุปผลสัญญาตามไตรมาส")
                              .AllowAnonymous()
                              .Produces(StatusCodes.Status200OK)
                              .Produces<string>(StatusCodes.Status404NotFound));
    }

    protected override async ValueTask<Results<Ok, NotFound<string>>> HandleRequestAsync(UpsertAttachmentsRequest req, CancellationToken ct)
    {
        await this.ValidateUserAsync(UserId.From(req.UserId), ct);
        await this.ValidateDocumentTypeCode(req.Attachments, ct);

        var contractQuarters = await this.dbContext.RpContractCompletionByQuarters
                                        .Include(a => a.Attachments)
                                        .SingleOrDefaultAsync(w => w.Id == RpContractCompletionByQuarterId.From(req.ContractCompletionByQuarterId), ct);

        if (contractQuarters is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลรายการจัดซื้อจัดจ้าง");
        }

        await this.UpsertAttachments(contractQuarters, req.Attachments);

        this.dbContext.RpContractCompletionByQuarters.Update(contractQuarters);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private async Task ValidateUserAsync(UserId userId, CancellationToken ct)
    {
        var user = await this.dbContext
                             .SuUsers
                             .SingleOrDefaultAsync(w => w.Id == userId && w.IsActive, ct);

        if (user is null)
        {
            this.ThrowError("ไม่พบผู้ใช้งานในระบบ", StatusCodes.Status404NotFound);
        }
    }

    private async Task ValidateDocumentTypeCode(AttachmentsDtoWithId[] attachments, CancellationToken ct)
    {
        var docTypeCodes = attachments.Select(s => s.DocumentTypeCode)
                                      .Where(w => !string.IsNullOrWhiteSpace(w))
                                      .Select(ParameterCode.From)
                                      .ToArray();

        var docType = await this.dbContext.SuParameters
                                .Where(x => docTypeCodes.Contains(x.Code))
                                .ToArrayAsync(ct);

        var missingDocumentTypes = docTypeCodes
                                   .Except(docType.Select(dt => dt.Code))
                                   .ToArray();

        if (missingDocumentTypes.Any())
        {
            this.ThrowError(
                $"ไม่พบประเภทไฟล์",
                StatusCodes.Status404NotFound);
        }
    }
}