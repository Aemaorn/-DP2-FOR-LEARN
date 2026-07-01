namespace GHB.DP2.Application.Features.Procurement.P79Clause2;

using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Contracts;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.P79Clause2.Abstract;
using GHB.DP2.Application.Validators;
using GHB.DP2.Domain.Procurement.P79Clause2;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;

public class UpsertAttachmentsRequest : IHasAttachmentsWithId
{
    [FromClaim(JwtRegisteredClaimNames.Sub)]
    public Guid UserId { get; init; }

    public Guid P79Clause2Id { get; init; }

    public AttachmentsDtoWithId[] Attachments { get; init; }
}

public class UpsertAttachmentsRequestValidator : Validator<UpsertAttachmentsRequest>
{
    public UpsertAttachmentsRequestValidator()
    {
        this.AddAttachmentsRules();
    }
}

public class UpsertAttachmentsEndpoint : P79Clause2EndpointBase<UpsertAttachmentsRequest, Results<Ok, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;
    private readonly IOperationService operationService;

    public UpsertAttachmentsEndpoint(
        ILogger<UpsertAttachmentsEndpoint> logger,
        Dp2DbContext dbContext,
        IOperationService operationService,
        IFileServiceClient fileServiceClient)
        : base(logger, dbContext, operationService, fileServiceClient)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags(nameof(P79Clause2))
             .WithName("UpsertP79Clause2Attachment")
             .Produces<Ok>()
             .Produces<NotFound>()
             .Accepts<UpsertAttachmentsRequest>("application/json"));
        this.Put("P79Clause2/{P79Clause2Id:guid}/attachment");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>>> HandleRequestAsync(UpsertAttachmentsRequest req, CancellationToken ct)
    {
        await this.ValidateUserAsync(UserId.From(req.UserId), ct);
        await this.ValidateDocumentTypeCode(req.Attachments, ct);

        var P79Clause2 = await this.dbContext.P79Clause2s
                             .Include(a => a.Attachments)
                             .SingleOrDefaultAsync(w => w.Id == P79Clause2Id.From(req.P79Clause2Id) && w.IsActive, ct);

        if (P79Clause2 is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลรายการจัดซื้อจัดจ้าง");
        }

        await this.UpsertAttachments(P79Clause2, req.Attachments);

        this.dbContext.P79Clause2s.Update(P79Clause2);
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