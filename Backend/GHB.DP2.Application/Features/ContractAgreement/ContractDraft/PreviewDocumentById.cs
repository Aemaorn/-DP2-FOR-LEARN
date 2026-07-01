namespace GHB.DP2.Application.Features.ContractAgreement.ContractDraft;

using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Application.Features.ContractAgreement.ContractDraft.Abstract;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;

public record PreviewContractDraftDocumentRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid ProcurementId,
    Guid ContractDraftId,
    Guid VendorId,
    CaContractDraftVendorDocumentType DocumentType);

public class PreviewContractDraftDocumentEndpoint : ContractDraftEndpointBase<PreviewContractDraftDocumentRequest, Results<Ok<Guid>, NotFound<string>>>
{
    private readonly IOperationService operationService;
    private readonly ICommandTextService commandTextService;
    private readonly IFileServiceClient fileServiceClient;
    private readonly Dp2DbContext dbContext;

    public PreviewContractDraftDocumentEndpoint(
        Dp2DbContext dbContext,
        IOperationService operationService,
        ICommandTextService commandTextService,
        IFileServiceClient fileServiceClient,
        ILogger<PreviewContractDraftDocumentEndpoint> logger)
        : base(logger, dbContext, operationService, commandTextService, fileServiceClient)
    {
        this.operationService = operationService;
        this.commandTextService = commandTextService;
        this.fileServiceClient = fileServiceClient;
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("ContractDraft")
             .WithName("ContractDraftPreviewDocument")
             .Produces<Ok>()
             .Produces<NotFound>());
        this.Get("procurement/{ProcurementId:guid}/ContractDraft/{ContractDraftId:guid}/vendor/{VendorId:guid}/review-document");
    }

    protected override async ValueTask<Results<Ok<Guid>, NotFound<string>>> HandleRequestAsync(PreviewContractDraftDocumentRequest req, CancellationToken ct)
    {
        var contractDraft =
            await this.QueryContractDraftsAsync(
                req.ProcurementId,
                req.ContractDraftId,
                ct);

        var vendor = await this.dbContext
                               .CaContractDrafts
                               .Where(c =>
                                   c.ProcurementId == ProcurementId.From(req.ProcurementId) &&
                                   c.Id == ContractDraftId.From(req.ContractDraftId))
                               .SelectMany(c => c.Vendors)
                               .Include(caContractDraftVendor => caContractDraftVendor.DocumentHistories)
                               .Include(caContractDraftVendor => caContractDraftVendor.Acceptors)
                               .ThenInclude(acceptor => acceptor.Delegatee)
                               .ThenInclude(delegatee => delegatee!.SuUser)
                               .FirstOrDefaultAsync(
                                   v => v.Id == ContractDraftVendorId.From(req.VendorId),
                                   ct);

        var purchaseOrder = await this.dbContext
                               .PPurchaseOrder
                               .FirstOrDefaultAsync(
                                   v => v.ProcurementId == ProcurementId.From(req.ProcurementId),
                                   ct);

        var principleApprovals = await this.dbContext
                               .PPrincipleApprovals
                               .FirstOrDefaultAsync(
                                   v => v.ProcurementId == ProcurementId.From(req.ProcurementId),
                                   ct);

        if (vendor is null)
        {
            return TypedResults.NotFound("ไม่พบผู้ขายที่ระบุ");
        }

        var managers =
            await this.operationService.GetDefaultAcceptorPositionAsync(
                SectionProcessType.ContractDraft,
                req.UserId,
                vendor.Budget,
                contractDraft.Procurement.SupplyMethodCode.Value,
                contractDraft.Procurement.SupplyMethodCode.Value is SupplyMethodConstant.Eighty ? default : (string?)contractDraft.Procurement.SupplyMethodSpecialTypeCode,
                ct);

        var commandNumber = managers.FirstOrDefault()?.CommandNumber;

        var commandText = this.commandTextService.GetCommandText(CommandTextProgram.MedianPrice, managers, contractDraft.Procurement.SupplyMethodCode, vendor.Budget, supplyMethodSpecialType: contractDraft.Procurement.SupplyMethodSpecialTypeCode, supplyMethodSpecialName: contractDraft.Procurement.SupplyMethodSpecialType?.Label, commandNumber: commandNumber);

        var location = await (
            from p in this.dbContext.RawProvinces
            where p.Code == vendor.Vendor.RawProvinceCode
            from d in this.dbContext.RawDistricts
                .Where(d => d.Code == vendor.Vendor.RawDistrictCode)
            from sd in this.dbContext.RawSubDistricts
                .Where(sd => sd.Code == vendor.Vendor.RawSubDistrictCode)
            select new LocationDto(
                p.NameTh,
                d.NameTh,
                sd.NameTh)
        ).FirstOrDefaultAsync(ct);

        var response = GetVendorReplaceDto.FromEntity(vendor, commandText, purchaseOrder, null, false, false, location, principleApprovals);

        var getLastedDraftDocumentHistory = vendor.DocumentHistories
                                                  .Where(d => d.DocumentType == req.DocumentType)
                                                  .Where(d => d.StatusState == ContractDraftVendorStatus.Draft)
                                                  .OrderByDescending(d => d.Version)
                                                  .FirstOrDefault();

        if (getLastedDraftDocumentHistory is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลแผนที่ร่าง");
        }

        var file = await this.fileServiceClient.DownloadAsync(getLastedDraftDocumentHistory.FileId, cancellationToken: ct);

        if (file == null)
        {
            return TypedResults.NotFound("ไม่พบไฟล์แผนที่ร่าง");
        }

        var fileContent = OdtDocumentExtensions.ReplaceOdtDocument(file.Contents, response);

        var odt = DocumentService.DetectContentType(fileContent);
        var unixTimeOneDay = DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds();
        var fileResult = await this.fileServiceClient.UploadFileAsync(
            fileContent,
            contentType: odt,
            expirationUnixSeconds: unixTimeOneDay,
            cancellationToken: ct);

        return TypedResults.Ok(fileResult.Id.Value);
    }
}