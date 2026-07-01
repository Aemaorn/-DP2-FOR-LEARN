namespace GHB.DP2.Application.Features.ContractAgreement.ContractDraft;

using System.ComponentModel;
using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.ContractAgreement.ContractDraft.Abstract;
using GHB.DP2.Application.Features.ContractAgreement.ContractDraft.Dto;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement;
using GHB.DP2.Application.Features.Procurement.Invite.Abstract;
using GHB.DP2.Application.Features.Procurement.Jp006.Dto;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft.SectionType;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PpTorDraft;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetVendorRequest(
    Guid VendorId,
    Guid ContractDraftId,
    Guid ProcurementId);

public record ContractDraftDocumentVersionResponse(
    Guid FileId,
    string Version,
    DateTimeOffset CreatedAt,
    string CreatedByName,
    bool IsCurrent);

public record VendorInfo(
    SuVendorType Type,
    string Name,
    ParameterCode EntrepreneurType,
    string EntrepreneurTypeName,
    string TaxpayerIdentificationNo,
    string? SapBranchNumber,
    string Nationality,
    string? Tel,
    string? Email,
    string? RegistrationPlace,
    string? VendorRegistrationPlace,
    string? Address,
    string? Street,
    string? SubDistrict,
    string? SubDistrictName,
    string? District,
    string? DistrictName,
    string? Province,
    string? ProvinceName,
    string? PostalCode,
    DateTimeOffset? StartDate,
    DateTimeOffset? EndDate) : Dto.VendorInfo(StartDate, EndDate, RegistrationPlace, VendorRegistrationPlace, Address, Street, Province, District, SubDistrict, PostalCode)
{
    public static VendorInfo FromEntity(Vendor vendor, LocationDto? location)
    {
        return new VendorInfo(
            vendor.VendorInfo.Type,
            vendor.VendorInfo.EstablishmentName,
            vendor.VendorInfo.EntrepreneurType,
            vendor.VendorInfo.EntrepreneurTypeInfo.Label,
            vendor.VendorInfo.TaxpayerIdentificationNo,
            vendor.VendorInfo.SapBranchNumber,
            vendor.VendorInfo.Nationality.ToString(),
            vendor.VendorInfo.Tel ?? string.Empty,
            vendor.VendorInfo.Email ?? string.Empty,
            vendor.EstablishmentName ?? string.Empty,
            vendor.VendorRegistrationPlace ?? string.Empty,
            !string.IsNullOrWhiteSpace(vendor.Address) ? vendor.Address : vendor.VendorInfo.AddressLine ?? string.Empty,
            !string.IsNullOrWhiteSpace(vendor.Road) ? vendor.Road : vendor.VendorInfo.Road ?? string.Empty,
            vendor.RawSubDistrictCode ?? vendor.VendorInfo.RawSubDistrictCode,
            location?.SubDistrictName ?? string.Empty,
            vendor.RawDistrictCode ?? vendor.VendorInfo.RawDistrictCode,
            location?.DistrictName ?? string.Empty,
            vendor.RawProvinceCode ?? vendor.VendorInfo.RawProvinceCode,
            location?.ProvinceName ?? string.Empty,
            vendor.VendorInfo.PostalCode ?? string.Empty,
            vendor.StartDate,
            vendor.EndDate);
    }
}

public record ContractDraftShareholderResponse(
    [property: Description("รหัสผู้ถือหุ้น")]
    Guid? Id,
    [property: Description("ลำดับ")] int? Sequence,
    [property: Description("เลขที่ผู้เสียภาษี")]
    string? TaxId,
    [property: Description("ชื่อจริง")] string? FirstName,
    [property: Description("นามสกุล")] string? LastName,
    [property: Description("เป็นกรรมการหรือถือหุ้น 20%")]
    bool? IsDirector,
    [property: Description("เป็นผู้ถือหุ้น")]
    bool? IsShareholder,
    [property: Description("เป็นนิติบุคคล")]
    bool? IsJuristic,
    [property: Description("ประเภทการตรวจสอบ")]
    string? CheckType,
    [property: Description("ผลการตรวจสอบ Watchlist")]
    bool? WatchlistResult,
    [property: Description("หมายเหตุผลการตรวจสอบ Watchlist")]
    string? WatchlistResultRemark,
    [property: Description("วันที่ตรวจสอบ Watchlist")]
    DateTimeOffset? WatchlistResultAt,
    [property: Description("ผลการตรวจสอบความขัดแย้งทางผลประโยชน์")]
    bool? CoiResult,
    [property: Description("หมายเหตุผลการตรวจสอบความขัดแย้งทางผลประโยชน์")]
    string? CoiResultRemark,
    [property: Description("วันที่ตรวจสอบความขัดแย้งทางผลประโยชน์")]
    DateTimeOffset? CoiResultAt,
    [property: Description("ผลการตรวจสอบ eGP")]
    bool? EgpResult,
    [property: Description("หมายเหตุ eGP")]
    string? EgpRemark,
    [property: Description("วันที่ตรวจสอบ eGP")]
    DateTimeOffset? EgpResultAt,
    [property: Description("ผลการตรวจสอบความขัดแย้งทางผลประโยชน์")]
    QualificationResultDto? CoiCheckerResult,
    [property: Description("ผลการตรวจสอบ Watchlist")]
    QualificationResultDto? WatchlistCheckerResult);

