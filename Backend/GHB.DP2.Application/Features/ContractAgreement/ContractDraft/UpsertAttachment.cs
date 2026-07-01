namespace GHB.DP2.Application.Features.ContractAgreement.ContractDraft;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client.Abstractions;
using FluentValidation;
using GHB.DP2.Application.Features.ContractAgreement.ContractDraft.Abstract;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement;
using GHB.DP2.Application.Features.Procurement.Jp006.Dto;
using GHB.DP2.Application.Validators;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
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
    public Guid UserId { get; set; }

    public Guid ContractDraftVendorsId { get; init; }

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

public class UpsertAttachmentsEndpoint : ContractDraftEndpointBase<UpsertAttachmentsRequest, Results<Ok, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public UpsertAttachmentsEndpoint(
        IOperationService operationService,
        ICommandTextService commandTextService,
        ILogger<GetVendorEndpoint> logger,
        IFileServiceClient fileServiceClient,
        Dp2DbContext dbContext)
        : base(logger, dbContext, operationService, commandTextService, fileServiceClient)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Put("contractDraft/vendor/{ContractDraftVendorsId}/attachments");
        this.Description(b => b
                              .WithTags(nameof(ContractDraft))
                              .WithName("UpsertContractDraftAttachments")
                              .Produces<GetVendorResponse>()
                              .Produces<string>(StatusCodes.Status404NotFound)
                              .WithSummary("Upsert ContractDraft Attachments"));
    }

    protected override async ValueTask<Results<Ok, NotFound<string>>> HandleRequestAsync(UpsertAttachmentsRequest req, CancellationToken ct)
    {
        await this.ValidateUserAsync(UserId.From(req.UserId), ct);
        await this.ValidateDocumentTypeCode(req.Attachments, ct);

        var contractInvitationEntrepreneur = await this.dbContext.CaContractDraftVendors
                                                       .Include(a => a.Attachments)
                                                       .SingleOrDefaultAsync(w => w.Id == ContractDraftVendorId.From(req.ContractDraftVendorsId), ct);

        if (contractInvitationEntrepreneur is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลรายการจัดซื้อจัดจ้าง");
        }

        await this.UpsertAttachments(contractInvitationEntrepreneur, req.Attachments);

        this.dbContext.CaContractDraftVendors.Update(contractInvitationEntrepreneur);
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

    private async Task ValidateDocumentTypeCode(EntrepreneurResponseAttachment[] attachments, CancellationToken ct)
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