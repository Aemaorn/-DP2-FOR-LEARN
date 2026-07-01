namespace GHB.DP2.Application.Features.Procurement.Jp005.Abstract;

using Codehard.Common.Extensions;
using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.PJp005;
using GHB.DP2.Domain.Procurement.PpPurchaseRequisition;
using GHB.DP2.Domain.SystemUtility;
using GreatFriends.ThaiBahtText;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Linq;

public abstract partial class Jp005EndpointBase<TRequest, TResponse>
{
    // Example for map response DTO for GetListMapping
    protected async Task<GetJp005ByIdReplaceDto> GetJp005MapToResponseMappingDtoAsync(
        PJp005 jp005Existing,
        Domain.Procurement.Procurement procurement,
        Guid userId,
        bool hasCreator = false,
        bool hasAcceptor = false,
        bool hasPublisher = false,
        bool isCommandDocument = false,
        CancellationToken cancellationToken = default)
    {
        var jp004Existing =
            await this.dbContext.PpPurchaseRequisitions
                      .Include(r => r.Budgets)
                      .ThenInclude(b => b.PpPurchaseRequisitionBudgetDetails)
                      .Include(r => r.Warranties)
                      .Include(r => r.PaymentTerms)
                      .Include(r => r.FineRates)
                      .Include(r => r.Committees)
                      .ThenInclude(ppPurchaseRequisitionCommittee => ppPurchaseRequisitionCommittee.User)
                      .ThenInclude(suUser => suUser.Employee)
                      .ThenInclude(rawEmployee => rawEmployee.View)
                      .Include(r => r.Acceptors)
                      .ThenInclude(r => r.User)
                      .ThenInclude(r => r.Employee)
                      .Include(r => r.Assignees)
                      .ThenInclude(r => r.User)
                      .ThenInclude(r => r.Employee)
                      .Include(ppPurchaseRequisition => ppPurchaseRequisition.TechnicalSpecifications).ThenInclude(ppPurchaseRequisitionTechnicalSpecifications => ppPurchaseRequisitionTechnicalSpecifications.Unit)
                      .Include(ppPurchaseRequisition => ppPurchaseRequisition.Procurement)
                      .ThenInclude(procurement => procurement.Department)
                      .Include(ppPurchaseRequisition => ppPurchaseRequisition.Procurement)
                      .ThenInclude(procurement => procurement.SupplyMethod)
                      .Include(ppPurchaseRequisition => ppPurchaseRequisition.Procurement)
                      .ThenInclude(procurement => procurement.SupplyMethodType)
                      .Include(ppPurchaseRequisition => ppPurchaseRequisition.Procurement)
                      .ThenInclude(procurement => procurement.SupplyMethodSpecialType)
                      .Include(ppPurchaseRequisition => ppPurchaseRequisition.Procurement)
                      .ThenInclude(procurement => procurement.Plan)
                      .AsNoTracking()
                      .AsSplitQuery()
                      .FirstOrDefaultAsync(r => r.ProcurementId == procurement.Id, cancellationToken);

        if (jp004Existing == null)
        {
            this.ThrowError(
                "ไม่พบข้อมูล จพ.004",
                StatusCodes.Status404NotFound);
        }

        var torDraft = procurement.TorDrafts.FirstOrDefault(t => t is { IsActive: true, IsDeleted: false });

        var purchaseRequisition = await this.GetPurchaseRequisitionReplaceAsync(jp004Existing, cancellationToken);

        var hasEditPermission = purchaseRequisition.Operators
                                                   .Any(s => s.UserId == userId);

        var lastedAssigneeJp04 = jp004Existing.Assignees.LastOrDefault();

        if (lastedAssigneeJp04 is null)
        {
            this.ThrowError("ไม่พบข้อมูล assignee Jp04");
        }

        var processType = SectionProcessType.ApprovePurchaseRequest;
        var isCommercialMaterial = procurement.IsCommercialMaterial;

        if (isCommercialMaterial && procurement.SupplyMethodCode.Value == SupplyMethodConstant.Eighty)
        {
            processType = SectionProcessType.ApprovePurchaseRequestCommercialParcel;
        }

        var managers = await this.operationService.GetDefaultAcceptorPositionAsync(
            processType,
            lastedAssigneeJp04.UserId.Value,
            procurement.Budget ?? 0,
            procurement.SupplyMethodCode.Value,
            procurement.SupplyMethodCode.Value is SupplyMethodConstant.Eighty ? default : (string?)procurement.SupplyMethodSpecialTypeCode,
            cancellationToken);

        var sectionApproveName = managers.Select(m => new SectionApprove(m.PositionName))
                                         .DefaultIfEmpty(new SectionApprove(string.Empty));

        var reason = torDraft is null ? string.Empty : torDraft.Reason;
        var parcelDescription = jp004Existing
                                .TechnicalSpecifications
                                .OrderBy(x => x.Sequence)
                                .Select(ts => new ParcelDescriptionDto(
                                    Description: ts.Description ?? "-",
                                    Name: ts.Name ?? "-",
                                    Quantity: ts.Quantity,
                                    UnitName: ts.Unit?.Label ?? "-"));

        var considerationDescription = procurement.Budget > 100000
            ? jp004Existing.PriceReasonablenessInfo
            : jp004Existing.Description;

        var consideration = procurement.Budget > 100000
            ? $"กรณีมีราคากลาง คณะกรรมการกำหนดราคากลาง ได้ดำเนินการกำหนดราคากลาง (ราคากลางอ้างอิง) โดยใช้แหล่งที่มาของรากลาง จาก {considerationDescription} เป็นจำนวนเงินทั้งสิ้น {jp004Existing.MedianPriceAmount.Value.ToCurrencyStringWithComma()} บาท ({jp004Existing.MedianPriceAmount.Value.ThaiBahtText()}) ซึ่งได้รับอนุมัติเรียบร้อยแล้ว"
            : $"กรณีไม่มีราคากลาง กรณีวงเงินไม่เกิน 100,000 บาท มีข้อมูลประกอบการพิจารณาด้านความเหมาะสมของราคา ดังนี้ {considerationDescription} เป็นจำนวนเงินทั้งสิ้น {jp004Existing.MedianPriceAmount.Value.ToCurrencyStringWithComma()} บาท ({jp004Existing.MedianPriceAmount.Value.ThaiBahtText()})";

        var commandNumber = managers.FirstOrDefault()?.CommandNumber;

        var commandText = this.commandTextService.GetCommandText(
            CommandTextProgram.JorPor05,
            managers,
            jp005Existing.Procurement.SupplyMethodCode,
            procurement.Budget ?? 0,
            supplyMethodSpecialType: jp005Existing.Procurement.SupplyMethodSpecialTypeCode,
            supplyMethodSpecialName: jp005Existing.Procurement.SupplyMethodSpecialType?.Label,
            commandNumber: commandNumber);

        var user = await this.dbContext.SuUsers
                             .Include(u => u.Employee)
                             .ThenInclude(rawEmployee => rawEmployee.View)
                             .FirstOrDefaultAsync(u => u.Id == UserId.From(userId), cancellationToken);

        if (user is null)
        {
            this.ThrowError($"User with ID {userId} not found.", StatusCodes.Status404NotFound);
        }

        var userCreator = await this.dbContext.SuUsers
                                    .Include(u => u.Employee)
                                    .Where(a => a.Id == UserId.From(userId))
                                    .FirstOrDefaultAsync(cancellationToken);

        var creator =
            hasCreator
                ? new Jp005CreatorDto(
                    Action: "ผู้จัดทำ",
                    Signature: "ลงนาม",
                    FullName: userCreator?.FullName ?? string.Empty,
                    PositionName: userCreator?.Employee.View?.FullPositionName ?? string.Empty,
                    PositionOnBoard: "ประธานคณะกรรมการ")
                : null;

        var lastAcceptor = jp005Existing.Acceptors
            .Where(a => a.Type == AcceptorType.Approver && a.IsActive)
            .OrderBy(a => a.Sequence)
            .Select(DelegatorExtensions.DelegatorToAcceptor)
            .LastOrDefault();

        var isMd = lastAcceptor != null && hasPublisher && await this.dbContext.SuUsers
            .Where(u => u.Id == lastAcceptor.UserId)
            .SelectMany(u => u.Employee.Positions)
            .Where(p => p.Position != null && InRefCodeConstant.MD.Contains(p.Position.InRefCode))
            .AnyAsync(cancellationToken);

        var publisherPositionName = isMd
            ? lastAcceptor?.PositionName ?? string.Empty
            : $"{lastAcceptor?.PositionName} ทำการแทน";

        var publisher =
            hasPublisher
                ? new Jp005PublisherReplace(
                    Signature: lastAcceptor?.Delegatee != null ? lastAcceptor.SignatureDelegatee : lastAcceptor?.Signature,
                    FullName: lastAcceptor?.FullName ?? string.Empty,
                    PositionName: publisherPositionName,
                    Delegate: string.Empty,
                    ManagingDirector: !isMd ? "กรรมการผู้จัดการ" : string.Empty)
                : null;

        var lastedApprovalDate = jp005Existing.Acceptors
            .Where(a => a.Type == AcceptorType.Approver && a.IsActive)
            .OrderBy(a => a.Sequence)
            .LastOrDefault()?.AuditInfo.LastModifiedAt;

        var approverDate =
            hasPublisher
                ? lastedApprovalDate != null ? lastedApprovalDate.Value.ToThaiDateString(includeBuddhistEra: isCommandDocument) : string.Empty
                : null;

        var acceptorDate = jp005Existing.Status is not (
                PJp005Status.Draft or
                PJp005Status.Rejected or
                PJp005Status.Edit)
            ? jp005Existing.DocumentDate?.ToThaiDateString(includeBuddhistEra: false) ?? DateTimeOffset.UtcNow.ToThaiDateString(includeBuddhistEra: false)
            : null;

        var jorPorNumber = jp005Existing.Status == PJp005Status.Approved
                ? jp005Existing.JorPorNumber
                : null;

        return new GetJp005ByIdReplaceDto(
            Telephone: jp004Existing.Telephone ?? string.Empty,
            ApproverDate: approverDate,
            Publisher: publisher,
            DocumentDate: acceptorDate,
            SectionApproveName: (IEnumerable<SectionApprove>?)sectionApproveName,
            Reason: reason,
            ParcelDescriptions: parcelDescription,
            Consideration: consideration,
            ConsiderationDescription: considerationDescription,
            CommandText: commandText,
            Jp005CreatorDto: creator,
            ProcurementId: procurement.Id.Value,
            Procurement: new Jp005ProcurementReplaceDto(
                PlanId: jp005Existing.Procurement.PlanId.HasValue ? jp005Existing.Procurement.PlanId.ToString() : string.Empty,
                ProcurementNumber: jp005Existing.Procurement.ProcurementNumber?.Value,
                ProcurementType: jp005Existing.Procurement.Type,
                ProcurementStep: jp005Existing.Procurement.Step,
                DepartmentName: jp005Existing.Procurement.Department?.Name ?? string.Empty,
                DepartmentCode: jp005Existing.Procurement.DepartmentId.Value,
                PlanNumber: jp005Existing.Procurement.Plan?.PlanNumber.ToString() ?? string.Empty,
                PlanName: jp005Existing.Procurement.Name,
                Budget: jp005Existing.Procurement.Budget.Value.ToCurrencyStringWithComma(),
                BudgetText: jp005Existing.Procurement.Budget.ThaiBahtText(),
                BudgetYear: jp005Existing.Procurement.BudgetYear,
                SupplyMethod: jp005Existing.Procurement.SupplyMethod.Label,
                SupplyMethodCode: (string?)jp005Existing.Procurement.SupplyMethodCode,
                SupplyMethodType: jp005Existing.Procurement.SupplyMethodType?.Label ?? string.Empty,
                SupplyMethodTypeCode: (string?)jp005Existing.Procurement.SupplyMethodTypeCode,
                SupplyMethodSpecialType: jp005Existing.Procurement.SupplyMethodSpecialType?.Label ?? string.Empty,
                SupplyMethodSpecialTypeCode: (string?)jp005Existing.Procurement.SupplyMethodSpecialTypeCode,
                Status: jp005Existing.Procurement.Status,
                ExpectingProcurementAt: jp005Existing.Procurement.ExpectingProcurementAt,
                IsStock: jp005Existing.Procurement.IsStock,
                IsCommercialMaterial: jp005Existing.Procurement.IsCommercialMaterial,
                PlanType: jp005Existing.Procurement.Plan?.Type,
                CurrentStep: jp005Existing.Procurement.ProcessType),
            Id: jp005Existing.Id,
            PurchaseRequisition: purchaseRequisition,
            Jp005: await this.GetJp005Replace(jp005Existing, hasAcceptor, lastedAssigneeJp04!.BusinessUnitName, cancellationToken),
            Status: jp005Existing.Status,
            jorPorNumber,
            HasEditPermission: hasEditPermission);
    }