public record Operators(
    Guid UserId,
    int Sequence);

public class ContractDraftInfo : ContractDraftDetailBase
{
    public new VendorInfo Vendor { get; init; }

    public static ContractDraftInfo FromEntity(CaContractDraftVendor vendor, PpTorDraft? tor, LocationDto? location = default)
    {
        return new ContractDraftInfo
        {
            DefectWarrantyTypeCode = vendor.DraftTermsConditions.DefectWarrantyTypeCode,
            Buyer = BuyerInfo.FromEntity(vendor.Buyer),
            Vendor = VendorInfo.FromEntity(vendor.Vendor, location),
            Agreement = Optional(vendor.Agreement)
                        .Map(AgreementBase.MapToModel)
                        .IfNoneUnsafe((AgreementBase?)null),
            Payment = PaymentBase.MapToModel(vendor, tor),
            Guarantee = GuaranteeInfo.FromEntity(vendor.DraftTermsConditions.Guarantee, vendor.ContractInvitationVendors, tor),
            Penalty = PenaltyInfo.FromEntity(vendor.DraftTermsConditions.Penalty, vendor.Budget, tor),
            Redelivery = RedeliveryBase.FromEntity(vendor.DraftTermsConditions.RedeliveryCorrection),
            AdvancePayment = Dto.AdvancePayment.FromEntity(vendor.DraftTermsConditions.AdvancePayment),
            Delivery = DeliveryInfo.FromEntity(vendor.Delivery, tor),
            Warranty = WarrantyInfo.FromEntity(vendor.DraftTermsConditions.Warranty, tor),
            Termination = TerminationInfo.FromEntity(vendor.Termination),
            CopierLease = CopierLeaseInfo.FromEntity(vendor.DraftEquipmentRental.CopierLease),
            ComputerLease = ComputerLeaseInfo.FromEntity(vendor.DraftEquipmentRental.LeaseDuration),
            CarLease = CarLeaseInfo.FromEntity(vendor.DraftEquipmentRental.CarLease),
            Attachments = [.. vendor.Attachments.Map(Attachment.FromEntity).OrderBy(x => x.Sequence)],
            RetentionPayment = Dto.RetentionPayment.FromEntity(vendor.DraftTermsConditions.RetentionPayment),
        };
    }
}

public record GetVendorResponse : ContractDraftInfoBase
{
    public new string ContractNumber { get; init; }

    public new AcceptorResponse[] Acceptors { get; init; }

    public new ContractDraftInfo Detail { get; init; }

    public ContractDraftStatus ContractStatus { get; init; }

    public Guid? VendorId { get; init; }

    public new bool? EgpResult { get; init; }

    public new string? EgpRemark { get; init; }

    public new DateTimeOffset? EgpDate { get; init; }

    public new bool? CoiResult { get; init; }

    public new string? CoiRemark { get; init; }

    public new DateTimeOffset? CoiDate { get; init; }

    public new bool? WatchlistResult { get; init; }

    public new string? WatchlistRemark { get; init; }

    public new DateTimeOffset? WatchlistDate { get; init; }

    public new QualificationResultDto? CoiCheckerResult { get; init; }

    public new QualificationResultDto? WatchlistCheckerResult { get; init; }

    public new ContractDraftShareholderResponse[]? Shareholder { get; init; }

    public EntrepreneurResponseAttachment[] CheckerAttachments { get; init; }

    public new DateTimeOffset? DocumentDate { get; init; }

    public ContractDraftDocumentVersionResponse[] ContractDraftDocumentVersions { get; init; }

    public ContractDraftDocumentVersionResponse[] ApprovalContractDraftDocumentVersions { get; init; }

    public ContractDraftDocumentVersionResponse[] ConfidentialContractDraftDocumentVersions { get; init; }

