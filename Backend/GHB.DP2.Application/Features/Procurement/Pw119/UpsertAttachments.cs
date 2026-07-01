namespace GHB.DP2.Application.Features.Procurement.Pw119;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Contracts;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.Pw119.Abstract;
using GHB.DP2.Application.Validators;
using GHB.DP2.Domain.Procurement.Pw119;
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

    public Guid Pw119Id { get; init; }

    public AttachmentsDtoWithId[] Attachments { get; init; }
}

public class UpsertAttachmentsRequestValidator : Validator<UpsertAttachmentsRequest>
{
    public UpsertAttachmentsRequestValidator()
    {
        this.AddAttachmentsRules();
    }
}

public class UpsertAttachmentsEndpoint : Pw119EndpointBase<UpsertAttachmentsRequest, Results<Ok, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;
    private readonly IOperationService operationService;

    public UpsertAttachmentsEndpoint(
        ILogger<UpsertAttachmentsEndpoint> logger,
        Dp2DbContext dbContext,
        IFileServiceClient fileServiceClient,
        IOperationService operationService)
        : base(logger, dbContext, fileServiceClient, operationService)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags(nameof(Pw119))
             .WithName("UpsertPw119Attachment")
             .Produces<Ok>()
             .Produces<NotFound>()
             .Accepts<UpsertAttachmentsRequest>("application/json"));
        this.Put("Pw119/{Pw119Id:guid}/attachment");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>>> HandleRequestAsync(UpsertAttachmentsRequest req, CancellationToken ct)
    {
        await this.ValidateUserAsync(UserId.From(req.UserId), ct);
        await this.ValidateDocumentTypeCode(req.Attachments, ct);

        var pw119 = await this.dbContext.Pw119s
                              .Include(a => a.Attachments)
                              .SingleOrDefaultAsync(w => w.Id == Pw119Id.From(req.Pw119Id) && w.IsActive, ct);

        if (pw119 is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลรายการจัดซื้อจัดจ้าง");
        }

        await this.UpsertAttachments(pw119, req.Attachments);

        this.dbContext.Pw119s.Update(pw119);
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