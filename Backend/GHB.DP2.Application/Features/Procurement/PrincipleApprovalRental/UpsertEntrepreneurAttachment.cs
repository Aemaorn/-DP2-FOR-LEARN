namespace GHB.DP2.Application.Features.Procurement.PrincipleApprovalRental;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client.Abstractions;
using FluentValidation;
using GHB.DP2.Application.Features.Procurement.Jp006.Dto;
using GHB.DP2.Application.Features.Procurement.PrincipleApprovalRental.Abstact;
using GHB.DP2.Application.Validators;
using GHB.DP2.Domain.Procurement.PPrincipleApprovalRental;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class UpsertEntrepreneurAttachment
{
    [FromClaim(JwtRegisteredClaimNames.Sub)]
    public Guid UserId { get; set; }

    public Guid PPrincipleApprovalRentalEntrepreneurId { get; init; }

    public EntrepreneurResponseAttachment[] Attachments { get; init; }
}

public class UpsertAttachmentsRequestValidator : Validator<UpsertEntrepreneurAttachment>
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

public class UpsertAttachmentsEndpoint : PrincipleApprovalRentalEndpointBase<UpsertEntrepreneurAttachment, Results<Ok, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public UpsertAttachmentsEndpoint(
        ILogger<UpsertAttachmentsEndpoint> logger,
        Dp2DbContext dbContext,
        IFileServiceClient fileServiceClient)
        : base(logger, dbContext, fileServiceClient)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Put("principle-approval-rental/entrepreneurs/{PPrincipleApprovalRentalEntrepreneurId:guid}/attachments");
        this.Description(b => b
                              .WithTags("Procurement/PrincipleApprovalRental")
                              .WithName("UpsertPrincipleApprovalRentalEntrepreneurAttachment")
                              .Produces<Created<Guid>>(StatusCodes.Status201Created)
                              .ProducesProblem(StatusCodes.Status400BadRequest)
                              .ProducesProblem(StatusCodes.Status500InternalServerError));
    }

    protected override async ValueTask<Results<Ok, NotFound<string>>> HandleRequestAsync(UpsertEntrepreneurAttachment req, CancellationToken ct)
    {
        await this.ValidateUserAsync(UserId.From(req.UserId), ct);
        await this.ValidateDocumentTypeCode(req.Attachments, ct);

        var contractInvitationEntrepreneur = await this.dbContext.PPrincipleApprovalRentalEntrepreneurs
                                                       .Include(a => a.Attachments)
                                                       .Include(a => a.PPrincipleApprovalRental)
                                                       .SingleOrDefaultAsync(w => w.Id == PPrincipleApprovalRentalEntrepreneursId.From(req.PPrincipleApprovalRentalEntrepreneurId), ct);

        if (contractInvitationEntrepreneur is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลรายการจัดซื้อจัดจ้าง");
        }

        await this.UpsertAttachments(contractInvitationEntrepreneur.PPrincipleApprovalRental, contractInvitationEntrepreneur, req.Attachments);

        this.dbContext.PPrincipleApprovalRentalEntrepreneurs.Update(contractInvitationEntrepreneur);
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