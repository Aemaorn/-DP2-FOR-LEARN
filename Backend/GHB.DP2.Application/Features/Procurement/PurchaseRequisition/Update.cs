namespace GHB.DP2.Application.Features.Procurement.PurchaseRequisition;

using Codehard.FileService.Contracts.ValueObjects;
using FluentValidation;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.Procurement.PurchaseRequisition.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PpPurchaseRequisition;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;

public record UpdatePurchaseRequisitionRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid ProcurementId,
    Guid Id,
    string? PaymentTypeCode,
    PurchaseRequisition Requisition,
    IEnumerable<UpdatePurchaseRequisitionBudget> Budgets,
    IEnumerable<UpdatePurchaseRequisitionWarranty> Warranties,
    IEnumerable<UpdatePurchaseRequisitionPaymentTerm> PaymentTerms,
    IEnumerable<UpdatePurchaseRequisitionFineRate> FineRates,
    IEnumerable<UpdatePurchaseRequisitionCommittee> Committees,
    IEnumerable<PpPurchaseRequisitionAcceptorResponse> Acceptors,
    IEnumerable<AssigneeRequest> Assignees,
    IEnumerable<UpdatePurchaseRequisitionTechnicalSpecification> ScopeOfWorks);

public record UpdatePurchaseRequisitionBudget(
    Guid? Id,
    string Description,
    decimal BudgetAmount,
    int Sequence,
    IEnumerable<UpdatePurchaseRequisitionBudgetDetail> Details);

public record UpdatePurchaseRequisitionBudgetDetail(
    Guid? Id,
    int Sequence,
    string Department,
    string BudgetType,
    string? ProjectCode,
    string AccountNo,
    decimal Budget);

public record UpdatePurchaseRequisitionWarranty(
    Guid? Id,
    bool HasWarranty,
    int Period,
    string? PeriodTypeCode,
    string? ConditionOther);

public record UpdatePurchaseRequisitionPaymentTerm(
    Guid? Id,
    int? TermNumber,
    decimal? Percent,
    int? Period,
    string? Description,
    string? PaymentTypeCode,
    string? TotalPeriodTypeCode,
    int? TotalPeriod,
    string? PeriodTypeCode,
    bool? IsMa);

public record UpdatePurchaseRequisitionFineRate(
    Guid? Id,
    int Sequence,
    decimal Percentage,
    string PeriodTypeCode,
    string ConditionCode,
    string? ConditionOther);

public record UpdatePurchaseRequisitionCommittee(
    Guid? Id,
    GroupType GroupType,
    Guid SuUserId,
    string FullName,
    string CommitteePositionsCode,
    int Sequence);

public record UpdatePurchaseRequisitionTechnicalSpecification(
    Guid? Id,
    int Sequence,
    string Name,
    string Description,
    int Quantity,
    ParameterCode? UnitCode);

public class UpdatePurchaseRequisitionRequestValidator : Validator<UpdatePurchaseRequisitionRequest>
{
    public UpdatePurchaseRequisitionRequestValidator()
    {
        this.RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("PurchaseRequisitionId is required.");

        this.RuleFor(x => x.Requisition)
            .NotNull()
            .WithMessage("Requisition cannot be null.");

        // Validate nested Requisition properties
        this.RuleFor(x => x.Requisition.DeliveryPeriod)
            .GreaterThan(0)
            .WithMessage("DeliveryPeriod must be greater than 0.");

        this.RuleFor(x => x.Requisition.MedianPriceAmount)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Requisition.MedianPriceAmount.HasValue)
            .WithMessage("MedianPriceAmount must be greater than or equal to 0.");

        // Validate Budgets collection
        this.RuleFor(x => x.Budgets)
            .NotNull()
            .WithMessage("Budgets cannot be null.");

        this.RuleForEach(x => x.Budgets).ChildRules(budget =>
        {
            budget.RuleFor(b => b.Description)
                  .NotEmpty()
                  .WithMessage("Budget Description is required.");

            budget.RuleFor(b => b.BudgetAmount)
                  .GreaterThan(0)
                  .WithMessage("Budget Amount must be greater than 0.");

            budget.RuleFor(b => b.Details)
                  .NotNull()
                  .NotEmpty()
                  .WithMessage("Budget Details are required for each budget.");

            budget.RuleForEach(b => b.Details).ChildRules(detail =>
            {
                detail.RuleFor(d => d.Sequence)
                      .GreaterThan(0)
                      .WithMessage("Budget Detail Sequence must be greater than 0.");

                detail.RuleFor(d => d.Department)
                      .NotEmpty()
                      .WithMessage("Department is required for Budget Detail.");

                detail.RuleFor(d => d.BudgetType)
                      .NotEmpty()
                      .WithMessage("Budget Type Code is required for Budget Detail.");

                detail.RuleFor(d => d.AccountNo)
                      .NotEmpty()
                      .WithMessage("Account Number is required for Budget Detail.");

                detail.RuleFor(d => d.Budget)
                      .GreaterThanOrEqualTo(0)
                      .WithMessage("Budget Detail Budget must be greater than or equal to 0.");
            });
        });

        // Validate Warranties collection
        this.RuleFor(x => x.Warranties)
            .NotNull()
            .WithMessage("Warranties cannot be null.");

