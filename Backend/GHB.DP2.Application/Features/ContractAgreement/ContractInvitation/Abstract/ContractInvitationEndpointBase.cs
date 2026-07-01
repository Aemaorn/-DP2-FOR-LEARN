namespace GHB.DP2.Application.Features.ContractAgreement.ContractInvitation.Abstract;

using System.ComponentModel;
using System.Linq;
using Codehard.FileService.Client.Abstractions;
using Codehard.FileService.Contracts.ValueObjects;
using FluentValidation;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.Invite;
using GHB.DP2.Application.Features.Procurement.Invite.Abstract;
using GHB.DP2.Application.Features.Procurement.Jp006.Dto;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractInvitation;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PpTorDraft;
using GHB.DP2.Domain.Procurement.PPurchaseOrderApproval;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetContractInvitationVendorByIdResponse(
    [property: Description("รหัสหนังสือเชิญสัญญา")]
    ContractInvitationId? Id,
    [property: Description("รหัสการจัดซื้อ")]
    ProcurementId ProcurementId,
    [property: Description("สถานะหนังสือเชิญสัญญา")]
    ContractInvitationStatus Status,
    [property: Description("ผู้ขาย")] GetById.ContractInvitationVendorResponse Vendor,
    [property: Description("รายชื่อผู้อนุมัติ")]
    AcceptorResponse[] Acceptors,
    [property: Description("สิทธิ์ในการแก้ไข")]
    bool HasEditPermission = false);

public record ContractInvitationVendorDto(
    Guid? Id,
    Guid PurchaseOrderApprovalContractId,
    FileId? DocumentId,
    bool? IsDocumentIdReplace,
    string? Email,
    string? ContractName,
    string? PoNumber,
    string? ContractNumber,
    decimal? AgreedPrice,
    bool? HasContractGuarantee,
    decimal? ContractGuaranteePercent,
    decimal? GuaranteeAmount,
    string? ContractOfficerName,
    string? ContractOfficerPhone,
    string? ContractOfficerEmail,
    bool? EgpResult,
    string? EgpRemark,
    DateTimeOffset? EgpDate,
    bool? CoiResult,
    string? CoiRemark,
    DateTimeOffset? CoiDate,
    bool? WatchListResult,
    string? WatchListRemark,
    DateTimeOffset? WatchListDate,
    QualificationResultDto? CoiCheckerResult,
    QualificationResultDto? WatchlistCheckerResult,
    string? DocumentTemplateCode,
    IEnumerable<ShareholderDto>? Shareholder,
    EntrepreneurResponseAttachment[]? Attachments,
    DateTimeOffset? DocumentDate = null
);

public record ShareholderDto(
    Guid? CoiId,
    Guid? WatchlistId,
    int Sequence,
    string TaxId,
    string FirstName,
    string LastName,
    bool IsDirector,
    bool? IsShareholder,
    bool? IsJuristic,
    bool? WatchlistResult,
    string? WatchlistResultRemark,
    DateTimeOffset? WatchlistResultAt,
    bool? CoiResult,
    string? CoiResultRemark,
    DateTimeOffset? CoiResultAt,
    bool? EgpResult,
    string? EgpRemark,
    DateTimeOffset? EgpResultAt,
    QualificationResultDto? CoiCheckerResult,
    QualificationResultDto? WatchlistCheckerResult,
    string? CheckType = null
);

