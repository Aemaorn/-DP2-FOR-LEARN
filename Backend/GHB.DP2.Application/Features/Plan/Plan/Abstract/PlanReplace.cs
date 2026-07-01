namespace GHB.DP2.Application.Features.Plan.Plan.Abstract;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.Plan.Dto;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Plan;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;

public abstract partial class PlanEndpointBase<TRequest, TResponse>
{
    protected async Task UpdateDocumentAsync(
        Plan plan,
        bool isReplace,
        bool hasAcceptors = false,
        bool hasAssignees = false,
        bool hasPublish = false,
        bool skipUpdateDocument = false,
        bool forceStampReplace = false,
        bool isPlanAnnouncementDocumentIdReplace = false,
        CancellationToken cancellationToken = default)
    {
        if (skipUpdateDocument)
        {
            return;
        }

        var documentService =
            this.Resolve<IDocumentService>();

        var document = isReplace ? plan.Document : plan.LastDraftDocument;

        var documentWithAcceptor = hasAcceptors
            ? plan.FirstWaitingAcceptorPlanDocument
            : document;

        if (documentWithAcceptor is not null)
        {
            var planDocumentId = documentWithAcceptor.FileId;

            var dataReplate = await this.MapToPlanReplaceAsync(plan, hasAcceptors, hasAssignees, hasPublish: hasPublish, cancellationToken: cancellationToken);

            try
            {
                var replaceDocumentAsync =
                    isReplace
                        ? documentService.CopyDocumentTemplateAsync(
                            planDocumentId,
                            contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, dataReplate),
                            parentDirectory: $"{DocumentTemplateGroups.Plan}/{plan.PlanNumber}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
                            cancellationToken: cancellationToken)
                        : Task.FromResult<FileId?>(planDocumentId);

                var finalFileId = await replaceDocumentAsync;

                if (finalFileId is null)
                {
                    this.ThrowError(
                        "ไม่สามารถอัพเดทเอกสารแผนได้",
                        StatusCodes.Status500InternalServerError);
                }

                plan.AddDocumentHistory(PlanDocumentType.Plan, finalFileId.Value, hasAcceptors || hasAssignees || hasPublish || forceStampReplace);

                if (plan.Budget > 500000)
                {
                    await this.UpdateAnnouncementDocumentAsync(plan, isReplace, hasAcceptors, hasAssignees, hasPublish, forceStampReplace, isPlanAnnouncementDocumentIdReplace, cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                this.Logger?.LogWarning(ex, "ไม่พบไฟล์ต้นฉบับของแผน {PlanNumber}, ข้ามขั้นตอนนี้", plan.PlanNumber);
            }
        }
    }

    private async Task UpdateAnnouncementDocumentAsync(
        Plan plan,
        bool isReplace,
        bool hasAcceptors = false,
        bool hasAssignees = false,
        bool hasPublish = false,
        bool forceStampReplace = false,
        bool isPlanAnnouncementDocumentIdReplace = false,
        CancellationToken cancellationToken = default)
    {
        var documentService =
            this.Resolve<IDocumentService>();

        var document = isReplace ? plan.AnnouncementDocument : plan.LastDraftAnnouncementDocument;

        if (document is not null)
        {
            var planDocumentId = document.FileId;

            if (isPlanAnnouncementDocumentIdReplace)
            {
                var planAnnouncementTemplate =
                    await documentService.GetDocumentTemplateAsync(
                        d =>
                            d.Group == DocumentTemplateGroups.Plan &&
                            d.SupplyMethodCode == plan.SupplyMethodCode &&
                            (d.BudgetForDocument.Min <= plan.Budget &&
                             (d.BudgetForDocument.Max >= plan.Budget || d.BudgetForDocument.Max == null)) &&
                            (d.IsChange == null || d.IsChange == plan.IsChange) &&
                            (d.IsCancel == null || d.IsCancel == plan.IsCancel) &&
                            d.AdditionalInfo!.RootElement
                             .GetProperty(nameof(SuDocumentTemplate.IsPublished))
                             .GetBoolean(),
                        cancellationToken);

                if (planAnnouncementTemplate is not null)
                {
                    planDocumentId = planAnnouncementTemplate.Value;
                }
            }

            var dataReplate = await this.MapToPlanReplaceAsync(plan, hasAcceptors, hasAssignees, hasPublish, cancellationToken: cancellationToken);

            var replaceDocumentAsync =
                isReplace
                    ? documentService.CopyDocumentTemplateAsync(
                        planDocumentId,
                        contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, dataReplate),
                        parentDirectory: $"{DocumentTemplateGroups.Plan}/{plan.PlanNumber}_{PlanDocumentType.Announcement}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
                        cancellationToken: cancellationToken)
                    : Task.FromResult<FileId?>(planDocumentId);

            var finalFileId = await replaceDocumentAsync;

            if (finalFileId is null)
            {
                this.ThrowError(
                    "ไม่สามารถอัพเดทเอกสารแผนได้",
                    StatusCodes.Status500InternalServerError);
            }

            plan.AddDocumentHistory(PlanDocumentType.Announcement, finalFileId.Value, hasAcceptors || hasAssignees || hasPublish || forceStampReplace || isPlanAnnouncementDocumentIdReplace);
        }
    }

