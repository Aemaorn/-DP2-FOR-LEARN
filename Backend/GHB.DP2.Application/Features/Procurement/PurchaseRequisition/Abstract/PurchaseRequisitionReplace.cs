namespace GHB.DP2.Application.Features.Procurement.PurchaseRequisition.Abstract;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.PpPurchaseRequisition;
using GHB.DP2.Domain.Procurement.PpTorDraft;
using GHB.DP2.Domain.SystemUtility;
using GreatFriends.ThaiBahtText;
using Microsoft.EntityFrameworkCore;

public abstract partial class PurchaseRequisitionEndpointBase<TRequest, TResponse>
{
    protected async Task<PurchaseRequisitionReplaceDto> MapToReplaceDto(PpPurchaseRequisition purchaseRequisition)
    {
        var torDraft = purchaseRequisition.TorDraft;

        var procurementCommittee = MapCommitteeByGroupType(purchaseRequisition.Committees, GroupType.ProcurementCommittee);
        var inspectionCommittee = MapCommitteeByGroupType(purchaseRequisition.Committees, GroupType.InspectionCommittee);
        var maintenanceInspectionCommittee = MapCommitteeByGroupType(purchaseRequisition.Committees, GroupType.MaintenanceInspectionCommittee);
        var constructionSupervisor = MapCommitteeByGroupType(purchaseRequisition.Committees, GroupType.ConstructionSupervisor);

        var subject = string.Format("{0}{1}", purchaseRequisition.Procurement.SupplyMethodType, purchaseRequisition.Procurement.Name);

        var parameters = await this.GetParametersAsync(CancellationToken.None);

        var sectionApproveName = string.Format("ผู้อำนวยการ{0}", purchaseRequisition.Procurement.Department.Name);

        var deliveryPeriod = purchaseRequisition.DeliveryConditionCode.ToString() != "DelvCUnit005"
            ? string.Format("{0} {1} {2}", purchaseRequisition.DeliveryPeriod, purchaseRequisition.DeliveryPeriodType?.Label, purchaseRequisition.DeliveryCondition?.Label)
            : string.Format("{0} {1}", purchaseRequisition.DeliveryDate.ToThaiDateString(), purchaseRequisition.DeliveryCondition?.Label);

        return new PurchaseRequisitionReplaceDto(
            sectionApproveName,
            subject,
            purchaseRequisition.Telephone ?? string.Empty,
            MapProcurementReplate(purchaseRequisition.Procurement),
            MapPurchaseRequisitionReplate(purchaseRequisition),
            MapTorObjects(torDraft),
            await this.MapBudgetReplates(purchaseRequisition.Budgets),
            MapDeliveryReplate(purchaseRequisition),
            deliveryPeriod,
            purchaseRequisition.EvaluationCriteria?.Label,
            procurementCommittee,
            inspectionCommittee,
            maintenanceInspectionCommittee,
            constructionSupervisor,
            MapWarrantyReplates(purchaseRequisition.Warranties),
            MapPaymentTermReplates(purchaseRequisition.PaymentTerms),
            MapFineRateReplates(purchaseRequisition.FineRates),
            torDraft?.Reason ?? "........................................................................................",
            AcceptorDate(purchaseRequisition.Status, purchaseRequisition.DocumentDate),
            await this.MapCreateReplate(purchaseRequisition),
            MapAcceptorReplace(purchaseRequisition),
            torDraft?.PpTorDraftTechnicalSpecifications != null ? MapTorTechnicalSpecifications(torDraft, parameters) : MapScopeOfWorksReplates(purchaseRequisition.TechnicalSpecifications));
    }

    protected async ValueTask SetDefaultDocumentTemplate(
        PpPurchaseRequisition purchaseRequisition,
        ParameterCode supplyMethodCode,
        CancellationToken ct)
    {
        var docId = await this.GetDocumentTemplateByCriteria(
            supplyMethodCode,
            ct);

        var createPurchaseOrderDocumentHistory =
            PurchaseRequisitionDocumentHistory.Create(
                purchaseRequisition.DocumentHistories.Any() ? purchaseRequisition.Status : PurchaseRequisitionStatus.Draft,
                "1.0",
                docId);

        purchaseRequisition.AddDocumentHistory(createPurchaseOrderDocumentHistory);
    }

