namespace GHB.DP2.Application.Features.ContractAgreement.ContractInvitation;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client.Abstractions;
using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Features.ContractAgreement.ContractInvitation.Abstract;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractInvitation;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record RestoreVersionRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid ProcurementId,
    Guid ContractInvitationId,
    Guid VendorId,
    Guid SourceFileId);

public record RestoreVersionResponse(
    Guid FileId,
    string Version);

public class RestoreVersionEndpoint
    : ContractInvitationEndpointBase<RestoreVersionRequest, Results<Ok<RestoreVersionResponse>, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public RestoreVersionEndpoint(
        Dp2DbContext dbContext,
        IOperationService operationService,
        IFileServiceClient fileServiceClient,
        ILogger<RestoreVersionEndpoint> logger)
        : base(dbContext, operationService, fileServiceClient, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b => b.WithTags("ContractAgreement/ContractInvitation"));
        this.Post("procurement/{ProcurementId:guid}/contract-invitation/{ContractInvitationId:guid}/vendor/{VendorId:guid}/restore-version/{SourceFileId:guid}");
    }

    protected override async ValueTask<Results<Ok<RestoreVersionResponse>, NotFound<string>, BadRequest<string>>> HandleRequestAsync(
        RestoreVersionRequest req,
        CancellationToken ct)
    {
        // 1. Get ContractInvitation
        var contractInvitation = await this.GetById(
            ContractInvitationId.From(req.ContractInvitationId),
            ProcurementId.From(req.ProcurementId),
            ct);

        // 2. Get Vendor
        var vendor = contractInvitation.Vendors
            .FirstOrDefault(v => v.Id == ContractInvitationVendorsId.From(req.VendorId));

        if (vendor is null)
        {
            this.ThrowError(
                $"ไม่พบข้อมูลผู้ขายที่มีรหัส {req.VendorId}",
                StatusCodes.Status404NotFound);
        }

        // 3. Validate SourceFileId exists in document history
        var sourceHistory = vendor.DocumentHistories
            .FirstOrDefault(h => h.FileId == FileId.From(req.SourceFileId));

        if (sourceHistory is null)
        {
            this.ThrowError(
                $"ไม่พบเอกสาร version ที่ต้องการดึง",
                StatusCodes.Status404NotFound);
        }

        // 4. Copy document using DocumentService
        var documentService = this.Resolve<IDocumentService>();

        var parentDirectory =
            $"{DocumentTemplateGroups.PlanAnnouncement}/{vendor.Id}_RestoreFrom_{sourceHistory.Version}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt";

        var newFileId = await documentService.CopyDocumentTemplateAsync(
            FileId.From(req.SourceFileId),
            parentDirectory: parentDirectory,
            cancellationToken: ct);

        if (newFileId is null)
        {
            this.ThrowError(
                DocumentErrorMessages.CopyDocumentFailed,
                StatusCodes.Status500InternalServerError);
        }

        // 5. Calculate new version
        var lastHistory = vendor.DocumentHistories
            .OrderVersions()
            .FirstOrDefault();
        var incrementMajor =

        lastHistory?.StatusState != contractInvitation.Status;

        var newVersion =
            vendor.DocumentHistories
                .NextVersion(incrementMajor);

        // 6. Create new document history entry
        var newHistory = CaContractInvitationVendorsDocumentHistory.Create(
            CaContractInvitationDocumentType.ContractInvitation,
            contractInvitation.Status,
            newVersion,
            newFileId.Value,
            isReplace: true);

        vendor.AddDocumentHistory(newHistory);

        // 7. Save changes
        this.dbContext.CaContractInvitations.Update(contractInvitation);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok(new RestoreVersionResponse(
            newFileId.Value.Value,
            newVersion));
    }
}
