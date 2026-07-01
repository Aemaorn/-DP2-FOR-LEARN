namespace GHB.DP2.Application.Features.ContractManagement.DeliveryAcceptancePeriod;

using System.IdentityModel.Tokens.Jwt;
using FluentValidation;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Features.ContractManagement.DeliveryAcceptancePeriod.Abstract;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement;
using GHB.DP2.Application.Features.Procurement.PrincipleApproval.Dto;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractManagement.CaContractDraftVendorEdit;
using GHB.DP2.Domain.ContractManagement.CmDeliveryAcceptance;
using GHB.DP2.Domain.Plan;
using GHB.DP2.Domain.Procurement.PPurchaseOrderApproval;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record CreateDeliveryAcceptancePeriodRequest(
    Guid DeliveryAcceptanceId,
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    CmDeliveryAcceptancePeriodStatus Status,
    IEnumerable<AcceptorRequest> AcceptanceCommittees,
    IEnumerable<AssigneeRequest> Assignees,
    IEnumerable<AcceptorRequest> Acceptors,
    Guid? DocumentId,
    bool? IsDocumentReplaced,
    BudgetDetail[] BudgetDetails,
    string? AcceptanceNumber,
    string? Description,
    string? PhoneNumber,
    decimal? ContractBudgetAmount,
    string? ObjectiveDescription,
    bool HasDeduction,
    string? DeductionDescription,
    decimal? DeductionAmount,
    bool HasInvoiceSlip,
    string? InvoiceSlipDescription,
    decimal? InvoiceSlipAmount,
    GetById.Cm001PaymentTermResponse[] PaymentTerms,
    GetById.InspectionCommitteeSectionResponse? InspectionCommittees,
    DateTimeOffset? DocumentDate = null);

public class CreateDeliveryAcceptancePeriodRequestValidator : Validator<CreateDeliveryAcceptancePeriodRequest>
{
    public CreateDeliveryAcceptancePeriodRequestValidator()
    {
        this.RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage("สถานะไม่ถูกต้อง");

        this.When(x => x.Status == CmDeliveryAcceptancePeriodStatus.WaitingCommitteeApproval, () =>
        {
            this.RuleFor(r => r.AcceptanceCommittees)
                .Must(x => x.Any(acceptorRequest => acceptorRequest.AcceptorType is AcceptorType.AcceptanceCommittee))
                .WithMessage("ต้องมีบุคคล/คณะกรรมการตรวจรับพัสดุ 1 คน");
        });

        this.When(x => x.Status == CmDeliveryAcceptancePeriodStatus.WaitingAcceptance, () =>
        {
            this.RuleFor(r => r.Acceptors)
                .Must(x => x.Any(acceptorRequest => acceptorRequest.AcceptorType is AcceptorType.Approver))
                .WithMessage("ต้องมีผู้มีอำนาจเห็นชอบ/อนุมัติอย่างน้อย 1 คน");
        });
    }
}