    protected async Task<PlanReplaceDto> MapToPlanReplaceAsync(
        Plan plan,
        bool hasAcceptors = false,
        bool hasAssignees = false,
        bool hasPublish = false,
        CancellationToken cancellationToken = default)
    {
        var (budget, budgetText, changePlan) =
            await this.PlanChangeAsync(plan, cancellationToken);

        var createPlan =
            hasAssignees || plan.Status is PlanStatus.WaitingAcceptor or PlanStatus.WaitingAnnouncement or PlanStatus.Announcement
                ? this.GetCreatePlanReplace(plan)
                : null;

        var acceptorDate =
            Optional(createPlan)
                .Map(_ => plan.DocumentDate?.ToThaiDateString() ?? DateTimeOffset.UtcNow.ToThaiDateString())
                .IfNoneUnsafe((string?)null);

        var rowNumber = 1;

        var planSelected = new PlanSelectedReplate(
            rowNumber,
            plan.PlanNumber.Value,
            plan.EgpNumber,
            plan.Name,
            plan.Budget.ToCurrencyStringWithComma(),
            plan.ExpectingProcurementAt.ToOffset(TimeSpan.FromHours(7)).ToThaiDateString(format: "MM/yyyy"));

        var acceptors =
            hasAcceptors
                ? PlanEndpointBase<TRequest, TResponse>.GetAcceptors(plan, hasAcceptors)
                : new List<AcceptorReplace>();

        var additional =
            plan.Type == PlanType.InYearPlan
                ? "(เพิ่มเติม)"
                : string.Empty;

        var publicPlanReplace =
            hasPublish
                ? PlanEndpointBase<TRequest, TResponse>.GetPublicPlanReplace(plan)
                : null;

        return new PlanReplaceDto(
            plan.PlanNumber.Value,
            plan.Department.Name,
            plan.Type,
            plan.SupplyMethod.Label,
            plan.SupplyMethodType?.Label,
            plan.SupplyMethodSpecialType?.Label,
            plan.BudgetYear,
            plan.Name,
            budget,
            budgetText,
            plan.Remark ?? string.Empty,
            plan.Telephone ?? string.Empty,
            plan.IsStock,
            plan.AssignSegment?.Label,
            plan.GroupEgpNumber,
            plan.EgpNumber,
            plan.IsCommercialMaterial ?? false,
            plan.IsChange,
            plan.IsCancel,
            plan.ChangeReason,
            plan.CancelReason,
            rowNumber,
            acceptorDate,
            hasPublish ? DateTimeOffset.UtcNow.ToThaiDateString(includeBuddhistEra: true) : null,
            additional,
            createPlan,
            planSelected,
            changePlan,
            publicPlanReplace,
            acceptors);
    }

    private static PublicPlanReplace? GetPublicPlanReplace(Plan plan)
    {
        return plan.Assignees
                   .Where(a =>
                       a is { Type: AssigneeType.Director })
                   .OrderBy(a => a.Sequence)
                   .Select(DelegatorExtensions.DelegatorToAssignee)
                   .Select(a => new PublicPlanReplace(
                       a.DelegateeId != null ? a.SignatureDelegatee : a.Signature,
                       a.FullName,
                       a.PositionName,
                       string.Empty))
                   .FirstOrDefault();
    }

    private static IEnumerable<AcceptorReplace> GetAcceptors(Plan plan, bool hasAcceptors = false)
    {
        var lastAcceptor =
            hasAcceptors
                ? plan.Acceptors
                      .Where(a =>
                          a is { Type: AcceptorType.Approver })
                      .OrderBy(a => a.Sequence)
                      .LastOrDefault()
                : null;

        return plan.Acceptors
                   .Where(a =>
                       a is { Status: AcceptorStatus.Approved, Type: AcceptorType.Approver })
                   .OrderBy(a => a.Sequence)
                   .Select(a =>
                   {
                       var isLast = lastAcceptor != null && a.Sequence == lastAcceptor.Sequence;

                       var action = a.Status switch
                       {
                           AcceptorStatus.Approved when isLast => "อนุมัติ",
                           AcceptorStatus.Approved => "เห็นชอบ",
                           _ => "ไม่เห็นชอบ",
                       };

                       var acceptor = DelegatorExtensions.DelegatorToAcceptor(a);

                       return new AcceptorReplace(
                           action,
                           acceptor.FullName,
                           acceptor.PositionName,
                           string.Empty);
                   });
    }

    private CreatePlanReplace? GetCreatePlanReplace(Plan plan)
    {
        return Optional(this.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value)
               .Map(Guid.Parse)
               .Map(UserId.From)
               .MatchUnsafe(
                   id =>
                   {
                       if (plan.Status is PlanStatus.DraftPlan or PlanStatus.WaitingApprovePlan)
                       {
                           return null;
                       }

                       var assignee =
                           plan.Assignees.FirstOrDefault(u => u.UserId == id);

                       if (assignee is null)
                       {
                           return null;
                       }

                       var createPlan = new CreatePlanReplace(
                           "ผู้จัดทำ",
                           assignee.FullName,
                           assignee.PositionName,
                           assignee.BusinessUnitName);

                       return createPlan;
                   },
                   () => null);
    }
}