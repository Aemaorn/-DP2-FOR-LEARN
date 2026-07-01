namespace GHB.DP2.Application.Features.Procurement.Pw184;

using System.IdentityModel.Tokens.Jwt;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Features.Procurement.Pw184.Abstract;
using GHB.DP2.Domain.Procurement.Pw184;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record UpsertPw184AttachmentsRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid Pw184Id,
    AttachmentsDtoWithId[] Attachments);

public class UpsertPw184AttachmentsEndpoint : Pw184EndpointBase<UpsertPw184AttachmentsRequest, Results<Ok, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public UpsertPw184AttachmentsEndpoint(ILogger<UpsertPw184AttachmentsEndpoint> logger, Dp2DbContext dbContext)
        : base(logger, dbContext)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("Pw184")
             .WithName("UpsertPw184Attachments")
             .Produces<Ok>()
             .Produces<NotFound>()
             .Accepts<UpsertPw184AttachmentsRequest>("application/json"));
        this.Put("pw184/{Pw184Id:guid}/attachment");
        this.AuditLog("รายการ ว 184", "แนบไฟล์ ว 184");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>>> HandleRequestAsync(
        UpsertPw184AttachmentsRequest req,
        CancellationToken ct)
    {
        await this.ValidateDocumentTypeCodesAsync(req.Attachments, ct);

        var entity = await this.dbContext.Pw184s
                               .Include(p => p.Attachments)
                               .SingleOrDefaultAsync(p => p.Id == Pw184Id.From(req.Pw184Id) && p.IsActive, ct);

        if (entity is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลรายการ ว 184");
        }

        await this.UpsertAttachments(entity, req.Attachments);

        this.dbContext.Pw184s.Update(entity);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private async Task ValidateDocumentTypeCodesAsync(AttachmentsDtoWithId[] attachments, CancellationToken ct)
    {
        var docTypeCodes = attachments
                           .Select(a => a.DocumentTypeCode)
                           .Where(c => !string.IsNullOrWhiteSpace(c))
                           .Select(ParameterCode.From)
                           .ToArray();

        if (docTypeCodes.Length == 0)
        {
            return;
        }

        var found = await this.dbContext.SuParameters
                              .Where(p => docTypeCodes.Contains(p.Code))
                              .Select(p => p.Code)
                              .ToArrayAsync(ct);

        var missing = docTypeCodes.Except(found).ToArray();

        if (missing.Length > 0)
        {
            this.ThrowError("ไม่พบประเภทไฟล์ที่ระบุ", StatusCodes.Status404NotFound);
        }
    }
}
