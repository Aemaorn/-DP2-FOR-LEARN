namespace GHB.DP2.Application.Features.Procurement.PrincipleApprovalRental;

using Codehard.FileService.Client.Abstractions;
using FluentValidation;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Validators;
using GHB.DP2.Application.Features.Procurement.Jp006.Dto;
using GHB.DP2.Application.Features.Procurement.PrincipleApprovalRental.Abstact;
using GHB.DP2.Application.Features.Procurement.PrincipleApprovalRental.Dto;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PPrincipleApprovalRental;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;

public record CreatePrincipleApprovalRentalRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid ProcurementId,
    UseContractType UseContract,
    PPrincipleApprovalRentalStatus Status,
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
    decimal? AnalysisSummaryNpv,
    decimal? AnalysisSummaryPaybackYearPeriod,
    decimal? AnalysisSummaryDiscountedPaybackYearPeriod,
    string? PhoneNumber,
    IEnumerable<AcceptorRequest>? Acceptors,
    IEnumerable<AssigneeRequest>? Assignees,
    PerfSupportDataRequest? PerfSupportData,
    IEnumerable<PerfSupportDataDetailsRequest>? PerfSupportDataDetails,
    IEnumerable<RoiLoanAndDepositSummaryRequest>? RoiLoanAndDepositSummaries,
    IEnumerable<RoiPerfResultRequest>? RoiPerfResults,
    IEnumerable<BudgetRequest>? Budgets,
    IEnumerable<RentalAnalysisRequest>? RentalAnalysis,
    IEnumerable<EntrepreneursRequest>? Entrepreneurs,
    AttachmentsWithOutTypeDto? ComparingAttachments,
    DateTimeOffset? DocumentDate = null);

public class Validator : FastEndpoints.Validator<CreatePrincipleApprovalRentalRequest>
{
    public Validator()
    {
        this.RuleFor(x => x.ProcurementId).NotEmpty();
        this.RuleFor(x => x.Status).IsInEnum();
        this.RuleFor(x => x.ProcurementId).NotNull();

        this.When(x => x.ComparingAttachments is not null, () =>
            this.RuleForEach(x => x.ComparingAttachments!.FileAttachments)
                .ChildRules(f => f.RuleFor(x => x.FileName).MustBeValidFileExtension()));
    }
}

public class CreatePrincipleApprovalRentalEndpoint : PrincipleApprovalRentalEndpointBase<CreatePrincipleApprovalRentalRequest, Created<Guid>>
{
    private readonly Dp2DbContext dbContext;

