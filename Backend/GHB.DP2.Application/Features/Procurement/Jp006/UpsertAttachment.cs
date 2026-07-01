namespace GHB.DP2.Application.Features.Procurement.Jp006;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client.Abstractions;
using FluentValidation;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.Jp006.Abstract;
using GHB.DP2.Application.Features.Procurement.Jp006.Dto;
using GHB.DP2.Application.Validators;
using GHB.DP2.Domain.Procurement.PPurchaseOrder;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class UpsertAttachmentsRequest
{
    [FromClaim(JwtRegisteredClaimNames.Sub)]
    public Guid UserId { get; init; }

    public Guid PurchaseOrderEntrepreneurId { get; init; }

    public EntrepreneurResponseAttachment[] Attachments { get; init; }
}

public class UpsertAttachmentsRequestValidator : Validator<UpsertAttachmentsRequest>
{
    public UpsertAttachmentsRequestValidator()
    {
        this.RuleFor(r => r.Attachments)
            .NotNull()
            .WithMessage("ต้องระบุไฟล์แนบอย่างน้อย 1 ไฟล์");

        this.RuleForEach(x => x.Attachments)
            .ChildRules(attachment =>
            {
                attachment.RuleFor(x => x.DocumentTypeCode)
                          .NotEmpty()
                          .WithMessage("ต้องระบุประเภทเอกสาร");

                attachment
                    .RuleForEach(x => x.FileAttachments)
                    .ChildRules(file =>
                    {
                        file.RuleFor(x => x.Type)
                            .NotNull()
                            .WithMessage("ต้องระบุประเภทการตรวจสอบ");

                        file.AddSequenceFileAttachmentRule();
                    });
            });
    }
}

public class UpsertAttachmentsEndpoint : Jp006EndpointBase<UpsertAttachmentsRequest, Results<Ok, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public UpsertAttachmentsEndpoint(
        ILogger<AssigneePurchaseOrderEndpoint> logger,
        IOperationService operationService,
        ICommandTextService commandTextService,
        IFileServiceClient fileServiceClient,
        Dp2DbContext dbContext)
        : base(logger, operationService, commandTextService, fileServiceClient, dbContext)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags(nameof(Jp006))
             .WithName("UpsertJp006Attachment")
             .Produces<Ok>()
             .Produces<NotFound>()
             .Accepts<UpsertAttachmentsRequest>("application/json"));
        this.Put("jp006/{PurchaseOrderEntrepreneurId:guid}/attachment");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>>> HandleRequestAsync(UpsertAttachmentsRequest req, CancellationToken ct)
    {
        await this.ValidateUserAsync(UserId.From(req.UserId), ct);
        await this.ValidateDocumentTypeCode(req.Attachments, ct);

        var purchaseOrderEntrepreneur = await this.dbContext.PJp006Entrepreneurs
                                                  .Include(a => a.Attachments)
                                                  .SingleOrDefaultAsync(w => w.Id == PurchaseOrderEntrepreneurId.From(req.PurchaseOrderEntrepreneurId), ct);

        if (purchaseOrderEntrepreneur is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลรายการจัดซื้อจัดจ้าง");
        }

        await this.UpsertAttachments(purchaseOrderEntrepreneur, req.Attachments);

        this.dbContext.PJp006Entrepreneurs.Update(purchaseOrderEntrepreneur);
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

    private new async Task ValidateDocumentTypeCode(EntrepreneurResponseAttachment[] attachments, CancellationToken ct)
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