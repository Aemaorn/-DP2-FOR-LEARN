namespace GHB.DP2.Application.Features.Procurement.PrincipleApprovalRental;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client.Abstractions;
using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Constants;
using FluentValidation;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Validators;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.Procurement.PrincipleApprovalRental.Abstact;
using GHB.DP2.Application.Features.Procurement.PrincipleApprovalRental.Dto;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PPrincipleApproval;
using GHB.DP2.Domain.Procurement.PPrincipleApprovalRental;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record UpdatePrincipleApprovalRentalResponse(Guid? NewApprovalDocumentFileId, Guid? NewWinnerDocumentFileId);

public record UpdatePrincipleApprovalRentalRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid Id,
    Guid ProcurementId,
    UseContractType UseContract,
    PPrincipleApprovalRentalStatus Status,
    Guid? DocumentId,
    bool? IsDocumentIdReplaced,
    bool? IsDocumentReplace,
    Guid? WinnerDocumentId,
    bool? IsWinnerDocumentIdReplaced,
    bool? IsWinnerDocumentReplace,
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
    IEnumerable<ComparingAttachmentsDto>? ComparingAttachments,
    DateTimeOffset? DocumentDate = null);

public class UpdatePrincipleApprovalRentalRequestValidator : Validator<UpdatePrincipleApprovalRentalRequest>
{
    public UpdatePrincipleApprovalRentalRequestValidator()
    {
        this.RuleForEach(x => x.ComparingAttachments)
            .ChildRules(a => a.RuleFor(x => x.FileName).MustBeValidFileExtension())
            .When(x => x.ComparingAttachments is not null);
    }
}