    protected async Task<Jp004ReplaceDto> GetPurchaseRequisitionReplaceAsync(PpPurchaseRequisition jp004, CancellationToken ct = default)
    {
        var requisition =
            new GetPurchaseRequisitionReplaceDto(
                PurchaseRequisitionNumber: jp004.PurchaseRequisitionNumber.Value,
                EgpNumber: jp004.EgpNumber,
                PrNumber: jp004.PrNumber,
                Description: jp004.Description,
                PriceReasonablenessInfo: jp004.PriceReasonablenessInfo,
                MedianPriceAmount: jp004.MedianPriceAmount.Value.ToCurrencyStringWithComma(),
                EvaluationCriteriaName: await this.GetLabelFromSyParameterAsync(jp004.EvaluationCriteriaCode.ToString(), ct),
                DeliveryPeriod: jp004.DeliveryPeriod ?? 0,
                DeliveryPeriodTypeName: await this.GetLabelFromSyParameterAsync(jp004.DeliveryPeriodTypeCode.ToString(), ct),
                DeliveryConditionName: await this.GetLabelFromSyParameterAsync(jp004.DeliveryConditionCode.ToString(), ct),
                HasFineRate: jp004.HasFineRate,
                HasWarranty: jp004.HasWarranty,
                WarrantyPeriod: jp004.WarrantyPeriod,
                WarrantyPeriodCode: jp004.WarrantyPeriodCode.ToString(),
                WarrantyConditionCode: jp004.WarrantyConditionCode.ToString(),
                HasContractGuarantee: jp004.HasContractGuarantee,
                HasInspectionCommittee: jp004.HasInspectionCommittee,
                HasConstructionSupervisor: jp004.HasConstructionSupervisor);

        var budgets = jp004.Budgets.ToList();
        var budgetsReplace = new List<GetPurchaseRequisitionBudgetReplaceDto>();

        var budgetDetailReplacement =
            budgets.Select(s => s.PpPurchaseRequisitionBudgetDetails)
                   .SelectMany(s => s)
                   .Map((index, item) => new GetPurchaseRequisitionBudgetDetailReplaceDto(
                       Id: item.Id.Value,
                       Sequence: index + 1,
                       DepartmentName: this.GetLabelFromSyParameterAsync(item.Department, ct).GetAwaiter().GetResult(),
                       BudgetType: item.BudgetTypeCode.Value,
                       ProjectCode: item.ProjectCode?.ToString(),
                       AccountName: this.GetLabelFromSyParameterAsync(item.AccountNoCode.ToString(), ct).GetAwaiter().GetResult(),
                       Budget: item.Budget.ToCurrencyStringWithComma()))
                   .ToList();

        foreach (var budget in budgets)
        {
            var budgetsDetailReplace = new List<GetPurchaseRequisitionBudgetDetailReplaceDto>();

            foreach (var item in budget.PpPurchaseRequisitionBudgetDetails.OrderBy(s => s.Sequence).Select((s, index) => new { budgetDetail = s, Index = index, }))
            {
                var newBudgetsDetailReplace = new GetPurchaseRequisitionBudgetDetailReplaceDto(
                    Id: item.budgetDetail.Id.Value,
                    Sequence: item.Index + 1,
                    DepartmentName: await this.GetLabelFromSyParameterAsync(item.budgetDetail.Department, ct),
                    BudgetType: item.budgetDetail.BudgetTypeCode.Value,
                    ProjectCode: item.budgetDetail.ProjectCode?.ToString(),
                    AccountName: await this.GetLabelFromSyParameterAsync(item.budgetDetail.AccountNoCode.ToString(), ct),
                    Budget: item.budgetDetail.Budget.ToCurrencyStringWithComma());

                budgetsDetailReplace.Add(newBudgetsDetailReplace);
            }

            var newBudget = new GetPurchaseRequisitionBudgetReplaceDto(
                budget.Id.Value,
                budget.Description,
                budget.BudgetAmount.ToCurrencyStringWithComma(),
                budgetsDetailReplace,
                budget.Sequence);

            budgetsReplace.Add(newBudget);
        }

        var warranties =
            jp004.Warranties.Select(w =>
                new GetPurchaseRequisitionWarrantyReplaceDto(
                    w.Id.Value,
                    w.HasWarranty,
                    w.Period,
                    w.PeriodTypeCode?.ToString(),
                    w.ConditionOther));

        var committees =
            jp004.Committees
                 .Where(x => x.GroupType == GroupType.ProcurementCommittee || x.GroupType == GroupType.InspectionCommittee)
                 .Select(c =>
                     new GetPurchaseRequisitionCommitteeReplaceDto(
                         c.Id.Value,
                         c.GroupType,
                         c.SuUserId.Value,
                         c.FullName,
                         c.User.Employee.View?.FullPositionName ?? string.Empty,
                         c.CommitteePositionsCode.Value,
                         c.CommitteePositionsName,
                         c.Sequence))
                 .OrderBy(c => c.Sequence)
                 .ToList();

        var operators =
            committees.Where(c => c.GroupType == GroupType.ProcurementCommittee)
                      .Select(c => new Jp004OperatorReplace(
                          c.SuUserId,
                          GetById.Jp004OperatorType.ProcurementCommittee))
                      .ToList();

        var assignee =
            jp004.Assignees
                 .MaxBy(a => a.Sequence);

        var scopeOfWork =
            jp004.TechnicalSpecifications
                 .Select(s => new GetScopeOfWorkReplaceDto(
                     s.Id.Value,
                     s.Sequence,
                     s.Name,
                     s.Description,
                     s.Quantity,
                     s.UnitCode.HasValue ? (string)s.UnitCode : string.Empty));

        if (assignee?.UserId is not null)
        {
            operators.Add(
                new Jp004OperatorReplace(
                    assignee.UserId.Value,
                    GetById.Jp004OperatorType.Assignee));
        }

        var isProcurementCommittee = jp004.Committees.Where(c => c.GroupType == GroupType.ProcurementCommittee)
                                          .All(s => s.IsCommittee());

        var procurement = isProcurementCommittee ? "คณะกรรมการจัดซื้อจัดจ้าง" : "ผู้จัดซื้อจัดจ้าง";

        var isInspectionCommittee = jp004.Committees.Where(c => c.GroupType == GroupType.InspectionCommittee)
                                         .All(s => s.IsCommittee());

        var inspection = isInspectionCommittee ? "คณะกรรมการตรวจรับพัสดุ" : "ผู้ตรวจรับพัสดุ";

        var isMaintenanceInspection = jp004.Committees.Where(c => c.GroupType == GroupType.MaintenanceInspectionCommittee)
                                           .All(s => s.IsCommittee());

        var maintenanceInspectionName = isMaintenanceInspection ? ", คณะกรรมการตรวจรับพัสดุ (งานจ้างบริการบำรุงรักษา)" : ", ผู้ตรวจรับพัสดุงานจ้างบริการบำรุงรักษา";

        var isConstructionSupervisor = jp004.Committees.Where(c => c.GroupType == GroupType.ConstructionSupervisor)
                                            .All(s => s.IsCommittee());

        var constructionSupervisorName = isConstructionSupervisor ? ", คณะกรรมการ" : ", ผู้ควบคุมงาน";

        var isSupCommittee = jp004.Committees.Any(w => w.GroupType == GroupType.ConstructionSupervisor);

        var isMaCommittee = jp004.Committees.Any(w => w.GroupType == GroupType.MaintenanceInspectionCommittee);

        return
            new Jp004ReplaceDto(
                ProcurementType: procurement,
                InspectionType: inspection,
                MaintenanceInspectionType: isMaCommittee ? maintenanceInspectionName : string.Empty,
                ConstructionSupervisorType: isSupCommittee ? constructionSupervisorName : string.Empty,
                MaintenanceInspectionName: string.Empty,
                ConstructionSupervisorName: string.Empty,
                PurchaseRequisitionId: jp004.Id.Value,
                Requisition: requisition,
                Budgets: budgetsReplace,
                BudgetDetails: budgetDetailReplacement,
                Warranties: warranties,
                Committees: committees,
                ScopeOfWorks: scopeOfWork,
                Operators: operators,
                IsProcurementCommittee: jp004.Committees
                                             .Where(w => w.GroupType is GroupType.ProcurementCommittee)
                                             .OrderBy(c => c.Sequence)
                                             .All(a => a.IsCommittee()),
                IsInspectCommittee: jp004.Committees
                                         .Where(w => w.GroupType is GroupType.InspectionCommittee)
                                         .OrderBy(c => c.Sequence)
                                         .All(a => a.IsCommittee()));
    }