        this.RuleForEach(x => x.Warranties).ChildRules(warranty =>
        {
            warranty.RuleFor(w => w.Period)
                    .GreaterThan(0)
                    .When(w => w.HasWarranty)
                    .WithMessage("Warranty Period must be greater than 0 when warranty is required.");
        });

        // Validate Payment Terms collection
        this.RuleFor(x => x.PaymentTerms)
            .NotNull()
            .WithMessage("Payment Terms cannot be null.");

        // Validate Fine Rates collection
        this.RuleFor(x => x.FineRates)
            .NotNull()
            .WithMessage("Fine Rates cannot be null.");

        this.RuleForEach(x => x.FineRates).ChildRules(fineRate =>
        {
            fineRate.RuleFor(f => f.Sequence)
                    .GreaterThan(0)
                    .WithMessage("Fine Rate Sequence must be greater than 0.");

            fineRate.RuleFor(f => f.Percentage)
                    .GreaterThanOrEqualTo(0)
                    .WithMessage("Fine Rate must be greater than or equal to 0.");

            fineRate.RuleFor(f => f.PeriodTypeCode)
                    .NotEmpty()
                    .WithMessage("Period Type Code is required for Fine Rate.");

            fineRate.RuleFor(f => f.ConditionCode)
                    .NotEmpty()
                    .WithMessage("Condition Code is required for Fine Rate.");
        });

        // Validate Committees collection
        this.RuleFor(x => x.Committees)
            .NotNull()
            .WithMessage("Committees cannot be null.");

        this.RuleForEach(x => x.Committees).ChildRules(committee =>
        {
            committee.RuleFor(c => c.SuUserId)
                     .NotEmpty()
                     .WithMessage("User ID is required for Committee.");

            committee.RuleFor(c => c.FullName)
                     .NotEmpty()
                     .WithMessage("Full Name is required for Committee.");

            committee.RuleFor(c => c.CommitteePositionsCode)
                     .NotEmpty()
                     .WithMessage("Committee Positions Code is required for Committee.");

            committee.RuleFor(c => c.GroupType)
                     .IsInEnum()
                     .WithMessage("Group Type is invalid for Committee.");
        });

        // Validate Acceptors collection
        this.RuleFor(x => x.Acceptors)
            .NotNull()
            .WithMessage("Acceptors cannot be null.");

        this.RuleForEach(x => x.Acceptors).ChildRules(acceptor =>
        {
            acceptor.RuleFor(a => a.UserId)
                    .NotEmpty()
                    .WithMessage("User ID is required for Acceptor.");

            acceptor.RuleFor(a => a.AcceptorType)
                    .IsInEnum()
                    .WithMessage("Acceptor Type must be a valid enum value.");

            acceptor.RuleFor(a => a.Sequence)
                    .GreaterThan(0)
                    .WithMessage("Acceptor Sequence must be greater than 0.");
        });
    }
}

public record UpdatePurchaseRequisitionResponse(Guid? NewDocumentFileId);