    public CreatePrincipleApprovalRentalEndpoint(
        ILogger<CreatePrincipleApprovalRentalEndpoint> logger,
        Dp2DbContext dbContext,
        IFileServiceClient fileServiceClient)
        : base(logger, dbContext, fileServiceClient)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Post("procurement/{ProcurementId:guid}/principle-approval-rental");
        this.Description(b => b
                              .WithTags("Procurement/PrincipleApprovalRental")
                              .WithName("CreatePrincipleApprovalRental")
                              .Produces<Created<Guid>>(StatusCodes.Status201Created)
                              .ProducesProblem(StatusCodes.Status400BadRequest)
                              .ProducesProblem(StatusCodes.Status500InternalServerError));
    }

    public async Task<Guid> CreateEntityAsync(CreatePrincipleApprovalRentalRequest req, CancellationToken ct)
    {
        var procurement = await this.GetProcurementAsync(req, ct);

        var ppPrincipleApproval = procurement.PrincipleApprovals
                                             .FirstOrDefault(a => a.IsDeleted == false);

        if (ppPrincipleApproval is null)
        {
            this.ThrowError($"PPrincipleApproval not found", StatusCodes.Status400BadRequest);
        }

        var entity =
            PPrincipleApprovalRental.Create(
                                        ProcurementId.From(req.ProcurementId))
                                    .SetUseContract(req.UseContract)
                                    .SetRentalInfo(
                                        req.BranchLocation,
                                        ParameterCode.From(req.RentTypeCode),
                                        req.RentalStartDate,
                                        req.RentalEndDate,
                                        req.RentalDurationYear,
                                        req.RentalDurationMonth,
                                        req.RentalDurationDay,
                                        req.MaxMonthlyRent,
                                        req.TotalRentalAmount,
                                        req.ExpectedContractDate,
                                        req.RentalLocationDetails,
                                        req.SubDistrictCode,
                                        req.SubDistrictName,
                                        req.DistrictCode,
                                        req.DistrictName,
                                        req.ProvinceCode,
                                        req.ProvinceName,
                                        req.PhoneNumber);

        if (req.DocumentDate is not null)
        {
            entity.SetDocumentDate(req.DocumentDate);
        }

        if (req.PerfSupportData is not null)
        {
            var data = PPrincipleApprovalRentalConsoPerfSupportData.Create(
                req.PerfSupportData.TransactionVolume,
                req.PerfSupportData.ActivityDescription,
                req.PerfSupportData.PeriodYear,
                req.PerfSupportData.StartMonth,
                req.PerfSupportData.EndMonth);
            entity.SetPerfSupportData(data);
        }

        if (req.ReferencePriceAmount.HasValue)
        {
            entity.SetReferencePrice(req.ReferencePriceAmount.Value);
        }

        if (req.AnalysisSummaryNpv.HasValue)
        {
            entity.SetAnalysisInfo(
                req.AnalysisSummaryNpv,
                req.AnalysisSummaryPaybackYearPeriod,
                req.AnalysisSummaryDiscountedPaybackYearPeriod);
        }

        if (req.Acceptors != null)
        {
            await this.UpsertAcceptors(entity, req.Acceptors, req.Status, ct, UserId.From(req.UserId));
        }

        if (req.Assignees != null)
        {
            await this.UpsertAssignee(entity, req.Assignees, UserId.From(req.UserId), ct);
        }

        if (req.PerfSupportDataDetails != null)
        {
            this.UpsertPerfSupportDataDetails(entity, req.PerfSupportDataDetails);
        }

        if (req.RoiLoanAndDepositSummaries != null)
        {
            this.UpsertRoiLoanAndDepositSummaries(entity, req.RoiLoanAndDepositSummaries);
        }

        if (req.RoiPerfResults != null)
        {
            this.UpsertRoiPerfResults(entity, req.RoiPerfResults);
        }

        if (req.Budgets != null)
        {
            this.UpsertBudgets(entity, req.Budgets);
        }

        if (req.RentalAnalysis != null)
        {
            this.UpsertRentalAnalysis(entity, req.RentalAnalysis);
        }

        if (req.Entrepreneurs != null)
        {
            this.UpsertEntrepreneurs(entity, req.Entrepreneurs);

            _ = await req.Entrepreneurs.Select(async x =>
            {
                var entrepreneursData = x.Id.HasValue
                    ? entity.Entrepreneurs.FirstOrDefault(e => e.Id == PPrincipleApprovalRentalEntrepreneursId.From(x.Id.Value))
                    : null;

                if (entrepreneursData is null)
                {
                    return unit;
                }

                if (x.Attachments is not null && x.Attachments.Any())
                {
                    await this.ValidateDocumentTypeCode(x.Attachments, ct);
                    await this.UpsertAttachments(entity, entrepreneursData, x.Attachments);
                }

                return unit;
            }).SequenceSerial();
        }

        if (req.ComparingAttachments != null)
        {
            await this.UpsertComparingAttachments(entity, (IEnumerable<ComparingAttachmentsDto>)req.ComparingAttachments);
        }

        await this.SetDefaultDocumentTemplate(
            entity,
            ppPrincipleApproval.RentTypeCode);

        this.dbContext.PPrincipleApprovalRentals.Add(entity);

        await this.ReplaceDocumentsAsync(entity, ppPrincipleApproval, ct, false);

        await this.dbContext.SaveChangesAsync(CancellationToken.None);

        return entity.Id.Value;
    }

    protected override async ValueTask<Created<Guid>> HandleRequestAsync(CreatePrincipleApprovalRentalRequest req, CancellationToken ct)
    {
        var id = await this.CreateEntityAsync(req, ct);

        return TypedResults.Created(string.Empty, id);
    }

    private async Task<Procurement> GetProcurementAsync(CreatePrincipleApprovalRentalRequest req, CancellationToken ct)
    {
        var procurement = await this.dbContext.Procurements
                                    .Include(a => a.PrincipleApprovals)
                                    .SingleOrDefaultAsync(p => p.Id == ProcurementId.From(req.ProcurementId), ct);

        if (procurement is null)
        {
            this.ThrowError($"ไม่พบข้อมูลโครงการที่มีรหัส {req.ProcurementId}", StatusCodes.Status404NotFound);
        }

        return procurement;
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