public class ContractInvitationVendorDtoValidator : Validator<ContractInvitationVendorDto>
{
    public ContractInvitationVendorDtoValidator()
    {
        this.RuleFor(x => x.Email)
            .NotEmpty().WithMessage("ต้องระบุอีเมล")
            .EmailAddress().WithMessage("รูปแบบอีเมลไม่ถูกต้อง");

        this.RuleFor(x => x.ContractName)
            .NotEmpty().WithMessage("ต้องระบุชื่อสัญญา");

        this.RuleFor(x => x.PoNumber)
            .NotEmpty().WithMessage("ต้องระบุเลขที่ใบสั่งซื้อ");

        this.RuleFor(x => x.ContractNumber)
            .NotEmpty().WithMessage("ต้องระบุเลขที่สัญญา");

        this.RuleFor(x => x.AgreedPrice)
            .GreaterThan(0).WithMessage("ราคาตกลงต้องมากกว่า 0");

        this.RuleFor(x => x.ContractOfficerName)
            .NotEmpty().WithMessage("ต้องระบุชื่อเจ้าหน้าที่ดูแลสัญญา");

        this.RuleFor(x => x.ContractOfficerPhone)
            .NotEmpty().WithMessage("กรุณาระบุเบอร์โทรศัพท์");

        this.When(x => x.HasContractGuarantee is true, () =>
        {
            this.RuleFor(x => x.ContractGuaranteePercent)
                .NotNull().WithMessage("ต้องระบุร้อยละของราคาค่าจ้างตามสัญญา")
                .GreaterThan(0).WithMessage("ร้อยละต้องมากกว่า 0");

            this.RuleFor(x => x.GuaranteeAmount)
                .NotNull().WithMessage("ต้องระบุจำนวนเงินหลักประกันสัญญา")
                .GreaterThan(0).WithMessage("จำนวนเงินต้องมากกว่า 0");
        });

        this.RuleFor(x => x.EgpDate)
            .NotEqual(default(DateTimeOffset)).WithMessage("ต้องระบุวันที่จากระบบ e-GP");
    }
}

