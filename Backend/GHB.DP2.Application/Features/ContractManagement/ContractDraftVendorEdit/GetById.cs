namespace GHB.DP2.Application.Features.ContractManagement.ContractDraftVendorEdit;

using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.ContractAgreement.ContractDraft.Dto;
using GHB.DP2.Application.Features.ContractManagement.ContractDraftVendorEdit.Abstract;
using GHB.DP2.Application.Features.ContractManagement.ContractDraftVendorEdit.Dto;
using System.IdentityModel.Tokens.Jwt;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractManagement.CaContractDraftVendorEdit;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PPurchaseOrder;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetContractDraftVendorEditByIdRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid Id);

public class GetContractDraftVendorEditByIdEndpoint
    : ContractDraftVendorEditEndpoint<GetContractDraftVendorEditByIdRequest, Results<Ok<ContractDraftVendorEditDetailResponse>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public GetContractDraftVendorEditByIdEndpoint(
        ILogger<GetContractDraftVendorEditByIdEndpoint> logger,
        Dp2DbContext dbContext)
        : base(logger, dbContext)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Get("contract/contract-draft-vendor-edit/{Id:guid}");
        this.Description(b => b
                              .WithTags("ContractManagement/ContractDraftVendorEdit")
                              .WithName("GetContractDraftVendorEditById")
                              .AllowAnonymous()
                              .Produces<ContractDraftVendorEditDetailResponse>(StatusCodes.Status200OK)
                              .Produces<string>(StatusCodes.Status404NotFound));
    }

    protected override async ValueTask<Results<Ok<ContractDraftVendorEditDetailResponse>, NotFound<string>>>
        HandleRequestAsync(GetContractDraftVendorEditByIdRequest req, CancellationToken ct)
    {
        var entity = await this.GetEditByIdAsync(ContractDraftVendorEditId.From(req.Id), ct);

        // ── Fetch old data from original ContractDraftVendor (section-level only) ──
        var source = await this.dbContext.CaContractDraftVendors
                               .Include(v => v.Buyer)
                               .Include(v => v.Vendor)
                               .Include(v => v.Agreement)
                               .Include(v => v.Payment)
                               .Include(v => v.Delivery)
                               .Include(v => v.Termination)
                               .ThenInclude(termination => termination.VendorProcessingTime)
                               .Include(v => v.DraftTermsConditions)
                               .ThenInclude(tc => tc.AdvancePayment)
                               .Include(v => v.DraftTermsConditions)
                               .ThenInclude(tc => tc.Warranty)
                               .Include(v => v.DraftTermsConditions)
                               .ThenInclude(tc => tc.Penalty)
                               .Include(v => v.DraftTermsConditions)
                               .ThenInclude(tc => tc.Guarantee)
                               .Include(v => v.DraftTermsConditions)
                               .ThenInclude(tc => tc.RetentionPayment)
                               .Include(v => v.DraftTermsConditions)
                               .ThenInclude(tc => tc.RedeliveryCorrection)
                               .Include(v => v.DraftEquipmentRental)
                               .ThenInclude(er => er.CopierLease)
                               .Include(v => v.DraftEquipmentRental)
                               .ThenInclude(er => er.CarLease)
                               .Include(v => v.DraftEquipmentRental)
                               .ThenInclude(er => er.LeaseDuration)
                               .Include(v => v.PaymentTerms).Include(caContractDraftVendor => caContractDraftVendor.ContractInvitationVendors)
                               .ThenInclude(caContractInvitationVendors => caContractInvitationVendors.ContractInvitation).Include(caContractDraftVendor => caContractDraftVendor.ContractDraft)
                               .ThenInclude(caContractDraft => caContractDraft.Procurement)
                               .AsSplitQuery()
                               .SingleOrDefaultAsync(c => c.Id == entity.ContractDraftVendorId, ct);

        var oldData = source != null
            ? new ContractDraftVendorEditOldDataDto(
                source.PoNumber,
                source.Budget,
                source.ContractSignedDate,
                source.ContractEndDate,
                BuyerInfo.FromEntity(source.Buyer),
                MapVendorInfo(source.Vendor),
                source.Agreement != null ? AgreementBase.MapToModel(source.Agreement) : null,
                PaymentBase.MapToModel(source, null),
                source.Delivery != null ? new DeliveryInfo(source.Delivery.Address ?? string.Empty, source.Delivery.Date, source.Delivery.LeadTime, source.Delivery.PeriodTypeCode?.Value ?? string.Empty, source.Delivery.LeadTimeTypeCode?.Value, source.Delivery.LeadOtherTime, source.Delivery.LeadOtherTimeTypeCode?.Value, source.Delivery.CountingConditionCode?.Value) : null,
                source.Termination != null ? new TerminationInfo(source.Termination.StartDate, source.Termination.EndDate, source.Termination.VendorProcessingTime) : null,
                source.DraftTermsConditions?.DefectWarrantyTypeCode?.Value,
                source.DraftTermsConditions?.Warranty != null ? WarrantyInfo.FromEntity(source.DraftTermsConditions.Warranty, null) : null,
                source.DraftTermsConditions?.Penalty != null ? PenaltyInfo.FromEntity(source.DraftTermsConditions.Penalty, source.Budget, null) : null,
                source.DraftTermsConditions?.Guarantee != null ? GuaranteeInfo.FromEntity(source.DraftTermsConditions.Guarantee, source.ContractInvitationVendors, null) : null,
                source.DraftTermsConditions?.AdvancePayment != null ? AdvancePayment.FromEntity(source.DraftTermsConditions.AdvancePayment) : null,
                source.DraftTermsConditions?.RetentionPayment != null ? RetentionPayment.FromEntity(source.DraftTermsConditions.RetentionPayment) : null,
                RedeliveryBase.FromEntity(source.DraftTermsConditions?.RedeliveryCorrection),
                source.DraftEquipmentRental?.CopierLease != null ? CopierLeaseInfo.FromEntity(source.DraftEquipmentRental.CopierLease) : null,
                source.DraftEquipmentRental?.LeaseDuration != null ? ComputerLeaseInfo.FromEntity(source.DraftEquipmentRental.LeaseDuration) : null,
                source.DraftEquipmentRental?.CarLease != null ? CarLeaseInfo.FromEntity(source.DraftEquipmentRental.CarLease) : null)
            : null;

        // ── Document versions ──
        var amendmentDoc = entity.GetAmendmentDocumentForStatus(entity.Status);
        var approvalRequestDoc = entity.GetApprovalRequestDocumentForStatus(entity.Status);

        var isAmendmentReplaced = HasReplacedDocument(entity, CaContractDraftEditVendorDocumentType.Amendment);
        var isApprovalRequestReplaced = HasReplacedDocument(entity, CaContractDraftEditVendorDocumentType.AmendmentApprovalRequest);

        var amendmentVersions = MapDocumentVersions(entity, CaContractDraftEditVendorDocumentType.Amendment);
        var approvalRequestVersions = MapDocumentVersions(entity, CaContractDraftEditVendorDocumentType.AmendmentApprovalRequest);

        // ── Map acceptors ──
        var currentCommittees = entity.Acceptors
                                      .Where(x => x.Type == AcceptorType.AcceptanceCommittee)
                                      .ToList();

        var currentApprovers = entity.Acceptors
                                     .Where(x => x.Type != AcceptorType.AcceptanceCommittee)
                                     .Select(DelegatorExtensions.DelegatorToAcceptor)
                                     .ToList();

        var allAcceptors = currentApprovers.Union(currentCommittees);

        // ── Check if current user is PurchaseOrderApproval assignee ──
        var isPurchaseOrderApprovalAssignee = await this.dbContext.PPurchaseOrderApprovals
            .AnyAsync(
                poa => poa.ProcurementId == ProcurementId.From(entity.ProcurementId)
                && !poa.IsDeleted
                && poa.Assignees.Any(a => a.Type == AssigneeType.Assignee
                    && a.UserId == UserId.From(req.UserId)
                    && !a.IsDeleted),
                ct);

        var contractInvitaions = await this.dbContext.CaContractInvitationVendors
                                           .Where(x => x.Id == entity.ContractInvitationVendorsId)
                                           .FirstOrDefaultAsync(ct);

        // ── Build response ──
        var response = new ContractDraftVendorEditDetailResponse(
            entity.Id.Value,
            entity.ContractDraftVendorId.Value,
            entity.ProcurementId,
            entity.Status,
            entity.Email,
            entity.Title,
            entity.Description,
            entity.ContractName,
            entity.PoNumber,
            entity.ContractDraftNumber.Value,
            entity.ContractNumber,
            entity.Budget,
            entity.ContractSignedDate,
            entity.ContractEndDate,
            source?.ContractDraft.Procurement.SupplyMethodCode,
            source?.ContractDraft.Procurement.SupplyMethodSpecialTypeCode,
            entity.ContractTypeCode?.Value,
            entity.ContractType?.Label,
            entity.TemplateCode?.Value,
            entity.Template?.Label,
            entity.TemplateText,
            entity.SubTemplateCode?.Value,
            entity.SubTemplateText,
            entity.StartDate,
            entity.EndDate,
            entity.IsWorkingDayOnly,
            entity.VendorAppointmentMemoDate,
            entity.PeriodConditionTypeCode?.Value,
            BuyerInfo.FromEntity(entity.Buyer),
            MapVendorInfo(entity.Vendor),
            entity.Agreement != null ? AgreementBase.MapToModel(entity.Agreement) : null,
            MapPaymentBase(entity),
            entity.Delivery != null ? new DeliveryInfo(entity.Delivery.Address ?? string.Empty, entity.Delivery.Date, entity.Delivery.LeadTime, entity.Delivery.PeriodTypeCode?.Value ?? string.Empty, entity.Delivery.LeadTimeTypeCode?.Value, entity.Delivery.LeadOtherTime, entity.Delivery.LeadOtherTimeTypeCode?.Value, entity.Delivery.CountingConditionCode?.Value) : null,
            entity.Termination != null ? new TerminationInfo(entity.Termination.StartDate, entity.Termination.EndDate, entity.Termination.VendorProcessingTime) : null,
            entity.DraftTermsConditions?.DefectWarrantyTypeCode?.Value,
            entity.DraftTermsConditions?.Warranty != null ? WarrantyInfo.FromEntity(entity.DraftTermsConditions.Warranty, null) : null,
            entity.DraftTermsConditions?.Penalty != null ? PenaltyInfo.FromEntity(entity.DraftTermsConditions.Penalty, entity.Budget, null) : null,
            entity.DraftTermsConditions?.Guarantee != null ? GuaranteeInfo.FromEntity(entity.DraftTermsConditions.Guarantee, contractInvitaions, null) : null,
            entity.DraftTermsConditions?.AdvancePayment != null ? AdvancePayment.FromEntity(entity.DraftTermsConditions.AdvancePayment) : null,
            entity.DraftTermsConditions?.RetentionPayment != null ? RetentionPayment.FromEntity(entity.DraftTermsConditions.RetentionPayment) : null,
            RedeliveryBase.FromEntity(entity.DraftTermsConditions?.RedeliveryCorrection),
            entity.DraftEquipmentRental?.CopierLease != null ? CopierLeaseInfo.FromEntity(entity.DraftEquipmentRental.CopierLease) : null,
            entity.DraftEquipmentRental?.LeaseDuration != null ? ComputerLeaseInfo.FromEntity(entity.DraftEquipmentRental.LeaseDuration) : null,
            entity.DraftEquipmentRental?.CarLease != null ? CarLeaseInfo.FromEntity(entity.DraftEquipmentRental.CarLease) : null,
            entity.EgpResult,
            entity.EgpRemark,
            entity.EgpDate,
            entity.CoiResult,
            entity.CoiRemark,
            entity.CoiDate,
            entity.WatchlistResult,
            entity.WatchlistRemark,
            entity.WatchlistDate,
            entity.ContractStatus,
            [
                .. entity.Assignees
                         .OrderBy(a => a.Sequence)
                         .Select(DelegatorExtensions.DelegatorToAssignee)
                         .Select(a => new AssigneeResponse(
                             a.Id.Value,
                             a.Group,
                             a.Type,
                             a.UserId.Value,
                             a.Sequence,
                             a.FullName,
                             a.PositionName,
                             a.BusinessUnitName,
                             a.Status,
                             a.Remark,
                             a.ActionAt,
                             a.Delegatee?.SuUserId.Value))
            ],
            [
                .. allAcceptors
                    .OrderBy(a => a.Sequence)
                    .Select(a => new AcceptorNoIdResponse(
                        a.Id.Value,
                        a.Type,
                        a.UserId.Value,
                        a.Sequence,
                        a.FullName,
                        a.PositionName,
                        a.BusinessUnitName,
                        a.Status,
                        a.Remark,
                        a.ActionAt,
                        (string?)a.CommitteePositionsCode,
                        a.CommitteePosition?.Label ?? string.Empty,
                        a.IsUnableToPerformDuties,
                        IsCurrent: a.IsCurrentApprover(),
                        DelegateeUserId: a.Delegatee?.SuUserId.Value))
            ],
            [
                .. entity.Components
                         .Select(c => new ContractDraftVendorEditComponentDto(
                             c.Id.Value,
                             c.ComponentCode,
                             c.ComponentName,
                             c.IsEdited))
            ],
            [
                .. entity.Attachments
                         .OrderBy(a => a.Sequence)
                         .Select(a => new ContractDraftVendorEditAttachmentDto(
                             a.Id.Value,
                             a.TypeCode.Value,
                             a.Description,
                             a.PageNumber,
                             a.Sequence,
                             a.FormatOtherName,
                             [.. a.Files.OrderBy(f => f.Sequence)
                                       .Select(f => new ContractDraftVendorEditAttachmentFileDto(
                                           f.Id.Value,
                                           f.Id.Value,
                                           f.FileName,
                                           f.FileType,
                                           f.Sequence))]))
            ],
            [
                .. entity.Shareholders
                         .OrderBy(s => s.Sequence)
                         .Select(s => new ContractDraftVendorEditShareholderDto(
                             s.Id.Value,
                             (int)s.Sequence,
                             s.TaxId,
                             s.FirstName,
                             s.LastName,
                             (bool)s.IsDirector,
                             s.IsShareholder))
            ],
            oldData,
            amendmentDoc?.FileId.Value,
            isAmendmentReplaced,
            approvalRequestDoc?.FileId.Value,
            isApprovalRequestReplaced,
            amendmentVersions,
            approvalRequestVersions,
            isPurchaseOrderApprovalAssignee,
            [.. entity.CheckerAttachment
                       .Where(a => a.Type == EntrepreneurAttachmentType.General)
                       .GroupBy(a => a.DocumentTypeCode.Value)
                       .Select(g => new AttachmentsDto(
                           g.Key,
                           [.. g.OrderBy(f => f.Sequence)
                                .Select(f => new GHB.DP2.Application.Dtos.FileAttachments(
                                    f.FileId.Value,
                                    f.FileName,
                                    f.Sequence,
                                    f.IsPublic,
                                    Guid.Empty))]))],
            DocumentDate: entity.DocumentDate);

        return TypedResults.Ok(response);
    }

    // ── Map Vendor domain type to VendorInfo DTO ──
    private static VendorInfo? MapVendorInfo(
        GHB.DP2.Domain.ContractAgreement.CaContractDraft.SectionType.Vendor? vendor) =>
        vendor != null
            ? new VendorInfo(
                vendor.StartDate,
                vendor.EndDate,
                vendor.EstablishmentName,
                vendor.VendorRegistrationPlace,
                vendor.Address,
                vendor.Road,
                vendor.RawProvinceCode,
                vendor.RawDistrictCode,
                vendor.RawSubDistrictCode,
                null)
            : null;

    // ── Map edit entity Payment + PaymentTerms to PaymentBase DTO ──
    private static PaymentBase? MapPaymentBase(CaContractDraftVendorEdit entity)
    {
        if (entity.PaymentTerms.Any())
        {
            return new Term
            {
                DueDay = entity.Payment?.DueDays ?? 0,
                RedeliveryTypeCode = entity.Payment?.RedeliveryDateCode?.Value,
                PaymentTypeCode = entity.Payment?.TypeCode?.Value,
                Details =
                [
                    .. entity.PaymentTerms
                             .OrderBy(x => x.Sequence)
                             .Select(x => new PaymentTermDetail
                             {
                                 Id = x.Id.Value,
                                 No = x.PaymentTermNo,
                                 LeadTime = x.LeadTime,
                                 DeliveryDate = x.DeliveryDate,
                                 InstallmentPercentage = x.InstallmentPercentage,
                                 Amount = x.Amount,
                                 AdvanceDeductionAmount = x.AdvanceDeductionAmount,
                                 PerformanceDeductionAmount = x.PerformanceDeductionAmount,
                                 Description = x.Description,
                                 Sequence = x.Sequence,
                                 PeriodTypeCode = x.PeriodTypeCode ?? ParameterCode.From("PeriodType001"),
                                 IsCanEditLeadTime = true,
                             })
                ],
            };
        }

        if (entity.Payment == null)
        {
            return null;
        }

        return new Term
        {
            DueDay = entity.Payment.DueDays ?? 0,
            RedeliveryTypeCode = entity.Payment.RedeliveryDateCode?.Value,
            PaymentTypeCode = entity.Payment.TypeCode?.Value,
            Details = [],
        };
    }

    private static bool HasReplacedDocument(
        CaContractDraftVendorEdit entity, CaContractDraftEditVendorDocumentType type) =>
        entity.DocumentHistories.Any(d => d.DocumentType == type && d.IsReplaced);

    private static ContractDraftVendorEditDocumentVersionResponse[] MapDocumentVersions(
        CaContractDraftVendorEdit entity, CaContractDraftEditVendorDocumentType type) =>
        entity.DocumentHistories
            .Where(d => d.DocumentType == type)
            .OrderVersions()
            .Select((d, i) => new ContractDraftVendorEditDocumentVersionResponse(
                d.FileId.Value, d.Version, d.CreatedAt, d.CreatedByName, i == 0))
            .ToArray();
}