    private async Task<Jp005ReplaceDto> GetJp005Replace(PJp005 jp005, bool hasAcceptor, string? lastedAssigneeJp04Department, CancellationToken ct = default)
    {
        var committees = jp005.Committees.ToArray();
        var procurementSuppliesDivisions = jp005.ProcurementSuppliesDivisions.ToArray();

        var duties = jp005.CommitteeDuties.ToArray();

        var lastAcceptors =
            hasAcceptor
                ? jp005.Acceptors
                       .Where(a => a.Type == AcceptorType.Approver && a.IsActive)
                       .OrderBy(a => a.Sequence)
                       .LastOrDefault()
                : null;

        var acceptors =
            hasAcceptor
                ? jp005.Acceptors
                       .WhereIf(jp005.Status == PJp005Status.WaitingApproval || jp005.Status == PJp005Status.Approved, a => a.Status == AcceptorStatus.Approved)
                       .Where(a => a.Type == AcceptorType.Approver)
                       .Select(DelegatorExtensions.DelegatorToAcceptor)
                       .OrderBy(a => a.ActionAt)
                       .Select(a => new Jp005AcceptorReplace(
                           (a.Status, lastAcceptors == a) switch
                           {
                               (AcceptorStatus.Approved, false) => "เห็นชอบ",
                               (AcceptorStatus.Approved, true) => "อนุมัติ",
                               _ => "ไมเห็นชอบ",
                           },
                           a.FullName,
                           a.PositionName,
                           a.Delegatee?.FullPositionName ?? string.Empty))
                : new List<Jp005AcceptorReplace>();

        var lastedApprovalHistory = jp005.DocumentHistories
                                         .Where(d => d.DocumentType == PJp005DocumentType.Approval)
                                         .OrderVersions()
                                         .FirstOrDefault();

        var lastedCommandHistory = jp005.DocumentHistories
                                        .Where(d => d.DocumentType == PJp005DocumentType.Command)
                                        .OrderVersions()
                                        .FirstOrDefault();

        var isProcurementCommittee = committees.Where(c => c.GroupType == PJp005CommitteeGroupType.ProcurementCommittee)
                                               .All(s => s.IsCommittee());
        var procurementCommitteeSection = isProcurementCommittee ? "คณะกรรมการจัดซื้อจัดจ้าง" : "ผู้จัดซื้อ";

        var isInspectionCommittee = committees.Where(c => c.GroupType == PJp005CommitteeGroupType.InspectionCommittee)
                                              .All(s => s.IsCommittee());
        var inspectionCommitteeSection = isInspectionCommittee ? "คณะกรรมการตรวจรับพัสดุ" : "ผู้ตรวจรับพัสดุ";

        var isMaintenanceInspectionCommittee = committees.Where(c => c.GroupType == PJp005CommitteeGroupType.MaintenanceInspectionCommittee)
                                                         .All(s => s.IsCommittee());

        var maintenanceInspectionCommitteeSection = isMaintenanceInspectionCommittee ? "คณะกรรมการตรวจรับพัสดุ (งานจ้างบริการบำรุงรักษา)" : "ผู้ตรวจรับพัสดุงานจ้างบริการบำรุงรักษา";

        var isConstructionSupervisor = committees.Where(c => c.GroupType == PJp005CommitteeGroupType.ConstructionSupervisor)
                                                 .All(s => s.IsCommittee());

        var constructionSupervisorSection = isConstructionSupervisor ? "คณะกรรมการ" : "ผู้ควบคุมงาน";

        var jp05 = new Jp005ReplaceDto(
            EvaluationDueDate: jp005.EvaluationDueDate,
            EvaluationPeriodTypeName: await this.GetLabelFromSyParameterAsync(jp005.EvaluationPeriodTypeCode, ct),
            EvaluationPeriodConditionName: await this.GetLabelFromSyParameterAsync(jp005.EvaluationPeriodConditionCode, ct),
            EgpProjectNumber: jp005.EgpProjectNumber,
            Jp005ApprovalDocumentId: lastedApprovalHistory?.FileId.Value,
            Jp005CommandDocumentId: lastedCommandHistory?.FileId.Value,
            ProcurementCommitteeSection: procurementCommitteeSection,
            ProcurementCommittees: await this.MapCommitteeDtoAsync(committees, procurementSuppliesDivisions, PJp005CommitteeGroupType.ProcurementCommittee, ct),
            ProcurementDuties: MapCommitteeDutyDto(PJp005CommitteeGroupType.ProcurementCommittee),
            IsProcurementCommittee: isProcurementCommittee,
            InspectionCommitteeSection: inspectionCommitteeSection,
            InspectionCommittees: await this.MapCommitteeDtoAsync(committees, [], PJp005CommitteeGroupType.InspectionCommittee, ct),
            InspectionDuties: MapCommitteeDutyDto(PJp005CommitteeGroupType.InspectionCommittee),
            IsInspectionCommittee: isInspectionCommittee,
            MaintenanceInspectionCommitteeSection: maintenanceInspectionCommitteeSection,
            MaintenanceInspectionCommittees: await this.MapCommitteeDtoAsync(committees, [], PJp005CommitteeGroupType.MaintenanceInspectionCommittee, ct),
            MaintenanceInspectionDuties: MapCommitteeDutyDto(PJp005CommitteeGroupType.MaintenanceInspectionCommittee),
            IsMaintenanceInspectionCommittee: isMaintenanceInspectionCommittee,
            ConstructionSupervisorSection: constructionSupervisorSection,
            ConstructionSupervisorCommittees: await this.MapCommitteeDtoAsync(committees, [], PJp005CommitteeGroupType.ConstructionSupervisor, ct),
            ConstructionSupervisorDuties: MapCommitteeDutyDto(PJp005CommitteeGroupType.ConstructionSupervisor),
            IsConstructionSupervisorCommittee: isConstructionSupervisor,
            Jp005Number: jp005.PJp005Number.ToString() ?? string.Empty,
            Acceptors: acceptors,
            LastedAssigneeJp04Department: lastedAssigneeJp04Department);

        return jp05;

        IEnumerable<DutyReplaceDto> MapCommitteeDutyDto(PJp005CommitteeGroupType groupType)
        {
            if (!duties.Any(c => c.GroupType == groupType))
            {
                var mockingData = new List<DutyReplaceDto>
                {
                    new DutyReplaceDto(string.Empty, string.Empty, string.Empty),
                };

                return mockingData;
            }

            var duty = duties.Where(c =>
                                 c.GroupType == groupType)
                             .OrderBy(o => o.Sequence)
                             .Select(c => new DutyReplaceDto(
                                 c.Id.Value.ToString(),
                                 c.Description,
                                 c.Sequence.ToString()))
                             .ToArray();

            return duty;
        }
    }

