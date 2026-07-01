namespace GHB.DP2.Application.Features.Procurement.Appoint.Abstract;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.Procurement;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.PpAppoint;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using GreatFriends.ThaiBahtText;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record AppointAcceptorDto(
    AcceptorType AcceptorType,
    Guid UserId,
    string EmployeeCode,
    string FullName,
    string PositionName,
    string BusinessUnitName,
    int Sequence,
    Guid? DelegateeId,
    AcceptorStatus Status,
    DateTimeOffset? ActionAt,
    string? Remark);

public abstract partial class AppointEndpointBase<TRequest, TResponse> : EndpointBase<TRequest, TResponse>
    where TResponse : IResult
    where TRequest : notnull
{
    private readonly Dp2DbContext dbContext;
    private readonly IOperationService operationService;
    private readonly ICommandTextService commandTextService;

    protected AppointEndpointBase(
        Dp2DbContext dbContext,
        ILogger logger,
        IOperationService operationService,
        ICommandTextService commandTextService)
        : base(logger)
    {
        this.dbContext = dbContext;
        this.operationService = operationService;
        this.commandTextService = commandTextService;
    }

    protected async Task<(Domain.Procurement.Procurement Procurement, PpAppoint Appoint)> ValidateRequestAsync(PpAppointId appointId, CancellationToken ct)
    {
        var appoint = await this.dbContext.PpAppoints
                                .Include(x => x.TorDraftCommittees)
                                .ThenInclude(ppAppointTorDraftCommittee => ppAppointTorDraftCommittee.User)
                                .ThenInclude(suUser => suUser.Employee)
                                .ThenInclude(rawEmployee => rawEmployee.View)
                                .Include(x => x.TorDraftCommitteeDuties)
                                .Include(x => x.MedianPriceCommittees)
                                .ThenInclude(ppAppointMedianPriceCommittee => ppAppointMedianPriceCommittee.User)
                                .ThenInclude(suUser => suUser.Employee)
                                .ThenInclude(rawEmployee => rawEmployee.View)
                                .Include(x => x.MedianPriceCommitteeDuties)
                                .Include(x => x.Acceptors)
                                .ThenInclude(x => x.User)
                                .ThenInclude(x => x.Employee)
                                .Include(ppAppoint => ppAppoint.DocumentHistories)
                                .Include(auditableEntity => auditableEntity.AuditInfo)
                                .FirstOrDefaultAsync(x => x.Id == appointId, ct);

        if (appoint is null)
        {
            this.ThrowError("ไม่พบข้อมูลขอแต่งตั้ง", StatusCodes.Status404NotFound);
        }

        var procurement = await this.dbContext.Procurements
                                    .SingleOrDefaultAsync(f => f.Id == appoint.ProcurementId, ct);

        if (procurement is null)
        {
            this.ThrowError("ไม่พบข้อมูลจัดซื้อจัดจ้าง", StatusCodes.Status404NotFound);
        }

        return (procurement, appoint);
    }

    private async Task<FileId> GetDocumentTemplateByCriteria(
        ParameterCode supplyMethodCode,
        decimal? budget,
        bool isChange,
        bool isCancel,
        CancellationToken ct)
    {
        var documentService =
            this.Resolve<IDocumentService>();

        var fileId =
            await documentService.GetDocumentTemplateAsync(
                dt =>
                    dt.Group == DocumentTemplateGroups.Ap &&
                    dt.IsActive &&
                    dt.SupplyMethodCode == supplyMethodCode &&
                    (
                        (dt.IsChange == null && isChange == false) ||
                        dt.IsChange == isChange
                    ) &&
                    (
                        (dt.IsCancel == null && isCancel == false) ||
                        dt.IsCancel == isCancel
                    ) &&
                    dt.BudgetForDocument.Min <= budget &&
                    (dt.BudgetForDocument.Max == null || budget <= dt.BudgetForDocument.Max),
                ct);

        return (FileId)fileId;
    }

    protected async ValueTask SetDefaultDocumentTemplate(
        PpAppoint appointData,
        ParameterCode supplyMethodCode,
        decimal? budget,
        CancellationToken ct,
        bool isEdit = false,
        bool isCancel = false)
    {
        var appointDocId =
            await this.GetDocumentTemplateByCriteria(
                supplyMethodCode,
                budget,
                isEdit,
                isCancel,
                ct);

        appointData.AddDocumentHistory(appointDocId);
    }

    /// <summary>
    /// Creates a new document history version with a copy of the file.
    /// </summary>
    protected async Task<FileId?> UpdateDocumentHistoryAsync(
        PpAppoint appoint,
        FileId fileId,
        bool? isReplace = false,
        CancellationToken ct = default)
    {
        var latestHistory = appoint.DocumentHistories
                                    .OrderVersions()
                                    .FirstOrDefault();

        if (latestHistory == null)
        {
            return null;
        }

        var newVersion = RunningDocumentVersion.IncrementDocumentVersion(
            latestHistory.Version,
            latestHistory.StatusState.ToString(),
            appoint.Status.ToString());

        var documentService = this.Resolve<IDocumentService>();
        var copiedFileId = await documentService.CopyDocumentTemplateAsync(
            fileId,
            parentDirectory: $"{DocumentTemplateGroups.Ap}/{appoint.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
            cancellationToken: ct);

        if (!copiedFileId.HasValue)
        {
            return null;
        }

        appoint.AddDocumentHistory(copiedFileId.Value, isReplace ?? false);

        var newHistory = appoint.DocumentHistories
            .OrderVersions()
            .First(h => h.FileId == copiedFileId.Value);
        newHistory.SetVersion(newVersion);

        return copiedFileId.Value;
    }

    protected async Task<FileId> GetDocumentTemplateAsync(
        PpAppoint appoint,
        CancellationToken ct)
    {
        var documentService = this.Resolve<IDocumentService>();

        var fileId = await documentService.GetDocumentTemplateAsync(
            dt =>
                dt.Group == DocumentTemplateGroups.Ap &&
                dt.IsActive &&
                dt.SupplyMethodCode == appoint.Procurement.SupplyMethodCode &&
                (
                    (dt.IsChange == null && appoint.IsChange == false) ||
                    dt.IsChange == appoint.IsChange
                ) &&
                (
                    (dt.IsCancel == null && appoint.IsCancel == false) ||
                    dt.IsCancel == appoint.IsCancel
                ) &&
                dt.BudgetForDocument.Min <= appoint.Procurement.Budget &&
                (dt.BudgetForDocument.Max == null || appoint.Procurement.Budget <= dt.BudgetForDocument.Max),
            parentDirectory: $"{DocumentTemplateGroups.Ap}/{appoint.Id}_Reset_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
            cancellationToken: ct);

        return (FileId)fileId;
    }

    private async Task<List<SuParameter>> GetCommitteePositionParameters(CancellationToken ct)
    {
        return await this.dbContext.SuParameters
                         .Where(p =>
                             (p.GroupCode == GroupCode.From(ParameterGroupConstant.PositionOnBoard))
                             && p.ParentId == null)
                         .ToListAsync(ct);
    }

    private async Task<SuUser> GetUserOrThrow(Guid userId, CancellationToken ct)
    {
        var user = await this.dbContext.SuUsers
                             .Include(u => u.Employee)
                             .ThenInclude(e => e.View)
                             .FirstOrDefaultAsync(u => u.Id == UserId.From(userId), ct);

        if (user is null)
        {
            this.ThrowError($"User with ID {userId} not found.", StatusCodes.Status404NotFound);
        }

        return user;
    }

    private SuParameter GetPositionOrThrow(List<SuParameter> parameters, string committeePositionsCode)
    {
        var position = parameters.FirstOrDefault(p => p.Code == ParameterCode.From(committeePositionsCode));

        if (position is null)
        {
            this.ThrowError($"Position with code {committeePositionsCode} not found.", StatusCodes.Status404NotFound);
        }

        return position;
    }

    protected async Task<PpAppointTorDraftCommittee> CreateTorDraftCommittee(
        PpAppointId appointId,
        Guid userId,
        string committeePositionsCode,
        int sequence,
        CancellationToken ct)
    {
        var user = await this.GetUserOrThrow(userId, ct);
        var parameterList = await this.GetCommitteePositionParameters(ct);
        var position = this.GetPositionOrThrow(parameterList, committeePositionsCode);

        return PpAppointTorDraftCommittee.Create(
            appointId,
            user.Id,
            user.Employee.View?.FullName ?? string.Empty,
            user.Employee.View?.FullPositionName ?? string.Empty,
            position.Code,
            position.Label,
            sequence);
    }

    protected async Task<PpAppointMedianPriceCommittee> CreateMedianPriceCommittee(
        PpAppointId appointId,
        Guid userId,
        string committeePositionsCode,
        int sequence,
        CancellationToken ct)
    {
        var user = await this.GetUserOrThrow(userId, ct);
        var parameterList = await this.GetCommitteePositionParameters(ct);
        var position = this.GetPositionOrThrow(parameterList, committeePositionsCode);

        return PpAppointMedianPriceCommittee.Create(
            appointId,
            user.Id,
            user.Employee.View?.FullName ?? string.Empty,
            user.Employee.View?.FullPositionName ?? string.Empty,
            position.Code,
            position.Label,
            sequence);
    }

    protected static PpAppointTorDraftCommitteeDuties CreateTorDraftCommitteeDuties(
        PpAppointId appointId,
        string description,
        int sequence)
    {
        return PpAppointTorDraftCommitteeDuties.Create(
            appointId,
            description,
            sequence);
    }

    protected static PpAppointMedianPriceCommitteeDuties CreateMedianPriceCommitteeDuties(
        PpAppointId appointId,
        string description,
        int sequence)
    {
        return PpAppointMedianPriceCommitteeDuties.Create(
            appointId,
            description,
            sequence);
    }

    protected static PpAppointAcceptors CreateAppointAcceptor(
        PpAppoint appoint,
        AcceptorAppointInfoData info)
    {
        return PpAppointAcceptors.Create(appoint.Id, info, appoint.Status);
    }

    protected static bool IsPreviousApproved(List<PpAppointAcceptors> acceptors, PpAppointAcceptors current)
    {
        if (current.Sequence <= 1)
        {
            return true;
        }

        var prev = acceptors.LastOrDefault(a => a.Sequence < current.Sequence && a.IsActive);

        return prev == null || prev.Status == AcceptorStatus.Approved;
    }

    protected GetAppointResponseDto MapToResponseDto(PpAppoint appoint, Guid userId)
    {
        var lastedHistory =
            appoint.DocumentHistories
                   .OrderVersions()
                   .FirstOrDefault();

        var documentVersions =
            appoint.DocumentHistories
                   .OrderVersions()
                   .Select((d, index) => new AppointDocumentVersionResponse(
                       d.FileId.Value,
                       d.Version,
                       d.CreatedAt,
                       d.CreatedByName ?? string.Empty,
                       index == 0))
                   .ToArray();

        return new GetAppointResponseDto(
            new ProcurementDto(
                appoint.Procurement.PlanId.HasValue ? (Guid)appoint.Procurement.PlanId : null,
                appoint.Procurement.ProcurementNumber,
                appoint.Procurement.Type,
                appoint.Procurement.Step,
                appoint.Procurement.Department.Name,
                appoint.Procurement.DepartmentId,
                appoint.Procurement.Plan?.PlanNumber.ToString() ?? string.Empty,
                appoint.Procurement.Name,
                appoint.Procurement.Budget,
                appoint.Procurement.Budget.ThaiBahtText(),
                appoint.Procurement.BudgetYear,
                appoint.Procurement.SupplyMethod.Label,
                appoint.Procurement.SupplyMethodCode,
                appoint.Procurement.SupplyMethodType?.Label ?? string.Empty,
                appoint.Procurement.SupplyMethodTypeCode,
                appoint.Procurement.SupplyMethodSpecialType?.Label ?? string.Empty,
                appoint.Procurement.SupplyMethodSpecialTypeCode,
                appoint.Procurement.Status,
                appoint.Procurement.ExpectingProcurementAt,
                appoint.Procurement.IsStock,
                appoint.Procurement.IsCommercialMaterial,
                appoint.Procurement.Plan?.Type,
                appoint.Procurement.ProcessType),
            new AppointResponseDto(
                appoint.Id.Value,
                appoint.ProcurementId.Value,
                appoint.AppointNumber.Value,
                appoint.MemorandumDate,
                appoint.MemorandumNumber,
                appoint.Telephone,
                appoint.Reason,
                appoint.Status,
                appoint.ChangeReason,
                appoint.CancelReason,
                appoint.IsChange,
                appoint.IsCancel),
            appoint.TorDraftCommittees
                   .OrderBy(o => o.Sequence)
                   .Select(x => new AppointTorDraftCommitteeResponseDto(
                       x.Id.Value,
                       x.SuUserId.Value,
                       x.FullName,
                       x.FullPositionName,
                       x.User.Employee.PrimaryDepartment?.Id,
                       x.CommitteePositionsCode.Value,
                       x.Sequence)),
            appoint.TorDraftCommitteeDuties
                   .OrderBy(o => o.Sequence)
                   .Select(x => new DutiesResponseDto(
                       x.Id.Value,
                       x.Description,
                       x.Sequence)),
            appoint.MedianPriceCommittees
                   .OrderBy(o => o.Sequence)
                   .Select(x => new AppointMedianPriceCommitteeResponseDto(
                       x.Id.Value,
                       x.SuUserId.Value,
                       x.FullName,
                       x.FullPositionName,
                       x.User.Employee.PrimaryDepartment?.Id,
                       x.CommitteePositionsCode.Value,
                       x.Sequence)),
            appoint.MedianPriceCommitteeDuties
                   .OrderBy(o => o.Sequence)
                   .Select(x => new DutiesResponseDto(
                       x.Id.Value,
                       x.Description,
                       x.Sequence)),
            appoint.Acceptors
                   .Select(DelegatorExtensions.DelegatorToAcceptor)
                   .OrderBy(x => x.Sequence)
                   .Select(x => new AppointAcceptorResponseDto(
                       x.Id.Value,
                       x.Type,
                       x.UserId.Value,
                       x.EmployeeCode.Value,
                       x.FullName,
                       x.PositionName,
                       x.BusinessUnitName,
                       x.Sequence,
                       x.DelegateeId?.Value,
                       x.Status,
                       x.ActionAt,
                       x.Remark,
                       x.IsActive,
                       CurrentAcceptor(appoint.Acceptors, x.Id.Value, appoint.Status),
                       x.Delegatee?.SuUserId.Value)),
            lastedHistory?.FileId.Value,
            false,
            documentVersions,
            appoint.TorDraftCommittees.All(x => x.IsCommittee()),
            appoint.MedianPriceCommittees.All(x => x.IsCommittee()),
            appoint.AuditInfo.CreatedBy == userId);
    }

    private static bool CurrentAcceptor(IEnumerable<PpAppointAcceptors> acceptors, Guid acceptorId, AppointStatus status)
    {
        if (status != AppointStatus.WaitingApproval)
        {
            return false;
        }

        var requiredType = AcceptorType.Approver;

        var current = acceptors.FirstOrDefault(a =>
            a.Id.Value == acceptorId && a.Type == requiredType);

        if (current == null)
        {
            return false;
        }

        var prev = acceptors
                   .Where(a =>
                       a.Type == requiredType &&
                       a.Sequence < current.Sequence &&
                       a.IsActive)
                   .OrderByDescending(a => a.Sequence)
                   .FirstOrDefault();

        if (prev == null)
        {
            return current.Status != AcceptorStatus.Approved;
        }

        return prev.Status == AcceptorStatus.Approved;
    }

    private async Task<SuUser?> GetLastActivityCreatedByAsync(
        string key,
        string type,
        CancellationToken ct)
    {
        var lastActivity =
            await this.dbContext.SuActivityLogs
                      .AsNoTracking()
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
}