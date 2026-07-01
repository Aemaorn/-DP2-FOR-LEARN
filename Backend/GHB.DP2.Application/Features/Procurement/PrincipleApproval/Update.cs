namespace GHB.DP2.Application.Features.Procurement.PrincipleApproval;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Contracts.ValueObjects;
using FluentValidation;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Validators;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.Procurement.Invite;
using GHB.DP2.Application.Features.Procurement.PrincipleApproval.Abstract;
using GHB.DP2.Application.Features.Procurement.PrincipleApproval.Dto;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PPrincipleApproval;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record UpdatePrincipleApprovalRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid Id,
    Guid ProcurementId,
    string BranchLocation,
    string DocumentTemplateCode,
    Guid? DocumentTemplateId,
    bool? IsDocumentTemplateIdReplaced,
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

public class UpdatePrincipleApprovalValidator : Validator<UpdatePrincipleApprovalRequest>
{
    public UpdatePrincipleApprovalValidator()
    {
        this.RuleFor(x => x.Id).NotEmpty();
        this.RuleFor(x => x.RentTypeCode).NotEmpty();
        this.RuleFor(x => x.RentalStartDate).NotEmpty();
        this.RuleFor(x => x.RentalEndDate).NotEmpty();
        this.RuleFor(x => x.Status).IsInEnum();

        this.RuleForEach(x => x.Attachments)
            .ChildRules(a => a.RuleFor(x => x.FileName).MustBeValidFileExtension());
    }
}

public record UpdatePrincipleApprovalResponse(Guid? NewDocumentFileId);