    protected async Task<IEnumerable<CommitteeReplaceDto>> MapJp04CommitteeDtoAsync(IEnumerable<PpPurchaseRequisitionCommittee> committees, GroupType groupType, CancellationToken ct = default)
    {
        var commitee = committees.Where(c => c.GroupType == groupType)
                                 .OrderBy(r => r.Sequence)
                                 .ToArray();

        if (commitee.Length == 0)
        {
            return new List<CommitteeReplaceDto>()
            {
                new CommitteeReplaceDto(
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty),
            };
        }

        var committeesDto = new List<CommitteeReplaceDto>();

        foreach (var c in commitee)
        {
            var newCommittees = new CommitteeReplaceDto(
                c.Id.Value.ToString(),
                c.SuUserId.Value.ToString(),
                c.User.Employee.View?.FullName ?? c.FullName,
                c.User.Employee.View?.FullPositionName ?? string.Empty,
                await this.GetLabelFromSyParameterAsync((string)c.CommitteePositionsCode, ct),
                c.Sequence.ToString());

            committeesDto.Add(newCommittees);
        }

        return committeesDto;
    }

    protected async Task<IEnumerable<CommitteeReplaceDto>> MapCommitteeDtoAsync(
        PJp005Committee[] committees,
        PJp005ProcurementSuppliesDivision[]? procurementSuppliesDivision,
        PJp005CommitteeGroupType groupType,
        CancellationToken ct = default)
    {
        var commitee = committees.Where(c => c.GroupType == groupType)
                                 .OrderBy(r => r.Sequence)
                                 .ToArray();

        var mockEmptyCommitteeDto = new List<CommitteeReplaceDto>
        {
            new CommitteeReplaceDto(
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty),
        };

        if (!commitee.Any())
        {
            return mockEmptyCommitteeDto;
        }

        var committeesDto = new List<CommitteeReplaceDto>();

        foreach (var c in commitee)
        {
            var newCommittees = new CommitteeReplaceDto(
                c.Id.Value.ToString(),
                c.SuUserId.Value.ToString(),
                c.FullName,
                c.FullPositionName,
                await this.GetLabelFromSyParameterAsync((string)c.CommitteePositionsCode, ct),
                c.Sequence.ToString());

            committeesDto.Add(newCommittees);
        }

        var maxCommitteeSequence = committees.Any() ? committees.Max(s => s.Sequence) : 0;

        if (procurementSuppliesDivision != null && procurementSuppliesDivision.Any())
        {
            var procurementSuppliesDivisions = procurementSuppliesDivision.Select((c, index) => new CommitteeReplaceDto(
                c.Id.Value.ToString(),
                c.SuUserId.Value.ToString(),
                c.FullName,
                c.FullPositionName,
                "ผู้จัดทำ",
                (maxCommitteeSequence + index + 1).ToString()));

            committeesDto.AddRange(procurementSuppliesDivisions);
        }

        return committeesDto;
    }