public abstract partial class ContractInvitationEndpointBase<TRequest, TResponse>
    : TransactionalEndpointBase<TRequest, TResponse>
    where TResponse : IResult
    where TRequest : notnull
{
    private readonly Dp2DbContext dbContext;
    private readonly IOperationService operationService;
    private readonly IFileServiceClient fileServiceClient;

    protected ContractInvitationEndpointBase(
        Dp2DbContext dbContext,
        IOperationService operationService,
        IFileServiceClient fileServiceClient,
        ILogger logger)
        : base(logger, dbContext)
    {
        this.dbContext = dbContext;
        this.operationService = operationService;
        this.fileServiceClient = fileServiceClient;
    }

    protected async Task<CaContractInvitation> GetById(
        ContractInvitationId id,
        ProcurementId procurementId,
        CancellationToken ct)
    {
        var contractInvitationExisting =
            await this.dbContext.CaContractInvitations
                      .Include(caContractInvitation => caContractInvitation.Vendors)
                      .ThenInclude(invite => invite.PurchaseOrderApprovalContract)
                      .ThenInclude(p => p.Entrepreneur)
                      .ThenInclude(e => e!.SuVendor)
                      .Include(caContractInvitation => caContractInvitation.Vendors)
                      .ThenInclude(v => v.PurchaseOrderApprovalContract)
                      .ThenInclude(c => c.Budget)
                      .Include(caContractInvitation => caContractInvitation.Vendors)
                      .ThenInclude(v => v.PurchaseOrderApprovalContract)
                      .ThenInclude(c => c.PrincipleApprovalRentalBudget)
                      .Include(caContractInvitation => caContractInvitation.Vendors)
                      .ThenInclude(v => v.PurchaseOrderApprovalContract)
                      .ThenInclude(c => c.PpPurchaseRequisitionBudget)
                      .Include(caContractInvitation => caContractInvitation.Vendors)
                      .ThenInclude(v => v.PurchaseOrderApprovalContract)
                      .ThenInclude(c => c.PPurchaseOrderApprovalBudget)
                      .Include(caContractInvitation => caContractInvitation.Vendors)
                      .ThenInclude(caContractInvitationVendors => caContractInvitationVendors.DocumentHistories)
                      .Include(caContractInvitation => caContractInvitation.Vendors)
                      .ThenInclude(v => v.Shareholders)
                      .ThenInclude(s => s.VendorShareholderCheckers)
                      .Include(caContractInvitation => caContractInvitation.Vendors)
                      .ThenInclude(v => v.Checkers)
                      .Include(caContractInvitation => caContractInvitation.Vendors)
                      .ThenInclude(v => v.Attachments)
                      .Include(caContractInvitation => caContractInvitation.Vendors)
                      .ThenInclude(v => v.EmailAttachments)
                      .Include(caContractInvitation => caContractInvitation.Procurement)
                      .ThenInclude(p => p.PurchaseOrder)
                      .ThenInclude(po => po.Assignees)
                      .ThenInclude(po => po.User)
                      .ThenInclude(po => po.Employee)
                      .Include(po => po.Acceptors)
                      .ThenInclude(po => po.Delegatee)
                      .Include(po => po.Acceptors)
                      .ThenInclude(po => po.User)
                      .ThenInclude(po => po.Employee)
                      .AsSplitQuery()
                      .FirstOrDefaultAsync(
                          ci => ci.Id == id &&
                                ci.ProcurementId == procurementId,
                          ct);

        if (contractInvitationExisting is null)
        {
            this.ThrowError(
                r => id,
                $"ไม่พบหนังสือเชิญชวนทำสัญญาในระบบ",
                StatusCodes.Status404NotFound);
        }

        return contractInvitationExisting;
    }

    protected async Task<bool> HasEditPermission(
        ProcurementId procurementId,
        Guid userId,
        CancellationToken ct)
    {
        var purchaseOrderApproval =
            await this.dbContext.PPurchaseOrderApprovals
                      .Where(w => w.ProcurementId == procurementId)
                      .SelectMany(s => s.Assignees)
                      .Include(assigneeInfoEntity => assigneeInfoEntity.User)
                      .ToListAsync(ct);

        if (!purchaseOrderApproval.Any())
        {
            this.ThrowError("ไม่่มีสิทธิ์ในการจัดการหนังสือเชิญชวนทำสัญญา", StatusCodes.Status403Forbidden);
        }

        return purchaseOrderApproval
               .Select(DelegatorExtensions.DelegatorToAssignee)
               .Any(a => a.Delegatee?.SuUserId == null
                                        ? a.UserId == userId
                                        : a.Delegatee?.SuUserId == UserId.From(userId) &&
                                        !a.IsDeleted);
    }

    protected async Task<SuUser?> TryGetAssigneeWithEditPermissionAsync(
        ProcurementId procurementId,
        UserId userId,
        CancellationToken ct)
    {
        var purchaseOrderApproval =
            await this.dbContext.PPurchaseOrderApprovals
                      .Where(w => w.ProcurementId == procurementId)
                      .SelectMany(s => s.Assignees)
                      .Include(assigneeInfoEntity => assigneeInfoEntity.User)
                      .ThenInclude(a => a.Employee)
                      .ToListAsync(ct);

        if (!purchaseOrderApproval.Any())
        {
            this.ThrowError("ไม่่มีสิทธิ์ในการจัดการหนังสือเชิญชวนทำสัญญา", StatusCodes.Status403Forbidden);
        }

        return purchaseOrderApproval
               .Select(DelegatorExtensions.DelegatorToAssignee)
               .FirstOrDefault(a => a.Delegatee?.SuUserId == null
                                        ? a.UserId == userId
                                        : a.Delegatee?.SuUserId == userId &&
                                        !a.IsDeleted)
               ?.User;
    }

    protected void ValidateUsers(
        SuUser[] users,
        UserId[] userIds)
    {
        var foundUserIds =
            users.Select(u => u.Id)
                 .ToArray();

        var missingUserIds =
            userIds.Except(foundUserIds)
                   .ToArray();

        if (missingUserIds.Length > 0)
        {
            this.ThrowError(
                $"ไม่พบข้อมูลผู้ใช้งาน {string.Join(", ", missingUserIds)}.",
                StatusCodes.Status404NotFound);
        }
    }

    protected async Task<FileId> GetDocumentTemplateByCriteria(
    ParameterCode supplyMethodTypeCode,
    bool hasGuarantee,
    CancellationToken ct)
    {
        var documentService =
            this.Resolve<IDocumentService>();

        var fileId = await documentService.GetDocumentTemplateAsync(
            dt =>
                dt.Group == DocumentTemplateGroups.CAInv &&
                dt.IsActive &&
                dt.AdditionalInfo != null &&
                dt.AdditionalInfo.RootElement
                  .GetProperty(nameof(SuDocumentTemplate.SupplyMethodTypeCode))
                  .GetString() == supplyMethodTypeCode.ToString() &&
                (!hasGuarantee ||
                 dt.AdditionalInfo.RootElement
                   .GetProperty(nameof(SuDocumentTemplate.HasGuarantee))
                   .GetBoolean()),
            ct);

        return (FileId)fileId;
    }

    protected async Task SetDefaultDocumentTemplate(
        CaContractInvitation contractInvitation,
        CancellationToken ct)
    {
        foreach (var vendor in contractInvitation.Vendors.Where(v => v.DocumentTemplateCode is not null))
        {
            var docId =
                await this.GetDocumentTemplateByCriteria(
                    vendor.DocumentTemplateCode.Value,
                    vendor.HasContractGuarantee,
                    ct);

            var createContractInvitationHistory =
                CaContractInvitationVendorsDocumentHistory.Create(
                    CaContractInvitationDocumentType.ContractInvitation,
                    contractInvitation.Status,
                    "1.0",
                    docId);

            vendor.AddDocumentHistory(createContractInvitationHistory);
        }
    }

    protected async Task MapAndReplaceDocumentTemplate(
    CaContractInvitation contractInvitation,
    CancellationToken ct)
    {
        var documentService = this.Resolve<IDocumentService>();

        foreach (var vendor in contractInvitation.Vendors.Where(v => v.DocumentTemplateCode is not null))
        {
            var docId =
                await this.GetDocumentTemplateByCriteria(
                    vendor.DocumentTemplateCode.Value,
                    vendor.HasContractGuarantee,
                    ct);

            var replaceDto =
                await this.MapToInvitationVendorReplace(
                    contractInvitation.Id.Value,
                    vendor.Id.Value,
                    contractInvitation.ProcurementId.Value,
                    false,
                    ct);

            var parentDirectory =
                $"{DocumentTemplateGroups.CAInv}/{vendor.Id}_ResetFrom_{DateTime.UtcNow:yyyyMMddHHmmss}.odt";

            var newFileId = await documentService.CopyDocumentTemplateAsync(
                docId,
                contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto),
                parentDirectory: parentDirectory,
                cancellationToken: ct);

            if (newFileId is not null)
            {
                this.UpdateDocumentHistory(
                    CaContractInvitationDocumentType.ContractInvitation,
                    vendor,
                    ContractInvitationStatus.Draft,
                    newFileId.Value);
            }
        }
    }

    protected GetById.ContractInvitationVendorResponse[] MapContractByProcurementType(PPurchaseOrderApproval purchaseOrderApprovalExisting, SuUser? editableAssignee, ProcurementType procurementType, PpTorDraft? tor)
    {
        if (procurementType is ProcurementType.Procurement)
        {
            return [.. purchaseOrderApprovalExisting
                   .Contracts
                   .OrderBy(o => o.TorDraftBudgetId)
                   .ThenBy(c => c.Sequence)
                   .Select((c, index) =>
                   {
                       var shareholder =
                           c
                               .Entrepreneur?
                               .PurchaseOrderShareholders
                               .Where(s => !s.IsDeleted)
                               .DistinctBy(s => (s.Sequence, s.TaxId ?? string.Empty, s.CheckType ?? string.Empty))
                               .OrderBy(s => s.Sequence)
                               .Select(s =>
                               {
                                   return new GetById.ContractInviteShareholderDTO(
                                       s.Id.Value,
                                       s.Sequence,
                                       s.TaxId,
                                       s.FirstName,
                                       s.LastName,
                                       s.IsDirector,
                                       s.IsShareholder,
                                       s.IsJuristic,
                                       s.CheckType,
                                       null,
                                       null,
                                       null,
                                       null,
                                       null,
                                       null,
                                       null,
                                       null,
                                       null,
                                       null,
                                       null);
                               })
                               .ToArray();

                       var guaranteeAmount = (tor != null && tor.IsContractGuarantee == true) ? c.AgreedPrice * (tor?.PercentageContract / 100) : null;

                       var documentTemplateCode = purchaseOrderApprovalExisting.Procurement.SupplyMethodTypeCode?.Value;

                       if (purchaseOrderApprovalExisting.Procurement.SupplyMethodTypeCode?.Value is SupplyMethodTypeConstant.Hire or SupplyMethodTypeConstant.Buy)
                       {
                           documentTemplateCode = index == 0 ? purchaseOrderApprovalExisting.Procurement.SupplyMethodTypeCode?.Value : SupplyMethodTypeConstant.Hire;
                       }

                       return new GetById.ContractInvitationVendorResponse(
                           null,
                           c.Id,
                           null,
                           null,
                           [],
                           c.Entrepreneur is not null ? c.Entrepreneur!.SuVendor.EstablishmentName : c.PPurchaseOrderApprovalEntrepreneurs!.Vendor.EstablishmentName,
                           c.Entrepreneur is not null ? c.Entrepreneur.SuVendor.Email : c.PPurchaseOrderApprovalEntrepreneurs!.Vendor.Email,
                           purchaseOrderApprovalExisting.Procurement.Name,
                           c.PoNumber,
                           c.ContractNumber,
                           c.AgreedPrice,
                           (bool)(tor != null ? tor.IsContractGuarantee : false),
                           tor?.PercentageContract,
                           guaranteeAmount,
                           documentTemplateCode,
                           editableAssignee?.FullName ?? string.Empty,
                           editableAssignee?.OtherInfo?.Telephone2 ?? string.Empty,
                           editableAssignee?.Employee.Email ?? string.Empty,
                           null,
                           null,
                           null,
                           null,
                           null,
                           null,
                           null,
                           null,
                           null,
                           MapToEntrepreneurResponse(c.Entrepreneur is not null ? c.Entrepreneur!.SuVendor : c.PPurchaseOrderApprovalEntrepreneurs!.Vendor),
                           null,
                           null,
                           shareholder,
                           [],
                           c.Entrepreneur is not null ? c.Entrepreneur.SuVendor.Email : c.PPurchaseOrderApprovalEntrepreneurs!.Vendor.Email,
                           null,
                           [],
                           ResolveBudgetDescription(procurementType, c));
                   })];
        }

        return [.. purchaseOrderApprovalExisting
               .Contracts
               .OrderBy(o => o.PrincipleApprovalRentalBudgetId)
               .ThenBy(c => c.Sequence)
               .Select((c, index) =>
               {
                   var shareholder =
                       c
                           .PrincipleApprovalRentalEntrepreneurs?
                           .EntrepreneursShareholders
                           .Where(s => !s.IsDeleted)
                           .DistinctBy(s => (s.Sequence, s.TaxId ?? string.Empty, s.CheckType ?? string.Empty))
                           .OrderBy(s => s.Sequence)
                           .Select(s =>
                           {
                               return new GetById.ContractInviteShareholderDTO(
                                   s.Id.Value,
                                   s.Sequence,
                                   s.TaxId,
                                   s.FirstName,
                                   s.LastName,
                                   s.IsDirector,
                                   s.IsShareholder,
                                   s.IsJuristic,
                                   s.CheckType,
                                   null,
                                   null,
                                   null,
                                   null,
                                   null,
                                   null,
                                   null,
                                   null,
                                   null,
                                   null,
                                   null);
                           })
                           .ToArray();

                   return new GetById.ContractInvitationVendorResponse(
                       null,
                       c.Id,
                       null,
                       null,
                       [],
                       c.PrincipleApprovalRentalEntrepreneurs!.Vendor.EstablishmentName,
                       c.PrincipleApprovalRentalEntrepreneurs.Vendor.Email,
                       purchaseOrderApprovalExisting.Procurement.Name,
                       c.PoNumber,
                       c.ContractNumber,
                       c.AgreedPrice,
                       false,
                       null,
                       null,
                       purchaseOrderApprovalExisting.Procurement.SupplyMethodTypeCode?.Value,
                       editableAssignee?.FullName ?? string.Empty,
                       editableAssignee?.OtherInfo?.Telephone2 ?? string.Empty,
                       editableAssignee?.Employee.Email ?? string.Empty,
                       null,
                       null,
                       null,
                       null,
                       null,
                       null,
                       null,
                       null,
                       null,
                       MapToEntrepreneurResponse(c.PrincipleApprovalRentalEntrepreneurs.Vendor),
                       null,
                       null,
                       shareholder,
                       [],
                       c.PrincipleApprovalRentalEntrepreneurs.Vendor.Email,
                       null,
                       [],
                       ResolveBudgetDescription(procurementType, c));
               })];
    }

    protected static GetById.ContractInvitationVendorResponse MapToInvitationVendorResponse(ProcurementType type, CaContractInvitationVendors ciVendor)
    {
        var vendor = type switch
        {
            ProcurementType.Procurement =>
                ciVendor.PurchaseOrderApprovalContract.PPurchaseOrderApprovalEntrepreneurs != null
                    ? ciVendor.PurchaseOrderApprovalContract.PPurchaseOrderApprovalEntrepreneurs?.Vendor
                    : ciVendor.PurchaseOrderApprovalContract.Entrepreneur?.SuVendor,
            ProcurementType.Rent =>
                ciVendor.PurchaseOrderApprovalContract
                    .PrincipleApprovalRentalEntrepreneurs?.Vendor,
            _ => null,
        };

        var documentId = ciVendor.DocumentHistories
                                 .OrderVersions()
                                 .FirstOrDefault();

        var isReplacedDoc = ciVendor.DocumentHistories.Any(d => d.IsReplaced);

        var documentVersions = ciVendor.DocumentHistories
                                       .OrderVersions()
                                       .Select((d, index) => new GetById.DocumentVersionResponse(
                                           d.FileId.Value,
                                           d.Version,
                                           d.CreatedAt,
                                           d.CreatedByName ?? string.Empty,
                                           index == 0))
                                       .ToArray();

        var shareholder =
            ciVendor
                .Shareholders
                .Where(s => !s.IsDeleted)
                .DistinctBy(s => (s.Sequence, s.TaxId ?? string.Empty, s.CheckType ?? string.Empty))
                .OrderBy(s => s.Sequence)
                .Select(s =>
                {
                    var coiChecker = s.VendorShareholderCheckers
                                      .OrderByDescending(c => c.ResultAt)
                                      .FirstOrDefault(c => c.CheckType == QualificationType.COI);

                    var watchlistChecker = s.VendorShareholderCheckers
                                            .OrderByDescending(c => c.ResultAt)
                                            .FirstOrDefault(c => c.CheckType == QualificationType.Watchlist);

                    var coiCheckerResult = coiChecker is null
                        ? null
                        : new QualificationResultDto(
                            coiChecker.Result,
                            coiChecker.ResultAt,
                            coiChecker.Remark);

                    var watchlistCheckerResult = watchlistChecker is null
                        ? null
                        : new QualificationResultDto(
                            watchlistChecker.Result,
                            watchlistChecker.ResultAt,
                            watchlistChecker.Remark);

                    return new GetById.ContractInviteShareholderDTO(
                        s.Id.Value,
                        s.Sequence,
                        s.TaxId,
                        s.FirstName,
                        s.LastName,
                        s.IsDirector,
                        s.IsShareholder,
                        s.IsJuristic,
                        s.CheckType,
                        s.WatchlistResult,
                        s.WatchlistResultRemark,
                        s.WatchlistResultAt,
                        s.CoiResult,
                        s.CoiResultRemark,
                        s.CoiResultAt,
                        s.EgpResult,
                        s.EgpRemark,
                        s.EgpResultAt,
                        coiCheckerResult,
                        watchlistCheckerResult);
                })
                .ToArray();

        var coiChecker =
            ciVendor.Checkers
                    .OrderByDescending(c => c.ResultAt)
                    .FirstOrDefault(c => c.CheckType == QualificationType.COI);

        var watchlistChecker =
            ciVendor.Checkers
                    .OrderByDescending(c => c.ResultAt)
                    .FirstOrDefault(c => c.CheckType == QualificationType.Watchlist);

        var coiCheckerResult = coiChecker is null
            ? null
            : new QualificationResultDto(
                coiChecker.Result,
                coiChecker.ResultAt,
                coiChecker.Remark);

        var watchlistCheckerResult = watchlistChecker is null
            ? null
            : new QualificationResultDto(
                watchlistChecker.Result,
                watchlistChecker.ResultAt,
                watchlistChecker.Remark);

        return
            new GetById.ContractInvitationVendorResponse(
                ciVendor.Id,
                ciVendor.PurchaseOrderApprovalContractId,
                documentId?.FileId.Value,
                isReplacedDoc,
                documentVersions,
                vendor != null ? vendor.EstablishmentName : string.Empty,
                ciVendor.Email,
                ciVendor.ContractName,
                ciVendor.PoNumber,
                ciVendor.ContractNumber,
                ciVendor.AgreedPrice,
                ciVendor.HasContractGuarantee,
                ciVendor.ContractGuaranteePercent,
                ciVendor.GuaranteeAmount,
                ciVendor.DocumentTemplateCode?.Value ?? ciVendor.ContractInvitation?.Procurement?.SupplyMethodTypeCode?.Value,
                ciVendor.ContractOfficerName,
                ciVendor.ContractOfficerPhone,
                ciVendor.ContractOfficerEmail,
                ciVendor.EgpResult,
                ciVendor.EgpRemark,
                ciVendor.EgpDate,
                ciVendor.CoiResult,
                ciVendor.CoiRemark,
                ciVendor.CoiDate,
                ciVendor.WatchlistResult,
                ciVendor.WatchlistRemark,
                ciVendor.WatchlistDate,
                vendor != null ? MapToEntrepreneurResponse(vendor) : null,
                coiCheckerResult,
                watchlistCheckerResult,
                shareholder,
                [.. ciVendor.Attachments
                        .OrderBy(o => o.Sequence)
                        .GroupBy(
                            a => a.DocumentTypeCode,
                            (key, g) => new EntrepreneurResponseAttachment(
                                key.Value,
                                [.. g.Select(s =>
                                     new EntrepreneurFileWithId(
                                         s.Id.Value,
                                         s.FileId.Value,
                                         s.FileName,
                                         s.Sequence,
                                         s.IsPublic,
                                         s.AuditInfo.CreatedBy,
                                         s.Type))]))],
                ciVendor.EmailSend,
                ciVendor.EmailTemplate,
                [.. ciVendor.EmailAttachments
                        .OrderBy(o => o.Sequence)
                        .Select(e => new EmailAttachment(
                            e.Id.Value,
                            e.FileName,
                            e.FileId,
                            e.Sequence))],
                ResolveBudgetDescription(type, ciVendor.PurchaseOrderApprovalContract),
                ciVendor.DocumentDate);
    }

    protected static string? ResolveBudgetDescription(ProcurementType type, PPurchaseOrderApprovalContract contract)
    {
        if (type == ProcurementType.Rent && contract.PrincipleApprovalRentalBudget is not null)
        {
            return contract.PrincipleApprovalRentalBudget.Description;
        }

        if (type == ProcurementType.Procurement && contract.Budget is not null)
        {
            return contract.Budget.Description;
        }

        if (contract.PpPurchaseRequisitionBudget is not null)
        {
            return contract.PpPurchaseRequisitionBudget.Description;
        }

        if (contract.PPurchaseOrderApprovalBudget is not null)
        {
            return contract.PPurchaseOrderApprovalBudget.Description;
        }

        return null;
    }

    protected static GetById.VendorInfoResponse MapToEntrepreneurResponse(SuVendor vendor)
    {
        return
            new GetById.VendorInfoResponse(
                vendor.Id,
                vendor.Nationality,
                vendor.Type,
                vendor.EntrepreneurType,
                vendor.EntrepreneurTypeInfo.Label,
                vendor.TaxpayerIdentificationNo,
                vendor.EstablishmentName,
                vendor.Tel,
                vendor.Fax,
                vendor.SapVendorNumber,
                vendor.SapBranchNumber,
                vendor.Email);
    }

    protected static AcceptorResponse[] MapAcceptors(CaContractInvitationAcceptor[] acceptors)
    {
        return
            [.. acceptors
                .Map(DelegatorExtensions.DelegatorToAcceptor)
                .OrderBy(o => o.Sequence)
                .Select(acceptor =>
                    new AcceptorResponse(
                        acceptor.Id.Value,
                        acceptor.Type,
                        acceptor.UserId.Value,
                        acceptor.Sequence,
                        acceptor.FullName,
                        acceptor.PositionName,
                        acceptor.BusinessUnitName,
                        acceptor.Status,
                        acceptor.Remark,
                        acceptor.ActionAt,
                        DelegateId: acceptor.DelegateeId?.Value,
                        IsCurrent: acceptor.IsCurrentApprover(),
                        DelegateeUserId: acceptor.Delegatee?.SuUserId.Value))];
    }

    protected async Task UpsertAttachments(CaContractInvitation parent, CaContractInvitationVendors entity, EntrepreneurResponseAttachment[] attachments)
    {
        var fileList = attachments
                       .SelectMany(r => r.FileAttachments.Select(f => new
                       {
                           f.Id,
                           r.DocumentTypeCode,
                           f.FileId,
                           f.FileName,
                           f.Sequence,
                           f.IsPublic,
                           f.Type,
                       }))
                       .ToArray();

        var incomingFileIds = fileList.Select(f => FileId.From(f.FileId)).ToHashSet();
        var existingFileIds = entity.Attachments.Select(a => a.FileId).ToHashSet();

        var removedAttachments = entity.Attachments
                                       .Where(a => !incomingFileIds.Contains(a.FileId))
                                       .ToArray();

        foreach (var attachment in removedAttachments)
        {
            entity.RemoveAttachment(attachment);
            await this.fileServiceClient.DeleteAsync(attachment.FileId, CancellationToken.None);
        }

        if (removedAttachments.Length > 0)
        {
            parent.AddActivity(new ActivityInfo(
                ActivityLogActionTypeConstant.DeleteFile,
                ActivityLogActionTypeConstant.DeleteFile,
                nameof(parent.Status),
                string.Join(", ", removedAttachments.Select(a => a.FileName))));
        }

        var newFiles = fileList.Where(f => !existingFileIds.Contains(FileId.From(f.FileId))).ToArray();

        newFiles.Map(f => CaContractInvitationVendorAttachments.Create(
                    ParameterCode.From(f.DocumentTypeCode),
                    FileId.From(f.FileId),
                    f.FileName,
                    f.Type,
                    f.Sequence,
                    f.IsPublic))
                .Iter(r => entity.AddAttachment(r));

        if (newFiles.Length > 0)
        {
            parent.AddActivity(new ActivityInfo(
                ActivityLogActionTypeConstant.UploadFile,
                ActivityLogActionTypeConstant.UploadFile,
                nameof(parent.Status),
                string.Join(", ", newFiles.Select(f => f.FileName))));
        }

        foreach (var existing in entity.Attachments)
        {
            var match = fileList.FirstOrDefault(f => FileId.From(f.FileId) == existing.FileId);

            if (match != null)
            {
                existing.SetIsPublic(match.IsPublic)
                        .SetSequence(match.Sequence)
                        .SetDocumentType(ParameterCode.From(match.DocumentTypeCode));
            }
        }
    }

    protected async Task ValidateDocumentTypeCode(EntrepreneurResponseAttachment[] attachments, CancellationToken ct)
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