public class UpdatePrincipleApprovalRentalEndpoint : PrincipleApprovalRentalEndpointBase<UpdatePrincipleApprovalRentalRequest, Results<Ok<UpdatePrincipleApprovalRentalResponse>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public UpdatePrincipleApprovalRentalEndpoint(
        ILogger<UpdatePrincipleApprovalRentalEndpoint> logger,
        Dp2DbContext dbContext,
        IFileServiceClient fileServiceClient)
        : base(logger, dbContext, fileServiceClient)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Put("procurement/{procurementId:guid}/principle-approval-rental/{id:guid}");
        this.Description(b => b
                              .WithTags("Procurement/PrincipleApprovalRental")
                              .WithName("UpdatePrincipleApprovalRental")
                              .Produces<Created<Guid>>(StatusCodes.Status201Created)
                              .ProducesProblem(StatusCodes.Status400BadRequest)
                              .ProducesProblem(StatusCodes.Status500InternalServerError));
    }

    protected override async ValueTask<Results<Ok<UpdatePrincipleApprovalRentalResponse>, NotFound<string>>> HandleRequestAsync(UpdatePrincipleApprovalRentalRequest req, CancellationToken ct)
    {
        var entity = await this.dbContext.PPrincipleApprovalRentals
                               .Include(pPrincipleApprovalRental => pPrincipleApprovalRental.Procurement)
                               .ThenInclude(p => p.PrincipleApprovals)
                               .Include(pPrincipleApprovalRental => pPrincipleApprovalRental.PerfSupportData)
                               .Include(pPrincipleApprovalRental => pPrincipleApprovalRental.Acceptors)
                               .Include(x => x.Entrepreneurs)
                               .ThenInclude(x => x.EntrepreneursShareholders)
                               .ThenInclude(x => x.Checkers)
                               .Include(x => x.Entrepreneurs)
                               .ThenInclude(x => x.Checkers)
                               .Include(x => x.Entrepreneurs)
                               .ThenInclude(x => x.EntrepreneursPriceDetails)
                               .Include(x => x.Entrepreneurs)
                               .ThenInclude(x => x.Vendor)
                               .ThenInclude(x => x.EntrepreneurTypeInfo)
                               .Include(x => x.DocumentHistories)
                               .AsSplitQuery()
                               .SingleOrDefaultAsync(x => x.Id == PPrincipleApprovalRentalId.From(req.Id), cancellationToken: ct);

        this.ValidateDocument(req, entity);

        if (entity is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลอนุมัตเช่าที่มีรหัส " + req.Id);
        }

        var previousStatus = entity.Status;

        await this.UpdateEntityAsync(entity, req, ct);

        entity.SetUseContract(req.UseContract)
              .SetStatus(req.Status);

        var isDocumentReplaced = req.IsDocumentReplace ?? false;
        var isWinnerDocumentReplaced = req.IsWinnerDocumentReplace ?? false;

        FileId? newApprovalDocumentFileId = null;
        FileId? newWinnerDocumentFileId = null;

        var mustSaveApprovalDocument =
            req.DocumentId.HasValue &&
            req.Status != PPrincipleApprovalRentalStatus.WaitingCommitteeApproval &&
            isDocumentReplaced;

        if (mustSaveApprovalDocument)
        {
            newApprovalDocumentFileId = await this.UpdateDocumentHistoryAsync(
                entity,
                PPrincipleApprovalRentalDocumentType.Approval,
                FileId.From(req.DocumentId!.Value),
                isDocumentReplaced,
                ct);
        }

        var mustSaveWinnerDocument =
            req.WinnerDocumentId.HasValue &&
            req.Status != PPrincipleApprovalRentalStatus.WaitingCommitteeApproval &&
            isWinnerDocumentReplaced;

        if (mustSaveWinnerDocument)
        {
            newWinnerDocumentFileId = await this.UpdateDocumentHistoryAsync(
                entity,
                PPrincipleApprovalRentalDocumentType.Winner,
                FileId.From(req.WinnerDocumentId!.Value),
                isWinnerDocumentReplaced,
                ct);
        }

        switch (req.Status)
        {
            case PPrincipleApprovalRentalStatus.Edit:
                entity.AddActivity(new ActivityInfo(
                    ActivityLogActionTypeConstant.Recall,
                    ActivityLogActionTypeConstant.Recall,
                    req.Status.ToString()));

                break;

            case PPrincipleApprovalRentalStatus.WaitingCommitteeApproval:
                entity.AddActivity(
                    new ActivityInfo(
                        ActivityLogActionTypeConstant.SendCommitteeApprove,
                        ActivityLogActionTypeConstant.SendCommitteeApprove,
                        entity.Status.ToString()));

                await SendNotificationAcceptorRentCommitteeAsync(entity, ct);

                break;

            case PPrincipleApprovalRentalStatus.WaitingComment:
                if (previousStatus != PPrincipleApprovalRentalStatus.WaitingComment)
                {
                    _ = SendNotificationAssigneeAsync(entity, CancellationToken.None);
                }

                break;

            case PPrincipleApprovalRentalStatus.WaitingAcceptance:
                entity.SetUseContract(req.UseContract)
                      .SetWaitingAcceptance();

                var approvers = entity.Acceptors
                                      .Where(p => p.Type == AcceptorType.Approver)
                                      .OrderBy(a => a.Sequence)
                                      .ToList();

                var firstPending = approvers.FirstOrDefault(a => a.Status == AcceptorStatus.Pending && a.IsCurrent);

                if (firstPending != null)
                {
                    foreach (var targetUserId in firstPending.GetNotificationTargets())
                    {
                        _ = SendNotificationAsync(
                            entity,
                            targetUserId,
                            NotificationConstant.WaitForLike.Title,
                            string.Format(NotificationConstant.WaitForLike.Message, ProgramConstant.PrincipalApprovalRental.Name, entity.Procurement.ProcurementNumber));
                    }
                }

                break;

            case PPrincipleApprovalRentalStatus.ContractAssigned:
                entity.AddActivity(new ActivityInfo(
                    ActivityLogActionTypeConstant.Assigned,
                    ActivityLogActionTypeConstant.Assigned,
                    req.Status.ToString()));

                entity.Procurement.SetProcessType(ProcessType.PurchaseOrderApproval);

                await SendNotificationAssigneeContractAsync(entity, ct);

                break;

            default:
                entity.AddActivity(new ActivityInfo(
                    ActivityLogActionTypeConstant.Update,
                    ActivityLogActionTypeConstant.Update,
                    req.Status.ToString()));

                break;
        }

        if (req.Status == PPrincipleApprovalRentalStatus.WaitingCommitteeApproval || req.DocumentDate is not null)
        {
            entity.SetDocumentDate(req.DocumentDate);
        }

        if (entity.Status != PPrincipleApprovalRentalStatus.WaitingAssign
           && entity.Status != PPrincipleApprovalRentalStatus.WaitingContractAssign
            && entity.Status != PPrincipleApprovalRentalStatus.ContractAssigned
             && entity.Status != PPrincipleApprovalRentalStatus.WaitingComment)
        {
            await this.UpdateAndReplaceDocumentAsync(
                entity,
                entity.Procurement.PrincipleApprovals.FirstOrDefault()!,
                isDocumentReplaced,
                isWinnerDocumentReplaced,
                ct,
                UserId.From(req.UserId));
        }

        this.dbContext.PPrincipleApprovalRentals.Update(entity);
        await this.dbContext.SaveChangesAsync(CancellationToken.None);

        return TypedResults.Ok(new UpdatePrincipleApprovalRentalResponse(
            newApprovalDocumentFileId?.Value,
            newWinnerDocumentFileId?.Value));
    }

    private void ValidateDocument(UpdatePrincipleApprovalRentalRequest req, PPrincipleApprovalRental? entity)
    {
        if (req.Status == PPrincipleApprovalRentalStatus.WaitingCommitteeApproval &&
            (req.DocumentId is not null || req.WinnerDocumentId is not null) &&
            (entity != null && !entity.DocumentHistories.Any()))
        {
            this.ThrowError("กรุณาจัดทำเอกสาร", StatusCodes.Status400BadRequest);
        }
    }

    private async Task UpdateEntityAsync(PPrincipleApprovalRental entity, UpdatePrincipleApprovalRentalRequest req, CancellationToken ct)
    {
        entity.SetRentalInfo(
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

        if (req.PerfSupportData is not null)
        {
            var dataId = PPrincipleApprovalRentalConsoPerfSupportDataId.From(req.PerfSupportData.Id.Value);

            var match = entity.PerfSupportData?.FirstOrDefault(e => e.Id == dataId);

            match?.Update(
                req.PerfSupportData.TransactionVolume,
                req.PerfSupportData.ActivityDescription,
                req.PerfSupportData.PeriodYear,
                req.PerfSupportData.StartMonth,
                req.PerfSupportData.EndMonth);
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
        }

        if (req.ComparingAttachments != null)
        {
            await this.UpsertComparingAttachments(entity, req.ComparingAttachments);
        }
    }

    private async ValueTask UpdateAndReplaceDocumentAsync(
        PPrincipleApprovalRental entity,
        PPrincipleApproval principleApproval,
        bool? isDocumentIdReplaced,
        bool? isWinnerDocumentIdReplaced,
        CancellationToken ct,
        UserId? creatorUserId)
    {
        var documentService =
            this.Resolve<IDocumentService>();

        var lastedDraftApprovalDocument =
            entity.LastedDraftDocument(PPrincipleApprovalRentalDocumentType.Approval);

        var lastedDraftWinnerDocument =
            entity.LastedDraftDocument(PPrincipleApprovalRentalDocumentType.Winner);

        if (lastedDraftApprovalDocument is null || lastedDraftWinnerDocument is null)
        {
            this.ThrowError("ไม่พบเอกสาร");
        }

        var copiedApprovalFileId =
            await CopyDocument(
                lastedDraftApprovalDocument.FileId,
                PPrincipleApprovalRentalDocumentType.Approval);

        entity.AddDocumentHistory(
            PPrincipleApprovalRentalDocumentType.Approval,
            copiedApprovalFileId);

        var replaceDto =
            await this.MapToReplaceDto(entity, principleApproval, ct, creatorUserId, false);

        var approvalFileId =
            await ReplaceDocument(
                lastedDraftApprovalDocument.FileId,
                isDocumentIdReplaced ?? false,
                PPrincipleApprovalRentalDocumentType.Approval,
                entity.RentTypeCode);

        var winnerFileId =
            await ReplaceDocument(
                lastedDraftWinnerDocument.FileId,
                isWinnerDocumentIdReplaced ?? false,
                PPrincipleApprovalRentalDocumentType.Winner,
                entity.RentTypeCode);

        entity.AddDocumentHistory(
            PPrincipleApprovalRentalDocumentType.Approval,
            approvalFileId,
            true);

        entity.AddDocumentHistory(
            PPrincipleApprovalRentalDocumentType.Winner,
            winnerFileId);

        return;

        async Task<FileId> CopyDocument(
            FileId sourceFileId,
            PPrincipleApprovalRentalDocumentType documentType)
        {
            var replaceDocumentAsync =
                documentService.CopyDocumentTemplateAsync(
                    sourceFileId,
                    parentDirectory: $"{DocumentTemplateGroups.PrincipleApprovalRental}/{entity.Id}_{documentType.ToString()}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
                    cancellationToken: ct);

            var fileIdResult = await replaceDocumentAsync;

            if (fileIdResult is null)
            {
                this.ThrowError("ไม่สามารถคัดลอกเอกสารร่างได้");
            }

            return (FileId)fileIdResult;
        }

        async Task<FileId> ReplaceDocument(
            FileId fileId,
            bool isReplace,
            PPrincipleApprovalRentalDocumentType documentType,
            ParameterCode rentTypeCode)
        {
            if (isReplace)
            {
                fileId = await this.GetDocumentTemplateForResetAsync(entity, documentType);
            }

            var replaceDocumentAsync =
                documentService.CopyDocumentTemplateAsync(
                    fileId,
                    contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto),
                    parentDirectory: $"{DocumentTemplateGroups.PrincipleApprovalRental}/{entity.Id}_{documentType.ToString()}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
                    cancellationToken: ct);

            var fileIdResult = await replaceDocumentAsync;

            if (fileIdResult is null)
            {
                this.ThrowError("ไม่สามารถคัดลอกเอกสารร่างได้");
            }

            return (FileId)fileIdResult;
        }
    }

    private static async Task SendNotificationAcceptorRentCommitteeAsync(PPrincipleApprovalRental entity, CancellationToken ct)
    {
        _ = await entity.Acceptors.Where(x => x.Type == AcceptorType.RentCommittee).Map(pa =>
                            Notification
                                .Crate(
                                    pa.UserId,
                                    NotificationConstant.WaitForLike.Title,
                                    string.Format(NotificationConstant.WaitForLike.Message, ProgramConstant.PrincipalApprovalRental.Name, entity.Procurement.ProcurementNumber),
                                    NotificationProgram.Procurement)
                                .SetReferenceId(entity.Id.Value)
                                .SetLinkUrl(
                                    string.Format(ProgramConstant.PrincipalApprovalRental.Url, entity.Procurement.Id),
                                    "ดูรายละเอียด"))
                        .Map(n => n.PublishAsync(ct).ToUnit())
                        .SequenceSerial();
    }

    private static async Task SendNotificationAssigneeAsync(PPrincipleApprovalRental entity, CancellationToken ct)
    {
        foreach (var targetUserId in entity.Assignees.Where(x => x.Type != AssigneeType.Director).SelectMany(a => a.GetAssigneeNotificationTargets()))
        {
            await Notification
                  .Crate(
                      targetUserId,
                      NotificationConstant.Assignment.Title,
                      string.Format(NotificationConstant.Assignment.Message, ProgramConstant.PrincipalApprovalRental.Name, entity.Procurement.ProcurementNumber),
                      NotificationProgram.Procurement)
                  .SetReferenceId(entity.Id.Value)
                  .SetLinkUrl(
                      string.Format(ProgramConstant.PrincipalApprovalRental.Url, entity.Procurement.Id),
                      "ดูรายละเอียด")
                  .PublishAsync(ct);
        }
    }

    private static async Task SendNotificationAsync(PPrincipleApprovalRental entity, UserId userId, string title, string message)
    {
        await Notification
              .Crate(
                  userId,
                  title,
                  message,
                  NotificationProgram.Procurement)
              .SetReferenceId(entity.Id.Value)
              .SetLinkUrl(string.Format(ProgramConstant.PrincipalApprovalRental.Url, entity.Procurement.Id), "ดูรายละเอียด")
              .PublishAsync(CancellationToken.None);
    }

    private static async Task SendNotificationAssigneeContractAsync(PPrincipleApprovalRental entity, CancellationToken ct)
    {
        foreach (var targetUserId in entity.Assignees.Where(x => x.Type != AssigneeType.Director && x.Group == AssigneeGroup.Contract).SelectMany(a => a.GetAssigneeNotificationTargets()))
        {
            await Notification
                  .Crate(
                      targetUserId,
                      NotificationConstant.Assignment.Title,
                      string.Format(NotificationConstant.Assignment.Message, ProgramConstant.PrincipalApprovalRental.Name, entity.Procurement.ProcurementNumber),
                      NotificationProgram.Procurement)
                  .SetReferenceId(entity.Id.Value)
                  .SetLinkUrl(
                      string.Format(ProgramConstant.PrincipalApprovalRental.Url, entity.Procurement.Id),
                      "ดูรายละเอียด")
                  .PublishAsync(ct);
        }
    }
}