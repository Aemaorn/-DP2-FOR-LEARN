namespace GHB.DP2.Application.Features.Procurement.PurchaseRequisition;

using FluentValidation;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.PurchaseRequisition.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PpPurchaseRequisition;
using GHB.DP2.Domain.Procurement.PpTorDraft;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.Raws.Constants;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Threading;

public record CreatePurchaseRequisitionRequest(
    Guid ProcurementId,
    Guid? TorDraftId,
    Guid? UserId,
    string? PaymentTypeCode,
    PurchaseRequisition Requisition,
    IEnumerable<PurchaseRequisitionBudget> Budgets,
    IEnumerable<PurchaseRequisitionWarranty> Warranties,
    IEnumerable<PurchaseRequisitionPaymentTerm> PaymentTerms,
    IEnumerable<PurchaseRequisitionFineRate> FineRates,
    IEnumerable<PurchaseRequisitionCommittee> Committees,
    IEnumerable<PpPurchaseRequisitionTechnicalSpecification> ScopeOfWorks,
    IEnumerable<AcceptorRequest> Acceptors,
    IEnumerable<AssigneeRequest> Assignees);

public record PurchaseRequisitionBudget(
    string Description,
    decimal BudgetAmount,
    int Sequence,
    IEnumerable<PurchaseRequisitionBudgetDetail> Details);

public record PurchaseRequisitionBudgetDetail(
    int Sequence,
    string Department,
    string BudgetType,
    string? ProjectCode,
    string AccountNo,
    decimal Budget);

public record PurchaseRequisitionWarranty(
    bool HasWarranty,
    int Period,
    string? PeriodTypeCode,
    string? ConditionOther);

public record PurchaseRequisitionPaymentTerm(
    int? TermNumber,
    decimal? Percent,
    int? Period,
    string? Description,
    string? PaymentTypeCode,
    string? TotalPeriodTypeCode,
    int? TotalPeriod,
    string? PeriodTypeCode,
    bool? IsMA);

public record PurchaseRequisitionFineRate(
    int Sequence,
    decimal Percentage,
    string PeriodTypeCode,
    string ConditionCode,
    string? ConditionOther);

public record PurchaseRequisitionCommittee(
    GroupType GroupType,
    Guid SuUserId,
    string FullName,
    string CommitteePositionsCode,
    int Sequence);

public record PpPurchaseRequisitionTechnicalSpecification(
    int Sequence,
    string Name,
    string Description,
    int Quantity,
    string? UnitCode);