public class CreateDeliveryAcceptancePeriodEndPoint : DeliveryAcceptancePeriodEndpointBase<CreateDeliveryAcceptancePeriodRequest, Results<Ok<CmDeliveryAcceptancePeriodId>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public CreateDeliveryAcceptancePeriodEndPoint(
        Dp2DbContext dbContext,
        ILogger<UpdateDeliveryAcceptancePeriodEndpoint> logger,
        IOperationService operationService,
        ICommandTextService commandTextService)
        : base(dbContext, logger, operationService, commandTextService)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b => b.WithTags("ContractManagement/DeliveryAcceptance/Period"));
        this.Post("delivery-acceptance/{DeliveryAcceptanceId:guid?}/period");
    }

    protected override async ValueTask<Results<Ok<CmDeliveryAcceptancePeriodId>, NotFound<string>>> HandleRequestAsync(CreateDeliveryAcceptancePeriodRequest req, CancellationToken ct)
    {
        var deliveryAcceptance = await this.dbContext.CmDeliveryAcceptances
                                           .FirstOrDefaultAsync(x => x.Id == CmDeliveryAcceptanceId.From(req.DeliveryAcceptanceId), ct);

        if (deliveryAcceptance is null)
        {
            return TypedResults.NotFound("ไม่พบรายการส่งมอบตรวจรับ");
        }

        Plan? planData = null;
        CaContractDraftVendor? contractDraftVendorData = null;
        Domain.Procurement.Procurement? procurementData = null;

        if (deliveryAcceptance.SourceType == SourceType.Plan)
        {
            planData = await this.dbContext.Plans.FirstOrDefaultAsync(a => a.Id == PlanId.From((Guid)deliveryAcceptance.RefId), ct);

            if (planData is null)
            {
                return TypedResults.NotFound("ไม่พบแผนจัดซื้อจัดจ้าง");
            }
        }
        else if (deliveryAcceptance.SourceType == SourceType.ContractDraftVendor)
        {
            contractDraftVendorData = await this.dbContext.CaContractDraftVendors
                                                .Include(x => x.ContractDraft)
                                                .ThenInclude(x => x.Procurement)
                                                .Include(x => x.ContractInvitationVendors)
                                                .FirstOrDefaultAsync(x => x.Id == ContractDraftVendorId.From((Guid)deliveryAcceptance.RefId), ct);

            if (contractDraftVendorData is null)
            {
                return TypedResults.NotFound("ไม่พบร่างสัญญาของผู้ขาย");
            }

            procurementData = contractDraftVendorData.ContractDraft.Procurement;
        }
        else if (deliveryAcceptance.SourceType == SourceType.Procurement)
        {
            var poaData = await this.dbContext.PPurchaseOrderApprovals
                                    .Include(poa => poa.Procurement)
                                    .FirstOrDefaultAsync(poa => poa.Id == PurchaseOrderApprovalId.From((Guid)deliveryAcceptance.RefId), ct);

            if (poaData is null)
            {
                return TypedResults.NotFound("ไม่พบข้อมูลใบสั่งซื้อ/จ้าง/เช่า");
            }

            procurementData = poaData.Procurement;
        }
        else if (deliveryAcceptance.SourceType == SourceType.ContractDraftVendorEdit)
        {
            var vendorEditCreate = await this.dbContext.CaContractDraftVendorEdits
                .FirstOrDefaultAsync(ve => ve.Id == ContractDraftVendorEditId.From((Guid)deliveryAcceptance.RefId), ct);

            if (vendorEditCreate is null)
            {
                return TypedResults.NotFound("ไม่พบข้อมูลบันทึกต่อท้ายสัญญา");
            }

            contractDraftVendorData = await this.dbContext.CaContractDraftVendors
                .Include(v => v.ContractDraft).ThenInclude(cd => cd.Procurement)
                .Include(v => v.ContractInvitationVendors)
                .FirstOrDefaultAsync(v => v.Id == vendorEditCreate.ContractDraftVendorId, ct);

            if (contractDraftVendorData is null)
            {
                return TypedResults.NotFound("ไม่พบข้อมูลสัญญาต้นฉบับ");
            }

            procurementData = contractDraftVendorData.ContractDraft.Procurement;
        }

        var acceptanceNumber = await this.GenerateAcceptanceNumberAsync(ct);

        var newPeriod = CmDeliveryAcceptancePeriod.Create(
            deliveryAcceptance);

        newPeriod.SetValue(
            req.HasDeduction,
            req.DeductionDescription,
            req.DeductionAmount,
            req.HasInvoiceSlip,
            req.InvoiceSlipDescription,
            req.InvoiceSlipAmount,
            req.PhoneNumber,
            req.ObjectiveDescription,
            req.ContractBudgetAmount,
            req.Description);

        newPeriod.SetAcceptanceNumber(acceptanceNumber);

        if (req.DocumentDate is not null)
        {
            newPeriod.SetDocumentDate(req.DocumentDate);
        }

        this.UpsertBudgets(newPeriod, req.BudgetDetails ?? []);
        this.UpsertPaymentTerms(newPeriod, req.PaymentTerms ?? []);

        var acceptanceCommittees = req.InspectionCommittees is not null
            ? req.InspectionCommittees.Committees
                 .Select(x =>
                     new AcceptorRequest(
                         x.Id,
                         AcceptorType.AcceptanceCommittee,
                         x.UserId,
                         x.Sequence,
                         x.CommitteePositionsCode,
                         false))
                 .ToArray()
            : req.AcceptanceCommittees.ToArray();

        await this.UpsertAcceptorAsync(
            newPeriod,
            [.. req.Acceptors, .. acceptanceCommittees],
            ct,
            UserId.From(req.UserId));

        await this.UpsertAssigneeAsync(
            newPeriod,
            [.. req.Assignees],
            ct,
            UserId.From(req.UserId));

        newPeriod.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.Create,
                string.Empty,
                nameof(newPeriod.Status)));

        this.dbContext.CmDeliveryAcceptancePeriods.Add(newPeriod);

        var currentUserId = Guid.TryParse(this.HttpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var parsedUserId) ? parsedUserId : Guid.Empty;

        var rawSupplyMethodCode = deliveryAcceptance.SourceType switch
        {
            SourceType.Plan => planData!.SupplyMethodCode,
            SourceType.ContractDraftVendor => contractDraftVendorData!.ContractDraft.Procurement.SupplyMethodCode,
            SourceType.Procurement => procurementData!.SupplyMethodCode,
            SourceType.ContractDraftVendorEdit => procurementData!.SupplyMethodCode,
            SourceType.Manual => deliveryAcceptance.SupplyMethodCode,
            _ => throw new InvalidOperationException("SourceType ไม่ถูกต้อง"),
        };

        var supplyMethodCode = rawSupplyMethodCode ?? throw new InvalidOperationException("SupplyMethodCode ไม่ถูกต้อง");

        var acceptorIds = newPeriod.Acceptors
                                   .Where(c => c.Type == AcceptorType.Approver)
                                   .Select(c => c.UserId.Value)
                                   .ToArray();
        var hasJorPorAssign = await this.HasJorPorAssign(newPeriod, req.UserId, ct);

        await this.SetDefaultDocumentTemplate(
            newPeriod,
            supplyMethodCode,
            currentUserId,
            hasJorPorAssign || req.HasDeduction,
            ct);

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok(newPeriod.Id);
    }

    private void UpsertBudgets(
        CmDeliveryAcceptancePeriod periodExisting,
        BudgetDetail[] budgetDetails)
    {
        var allIncomingBudgets = budgetDetails.ToList();

        foreach (var budgetDto in allIncomingBudgets)
        {
            var newBudget =
                CmDeliveryAcceptancePeriodBudget.Create(
                    periodExisting.Id,
                    budgetDto.Sequence,
                    budgetDto.Department,
                    ParameterCode.From(budgetDto.BudgetType),
                    budgetDto.ProjectCode,
                    ParameterCode.From(budgetDto.AccountNo),
                    budgetDto.Budget);

            periodExisting.AddBudget(newBudget);
        }
    }

    private void UpsertPaymentTerms(
        CmDeliveryAcceptancePeriod periodExisting,
        GetById.Cm001PaymentTermResponse[] paymentTerms)
    {
        var allIncomingPaymentTerms = paymentTerms.ToList();

        foreach (var paymentTermDto in allIncomingPaymentTerms)
        {
            var newPaymentTerm =
                CmDeliveryAcceptancePeriodPaymentTerm.Create(
                    periodExisting.Id,
                    paymentTermDto.Sequence,
                    paymentTermDto.PaymentTerm,
                    paymentTermDto.Description,
                    paymentTermDto.Amount);

            periodExisting.AddPaymentTerm(newPaymentTerm);
        }

        var resequenced = periodExisting.PaymentTerms
                                        .OrderBy(p => p.PaymentTerm)
                                        .ToList();

        for (var i = 0; i < resequenced.Count; i++)
        {
            resequenced[i].Update(
                i + 1,
                resequenced[i].PaymentTerm,
                resequenced[i].Description,
                resequenced[i].Amount);
        }
    }

    private async Task UpsertAcceptorAsync(
        CmDeliveryAcceptancePeriod periodExisting,
        AcceptorRequest[] acceptorsRequest,
        CancellationToken ct,
        UserId? sendToAcceptorId = null)
    {
        var usersIncoming = await this.ValidateAndGetUsersAsync(acceptorsRequest, ct);

        var allCode = acceptorsRequest.Map(s => s.CommitteePositionsCode)
                                      .Where(c => !c.IsNullOrEmpty())
                                      .Select(c => ParameterCode.From(c!))
                                      .ToArray();

        var allCommitCodeDic = await this.dbContext.SuParameters
                                         .Where(p => allCode.Contains(p.Code))
                                         .ToDictionaryAsync(k => k.Code, v => v, ct);

        var lastAssigneeUserId = periodExisting.Assignees
            .OrderByDescending(a => a.Sequence)
            .Select(a => (UserId?)a.UserId)
            .FirstOrDefault();

        var resolvedSendToAcceptorId = lastAssigneeUserId ?? sendToAcceptorId;

        CreateNewAcceptors(periodExisting, acceptorsRequest, usersIncoming, allCommitCodeDic, resolvedSendToAcceptorId);
    }

    private async Task<SuUser[]> ValidateAndGetUsersAsync(
        AcceptorRequest[] acceptorsRequest,
        CancellationToken ct)
    {
        var userIdsIncoming = acceptorsRequest
                              .Select(s => UserId.From(s.UserId))
                              .ToArray();

        var usersIncoming = await this.dbContext.SuUsers
                                      .Include(r => r.Employee)
                                      .ThenInclude(r => r.View)
                                      .Where(w => userIdsIncoming.Contains(w.Id))
                                      .ToArrayAsync(ct);

        var userNotExistsInDb = userIdsIncoming
                                .Except(usersIncoming.Select(u => u.Id))
                                .ToArray();

        if (userNotExistsInDb.Length > 0)
        {
            this.ThrowError(
                $"User with ID {string.Join(", ", userNotExistsInDb)} not found.",
                StatusCodes.Status404NotFound);
        }

        return usersIncoming;
    }

    private static void CreateNewAcceptors(
        CmDeliveryAcceptancePeriod periodExisting,
        AcceptorRequest[] acceptorsRequest,
        SuUser[] usersIncoming,
        Dictionary<ParameterCode, SuParameter> allCommitCodeDic,
        UserId? sendToAcceptorId = null)
    {
        var newAcceptorRequests = acceptorsRequest.Where(w => !w.Id.HasValue);

        foreach (var req in newAcceptorRequests)
        {
            var user = usersIncoming.First(usr => usr.Id == UserId.From(req.UserId));

            var newAcceptor = CmDeliveryAcceptancePeriodAcceptor
                              .Create(
                                  periodExisting.Id,
                                  req.AcceptorType,
                                  user,
                                  req.Sequence,
                                  periodExisting.Status)
                              .SetIsUnableToPerformDuties(req.IsUnableToPerformDuties ?? false);

            newAcceptor.SetSendToAcceptorId(sendToAcceptorId);

            if (req.CommitteePositionsCode != null)
            {
                allCommitCodeDic.TryGetValue(ParameterCode.From(req.CommitteePositionsCode), out var commitParam);
                newAcceptor.SetCommitteePositions(commitParam);
            }

            periodExisting.AddAcceptor(newAcceptor);
        }
    }

    private async Task<string> GenerateAcceptanceNumberAsync(CancellationToken ct)
    {
        var yearSuffix = ((DateTimeOffset.UtcNow.Year + 543) % 100).ToString("D2");
        var prefix = $"RC{yearSuffix}";

        var lastAcceptanceNumber = await this.dbContext.CmDeliveryAcceptancePeriods
                                             .IgnoreQueryFilters()
                                             .Where(p => !string.IsNullOrWhiteSpace(p.AcceptanceNumber) && p.AcceptanceNumber.StartsWith(prefix))
                                             .OrderByDescending(p => p.AcceptanceNumber)
                                             .Select(p => p.AcceptanceNumber)
                                             .FirstOrDefaultAsync(ct);

        if (lastAcceptanceNumber is null)
        {
            return $"{prefix}00001";
        }

        var sequencePart = lastAcceptanceNumber[prefix.Length..];
        var nextSequence = int.Parse(sequencePart) + 1;

        return $"{prefix}{nextSequence:D5}";
    }
}