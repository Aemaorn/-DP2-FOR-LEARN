namespace GHB.DP2.Application.Features.Procurement.PrincipleApproval.Abstract;

using Codehard.Common.Extensions;
using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.Procurement.Invite;
using GHB.DP2.Application.Features.Procurement.PrincipleApproval.Dto;
using GHB.DP2.Application.Features.Procurement.Procurement;
using GHB.DP2.Application.Features.Procurement.Procurement.Abstact;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PPrincipleApproval;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using GreatFriends.ThaiBahtText;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public abstract class PrincipleApprovalEndpointBase<TRequest, TResponse> : EndpointBase<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : IResult
{
    private readonly Dp2DbContext dbContext;

    protected PrincipleApprovalEndpointBase(
        ILogger logger,
        Dp2DbContext dbContext)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    protected async ValueTask AddDefaultDocumentToHistory(
        PPrincipleApproval entity,
        CancellationToken ct)
    {
        var defaultDocumentTemplateId =
            await this.GetDocumentTemplateByCode(entity.RentTypeCode, ct);

        entity.AddDocumentHistory(defaultDocumentTemplateId);
    }

    private async Task<FileId> GetDocumentTemplateByCode(
        ParameterCode templateCode,
        CancellationToken ct)
    {
        var documentService =
            this.Resolve<IDocumentService>();

        var fileId =
            await documentService.GetDocumentTemplateAsync(
                d =>
                    d.Group == DocumentTemplateGroups.PrincipleApproval &&
                    d.AdditionalInfo!.RootElement
                     .GetProperty(nameof(SuDocumentTemplate.PrincipleApprovalTemplateCode))
                     .GetString() == templateCode.Value,
                ct);

        if (fileId == null)
        {
            this.ThrowError(
                "ไม่พบเอกสารแม่แบบ",
                StatusCodes.Status404NotFound);
        }

        return (FileId)fileId;
    }

    protected async Task SetDocumentTemplate(
        PPrincipleApproval entity,
        CancellationToken ct)
    {
        var documentTemplate =
            await this.dbContext.SuDocumentTemplates
                      .FirstOrDefaultAsync(
                          dt =>
                              dt.Group == DocumentTemplateGroups.PrincipleApproval &&
                              dt.AdditionalInfo!.RootElement
                                .GetProperty(nameof(SuDocumentTemplate.PrincipleApprovalTemplateCode))
                                .GetString() == entity.RentTypeCode.Value,
                          ct);

        if (documentTemplate is null)
        {
            this.ThrowError(
                "ไม่พบ Document Template ที่ระบุ",
                StatusCodes.Status404NotFound);
        }

        if (documentTemplate.Id == entity.DocumentTemplateId)
        {
            return;
        }

        entity.SetDocumentTemplate(documentTemplate.Id);
    }

    protected async Task UpsertAcceptors(PPrincipleApproval entity, IEnumerable<AcceptorRequest> requests, PPrincipleApprovalStatus status, CancellationToken ct, UserId? sendToAcceptorId = null)
    {
        var lastAssigneeUserId = entity.PrincipleApprovalAssignees?
            .Where(a => a.Type == AssigneeType.Assignee)
            .OrderByDescending(a => a.Sequence)
            .Select(a => (UserId?)a.UserId)
            .FirstOrDefault();

        var resolvedSendToAcceptorId = lastAssigneeUserId ?? sendToAcceptorId;

        _ = entity.PrincipleApprovalAcceptors.Where(w => !requests.Select(s => s.Id).Contains(w.Id.Value))
                  .Iter(s => entity.RemoveAcceptor(s));

        var userIds = requests.Select(r => r.UserId).ToList();
        var users = await this.ValidateUsersAsync(userIds, ct);

        _ = requests.Where(w => !w.Id.HasValue)
                    .Join(
                        users,
                        req => UserId.From(req.UserId),
                        usr => usr.Id,
                        (req, usr) =>
                        {
                            var acceptor = PPrincipleApprovalAcceptor.Create(req.AcceptorType, usr, req.Sequence, status);
                            acceptor.SetSendToAcceptorId(resolvedSendToAcceptorId);
                            return acceptor;
                        })
                    .Iter(r => entity.AddAcceptor(r));

        foreach (var existing in entity.PrincipleApprovalAcceptors)
        {
            var match = requests.FirstOrDefault(e => e.UserId == existing.UserId && existing.Type == e.AcceptorType);

            if (match != null)
            {
                existing.SetSequence(match.Sequence)
                        .SetActive();
                existing.SetSendToAcceptorId(resolvedSendToAcceptorId);

                switch (entity.Status)
                {
                    case PPrincipleApprovalStatus.Draft or PPrincipleApprovalStatus.Edit or PPrincipleApprovalStatus.Rejected when
                        status == PPrincipleApprovalStatus.WaitingUnitApproval &&
                        existing.Type is AcceptorType.DepartmentDirectorAgree:
                    case PPrincipleApprovalStatus.WaitingComment when
                        status == PPrincipleApprovalStatus.WaitingAcceptance &&
                        existing.Type is AcceptorType.Approver:
                        existing.SetStatus(AcceptorStatus.Pending);

                        PrincipleApprovalEndpointBase<TRequest, TResponse>.SetCurrentAndSendNotification(existing, entity);

                        break;

                    case PPrincipleApprovalStatus.WaitingUnitApproval when status == PPrincipleApprovalStatus.Edit:
                        existing.SetStatus(AcceptorStatus.Draft);

                        break;
                }
            }
        }
    }

    private static void SetCurrentAndSendNotification(PPrincipleApprovalAcceptor existing, PPrincipleApproval entity)
    {
        if (existing.Sequence == 1)
        {
            existing.SetIsCurrent(true);
            foreach (var targetUserId in existing.GetNotificationTargets())
            {
                _ = SendNotificationAsync(entity, targetUserId, NotificationConstant.WaitForApprove.Title, string.Format(NotificationConstant.WaitForApprove.Message, ProgramConstant.PrincipalApproval.Name, entity.Procurement.ProcurementNumber));
            }
        }
    }

    protected async Task UpsertAssignee(PPrincipleApproval entity, IEnumerable<AssigneeRequest> requests, UserId? sendToAcceptorId = null, CancellationToken cancellationToken = default)
    {
        _ = entity.PrincipleApprovalAssignees.Where(w => !requests.Select(s => s.Id).Contains(w.Id.Value))
                  .Iter(s => entity.RemoveAssignee(s));

        var userIds = requests.Select(r => r.UserId).ToList();
        var users = await this.ValidateUsersAsync(userIds, cancellationToken);

        var lastAssigneeUserId = requests
            .Where(a => a.AssigneeType == AssigneeType.Assignee)
            .OrderByDescending(a => a.Sequence)
            .Select(a => (UserId?)UserId.From(a.UserId))
            .FirstOrDefault();

        var resolvedSendToAcceptorId = lastAssigneeUserId ?? sendToAcceptorId;

        _ = requests.Where(w => !w.Id.HasValue)
                    .Join(
                        users,
                        req => UserId.From(req.UserId),
                        usr => usr.Id,
                        (req, usr) =>
                        {
                            var assignee = PPrincipleApprovalAssignee.Create(req.AssigneeGroup, req.AssigneeType, usr, req.Sequence);
                            assignee.SetSendToAcceptorId(resolvedSendToAcceptorId);
                            return assignee;
                        })
                    .Iter(r => entity.AddAssignee(r));

        foreach (var existing in entity.PrincipleApprovalAssignees)
        {
            var match = requests.FirstOrDefault(e => e.UserId == existing.UserId);

            if (match != null)
            {
                existing.SetSequence(match.Sequence);
                existing.SetSendToAcceptorId(resolvedSendToAcceptorId);
            }
        }
    }

    protected async Task UpsertCommittees(
        PPrincipleApproval entity,
        IEnumerable<CommitteeRequest> requests,
        CancellationToken cancellationToken = default)
    {
        _ = entity.PrincipleApprovalCommittees.Where(w => !requests.Select(s => s.Id).Contains(w.Id.Value))
                  .Iter(s => entity.RemoveCommittee(s));

        var userIds = requests.Select(r => r.UserId).ToList();
        var users = await this.ValidateUsersAsync(userIds, cancellationToken);

        var newEntities = requests.Select(dto =>
        {
            var user = users.FirstOrDefault(u => u.Id == dto.UserId);

            if (user == null)
            {
                this.ThrowError($"User with ID {dto.UserId} not found.", StatusCodes.Status404NotFound);
            }

            var existing = (entity.PrincipleApprovalCommittees ?? []).FirstOrDefault(a => a.Id.Value == dto.Id);

            if (existing != null)
            {
                existing.Update(
                    dto.GroupType,
                    user,
                    ParameterCode.From(dto.CommitteePositionsCode),
                    dto.CommitteePositionsName,
                    dto.Sequence);

                return existing;
            }

            return PPrincipleApprovalCommittee.Create(
                dto.GroupType,
                user,
                ParameterCode.From(dto.CommitteePositionsCode),
                dto.CommitteePositionsName,
                dto.Sequence);
        }).ToList();

        // Add new
        foreach (var toAdd in newEntities.Where(e => (entity.PrincipleApprovalCommittees ?? []).All(a => a.Id != e.Id)))
        {
            entity.AddCommittee(toAdd);
        }
    }

    protected void UpsertPerfSupportDataDetails(
        PPrincipleApproval entity,
        IEnumerable<PerfSupportDataDetailsRequest> requests)
    {
        var newEntities = requests.Select(dto =>
        {
            var existing = entity.PerfSupportDataDetails.FirstOrDefault(c => dto.Id == c.Id.Value);

            if (existing != null)
            {
                existing.Update(
                    dto.Sequence,
                    dto.ActivityDescription,
                    dto.AccountCountYear1,
                    dto.AmountYear1,
                    dto.AccountCountYear2,
                    dto.AmountYear2);

                return existing;
            }

            return PPrincipleApprovalConsoPerfSupportDataDetails.Create(
                dto.Sequence,
                dto.ActivityDescription,
                dto.AccountCountYear1,
                dto.AmountYear1,
                dto.AccountCountYear2,
                dto.AmountYear2);
        }).ToList();

        // Add new
        foreach (var toAdd in newEntities.Where(e => entity.PerfSupportDataDetails.All(a => !a.Id.Equals(e.Id))))
        {
            entity.AddPerfSupportDataDetail(toAdd);
        }

        // Remove obsolete
        foreach (var toRemove in entity.PerfSupportDataDetails.Where(a => !newEntities.Any(e => e.Id.Equals(a.Id))).ToList())
        {
            entity.RemovePerfSupportDataDetail(toRemove);
        }
    }

    protected void UpsertRoiLoanAndDepositSummaries(
        PPrincipleApproval entity,
        IEnumerable<RoiLoanAndDepositSummaryRequest> requests)
    {
        var newEntities = requests.Select(dto =>
        {
            var existing = entity.RoiLoanAndDepositSummaries.FirstOrDefault(c => dto.Id == c.Id.Value);

            if (existing != null)
            {
                existing.Update(
                    dto.Sequence,
                    dto.ActivityDescription,
                    dto.AmountYear1,
                    dto.AmountYear2,
                    dto.AmountYear3);

                return existing;
            }

            return PPrincipleApprovalRoiLoanAndDepositSummary.Create(
                dto.Sequence,
                dto.ActivityDescription,
                dto.AmountYear1,
                dto.AmountYear2,
                dto.AmountYear3);
        }).ToList();

        // Add new
        foreach (var toAdd in newEntities.Where(e => entity.RoiLoanAndDepositSummaries.All(a => !a.Id.Equals(e.Id))))
        {
            entity.AddRoiLoanAndDepositSummary(toAdd);
        }

        // Remove obsolete
        foreach (var toRemove in entity.RoiLoanAndDepositSummaries.Where(a => !newEntities.Any(e => e.Id.Equals(a.Id))).ToList())
        {
            entity.RemoveRoiLoanAndDepositSummary(toRemove);
        }
    }

    protected void UpsertRoiPerfResults(
        PPrincipleApproval entity,
        IEnumerable<RoiPerfResultRequest> requests)
    {
        var newEntities = requests.Select(dto =>
        {
            var existing = entity.RoiPerfResults.FirstOrDefault(c => dto.Id == c.Id.Value);

            if (existing != null)
            {
                existing.Update(
                            dto.Sequence,
                            dto.PerformanceResultGroup,
                            dto.Year)
                        .SetValues(
                            dto.AccountActual,
                            dto.AccountGrowth,
                            dto.AmountTarget,
                            dto.AmountActual,
                            dto.AmountRate,
                            dto.AmountGrowth);

                return existing;
            }

            return PPrincipleApprovalRoiPerfResult.Create(
                dto.Sequence,
                dto.PerformanceResultGroup,
                dto.Year).SetValues(
                dto.AccountActual,
                dto.AccountGrowth,
                dto.AmountTarget,
                dto.AmountActual,
                dto.AmountRate,
                dto.AmountGrowth);
        }).ToList();

        // Add new
        foreach (var toAdd in newEntities.Where(e => entity.RoiPerfResults.All(a => !a.Id.Equals(e.Id))))
        {
            entity.AddRoiPerfResult(toAdd);
        }

        // Remove obsolete
        foreach (var toRemove in entity.RoiPerfResults.Where(a => !newEntities.Any(e => e.Id.Equals(a.Id))).ToList())
        {
            entity.RemoveRoiPerfResult(toRemove);
        }
    }

    protected void UpsertBudgets(
        PPrincipleApproval entity,
        IEnumerable<BudgetRequest> requests)
    {
        var budgets = entity.PrincipleApprovalBudgets ?? new List<PPrincipleApprovalBudget>();
        var newEntities = requests.Select(dto =>
        {
            var existing = budgets.FirstOrDefault(c => c.Id.Value == dto.Id);

            if (existing != null)
            {
                existing.Update(
                    dto.Sequence,
                    dto.Description,
                    dto.BudgetAmount);
                UpsertBudgetDetails(existing, dto.Details);

                return existing;
            }

            var created = PPrincipleApprovalBudget.Create(
                dto.Sequence,
                dto.Description,
                dto.BudgetAmount);
            UpsertBudgetDetails(created, dto.Details);

            return created;
        }).ToList();

        // Add new
        foreach (var toAdd in newEntities.Where(e => budgets.All(a => !a.Id.Equals(e.Id))))
        {
            entity.AddBudget(toAdd);
        }

        // Remove obsolete
        foreach (var toRemove in budgets.Where(a => !newEntities.Any(e => e.Id.Equals(a.Id))).ToList())
        {
            entity.RemoveBudget(toRemove);
        }
    }

    private static void UpsertBudgetDetails(PPrincipleApprovalBudget budget, BudgetDetail[]? details)
    {
        var existingDetails = budget.PrincipleApprovalBudgetDetails?.ToList() ?? new List<PPrincipleApprovalBudgetDetail>();
        var newDetails = details ?? [];

        // Update or add
        foreach (var detail in newDetails)
        {
            var exist = existingDetails.FirstOrDefault(x => x.Id.Value == detail.Id);

            if (exist != null)
            {
                exist.Update(
                    detail.Sequence,
                    detail.Department,
                    detail.BudgetType,
                    detail.ProjectCode,
                    detail.AccountNo,
                    detail.Budget);
            }
            else
            {
                var newEntity = PPrincipleApprovalBudgetDetail.Create(
                    detail.Sequence,
                    detail.Department,
                    detail.BudgetType,
                    detail.ProjectCode,
                    detail.AccountNo,
                    detail.Budget);

                budget.AddBudgetDetail(newEntity);
            }
        }

        // Remove obsolete
        foreach (var toRemove in existingDetails.Where(x => newDetails.All(d => d.Id != x.Id.Value)).ToList())
        {
            budget.RemoveBudgetDetail(toRemove);
        }
    }

    protected void UpsertRentalAnalysis(
        PPrincipleApproval entity,
        IEnumerable<RentalAnalysisRequest> requests)
    {
        var rentalAnalyses = entity.PrincipleApprovalRentalAnalyses ?? new List<PPrincipleApprovalRentalAnalysis>();
        var newEntities = requests.Select(dto =>
        {
            var existing = rentalAnalyses.FirstOrDefault(c => c.Id.Value == dto.Id);

            if (existing != null)
            {
                existing.Update(
                    dto.Sequence,
                    dto.Description);
                UpsertRentalAnalysisDetails(existing, dto.Details);

                return existing;
            }

            var created = PPrincipleApprovalRentalAnalysis.Create(
                dto.Sequence,
                dto.Type,
                dto.Description);
            UpsertRentalAnalysisDetails(created, dto.Details);

            return created;
        }).ToList();

        // Add new
        foreach (var toAdd in newEntities.Where(e => rentalAnalyses.All(a => !a.Id.Equals(e.Id))))
        {
            entity.AddRentalAnalysis(toAdd);
        }

        // Remove obsolete
        foreach (var toRemove in rentalAnalyses.Where(a => !newEntities.Any(e => e.Id.Equals(a.Id))).ToList())
        {
            entity.RemoveRentalAnalysis(toRemove);
        }
    }

    private static void UpsertRentalAnalysisDetails(PPrincipleApprovalRentalAnalysis budget, RentalAnalysisDetail[]? details)
    {
        var existingDetails = budget.PrincipleApprovalRentalAnalysisDetails?.ToList() ?? new List<PPrincipleApprovalRentalAnalysisDetail>();
        var newDetails = details ?? [];

        // Update or add
        foreach (var detail in newDetails)
        {
            var exist = existingDetails.FirstOrDefault(x => x.Id.Value == detail.Id);

            if (exist != null)
            {
                exist.Update(
                    detail.Year,
                    detail.Amount);
            }
            else
            {
                var newEntity = PPrincipleApprovalRentalAnalysisDetail.Create(
                    detail.Year,
                    detail.Amount);

                budget.AddApprovalRentalAnalysisDetail(newEntity);
            }
        }

        // Remove obsolete
        foreach (var toRemove in existingDetails.Where(x => newDetails.All(d => d.Id != x.Id.Value)).ToList())
        {
            budget.RemoveApprovalRentalAnalysisDetail(toRemove);
        }
    }

    private async Task<SuUser[]> ValidateUsersAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken)
    {
        var ids = userIds.Map(UserId.From).ToArray();

        var users = await this.dbContext.SuUsers
                              .Include(u => u.Employee)
                              .ThenInclude(e => e.View)
                              .Where(u => ids.Contains(u.Id))
                              .ToArrayAsync(cancellationToken);

        var missingIds = ids.Except(users.Map(u => u.Id)).ToArray();

        if (missingIds.Length > 0)
        {
            this.ThrowError($"User with ID {string.Join(", ", missingIds)} not found.", StatusCodes.Status404NotFound);
        }

        return users;
    }

    protected PrincipleApprovalResponseDto MapToResponse(PPrincipleApproval approval)
    {
        var perfSupportData = approval.PerfSupportData.FirstOrDefault();
        var acceptor = approval.PrincipleApprovalAcceptors;
        var assignee = approval.PrincipleApprovalAssignees;
        var committee = approval.PrincipleApprovalCommittees;
        var perfSupportDataDetailData = approval.PerfSupportDataDetails;
        var roiLoanAndDepositSummaries = approval.RoiLoanAndDepositSummaries;
        var roiPerfResults = approval.RoiPerfResults;

        var lastedDocumentHistory =
            approval.DocumentHistories
                    .OrderVersions()
                    .FirstOrDefault();

        var isReplacedDoc =
            approval.DocumentHistories.Any(d => d.IsReplaced);

        var documentVersions = approval.DocumentHistories
                                        .OrderVersions()
                                        .Select((d, index) => new PrincipleApprovalDocumentVersionResponse(
                                            d.FileId.Value,
                                            d.Version,
                                            d.CreatedAt,
                                            d.CreatedByName ?? string.Empty,
                                            index == 0))
                                        .ToArray();

        return new PrincipleApprovalResponseDto(
            approval.Id.Value,
            approval.ProcurementId.Value,
            new ProcurementDto(
                approval.Procurement.PlanId.HasValue ? (Guid)approval.Procurement.PlanId : null,
                approval.Procurement.ProcurementNumber,
                approval.Procurement.Type,
                approval.Procurement.Step,
                approval.Procurement.Department.Name,
                approval.Procurement.DepartmentId,
                approval.Procurement.Plan?.PlanNumber.ToString() ?? string.Empty,
                approval.Procurement.Name,
                approval.Procurement.Budget,
                approval.Procurement.Budget.ThaiBahtText(),
                approval.Procurement.BudgetYear,
                approval.Procurement.SupplyMethod.Label,
                approval.Procurement.SupplyMethodCode,
                approval.Procurement.SupplyMethodType?.Label ?? string.Empty,
                approval.Procurement.SupplyMethodTypeCode,
                approval.Procurement.SupplyMethodSpecialType?.Label ?? string.Empty,
                approval.Procurement.SupplyMethodSpecialTypeCode,
                approval.Procurement.Status,
                approval.Procurement.ExpectingProcurementAt,
                approval.Procurement.IsStock,
                approval.Procurement.IsCommercialMaterial,
                approval.Procurement.Plan?.Type,
                approval.Procurement.ProcessType),
            approval.BranchLocation,
            lastedDocumentHistory?.FileId.Value,
            false,
            documentVersions,
            approval.RentTypeCode.Value,
            approval.RentalStartDate,
            approval.RentalEndDate,
            approval.RentalDurationYear,
            approval.RentalDurationMonth,
            approval.RentalDurationDay,
            approval.MaxMonthlyRent,
            approval.TotalRentalAmount,
            approval.ExpectedContractDate,
            approval.RentalLocationDetails,
            approval.SubDistrictCode,
            approval.SubDistrictName,
            approval.DistrictCode,
            approval.DistrictName,
            approval.ProvinceCode,
            approval.ProvinceName,
            approval.ReferencePriceAmount,
            approval.AnalysisSummaryNpv,
            approval.AnalysisSummaryPaybackYearPeriod,
            approval.AnalysisSummaryDiscountedPaybackYearPeriod,
            approval.PhoneNumber,
            approval.Status,
            approval.PrincipleApprovalAcceptors
                    .Map(DelegatorExtensions.DelegatorToAcceptor)
                    .Select(a => new PrincipleApprovalAcceptorResponseDto(
                                a.Id.Value,
                                a.Type,
                                a.UserId.Value,
                                a.EmployeeCode.Value,
                                a.FullName,
                                a.PositionName,
                                a.BusinessUnitName,
                                a.Sequence,
                                a.Status,
                                a.Remark,
                                a.ActionAt,
                                a.IsCurrentApprover(),
                                a.Delegatee?.SuUserId.Value))
                    .OrderBy(x => x.Sequence),
            assignee.Map(DelegatorExtensions.DelegatorToAssignee)
                    .Select(a => new PrincipleApprovalAssigneeResponseDto(
                                a.Id.Value,
                                a.Group,
                                a.Type,
                                a.UserId.Value,
                                a.EmployeeCode.Value,
                                a.FullName,
                                a.PositionName,
                                a.BusinessUnitName,
                                a.Remark,
                                a.Sequence,
                                a.Status,
                                a.ActionAt,
                                a.Delegatee?.SuUserId.Value))
                    .OrderBy(x => x.Sequence),
            committee
                .Select(c => new PrincipleApprovalCommitteeResponseDto(
                    c.Id.Value,
                    c.GroupType,
                    c.SuUserId,
                    c.FullName,
                    c.FullPositionName,
                    c.CommitteePositionsCode,
                    c.CommitteePositionsName,
                    c.Sequence))
                .OrderBy(x => x.Sequence),
            new PrincipleApprovalPerfSupportDataResponseDto(
                perfSupportData!.Id.Value,
                perfSupportData.TransactionVolume,
                perfSupportData.ActivityDescription,
                perfSupportData.PeriodYear,
                perfSupportData.StartMonth,
                perfSupportData.EndMonth),
            perfSupportDataDetailData
                .Select(d => new PrincipleApprovalPerfSupportDataDetailResponseDto(
                    d.Id.Value,
                    d.Sequence,
                    d.ActivityDescription,
                    d.AccountCountYear1,
                    d.AmountYear1,
                    d.AccountCountYear2,
                    d.AmountYear2))
                .OrderBy(x => x.Sequence),
            roiLoanAndDepositSummaries
                .Select(r => new PrincipleApprovalRoiLoanAndDepositSummaryResponseDto(
                    r.Id.Value,
                    r.Sequence,
                    r.ActivityDescription,
                    r.AmountYear1,
                    r.AmountYear2,
                    r.AmountYear3))
                .OrderBy(x => x.Sequence),
            roiPerfResults
                .Select(r => new PrincipleApprovalRoiPerfResultResponseDto(
                    r.Id.Value,
                    r.Sequence,
                    r.PerformanceResultGroup,
                    r.Year,
                    r.AccountActual,
                    r.AccountGrowth,
                    r.AmountTarget,
                    r.AmountActual,
                    r.AmountRate,
                    r.AmountGrowth))
                .OrderBy(x => x.Sequence),
            approval.PrincipleApprovalBudgets
                    .OrderBy(o => o.Sequence)
                    .Select(r => new PrincipleApprovalBudgetDto(
                        r.Id.Value,
                        r.Sequence,
                        r.Description,
                        r.BudgetAmount,
                        r.PrincipleApprovalBudgetDetails
                         .OrderBy(d => d.Sequence)
                         .Select(d => new PrincipleApprovalBudgetDetail(
                             d.Id.Value,
                             d.Sequence,
                             d.Department,
                             d.BudgetType,
                             d.ProjectCode,
                             d.AccountNo,
                             d.Budget)))),
            approval.PrincipleApprovalRentalAnalyses
                    .OrderBy(r => r.Sequence)
                    .Select(r => new PrincipleApprovalRentalAnalysisDto(
                        r.Id.Value,
                        r.Sequence,
                        r.Type,
                        r.Description,
                        r.PrincipleApprovalRentalAnalysisDetails
                         .OrderBy(d => d.Year)
                         .Select(d => new PrincipleApprovalRentalAnalysisDetail(
                             d.Id.Value,
                             d.Year,
                             d.Amount)))),
            approval.IsRentCommittee,
            approval.IsAcceptanceCommittee,
            approval.Attachments
                    .OrderBy(a => a.Sequence)
                    .Select(a => new EmailAttachment(
                        a.Id.Value,
                        a.FileName,
                        a.FileId,
                        a.Sequence)),
            approval.DocumentDate);
    }

    protected async Task<PPrincipleApproval> GetPPrincipleApprovalById(PPrincipleApprovalId id, ProcurementId procurementId, CancellationToken ct)
    {
        var principleApproval = await this.dbContext.PPrincipleApprovals
                                          .Include(pPrincipleApproval => pPrincipleApproval.PrincipleApprovalBudgets)
                                          .ThenInclude(pPrincipleApprovalBudget => pPrincipleApprovalBudget.PrincipleApprovalBudgetDetails)
                                          .Include(pPrincipleApproval => pPrincipleApproval.PrincipleApprovalRentalAnalyses)
                                          .ThenInclude(pPrincipleApprovalBudget => pPrincipleApprovalBudget.PrincipleApprovalRentalAnalysisDetails)
                                          .Include(pPrincipleApproval => pPrincipleApproval.DocumentHistories)
                                          .Include(pPrincipleApproval => pPrincipleApproval.PrincipleApprovalAcceptors)
                                          .Include(pPrincipleApproval => pPrincipleApproval.PerfSupportData)
                                          .Include(pPrincipleApproval => pPrincipleApproval.PrincipleApprovalAssignees)
                                          .Include(pPrincipleApproval => pPrincipleApproval.PrincipleApprovalCommittees)
                                          .Include(pPrincipleApproval => pPrincipleApproval.PerfSupportDataDetails)
                                          .Include(pPrincipleApproval => pPrincipleApproval.RoiLoanAndDepositSummaries)
                                          .Include(pPrincipleApproval => pPrincipleApproval.RoiPerfResults)
                                          .Include(pPrincipleApproval => pPrincipleApproval.Attachments)
                                          .Include(pPrincipleApproval => pPrincipleApproval.Procurement)
                                          .ThenInclude(procurement => procurement.Department)
                                          .Include(pPrincipleApproval => pPrincipleApproval.Procurement)
                                          .ThenInclude(procurement => procurement.SupplyMethod)
                                          .Include(pPrincipleApproval => pPrincipleApproval.Procurement)
                                          .ThenInclude(procurement => procurement.SupplyMethodType)
                                          .Include(pPrincipleApproval => pPrincipleApproval.Procurement)
                                          .ThenInclude(procurement => procurement.SupplyMethodSpecialType)
                                          .Include(pPrincipleApproval => pPrincipleApproval.Procurement)
                                          .ThenInclude(procurement => procurement.Plan)
                                          .AsSplitQuery()
                                          .SingleOrDefaultAsync(
                                              p => p.Id == id && p.ProcurementId == procurementId,
                                              ct);

        if (principleApproval is null)
        {
            this.ThrowError($"PPrincipleApproval with ID {id} not found for procurement {procurementId}.", StatusCodes.Status404NotFound);
        }

        return principleApproval;
    }

    protected async Task<FileId?> UpdateDocumentHistoryAsync(
        PPrincipleApproval entity,
        FileId fileId,
        bool? isReplaced = false,
        CancellationToken ct = default)
    {
        var latestHistory = entity.DocumentHistories
                                   .OrderVersions()
                                   .FirstOrDefault();

        if (latestHistory == null)
        {
            return null;
        }

        var newVersion = RunningDocumentVersion.IncrementDocumentVersion(
            latestHistory.Version,
            latestHistory.StatusState.ToString(),
            entity.Status.ToString());

        var documentService = this.Resolve<IDocumentService>();
        var copiedFileId = await documentService.CopyDocumentTemplateAsync(
            fileId,
            parentDirectory: $"{DocumentTemplateGroups.PrincipleApproval}/{entity.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
            cancellationToken: ct);

        if (!copiedFileId.HasValue)
        {
            return null;
        }

        entity.AddDocumentHistory(copiedFileId.Value, isReplaced ?? false);

        var histories = entity.DocumentHistories.ToHashSet();
        var newHistory = histories.OrderVersions().First(h => h.FileId == copiedFileId.Value);
        newHistory.SetVersion(newVersion);

        return copiedFileId.Value;
    }

    protected async Task<FileId> GetDocumentTemplateForResetAsync(
        PPrincipleApproval entity,
        CancellationToken ct)
    {
        var documentService = this.Resolve<IDocumentService>();

        var fileId = await documentService.GetDocumentTemplateAsync(
            d =>
                d.Group == DocumentTemplateGroups.PrincipleApproval &&
                d.AdditionalInfo!.RootElement
                 .GetProperty(nameof(SuDocumentTemplate.PrincipleApprovalTemplateCode))
                 .GetString() == entity.RentTypeCode.Value,
            parentDirectory: $"{DocumentTemplateGroups.PrincipleApproval}/{entity.Id}_Reset_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
            cancellationToken: ct);

        if (fileId == null)
        {
            this.ThrowError(
                "ไม่พบเอกสารเทมเพลตสำหรับรีเซ็ต",
                StatusCodes.Status404NotFound);
        }

        return (FileId)fileId;
    }

    protected async ValueTask ReplaceDocumentsAsync(
        PPrincipleApproval entity,
        bool hasPublisher,
        bool isReplace,
        CancellationToken ct,
        UserId? creatorUserId = null)
    {
        var documentService = this.Resolve<IDocumentService>();

        var replaceDto =
            await this.MapToReplaceDtoAsync(entity, hasPublisher, ct, creatorUserId);

        var lastedDocumentHistory =
            entity.Status is (PPrincipleApprovalStatus.Draft
            or PPrincipleApprovalStatus.Edit
            or PPrincipleApprovalStatus.Rejected
            or PPrincipleApprovalStatus.WaitingComment)
                ? entity.LastedVersionDocument
                : entity.LastedWaitingDocument;

        if (lastedDocumentHistory is null)
        {
            this.ThrowError($"ไม่พบข้อมูลเอกสารราคากลางที่ส่งเห็นชอบ");
        }

        var finalFileId =
            await documentService.CopyDocumentTemplateAsync(
                lastedDocumentHistory.FileId,
                contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto),
                parentDirectory: $"{DocumentTemplateGroups.PrincipleApproval}/{entity.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
                cancellationToken: ct);

        if (finalFileId is null)
        {
            this.ThrowError("ไม่สามารถคัดลอกเอกสารที่ส่งเห็นชอบ");
        }

        entity.AddDocumentHistory(finalFileId.Value, isReplace);
    }

    private async Task<SuUser?> GetLastActivityCreatedByAsync(
        string key,
        string type,
        CancellationToken ct)
    {
        var lastActivity =
            await this.dbContext.SuActivityLogs
                      .Where(l =>
                          l.Key == key &&
                          l.ActivityInfo.Type == type)
                      .OrderByDescending(l => l.AuditInfo.CreatedAt)
                      .FirstOrDefaultAsync(cancellationToken: ct);

        if (lastActivity is null)
        {
            return null;
        }

        var createByUser =
            await this.dbContext.SuUsers
                      .Include(u => u.Employee)
                      .ThenInclude(e => e.View)
                      .FirstOrDefaultAsync(
                          u => u.Id == UserId.From(lastActivity.AuditInfo.CreatedBy),
                          ct);

        return createByUser;
    }

    protected async Task<PrincipleApprovalReplaceDto> MapToReplaceDtoAsync(
        PPrincipleApproval entity,
        bool hasAcceptor,
        CancellationToken ct,
        UserId? creatorUserId)
    {
        var hasCreator = entity.Status is PPrincipleApprovalStatus.WaitingUnitApproval
                                       or PPrincipleApprovalStatus.WaitingAssign
                                       or PPrincipleApprovalStatus.WaitingComment
                                       or PPrincipleApprovalStatus.WaitingAcceptance
                                       or PPrincipleApprovalStatus.Approved;

        var perfSupportData = entity.PerfSupportData.FirstOrDefault();

        var creator = hasCreator ? await GetCreatorReplaceAsync() : null;

        var lastAcceptors = GetValue(
                hasAcceptor,
                entity.PrincipleApprovalAcceptors
                              .Where(a => a.Type == AcceptorType.Approver && a.IsActive)
                              .OrderBy(a => a.Sequence)
                              .LastOrDefault(),
                null);

        var acceptors = GetValue(
                hasAcceptor,
                entity.PrincipleApprovalAcceptors
                              .WhereIf(entity.Status == PPrincipleApprovalStatus.WaitingAcceptance, a => a.Status == AcceptorStatus.Approved)
                              .Where(a => a is { Type: AcceptorType.Approver })
                              .Select(DelegatorExtensions.DelegatorToAcceptor)
                              .Select(a => MapToAcceptorReplace(a, lastAcceptors))
                              .OrderBy(a => a.Sequence)
                              .ToList(),
                []);

        var departmentApproverList = entity.PrincipleApprovalAcceptors
                  .Where(a => a is
                  {
                      Type: AcceptorType.DepartmentDirectorAgree,
                      Status: AcceptorStatus.Approved
                  })
                  .Select(DelegatorExtensions.DelegatorToAcceptor)
                  .OrderBy(a => a.Sequence)
                  .ToList();

        var departmentAcceptors = departmentApproverList
                  .Select((a, index) =>
                      new PrincipleApprovalAcceptorReplaceDto(
                          a.Id.Value,
                          index == departmentApproverList.Count - 1 ? "อนุมัติ" : "เห็นชอบ",
                          a.Type,
                          a.UserId.Value,
                          a.EmployeeCode.Value,
                          a.FullName,
                          a.PositionName,
                          a.BusinessUnitName,
                          a.Sequence,
                          a.Status,
                          a.Remark,
                          string.Empty,
                          a.ActionAt.ToThaiDateString(),
                          a.IsCurrentApprover()));

        var assignees =
            entity.PrincipleApprovalAssignees
                  .Select(DelegatorExtensions.DelegatorToAssignee)
                  .Select(a =>
                      new PrincipleApprovalAssigneeResponseDto(
                          a.Id.Value,
                          a.Group,
                          a.Type,
                          a.UserId.Value,
                          a.EmployeeCode.Value,
                          a.FullName,
                          a.PositionName,
                          a.BusinessUnitName,
                          a.Remark,
                          a.Sequence,
                          a.Status,
                          a.ActionAt))
                  .OrderBy(a => a.Sequence);

        var rentCommittees =
            entity.PrincipleApprovalCommittees
                  .Where(c => c.GroupType == CommitteeGroupType.RentCommittee)
                  .Select(c =>
                      new PrincipleApprovalCommitteeResponseDto(
                          c.Id.Value,
                          c.GroupType,
                          c.SuUserId,
                          c.FullName,
                          c.FullPositionName,
                          c.CommitteePositionsCode,
                          c.CommitteePositionsName,
                          c.Sequence))
                  .OrderBy(c => c.Sequence);

        var acceptanceCommittees =
            entity.PrincipleApprovalCommittees
                  .Where(c => c.GroupType == CommitteeGroupType.AcceptanceCommittee)
                  .Select(c =>
                      new PrincipleApprovalCommitteeResponseDto(
                          c.Id.Value,
                          c.GroupType,
                          c.SuUserId,
                          c.FullName,
                          c.FullPositionName,
                          c.CommitteePositionsCode,
                          c.CommitteePositionsName,
                          c.Sequence))
                  .OrderBy(c => c.Sequence);

        var perfSupportDataDetailData =
            entity.PerfSupportDataDetails
                  .Select(d =>
                      new PrincipleApprovalPerfSupportDataDetailResponseDto(
                          d.Id.Value,
                          d.Sequence,
                          d.ActivityDescription,
                          d.AccountCountYear1,
                          d.AmountYear1,
                          d.AccountCountYear2,
                          d.AmountYear2))
                  .OrderBy(d => d.Sequence);

        var roiLoanAndDepositSummaries =
            entity.RoiLoanAndDepositSummaries
                  .Select(r =>
                      new PrincipleApprovalRoiLoanAndDepositSummaryResponseDto(
                          r.Id.Value,
                          r.Sequence,
                          r.ActivityDescription,
                          r.AmountYear1,
                          r.AmountYear2,
                          r.AmountYear3))
                  .OrderBy(r => r.Sequence);

        var roiPerfResults =
            entity.RoiPerfResults
                  .Select(r =>
                      new PrincipleApprovalRoiPerfResultResponseDto(
                          r.Id.Value,
                          r.Sequence,
                          r.PerformanceResultGroup,
                          r.Year,
                          r.AccountActual,
                          r.AccountGrowth,
                          r.AmountTarget,
                          r.AmountActual,
                          r.AmountRate,
                          r.AmountGrowth))
                  .OrderBy(r => r.Sequence);

        var perfSupportDataResponse = perfSupportData != null
            ? new PrincipleApprovalPerfSupportDataResponseDto(
                perfSupportData.Id.Value,
                perfSupportData.TransactionVolume,
                perfSupportData.ActivityDescription,
                perfSupportData.PeriodYear,
                perfSupportData.StartMonth,
                perfSupportData.EndMonth)
            : null;

        var procurementNumber =
            entity.Procurement.ProcurementNumber.HasValue
                ? entity.Procurement.ProcurementNumber.Value.ToString()
                : string.Empty;

        var procurementReplace =
            new ProcurementReplaceDto(
                entity.Procurement.PlanId.HasValue
                    ? (Guid)entity.Procurement.PlanId
                    : null,
                procurementNumber,
                entity.Procurement.Type,
                entity.Procurement.Step,
                entity.Procurement.Department.Name,
                entity.Procurement.DepartmentId,
                entity.Procurement.Plan?.PlanNumber.ToString() ?? string.Empty,
                entity.Procurement.Name,
                (entity.Procurement.Budget ?? 0).ToCurrencyStringWithComma(),
                entity.Procurement.Budget.ThaiBahtText(),
                entity.Procurement.BudgetYear,
                entity.Procurement.SupplyMethod.Label,
                entity.Procurement.SupplyMethodCode,
                entity.Procurement.SupplyMethodType?.Label ?? string.Empty,
                entity.Procurement.SupplyMethodTypeCode,
                entity.Procurement.SupplyMethodSpecialType?.Label ?? string.Empty,
                entity.Procurement.SupplyMethodSpecialTypeCode,
                entity.Procurement.Status,
                entity.Procurement.ExpectingProcurementAt,
                entity.Procurement.IsStock,
                entity.Procurement.IsCommercialMaterial,
                entity.Procurement.Plan?.Type,
                entity.Procurement.ProcessType);

        var approvalBudgets =
            entity.PrincipleApprovalBudgets
                  .OrderBy(o => o.Sequence)
                  .Select(r => new PrincipleApprovalBudgetDto(
                      r.Id.Value,
                      r.Sequence,
                      r.Description,
                      r.BudgetAmount,
                      r.PrincipleApprovalBudgetDetails
                       .OrderBy(d => d.Sequence)
                       .Select(d => new PrincipleApprovalBudgetDetail(
                           d.Id.Value,
                           d.Sequence,
                           d.Department,
                           d.BudgetType,
                           d.ProjectCode,
                           d.AccountNo,
                           d.Budget))))
                  .ToArray();

        var approvalRentalAnalyses = entity.PrincipleApprovalRentalAnalyses != null
                ? entity.PrincipleApprovalRentalAnalyses?
                  .OrderBy(r => r.Sequence)
                  .Select(r => new PrincipleApprovalRentalAnalysisDto(
                      r.Id.Value,
                      r.Sequence,
                      r.Type,
                      r.Description,
                      r.PrincipleApprovalRentalAnalysisDetails
                       .OrderBy(d => d.Year)
                       .Select(d => new PrincipleApprovalRentalAnalysisDetail(
                           d.Id.Value,
                           d.Year,
                           d.Amount))))
                : null;

        var acceptorDate = hasCreator
            ? entity.DocumentDate?.ToThaiDateString(includeBuddhistEra: false) ?? DateTimeOffset.Now.ToThaiDateString(includeBuddhistEra: false)
            : null;

        var sectionApprover = new List<SectionApprove>
        {
            new SectionApprove(string.Empty),
        };

        var budgetAccountsNo = approvalBudgets.SelectMany(a => a.Details).Select(a => a.AccountNo);

        var accountNo = new List<string>();

        foreach (var budgetAccountNo in budgetAccountsNo)
        {
            var budgetAccountNoLabel = await this.GetLabelFromSyParameterAsync(budgetAccountNo, ct);
            accountNo.Add(budgetAccountNoLabel);
        }

        var commandText = string.Empty;

        var lastAssignee = (entity.Status is PPrincipleApprovalStatus.WaitingComment
            or PPrincipleApprovalStatus.WaitingAcceptance
            or PPrincipleApprovalStatus.Approved)
            ? entity.PrincipleApprovalAssignees
                .Where(a => a.Type == AssigneeType.Assignee)
                .Select(DelegatorExtensions.DelegatorToAssignee)
                .OrderBy(a => a.Sequence)
                .LastOrDefault()
            : null;

        var jorPorCommentReplace = lastAssignee is not null
            ? new JorPorCommentReplace(
                lastAssignee.UserId.Value,
                lastAssignee.FullName,
                lastAssignee.FullName,
                lastAssignee.PositionName,
                lastAssignee.Remark,
                "ผู้จัดทำ")
            : null;

        var replaceDtoResult =
            new PrincipleApprovalReplaceDto(
                entity.Id.Value,
                acceptorDate,
                sectionApprover,
                entity.ProcurementId.Value,
                procurementReplace,
                entity.BranchLocation,
                entity.DocumentTemplateId.Value,
                entity.RentTypeCode.Value,
                entity.RentalStartDate.ToThaiDateString(includeBuddhistEra: false),
                entity.RentalEndDate.ToThaiDateString(includeBuddhistEra: false),
                entity.RentalDurationYear > 0 ? string.Format("{0} ปี", entity.RentalDurationYear) : string.Empty,
                entity.RentalDurationMonth > 0 ? string.Format("{0} เดือน", entity.RentalDurationMonth) : string.Empty,
                entity.RentalDurationDay > 0 ? string.Format("{0} วัน", entity.RentalDurationDay) : string.Empty,
                entity.MaxMonthlyRent.ToCurrencyStringWithComma(),
                entity.MaxMonthlyRent.ThaiBahtText(),
                entity.TotalRentalAmount.ToCurrencyStringWithComma(),
                entity.TotalRentalAmount.ThaiBahtText(),
                entity.ExpectedContractDate.ToThaiDateString(includeBuddhistEra: false),
                entity.RentalLocationDetails,
                entity.PhoneNumber ?? string.Empty,
                entity.SubDistrictCode,
                entity.SubDistrictName,
                entity.DistrictCode,
                entity.DistrictName,
                entity.ProvinceCode,
                entity.ProvinceName,
                entity.ReferencePriceAmount.ToCurrencyStringWithComma() ?? string.Empty,
                entity.ReferencePriceAmount.ThaiBahtText() ?? string.Empty,
                entity.AnalysisSummaryNpv.ToCurrencyStringWithComma(),
                entity.AnalysisSummaryNpv.ThaiBahtText(),
                entity.AnalysisSummaryPaybackYearPeriod,
                entity.AnalysisSummaryDiscountedPaybackYearPeriod,
                entity.Status,
                departmentAcceptors,
                acceptors,
                assignees,
                rentCommittees,
                acceptanceCommittees,
                perfSupportDataResponse,
                perfSupportDataDetailData,
                roiLoanAndDepositSummaries,
                roiPerfResults,
                approvalBudgets,
                string.Join(", ", accountNo),
                approvalRentalAnalyses,
                entity.IsRentCommittee,
                entity.IsAcceptanceCommittee,
                creator,
                commandText,
                jorPorCommentReplace);

        return replaceDtoResult;

        async Task<CreatorReplaceDto?> GetCreatorReplaceAsync()
        {
            var sendToCommitteeApproveByUser =
                creatorUserId is not null
                    ? await this.dbContext.SuUsers
                                .Include(suUser => suUser.Employee)
                                .ThenInclude(rawEmployee => rawEmployee.View)
                                .FirstOrDefaultAsync(u => u.Id == creatorUserId, ct)
                    : await this.GetLastActivityCreatedByAsync(
                        entity.Id.ToString(),
                        ActivityLogActionTypeConstant.SendUnitApprove,
                        ct);

            if (sendToCommitteeApproveByUser == null)
            {
                return null;
            }

            return new CreatorReplaceDto(
                sendToCommitteeApproveByUser.Id.Value,
                "ผู้จัดทำ",
                sendToCommitteeApproveByUser.FullName,
                sendToCommitteeApproveByUser.Employee.View?.FullPositionName ?? string.Empty,
                string.Empty);
        }
    }

    private static PrincipleApprovalAcceptorReplaceDto MapToAcceptorReplace(PPrincipleApprovalAcceptor acceptor, PPrincipleApprovalAcceptor? lastedAcceptor)
    {
        var action =
            (acceptor.Status, lastedAcceptor == acceptor) switch
            {
                (AcceptorStatus.Approved, false) => "เห็นชอบ",
                (AcceptorStatus.Approved, true) => "อนุมัติ",
                _ => "ไมเห็นชอบ",
            };

        return new PrincipleApprovalAcceptorReplaceDto(
            acceptor.Id.Value,
            action,
            acceptor.Type,
            acceptor.UserId.Value,
            acceptor.EmployeeCode.Value,
            acceptor.FullName,
            acceptor.PositionName,
            acceptor.BusinessUnitName,
            acceptor.Sequence,
            acceptor.Status,
            acceptor.Remark,
            string.Empty,
            acceptor.ActionAt.ToThaiDateString(),
            acceptor.IsCurrentApprover());
    }

    private static T GetValue<T>(bool condition, T valueIfTrue, T valueIfFalse)
    {
        return condition ? valueIfTrue : valueIfFalse;
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

    private static async Task SendNotificationAsync(PPrincipleApproval entity, UserId userId, string title, string message)
    {
        await Notification
              .Crate(
                  userId,
                  title,
                  message,
                  NotificationProgram.Procurement)
              .SetReferenceId(entity.Id.Value)
              .SetLinkUrl(string.Format(ProgramConstant.BranchSpaceRent.Url, entity.Procurement.Id), "ดูรายละเอียด")
              .PublishAsync(CancellationToken.None);
    }
}