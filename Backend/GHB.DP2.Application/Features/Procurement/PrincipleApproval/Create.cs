namespace GHB.DP2.Application.Features.Procurement.PrincipleApproval;

using FluentValidation;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Validators;
using GHB.DP2.Application.Features.Procurement.Invite;
using GHB.DP2.Application.Features.Procurement.PrincipleApproval.Abstract;
using GHB.DP2.Application.Features.Procurement.PrincipleApproval.Dto;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PPrincipleApproval;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;

public record CreatePrincipleApprovalRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid ProcurementId,
    string BranchLocation,
    string RentTypeCode,
    DateTimeOffset RentalStartDate,
    DateTimeOffset RentalEndDate,
    int RentalDurationYear,
    int RentalDurationMonth,
    int RentalDurationDay,
    decimal MaxMonthlyRent,
    decimal TotalRentalAmount,
    DateTimeOffset ExpectedContractDate,
    string RentalLocationDetails,
    string SubDistrictCode,
    string SubDistrictName,
    string DistrictCode,
    string DistrictName,
    string ProvinceCode,
    string ProvinceName,
    decimal? ReferencePriceAmount,
    decimal? BudgetYear,
    string? Branch,
    decimal? OperationExpense,
    decimal? AnalysisSummaryNpv,
    decimal? AnalysisSummaryPaybackYearPeriod,
    decimal? AnalysisSummaryDiscountedPaybackYearPeriod,
    string? PhoneNumber,
    PPrincipleApprovalStatus Status,
    IEnumerable<AcceptorRequest> Acceptors,
    IEnumerable<AssigneeRequest> Assignees,
    IEnumerable<CommitteeRequest> Committees,
    PerfSupportDataRequest? PerfSupportData,
    IEnumerable<PerfSupportDataDetailsRequest> PerfSupportDataDetails,
    IEnumerable<RoiLoanAndDepositSummaryRequest> RoiLoanAndDepositSummaries,
    IEnumerable<RoiPerfResultRequest> RoiPerfResults,
    IEnumerable<BudgetRequest> Budgets,
    IEnumerable<RentalAnalysisRequest> RentalAnalyses,
    bool IsRentCommittee,
    bool IsAcceptanceCommittee,
    IEnumerable<EmailAttachment> Attachments,
    DateTimeOffset? DocumentDate = null);

public class Validator : Validator<CreatePrincipleApprovalRequest>
{
    public Validator()
    {
        this.RuleFor(x => x.ProcurementId).NotEmpty();
        this.RuleFor(x => x.RentTypeCode).NotEmpty();
        this.RuleFor(x => x.Status).IsInEnum();
        this.RuleFor(x => x.BranchLocation).NotEmpty();
        this.RuleFor(x => x.RentalStartDate).NotEmpty();
        this.RuleFor(x => x.RentalEndDate).NotEmpty();

        this.RuleForEach(x => x.Attachments)
            .ChildRules(a => a.RuleFor(x => x.FileName).MustBeValidFileExtension());
    }
}

public class CreatePrincipleApprovalEndpoint : PrincipleApprovalEndpointBase<CreatePrincipleApprovalRequest, Created<Guid>>
{
    private readonly Dp2DbContext dbContext;