public class UpdatePrincipleApprovalEndpoint : PrincipleApprovalEndpointBase<UpdatePrincipleApprovalRequest, Results<Ok<UpdatePrincipleApprovalResponse>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public UpdatePrincipleApprovalEndpoint(ILogger<UpdatePrincipleApprovalEndpoint> logger, Dp2DbContext dbContext)
        : base(logger, dbContext)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Put("procurement/{procurementId:guid}/principle-approval/{id:guid}");
        this.Description(b => b
                              .WithTags("Procurement/PrincipleApproval")
                              .WithName("UpdatePrincipleApproval")
                              .AllowAnonymous()
                              .Produces<UpdatePrincipleApprovalResponse>(StatusCodes.Status200OK)
                              .Produces<string>(StatusCodes.Status404NotFound));
    }

    protected override async ValueTask<Results<Ok<UpdatePrincipleApprovalResponse>, NotFound<string>>> HandleRequestAsync(
        UpdatePrincipleApprovalRequest req,
        CancellationToken ct)
    {
        var entity =
            await this.GetPPrincipleApprovalById(
                PPrincipleApprovalId.From(req.Id),
                ProcurementId.From(req.ProcurementId),
                ct);

        this.ValidateDocument(req, entity);

        var previousStatus = entity.Status;

        entity.SetPPrincipleApproval(
            req.BranchLocation,
            ParameterCode.From(req.RentTypeCode),
            req.RentalStartDate,
            req.RentalEndDate);

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

        entity.SetAnalysisInfo(
            req.AnalysisSummaryNpv,
            req.AnalysisSummaryPaybackYearPeriod,
            req.AnalysisSummaryDiscountedPaybackYearPeriod);

        entity.SetIsRentalCommittee(req.IsRentCommittee);
        entity.SetIsAcceptanceCommittee(req.IsAcceptanceCommittee);

        await this.UpdateEntityAsync(entity, req, ct);

        if (entity.Status == req.Status)
        {
            entity.AddActivity(
                new ActivityInfo(
                    ActivityLogActionTypeConstant.Update,
                    "อัพเดตข้อมูล",
                    entity.Status.ToString()));
        }
        else
        {
            entity.SetStatus(req.Status);
        }

        var isReplaceDocument = req.IsDocumentTemplateIdReplaced ?? false;

        var mustSaveDocument =
            isReplaceDocument &&
            req.DocumentTemplateId.HasValue &&
            entity.Status != PPrincipleApprovalStatus.WaitingUnitApproval;

        FileId? newDocumentFileId = null;

        if (mustSaveDocument)
        {
            newDocumentFileId = await this.UpdateDocumentHistoryAsync(
                entity,
                FileId.From(req.DocumentTemplateId!.Value),
                isReplaceDocument,
                ct);
        }

        if (req.Status == PPrincipleApprovalStatus.WaitingUnitApproval || req.DocumentDate is not null)
        {
            entity.SetDocumentDate(req.DocumentDate);
        }

        if (previousStatus != PPrincipleApprovalStatus.WaitingComment && req.Status == PPrincipleApprovalStatus.WaitingComment)
        {
            _ = SendNotificationAssigneeAsync(entity, CancellationToken.None);
        }

        this.dbContext.PPrincipleApprovals.Update(entity);

        await this.dbContext.SaveChangesAsync(ct);

        await this.UpdateAndReplaceDocumentAsync(
            entity,
            req,
            isReplaceDocument,
            ct);

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok(new UpdatePrincipleApprovalResponse(newDocumentFileId?.Value));
    }

    private void ValidateDocument(UpdatePrincipleApprovalRequest req, PPrincipleApproval entity)
    {
        if (req is { DocumentTemplateId: not null, Status: PPrincipleApprovalStatus.WaitingUnitApproval } &&
            !entity.DocumentHistories.Any())
        {
            this.ThrowError("กรุณาจัดทำเอกสาร", StatusCodes.Status400BadRequest);
        }
    }

    private async Task UpdateEntityAsync(PPrincipleApproval entity, UpdatePrincipleApprovalRequest req, CancellationToken ct)
    {
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

        if (req.PerfSupportData is not null)
        {
            var dataId = PPrincipleApprovalConsoPerfSupportDataId.From(req.PerfSupportData.Id.Value);

            var match = entity.PerfSupportData?.FirstOrDefault(e => e.Id == dataId);

            if (match is not null)
            {
                match.Update(
                    req.PerfSupportData.TransactionVolume,
                    req.PerfSupportData.ActivityDescription,
                    req.PerfSupportData.PeriodYear,
                    req.PerfSupportData.StartMonth,
                    req.PerfSupportData.EndMonth);
            }
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

        var existingAttachments = entity.Attachments.ToList();
        var requestIds = req.Attachments.Where(a => a.Id.HasValue).Select(a => a.Id!.Value).ToHashSet();

        foreach (var existingAttachment in existingAttachments)
        {
            if (!requestIds.Contains(existingAttachment.Id.Value))
            {
                entity.RemoveAttachment(existingAttachment);
            }
        }

        if (req.Attachments.Any())
        {
            var newAttachments = req.Attachments
                                    .Where(a => !a.Id.HasValue)
                                    .Select(a =>
                                        PPrincipleApprovalAttachment.Create(
                                            a.FileId,
                                            a.FileName,
                                            a.Sequence));

            foreach (var attachment in newAttachments)
            {
                entity.AddAttachment(attachment);
            }
        }
    }

    private async ValueTask UpdateAndReplaceDocumentAsync(
        PPrincipleApproval entity,
        UpdatePrincipleApprovalRequest req,
        bool isReplace,
        CancellationToken ct)
    {
        var documentService =
            this.Resolve<IDocumentService>();

        var lastedDocumentHistory = entity.LastedDocument;

        if (lastedDocumentHistory is null)
        {
            return;
        }

        var replaceDto =
            await this.MapToReplaceDtoAsync(entity, false, ct, UserId.From(req.UserId));

        var templateFileId = isReplace
            ? await this.GetDocumentTemplateForResetAsync(entity, ct)
            : lastedDocumentHistory.FileId;

        var shouldCopy = isReplace || entity.Status == PPrincipleApprovalStatus.WaitingUnitApproval;

        var newFileId = shouldCopy
            ? await documentService.CopyDocumentTemplateAsync(
                templateFileId,
                contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto),
                parentDirectory: $"{DocumentTemplateGroups.PrincipleApproval}/{entity.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
                cancellationToken: ct)
            : templateFileId;

        if (newFileId is null)
        {
            this.ThrowError("ไม่สามารถคัดลอกเอกสารร่างได้");
        }

        entity.AddDocumentHistory(newFileId.Value);
    }

    private static async Task SendNotificationAssigneeAsync(PPrincipleApproval entity, CancellationToken ct)
    {
        foreach (var targetUserId in entity.PrincipleApprovalAssignees.Where(x => x.Type != AssigneeType.Director).SelectMany(a => a.GetAssigneeNotificationTargets()))
        {
            await Notification
                  .Crate(
                      targetUserId,
                      NotificationConstant.Assignment.Title,
                      string.Format(NotificationConstant.Assignment.Message, ProgramConstant.PrincipalApproval.Name, entity.Procurement.ProcurementNumber),
                      NotificationProgram.Procurement)
                  .SetReferenceId(entity.Id.Value)
                  .SetLinkUrl(
                      string.Format(ProgramConstant.PrincipalApproval.Url, entity.Procurement.Id),
                      "ดูรายละเอียด")
                  .PublishAsync(ct);
        }
    }
}