public class CreateRequisitionRequestValidator : Validator<CreatePurchaseRequisitionRequest>
{
    public CreateRequisitionRequestValidator()
    {
        this.RuleFor(x => x.Requisition)
            .NotNull()
            .WithMessage("Requisition cannot be null.");

        this.RuleFor(x => x.ProcurementId)
            .NotEmpty()
            .WithMessage("ProcurementId is required.");

        // TorDraftId is nullable, so only validate if it has a value
        this.RuleFor(x => x.TorDraftId)
            .NotEmpty()
            .When(x => x.TorDraftId.HasValue)
            .WithMessage("TorDraftId cannot be empty when provided.");

        // These fields are nullable strings, so handle appropriately
        this.RuleFor(x => x.Requisition.PurchaseRequisitionNumber)
            .NotEmpty()
            .When(x => !string.IsNullOrEmpty(x.Requisition.PurchaseRequisitionNumber))
            .WithMessage("PurchaseRequisitionNumber cannot be empty when provided.");

        this.RuleFor(x => x.Requisition.EgpNumber)
            .NotEmpty()
            .When(x => !string.IsNullOrEmpty(x.Requisition.EgpNumber))
            .WithMessage("EgpNumber cannot be empty when provided.");

        this.RuleFor(x => x.Requisition.PrNumber)
            .NotEmpty()
            .When(x => !string.IsNullOrEmpty(x.Requisition.PrNumber))
            .WithMessage("PrNumber cannot be empty when provided.");

        this.RuleFor(x => x.Requisition.Description)
            .NotEmpty()
            .When(x => !string.IsNullOrEmpty(x.Requisition.Description))
            .WithMessage("Description cannot be empty when provided.");

        this.RuleFor(x => x.Requisition.PriceReasonablenessInfo)
            .NotEmpty()
            .When(x => !string.IsNullOrEmpty(x.Requisition.PriceReasonablenessInfo))
            .WithMessage("PriceReasonablenessInfo cannot be empty when provided.");

        // MedianPriceAmount is nullable, validate only when it has a value
        this.RuleFor(x => x.Requisition.MedianPriceAmount)
            .GreaterThan(0)
            .When(x => x.Requisition.MedianPriceAmount.HasValue)
            .WithMessage("MedianPriceAmount must be greater than 0 when provided.");

        this.RuleFor(x => x.Requisition.EvaluationCriteriaCode)
            .NotEmpty()
            .When(x => !string.IsNullOrEmpty(x.Requisition.EvaluationCriteriaCode))
            .WithMessage("EvaluationCriteriaCode cannot be empty when provided.");

        this.RuleFor(x => x.Requisition.DeliveryPeriod)
            .GreaterThan(0)
            .WithMessage("DeliveryPeriod must be greater than 0.");

        this.RuleFor(x => x.Requisition.DeliveryPeriodTypeCode)
            .NotEmpty()
            .When(x => !string.IsNullOrEmpty(x.Requisition.DeliveryPeriodTypeCode))
            .WithMessage("DeliveryPeriodTypeCode cannot be empty when provided.");

        this.RuleFor(x => x.Requisition.DeliveryConditionCode)
            .NotEmpty()
            .When(x => !string.IsNullOrEmpty(x.Requisition.DeliveryConditionCode))
            .WithMessage("DeliveryConditionCode cannot be empty when provided.");

        // Fix the logic for HasFineRate validation
        this.RuleFor(x => x.FineRates)
            .NotEmpty()
            .When(x => x.Requisition.HasFineRate)
            .WithMessage("When HasFineRate is true, at least one fine rate must be provided.");

        // Validate warranty period and related fields when HasWarranty is true
        this.RuleFor(x => x.Requisition.HasContractGuarantee)
            .NotNull()
            .WithMessage("HasContractGuarantee must be specified.");

        this.RuleFor(x => x.Requisition.HasInspectionCommittee)
            .NotNull()
            .WithMessage("HasInspectionCommittee must be specified.");

        this.RuleFor(x => x.Requisition.HasConstructionSupervisor)
            .NotNull()
            .WithMessage("HasConstructionSupervisor must be specified.");

        // Validate budgets
        this.RuleFor(x => x.Budgets)
            .NotNull()
            .WithMessage("Budgets collection cannot be null.");

        this.RuleForEach(x => x.Budgets).ChildRules(budget =>
        {
            budget.RuleFor(b => b.Description)
                  .NotEmpty()
                  .WithMessage("Budget Description is required.");

            budget.RuleFor(b => b.BudgetAmount)
                  .GreaterThan(0)
                  .WithMessage("Budget Amount must be greater than 0.");
        });

        // Validate warranties
        this.RuleFor(x => x.Warranties)
            .NotNull()
            .WithMessage("Warranties collection cannot be null.");

        this.RuleForEach(x => x.Warranties).ChildRules(warranty =>
        {
            warranty.RuleFor(w => w.Period)
                    .GreaterThan(0)
                    .When(w => w.HasWarranty)
                    .WithMessage("Warranty Period must be greater than 0 when HasWarranty is true.");

            warranty.RuleFor(w => w.PeriodTypeCode)
                    .NotEmpty()
                    .When(w => w.HasWarranty && !string.IsNullOrEmpty(w.PeriodTypeCode))
                    .WithMessage("Warranty PeriodTypeCode cannot be empty when provided and HasWarranty is true.");

            warranty.RuleFor(w => w.ConditionOther)
                    .NotEmpty()
                    .When(w => w.HasWarranty && !string.IsNullOrEmpty(w.ConditionOther))
                    .WithMessage("Warranty ConditionOther cannot be empty when provided and HasWarranty is true.");
        });

        // Validate payment terms
        this.RuleFor(x => x.PaymentTerms)
            .NotNull()
            .WithMessage("PaymentTerms collection cannot be null.");

        // Validate fine rates
        this.RuleFor(x => x.FineRates)
            .NotNull()
            .WithMessage("FineRates collection cannot be null.");

        this.RuleForEach(x => x.FineRates).ChildRules(fineRate =>
        {
            fineRate.RuleFor(fr => fr.Sequence)
                    .GreaterThan(0)
                    .WithMessage("Fine Rate Sequence must be greater than 0.");

            fineRate.RuleFor(fr => fr.Percentage)
                    .GreaterThan(0)
                    .WithMessage("Fine Rate must be greater than 0.");

            fineRate.RuleFor(fr => fr.PeriodTypeCode)
                    .NotEmpty()
                    .WithMessage("Fine Rate PeriodTypeCode is required.");

            fineRate.RuleFor(fr => fr.ConditionCode)
                    .NotEmpty()
                    .WithMessage("Fine Rate ConditionCode is required.");

            fineRate.RuleFor(fr => fr.ConditionOther)
                    .NotEmpty()
                    .When(fr => !string.IsNullOrEmpty(fr.ConditionOther))
                    .WithMessage("Fine Rate ConditionOther cannot be empty when provided.");
        });

        // Validate committees
        this.RuleFor(x => x.Committees)
            .NotNull()
            .WithMessage("Committees collection cannot be null.");

        this.RuleForEach(x => x.Committees).ChildRules(committee =>
        {
            committee.RuleFor(c => c.GroupType)
                     .IsInEnum()
                     .WithMessage("Committee GroupType must be a valid enum value.");

            committee.RuleFor(c => c.SuUserId)
                     .NotEmpty()
                     .WithMessage("Committee Id is required.");

            committee.RuleFor(c => c.FullName)
                     .NotEmpty()
                     .WithMessage("Committee FullName is required.");

            committee.RuleFor(c => c.CommitteePositionsCode)
                     .NotEmpty()
                     .WithMessage("CommitteePositionsCode is required.");
        });

        // Validate acceptors
        this.RuleFor(x => x.Acceptors)
            .NotNull()
            .WithMessage("Acceptors collection cannot be null.");

        this.RuleForEach(x => x.Acceptors).ChildRules(acceptor =>
        {
            acceptor.RuleFor(a => a.AcceptorType)
                    .IsInEnum()
                    .WithMessage("Acceptor Type must be a valid enum value.");

            acceptor.RuleFor(a => a.UserId)
                    .NotEmpty()
                    .WithMessage("Acceptor UserId is required.");

            acceptor.RuleFor(a => a.Sequence)
                    .GreaterThan(0)
                    .WithMessage("Acceptor Sequence must be greater than 0.");
        });

        this.RuleFor(x => x.ScopeOfWorks)
            .NotNull()
            .WithMessage("Acceptors collection cannot be null.");

        this.RuleForEach(x => x.ScopeOfWorks).ChildRules(technical =>
        {
            technical.RuleFor(t => t.Sequence)
                     .NotEmpty();

            technical.RuleFor(t => t.Name)
                     .NotNull()
                     .NotEmpty();

            technical.RuleFor(t => t.Description)
                     .NotEmpty()
                     .NotNull();

            technical.RuleFor(t => t.Quantity)
                     .NotEmpty();
        });
    }
}