public class UpdatePurchaseRequisitionEndpoint : PurchaseRequisitionEndpointBase<UpdatePurchaseRequisitionRequest, Results<Ok<UpdatePurchaseRequisitionResponse>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public UpdatePurchaseRequisitionEndpoint(
        Dp2DbContext dbContext,
        ILogger<UpdatePurchaseRequisitionEndpoint> logger)
        : base(logger, dbContext)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Put("procurement/{ProcurementId:guid}/JorPor04/{Id:guid}");
        this.Description(b => b
                              .WithTags("Procurement/PurchaseRequisition")
                              .WithName("UpdatePurchaseRequisition")
                              .Produces<Ok<UpdatePurchaseRequisitionResponse>>()
                              .Produces<string>(StatusCodes.Status404NotFound)
                              .ProducesProblem(StatusCodes.Status400BadRequest));
    }

    protected override async ValueTask<Results<Ok<UpdatePurchaseRequisitionResponse>, NotFound<string>>> HandleRequestAsync(UpdatePurchaseRequisitionRequest request, CancellationToken ct)
    {
        var purchaseRequisition = await this.GetPurchaseRequisitionById(
            PpPurchaseRequisitionId.From(request.Id),
            ProcurementId.From(request.ProcurementId),
            ct);

        this.ValidateDocument(request, purchaseRequisition);

        var oldStatus = purchaseRequisition.Status;

        var updateModel = MapPurchaseModelToDomain(request.Requisition);

        var isReplace = request.Requisition.IsPurchaseRequisitionDocumentIdReplaced ?? false;

        var mustSaveDocument =
            isReplace &&
            request.Requisition.PurchaseRequisitionDocumentId.HasValue &&
            purchaseRequisition.Status != PurchaseRequisitionStatus.WaitingApproval &&
            request.Requisition.Status != PurchaseRequisitionStatus.WaitingApproval;

        FileId? newDocumentFileId = null;

        if (mustSaveDocument)
        {
            newDocumentFileId = await this.UpdateDocumentHistoryAsync(
                purchaseRequisition,
                FileId.From(request.Requisition.PurchaseRequisitionDocumentId!.Value),
                isReplace,
                ct);
        }

        // Update main purchase requisition
        purchaseRequisition.Update(
            updateModel.BasicInfo,
            updateModel.PriceInfo,
            updateModel.DeliveryInfo,
            updateModel.WarrantyInfo,
            updateModel.ContractOptions,
            request.Requisition.Status,
            request.PaymentTypeCode);

        if (request.Requisition.Status == PurchaseRequisitionStatus.WaitingApproval
            || request.Requisition.DocumentDate is not null)
        {
            purchaseRequisition.SetDocumentDate(request.Requisition.DocumentDate);
        }

        await this.UpdateBudgets(purchaseRequisition, request.Budgets, ct);
        await this.UpdateWarranties(purchaseRequisition, request.Warranties, ct);
        await this.UpdatePaymentTerms(purchaseRequisition, request.PaymentTerms, ct);
        await this.UpdateFineRates(purchaseRequisition, request.FineRates, ct);
        await this.UpdateCommittees(purchaseRequisition, request.Committees, ct);

        if (purchaseRequisition.DocumentHistories == null || !purchaseRequisition.DocumentHistories.Any())
        {
            await this.SetDefaultDocumentTemplate(
                purchaseRequisition,
                purchaseRequisition.Procurement.SupplyMethodCode,
                ct);
        }

        if (oldStatus == request.Requisition.Status)
        {
            purchaseRequisition.AddActivity(new ActivityInfo(
                ActivityLogActionTypeConstant.Update,
                "อัปเดตข้อมูลใบขอซื้อขอจ้าง(จพ.004)",
                purchaseRequisition.Status.ToString()));
        }
        else if (request.Requisition.Status == PurchaseRequisitionStatus.WaitingApproval)
        {
            purchaseRequisition.AddActivity(new ActivityInfo(
                ActivityLogActionTypeConstant.SendApprove,
                "ส่งผู้มีอำนาจเห็นชอบ/อนุมัติ",
                purchaseRequisition.Status.ToString()));
        }

        await this.dbContext.SaveChangesAsync(ct);

        await this.UpdateScopeOfWorks(purchaseRequisition, request.ScopeOfWorks, ct);

        var acceptorUserIds = request.Acceptors.Select(a => UserId.From(a.UserId)).ToArray();

        var user = await this.dbContext.SuUsers
                             .Include(u => u.Employee)
                             .ThenInclude(s => s.View)
                             .Where(u => acceptorUserIds.Contains(u.Id))
                             .ToArrayAsync(ct);

        this.UpdateAcceptors(purchaseRequisition, request.Acceptors, request.Requisition.Status, user, UserId.From(request.UserId));
        await this.ManageAssigneeAsync(purchaseRequisition, request.Assignees, ct, UserId.From(request.UserId));

        if (request.Requisition.Status is PurchaseRequisitionStatus.Approved && oldStatus != PurchaseRequisitionStatus.Approved)
        {
            await SendNotificationAssigneesAsync(purchaseRequisition, ct);

            purchaseRequisition.Procurement.SetProcessType(ProcessType.Jp005);
            purchaseRequisition.Procurement.SetProcurementStep(purchaseRequisition.Procurement.Type, ProcurementStep.Procurement);
        }

        var isReplaceOnUpdate = request.Requisition.IsPurchaseRequisitionDocumentIdReplaced ?? false;
        await this.UpdateDocumentAsync(purchaseRequisition, isReplaceOnUpdate, false, ct);

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok(new UpdatePurchaseRequisitionResponse(newDocumentFileId?.Value));
    }

    private void ValidateDocument(UpdatePurchaseRequisitionRequest request, PpPurchaseRequisition? purchaseRequisition)
    {
        if (request.Requisition is { PurchaseRequisitionDocumentId: not null, Status: PurchaseRequisitionStatus.WaitingApproval } &&
            (purchaseRequisition != null && !purchaseRequisition.IsMigration.GetValueOrDefault(false) && !purchaseRequisition.DocumentHistories.Any()))
        {
            this.ThrowError("กรุณาจัดทำเอกสาร", StatusCodes.Status400BadRequest);
        }
    }

    private async ValueTask UpdateBudgets(
        PpPurchaseRequisition requisition,
        IEnumerable<UpdatePurchaseRequisitionBudget> budgets,
        CancellationToken ct)
    {
        // Delete removed budgets
        var updatePurchaseRequisitionBudgets =
            budgets as UpdatePurchaseRequisitionBudget[] ?? [.. budgets];

        var incomingBudgetIds = updatePurchaseRequisitionBudgets
                                .Where(b => b.Id.HasValue)
                                .Select(b => PpPurchaseRequisitionBudgetId.From(b.Id.Value))
                                .ToList();

        var existingBudgets = await this.dbContext.PpPurchaseRequisitionBudgets
                                        .Where(b => b.PpPurchaseRequisitionId == requisition.Id)
                                        .ToListAsync(ct);

        var budgetsToDelete = existingBudgets
                              .Where(existing => !incomingBudgetIds.Contains(existing.Id))
                              .ToList();

        this.dbContext.PpPurchaseRequisitionBudgets.RemoveRange(budgetsToDelete);

        // Create or update budgets
        foreach (var budgetDto in updatePurchaseRequisitionBudgets)
        {
            await this.CreateOrUpdateBudget(requisition, budgetDto, ct);
        }
    }

    private async ValueTask CreateOrUpdateBudget(
        PpPurchaseRequisition requisition,
        UpdatePurchaseRequisitionBudget budgetDto,
        CancellationToken ct)
    {
        if (budgetDto.Id.HasValue)
        {
            var existingBudget = await this.dbContext.PpPurchaseRequisitionBudgets
                                           .FirstOrDefaultAsync(b => b.Id == PpPurchaseRequisitionBudgetId.From(budgetDto.Id.Value), ct);

            if (existingBudget == null)
            {
                this.ThrowError($"Budget with ID {budgetDto.Id} not found.", StatusCodes.Status404NotFound);
            }

            existingBudget.Update(budgetDto.Description, budgetDto.BudgetAmount, budgetDto.Sequence);

            // Update budget details
            await this.UpdatePurchaseRequisitionBudgetDetails(existingBudget, budgetDto.Details, ct);

            return;
        }

        var newBudget = PpPurchaseRequisitionBudget.Create(
            requisition.Id,
            budgetDto.Description,
            budgetDto.BudgetAmount,
            budgetDto.Sequence);

        this.dbContext.PpPurchaseRequisitionBudgets.Add(newBudget);
        requisition.AddPpPurchaseRequisitionBudget(newBudget);

        // Add budget details for new budget
        await this.UpdatePurchaseRequisitionBudgetDetails(newBudget, budgetDto.Details, ct);
    }

    private async ValueTask UpdatePurchaseRequisitionBudgetDetails(
        PpPurchaseRequisitionBudget budget,
        IEnumerable<UpdatePurchaseRequisitionBudgetDetail> budgetDetails,
        CancellationToken ct)
    {
        // Delete removed budget details
        var updatePurchaseRequisitionBudgetDetails =
            budgetDetails as UpdatePurchaseRequisitionBudgetDetail[] ?? [.. budgetDetails];

        var incomingDetailIds = updatePurchaseRequisitionBudgetDetails
                                .Where(d => d.Id.HasValue)
                                .Select(d => PpPurchaseRequisitionBudgetDetailId.From(d.Id.Value))
                                .ToList();

        var existingDetails = await this.dbContext.PpPurchaseRequisitionBudgetDetails
                                        .Where(d => d.PpPurchaseRequisitionBudgetId == budget.Id)
                                        .ToListAsync(ct);

        var detailsToDelete = existingDetails
                              .Where(existing => !incomingDetailIds.Contains(existing.Id))
                              .ToList();

        this.dbContext.PpPurchaseRequisitionBudgetDetails.RemoveRange(detailsToDelete);

        // Create or update budget details
        foreach (var detailDto in updatePurchaseRequisitionBudgetDetails)
        {
            await this.CreateOrUpdatePurchaseRequisitionBudgetDetail(budget, detailDto, ct);
        }
    }

    private async ValueTask CreateOrUpdatePurchaseRequisitionBudgetDetail(
        PpPurchaseRequisitionBudget budget,
        UpdatePurchaseRequisitionBudgetDetail detailDto,
        CancellationToken ct)
    {
        if (detailDto.Id.HasValue)
        {
            var existingDetail = await this.dbContext.PpPurchaseRequisitionBudgetDetails
                                           .FirstOrDefaultAsync(d => d.Id == PpPurchaseRequisitionBudgetDetailId.From(detailDto.Id.Value), ct);

            if (existingDetail == null)
            {
                this.ThrowError($"Budget Detail with ID {detailDto.Id} not found.", StatusCodes.Status404NotFound);
            }

            existingDetail.Update(
                detailDto.Sequence,
                detailDto.Department,
                ParameterCode.From(detailDto.BudgetType),
                detailDto.ProjectCode,
                ParameterCode.From(detailDto.AccountNo),
                detailDto.Budget);

            return;
        }

        var newDetail = PpPurchaseRequisitionBudgetDetail.Create(
            budget.Id,
            detailDto.Sequence,
            detailDto.Department,
            ParameterCode.From(detailDto.BudgetType),
            detailDto.ProjectCode,
            ParameterCode.From(detailDto.AccountNo),
            detailDto.Budget);

        this.dbContext.PpPurchaseRequisitionBudgetDetails.Add(newDetail);
        budget.AddBudgetDetail(newDetail);
    }

    private async ValueTask UpdatePaymentTerms(
        PpPurchaseRequisition requisition,
        IEnumerable<UpdatePurchaseRequisitionPaymentTerm> paymentTerms,
        CancellationToken ct)
    {
        // Delete removed payment terms
        var incomingIds = paymentTerms
                          .Where(p => p.Id.HasValue)
                          .Select(p => PpPurchaseRequisitionPaymentTermId.From(p.Id.Value))
                          .ToList();

        var existingPaymentTerms = await this.dbContext.PpPurchaseRequisitionPaymentTerms
                                             .Where(p => p.PpPurchaseRequisitionId == requisition.Id)
                                             .ToListAsync(ct);

        var termsToDelete = existingPaymentTerms
                            .Where(existing => !incomingIds.Contains(existing.Id))
                            .ToList();

        this.dbContext.PpPurchaseRequisitionPaymentTerms.RemoveRange(termsToDelete);

        // Create or update payment terms
        foreach (var termDto in paymentTerms)
        {
            await this.CreateOrUpdatePaymentTerm(requisition, termDto, ct);
        }
    }

    private async ValueTask CreateOrUpdatePaymentTerm(
        PpPurchaseRequisition requisition,
        UpdatePurchaseRequisitionPaymentTerm termDto,
        CancellationToken ct)
    {
        if (termDto.Id.HasValue)
        {
            var existingTerm = await this.dbContext.PpPurchaseRequisitionPaymentTerms
                                         .FirstOrDefaultAsync(t => t.Id == PpPurchaseRequisitionPaymentTermId.From(termDto.Id.Value), ct);

            if (existingTerm == null)
            {
                this.ThrowError($"Payment Term with ID {termDto.Id} not found.", StatusCodes.Status404NotFound);
            }

            existingTerm.Update(termDto.TermNumber, termDto.Percent, termDto.Period, termDto.Description, termDto.IsMa, termDto.PaymentTypeCode, termDto.TotalPeriodTypeCode, termDto.TotalPeriod, termDto.PeriodTypeCode);

            return;
        }

        var newTerm = PpPurchaseRequisitionPaymentTerm.Create(
            requisition.Id,
            termDto.TermNumber,
            termDto.Percent,
            termDto.Period,
            termDto.Description,
            termDto.IsMa,
            termDto.PaymentTypeCode,
            termDto.TotalPeriodTypeCode,
            termDto.TotalPeriod,
            termDto.PeriodTypeCode);

        this.dbContext.PpPurchaseRequisitionPaymentTerms.Add(newTerm);
        requisition.AddPpPurchaseRequisitionPaymentTerm(newTerm);
    }

    private async ValueTask UpdateFineRates(
        PpPurchaseRequisition requisition,
        IEnumerable<UpdatePurchaseRequisitionFineRate> fineRates,
        CancellationToken ct)
    {
        // Delete removed fine rates
        var incomingIds = fineRates
                          .Where(f => f.Id.HasValue)
                          .Select(f => PpPurchaseRequisitionFineRateId.From(f.Id.Value))
                          .ToList();

        var existingFineRates = await this.dbContext.PpPurchaseRequisitionFineRates
                                          .Where(f => f.PpPurchaseRequisitionId == requisition.Id)
                                          .ToListAsync(ct);

        var ratesToDelete = existingFineRates
                            .Where(existing => !incomingIds.Contains(existing.Id))
                            .ToList();

        this.dbContext.PpPurchaseRequisitionFineRates.RemoveRange(ratesToDelete);

        // Create or update fine rates
        foreach (var rateDto in fineRates)
        {
            await this.CreateOrUpdateFineRate(requisition, rateDto, ct);
        }
    }

    private async ValueTask CreateOrUpdateFineRate(
        PpPurchaseRequisition requisition,
        UpdatePurchaseRequisitionFineRate rateDto,
        CancellationToken ct)
    {
        if (rateDto.Id.HasValue)
        {
            var existingRate = await this.dbContext.PpPurchaseRequisitionFineRates
                                         .FirstOrDefaultAsync(r => r.Id == PpPurchaseRequisitionFineRateId.From(rateDto.Id.Value), ct);

            if (existingRate == null)
            {
                this.ThrowError($"Fine Rate with ID {rateDto.Id} not found.", StatusCodes.Status404NotFound);
            }

            existingRate.Update(
                rateDto.Sequence,
                rateDto.Percentage,
                ParameterCode.From(rateDto.PeriodTypeCode),
                ParameterCode.From(rateDto.ConditionCode),
                rateDto.ConditionOther);

            return;
        }

        var newRate = PpPurchaseRequisitionFineRate.Create(
            requisition.Id,
            rateDto.Sequence,
            rateDto.Percentage,
            ParameterCode.From(rateDto.PeriodTypeCode),
            ParameterCode.From(rateDto.ConditionCode),
            rateDto.ConditionOther);

        this.dbContext.PpPurchaseRequisitionFineRates.Add(newRate);
        requisition.AddPpPurchaseRequisitionFineRate(newRate);
    }

    private async ValueTask UpdateCommittees(
        PpPurchaseRequisition requisition,
        IEnumerable<UpdatePurchaseRequisitionCommittee> committees,
        CancellationToken ct)
    {
        // Delete removed committees
        var incomingIds = committees
                          .Where(c => c.Id.HasValue)
                          .Select(c => PpPurchaseRequisitionCommitteeId.From(c.Id.Value))
                          .ToList();

        var existingCommittees = await this.dbContext.PpPurchaseRequisitionCommittees
                                           .Where(c => c.PpPurchaseRequisitionId == requisition.Id)
                                           .ToListAsync(ct);

        var committeesToDelete = existingCommittees
                                 .Where(existing => !incomingIds.Contains(existing.Id))
                                 .ToList();

        this.dbContext.PpPurchaseRequisitionCommittees.RemoveRange(committeesToDelete);

        // Create or update committees
        foreach (var committeeDto in committees)
        {
            await this.CreateOrUpdateCommittee(requisition, committeeDto, ct);
        }
    }

    private async Task CreateOrUpdateCommittee(
        PpPurchaseRequisition requisition,
        UpdatePurchaseRequisitionCommittee committeeDto,
        CancellationToken ct)
    {
        var committeePost = await this.dbContext.SuParameters
                                      .Where(w => w.Code == ParameterCode.From(committeeDto.CommitteePositionsCode))
                                      .FirstOrDefaultAsync(ct);

        if (committeePost is null)
        {
            this.ThrowError("ไม่พบตำแหน่งในคณะกรรมการในระบ", StatusCodes.Status404NotFound);
        }

        if (committeeDto.Id.HasValue)
        {
            var existingCommittee = await this.dbContext.PpPurchaseRequisitionCommittees
                                              .FirstOrDefaultAsync(c => c.Id == PpPurchaseRequisitionCommitteeId.From(committeeDto.Id.Value), ct);

            if (existingCommittee == null)
            {
                this.ThrowError($"Committee with ID {committeeDto.Id} not found.", StatusCodes.Status404NotFound);
            }

            existingCommittee.Update(
                committeePost.Code,
                committeePost.Label,
                committeeDto.Sequence);

            return;
        }

        var committeeUser = await this.dbContext.SuUsers
                                      .Include(u => u.Employee)
                                      .ThenInclude(e => e.View)
                                      .FirstOrDefaultAsync(u => u.Id == UserId.From(committeeDto.SuUserId), ct);

        var newCommittee = PpPurchaseRequisitionCommittee.Create(
            requisition.Id,
            committeeDto.GroupType,
            UserId.From(committeeDto.SuUserId),
            committeeDto.FullName,
            committeeUser?.Employee.View?.FullPositionName ?? string.Empty,
            committeePost.Code,
            committeePost.Label,
            committeeDto.Sequence);

        this.dbContext.PpPurchaseRequisitionCommittees.Add(newCommittee);
        requisition.AddPpPurchaseRequisitionCommittee(newCommittee);
    }

    private async ValueTask UpdateWarranties(
        PpPurchaseRequisition requisition,
        IEnumerable<UpdatePurchaseRequisitionWarranty> warranties,
        CancellationToken ct)
    {
        // Delete removed warranties
        var incomingIds = warranties
                          .Where(w => w.Id.HasValue)
                          .Select(w => PpPurchaseRequisitionWarrantyId.From(w.Id.Value))
                          .ToList();

        var existingWarranties = await this.dbContext.PpPurchaseRequisitionWarranties
                                           .Where(w => w.PpPurchaseRequisitionId == requisition.Id)
                                           .ToListAsync(ct);

        var warrantiesToDelete = existingWarranties
                                 .Where(existing => !incomingIds.Contains(existing.Id))
                                 .ToList();

        this.dbContext.PpPurchaseRequisitionWarranties.RemoveRange(warrantiesToDelete);

        // Create or update warranties
        foreach (var warrantyDto in warranties)
        {
            await this.CreateOrUpdateWarranty(requisition, warrantyDto, ct);
        }
    }

    private async ValueTask CreateOrUpdateWarranty(
        PpPurchaseRequisition requisition,
        UpdatePurchaseRequisitionWarranty warrantyDto,
        CancellationToken ct)
    {
        if (warrantyDto.Id.HasValue)
        {
            var existingWarranty = await this.dbContext.PpPurchaseRequisitionWarranties
                                             .FirstOrDefaultAsync(w => w.Id == PpPurchaseRequisitionWarrantyId.From(warrantyDto.Id.Value), ct);

            if (existingWarranty == null)
            {
                this.ThrowError($"Warranty with ID {warrantyDto.Id} not found.", StatusCodes.Status404NotFound);
            }

            existingWarranty.Update(
                warrantyDto.HasWarranty,
                warrantyDto.Period,
                warrantyDto.PeriodTypeCode != null ? ParameterCode.From(warrantyDto.PeriodTypeCode) : null,
                warrantyDto.ConditionOther);

            return;
        }

        var newWarranty = PpPurchaseRequisitionWarranty.Create(
            requisition.Id,
            warrantyDto.HasWarranty,
            warrantyDto.Period,
            warrantyDto.PeriodTypeCode != null ? ParameterCode.From(warrantyDto.PeriodTypeCode) : null,
            warrantyDto.ConditionOther);

        this.dbContext.PpPurchaseRequisitionWarranties.Add(newWarranty);
        requisition.AddPpPurchaseRequisitionWarranty(newWarranty);
    }

    private async ValueTask UpdateScopeOfWorks(
        PpPurchaseRequisition requisition,
        IEnumerable<UpdatePurchaseRequisitionTechnicalSpecification> technicalSpecifications,
        CancellationToken ct)
    {
        var incomingIds = technicalSpecifications
                          .Where(w => w.Id.HasValue)
                          .Select(w => PpPurchaseRequisitionTechnicalSpecificationsId.From(w.Id.Value))
                          .ToList();

        var existingTechnicalSpecifications = await this.dbContext.PpPurchaseRequisitionTechnicalSpecifications
                                                        .Where(w => w.PpPurchaseRequisitionId == requisition.Id)
                                                        .ToListAsync(ct);

        var technicalSpecificationsToDelete = existingTechnicalSpecifications
                                              .Where(existing => !incomingIds.Contains(existing.Id))
                                              .ToList();

        this.dbContext.PpPurchaseRequisitionTechnicalSpecifications.RemoveRange(technicalSpecificationsToDelete);

        foreach (var technicalSpecificationDto in technicalSpecifications)
        {
            await this.CreateOrUpdateScopeOfWorks(requisition, technicalSpecificationDto, ct);
        }
    }

    private async ValueTask CreateOrUpdateScopeOfWorks(
        PpPurchaseRequisition requisition,
        UpdatePurchaseRequisitionTechnicalSpecification technicalSpecification,
        CancellationToken ct)
    {
        if (technicalSpecification.Id.HasValue)
        {
            var existingTechnicalSpecification = await this.dbContext.PpPurchaseRequisitionTechnicalSpecifications
                                                           .FirstOrDefaultAsync(w => w.Id == PpPurchaseRequisitionTechnicalSpecificationsId.From(technicalSpecification.Id.Value), ct);

            if (existingTechnicalSpecification == null)
            {
                this.ThrowError($"Warranty with ID {technicalSpecification.Id} not found.", StatusCodes.Status404NotFound);
            }

            existingTechnicalSpecification.Update(
                technicalSpecification.Sequence,
                technicalSpecification.Name,
                technicalSpecification.Description,
                technicalSpecification.Quantity,
                technicalSpecification.UnitCode);

            return;
        }

        var newTechnicalSpecification = PpPurchaseRequisitionTechnicalSpecifications.Create(
            technicalSpecification.Sequence,
            technicalSpecification.Name,
            technicalSpecification.Description,
            technicalSpecification.Quantity,
            technicalSpecification.UnitCode,
            requisition.Id);

        this.dbContext.PpPurchaseRequisitionTechnicalSpecifications.Add(newTechnicalSpecification);
        requisition.AddPpPurchaseRequisitionTechnicalSpecification(newTechnicalSpecification);
    }

    private void UpdateAcceptors(
        PpPurchaseRequisition entity,
        IEnumerable<PpPurchaseRequisitionAcceptorResponse>? acceptors,
        PurchaseRequisitionStatus status,
        SuUser[] users,
        UserId sendToAcceptorId)
    {
        if (acceptors == null)
        {
            return;
        }

        // Get a mutable copy of the read-only collection
        var acceptorsList = entity.Acceptors.ToList();

        // Remove unmatched acceptors
        var toRemove = entity.Acceptors
                             .Where(x => acceptors.All(r => x.Id != AcceptorId.From(r.Id)))
                             .ToList();

        foreach (var item in toRemove)
        {
            entity.RemoveAcceptorById(item.Id);
        }

        AddOrUpdateAcceptors(entity, acceptorsList, status, acceptors, users, entity.Procurement.DepartmentId, sendToAcceptorId);

        // Re-evaluate IsCurrent flags based on sequence when entering WaitingApproval
        if (status == PurchaseRequisitionStatus.WaitingApproval)
        {
            var activeApprovers = acceptorsList
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

            var firstPending = activeApprovers.Select(DelegatorExtensions.DelegatorToAcceptor)
                                              .FirstOrDefault(a => a.Status == AcceptorStatus.Pending && a.IsCurrent);

            if (firstPending != null)
            {
                foreach (var targetUserId in firstPending.GetNotificationTargets())
                {
                    _ = SendNotificationAsync(
                        entity,
                        targetUserId,
                        NotificationConstant.WaitForLike.Title,
                        string.Format(NotificationConstant.WaitForLike.Message, ProgramConstant.PreProcurementJorPor04.Name, entity.PurchaseRequisitionNumber));
                }
            }
        }
        else
        {
            // Clear IsCurrent when not in WaitingApproval state
            foreach (var a in acceptorsList)
            {
                a.SetCurrent(false);
            }
        }
    }

    private static void AddOrUpdateAcceptors(
        PpPurchaseRequisition entity,
        List<PpPurchaseRequisitionAcceptors> acceptorsList,
        PurchaseRequisitionStatus status,
        IEnumerable<PpPurchaseRequisitionAcceptorResponse> acceptors,
        SuUser[] users,
        BusinessUnitId workBusinessUnitId,
        UserId sendToAcceptorId)
    {
        var userAcceptor = acceptors
            .Join(
                users,
                a => UserId.From(a.UserId),
                u => u.Id,
                (a, u) => new { a, u });

        // Add or update acceptors
        foreach (var acceptorDto in userAcceptor)
        {
            var existingAcceptor = acceptorsList.FirstOrDefault(x => x.Id == AcceptorId.From(acceptorDto.a.Id));

            if (existingAcceptor == null)
            {
                var newAcceptor = PpPurchaseRequisitionAcceptors.Create(
                    entity.Id,
                    new PpPurchaseRequisitionAcceptorInfoData(
                        acceptorDto.a.AcceptorType,
                        acceptorDto.u.Id,
                        acceptorDto.u.EmployeeCode,
                        acceptorDto.u.Employee.View?.FullName ?? string.Empty,
                        acceptorDto.a.PositionName,
                        acceptorDto.u.Employee.ConvertPositionName(workBusinessUnitId) ?? string.Empty,
                        acceptorDto.a.Sequence),
                    status);

                newAcceptor.SetSendToAcceptorId(sendToAcceptorId);
                acceptorsList.Add(newAcceptor);
                entity.AddPpPurchaseRequisitionAcceptor(newAcceptor);
            }
            else
            {
                var acceptorStatus =
                    status == PurchaseRequisitionStatus.WaitingApproval
                        ? AcceptorStatus.Pending
                        : acceptorDto.a.Status;

                existingAcceptor.Update(
                    new PpPurchaseRequisitionAcceptorInfoData(
                        acceptorDto.a.AcceptorType,
                        acceptorDto.u.Id,
                        acceptorDto.u.EmployeeCode,
                        acceptorDto.u.FullName,
                        acceptorDto.u.Employee.ConvertPositionName(workBusinessUnitId),
                        acceptorDto.u.Employee.View?.BusinessUnitName ?? string.Empty,
                        acceptorDto.a.Sequence),
                    acceptorStatus);

                existingAcceptor.SetSendToAcceptorId(sendToAcceptorId);
            }
        }
    }

    private async Task ManageAssigneeAsync(
        PpPurchaseRequisition purchaseRequisition,
        IEnumerable<AssigneeRequest> requestsAssignee,
        CancellationToken ct,
        UserId? sendToAcceptorId = null)
    {
        _ = purchaseRequisition.Assignees
                               .ExceptBy(
                                   requestsAssignee
                                       .Where(w => w.Id.HasValue)
                                       .Select(s => s.Id.Value),
                                   a => a.Id.Value)
                               .Iter(r => purchaseRequisition.RemoveAssigneeById(r.Id));

        var lastAssigneeUserId = requestsAssignee
            .Where(a => a.AssigneeType == AssigneeType.Assignee)
            .OrderByDescending(a => a.Sequence)
            .Select(a => (UserId?)UserId.From(a.UserId))
            .FirstOrDefault();

        var resolvedSendToAcceptorId = lastAssigneeUserId ?? sendToAcceptorId;

        _ = requestsAssignee.Where(w => w.Id.HasValue)
                            .Join(
                                purchaseRequisition.Assignees,
                                db => db.Id.Value,
                                payload => payload.Id.Value,
                                (payload, db) => new { db, payload })
                            .Iter(r =>
                            {
                                r.db.SetSequence(r.payload.Sequence);
                                r.db.SetSendToAcceptorId(resolvedSendToAcceptorId);
                            });

        var assigneeIds = requestsAssignee
                          .Where(w => !w.Id.HasValue)
                          .Select(s => UserId.From(s.UserId))
                          .ToArray();

        var userData = await this.dbContext.SuUsers
                                 .Include(e => e.Employee)
                                 .ThenInclude(v => v.View)
                                 .Where(w => assigneeIds.Contains(w.Id))
                                 .ToArrayAsync(ct);

        this.ValidateUsers(userData, assigneeIds);

        var newAssignees = requestsAssignee.Where(w => !w.Id.HasValue && w.AssigneeType == AssigneeType.Assignee).ToList();

        foreach (var inComing in newAssignees)
        {
            await SendNotificationAsync(
                purchaseRequisition,
                UserId.From(inComing.UserId),
                NotificationConstant.Assignment.Title,
                string.Format(NotificationConstant.Assignment.Message, ProgramConstant.PreProcurementJorPor04.Name, purchaseRequisition.PurchaseRequisitionNumber));
        }

        this.AddAssignee(
            purchaseRequisition,
            requestsAssignee.Where(w => !w.Id.HasValue).ToArray(),
            userData,
            sendToAcceptorId);
    }

    private static async Task SendNotificationAsync(PpPurchaseRequisition purchaseRequisition, UserId userId, string title, string message)
    {
        await Notification
              .Crate(
                  userId,
                  title,
                  message,
                  NotificationProgram.Procurement)
              .SetReferenceId(purchaseRequisition.Id.Value)
              .SetLinkUrl(string.Format(ProgramConstant.Procurement.Url, purchaseRequisition.Procurement.Id), "ดูรายละเอียด")
              .PublishAsync(CancellationToken.None);
    }

    private static async Task SendNotificationAssigneesAsync(PpPurchaseRequisition purchaseRequisition, CancellationToken ct)
    {
        foreach (var targetUserId in purchaseRequisition.Assignees.Where(x => x.Type != AssigneeType.Director).SelectMany(a => a.GetAssigneeNotificationTargets()))
        {
            await Notification
                  .Crate(
                      targetUserId,
                      NotificationConstant.Assignment.Title,
                      string.Format(NotificationConstant.Assignment.Message, ProgramConstant.PreProcurementJorPor04.Name, purchaseRequisition.PurchaseRequisitionNumber),
                      NotificationProgram.Procurement)
                  .SetReferenceId(purchaseRequisition.Id.Value)
                  .SetLinkUrl(
                      string.Format(ProgramConstant.Procurement.Url, purchaseRequisition.Procurement.Id),
                      "ดูรายละเอียด")
                  .PublishAsync(ct);
        }
    }
}