    protected async ValueTask UpdateDocumentAsync(
        PJp005 jp005,
        Guid userId,
        Domain.Procurement.Procurement procurement,
        bool isReplace,
        bool hasCreator,
        bool hasAcceptor,
        bool hasPublisher,
        CancellationToken cancellationToken = default)
    {
        if (procurement.SupplyMethodCode == SupplyMethodConstant.Sixty && procurement.Budget > 100000)
        {
            return;
        }

        var documentService =
            this.Resolve<IDocumentService>();

        var lastedDraftApprovalDocument = jp005.LastedDraftApprovalDocument;

        var lastedApprovalDocument = hasAcceptor || hasPublisher
            ? jp005.LastedWaitingApprovalIsReplaceApprovalDocument
            : jp005.LastedManageApprovalDocument;

        var isAllPendingOrDraft = jp005.Acceptors
                                       .Where(a =>
                                           a is
                                           {
                                               Type: AcceptorType.Approver or AcceptorType.DepartmentDirectorAgree,
                                               Status: AcceptorStatus.Pending,
                                               IsActive: true
                                           })
                                       .All(a => a.Status is AcceptorStatus.Pending or AcceptorStatus.Draft);

        lastedApprovalDocument = hasAcceptor && isAllPendingOrDraft ? jp005.LastedWaitingApprovalIsReplaceApprovalDocument : lastedApprovalDocument;

        if (lastedApprovalDocument is null)
        {
            this.ThrowError(
                $"ไม่พบเอกสารร่าง จพ.005 ที่ต้องการอัปโหลด",
                StatusCodes.Status404NotFound);
        }

        var approvalFileId = await ReplaceDocument(lastedApprovalDocument.FileId, PJp005DocumentType.Approval);

        jp005.AddDocumentHistory(
            PJp005DocumentType.Approval,
            approvalFileId,
            hasAcceptor || hasPublisher);

        if (jp005.Procurement.SupplyMethodCode == SupplyMethodConstant.Eighty)
        {
            var lastedDraftCommandDocument = jp005.LastedDraftCommandDocument;

            var lastedCommandDocument = hasAcceptor
                ? jp005.LastedWaitingApprovalCommandDocument
                : jp005.LastedManageCommandDocument;

            if (lastedCommandDocument is null)
            {
                this.ThrowError(
                    $"ไม่พบเอกสารร่าง จพ.005 ที่ต้องการอัปโหลด",
                    StatusCodes.Status404NotFound);
            }

            var commandFileId = await ReplaceDocument(lastedCommandDocument.FileId, PJp005DocumentType.Command);

            jp005.AddDocumentHistory(
                PJp005DocumentType.Command,
                commandFileId,
                hasAcceptor || hasPublisher);
        }

        return;

        async Task<FileId> ReplaceDocument(FileId fileId, PJp005DocumentType documentType)
        {
            // Get original template (with placeholders) instead of LastedDraftDocument
            var templateFileId = fileId;

            if (isReplace && jp005.Status is PJp005Status.Draft or PJp005Status.Edit or PJp005Status.Rejected)
            {
                templateFileId = await this.GetDocumentTemplateForResetAsync(
                    jp005,
                    documentType,
                    procurement.SupplyMethodCode,
                    cancellationToken);
            }

            var replaceDto =
                await this.GetJp005MapToResponseMappingDtoAsync(jp005, procurement, userId, hasCreator, hasAcceptor, hasPublisher, documentType == PJp005DocumentType.Command, cancellationToken);

            var parentDirectory =
                $"{DocumentTemplateGroups.PlanAnnouncement}/{jp005.Id}_{documentType.ToString()}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt";

            var copyFileId =
                await documentService.CopyDocumentTemplateAsync(
                    templateFileId,
                    contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto),
                    parentDirectory: parentDirectory,
                    cancellationToken: cancellationToken);

            if (copyFileId is null)
            {
                this.ThrowError(
                    DocumentErrorMessages.CopyDocumentFailed,
                    StatusCodes.Status500InternalServerError);
            }

            return copyFileId.Value;
        }
    }

    protected async ValueTask UpdateDocumentCommandAsync(
       PJp005 jp005,
       Guid userId,
       Domain.Procurement.Procurement procurement,
       bool hasCreator,
       bool hasAcceptor,
       bool hasPublisher,
       CancellationToken cancellationToken = default)
    {
        var documentService =
            this.Resolve<IDocumentService>();

        if (jp005.Procurement.SupplyMethodCode == SupplyMethodConstant.Eighty)
        {
            var lastedCommandDocument = jp005.LastedApprovedMajorCommandDocument;

            if (lastedCommandDocument is null)
            {
                this.ThrowError(
                    $"ไม่พบเอกสารร่าง จพ.005 ที่ต้องการอัปโหลด",
                    StatusCodes.Status404NotFound);
            }

            var commandFileId = await ReplaceDocument(lastedCommandDocument.FileId);

            jp005.AddDocumentHistory(
                PJp005DocumentType.Command,
                commandFileId,
                hasAcceptor || hasPublisher);
        }

        return;

        async Task<FileId> ReplaceDocument(FileId fileId)
        {
            var replaceDto =
                await this.GetJp005MapToResponseMappingDtoAsync(jp005, procurement, userId, hasCreator, hasAcceptor, hasPublisher, isCommandDocument: true, cancellationToken);

            var parentDirectory =
                $"{DocumentTemplateGroups.PlanAnnouncement}/{jp005.Id}_{PJp005DocumentType.Command.ToString()}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt";

            var copyFileId =
                await documentService.CopyDocumentTemplateAsync(
                    fileId,
                    contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto),
                    parentDirectory: parentDirectory,
                    cancellationToken: cancellationToken);

            if (copyFileId is null)
            {
                this.ThrowError(
                    DocumentErrorMessages.CopyDocumentFailed,
                    StatusCodes.Status500InternalServerError);
            }

            return copyFileId.Value;
        }
    }

    private async Task<string> GetLabelFromSyParameterAsync(string? code, CancellationToken ct = default)
    {
        if (code is null)
        {
            return string.Empty;
        }

        var suParameter = await this.dbContext.SuParameters.FirstOrDefaultAsync(s => s.Code == ParameterCode.From(code), ct);

        if (suParameter is null)
        {
            return string.Empty;
        }

        return suParameter.Label;
    }
}