    public CreatePrincipleApprovalEndpoint(ILogger<CreatePrincipleApprovalEndpoint> logger, Dp2DbContext dbContext)
        : base(logger, dbContext)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Post("procurement/{ProcurementId:guid}/principle-approval");
        this.Description(b => b
                              .WithTags("Procurement/PrincipleApproval")
                              .WithName("CreatePrincipleApproval")
                              .AllowAnonymous()
                              .Produces<Created<Guid>>(StatusCodes.Status201Created)
                              .ProducesProblem(StatusCodes.Status400BadRequest)
                              .ProducesProblem(StatusCodes.Status500InternalServerError));
    }

    protected override async ValueTask<Created<Guid>> HandleRequestAsync(CreatePrincipleApprovalRequest req, CancellationToken ct)
    {
        await this.ValidateRequestAsync(req, ct);

        var entity = PPrincipleApproval.Create(
            ProcurementId.From(req.ProcurementId),
            req.BranchLocation,
            ParameterCode.From(req.RentTypeCode),
            req.RentalStartDate,
            req.RentalEndDate,
            req.Status);

        entity.SetRentalInfo(
            req.RentalDurationYear,
            req.RentalDurationMonth,
            req.RentalDurationDay,
            req.MaxMonthlyRent,
            req.TotalRentalAmount,
            req.ExpectedContractDate);

        entity.SetLocationInfo(
            req.RentalLocationDetails,
            req.SubDistrictCode,
            req.SubDistrictName,
            req.DistrictCode,
            req.DistrictName,
            req.ProvinceCode,
            req.ProvinceName);

        entity.SetPhoneNumber(
            req.PhoneNumber);

        entity.SetIsRentalCommittee(req.IsRentCommittee);
        entity.SetIsAcceptanceCommittee(req.IsAcceptanceCommittee);

        if (req.DocumentDate is not null)
        {
            entity.SetDocumentDate(req.DocumentDate);
        }

        if (req.PerfSupportData is not null)
        {
            var data = PPrincipleApprovalConsoPerfSupportData.Create(
                req.PerfSupportData.TransactionVolume,
                req.PerfSupportData.ActivityDescription,
                req.PerfSupportData.PeriodYear,
                req.PerfSupportData.StartMonth,
                req.PerfSupportData.EndMonth);

            entity.SetPerfSupportData(data);
        }

        entity.SetAnalysisInfo(
            req.AnalysisSummaryNpv,
            req.AnalysisSummaryPaybackYearPeriod,
            req.AnalysisSummaryDiscountedPaybackYearPeriod);

        if (req.ReferencePriceAmount.HasValue)
        {
            entity.SetReferencePrice(req.ReferencePriceAmount.Value);
        }

        if (req.Acceptors.Any())
        {
            await this.UpsertAcceptors(entity, req.Acceptors, req.Status, ct, UserId.From(req.UserId));
        }

        if (req.Assignees.Any())
        {
            await this.UpsertAssignee(entity, req.Assignees, UserId.From(req.UserId), ct);
        }

        if (req.Committees.Any())
        {
            await this.UpsertCommittees(entity, req.Committees, ct);
        }

        if (req.PerfSupportDataDetails.Any())
        {
            this.UpsertPerfSupportDataDetails(entity, req.PerfSupportDataDetails);
        }

        if (req.RoiLoanAndDepositSummaries.Any())
        {
            this.UpsertRoiLoanAndDepositSummaries(entity, req.RoiLoanAndDepositSummaries);
        }

        if (req.RoiPerfResults.Any())
        {
            this.UpsertRoiPerfResults(entity, req.RoiPerfResults);
        }

        if (req.Budgets.Any())
        {
            this.UpsertBudgets(entity, req.Budgets);
        }

        if (req.RentalAnalyses.Any())
        {
            this.UpsertRentalAnalysis(entity, req.RentalAnalyses);
        }

        if (req.Attachments.Any())
        {
            var attachments = req.Attachments
                                 .Select(a =>
                                     PPrincipleApprovalAttachment.Create(
                                         a.FileId,
                                         a.FileName,
                                         a.Sequence));

            foreach (var attachment in attachments)
            {
                entity.AddAttachment(attachment);
            }
        }

        await this.SetDocumentTemplate(entity, ct);

        await this.AddDefaultDocumentToHistory(entity, ct);

        this.dbContext.PPrincipleApprovals.Add(entity);

        await this.ReplaceDocumentsAsync(entity, false, false, ct);

        await this.dbContext.SaveChangesAsync(CancellationToken.None);

        return TypedResults.Created(string.Empty, entity.Id.Value);
    }

    private async Task ValidateRequestAsync(CreatePrincipleApprovalRequest req, CancellationToken ct)
    {
        var procurement = await this.dbContext.Procurements.SingleOrDefaultAsync(
            p => p.Id == ProcurementId.From(req.ProcurementId), ct);

        if (procurement is null)
        {
            this.ThrowError($"ไม่พบข้อมูลโครงการที่มีรหัส {req.ProcurementId}", StatusCodes.Status404NotFound);
        }
    }
}