    protected async ValueTask UpdateDocumentAsync(PpPurchaseRequisition purchaseRequisition, bool isReplace, bool isApprove, CancellationToken ct)
    {
        var documentService =
            this.Resolve<IDocumentService>();

        var getLastedDraftDocumentHistory = purchaseRequisition.LastedDocument;

        if (isApprove)
        {
            getLastedDraftDocumentHistory = purchaseRequisition.LastedNotReplacedDocument;
        }

        if (getLastedDraftDocumentHistory is null)
        {
            this.ThrowError("ไม่พบข้อมูลแผนที่ร่าง");
        }

        var dto = await this.MapToReplaceDto(purchaseRequisition);

        FileId? replaceSourceFileId = getLastedDraftDocumentHistory.FileId;

        if (isReplace && purchaseRequisition.Status is PurchaseRequisitionStatus.Draft or PurchaseRequisitionStatus.Edit or PurchaseRequisitionStatus.Rejected)
        {
            // Get original template (with placeholders) instead of LastedDraftDocument
            replaceSourceFileId = await this.GetDocumentTemplateByCriteria(
                purchaseRequisition.Procurement.SupplyMethodCode,
                ct);
        }

        var replaceDocumentAsync =
            documentService.CopyDocumentTemplateAsync(
                    replaceSourceFileId.Value,
                    contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, dto),
                    parentDirectory: $"{DocumentTemplateGroups.Jp04}/{purchaseRequisition.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
                    cancellationToken: ct);

        var finalFileId = await replaceDocumentAsync;

        if (finalFileId is null)
        {
            this.ThrowError("ไม่สามารถคัดลอกเอกสารร่างได้");
        }

        var isReplaceDocument = isReplace && purchaseRequisition.Status is PurchaseRequisitionStatus.WaitingApproval or PurchaseRequisitionStatus.Approved or PurchaseRequisitionStatus.WaitingAssign;