    public static GetVendorResponse FromEntity(CaContractDraftVendor vendor, PpTorDraft? tor)
    {
        var coiChecker =
            vendor.Checkers
                  .OrderByDescending(c => c.ResultAt)
                  .FirstOrDefault(c => c.CheckType == QualificationType.COI);

        var watchlistChecker =
            vendor.Checkers
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

        var shareholder = vendor.Shareholders.Count != 0
            ? vendor.Shareholders
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

                        return new ContractDraftShareholderResponse(
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
                    .ToArray()
            : (vendor.ContractInvitationVendors?.Shareholders ?? [])
                    .Where(s => !s.IsDeleted)
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

                        return new ContractDraftShareholderResponse(
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

        var periodCondition =
                    tor?.PpTorDraftTechnicalPeriods?
                        .FirstOrDefault()?
                        .PeriodConditionCode?
                        .Value;

        var periodConditionCode =
            periodCondition == "DelvCUnit004"
                ? null
                : periodCondition?.Replace("DelvCUnit", "CSDPCond");

        var operators = vendor.ContractInvitationVendors?
                                .PurchaseOrderApprovalContract?
                                .Approval?.Assignees?
                                .Where(x => x.Type == AssigneeType.Assignee)
                                .OrderBy(x => x.Sequence)
                                .Select(c => new Operators((Guid)c.UserId, c.Sequence)).ToArray();

        return new GetVendorResponse
        {
            Id = vendor.Id.Value,
            VendorId = vendor.Vendor.VendorId.Value,
            Email = vendor.Email,
            ContractName = vendor.ContractName,
            ContractNumber = vendor.ContractNumber,
            PoNumber = vendor.PoNumber,
            Budget = vendor.Budget,
            ContractSignedDate = vendor.ContractSignedDate,
            ContractType = vendor.ContractTypeCode?.Value ?? string.Empty,
            Template = vendor.TemplateCode?.Value ?? string.Empty,
            TemplateText = vendor.TemplateText,
            SubTemplate = vendor.SubTemplateCode?.Value,
            SubTemplateText = vendor.SubTemplateText,
            PeriodConditionType = vendor.PeriodConditionTypeCode?.Value ?? periodConditionCode,
            IsWorkingDayOnly = vendor.IsWorkingDayOnly,
            StartDate = vendor.StartDate,
            EndDate = vendor.EndDate,
            Status = vendor.Status,
            Detail = ContractDraftInfo.FromEntity(vendor, tor),
            Acceptors = [.. vendor.Acceptors
                              .Map(DelegatorExtensions.DelegatorToAcceptor)
                              .Map(MapAcceptor)],
            ContractDraftDocumentId = vendor.ContractDraftDocument?.FileId.Value,
            IsContractDraftDocumentIdReplace = vendor.ContractDraftDocument?.IsReplaced,
            ApprovalContractDraftDocumentId = vendor.ApprovedDocument?.FileId.Value,
            IsApprovalContractDraftDocumentIdReplace = vendor.ApprovedDocument?.IsReplaced,
            ConfidentialContractDraftDocumentId = vendor.ConfidentialDocument?.FileId.Value,
            IsConfidentialContractDraftDocumentIdReplace = vendor.ConfidentialDocument?.IsReplaced,
            ContractStatus = vendor.ContractDraft.Status,
            EgpResult = vendor.EgpResult,
            EgpRemark = vendor.EgpRemark,
            EgpDate = vendor.EgpDate,
            CoiResult = vendor.CoiResult,
            CoiRemark = vendor.CoiRemark,
            CoiDate = vendor.CoiDate,
            WatchlistResult = vendor.WatchlistResult,
            WatchlistRemark = vendor.WatchlistRemark,
            WatchlistDate = vendor.WatchlistDate,
            CoiCheckerResult = coiCheckerResult,
            WatchlistCheckerResult = watchlistCheckerResult,
            Shareholder = shareholder,
            CheckerAttachments = MapCheckerAttachments(vendor.CheckerAttachment),
            VatRateTypeCode = vendor.Agreement?.VatRateTypeCode?.Value ?? (vendor.ContractInvitationVendors?.PurchaseOrderApprovalContract?.Entrepreneur?.PJp006PriceDetails.Count(x => x.VatTypeCode == VatTypeConstant.IncluedVat) > 0 ? VatTypeConstant.IncluedVat : VatTypeConstant.NotIncludeVat),
            Operators = operators,
            DocumentDate = vendor.DocumentDate,
            ContractDraftDocumentVersions = (vendor.DocumentHistories ?? [])
                                                  .Where(d => d.DocumentType == CaContractDraftVendorDocumentType.ContractDraft)
                                                  .OrderVersions()
                                                  .Select((d, index) => new ContractDraftDocumentVersionResponse(
                                                      d.FileId.Value,
                                                      d.Version,
                                                      d.CreatedAt,
                                                      d.CreatedByName ?? string.Empty,
                                                      index == 0))
                                                  .ToArray(),
            ApprovalContractDraftDocumentVersions = (vendor.DocumentHistories ?? [])
                                                          .Where(d => d.DocumentType == CaContractDraftVendorDocumentType.ApprovalContractDraft)
                                                          .OrderVersions()
                                                          .Select((d, index) => new ContractDraftDocumentVersionResponse(
                                                              d.FileId.Value,
                                                              d.Version,
                                                              d.CreatedAt,
                                                              d.CreatedByName ?? string.Empty,
                                                              index == 0))
                                                          .ToArray(),
            ConfidentialContractDraftDocumentVersions = (vendor.DocumentHistories ?? [])
                                                              .Where(d => d.DocumentType == CaContractDraftVendorDocumentType.ConfidentialContractDraft)
                                                              .OrderVersions()
                                                              .Select((d, index) => new ContractDraftDocumentVersionResponse(
                                                                  d.FileId.Value,
                                                                  d.Version,
                                                                  d.CreatedAt,
                                                                  d.CreatedByName ?? string.Empty,
                                                                  index == 0))
                                                              .ToArray(),
        };
    }

    private static AcceptorResponse MapAcceptor(CaContractDraftAcceptor acceptor)
    {
        return new AcceptorResponse(
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
            DelegateeUserId: acceptor.Delegatee?.SuUserId.Value);
    }

    private static EntrepreneurResponseAttachment[] MapCheckerAttachments(
        IEnumerable<CaContractDraftVendorCheckerAttachments> attachments)
    {
        return
        [
            .. attachments
                .OrderBy(o => o.Sequence)
                .GroupBy(
                    a => a.DocumentTypeCode,
                    (key, g) => new EntrepreneurResponseAttachment(
                        key.Value,
                        [.. g.Select(
                            a => new EntrepreneurFileWithId(
                                a.Id.Value,
                                a.FileId.Value,
                                a.FileName,
                                a.Sequence,
                                a.IsPublic,
                                a.AuditInfo.CreatedBy,
                                a.Type))]))
        ];
    }
}

public class GetVendorEndpoint : ContractDraftEndpointBase<GetVendorRequest, Results<Ok<GetVendorResponse>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public GetVendorEndpoint(
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
        this.Get("procurement/{ProcurementId}/contract-draft/{ContractDraftId}/vendor/{VendorId}");
        this.Description(b => b
                              .WithTags(nameof(ContractDraft))
                              .WithName("GetVendor")
                              .Produces<GetVendorResponse>()
                              .Produces<string>(StatusCodes.Status404NotFound)
                              .WithSummary("Get Vendor Details")
                              .WithDescription("Retrieve details of a vendor associated with a contract draft for a specific procurement."));
    }

    protected override async ValueTask<Results<Ok<GetVendorResponse>, NotFound<string>>> HandleRequestAsync(GetVendorRequest req, CancellationToken ct)
    {
        var vendor = await this.dbContext
                               .CaContractDrafts
                               .Where(c =>
                                   c.ProcurementId == ProcurementId.From(req.ProcurementId) &&
                                   c.Id == ContractDraftId.From(req.ContractDraftId))
                               .SelectMany(c => c.Vendors)
                               .Include(caContractDraftVendor => caContractDraftVendor.Buyer)
                               .Include(caContractDraftVendor => caContractDraftVendor.DocumentHistories)
                               .Include(caContractDraftVendor => caContractDraftVendor.Acceptors)
                               .ThenInclude(acceptor => acceptor.Delegatee)
                               .ThenInclude(delegatee => delegatee!.SuUser)
                               .Include(caContractDraftVendor => caContractDraftVendor.Shareholders)
                               .ThenInclude(s => s.VendorShareholderCheckers)
                               .Include(c => c.ContractInvitationVendors)
                               .ThenInclude(v => v.PurchaseOrderApprovalContract)
                               .ThenInclude(poac => poac.Entrepreneur)
                               .ThenInclude(e => e!.PJp006PriceDetails)
                               .Include(c => c.ContractInvitationVendors)
                               .ThenInclude(v => v.Shareholders)
                               .ThenInclude(s => s.VendorShareholderCheckers)
                               .AsSplitQuery()
                               .FirstOrDefaultAsync(
                                   v => v.Id == ContractDraftVendorId.From(req.VendorId),
                                   ct);

        if (vendor is null)
        {
            return TypedResults.NotFound("ไม่พบผู้ขายที่ระบุ");
        }

        var tor = await this.dbContext
                               .PpTorDrafts
                               .Include(t => t.PpTorTemplateComputer)
                               .FirstOrDefaultAsync(
                                   c => c.ProcurementId == ProcurementId.From(req.ProcurementId) && c.IsActive,
                                   ct);

        var response = GetVendorResponse.FromEntity(vendor, tor);

        return TypedResults.Ok(response);
    }
}