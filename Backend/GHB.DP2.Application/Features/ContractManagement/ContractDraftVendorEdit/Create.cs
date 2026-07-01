namespace GHB.DP2.Application.Features.ContractManagement.ContractDraftVendorEdit;

using System.IdentityModel.Tokens.Jwt;
using FluentValidation;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Features.ContractManagement.ContractDraftVendorEdit.Abstract;
using GHB.DP2.Application.Features.ContractManagement.ContractDraftVendorEdit.Dto;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractManagement.CaContractDraftVendorEdit;
using GHB.DP2.Domain.ContractManagement.ContractManagement;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PJp005;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record CreateContractDraftVendorEditRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid ContractDraftVendorId,
    ContractDraftVendorEditComponentDto[]? Components,
    IEnumerable<AcceptorRequest>? Acceptors,
    IEnumerable<AssigneeRequest>? Assignees,
    DateTimeOffset? DocumentDate = null);

public class CreateContractDraftVendorEditValidator : Validator<CreateContractDraftVendorEditRequest>
{
    public CreateContractDraftVendorEditValidator()
    {
        this.RuleFor(x => x.ContractDraftVendorId)
            .NotEmpty()
            .WithMessage("กรุณาระบุรหัสสัญญา");
    }
}

public class CreateContractDraftVendorEditEndpoint
    : ContractDraftVendorEditEndpoint<CreateContractDraftVendorEditRequest, Created<Guid>>
{
    private readonly Dp2DbContext dbContext;
    private readonly IOperationService operationService;

    public CreateContractDraftVendorEditEndpoint(
        ILogger<CreateContractDraftVendorEditEndpoint> logger,
        Dp2DbContext dbContext,
        IOperationService operationService)
        : base(logger, dbContext)
    {
        this.dbContext = dbContext;
        this.operationService = operationService;
    }

    public override void Configure()
    {
        this.Post("contract/contract-draft-vendor-edit");
        this.Description(b => b
                              .WithTags("ContractManagement/ContractDraftVendorEdit")
                              .WithName("CreateContractDraftVendorEdit")
                              .AllowAnonymous()
                              .Produces<Created<Guid>>(StatusCodes.Status201Created)
                              .ProducesProblem(StatusCodes.Status400BadRequest)
                              .ProducesProblem(StatusCodes.Status500InternalServerError));
    }

    protected override async ValueTask<Created<Guid>> HandleRequestAsync(
        CreateContractDraftVendorEditRequest req, CancellationToken ct)
    {
        var source = await this.dbContext.CaContractDraftVendors
                               .Include(c => c.ContractDraft)
                               .ThenInclude(cd => cd.Procurement)
                               .Include(c => c.ContractInvitationVendors)
                               .Include(c => c.PaymentTerms)
                               .Include(c => c.Attachments)
                               .ThenInclude(a => a.Files)
                               .Include(c => c.Acceptors)
                               .Include(c => c.Shareholders)
                               .ThenInclude(s => s.VendorShareholderCheckers)
                               .Include(c => c.Checkers)
                               .Include(c => c.CheckerAttachment)
                               .Include(c => c.DraftTermsConditions)
                               .ThenInclude(caContractDraftTermsConditions => caContractDraftTermsConditions.AdvancePayment)
                               .Include(c => c.DraftEquipmentRental)
                               .ThenInclude(caContractDraftEquipmentRental => caContractDraftEquipmentRental.CopierLease)
                               .Include(caContractDraftVendor => caContractDraftVendor.Buyer)
                               .Include(caContractDraftVendor => caContractDraftVendor.Agreement)
                               .Include(caContractDraftVendor => caContractDraftVendor.Payment)
                               .Include(caContractDraftVendor => caContractDraftVendor.Termination)
                               .Include(caContractDraftVendor => caContractDraftVendor.DraftTermsConditions)
                               .ThenInclude(caContractDraftTermsConditions => caContractDraftTermsConditions.Warranty)
                               .Include(caContractDraftVendor => caContractDraftVendor.DraftTermsConditions)
                               .ThenInclude(caContractDraftTermsConditions => caContractDraftTermsConditions.Penalty)
                               .Include(caContractDraftVendor => caContractDraftVendor.DraftTermsConditions)
                               .ThenInclude(caContractDraftTermsConditions => caContractDraftTermsConditions.Guarantee)
                               .Include(caContractDraftVendor => caContractDraftVendor.DraftTermsConditions)
                               .ThenInclude(caContractDraftTermsConditions => caContractDraftTermsConditions.RetentionPayment)
                               .Include(caContractDraftVendor => caContractDraftVendor.DraftTermsConditions)
                               .ThenInclude(caContractDraftTermsConditions => caContractDraftTermsConditions.RedeliveryCorrection)
                               .Include(caContractDraftVendor => caContractDraftVendor.DraftEquipmentRental)
                               .ThenInclude(caContractDraftEquipmentRental => caContractDraftEquipmentRental.CarLease)
                               .Include(c => c.Delivery)
                               .Include(caContractDraftVendor => caContractDraftVendor.Vendor)
                               .AsSplitQuery()
                               .SingleOrDefaultAsync(
                                   d => d.Id == ContractDraftVendorId.From(req.ContractDraftVendorId), ct);

        if (source is null)
        {
            this.ThrowError($"ไม่พบสัญญาที่มีรหัส {req.ContractDraftVendorId}", StatusCodes.Status404NotFound);
        }

        if (source.Status != ContractDraftVendorStatus.Approved)
        {
            this.ThrowError("สามารถแก้ไขได้เฉพาะสัญญาที่ผ่านอนุมัติแล้วเท่านั้น", StatusCodes.Status400BadRequest);
        }

        var procurement = source.ContractDraft.Procurement;

        var contractManagement = ContractManagement.Create(
            source.Id,
            ProcurementId.From((Guid)source.ContractDraft.ProcurementId),
            source.ContractName,
            procurement.DepartmentId,
            procurement.SupplyMethodCode,
            procurement.SupplyMethodTypeCode,
            procurement.SupplyMethodSpecialTypeCode,
            procurement.BudgetYear ?? 0);

        this.dbContext.ContractManagements.Add(contractManagement);

        var entity = CaContractDraftVendorEdit.Create(
            source.Id,
            (Guid)source.ContractDraft.ProcurementId,
            source.ContractInvitationVendorsId);

        entity.SetContractManagementId(contractManagement.Id);

        var existingEditCount = await this.dbContext.CaContractDraftVendorEdits
            .CountAsync(e => e.ContractDraftVendorId == source.Id && !e.IsDeleted, ct);
        var contractNumber = $"{source.ContractNumber}({existingEditCount + 1})";

        entity.SetEmail(source.Email)
              .SetContractName(source.ContractName)
              .SetPoNumber(source.PoNumber)
              .SetContractDraftNumber(source.ContractDraftNumber)
              .SetContractNumber(contractNumber)
              .SetBudget(source.Budget);

        entity.SetVendor(source.Vendor);

        if (source.ContractSignedDate.HasValue)
        {
            entity.SetContractSignedDate(source.ContractSignedDate.Value);
        }

        if (source.ContractEndDate.HasValue)
        {
            entity.SetContractEndDate(source.ContractEndDate.Value);
        }

        entity.SetContractType(source.ContractTypeCode?.Value ?? string.Empty)
              .SetTemplate(source.TemplateCode?.Value ?? string.Empty)
              .SetTemplateText(source.TemplateText)
              .SetSubTemplate(source.SubTemplateCode?.Value)
              .SetSubTemplateText(source.SubTemplateText)
              .SetIsWorkingDayOnly(source.IsWorkingDayOnly);

        if (source.StartDate.HasValue)
        {
            entity.SetStartDate(source.StartDate.Value);
        }

        if (source.EndDate.HasValue)
        {
            entity.SetEndDate(source.EndDate.Value);
        }

        entity.SetPeriodConditionType(source.PeriodConditionTypeCode?.Value)
              .SetContractStatus(source.ContractStatus)
              .SetVendorAppointmentMemoDate(source.VendorAppointmentMemoDate);

        if (source.Buyer != null)
        {
            entity.SetBuyer(source.Buyer);
        }

        if (source.Agreement != null)
        {
            entity.SetAgreement(source.Agreement);
        }

        if (source.Payment != null)
        {
            entity.SetPayment(source.Payment);
        }

        if (source.Delivery != null)
        {
            entity.SetDelivery(source.Delivery);
        }

        if (source.Termination != null)
        {
            entity.SetTermination(source.Termination);
        }

        if (source.EgpResult.HasValue)
        {
            entity.SetEgp(source.EgpResult.Value, source.EgpRemark, source.EgpDate);
        }

        if (source.CoiResult.HasValue)
        {
            entity.SetCoi(source.CoiResult.Value, source.CoiRemark, source.CoiDate);
        }

        if (source.WatchlistResult.HasValue)
        {
            entity.SetWatchlist(source.WatchlistResult.Value, source.WatchlistRemark, source.WatchlistDate);
        }

        var paymentTerms = source.PaymentTerms.Select(pt =>
        {
            var newPt = CaContractDraftEditPaymentTerm.Create();
            newPt.SetPaymentTermNo(pt.PaymentTermNo)
                 .SetLeadTime(pt.LeadTime)
                 .SetDeliveryDate(pt.DeliveryDate)
                 .SetInstallmentPercentage(pt.InstallmentPercentage)
                 .SetAmount(pt.Amount)
                 .SetAdvanceDeductionAmount(pt.AdvanceDeductionAmount)
                 .SetPerformanceDeductionAmount(pt.PerformanceDeductionAmount)
                 .SetDescription(pt.Description)
                 .SetSequence(pt.Sequence)
                 .SetPeriodType(pt.PeriodTypeCode);

            return newPt;
        }).ToList();
        entity.SetPaymentTerm(paymentTerms);

        foreach (var att in source.Attachments)
        {
            var newAtt = CaContractDraftEditVendorsAttachment.Create(
                att.TypeCode,
                att.Description,
                att.PageNumber,
                att.Sequence,
                att.FormatOtherName);

            entity.SetAttachments(newAtt);
        }

        var jp005 = await this.dbContext.PJp005S
                              .Include(jp => jp.Committees)
                              .Where(jp => jp.ProcurementId == source.ContractDraft.ProcurementId)
                              .Where(jp => !jp.IsDeleted)
                              .FirstOrDefaultAsync(ct);

        var jp005Committees = jp005?.Committees
                                   .Where(c => c.GroupType == PJp005CommitteeGroupType.InspectionCommittee)
                                   .OrderBy(c => c.Sequence)
                                   .ToList();

        var contractSignAcceptor = source.Acceptors
                                         .Where(a => a.Type == AcceptorType.AcceptorSign)
                                         .ToList();

        if (contractSignAcceptor.Any())
        {
            foreach (var acceptor in contractSignAcceptor)
            {
                var user = await this.dbContext.SuUsers
                                     .Include(u => u.Employee)
                                     .ThenInclude(e => e.View)
                                     .FirstOrDefaultAsync(u => u.Id == acceptor.UserId, ct);

                if (user != null)
                {
                    var newAcc = CaContractDraftEditAcceptor.Create(user, AcceptorType.AcceptorSign, acceptor.Sequence);
                    entity.AddAcceptor(newAcc);
                }
            }
        }

        if (jp005Committees != null && jp005Committees.Any())
        {
            foreach (var committee in jp005Committees)
            {
                var user = await this.dbContext.SuUsers
                                     .Include(u => u.Employee)
                                     .ThenInclude(e => e.View)
                                     .FirstOrDefaultAsync(u => u.Id == committee.SuUserId, ct);

                if (user != null)
                {
                    var newAcc = CaContractDraftEditAcceptor.Create(user, AcceptorType.AcceptanceCommittee, committee.Sequence);
                    newAcc.SetCommitteePositionsCode(committee.CommitteePositionsCode);
                    entity.AddAcceptor(newAcc);
                }
            }
        }
        else
        {
            var purchaseOrderApproval = await this.dbContext.PPurchaseOrderApprovals
                                                  .Include(p => p.Committees)
                                                  .Where(p => p.ProcurementId == source.ContractDraft.ProcurementId)
                                                  .Where(p => !p.IsDeleted)
                                                  .FirstOrDefaultAsync(ct);

            if (purchaseOrderApproval != null)
            {
                foreach (var committee in purchaseOrderApproval.Committees.OrderBy(c => c.Sequence))
                {
                    var user = await this.dbContext.SuUsers
                                         .Include(u => u.Employee)
                                         .ThenInclude(e => e.View)
                                         .FirstOrDefaultAsync(u => u.Id == committee.SuUserId, ct);

                    if (user != null)
                    {
                        var newAcc = CaContractDraftEditAcceptor.Create(user, AcceptorType.AcceptanceCommittee, committee.Sequence);
                        newAcc.SetCommitteePositionsCode(committee.CommitteePositionsCode);
                        entity.AddAcceptor(newAcc);
                    }
                }
            }
        }

        foreach (var sh in source.Shareholders.OrderBy(s => s.Sequence))
        {
            var newSh = CaContractDraftEditVendorShareholders.Create(
                (int)sh.Sequence,
                sh.TaxId ?? string.Empty,
                sh.FirstName ?? string.Empty,
                sh.LastName ?? string.Empty,
                (bool)sh.IsDirector,
                sh.IsShareholder);
            entity.AddCaContractDraftEditVendorShareholder(newSh);
        }

        foreach (var ck in source.Checkers)
        {
            entity.AddChecker(ck.CheckType, ck.Result, ck.ResultAt, ck.Remark);
        }

        foreach (var ca in source.CheckerAttachment.OrderBy(c => c.Sequence))
        {
            entity.AddAttachment(CaContractDraftEditVendorCheckerAttachments.Create(
                ca.DocumentTypeCode,
                ca.FileId,
                ca.FileName,
                ca.Type,
                ca.Sequence,
                ca.IsPublic));
        }

        if (source.DraftTermsConditions != null)
        {
            entity.SetDefectWarrantyTypeCode(source.DraftTermsConditions.DefectWarrantyTypeCode);

            if (source.DraftTermsConditions.AdvancePayment != null)
            {
                entity.SetAdvancePayment(source.DraftTermsConditions.AdvancePayment);
            }

            if (source.DraftTermsConditions.Warranty != null)
            {
                entity.SetWarranty(source.DraftTermsConditions.Warranty);
            }

            if (source.DraftTermsConditions.Penalty != null)
            {
                entity.SetPenalty(source.DraftTermsConditions.Penalty);
            }

            if (source.DraftTermsConditions.Guarantee != null)
            {
                entity.SetGuarantee(source.DraftTermsConditions.Guarantee);
            }

            if (source.DraftTermsConditions.RetentionPayment != null)
            {
                entity.SetRetentionPayment(source.DraftTermsConditions.RetentionPayment);
            }

            if (source.DraftTermsConditions.RedeliveryCorrection != null)
            {
                entity.SetRedeliveryCorrection(source.DraftTermsConditions.RedeliveryCorrection);
            }
        }

        if (source.DraftEquipmentRental != null)
        {
            entity.SetCopierLease(source.DraftEquipmentRental.CopierLease);
            entity.SetCarLease(source.DraftEquipmentRental.CarLease);
        }

        if (source.TemplateCode.HasValue)
        {
            _ = this.MapComponentByTemplateCode(source.TemplateCode!.Value.Value, [])
                    .Iter(r =>
                        entity.AddComponent(
                            CaContractDraftVendorEditComponent.Create(
                                entity.Id,
                                r.ComponentCode,
                                r.ComponentName,
                                false)));

            entity.AddComponent(
                CaContractDraftVendorEditComponent.Create(
                    entity.Id,
                    "EditPo",
                    "เพิ่ม PO ใหม่"));
        }

        if (req.Acceptors != null && req.Acceptors.Any())
        {
            await this.UpsertAcceptors(entity, [.. req.Acceptors], ct, UserId.From(req.UserId));
        }

        if (!entity.Assignees.Any())
        {
            var contractDirector = await this.operationService.GetDefaultJorPorDirectorAsync(ct);

            if (contractDirector is not null)
            {
                var user = await this.dbContext.SuUsers
                                     .Include(u => u.Employee)
                                     .ThenInclude(e => e.View)
                                     .FirstOrDefaultAsync(u => u.Id == contractDirector.UserId, ct);

                if (user != null)
                {
                    var newAssignee = CaContractDraftVendorEditAssignee.Create(
                        AssigneeGroup.Contract,
                        AssigneeType.Director,
                        user,
                        1);

                    entity.AddAssignee(newAssignee);
                }
            }
        }

        var segmentContractManager = await this.operationService.GetSegmentContractManagerAsync(ct);

        if (segmentContractManager is not null)
        {
            var user = await this.dbContext.SuUsers
                                 .Include(u => u.Employee)
                                 .ThenInclude(e => e.View)
                                 .FirstOrDefaultAsync(u => u.Id == segmentContractManager.UserId, ct);

            if (user != null)
            {
                var newAssignee = CaContractDraftVendorEditAssignee.Create(
                    AssigneeGroup.AddendumDrafter,
                    AssigneeType.Director,
                    user,
                    1);

                entity.AddAssignee(newAssignee);

                var reviewer = CaContractDraftEditAcceptor.Create(user, AcceptorType.Reviewer, 1);
                reviewer.Pending();
                entity.AddAcceptor(reviewer);
            }
        }

        if (req.DocumentDate is not null)
        {
            entity.SetDocumentDate(req.DocumentDate);
        }

        var supplyMethodCode = source.ContractDraft.Procurement.SupplyMethodCode;
        await this.SetDefaultDocumentTemplate(entity, supplyMethodCode, ct);

        await this.UpdateDocumentAsync(
            entity,
            supplyMethodCode,
            new ContractDraftVendorEditDocumentOptions(
                true,
                true,
                IsMarkReplaced: true),
            ct);

        this.dbContext.CaContractDraftVendorEdits.Add(entity);
        await this.dbContext.SaveChangesAsync(CancellationToken.None);

        return TypedResults.Created(string.Empty, entity.Id.Value);
    }
}