        purchaseRequisition.AddDocumentHistory(finalFileId.Value, isReplaceDocument);
    }

    private async Task<FileId> GetDocumentTemplateByCriteria(
        ParameterCode supplyMethodCode,
        CancellationToken ct)
    {
        var documentService =
            this.Resolve<IDocumentService>();

        var fileId =
            await documentService.GetDocumentTemplateAsync(
                dt =>
                    dt.Group == DocumentTemplateGroups.Jp04 &&
                    dt.SupplyMethodCode == supplyMethodCode &&
                    dt.IsActive,
                ct);

        return (FileId)fileId;
    }

    private static string? AcceptorDate(PurchaseRequisitionStatus status, DateTimeOffset? documentDate)
    {
        return status switch
        {
            PurchaseRequisitionStatus.WaitingAssign => documentDate.ToThaiDateString() ?? DateTimeOffset.UtcNow.ToThaiDateString(),
            PurchaseRequisitionStatus.Approved => documentDate.ToThaiDateString() ?? DateTimeOffset.UtcNow.ToThaiDateString(),
            PurchaseRequisitionStatus.WaitingApproval => documentDate.ToThaiDateString() ?? DateTimeOffset.UtcNow.ToThaiDateString(),
            _ => null,
        };
    }

    private static IEnumerable<AcceptorReplace> MapAcceptorReplace(PpPurchaseRequisition purchaseRequisition)
    {
        if (purchaseRequisition.Status is
            PurchaseRequisitionStatus.Draft or
            PurchaseRequisitionStatus.Edit or
            PurchaseRequisitionStatus.Rejected)
        {
            return [];
        }

        var lastAcceptors =
            purchaseRequisition.Acceptors
                               .Where(a =>
                                   a is
                                   {
                                       IsActive: true,
                                       Status: AcceptorStatus.Approved
                                   })
                               .OrderBy(a => a.Sequence)
                               .LastOrDefault();

        var acceptors =
            purchaseRequisition.Acceptors
                               .Where(a =>
                                   a is
                                   {
                                       IsActive: true,
                                       Status: AcceptorStatus.Approved
                                   })
                               .Select(DelegatorExtensions.DelegatorToAcceptor)
                               .OrderBy(a => a.Sequence)
                               .Select(a =>
                               {
                                   var action =
                                       (a.Status, lastAcceptors == a) switch
                                       {
                                           (AcceptorStatus.Approved, false) => "เห็นชอบ",
                                           (AcceptorStatus.Approved, true) => "เห็นชอบ",
                                           _ => "ไม่เห็นชอบ",
                                       };

                                   return new AcceptorReplace(
                                       action,
                                       a.FullName,
                                       a.PositionName,
                                       string.Empty);
                               });

        return acceptors;
    }

    private async Task<CreateReplate?> MapCreateReplate(PpPurchaseRequisition purchaseRequisition)
    {
        if (purchaseRequisition.Status is
            PurchaseRequisitionStatus.Draft or
            PurchaseRequisitionStatus.Edit or
            PurchaseRequisitionStatus.Rejected)
        {
            return null;
        }

        var creator =
            await Optional(this.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value)
                  .Map(Guid.Parse)
                  .Map(UserId.From)
                  .Match(
                      async id =>
                      {
                          var createPlanReplace =
                              purchaseRequisition.Assignees
                                                 .Where(u => u.UserId == id)
                                                 .Select(u => new CreateReplate(
                                                     "ผู้จัดทำ",
                                                     u.FullName,
                                                     u.PositionName))
                                                 .FirstOrDefault();

                          if (createPlanReplace is null)
                          {
                              var user = await this.dbContext.SuUsers
                                                   .Where(u => u.Id == id)
                                                   .Select(u => new CreateReplate(
                                                       "ผู้จัดทำ",
                                                       u.Employee.View!.FullName,
                                                       u.Employee.View!.FullPositionName))
                                                   .FirstOrDefaultAsync(CancellationToken.None);

                              return user;
                          }

                          return (CreateReplate?)createPlanReplace;
                      },
                      () => null);

        return creator;
    }

    private static ProcurementReplate MapProcurementReplate(Domain.Procurement.Procurement procurement)
    {
        return new ProcurementReplate(
            procurement.ProcurementNumber,
            procurement.Department.Name,
            procurement.Plan.PlanNumber.ToString(),
            procurement.Name,
            procurement.Budget.ToCurrencyStringWithComma(),
            procurement.Budget.ThaiBahtText(),
            procurement.BudgetYear,
            procurement.SupplyMethod.Label,
            procurement.SupplyMethodType?.Label ?? string.Empty,
            procurement.SupplyMethodSpecialType?.Label ?? string.Empty,
            procurement.ExpectingProcurementAt,
            procurement.IsStock,
            procurement.IsCommercialMaterial);
    }

    private static PurchaseRequisitionReplate MapPurchaseRequisitionReplate(PpPurchaseRequisition purchaseRequisition)
    {
        var purchaseRequisitionReplace = new PurchaseRequisitionReplate(
            (string?)purchaseRequisition.PurchaseRequisitionNumber,
            purchaseRequisition.EgpNumber,
            purchaseRequisition.PrNumber,
            purchaseRequisition.Description,
            purchaseRequisition.PriceReasonablenessInfo ?? purchaseRequisition.Description,
            purchaseRequisition.MedianPriceAmount.ToCurrencyStringWithComma(),
            purchaseRequisition.HasContractGuarantee,
            purchaseRequisition.HasInspectionCommittee,
            purchaseRequisition.HasConstructionSupervisor);

        return purchaseRequisitionReplace;
    }

    private static IEnumerable<TorObjectReplate> MapTorObjects(PpTorDraft? torDraft)
    {
        return torDraft?.PpTorDraftObjects.Select(s =>
            new TorObjectReplate((int)s.Sequence, s.Description ?? string.Empty)) ?? [];
    }

    private static TorTechnicalSpecificationReplace[] MapTorTechnicalSpecifications(PpTorDraft? torDraft, SuParameter[] parameters)
    {
        if (torDraft is null)
        {
            return [];
        }

        return
        [
            .. torDraft.PpTorDraftTechnicalSpecifications
                       .Select(t =>
                       {
                           var unitOfMeasuresLabel = parameters.FirstOrDefault(u => u.Code == t.UnitCode)?.Label ?? string.Empty;

                           return new TorTechnicalSpecificationReplace(
                               (int)t.Sequence,
                               t.Name ?? string.Empty,
                               t.Description ?? string.Empty,
                               (int)t.Quantity,
                               t.UnitCode?.Value ?? string.Empty,
                               unitOfMeasuresLabel);
                       })
                       .OrderBy(o => o.Sequence)
        ];
    }

    private async Task<SuParameter[]> GetParametersAsync(CancellationToken ct)
    {
        return await this.dbContext.SuParameters
                         .AsNoTracking()
                         .Where(su =>
                             su.GroupCode == GroupCode.From(SuParameterGroupCodeConstant.UnitOfMeasures))
                         .ToArrayAsync(ct);
    }

    private async Task<IEnumerable<BudgetReplate>> MapBudgetReplates(IEnumerable<PpPurchaseRequisitionBudget> budgets)
    {
        var budgetReplate = budgets
            .SelectMany(b => b.PpPurchaseRequisitionBudgetDetails)
            .OrderBy(b => b.Sequence);

        var result = new List<BudgetReplate>();

        foreach (var item in budgetReplate.Select((s, index) => new { budget = s, index }))
        {
            var suParameter = await this.GetSuParameterByCode(item.budget.Department);

            var accountNoLabel = item.budget.AccountNo?.Label;
            if (accountNoLabel is null && !string.IsNullOrEmpty(item.budget.AccountNoCode.Value))
            {
                var accountNoParam = await this.GetSuParameterByCode(item.budget.AccountNoCode.Value);
                accountNoLabel = accountNoParam?.Label;
            }

            result.Add(new BudgetReplate(
                item.index + 1,
                suParameter!.Label,
                accountNoLabel ?? string.Empty,
                item.budget.Budget.ToCurrencyStringWithComma()));
        }

        return result;
    }

    private async Task<SuParameter?> GetSuParameterByCode(string code)
    {
        return await this.dbContext.SuParameters.FirstOrDefaultAsync(s => s.Code == ParameterCode.From(code), CancellationToken.None);
    }

    private static DeliveryReplate MapDeliveryReplate(PpPurchaseRequisition purchaseRequisition)
    {
        return new DeliveryReplate(
            purchaseRequisition.DeliveryPeriodType?.Label ?? string.Empty,
            purchaseRequisition.DeliveryPeriod ?? 0,
            purchaseRequisition.DeliveryCondition?.Label ?? string.Empty);
    }

    private static CommitteeSectionReplate? MapCommitteeByGroupType(
        IEnumerable<PpPurchaseRequisitionCommittee> committees,
        GroupType groupType)
    {
        if (committees.Any(c => c.GroupType == groupType) is false)
        {
            return new CommitteeSectionReplate(string.Empty, new List<CommitteeReplate>
            {
                new CommitteeReplate(string.Empty, string.Empty, string.Empty, string.Empty),
            });
        }

        return committees
               .Where(c => c.GroupType == groupType)
               .OrderBy(c => c.Sequence)
               .GroupBy(c => c.GetSectionName())
               .Select(MapCommitteeSectionReplate)
               .FirstOrDefault();
    }

    private static IEnumerable<WarrantyReplate> MapWarrantyReplates(IEnumerable<PpPurchaseRequisitionWarranty> warranties)
    {
        return warranties.Select(w => new WarrantyReplate(
            w.HasWarranty,
            w.Period,
            w.PeriodType?.ToString(),
            w.ConditionOther));
    }

    private static IEnumerable<PaymentTermReplate> MapPaymentTermReplates(IEnumerable<PpPurchaseRequisitionPaymentTerm> paymentTerms)
    {
        return paymentTerms.Select(pt => new PaymentTermReplate(
            (int)pt.TermNumber,
            (decimal)pt.Percent,
            (int)pt.Period,
            pt.Description ?? string.Empty));
    }

    private static IEnumerable<FineRateReplate> MapFineRateReplates(IEnumerable<PpPurchaseRequisitionFineRate> fineRates)
    {
        return fineRates.Select(fr => new FineRateReplate(
            fr.Sequence,
            fr.Rate,
            fr.PeriodType.ToString(),
            fr.Condition.ToString(),
            fr.ConditionOther));
    }

    private static TorTechnicalSpecificationReplace[] MapScopeOfWorksReplates(IReadOnlyCollection<PpPurchaseRequisitionTechnicalSpecifications> technicalSpecifications)
    {
        if (technicalSpecifications is null)
        {
            return [];
        }

        return
        [
            .. technicalSpecifications
               .Select(t =>
               {
                   return new TorTechnicalSpecificationReplace(
                       t.Sequence,
                       t.Name,
                       t.Description,
                       t.Quantity,
                       t.UnitCode?.Value ?? string.Empty,
                       t.Unit?.Label ?? string.Empty);
               })
               .OrderBy(o => o.Sequence)
        ];
    }

    private static CommitteeSectionReplate MapCommitteeSectionReplate(
        IGrouping<string, PpPurchaseRequisitionCommittee> committeeGroup)
    {
        return new CommitteeSectionReplate(
            committeeGroup.Key,
            committeeGroup.Select(committee => new CommitteeReplate(
                committee.Sequence.ToString(),
                committee.FullName,
                committee.User?.Employee?.View?.FullPositionName ?? string.Empty,
                committee.CommitteePositionsName)));
    }
}