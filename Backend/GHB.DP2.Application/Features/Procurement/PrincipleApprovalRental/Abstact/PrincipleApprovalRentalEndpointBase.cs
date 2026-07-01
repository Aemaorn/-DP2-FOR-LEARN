namespace GHB.DP2.Application.Features.Procurement.PrincipleApprovalRental.Abstact;

using Codehard.Common.Extensions;
using Codehard.FileService.Client.Abstractions;
using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.Procurement.Invite.Abstract;
using GHB.DP2.Application.Features.Procurement.Jp006.Dto;
using GHB.DP2.Application.Features.Procurement.PrincipleApproval;
using GHB.DP2.Application.Features.Procurement.PrincipleApprovalRental.Dto;
using GHB.DP2.Application.Features.Procurement.Procurement.Abstact;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.PPrincipleApproval;
using GHB.DP2.Domain.Procurement.PPrincipleApprovalRental;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using GreatFriends.ThaiBahtText;
using LanguageExt;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public abstract class PrincipleApprovalRentalEndpointBase<TRequest, TResponse> : EndpointBase<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : IResult
{
    private readonly Dp2DbContext dbContext;
    private readonly IFileServiceClient fileServiceClient;

    protected IFileServiceClient FileServiceClient => this.fileServiceClient;

    protected PrincipleApprovalRentalEndpointBase(
        ILogger logger,
        Dp2DbContext dbContext,
        IFileServiceClient fileServiceClient)
        : base(logger)
    {
        this.dbContext = dbContext;
        this.fileServiceClient = fileServiceClient;
    }

    protected async ValueTask SetDefaultDocumentTemplate(
        PPrincipleApprovalRental pa,
        ParameterCode rentTypeCode)
    {
        var docId = await this.GetDocumentTemplateByCode(rentTypeCode);
        var docWinnerId = await this.GetWinnerDocumentTemplateByCode();

        pa.AddDocumentHistory(PPrincipleApprovalRentalDocumentType.Approval, docId);
        pa.AddDocumentHistory(PPrincipleApprovalRentalDocumentType.Winner, docWinnerId);
    }

    protected async Task<FileId> GetDocumentTemplateByCode(ParameterCode code)
    {
        var documentService =
            this.Resolve<IDocumentService>();

        // TODO: must use nameof(SuDocumentTemplate.IsWinnerPrincipleApprovalRental)
        var isWinnerFlag = "isWinnerPrincipleApprovalRental";

        var docId =
            await documentService.GetDocumentTemplateAsync(
                d =>
                    d.Group == DocumentTemplateGroups.PrincipleApprovalRental &&
                    d.AdditionalInfo!.RootElement
                     .GetProperty(nameof(SuDocumentTemplate.PrincipleApprovalRentalTemplateCode))
                     .GetString() == code.Value
                    &&
                    (
                        EF.Functions.JsonExists(
                            d.AdditionalInfo,
                            isWinnerFlag) == false ||
                        d.AdditionalInfo.RootElement
                         .GetProperty(isWinnerFlag)
                         .GetBoolean() == false
                    ),
                CancellationToken.None);

        if (docId == null)
        {
            this.ThrowError(
                "ไม่พบเอกสารแม่แบบ",
                StatusCodes.Status404NotFound);
        }

        return (FileId)docId;
    }

    protected async Task<FileId> GetWinnerDocumentTemplateByCode(
        bool? isCancel = null,
        bool? isChange = null)
    {
        var documentService =
            this.Resolve<IDocumentService>();

        // TODO: must use nameof(SuDocumentTemplate.IsWinnerPrincipleApprovalRental)
        var isWinnerFlag = "isWinnerPrincipleApprovalRental";

        var docId =
            await documentService.GetDocumentTemplateAsync(
                d =>
                    d.Group == DocumentTemplateGroups.PrincipleApprovalRental &&
                    d.IsCancel == isCancel &&
                    d.IsChange == isChange &&
                    d.AdditionalInfo!.RootElement
                     .GetProperty(nameof(SuDocumentTemplate.PrincipleApprovalRentalTemplateCode))
                     .GetString() == string.Empty &&
                    (
                        EF.Functions.JsonExists(
                            d.AdditionalInfo,
                            isWinnerFlag) == true ||
                        d.AdditionalInfo.RootElement
                         .GetProperty(isWinnerFlag)
                         .GetBoolean() == true
                    ),
                CancellationToken.None);

        if (docId == null)
        {
            this.ThrowError(
                "ไม่พบเอกสารแม่แบบ",
                StatusCodes.Status404NotFound);
        }

        return (FileId)docId;
    }

    protected async Task UpsertAcceptors(PPrincipleApprovalRental entity, IEnumerable<AcceptorRequest> requests, PPrincipleApprovalRentalStatus status, CancellationToken ct, UserId? sendToAcceptorId = null)
    {
        var lastAssigneeUserId = entity.Assignees?
            .Where(a => a.Type == AssigneeType.Assignee)
            .OrderByDescending(a => a.Sequence)
            .Select(a => (UserId?)a.UserId)
            .FirstOrDefault();

        var resolvedSendToAcceptorId = lastAssigneeUserId ?? sendToAcceptorId;

        _ = entity.Acceptors.Where(w => !requests.Select(s => s.Id).Contains(w.Id.Value))
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
                            var acceptor = PPrincipleApprovalRentalAcceptor.Create(req.AcceptorType, usr, req.Sequence, status)
                                                                           .SetCommitteePositionsCode(req.CommitteePositionsCode)
                                                                           .SetIsUnableToPerformDuties(req.IsUnableToPerformDuties ?? false);
                            acceptor.SetSendToAcceptorId(resolvedSendToAcceptorId);
                            return acceptor;
                        })
                    .Iter(r => entity.AddAcceptor(r));

        foreach (var existing in entity.Acceptors)
        {
            var match = requests.FirstOrDefault(e => e.UserId == existing.UserId && existing.Type == e.AcceptorType);

            if (match != null)
            {
                existing.SetSequence(match.Sequence)
                        .SetActive();
                existing.SetSendToAcceptorId(resolvedSendToAcceptorId);

                existing.SetIsUnableToPerformDuties(match.IsUnableToPerformDuties ?? false);

                switch (entity.Status)
                {
                    case PPrincipleApprovalRentalStatus.Draft or PPrincipleApprovalRentalStatus.Edit or PPrincipleApprovalRentalStatus.Rejected when
                        status == PPrincipleApprovalRentalStatus.WaitingCommitteeApproval && existing.Type is AcceptorType.RentCommittee && !existing.IsUnableToPerformDuties:
                        existing.SetStatus(AcceptorStatus.Pending);

                        break;

                    case PPrincipleApprovalRentalStatus.WaitingCommitteeApproval when status == PPrincipleApprovalRentalStatus.Edit && !existing.IsUnableToPerformDuties:
                        existing.SetStatus(AcceptorStatus.Draft);

                        break;

                    case PPrincipleApprovalRentalStatus.WaitingAssign when status == PPrincipleApprovalRentalStatus.WaitingAcceptance && existing.Type is AcceptorType.Approver:
                        existing.SetStatus(AcceptorStatus.Pending);

                        break;

                    case PPrincipleApprovalRentalStatus.WaitingComment when status == PPrincipleApprovalRentalStatus.WaitingAcceptance && existing.Type is AcceptorType.Approver:
                        existing.SetStatus(AcceptorStatus.Pending);

                        break;
                }
            }
        }
    }

    protected async Task UpsertAssignee(PPrincipleApprovalRental entity, IEnumerable<AssigneeRequest> requests, UserId? sendToAcceptorId = null, CancellationToken cancellationToken = default)
    {
        _ = entity.Assignees.Where(w => !requests.Select(s => s.Id).Contains(w.Id.Value))
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
                            var assignee = PPrincipleApprovalRentalAssignee.Create(req.AssigneeGroup, req.AssigneeType, usr, req.Sequence);
                            assignee.SetSendToAcceptorId(resolvedSendToAcceptorId);
                            return assignee;
                        })
                    .Iter(r => entity.AddAssignee(r));

        foreach (var existing in entity.Assignees)
        {
            var match = requests.FirstOrDefault(e => e.UserId == existing.UserId);

            if (match != null)
            {
                existing.SetSequence(match.Sequence);
                existing.SetSendToAcceptorId(resolvedSendToAcceptorId);
            }
        }
    }

    protected void UpsertPerfSupportDataDetails(
        PPrincipleApprovalRental entity,
        IEnumerable<PerfSupportDataDetailsRequest> requests)
    {
        var details = entity.PerfSupportDataDetails ?? new List<PPrincipleApprovalRentalConsoPerfSupportDataDetails>();
        var newEntities = requests.Select(dto =>
        {
            var existing = details.FirstOrDefault(c => c.Id.Value == dto.Id);

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

            return PPrincipleApprovalRentalConsoPerfSupportDataDetails.Create(
                dto.Sequence,
                dto.ActivityDescription,
                dto.AccountCountYear1,
                dto.AmountYear1,
                dto.AccountCountYear2,
                dto.AmountYear2);
        }).ToList();

        // Add new
        foreach (var toAdd in newEntities.Where(e => details.All(a => !a.Id.Equals(e.Id))))
        {
            entity.AddPerfSupportDataDetail(toAdd);
        }

        // Remove obsolete
        foreach (var toRemove in details.Where(a => !newEntities.Any(e => e.Id.Equals(a.Id))).ToList())
        {
            entity.RemovePerfSupportDataDetail(toRemove);
        }
    }

    protected void UpsertRoiLoanAndDepositSummaries(
        PPrincipleApprovalRental entity,
        IEnumerable<RoiLoanAndDepositSummaryRequest> requests)
    {
        var roiLoanAndDepositSummaries = entity.RoiLoanAndDepositSummaries ?? [];
        var newEntities = requests.Select(dto =>
        {
            var existing = roiLoanAndDepositSummaries.FirstOrDefault(c => dto.Id == c.Id.Value);

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

            return PPrincipleApprovalRentalRoiLoanAndDepositSummary.Create(
                dto.Sequence,
                dto.ActivityDescription,
                dto.AmountYear1,
                dto.AmountYear2,
                dto.AmountYear3);
        }).ToList();

        // Add new
        foreach (var toAdd in newEntities.Where(e => roiLoanAndDepositSummaries.All(a => !a.Id.Equals(e.Id))))
        {
            entity.AddRoiLoanAndDepositSummary(toAdd);
        }

        // Remove obsolete
        foreach (var toRemove in roiLoanAndDepositSummaries.Where(a => !newEntities.Any(e => e.Id.Equals(a.Id))).ToList())
        {
            entity.RemoveRoiLoanAndDepositSummary(toRemove);
        }
    }

    protected void UpsertRoiPerfResults(
        PPrincipleApprovalRental entity,
        IEnumerable<RoiPerfResultRequest> requests)
    {
        var roiPerfResults = entity.RoiPerfResults ?? [];
        var newEntities = requests.Select(dto =>
        {
            var existing = roiPerfResults.FirstOrDefault(c => dto.Id == c.Id.Value);

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

            return PPrincipleApprovalRentalRoiPerfResult.Create(
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
        foreach (var toAdd in newEntities.Where(e => roiPerfResults.All(a => !a.Id.Equals(e.Id))))
        {
            entity.AddRoiPerfResult(toAdd);
        }

        // Remove obsolete
        foreach (var toRemove in roiPerfResults.Where(a => !newEntities.Any(e => e.Id.Equals(a.Id))).ToList())
        {
            entity.RemoveRoiPerfResult(toRemove);
        }
    }

    protected void UpsertBudgets(
        PPrincipleApprovalRental entity,
        IEnumerable<BudgetRequest> requests)
    {
        var budgets = entity.Budgets ?? new List<PPrincipleApprovalRentalBudget>();
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

            var created = PPrincipleApprovalRentalBudget.Create(
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

    private static void UpsertBudgetDetails(PPrincipleApprovalRentalBudget budget, BudgetDetail[]? details)
    {
        var existingDetails = budget.PrincipleApprovalRentalBudgetDetails?.ToList() ?? new List<PPrincipleApprovalRentalBudgetDetail>();
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
                var newEntity = PPrincipleApprovalRentalBudgetDetail.Create(
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
        PPrincipleApprovalRental entity,
        IEnumerable<RentalAnalysisRequest> requests)
    {
        var rentalAnalyses = entity.RentalAnalyses ?? new List<PPrincipleApprovalRentalRentalAnalysis>();
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

            var created = PPrincipleApprovalRentalRentalAnalysis.Create(
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

    private static void UpsertRentalAnalysisDetails(PPrincipleApprovalRentalRentalAnalysis budget, RentalAnalysisDetail[]? details)
    {
        var existingDetails = budget.PrincipleApprovalRentalRentalAnalysisDetails?.ToList() ?? new List<PPrincipleApprovalRentalRentalAnalysisDetail>();
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
                var newEntity = PPrincipleApprovalRentalRentalAnalysisDetail.Create(
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

    protected void UpsertEntrepreneurs(
        PPrincipleApprovalRental entity,
        IEnumerable<EntrepreneursRequest> requests)
    {
        var entrepreneurs = entity.Entrepreneurs ?? new List<PPrincipleApprovalRentalEntrepreneurs>();
        var newEntities = requests.Select(dto =>
        {
            var existing = entrepreneurs.FirstOrDefault(c => c.Id.Value == dto.Id);

            if (existing != null)
            {
                existing.Update(dto.Sequence, dto.EmailSend)
                        .SetWatchlist(dto.WatchlistResult, dto.WatchlistResultRemark, dto.WatchlistResultAt)
                        .SetCoi(dto.CoiResult, dto.CoiResultRemark, dto.CoiResultAt)
                        .SetEgp(dto.EgpResult, dto.EgpResultRemark, dto.EgpResultAt)
                        .SetSequence(dto.Sequence);

                if (dto.CoiCheckerResult is not null)
                {
                    existing.AddChecker(
                        QualificationType.COI,
                        dto.CoiCheckerResult.Result,
                        dto.CoiCheckerResult.ResultAt,
                        dto.CoiCheckerResult.Remark);
                }

                if (dto.WatchlistCheckerResult is not null)
                {
                    existing.AddChecker(
                        QualificationType.COI,
                        dto.WatchlistCheckerResult.Result,
                        dto.WatchlistCheckerResult.ResultAt,
                        dto.WatchlistCheckerResult.Remark);
                }

                UpsertShareholder(existing, dto.Shareholders);

                this.UpsertEntrepreneurPriceDetail(existing, dto.Details);

                return existing;
            }

            var vendor = this.dbContext.SuVendors
                             .SingleOrDefault(x => x.Id == SuVendorId.From(dto.VendorId));

            if (vendor == null)
            {
                this.ThrowError($"ไม่พบข้อมูลผู้ประกอบการที่มีรหัส {dto.Id}", StatusCodes.Status404NotFound);
            }

            var created = PPrincipleApprovalRentalEntrepreneurs.Create(
                                                                   vendor,
                                                                   dto.Sequence,
                                                                   dto.EmailSend)
                                                               .SetWatchlist(dto.WatchlistResult, dto.WatchlistResultRemark, dto.WatchlistResultAt)
                                                               .SetCoi(dto.CoiResult, dto.CoiResultRemark, dto.CoiResultAt)
                                                               .SetEgp(dto.EgpResult, dto.EgpResultRemark, dto.EgpResultAt)
                                                               .SetSequence(dto.Sequence);

            if (dto.CoiCheckerResult is not null)
            {
                created.AddChecker(
                    QualificationType.COI,
                    dto.CoiCheckerResult.Result,
                    dto.CoiCheckerResult.ResultAt,
                    dto.CoiCheckerResult.Remark);
            }

            if (dto.WatchlistCheckerResult is not null)
            {
                created.AddChecker(
                    QualificationType.COI,
                    dto.WatchlistCheckerResult.Result,
                    dto.WatchlistCheckerResult.ResultAt,
                    dto.WatchlistCheckerResult.Remark);
            }

            if (dto.Shareholders != null && dto.Shareholders.Any())
            {
                var shareholders = dto.Shareholders.SelectMany(s =>
                {
                    var checkTypes = s.CheckType != null
                        ? new[] { s.CheckType }
                        : new[] { "COI", "Watchlist" };

                    return checkTypes.Select(checkType =>
                    {
                        var newShareholder = PPrincipleApprovalRentalEntrepreneursShareholders.Create(
                                                                                                  s.Sequence,
                                                                                                  s.TaxId,
                                                                                                  s.FirstName,
                                                                                                  s.LastName,
                                                                                                  s.IsDirector,
                                                                                                  s.IsShareholder,
                                                                                                  s.IsJuristic)
                                                                                              .SetCheckType(checkType)
                                                                                              .SetWatchlist(s.WatchlistResult, s.WatchlistResultRemark, s.WatchlistResultAt)
                                                                                              .SetCoi(s.CoiResult, s.CoiResultRemark, s.CoiResultAt)
                                                                                              .SetEgp(s.EgpResult, s.EgpRemark, s.EgpResultAt);

                        if (s.CoiCheckerResult is not null)
                        {
                            newShareholder.AddChecker(
                                QualificationType.COI,
                                s.CoiCheckerResult.Result,
                                s.CoiCheckerResult.ResultAt,
                                s.CoiCheckerResult.Remark);
                        }

                        if (s.WatchlistCheckerResult is not null)
                        {
                            newShareholder.AddChecker(
                                QualificationType.Watchlist,
                                s.WatchlistCheckerResult.Result,
                                s.WatchlistCheckerResult.ResultAt,
                                s.WatchlistCheckerResult.Remark);
                        }

                        return newShareholder;
                    });
                }).ToList();

                foreach (var s in shareholders)
                {
                    created.AddShareholder(s);
                }
            }

            this.UpsertEntrepreneurPriceDetail(created, dto.Details);

            return created;
        }).ToList();

        // Add new
        foreach (var toAdd in newEntities.Where(e => entrepreneurs.All(a => a.Id != e.Id)))
        {
            entity.AddEntrepreneurs(toAdd);
        }

        var toRemoveList = entrepreneurs.Where(a => !newEntities.Any(e => e.Id == a.Id)).ToList();

        // Remove obsolete
        foreach (var toRemove in toRemoveList)
        {
            entity.RemoveEntrepreneurs(toRemove);
        }
    }

    private static void CreateNewShareholder(PPrincipleApprovalRentalEntrepreneurs entrepreneurs, Dto.ShareholderDto s, string checkType)
    {
        var newShareholder = PPrincipleApprovalRentalEntrepreneursShareholders
            .Create(s.Sequence, s.TaxId, s.FirstName, s.LastName, s.IsDirector, s.IsShareholder, s.IsJuristic)
            .SetCheckType(checkType)
            .SetWatchlist(s.WatchlistResult, s.WatchlistResultRemark, s.WatchlistResultAt)
            .SetCoi(s.CoiResult, s.CoiResultRemark, s.CoiResultAt)
            .SetEgp(s.EgpResult, s.EgpRemark, s.EgpResultAt);

        if (s.CoiCheckerResult is not null)
        {
            newShareholder.AddChecker(QualificationType.COI, s.CoiCheckerResult.Result, s.CoiCheckerResult.ResultAt, s.CoiCheckerResult.Remark);
        }

        if (s.WatchlistCheckerResult is not null)
        {
            newShareholder.AddChecker(QualificationType.Watchlist, s.WatchlistCheckerResult.Result, s.WatchlistCheckerResult.ResultAt, s.WatchlistCheckerResult.Remark);
        }

        entrepreneurs.AddShareholder(newShareholder);
    }

    private static void UpdateExistingShareholder(PPrincipleApprovalRentalEntrepreneursShareholders existing, Dto.ShareholderDto s)
    {
        existing.Update(s.Sequence, s.TaxId, s.FirstName, s.LastName, s.IsDirector, s.IsShareholder, s.IsJuristic)
                .SetWatchlist(s.WatchlistResult, s.WatchlistResultRemark, s.WatchlistResultAt)
                .SetCoi(s.CoiResult, s.CoiResultRemark, s.CoiResultAt)
                .SetEgp(s.EgpResult, s.EgpRemark, s.EgpResultAt);

        if (s.CoiCheckerResult is not null)
        {
            existing.AddChecker(QualificationType.COI, s.CoiCheckerResult.Result, s.CoiCheckerResult.ResultAt, s.CoiCheckerResult.Remark);
        }

        if (s.WatchlistCheckerResult is not null)
        {
            existing.AddChecker(QualificationType.Watchlist, s.WatchlistCheckerResult.Result, s.WatchlistCheckerResult.ResultAt, s.WatchlistCheckerResult.Remark);
        }
    }

    private static void UpsertShareholder(PPrincipleApprovalRentalEntrepreneurs entrepreneurs, Dto.ShareholderDto[]? shareholders)
    {
        var newDetails = shareholders ?? [];

        if (newDetails.Length == 0)
        {
            var all = entrepreneurs.EntrepreneursShareholders?.ToList() ?? [];
            foreach (var shareholder in all)
            {
                entrepreneurs.RemoveShareholder(shareholder);
            }

            return;
        }

        var allKnownIds = newDetails
            .SelectMany(s => new[] { s.CoiId, s.WatchlistId })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToHashSet();

        var toRemove = entrepreneurs.EntrepreneursShareholders?
            .Where(a => !allKnownIds.Contains(a.Id.Value))
            .ToList() ?? [];

        foreach (var shareholder in toRemove)
        {
            entrepreneurs.RemoveShareholder(shareholder);
        }

        foreach (var s in newDetails)
        {
            var processTypes = s.CheckType != null
                ? new[] { s.CheckType }
                : new[] { "COI", "Watchlist" };

            foreach (var checkType in processTypes)
            {
                var id = checkType == "COI" ? s.CoiId : s.WatchlistId;
                ProcessShareholder(entrepreneurs, s, id, checkType);
            }
        }
    }

    private static void ProcessShareholder(PPrincipleApprovalRentalEntrepreneurs entrepreneurs, Dto.ShareholderDto s, Guid? id, string checkType)
    {
        var existing = id.HasValue
            ? entrepreneurs.EntrepreneursShareholders?.FirstOrDefault(a => a.Id == PPrincipleApprovalRentalEntrepreneursShareholdersId.From(id.Value))
            : null;

        if (existing != null)
        {
            existing.Update(
                        s.Sequence,
                        s.TaxId,
                        s.FirstName,
                        s.LastName,
                        s.IsDirector,
                        s.IsShareholder,
                        s.IsJuristic)
                    .SetWatchlist(s.WatchlistResult, s.WatchlistResultRemark, s.WatchlistResultAt)
                    .SetCoi(s.CoiResult, s.CoiResultRemark, s.CoiResultAt)
                    .SetEgp(s.EgpResult, s.EgpRemark, s.EgpResultAt);
        }
        else
        {
            var newEntity = PPrincipleApprovalRentalEntrepreneursShareholders
                            .Create(
                                s.Sequence,
                                s.TaxId,
                                s.FirstName,
                                s.LastName,
                                s.IsDirector,
                                s.IsShareholder,
                                s.IsJuristic)
                            .SetCheckType(checkType)
                            .SetWatchlist(s.WatchlistResult, s.WatchlistResultRemark, s.WatchlistResultAt)
                            .SetCoi(s.CoiResult, s.CoiResultRemark, s.CoiResultAt)
                            .SetEgp(s.EgpResult, s.EgpRemark, s.EgpResultAt);

            entrepreneurs.AddShareholder(newEntity);
        }
    }

    protected void UpsertEntrepreneurPriceDetail(PPrincipleApprovalRentalEntrepreneurs entrepreneurs, EntrepreneursPriceDetailDto[]? priceDetail)
    {
        var existingDetails = entrepreneurs.EntrepreneursPriceDetails?.ToList() ?? new List<PPrincipleApprovalRentalEntrepreneursPriceDetails>();
        var newDetails = priceDetail ?? [];

        // Update or add
        foreach (var s in newDetails)
        {
            var existing = existingDetails.FirstOrDefault(x => x.Id.Value == s.Id);

            if (existing != null)
            {
                existing.SetDetails(
                    s.Sequence,
                    s.ParcelName,
                    s.ParcelQuantity,
                    ParameterCode.From(s.ParcelUnitCode),
                    ParameterCode.From(s.VatTypeCode),
                    s.OfferedPrice,
                    s.AgreedPrice,
                    s.Description);
            }
            else
            {
                var newEntity = PPrincipleApprovalRentalEntrepreneursPriceDetails
                                .Create()
                                .SetDetails(
                                    s.Sequence,
                                    s.ParcelName,
                                    s.ParcelQuantity,
                                    ParameterCode.From(s.ParcelUnitCode),
                                    ParameterCode.From(s.VatTypeCode),
                                    s.OfferedPrice,
                                    s.AgreedPrice,
                                    s.Description);

                entrepreneurs.AddPriceDetail(newEntity);
            }
        }

        // Remove obsolete
        foreach (var toRemove in existingDetails.Where(x => newDetails.All(d => d.Id != x.Id.Value)).ToList())
        {
            entrepreneurs.RemovePriceDetail(toRemove);
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

    protected PrincipleApprovalRentalResponseDto MapToResponse(
        PPrincipleApprovalRental approvalRental,
        bool hasPermission)
    {
        var perfSupportData = approvalRental.PerfSupportData.FirstOrDefault();
        var perfSupportDataDetails = approvalRental.PerfSupportDataDetails;
        var roiLoanAndDepositSummaries = approvalRental.RoiLoanAndDepositSummaries;
        var roiPerfResults = approvalRental.RoiPerfResults;
        var assignee = approvalRental.Assignees;

        var approvalDoc =
            approvalRental.DocumentHistories
                          .Where(w => w.DocumentType == PPrincipleApprovalRentalDocumentType.Approval)
                          .OrderVersions()
                          .FirstOrDefault();

        var winnerDoc =
            approvalRental.DocumentHistories
                          .Where(w => w.DocumentType == PPrincipleApprovalRentalDocumentType.Winner)
                          .OrderVersions()
                          .FirstOrDefault();

        var isReplacedApprovalDoc =
            approvalRental.DocumentHistories
                          .Any(w => w.DocumentType == PPrincipleApprovalRentalDocumentType.Approval && w.IsReplaced);

        var isReplacedWinnerDoc =
            approvalRental.DocumentHistories
                          .Any(w => w.DocumentType == PPrincipleApprovalRentalDocumentType.Winner && w.IsReplaced);

        var documentVersions = approvalRental.DocumentHistories
                                             .Where(w => w.DocumentType == PPrincipleApprovalRentalDocumentType.Approval)
                                             .OrderVersions()
                                             .Select((d, index) => new PrincipleApprovalRentalDocumentVersionResponse(
                                                 d.FileId.Value,
                                                 d.Version,
                                                 d.CreatedAt,
                                                 d.CreatedByName ?? string.Empty,
                                                 index == 0))
                                             .ToArray();

        var winnerDocumentVersions = approvalRental.DocumentHistories
                                                   .Where(w => w.DocumentType == PPrincipleApprovalRentalDocumentType.Winner)
                                                   .OrderVersions()
                                                   .Select((d, index) => new PrincipleApprovalRentalDocumentVersionResponse(
                                                       d.FileId.Value,
                                                       d.Version,
                                                       d.CreatedAt,
                                                       d.CreatedByName ?? string.Empty,
                                                       index == 0))
                                                   .ToArray();

        var acceptorsApprover =
            approvalRental.Acceptors
                          .Where(a => a.Type != AcceptorType.RentCommittee)
                          .Select(DelegatorExtensions.DelegatorToAcceptor)
                          .ToList();

        var committee =
            approvalRental.Acceptors
                          .Where(a => a.Type == AcceptorType.RentCommittee)
                          .ToList();

        var acceptors = acceptorsApprover.Union(committee)
                                         .ToArray();

        var attachment = approvalRental.ComparingAttachments?
                                       .OrderBy(c => c.Sequence)
                                       .Select(c => new FileAttachmentsWithId(
                                           Id: null,
                                           FileId: c.Id.Value,
                                           FileName: c.FileName,
                                           Sequence: c.Sequence,
                                           IsPublic: c.IsPublic,
                                           CreatedBy: c.AuditInfo.CreatedBy)) ?? [];

        return new PrincipleApprovalRentalResponseDto(
            approvalRental.Id.Value,
            approvalRental.ProcurementId.Value,
            approvalDoc?.FileId.Value,
            false,
            isReplacedApprovalDoc,
            documentVersions,
            winnerDoc?.FileId.Value,
            false,
            isReplacedWinnerDoc,
            winnerDocumentVersions,
            approvalRental.UseContract,
            approvalRental.BranchLocation,
            approvalRental.RentTypeCode.Value,
            approvalRental.RentTypeCodeInfo?.Label ?? string.Empty,
            approvalRental.RentalStartDate,
            approvalRental.RentalEndDate,
            approvalRental.RentalDurationYear,
            approvalRental.RentalDurationMonth,
            approvalRental.RentalDurationDay,
            approvalRental.MaxMonthlyRent,
            approvalRental.TotalRentalAmount,
            approvalRental.ExpectedContractDate,
            approvalRental.RentalLocationDetails,
            approvalRental.SubDistrictCode,
            approvalRental.SubDistrictName,
            approvalRental.DistrictCode,
            approvalRental.DistrictName,
            approvalRental.ProvinceCode,
            approvalRental.ProvinceName,
            approvalRental.ReferencePriceAmount,
            approvalRental.AnalysisSummaryNpv,
            approvalRental.AnalysisSummaryPaybackYearPeriod,
            approvalRental.AnalysisSummaryDiscountedPaybackYearPeriod,
            approvalRental.PhoneNumber,
            approvalRental.Status,
            acceptors.Select(a => new PrincipleApprovalRentalAcceptorResponseDto(
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
                         a.CommitteePositionsCode.IsNull() ? null : (string?)a.CommitteePositionsCode,
                         a.CommitteePosition?.Label,
                         a.IsUnableToPerformDuties,
                         a.IsCurrentApprover(),
                         a.Delegatee?.SuUserId.Value))
                     .OrderBy(o => o.AcceptorType)
                     .ThenBy(o => o.Sequence),
            assignee.Select(DelegatorExtensions.DelegatorToAssignee)
                    .Select(a => new PrincipleApprovalRentalAssigneeResponseDto(
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
                        a.ActionAt,
                        a.Delegatee?.SuUserId.Value))
                    .OrderBy(x => x.Sequence),
            new PrincipleApprovalRentalPerfSupportDataResponseDto(
                perfSupportData!.Id.Value,
                perfSupportData.TransactionVolume,
                perfSupportData.ActivityDescription,
                perfSupportData.PeriodYear,
                perfSupportData.StartMonth,
                perfSupportData.EndMonth),
            perfSupportDataDetails
                .Select(d => new PrincipleApprovalRentalPerfSupportDataDetailResponseDto(
                    d.Id.Value,
                    d.Sequence,
                    d.ActivityDescription,
                    d.AccountCountYear1,
                    d.AmountYear1,
                    d.AccountCountYear2,
                    d.AmountYear2))
                .OrderBy(x => x.Sequence),
            roiLoanAndDepositSummaries
                .Select(r => new PrincipleApprovalRentalRoiLoanAndDepositSummaryResponseDto(
                    r.Id.Value,
                    r.Sequence,
                    r.ActivityDescription,
                    r.AmountYear1,
                    r.AmountYear2,
                    r.AmountYear3))
                .OrderBy(x => x.Sequence),
            roiPerfResults
                .Select(r => new PrincipleApprovalRentalRoiPerfResultResponseDto(
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
            approvalRental.Budgets
                          .OrderBy(o => o.Sequence)
                          .Select(r => new PrincipleApprovalRentalBudgetDto(
                              r.Id.Value,
                              r.Sequence,
                              r.Description,
                              r.BudgetAmount,
                              r.PrincipleApprovalRentalBudgetDetails
                               .OrderBy(d => d.Sequence)
                               .Select(d => new PrincipleApprovalRentalBudgetDetail(
                                   d.Id.Value,
                                   d.Sequence,
                                   d.Department,
                                   d.BudgetType,
                                   d.ProjectCode,
                                   d.AccountNo,
                                   d.Budget)))),
            approvalRental.RentalAnalyses
                          .OrderBy(r => r.Sequence)
                          .Select(r => new PrincipleApprovalRentalRentalAnalysisDto(
                              r.Id.Value,
                              r.Sequence,
                              r.Type,
                              r.Description,
                              r.PrincipleApprovalRentalRentalAnalysisDetails
                               .OrderBy(d => d.Year)
                               .Select(d => new PrincipleApprovalRentalRentalAnalysisDetail(
                                   d.Id.Value,
                                   d.Year,
                                   d.Amount)))),
            approvalRental.Entrepreneurs
                          .Select(r =>
                          {
                              var coiChecker =
                                  r.Checkers
                                   .OrderByDescending(c => c.ResultAt)
                                   .FirstOrDefault(c => c.CheckType == QualificationType.COI);

                              var watchlistChecker =
                                  r.Checkers
                                   .OrderByDescending(c => c.ResultAt)
                                   .FirstOrDefault(c => c.CheckType == QualificationType.Watchlist);

                              var coiCheckerResult = coiChecker is null
                                  ? null
                                  : new QualificationResultDto(
                                      coiChecker.Result,
                                      coiChecker.ResultAt,
                                      coiChecker.Remark);

                              var watchlistCheckerResult = watchlistChecker is null
                                  ? null
                                  : new QualificationResultDto(
                                      watchlistChecker.Result,
                                      watchlistChecker.ResultAt,
                                      watchlistChecker.Remark);

                              return new PrincipleApprovalRentalEntrepreneursResponseDto(
                                  r.Id.Value,
                                  r.Vendor.Id.Value,
                                  r.Sequence,
                                  r.EmailSend,
                                  r.WatchlistResult,
                                  r.WatchlistResultRemark,
                                  r.WatchlistResultAt,
                                  r.CoiResult,
                                  r.CoiResultRemark,
                                  r.CoiResultAt,
                                  r.EgpResult,
                                  r.EgpResultRemark,
                                  r.EgpResultAt,
                                  r.Vendor.TaxpayerIdentificationNo,
                                  r.Vendor.EntrepreneurType.Value,
                                  r.Vendor.EntrepreneurTypeInfo.Label,
                                  r.Vendor.EstablishmentName,
                                  r.Vendor.Email,
                                  [
                                      .. r.EntrepreneursPriceDetails.Map(s =>
                                          new Dto.EntrepreneursPriceDetailDto(
                                              s.Id.Value,
                                              s.Sequence,
                                              s.ParcelName,
                                              s.ParcelQuantity,
                                              (string)s.ParcelUnitCode,
                                              s.VatTypeCode.Value,
                                              s.OfferedPrice,
                                              s.AgreedPrice,
                                              s.Description))
                                  ],
                                  [
                                      .. r.EntrepreneursShareholders.Select(s =>
                                          {
                                              var shareholdersCoiChecker = s.Checkers
                                                                            .OrderByDescending(c => c.ResultAt)
                                                                            .FirstOrDefault(c => c.CheckType == QualificationType.COI);

                                              var shareholdersWatchlistChecker = s.Checkers
                                                                                  .OrderByDescending(c => c.ResultAt)
                                                                                  .FirstOrDefault(c => c.CheckType == QualificationType.Watchlist);

                                              var shareholdersCoiCheckerResult = shareholdersCoiChecker is null
                                                  ? null
                                                  : new QualificationResultDto(
                                                      shareholdersCoiChecker.Result,
                                                      shareholdersCoiChecker.ResultAt,
                                                      shareholdersCoiChecker.Remark);

                                              var shareholdersWatchlistCheckerResult = shareholdersWatchlistChecker is null
                                                  ? null
                                                  : new QualificationResultDto(
                                                      shareholdersWatchlistChecker.Result,
                                                      shareholdersWatchlistChecker.ResultAt,
                                                      shareholdersWatchlistChecker.Remark);

                                              return new PrincipleApprovalRentalEntrepreneursShareholderDto(
                                                  s.Id.Value,
                                                  s.Sequence,
                                                  s.TaxId,
                                                  s.FirstName,
                                                  s.LastName,
                                                  s.IsDirector,
                                                  s.IsShareholder,
                                                  s.IsJuristic,
                                                  s.CheckType,
                                                  s.WatchlistResult,
                                                  s.WatchlistResultRemark,
                                                  s.WatchlistResultAt,
                                                  s.CoiResult,
                                                  s.CoiResultRemark,
                                                  s.CoiResultAt,
                                                  s.EgpResult,
                                                  s.EgpRemark,
                                                  s.EgpResultAt,
                                                  shareholdersCoiCheckerResult,
                                                  shareholdersWatchlistCheckerResult);
                                          })
                                          .OrderBy(s => s.Sequence)
                                  ],
                                  coiCheckerResult,
                                  watchlistCheckerResult,
                                  r.Vendor.SapBranchNumber);
                          })
                          .OrderBy(x => x.Sequence),
            approvalRental.ComparingAttachments?.OrderBy(a => a.Sequence).Select(a => new ComparingAttachmentsDto(a.Id.Value, a.Id.Value, a.FileName, a.Sequence, a.IsPublic)) ?? [],
            hasPermission,
            approvalRental.DocumentDate);
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

    private PrincipleApprovalReplaceDto MapToPrincipleReplaceDtoAsync(
        PPrincipleApproval principleApproval,
        PPrincipleApprovalRental principleApprovalRental)
    {
        var perfSupportData = principleApproval.PerfSupportData.FirstOrDefault();

        var approverList = principleApproval.PrincipleApprovalAcceptors
                                            .Where(a => a is
                                            {
                                                Type: AcceptorType.Approver,
                                                Status: AcceptorStatus.Approved
                                            })
                                            .OrderBy(a => a.Sequence)
                                            .ToList();

        var acceptors = approverList
            .Select((a, index) =>
                new PrincipleApprovalAcceptorReplaceDto(
                    a.Id.Value,
                    index == approverList.Count - 1 ? "อนุมัติ" : "เห็นชอบ",
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

        var departmentApproverList = principleApproval.PrincipleApprovalAcceptors
                                                      .Where(a => a is
                                                      {
                                                          Type: AcceptorType.DepartmentDirectorAgree,
                                                          Status: AcceptorStatus.Approved
                                                      })
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
            principleApproval.PrincipleApprovalAssignees
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
            principleApproval.PrincipleApprovalCommittees
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
            principleApproval.PrincipleApprovalCommittees
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
            principleApproval.PerfSupportDataDetails
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
            principleApproval.RoiLoanAndDepositSummaries
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
            principleApproval.RoiPerfResults
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
            principleApproval.Procurement.ProcurementNumber.HasValue
                ? principleApproval.Procurement.ProcurementNumber.Value.ToString()
                : string.Empty;

        var procurementReplace =
            new ProcurementReplaceDto(
                principleApproval.Procurement.PlanId.HasValue
                    ? (Guid)principleApproval.Procurement.PlanId
                    : null,
                procurementNumber,
                principleApproval.Procurement.Type,
                principleApproval.Procurement.Step,
                principleApproval.Procurement.Department.Name,
                principleApproval.Procurement.DepartmentId,
                principleApproval.Procurement.Plan?.PlanNumber.ToString() ?? string.Empty,
                principleApproval.Procurement.Name,
                (principleApproval.Procurement.Budget ?? 0).ToCurrencyStringWithComma(),
                principleApproval.Procurement.Budget.ThaiBahtText(),
                principleApproval.Procurement.BudgetYear,
                principleApproval.Procurement.SupplyMethod.Label,
                principleApproval.Procurement.SupplyMethodCode,
                principleApproval.Procurement.SupplyMethodType?.Label ?? string.Empty,
                principleApproval.Procurement.SupplyMethodTypeCode,
                principleApproval.Procurement.SupplyMethodSpecialType?.Label ?? string.Empty,
                principleApproval.Procurement.SupplyMethodSpecialTypeCode,
                principleApproval.Procurement.Status,
                principleApproval.Procurement.ExpectingProcurementAt,
                principleApproval.Procurement.IsStock,
                principleApproval.Procurement.IsCommercialMaterial,
                principleApproval.Procurement.Plan?.Type,
                principleApproval.Procurement.ProcessType);

        var approvalBudgets =
            principleApproval.PrincipleApprovalBudgets
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

        var approvalRentalAnalyses =
            principleApproval.PrincipleApprovalRentalAnalyses
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
                                      d.Amount))));

        var acceptorDate =
            principleApproval.Status == PPrincipleApprovalStatus.WaitingUnitApproval ||
            principleApproval.Status == PPrincipleApprovalStatus.WaitingAssign ||
            principleApproval.Status == PPrincipleApprovalStatus.WaitingAcceptance
                ? DateTimeOffset.Now.ToThaiDateString(includeBuddhistEra: false)
                : null;

        var sectionApprover = new List<SectionApprove>
        {
            new SectionApprove(string.Empty),
        };

        var budgetAccountsNo = approvalBudgets.SelectMany(a => a.Details).Select(a => a.AccountNo);

        var accountNo = new List<string>();
        var commandText = string.Empty;

        var lastAssignee = (principleApproval.Status is PPrincipleApprovalStatus.WaitingComment
            or PPrincipleApprovalStatus.WaitingAcceptance
            or PPrincipleApprovalStatus.Approved)
            ? principleApproval.PrincipleApprovalAssignees
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
                principleApproval.Id.Value,
                acceptorDate,
                sectionApprover,
                principleApproval.ProcurementId.Value,
                procurementReplace,
                principleApprovalRental.BranchLocation,
                principleApproval.DocumentTemplateId.Value,
                principleApprovalRental.RentTypeCode.Value,
                principleApprovalRental.RentalStartDate.ToThaiDateString(includeBuddhistEra: false),
                principleApprovalRental.RentalEndDate.ToThaiDateString(includeBuddhistEra: false),
                principleApprovalRental.RentalDurationYear > 0 ? string.Format("{0} ปี", principleApprovalRental.RentalDurationYear) : string.Empty,
                principleApprovalRental.RentalDurationMonth > 0 ? string.Format("{0} เดือน", principleApprovalRental.RentalDurationMonth) : string.Empty,
                principleApprovalRental.RentalDurationDay > 0 ? string.Format("{0} วัน", principleApprovalRental.RentalDurationDay) : string.Empty,
                principleApprovalRental.MaxMonthlyRent.ToCurrencyStringWithComma(),
                principleApprovalRental.MaxMonthlyRent.ThaiBahtText(),
                principleApprovalRental.TotalRentalAmount.ToCurrencyStringWithComma(),
                principleApprovalRental.TotalRentalAmount.ThaiBahtText(),
                principleApprovalRental.ExpectedContractDate.ToThaiDateString(includeBuddhistEra: false),
                principleApprovalRental.RentalLocationDetails,
                principleApproval.PhoneNumber ?? string.Empty,
                principleApprovalRental.SubDistrictCode,
                principleApprovalRental.SubDistrictName,
                principleApprovalRental.DistrictCode,
                principleApprovalRental.DistrictName,
                principleApprovalRental.ProvinceCode,
                principleApprovalRental.ProvinceName,
                principleApproval.ReferencePriceAmount.ToCurrencyStringWithComma() ?? string.Empty,
                principleApproval.ReferencePriceAmount.ThaiBahtText() ?? string.Empty,
                principleApproval.AnalysisSummaryNpv.ToCurrencyStringWithComma(),
                principleApproval.AnalysisSummaryNpv.ThaiBahtText(),
                principleApproval.AnalysisSummaryPaybackYearPeriod,
                principleApproval.AnalysisSummaryDiscountedPaybackYearPeriod,
                principleApproval.Status,
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
                principleApproval.IsRentCommittee,
                principleApproval.IsAcceptanceCommittee,
                null,
                commandText,
                jorPorCommentReplace);

        return replaceDtoResult;
    }

    protected async Task<PrincipleApprovalRentalReplaceDto> MapToReplaceDto(
        PPrincipleApprovalRental entity,
        PPrincipleApproval principleEntity,
        CancellationToken ct,
        UserId? creatorUserId,
        bool hasAcceptor)
    {
        var approvalDoc = GetLastDocumentByType(PPrincipleApprovalRentalDocumentType.Approval);
        var winnerDoc = GetLastDocumentByType(PPrincipleApprovalRentalDocumentType.Winner);

        var perfSupportData = entity.PerfSupportData.FirstOrDefault();
        var perfSupportDataDetails = entity.PerfSupportDataDetails;
        var roiLoanAndDepositSummaries = entity.RoiLoanAndDepositSummaries;
        var roiPerfResults = entity.RoiPerfResults;
        var assignee = entity.Assignees;

        CreatorReplace? creatorReplace = await GetCreatorReplaceAsync();

        var acceptorDate = entity.Status is not
                (PPrincipleApprovalRentalStatus.Draft
                or PPrincipleApprovalRentalStatus.Edit
                or PPrincipleApprovalRentalStatus.Rejected)
         ? entity.DocumentDate?.ToThaiDateString(includeBuddhistEra: false) ?? DateTimeOffset.Now.ToThaiDateString(includeBuddhistEra: false)
         : null;

        var acceptorsReplace = hasAcceptor ? GetAcceptorReplace() : [];
        var publisherReplace = GetPublisherReplace();
        var assigneeReplace = GetAssigneeReplace();
        var perfSupportDataDetailsReplace = GetPerfSupportDataDetailsReplace();
        var roiLoanAndDepositSummariesReplace = GetRoiLoanAndDepositSummariesReplace();
        var roiPerfResultsReplace = GetRoiPerfResultsReplace();
        var budgetsReplace = GetBudgetsReplace();
        var rentalAnalysesReplace = GetRentalAnalysesReplace();
        var entrepreneursReplace = GetEntrepreneursReplace();
        var perfSupport =
            new PrincipleApprovalRentalPerfSupportDataResponseDto(
                perfSupportData!.Id.Value,
                perfSupportData.TransactionVolume,
                perfSupportData.ActivityDescription,
                perfSupportData.PeriodYear,
                perfSupportData.StartMonth,
                perfSupportData.EndMonth);

        var entrepreneursName = entrepreneursReplace.Select(e => e.EntrepreneurName);

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

        var principleReplaceDto =
            this.MapToPrincipleReplaceDtoAsync(
        principleEntity,
        entity);

        var lastAssignee = (entity.Status is PPrincipleApprovalRentalStatus.WaitingComment
            or PPrincipleApprovalRentalStatus.WaitingAcceptance
            or PPrincipleApprovalRentalStatus.Approved
            or PPrincipleApprovalRentalStatus.WaitingContractAssign
            or PPrincipleApprovalRentalStatus.ContractAssigned)
            ? entity.Assignees
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

        var result =
            new PrincipleApprovalRentalReplaceDto(
                principleReplaceDto,
                entity.Id.Value,
                entity.ProcurementId.Value,
                procurementReplace,
                approvalDoc?.FileId.Value,
                approvalDoc?.IsReplaced ?? false,
                winnerDoc?.FileId.Value,
                winnerDoc?.IsReplaced ?? false,
                entity.UseContract,
                entity.ReferencePriceAmount,
                entity.AnalysisSummaryNpv,
                entity.AnalysisSummaryPaybackYearPeriod,
                entity.AnalysisSummaryDiscountedPaybackYearPeriod,
                entity.PhoneNumber ?? string.Empty,
                entity.Status,
                acceptorsReplace,
                assigneeReplace,
                perfSupport,
                perfSupportDataDetailsReplace,
                roiLoanAndDepositSummariesReplace,
                roiPerfResultsReplace,
                budgetsReplace,
                rentalAnalysesReplace,
                entrepreneursReplace,
                string.Join(", ", entrepreneursName),
                creatorReplace,
                publisherReplace,
                jorPorCommentReplace,
                acceptorDate);

        return result;

        IEnumerable<PrincipleApprovalRentalAssigneeResponseDto> GetAssigneeReplace()
        {
            return assignee
                   .Select(DelegatorExtensions.DelegatorToAssignee)
                   .Select(a =>
                       new PrincipleApprovalRentalAssigneeResponseDto(
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
                           a.ActionAt))
                   .OrderBy(x => x.Sequence);
        }

        IEnumerable<PrincipleApprovalRentalPerfSupportDataDetailResponseDto> GetPerfSupportDataDetailsReplace()
        {
            return perfSupportDataDetails
                   .Select(d =>
                       new PrincipleApprovalRentalPerfSupportDataDetailResponseDto(
                           d.Id.Value,
                           d.Sequence,
                           d.ActivityDescription,
                           d.AccountCountYear1,
                           d.AmountYear1,
                           d.AccountCountYear2,
                           d.AmountYear2))
                   .OrderBy(x => x.Sequence);
        }

        IEnumerable<PrincipleApprovalRentalRoiLoanAndDepositSummaryResponseDto> GetRoiLoanAndDepositSummariesReplace()
        {
            return roiLoanAndDepositSummaries
                   .Select(r =>
                       new PrincipleApprovalRentalRoiLoanAndDepositSummaryResponseDto(
                           r.Id.Value,
                           r.Sequence,
                           r.ActivityDescription,
                           r.AmountYear1,
                           r.AmountYear2,
                           r.AmountYear3))
                   .OrderBy(x => x.Sequence);
        }

        IEnumerable<PrincipleApprovalRentalRoiPerfResultResponseDto> GetRoiPerfResultsReplace()
        {
            return roiPerfResults
                   .Select(r =>
                       new PrincipleApprovalRentalRoiPerfResultResponseDto(
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
                   .OrderBy(x => x.Sequence);
        }

        IEnumerable<PrincipleApprovalRentalBudgetDto> GetBudgetsReplace()
        {
            return
                entity.Budgets
                      .OrderBy(o => o.Sequence)
                      .Select(r =>
                          new PrincipleApprovalRentalBudgetDto(
                              r.Id.Value,
                              r.Sequence,
                              r.Description,
                              r.BudgetAmount,
                              r.PrincipleApprovalRentalBudgetDetails
                               .OrderBy(d => d.Sequence)
                               .Select(d =>
                                   new PrincipleApprovalRentalBudgetDetail(
                                       d.Id.Value,
                                       d.Sequence,
                                       d.Department,
                                       d.BudgetType,
                                       d.ProjectCode,
                                       d.AccountNo,
                                       d.Budget))));
        }

        IEnumerable<PrincipleApprovalRentalRentalAnalysisDto> GetRentalAnalysesReplace()
        {
            return
                entity.RentalAnalyses
                      .OrderBy(r => r.Sequence)
                      .Select(r => new PrincipleApprovalRentalRentalAnalysisDto(
                          r.Id.Value,
                          r.Sequence,
                          r.Type,
                          r.Description,
                          r.PrincipleApprovalRentalRentalAnalysisDetails
                           .OrderBy(d => d.Year)
                           .Select(d =>
                               new PrincipleApprovalRentalRentalAnalysisDetail(
                                   d.Id.Value,
                                   d.Year,
                                   d.Amount))));
        }

        IEnumerable<PrincipleApprovalRentalEntrepreneursResponseDto> GetEntrepreneursReplace()
        {
            return
                entity
                    .Entrepreneurs
                    .Select(r =>
                    {
                        var coiChecker =
                            r.Checkers
                             .OrderByDescending(c => c.ResultAt)
                             .FirstOrDefault(c => c.CheckType == QualificationType.COI);

                        var watchlistChecker =
                            r.Checkers
                             .OrderByDescending(c => c.ResultAt)
                             .FirstOrDefault(c => c.CheckType == QualificationType.Watchlist);

                        var coiCheckerResult = coiChecker is null
                            ? null
                            : new QualificationResultDto(
                                coiChecker.Result,
                                coiChecker.ResultAt,
                                coiChecker.Remark);

                        var watchlistCheckerResult = watchlistChecker is null
                            ? null
                            : new QualificationResultDto(
                                watchlistChecker.Result,
                                watchlistChecker.ResultAt,
                                watchlistChecker.Remark);

                        return new PrincipleApprovalRentalEntrepreneursResponseDto(
                            r.Id.Value,
                            r.Vendor.Id.Value,
                            r.Sequence,
                            r.EmailSend,
                            r.WatchlistResult,
                            r.WatchlistResultRemark,
                            r.WatchlistResultAt,
                            r.CoiResult,
                            r.CoiResultRemark,
                            r.CoiResultAt,
                            r.EgpResult,
                            r.EgpResultRemark,
                            r.EgpResultAt,
                            r.Vendor.TaxpayerIdentificationNo,
                            r.Vendor.EntrepreneurType.Value,
                            r.Vendor.EntrepreneurTypeInfo?.Label ?? string.Empty,
                            r.Vendor.EstablishmentName,
                            r.Vendor.Email,
                            [
                                .. (r.EntrepreneursPriceDetails ?? []).Map(s =>
                                    new Dto.EntrepreneursPriceDetailDto(
                                        s.Id.Value,
                                        s.Sequence,
                                        s.ParcelName,
                                        s.ParcelQuantity,
                                        (string)s.ParcelUnitCode,
                                        s.VatTypeCode.Value,
                                        s.OfferedPrice,
                                        s.AgreedPrice,
                                        s.Description))
                            ],
                            [
                                .. (r.EntrepreneursShareholders ?? []).Select(s =>
                                    {
                                        var shareholdersCoiChecker = (s.Checkers ?? [])
                                                                      .OrderByDescending(c => c.ResultAt)
                                                                      .FirstOrDefault(c => c.CheckType == QualificationType.COI);

                                        var shareholdersWatchlistChecker = (s.Checkers ?? [])
                                                                            .OrderByDescending(c => c.ResultAt)
                                                                            .FirstOrDefault(c => c.CheckType == QualificationType.Watchlist);

                                        var shareholdersCoiCheckerResult = shareholdersCoiChecker is null
                                            ? null
                                            : new QualificationResultDto(
                                                shareholdersCoiChecker.Result,
                                                shareholdersCoiChecker.ResultAt,
                                                shareholdersCoiChecker.Remark);

                                        var shareholdersWatchlistCheckerResult = shareholdersWatchlistChecker is null
                                            ? null
                                            : new QualificationResultDto(
                                                shareholdersWatchlistChecker.Result,
                                                shareholdersWatchlistChecker.ResultAt,
                                                shareholdersWatchlistChecker.Remark);

                                        return new PrincipleApprovalRentalEntrepreneursShareholderDto(
                                            s.Id.Value,
                                            s.Sequence,
                                            s.TaxId,
                                            s.FirstName,
                                            s.LastName,
                                            s.IsDirector,
                                            s.IsShareholder,
                                            s.IsJuristic,
                                            s.CheckType,
                                            s.WatchlistResult,
                                            s.WatchlistResultRemark,
                                            s.WatchlistResultAt,
                                            s.CoiResult,
                                            s.CoiResultRemark,
                                            s.CoiResultAt,
                                            s.EgpResult,
                                            s.EgpRemark,
                                            s.EgpResultAt,
                                            shareholdersCoiCheckerResult,
                                            shareholdersWatchlistCheckerResult);
                                    })
                                    .OrderBy(s => s.Sequence)
                            ],
                            coiCheckerResult,
                            watchlistCheckerResult,
                            r.Vendor.SapBranchNumber);
                    })
                    .OrderBy(x => x.Sequence)
                    .ToArray();
        }

        AcceptorReplace[] GetAcceptorReplace()
        {
            AcceptorReplace[] acceptors =
            [
                .. entity.Acceptors
                         .Where(a => a.Type == AcceptorType.Approver)
                         .Select(DelegatorExtensions.DelegatorToAcceptor)
                         .Map(MapAcceptorReplace)
                         .OrderBy(a => a.Sequence)
            ];

            if (acceptors.Any())
            {
                acceptors[^1] =
                    acceptors.Last() with { Action = "อนุมัติ" };
            }

            return [.. acceptors.Where(a => a.Status == AcceptorStatus.Approved)];
        }

        PublisherDto? GetPublisherReplace()
        {
            var approverList =
                entity.Acceptors
                      .Where(a => a.Type == AcceptorType.Approver)
                      .Where(a => !a.IsUnableToPerformDuties)
                      .Select(DelegatorExtensions.DelegatorToAcceptor)
                      .ToArray();

            if (!approverList.Any())
            {
                return null;
            }

            var approverApproveAll =
                approverList.All(a => a.Status == AcceptorStatus.Approved);

            if (!approverApproveAll)
            {
                return null;
            }

            var publisherUser =
                approverList.MaxBy(a => a.Sequence)!;

            return
                new PublisherDto(
                    publisherUser.Delegatee != null ? publisherUser.SignatureDelegatee : publisherUser.Signature,
                    publisherUser.FullName,
                    publisherUser.PositionName,
                    string.Empty,
                    string.Empty,
                    DateTimeOffset.Now.ToThaiDateString(includeBuddhistEra: false));
        }

        AcceptorReplace MapAcceptorReplace(PPrincipleApprovalRentalAcceptor acceptor)
        {
            return new AcceptorReplace(
                acceptor.UserId.Value,
                acceptor.Sequence,
                "เห็นชอบ",
                acceptor.User?.FullName,
                acceptor.FullName,
                acceptor.User?.Employee.View?.FullPositionName ?? string.Empty,
                " ",
                string.Empty,
                acceptor.Status);
        }

        async Task<CreatorReplace?> GetCreatorReplaceAsync()
        {
            if (entity.Status == PPrincipleApprovalRentalStatus.Draft)
            {
                return null;
            }

            var sendToCommitteeApproveByUser =
                creatorUserId is not null
                    ? await this.dbContext.SuUsers
                                .Include(suUser => suUser.Employee)
                                .ThenInclude(rawEmployee => rawEmployee.View)
                                .FirstOrDefaultAsync(u => u.Id == creatorUserId, ct)
                    : await this.GetLastActivityCreatedByAsync(
                        entity.Id.ToString(),
                        ActivityLogActionTypeConstant.SendCommitteeApprove,
                        ct);

            if (sendToCommitteeApproveByUser == null)
            {
                return null;
            }

            return new CreatorReplace(
                sendToCommitteeApproveByUser.Id.Value,
                "ผู้จัดทำ",
                sendToCommitteeApproveByUser.FullName,
                sendToCommitteeApproveByUser.Employee.View?.FullPositionName ?? string.Empty,
                string.Empty);
        }

        PPrincipleApprovalRentalDocumentHistory? GetLastDocumentByType(PPrincipleApprovalRentalDocumentType documentType)
        {
            var useDraftTemplate =
                entity.Status == PPrincipleApprovalRentalStatus.Edit ||
                entity.Status == PPrincipleApprovalRentalStatus.Rejected;

            return
                entity.DocumentHistories
                      .WhereIf(
                          useDraftTemplate,
                          dh =>
                              dh.StatusState == PPrincipleApprovalRentalStatus.Draft ||
                              dh.StatusState == PPrincipleApprovalRentalStatus.Edit ||
                              dh.StatusState == PPrincipleApprovalRentalStatus.Rejected)
                      .Where(w => w.DocumentType == documentType)
                      .OrderVersions()
                      .FirstOrDefault();
        }
    }

    protected async ValueTask ReplaceDocumentsAsync(
        PPrincipleApprovalRental entity,
        PPrincipleApproval principleEntity,
        CancellationToken ct,
        bool hasAcceptor)
    {
        var documentService = this.Resolve<IDocumentService>();

        var replaceDto =
            await this.MapToReplaceDto(entity, principleEntity, ct, null, hasAcceptor);

        var timeStamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");

        await ReplaceDocumentAsync(PPrincipleApprovalRentalDocumentType.Approval);
        await ReplaceDocumentAsync(PPrincipleApprovalRentalDocumentType.Winner);

        async ValueTask ReplaceDocumentAsync(PPrincipleApprovalRentalDocumentType documentType)
        {
            var lastedDocumentHistory =
                    (entity.Status == PPrincipleApprovalRentalStatus.Draft || entity.Status == PPrincipleApprovalRentalStatus.Edit || entity.Status == PPrincipleApprovalRentalStatus.Rejected)
                        ? entity.LastedDraftDocument(documentType)
                        : entity.Status == PPrincipleApprovalRentalStatus.WaitingCommitteeApproval || entity.Status == PPrincipleApprovalRentalStatus.WaitingComment
                            ? entity.LastedNotReplacedDocument(documentType)
                            : entity.Status == PPrincipleApprovalRentalStatus.WaitingAcceptance || entity.Status == PPrincipleApprovalRentalStatus.WaitingContractAssign
                                ? entity.LastedWaitingCommentNotReplacedDocument(documentType)
                : entity.LastedDocument(documentType);

            if (lastedDocumentHistory is null)
            {
                this.ThrowError($"ไม่พบข้อมูลเอกสาร {documentType.ToString()} ที่ส่งเห็นชอบ");
            }

            var finalFileId =
                await documentService.CopyDocumentTemplateAsync(
                    lastedDocumentHistory.FileId,
                    contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto),
                    parentDirectory: $"{DocumentTemplateGroups.PrincipleApprovalRental}/{entity.Id}_{documentType.ToString()}_{timeStamp}.odt",
                    cancellationToken: ct);

            if (finalFileId is null)
            {
                this.ThrowError("ไม่สามารถคัดลอกเอกสารที่ส่งเห็นชอบ");
            }

            entity.AddDocumentHistory(documentType, finalFileId.Value, true);
        }
    }

    protected async Task UpsertAttachments(PPrincipleApprovalRental parent, PPrincipleApprovalRentalEntrepreneurs entity, EntrepreneurResponseAttachment[] attachments)
    {
        var fileList = attachments
                       .SelectMany(r => r.FileAttachments.Select(f => new
                       {
                           f.Id,
                           r.DocumentTypeCode,
                           f.FileId,
                           f.FileName,
                           f.Sequence,
                           f.IsPublic,
                           f.Type,
                       }))
                       .ToArray();

        var incomingFileIds = fileList.Select(f => FileId.From(f.FileId)).ToHashSet();
        var existingFileIds = entity.Attachments.Select(a => a.FileId).ToHashSet();

        var removedAttachments = entity.Attachments
                                       .Where(a => !incomingFileIds.Contains(a.FileId))
                                       .ToArray();

        foreach (var attachment in removedAttachments)
        {
            entity.RemoveAttachment(attachment);
            await this.fileServiceClient.DeleteAsync(attachment.FileId, CancellationToken.None);
        }

        if (removedAttachments.Length > 0)
        {
            parent.AddActivity(new ActivityInfo(
                ActivityLogActionTypeConstant.DeleteFile,
                ActivityLogActionTypeConstant.DeleteFile,
                nameof(parent.Status),
                string.Join(", ", removedAttachments.Select(a => a.FileName))));
        }

        var newFiles = fileList.Where(f => !existingFileIds.Contains(FileId.From(f.FileId))).ToArray();

        newFiles.Map(f => PPrincipleApprovalRentalEntrepreneursAttachments.Create(
                    ParameterCode.From(f.DocumentTypeCode),
                    FileId.From(f.FileId),
                    f.FileName,
                    f.Type,
                    f.Sequence,
                    f.IsPublic))
                .Iter(r => entity.AddAttachment(r));

        if (newFiles.Length > 0)
        {
            parent.AddActivity(new ActivityInfo(
                ActivityLogActionTypeConstant.UploadFile,
                ActivityLogActionTypeConstant.UploadFile,
                nameof(parent.Status),
                string.Join(", ", newFiles.Select(f => f.FileName))));
        }

        foreach (var existing in entity.Attachments)
        {
            var match = fileList.FirstOrDefault(f => FileId.From(f.FileId) == existing.FileId);

            if (match != null)
            {
                existing.SetIsPublic(match.IsPublic)
                        .SetSequence(match.Sequence)
                        .SetDocumentType(ParameterCode.From(match.DocumentTypeCode));
            }
        }
    }

    protected async Task UpsertComparingAttachments(PPrincipleApprovalRental entity, IEnumerable<ComparingAttachmentsDto> attachments)
    {
        var requestFiles = attachments.Select(f => new
        {
            f.FileId,
            f.FileName,
            f.Sequence,
            f.IsPublic,
        })
                                      .ToArray();

        // Only process deletion if there are existing attachments
        if (entity.ComparingAttachments != null)
        {
            var existingAttachments = entity.ComparingAttachments.ToList();

            // Get files to remove (existing files not in the new request)
            var requestFileIds = requestFiles.Select(f => FileId.From(f.FileId)).ToHashSet();
            var filesToRemove = existingAttachments.Where(c => !requestFileIds.Contains(c.Id)).ToArray();

            // Delete removed files from file service
            foreach (var fileToRemove in filesToRemove)
            {
                await this.fileServiceClient.DeleteAsync(fileToRemove.Id, CancellationToken.None);
            }
        }

        // Create new attachments from request data (frontend sends all data)
        var newAttachments = requestFiles.Select(f =>
            PPrincipleApprovalRentalComparingAttachments.Create(
                FileId.From(f.FileId),
                f.FileName,
                f.Sequence,
                f.IsPublic)).ToList();

        // Replace all attachments with new ones
        entity.UpsertAttachment(newAttachments);
    }

    protected async Task<FileId?> UpdateDocumentHistoryAsync(
        PPrincipleApprovalRental entity,
        PPrincipleApprovalRentalDocumentType documentType,
        FileId fileId,
        bool? isReplaced = false,
        CancellationToken ct = default)
    {
        var latestHistory = entity.DocumentHistories
                                  .Where(d => d.DocumentType == documentType)
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
            parentDirectory: $"{DocumentTemplateGroups.PrincipleApprovalRental}/{entity.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}_{documentType}.odt",
            cancellationToken: ct);

        if (!copiedFileId.HasValue)
        {
            return null;
        }

        entity.AddDocumentHistory(documentType, copiedFileId.Value, isReplaced);

        var histories = entity.DocumentHistories.ToHashSet();
        var newHistory = histories.OrderVersions().First(h => h.FileId == copiedFileId.Value);
        newHistory.SetVersion(newVersion);

        return copiedFileId.Value;
    }

    protected async Task<FileId> GetDocumentTemplateForResetAsync(
        PPrincipleApprovalRental entity,
        PPrincipleApprovalRentalDocumentType documentType)
    {
        var fileId = documentType == PPrincipleApprovalRentalDocumentType.Approval
            ? await this.GetDocumentTemplateByCode(entity.RentTypeCode)
            : await this.GetWinnerDocumentTemplateByCode();

        return fileId;
    }
}