public class CreateRequisitionEndpoint : PurchaseRequisitionEndpointBase<CreatePurchaseRequisitionRequest, Results<Ok<Guid>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;
    private readonly IOperationService operationService;

    public CreateRequisitionEndpoint(
        Dp2DbContext dbContext,
        IOperationService operationService,
        ILogger<CreateRequisitionEndpoint> logger)
        : base(logger, dbContext)
    {
        this.dbContext = dbContext;
        this.operationService = operationService;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("Procurement/PurchaseRequisition"));
        this.Post("JorPor04");
    }

    protected override async ValueTask<Results<Ok<Guid>, NotFound<string>>> HandleRequestAsync(CreatePurchaseRequisitionRequest req, CancellationToken ct)
    {
        var procurement = await this.dbContext.Procurements
                                    .AsNoTracking()
                                    .FirstOrDefaultAsync(x => x.Id == ProcurementId.From(req.ProcurementId), ct);

        if (procurement is null)
        {
            return TypedResults.NotFound($"Procurement with ID {req.ProcurementId} not found.");
        }

        var procurementOrganizationLevel = await this.dbContext.RawBusinessUnits
                                                    .AsNoTracking()
                                                    .Where(x => x.Id == procurement.DepartmentId)
                                                    .Select(x => x.OrganizationLevel)
                                                    .FirstOrDefaultAsync(ct);

        var createModel = MapPurchaseModelToDomain(req.Requisition);

        var processType = SectionProcessType.ApprovePurchaseRequest;
        var isCommercialMaterial = procurement.IsCommercialMaterial;

        if (isCommercialMaterial)
        {
            processType = SectionProcessType.ApprovePurchaseRequestCommercialParcel;
        }

        var managers = await this.operationService.GetDefaultAcceptorAsync(
            processType,
            req.UserId.Value,
            procurement.Budget ?? 0,
            procurement.SupplyMethodCode.Value,
            procurement.SupplyMethodCode.Value is SupplyMethodConstant.Eighty ? default : (string?)procurement.SupplyMethodSpecialTypeCode,
            ct,
            false);

        var branchOrganizationLevels = new[]
        {
            EmployeeConstant.OrganizationLevel.Zone,
            EmployeeConstant.OrganizationLevel.Segment,
            EmployeeConstant.OrganizationLevel.Branch,
        };

        var isCommercialMaterialUnderDirectorDepartment = (isCommercialMaterial || branchOrganizationLevels.Contains(procurementOrganizationLevel))
                                                            && !managers.Any(m => m.OrganizationLevel.ToString() == EmployeeConstant.OrganizationLevel.Line);

        var createPurchase = Domain.Procurement.PpPurchaseRequisition.PpPurchaseRequisition.Create(
            procurement,
            createModel.BasicInfo,
            createModel.PriceInfo,
            createModel.DeliveryInfo,
            createModel.WarrantyInfo,
            createModel.ContractOptions,
            isCommercialMaterialUnderDirectorDepartment,
            req.TorDraftId.HasValue ? PpTorDraftId.From(req.TorDraftId.Value) : null,
            req.PaymentTypeCode);

        if (!req.TorDraftId.HasValue)
        {
            procurement.SetProcessType(ProcessType.PurchaseRequisition);
        }

        UpdateBudgets(createPurchase, req);
        UpdateWarranties(createPurchase, req);
        UpdatePaymentTerms(createPurchase, req);
        UpdateFineRates(createPurchase, req);
        await this.UpdateCommittees(createPurchase, req, ct);

        if (req.Requisition.DocumentDate is not null)
        {
            createPurchase.SetDocumentDate(req.Requisition.DocumentDate);
        }

        var acceptorUserId =
            req.Acceptors
               .Map(a => a.UserId)
               .Map(UserId.From)
               .ToArray();

        var acceptorUsers =
            await this.dbContext.SuUsers
                      .Include(u => u.Employee)
                      .ThenInclude(s => s.View)
                      .Where(u => acceptorUserId.Contains(u.Id))
                      .ToArrayAsync(CancellationToken.None);

        var userAcceptor =
            acceptorUsers.Join(
                req.Acceptors,
                u => u.Id,
                a => UserId.From(a.UserId),
                (u, a) => new { u, a });

        foreach (var acceptor in userAcceptor)
        {
            var ppAcceptor = new PpPurchaseRequisitionAcceptorInfoData(
                acceptor.a.AcceptorType,
                acceptor.u.Id,
                acceptor.u.EmployeeCode,
                acceptor.u.Employee.View!.FullName,
                acceptor.u.Employee.ConvertPositionName(procurement.DepartmentId),
                acceptor.u.Employee.View.BusinessUnitName,
                acceptor.a.Sequence);

            var purchaseRequisition = PpPurchaseRequisitionAcceptors.Create(createPurchase.Id, ppAcceptor, createPurchase.Status);
            purchaseRequisition.SetSendToAcceptorId(UserId.From(req.UserId!.Value));

            createPurchase.AddPpPurchaseRequisitionAcceptor(purchaseRequisition);
        }

        // Initialize IsCurrent flags when entering WaitingApproval status (sequence-based)
        if (createPurchase.Status == PurchaseRequisitionStatus.WaitingApproval)
        {
            var activeApprovers = createPurchase.Acceptors
                                                .Where(a => a.Type == AcceptorType.Approver && a.IsActive)
                                                .OrderBy(a => a.Sequence)
                                                .ToList();

            foreach (var approver in activeApprovers)
            {
                var prevApproved = activeApprovers
                                   .Where(p => p.Sequence < approver.Sequence)
                                   .All(p => p.Status == AcceptorStatus.Approved);

                var isCurrent = approver.Status != AcceptorStatus.Approved && prevApproved;
                approver.SetCurrent(isCurrent);
            }
        }

        foreach (var technical in req.ScopeOfWorks)
        {
            var ppTechnical = PpPurchaseRequisitionTechnicalSpecifications.Create(
                technical.Sequence,
                technical.Name,
                technical.Description,
                technical.Quantity,
                technical.UnitCode.IsNull() ? null : ParameterCode.From(technical.UnitCode!),
                createPurchase.Id);

            createPurchase.AddPpPurchaseRequisitionTechnicalSpecification(ppTechnical);
        }

        if (req.Assignees.Any())
        {
            await this.AddAssigneeAsync(createPurchase, req.Assignees, ct, UserId.From(req.UserId!.Value));
        }

        await this.SetDefaultDocumentTemplate(
            createPurchase,
            procurement.SupplyMethodCode,
            ct);

        if (isCommercialMaterialUnderDirectorDepartment)
        {
            procurement.SetProcessType(ProcessType.Jp005);
        }

        createPurchase.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Create,
            "สร้างข้อมูลใบขอซื้อขอจ้าง(จพ.004)",
            createPurchase.Status.ToString()));

        this.dbContext.Procurements.Update(procurement);
        this.dbContext.PpPurchaseRequisitions.Add(createPurchase);
        await this.dbContext.SaveChangesAsync(ct);

        // Reload entity with includes needed by UpdateDocumentAsync
        var prReloaded = await this.GetPurchaseRequisitionById(createPurchase.Id, createPurchase.ProcurementId, ct);
        await this.UpdateDocumentAsync(prReloaded, isReplace: true, isApprove: false, ct);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok(createPurchase.Id.Value);
    }

    private static void UpdateBudgets(PpPurchaseRequisition createPurchase, CreatePurchaseRequisitionRequest req)
    {
        foreach (var budget in req.Budgets)
        {
            var ppBudget = Domain.Procurement.PpPurchaseRequisition.PpPurchaseRequisitionBudget.Create(
                createPurchase.Id,
                budget.Description,
                budget.BudgetAmount,
                budget.Sequence);

            foreach (var budgetDetail in budget.Details)
            {
                var ppBudgetDetail = Domain.Procurement.PpPurchaseRequisition.PpPurchaseRequisitionBudgetDetail.Create(
                    ppBudget.Id,
                    budgetDetail.Sequence,
                    budgetDetail.Department,
                    ParameterCode.From(budgetDetail.BudgetType),
                    budgetDetail.ProjectCode,
                    ParameterCode.From(budgetDetail.AccountNo),
                    budgetDetail.Budget);

                ppBudget.AddBudgetDetail(ppBudgetDetail);
            }

            createPurchase.AddPpPurchaseRequisitionBudget(ppBudget);
        }
    }

    private static void UpdateWarranties(PpPurchaseRequisition createPurchase, CreatePurchaseRequisitionRequest req)
    {
        foreach (var warranty in req.Warranties)
        {
            var ppWarranty = Domain.Procurement.PpPurchaseRequisition.PpPurchaseRequisitionWarranty.Create(
                createPurchase.Id,
                warranty.HasWarranty,
                warranty.Period,
                ParameterCode.From(warranty.PeriodTypeCode!),
                warranty.ConditionOther);

            createPurchase.AddPpPurchaseRequisitionWarranty(ppWarranty);
        }
    }

    private static void UpdatePaymentTerms(PpPurchaseRequisition createPurchase, CreatePurchaseRequisitionRequest req)
    {
        foreach (var paymentTerm in req.PaymentTerms)
        {
            var ppPaymentTerm = Domain.Procurement.PpPurchaseRequisition.PpPurchaseRequisitionPaymentTerm.Create(
                createPurchase.Id,
                paymentTerm.TermNumber,
                paymentTerm.Percent,
                paymentTerm.Period,
                paymentTerm.Description,
                paymentTerm.IsMA,
                paymentTerm.PaymentTypeCode,
                paymentTerm.TotalPeriodTypeCode,
                paymentTerm.TotalPeriod,
                paymentTerm.PeriodTypeCode);

            createPurchase.AddPpPurchaseRequisitionPaymentTerm(ppPaymentTerm);
        }
    }

    private static void UpdateFineRates(PpPurchaseRequisition createPurchase, CreatePurchaseRequisitionRequest req)
    {
        foreach (var fineRate in req.FineRates)
        {
            var ppFineRate = Domain.Procurement.PpPurchaseRequisition.PpPurchaseRequisitionFineRate.Create(
                createPurchase.Id,
                fineRate.Sequence,
                fineRate.Percentage,
                ParameterCode.From(fineRate.PeriodTypeCode!),
                ParameterCode.From(fineRate.ConditionCode!),
                fineRate.ConditionOther);

            createPurchase.AddPpPurchaseRequisitionFineRate(ppFineRate);
        }
    }

    private async Task UpdateCommittees(PpPurchaseRequisition createPurchase, CreatePurchaseRequisitionRequest req, CancellationToken ct)
    {
        var committeeUserIds = req.Committees.Select(c => UserId.From(c.SuUserId)).ToList();

        var committeeUsers = await this.dbContext.SuUsers
                                       .Include(u => u.Employee)
                                       .ThenInclude(e => e.View)
                                       .Where(u => committeeUserIds.Contains(u.Id))
                                       .ToListAsync(ct);

        foreach (var committee in req.Committees)
        {
            var committeePost = await this.dbContext.SuParameters
                                          .Where(w => w.Code == ParameterCode.From(committee.CommitteePositionsCode!))
                                          .FirstOrDefaultAsync(ct);

            if (committeePost is null)
            {
                this.ThrowError("ไม่พบตำแหน่งในคณะกรรมการในระบ", StatusCodes.Status404NotFound);
            }

            var user = committeeUsers.FirstOrDefault(u => u.Id == UserId.From(committee.SuUserId));

            var ppCommittee = Domain.Procurement.PpPurchaseRequisition.PpPurchaseRequisitionCommittee.Create(
                createPurchase.Id,
                committee.GroupType,
                UserId.From(committee.SuUserId),
                committee.FullName,
                user?.Employee.View?.FullPositionName ?? string.Empty,
                committeePost.Code,
                committeePost.Label,
                committee.Sequence);

            createPurchase.AddPpPurchaseRequisitionCommittee(ppCommittee);
        }
    }

    private async Task AddAssigneeAsync(
        PpPurchaseRequisition purchaseRequisition,
        IEnumerable<AssigneeRequest> requestsAssignee,
        CancellationToken ct,
        UserId? sendToAcceptorId = null)
    {
        var assigneeIds = requestsAssignee.Select(s => UserId.From(s.UserId))
                                          .ToArray();

        var userData = await this.dbContext.SuUsers
                                 .Include(e => e.Employee)
                                 .ThenInclude(v => v.View)
                                 .Where(w => assigneeIds.Contains(w.Id))
                                 .ToArrayAsync(ct);

        this.ValidateUsers(userData, assigneeIds);

        this.AddAssignee(purchaseRequisition, requestsAssignee, userData, sendToAcceptorId);
    }
}