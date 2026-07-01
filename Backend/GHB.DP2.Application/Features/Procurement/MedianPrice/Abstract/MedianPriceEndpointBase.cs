namespace GHB.DP2.Application.Features.Procurement.MedianPrice.Abstract;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.MedianPrice;
using GHB.DP2.Application.Features.Procurement.MedianPrice.Dto;
using GHB.DP2.Application.Features.Procurement.Procurement;
using GHB.DP2.Application.Features.Procurement.Procurement.Abstact;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PpMedianPrice;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using GreatFriends.ThaiBahtText;
using LanguageExt;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public abstract class MedianPriceEndpointBase<TRequest, TResponse> : EndpointBase<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : IResult
{
    private readonly Dp2DbContext dbContext;
    private readonly IOperationService operationService;
    private readonly ICommandTextService commandTextService;

    protected MedianPriceEndpointBase(
        ILogger logger,
        Dp2DbContext dbContext,
        IOperationService operationService,
        ICommandTextService commandTextService)
        : base(logger)
    {
        this.dbContext = dbContext;
        this.operationService = operationService;
        this.commandTextService = commandTextService;
    }

    protected async Task<Procurement> GetProcurementById(ProcurementId procurementId, CancellationToken ct)
    {
        var procurement = await this.dbContext.Procurements
                                    .Include(p => p.Appoints)
                                    .Include(p => p.MedianPrices)
                                    .Include(p => p.Department)
                                    .Include(p => p.SupplyMethod)
                                    .Include(p => p.SupplyMethodType)
                                    .Include(p => p.SupplyMethodSpecialType)
                                    .Include(p => p.Plan)
                                    .SingleOrDefaultAsync(
                                        mp =>
                                            mp.Id == procurementId,
                                        ct);

        if (procurement is null)
        {
            this.ThrowError($"Procurement is not found", StatusCodes.Status404NotFound);
        }

        return procurement;
    }

    protected async Task<PpMedianPrice> GetMedianPriceById(MedianPriceId id, ProcurementId procurementId, CancellationToken ct)
    {
        var medianPrice = await this.dbContext.PpMedianPrices
                                    .Include(mp => mp.DocumentTemplate)
                                    .Include(mp => mp.DocumentHistories)
                                    .Include(mp => mp.Acceptors)
                                    .ThenInclude(a => a.User)
                                    .ThenInclude(u => u.Employee)
                                    .Include(mp => mp.Staff)
                                    .ThenInclude(s => s.Details)
                                    .Include(mp => mp.BudgetAllocations)
                                    .ThenInclude(ba => ba.Details)
                                    .Include(mp => mp.Procurement)
                                    .ThenInclude(procurement => procurement.Department)
                                    .Include(mp => mp.Procurement)
                                    .ThenInclude(procurement => procurement.SupplyMethod)
                                    .Include(mp => mp.Procurement)
                                    .ThenInclude(procurement => procurement.SupplyMethodType)
                                    .Include(mp => mp.Procurement)
                                    .ThenInclude(procurement => procurement.SupplyMethodSpecialType)
                                    .Include(mp => mp.Procurement)
                                    .ThenInclude(procurement => procurement.Plan)
                                    .Include(mp => mp.Procurement)
                                    .ThenInclude(procurement => procurement.TorDrafts)
                                    .ThenInclude(x => x.DocumentTemplate)
                                    .AsSplitQuery()
                                    .SingleOrDefaultAsync(
                                        mp =>
                                            mp.Id == id &&
                                            mp.ProcurementId == procurementId,
                                        ct);

        if (medianPrice is null)
        {
            this.ThrowError($"Median price with ID {id} not found for procurement {procurementId}.", StatusCodes.Status404NotFound);
        }

        return medianPrice;
    }

    private async Task<FileId> GetDocumentTemplateByCode(
        string requestTemplateCode,
        CancellationToken ct)
    {
        var documentService =
            this.Resolve<IDocumentService>();

        var fileId =
            await documentService.GetDocumentTemplateAsync(
                dt =>
                    dt.Group == DocumentTemplateGroups.Mdp &&
                    dt.IsActive &&
                    dt.Code == requestTemplateCode,
                ct);

        if (fileId == null)
        {
            this.ThrowError(
                $"ไม่พบเอกสารแม่แบบที่ตรงตามเงื่อนไข: {requestTemplateCode}",
                StatusCodes.Status404NotFound);
        }

        return fileId.Value;
    }

    protected async ValueTask SetDefaultDocumentTemplate(
        PpMedianPrice medianPriceData,
        string requestDocumentTemplateCode,
        CancellationToken ct)
    {
        var defaultDocumentTemplateId =
            await this.GetDocumentTemplateByCode(
                requestDocumentTemplateCode,
                ct);

        medianPriceData.AddDocumentHistory(defaultDocumentTemplateId);
    }

    protected async Task<FileId?> UpdateDocumentHistoryAsync(
        PpMedianPrice medianPrice,
        FileId fileId,
        bool? isReplace = false,
        CancellationToken ct = default)
    {
        var latestHistory = medianPrice.DocumentHistories
                                       .OrderVersions()
                                       .FirstOrDefault();

        if (latestHistory == null)
        {
            return null;
        }

        var newVersion = RunningDocumentVersion.IncrementDocumentVersion(
            latestHistory.Version,
            latestHistory.StatusState.ToString(),
            medianPrice.Status.ToString());

        var documentService = this.Resolve<IDocumentService>();

        var copiedFileId = await documentService.CopyDocumentTemplateAsync(
            fileId,
            parentDirectory: $"{DocumentTemplateGroups.Mdp}/{medianPrice.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
            cancellationToken: ct);

        if (!copiedFileId.HasValue)
        {
            return null;
        }

        medianPrice.AddDocumentHistory(copiedFileId.Value, isReplace ?? false);

        var newHistory = medianPrice.DocumentHistories
            .OrderVersions()
            .First(h => h.FileId == copiedFileId.Value);
        newHistory.SetVersion(newVersion);

        return copiedFileId.Value;
    }

    protected async Task<FileId> GetDocumentTemplateAsync(
        PpMedianPrice medianPrice,
        CancellationToken ct)
    {
        var documentService = this.Resolve<IDocumentService>();

        var fileId = await documentService.GetDocumentTemplateAsync(
            dt =>
                dt.Group == DocumentTemplateGroups.Mdp &&
                dt.IsActive &&
                dt.Code == medianPrice.DocumentTemplate.Code,
            parentDirectory: $"{DocumentTemplateGroups.Mdp}/{medianPrice.Id}_Reset_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
            cancellationToken: ct);

        return (FileId)fileId;
    }

    protected async Task UpsertAcceptors(PpMedianPrice medianPrice, MedianPriceAcceptorInfo[] acceptors, BusinessUnitId workBusinessUnitId, UserId? sendToAcceptorId = null)
    {
        // Get a User list from the database
        var userIds =
            acceptors
                .Map(a => a.UserId)
                .Map(UserId.From)
                .ToArray();

        var users =
            await this.dbContext.SuUsers
                      .Where(u => userIds.Contains(u.Id))
                      .ToArrayAsync(CancellationToken.None);

        var userExists
            = userIds.Except(users.Map(u => u.Id)).ToArray();

        if (userExists.Length > 0)
        {
            this.ThrowError(
                $"User with ID {string.Join(", ", userExists)} not found.",
                StatusCodes.Status404NotFound);
        }

        var committeePositionsCodes =
            acceptors
                .Where(a => !string.IsNullOrWhiteSpace(a.CommitteePositionsCode))
                .Select(a => a.CommitteePositionsCode)
                .Map(ParameterCode.From!);

        var committeePositions =
            await this.dbContext.SuParameters
                      .Where(p => committeePositionsCodes.Contains(p.Code))
                      .ToArrayAsync(CancellationToken.None);

        var committeePositionsExists =
            committeePositionsCodes.Except(committeePositions.Map(p => p.Code))
                                   .ToArray();

        if (committeePositionsExists.Length > 0)
        {
            this.ThrowError(
                $"Committee position with code {string.Join(", ", committeePositionsExists)} not found.",
                StatusCodes.Status404NotFound);
        }

        // Remove assignees that are not in the request
        _ = medianPrice.Acceptors.Where(w => !acceptors.Select(s => s.Id).Contains(w.Id.Value))
                       .Iter(s => medianPrice.RemoveAcceptor(s));

        // Assuming AcceptorRequest is a DTO that maps to PpMedianPriceAcceptor
        var requestAcceptor =
            acceptors.Join(
                         users,
                         a => a.UserId,
                         u => u.Id.Value,
                         (a, u) =>
                         {
                             var acceptor = a.Id.IsNull()
                                 ? PpMedianPriceAcceptor.Create(
                                     a.AcceptorType,
                                     u,
                                     a.Sequence,
                                     workBusinessUnitId)
                                 : PpMedianPriceAcceptor.Create(
                                     AcceptorId.From(a.Id.Value),
                                     a.AcceptorType,
                                     u,
                                     a.Sequence,
                                     workBusinessUnitId);

                             _ = string.IsNullOrWhiteSpace(a.CommitteePositionsCode)
                                 ? acceptor.SetCommitteePositionsCode(null)
                                 : acceptor.SetCommitteePositionsCode(
                                     ParameterCode.From(a.CommitteePositionsCode));

                             acceptor.SetIsUnableToPerformDuties(a.IsUnableToPerformDuties ?? false);

                             if (a is { IsUnableToPerformDuties: true, Remark: not null })
                             {
                                 acceptor.UnableToPerformDuties(a.Remark);
                             }

                             acceptor.SetSendToAcceptorId(sendToAcceptorId);

                             return acceptor;
                         })
                     .ToHashSet();

        // Update existing acceptors
        _ = medianPrice.Acceptors
                       .Join(
                           requestAcceptor,
                           domainAcceptor => domainAcceptor.Id,
                           request => request.Id,
                           (domainAcceptor, request) =>
                           {
                               domainAcceptor.SetType(request.Type)
                                             .SetUser(
                                                 request.UserId,
                                                 request.EmployeeCode,
                                                 request.FullName,
                                                 request.PositionName,
                                                 request.BusinessUnitName)
                                             .SetSequence(request.Sequence);

                               domainAcceptor.SetCommitteePositionsCode(request.CommitteePositionsCode)
                                             .SetIsUnableToPerformDuties(request.IsUnableToPerformDuties);

                               if (request.IsUnableToPerformDuties)
                               {
                                   domainAcceptor.UnableToPerformDuties(request.Remark);
                               }

                               domainAcceptor.SetSendToAcceptorId(sendToAcceptorId);

                               return domainAcceptor;
                           })
                       .ToHashSet();

        // Add new acceptors
        _ = requestAcceptor
            .Except(medianPrice.Acceptors)
            .Map(medianPrice.AddAcceptor)
            .ToHashSet();
    }

    protected async Task UpsertAssignee(
        PpMedianPrice medianPrice,
        MedianPriceAssigneeInfo[] assignee,
        UserId? sendToAcceptorId = null,
        CancellationToken cancellationToken = default)
    {
        // Get the user from the database
        var userIds = assignee.Map(a => a.UserId)
                              .Map(UserId.From)
                              .ToArray();

        var users = await this.dbContext.SuUsers
                              .Include(suUser => suUser.Employee)
                              .ThenInclude(rawEmployee => rawEmployee.View)
                              .Where(u => userIds.Contains(u.Id))
                              .ToArrayAsync(cancellationToken);

        var userExists
            = userIds.Except(users.Map(u => u.Id)).ToArray();

        if (userExists.Length > 0)
        {
            this.ThrowError(
                $"User with ID {string.Join(", ", userExists)} not found.",
                StatusCodes.Status404NotFound);
        }

        _ = medianPrice.Assignees.Where(w => !assignee.Select(s => s.Id).Contains(w.Id.Value))
                       .Iter(s => medianPrice.RemoveAssignee(s));

        var requestAssignee =
            assignee.Join(
                        users,
                        a => a.UserId,
                        u => u.Id.Value,
                        (a, u) =>
                        {
                            var assigneeEntity = a.Id.IsNull()
                                ? PpMedianPriceAssignee.Create(
                                    a.AssigneeGroup,
                                    a.AssigneeType,
                                    u,
                                    a.Sequence)
                                : PpMedianPriceAssignee.Create(
                                    MedianPriceAssigneeId.From(a.Id.Value),
                                    a.AssigneeGroup,
                                    a.AssigneeType,
                                    u,
                                    a.Sequence);

                            return assigneeEntity;
                        })
                    .ToHashSet();

        var lastAssigneeUserId = assignee
            .Where(a => a.AssigneeType == AssigneeType.Assignee)
            .OrderByDescending(a => a.Sequence)
            .Select(a => (UserId?)UserId.From(a.UserId))
            .FirstOrDefault();

        var resolvedSendToAcceptorId = lastAssigneeUserId ?? sendToAcceptorId;

        // Update existing assignees
        _ = medianPrice.Assignees
                       .Join(
                           requestAssignee,
                           domainAssignee => domainAssignee.Id,
                           request => request.Id,
                           (domainAssignee, request) =>
                           {
                               domainAssignee.SetType(request.Type)
                                             .SetUser(
                                                 request.UserId,
                                                 request.EmployeeCode,
                                                 request.FullName,
                                                 request.PositionName,
                                                 request.BusinessUnitName)
                                             .SetSequence(request.Sequence)
                                             .SetSendToAcceptorId(resolvedSendToAcceptorId);

                               return domainAssignee;
                           })
                       .ToHashSet();

        // Add new assignees
        _ = requestAssignee
            .Except(medianPrice.Assignees)
            .Map(a =>
            {
                a.SetSendToAcceptorId(resolvedSendToAcceptorId);
                return medianPrice.AddAssignee(a);
            })
            .ToHashSet();
    }

    protected static void UpdateStatus(PpMedianPrice medianPrice, MedianPriceStatus status)
    {
        // If the status is already set to the requested status, do nothing
        if (medianPrice.Status == status)
        {
            return;
        }

        if (medianPrice.HasMajorityRejection())
        {
            medianPrice.SetRejected(null);

            return;
        }

        switch (status)
        {
            case MedianPriceStatus.WaitingCommitteeApproval:
                medianPrice.SetWaitingCommitteeApproval();

                break;

            case MedianPriceStatus.Edit:
                medianPrice.SetEdit();

                break;

            case MedianPriceStatus.WaitingAssign:
                medianPrice.SetWaitingAssign();

                break;

            case MedianPriceStatus.WaitingApproval:
                medianPrice.SetWaitingAcceptor();

                break;

            case MedianPriceStatus.WaitingComment:
                medianPrice.SetWaitingComment();
                _ = SendNotificationAssigneeAsync(medianPrice, CancellationToken.None);

                break;
        }
    }

    protected void ValidateMedianPriceStatus(PpMedianPrice medianPrice)
    {
        switch (medianPrice.Status)
        {
            case MedianPriceStatus.Draft:
                this.ThrowError(
                    "ไม่สามารถอนุมัติราคากลางได้ในสถานะปัจจุบัน",
                    StatusCodes.Status400BadRequest);

                break;

            case MedianPriceStatus.Approved or MedianPriceStatus.Cancelled:
                this.ThrowError(
                    "ราคากลางนี้ถูกอนุมัติหรือยกเลิกแล้ว",
                    StatusCodes.Status400BadRequest);

                break;

            case MedianPriceStatus.Rejected:
                medianPrice.SetWaitingCommitteeApproval();

                break;
        }
    }

    protected GetMedianPriceByIdResponse MapToResponse(PpMedianPrice medianPrice)
    {
        var lastedDocumentHistory =
            medianPrice.DocumentHistories
                       .OrderVersions()
                       .FirstOrDefault();

        var documentVersions =
            medianPrice.DocumentHistories
                       .OrderVersions()
                       .Select((d, index) => new MedianPriceDocumentVersionResponse(
                           d.FileId.Value,
                           d.Version,
                           d.CreatedAt,
                           d.CreatedByName ?? string.Empty,
                           index == 0))
                       .ToArray();

        var acceptorsApprover =
            medianPrice.Acceptors
                       .Where(a => a.Type != AcceptorType.MedianPriceCommittee)
                       .Select(DelegatorExtensions.DelegatorToAcceptor)
                       .ToList();

        var acceptors =
            medianPrice.Acceptors
                       .Where(a => a.Type == AcceptorType.MedianPriceCommittee)
                       .ToList();

        var medianPriceAcceptors =
            acceptorsApprover
                .Union(acceptors)
                .Map(MapAcceptor)
                .OrderBy(o => o.AcceptorType)
                .ThenBy(o => o.Sequence)
                .ToArray();

        return new GetMedianPriceByIdResponse(
            medianPrice.Id,
            medianPrice.ProcurementId,
            new ProcurementDto(
                medianPrice.Procurement.PlanId.HasValue ? (Guid)medianPrice.Procurement.PlanId : null,
                medianPrice.Procurement.ProcurementNumber,
                medianPrice.Procurement.Type,
                medianPrice.Procurement.Step,
                medianPrice.Procurement.Department.Name,
                medianPrice.Procurement.DepartmentId,
                medianPrice.Procurement.Plan?.PlanNumber.ToString() ?? string.Empty,
                medianPrice.Procurement.Name,
                medianPrice.Procurement.Budget,
                medianPrice.Procurement.Budget.ThaiBahtText(),
                medianPrice.Procurement.BudgetYear,
                medianPrice.Procurement.SupplyMethod.Label,
                medianPrice.Procurement.SupplyMethodCode,
                medianPrice.Procurement.SupplyMethodType?.Label ?? string.Empty,
                medianPrice.Procurement.SupplyMethodTypeCode,
                medianPrice.Procurement.SupplyMethodSpecialType?.Label ?? string.Empty,
                medianPrice.Procurement.SupplyMethodSpecialTypeCode,
                medianPrice.Procurement.Status,
                medianPrice.Procurement.ExpectingProcurementAt,
                medianPrice.Procurement.IsStock,
                medianPrice.Procurement.IsCommercialMaterial,
                medianPrice.Procurement.Plan?.Type,
                medianPrice.Procurement.ProcessType),
            (string)medianPrice.ReferenceNumber,
            medianPrice.DocumentDate,
            medianPrice.Object,
            medianPrice.Reason,
            medianPrice.SpecialDescription,
            medianPrice.JobDescription,
            medianPrice.PriceReasonablenessInfo,
            medianPrice.DocumentTemplate.Code,
            medianPrice.Status,
            lastedDocumentHistory?.FileId.Value,
            false,
            medianPrice.BudgetAllocations
                       .Map(MapBudgetAllocation)
                       .First(),
            medianPrice.Staff
                       .Map(MapStaff)
                       .FirstOrDefault(),
            MedianPriceExpenseDescriptionInfo.FromEntity(medianPrice.ExpenseDescription),
            medianPriceAcceptors,
            [
                .. medianPrice.Assignees
                              .Map(DelegatorExtensions.DelegatorToAssignee)
                              .Map(MapAssignee)
                              .OrderBy(o => o.AssigneeType)
                              .ThenBy(o => o.Sequence)
            ],
            medianPrice.CancelReason,
            medianPrice.ChangeReason,
            medianPrice.IsChange,
            medianPrice.IsCancel,
            medianPrice.Telephone,
            medianPrice.IsActive,
            medianPrice.Procurement.TorDrafts.FirstOrDefault()?.DocumentTemplate?.Code ?? string.Empty,
            documentVersions);
    }

    private static BudgetAllocationInfo MapBudgetAllocation(PpMedianPriceBudgetAllocations budgetAllocation)
    {
        return new BudgetAllocationInfo(
            budgetAllocation.Id,
            budgetAllocation.ReferenceDate,
            budgetAllocation.Budget,
            budgetAllocation.ReferenceMedianPrice,
            [
                .. budgetAllocation.Details
                                   .Map(MapDetail)
                                   .OrderBy(o => o.Sequence)
            ]);

        static BudgetAllocationDetailInfo MapDetail(PpMedianPriceBudgetAllocationsDetail detail)
        {
            return detail switch
            {
                PpMedianPriceBudgetAllocationsWithDetail d => new BudgetAllocationsWithDetail(
                    d.Id,
                    d.Sequence,
                    d.Source,
                    d.ReferenceBudge),
                PpMedianPriceBudgetAllocationsWithoutDetail d => new BudgetAllocationsWithoutDetail(
                    d.Id,
                    d.Sequence,
                    d.Source),
                _ => throw new NotSupportedException($"Unsupported budget allocation detail type: {detail.GetType()}"),
            };
        }
    }

    private static MedianPriceStaffInfo MapStaff(PpMedianPriceStaff staff)
    {
        return new MedianPriceStaffInfo(
            staff.Id,
            staff.PersonnelCompensation,
            staff.PersonnelCount,
            [
                .. staff.Details
                        .Map(MapStaffDetail)
                        .OrderBy(o => o.Sequence)
            ]);

        static MedianPriceStaffDetailInfo MapStaffDetail(PpMedianPriceStaffDetail detail)
        {
            return detail switch
            {
                PpMedianPriceStaffPersonal d => new StaffPersonnelDetail(
                    d.Id,
                    d.Sequence,
                    d.Description,
                    d.PersonalCount),
                PpMedianPriceStaffConsultantTypes d => new StaffConsultantTypeDetail(
                    d.Id,
                    d.Sequence,
                    d.Description),
                PpMedianPriceStaffConsultantQualifications d => new StaffConsultantQualificationDetail(
                    d.Id,
                    d.Sequence,
                    d.Description),
                _ => throw new NotSupportedException($"Unsupported median price staff detail type: {detail.GetType()}"),
            };
        }
    }

    private static MedianPriceAcceptorResponseInfo MapAcceptor(PpMedianPriceAcceptor acceptor)
    {
        return new MedianPriceAcceptorResponseInfo(
            acceptor.Id.Value,
            acceptor.Type,
            acceptor.UserId.Value,
            acceptor.Sequence,
            acceptor.FullName,
            acceptor.PositionName,
            acceptor.BusinessUnitName,
            acceptor.Status,
            acceptor.Remark,
            acceptor.ActionAt,
            Optional(acceptor.CommitteePositionsCode)
                .Map(v => v.Value)
                .IfNoneUnsafe((string?)null),
            acceptor.CommitteePosition?.Label,
            acceptor.IsUnableToPerformDuties,
            acceptor.IsCurrentApprover(),
            acceptor.User.Employee.PrimaryDepartment != null ? (string)acceptor.User.Employee.PrimaryDepartment.Id : string.Empty,
            acceptor.Delegatee?.SuUserId.Value);
    }

    private static AssigneeResponse MapAssignee(PpMedianPriceAssignee assignee)
    {
        return new AssigneeResponse(
            assignee.Id.Value,
            assignee.Group,
            assignee.Type,
            assignee.UserId.Value,
            assignee.Sequence,
            assignee.FullName,
            assignee.PositionName,
            assignee.BusinessUnitName,
            assignee.Status,
            assignee.Remark,
            assignee.ActionAt,
            assignee.Delegatee?.SuUserId.Value);
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

    protected async Task<MedianPriceReplaceDto> MapToReplaceDtoAsync(
        Procurement procurement,
        PpMedianPrice medianPrice,
        CancellationToken ct,
        UserId? creatorUserId,
        bool isPreview = false)
    {
        var appoint =
            procurement.Appoints
                       .FirstOrDefault(a => a.IsActive);

        if (appoint == null)
        {
            this.ThrowError(
                "ไม่พบการแต่งตั้งคณะกรรมการ",
                StatusCodes.Status404NotFound);
        }

        var committeePositionCode =
            appoint.MedianPriceCommittees
                   .Select(m => m.CommitteePositionsCode)
                   .FirstOrDefault();

        var medianPriceCommitteeName =
            committeePositionCode == SuParameterCodeConstant.PosBoard006
                ? "ผู้จัดทำราคากลาง (ราคาอ้างอิง)"
                : "คณะกรรมการกำหนดราคากลาง (ราคาอ้างอิง)";

        var acceptorsReplace = new MedianPriceAcceptorReplaceInfo[] { };
        var committeesReplace = new MedianPriceAcceptorReplaceInfo[] { };
        CreatorReplaceDto? creatorReplace = null;

        if (isPreview)
        {
            acceptorsReplace =
            [
                .. medianPrice.Acceptors
                              .Where(a =>
                                  a is
                                  {
                                      Type: AcceptorType.Approver
                                  })
                              .Select(DelegatorExtensions.DelegatorToAcceptor)
                              .Map(MapAcceptorReplace)
                              .OrderBy(a => a.Sequence)
            ];

            if (acceptorsReplace.Any())
            {
                acceptorsReplace[^1] =
                    acceptorsReplace.Last() with { Action = "อนุมัติ" };
            }

            acceptorsReplace =
                [.. acceptorsReplace.Where(a => a.Status == AcceptorStatus.Approved)];
        }

        committeesReplace = (medianPrice.Status is MedianPriceStatus.Draft or MedianPriceStatus.Rejected or MedianPriceStatus.Edit)
            ? []
            : [.. medianPrice.Acceptors
                              .Where(a =>
                                  a is
                                  {
                                      Type: AcceptorType.MedianPriceCommittee
                                  })
                              .Map(MapAcceptorReplace)
                              .OrderBy(a => a.Sequence)];

        creatorReplace = await this.GetCreatorReplaceAsync(medianPrice, creatorUserId, ct);

        var lastAssignee = (medianPrice.Status is MedianPriceStatus.WaitingComment
            or MedianPriceStatus.WaitingApproval
            or MedianPriceStatus.Approved)
            ? medianPrice.Assignees
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

        var assigneesReplace =
            medianPrice.Assignees
                       .Select(DelegatorExtensions.DelegatorToAssignee)
                       .Map(MapAssignee)
                       .OrderBy(o => o.AssigneeType)
                       .ThenBy(o => o.Sequence)
                       .ToArray();

        var medianPriceSourceQty =
            medianPrice.BudgetAllocations
                       .FirstOrDefault()?.Details.Count
            ?? 0;

        var mustHaveMedianPrice = procurement.Budget > 100000;

        var posBoard006WithMedianPrice =
            committeePositionCode == SuParameterCodeConstant.PosBoard006
                ? "ผู้จัดทำร่างขอบเขตของงาน (TOR) และกำหนดราคากลาง (ราคาอ้างอิง)"
                : "คณะกรรมการจัดทำร่างขอบเขตของงาน (TOR) และกำหนดราคากลาง (ราคาอ้างอิง)";

        var posBoard006WithoutMedianPrice =
            committeePositionCode == SuParameterCodeConstant.PosBoard006
                ? "ผู้จัดทำร่างขอบเขตของงาน (TOR)"
                : "คณะกรรมการจัดทำร่างขอบเขตของงาน (TOR)";

        var appointTypeDescription =
            mustHaveMedianPrice
                ? posBoard006WithMedianPrice
                : posBoard006WithoutMedianPrice;

        var sectionApprove = await this.GetSectionApproveNameAsync(medianPrice, ct);

        var acceptorDate = medianPrice.Status is not (
                MedianPriceStatus.Draft or
                MedianPriceStatus.Rejected or
                MedianPriceStatus.Edit)
            ? medianPrice.DocumentDate?.ToThaiDateString() ?? DateTimeOffset.UtcNow.ToThaiDateString()
            : null;

        var lastedDocumentHistory =
            medianPrice.DocumentHistories
                       .OrderVersions()
                       .FirstOrDefault();

        var memorandumDate =
            appoint.MemorandumDate
                   .ToThaiDateString(
                       thaiNumber: false,
                       format: "d MMMM yyyy");

        var procurementNumber =
            medianPrice.Procurement.ProcurementNumber.HasValue
                ? medianPrice.Procurement.ProcurementNumber.Value.ToString()
                : string.Empty;

        var procurementReplace =
            new ProcurementReplaceDto(
                medianPrice.Procurement.PlanId.Map(p => p.Value),
                procurementNumber,
                medianPrice.Procurement.Type,
                medianPrice.Procurement.Step,
                medianPrice.Procurement.Department.Name,
                medianPrice.Procurement.DepartmentId,
                medianPrice.Procurement.Plan.PlanNumber.ToString(),
                medianPrice.Procurement.Name,
                (medianPrice.Procurement.Budget ?? 0).ToCurrencyStringWithComma(),
                medianPrice.Procurement.Budget.ThaiBahtText(),
                medianPrice.Procurement.BudgetYear,
                medianPrice.Procurement.SupplyMethod.Label,
                medianPrice.Procurement.SupplyMethodCode,
                medianPrice.Procurement.SupplyMethodType?.Label ?? string.Empty,
                medianPrice.Procurement.SupplyMethodTypeCode,
                medianPrice.Procurement.SupplyMethodSpecialType?.Label ?? string.Empty,
                medianPrice.Procurement.SupplyMethodSpecialTypeCode,
                medianPrice.Procurement.Status,
                medianPrice.Procurement.ExpectingProcurementAt,
                medianPrice.Procurement.IsStock,
                medianPrice.Procurement.IsCommercialMaterial,
                medianPrice.Procurement.Plan.Type,
                medianPrice.Procurement.ProcessType);

        var budgetAllocations =
            medianPrice.BudgetAllocations
                       .Map(MapBudgetAllocationReplace)
                       .First();

        var expenseDescription =
            MedianPriceExpenseDescriptionInfo
                .FromEntity(medianPrice.ExpenseDescription);

        var expenseDescriptionReplace =
            new MedianPriceExpenseDescriptionInfoReplaceDto(
                (expenseDescription.MaterialCost ?? 0).ToCurrencyStringWithComma(),
                (expenseDescription.MaterialCost ?? 0).ThaiBahtText(),
                (expenseDescription.OverseasTravelCost ?? 0).ToCurrencyStringWithComma(),
                (expenseDescription.OverseasTravelCost ?? 0).ThaiBahtText(),
                (expenseDescription.OtherExpenses ?? 0).ToCurrencyStringWithComma(),
                (expenseDescription.OtherExpenses ?? 0).ThaiBahtText(),
                (expenseDescription.HardwareCost ?? 0).ToCurrencyStringWithComma(),
                (expenseDescription.HardwareCost ?? 0).ThaiBahtText(),
                (expenseDescription.SoftwareCost ?? 0).ToCurrencyStringWithComma(),
                (expenseDescription.SoftwareCost ?? 0).ThaiBahtText(),
                (expenseDescription.SystemDevelopmentCost ?? 0).ToCurrencyStringWithComma(),
                (expenseDescription.SystemDevelopmentCost ?? 0).ThaiBahtText());

        var appointReplace =
            new MedianPriceAppointReplaceDto(appoint.AppointNumber.Value);

        var staffReplace =
            medianPrice.Staff
                       .Map(MapStaffReplace)
                       .FirstOrDefault();

        var consultantReplace =
            medianPrice.Staff
                       .Map(MapConsultantReplace)
                       .FirstOrDefault();

        return new MedianPriceReplaceDto(
            appointReplace,
            acceptorDate,
            medianPrice.Telephone,
            sectionApprove.SectionApprove,
            medianPriceCommitteeName,
            memorandumDate,
            appointTypeDescription,
            medianPrice.PriceReasonablenessInfo,
            medianPriceSourceQty.ToString(),
            sectionApprove.CommandText,
            medianPrice.Id,
            medianPrice.ProcurementId,
            procurementReplace,
            (string)medianPrice.ReferenceNumber,
            medianPrice.Object,
            medianPrice.Reason,
            medianPrice.SpecialDescription,
            medianPrice.JobDescription,
            medianPrice.PriceReasonablenessInfo,
            medianPrice.DocumentTemplate.Code,
            medianPrice.Status,
            lastedDocumentHistory?.FileId.Value,
            budgetAllocations,
            staffReplace,
            consultantReplace,
            expenseDescriptionReplace,
            acceptorsReplace,
            committeesReplace,
            assigneesReplace,
            creatorReplace,
            jorPorCommentReplace);
    }

    private async Task<SectionApproverDto> GetSectionApproveNameAsync(PpMedianPrice medianPrice, CancellationToken ct)
    {
        var committeesInProcurementDepartment =
            medianPrice.Acceptors
                       .Where(a =>
                           a is { Type: AcceptorType.MedianPriceCommittee, CommitteePositionsCode: not null } &&
                           a.User.Employee.View?.BusinessUnitId == medianPrice.Procurement.DepartmentId)
                       .ToArray();

        var jorPorDirector = await this.operationService.GetDefaultJorPorDirectorAsync(ct);

        var minCommitteePosition =
            committeesInProcurementDepartment
                .OrderByDescending(cd => cd.CommitteePositionsCode.Value.Value)
                .ThenByDescending(cd => cd.Sequence)
                .FirstOrDefault()?
                .UserId.Value
            ?? medianPrice.Acceptors
                          .Where(a => a.Type == AcceptorType.MedianPriceCommittee)?
                          .MaxBy(a => a.Sequence)?
                          .UserId.Value;

        var processType = medianPrice.Procurement.HasMd ? SectionProcessType.MedianPriceHasMD : SectionProcessType.MedianPrice;
        var isStock = medianPrice.Procurement.IsStock;
        var isCommercialMaterial = medianPrice.Procurement.IsCommercialMaterial;

        if (medianPrice.Procurement.HasMd && jorPorDirector is null)
        {
            this.ThrowError("ไม่พบผู้อำนวยการฝ่ายจัดหาและการพัสดุ (จพ.)", StatusCodes.Status400BadRequest);
        }

        var operationUserId = medianPrice.Procurement.HasMd
            ? jorPorDirector!.UserId.Value
            : minCommitteePosition!.Value;

        if (medianPrice.Procurement.SupplyMethodCode.Value == SupplyMethodConstant.Eighty)
        {
            if (isStock)
            {
                processType = SectionProcessType.MedianPriceStock;
            }
            else if (isCommercialMaterial)
            {
                processType = medianPrice.Procurement.HasMd ? SectionProcessType.MedianPriceCommercialParcelHasMD : SectionProcessType.MedianPriceCommercialParcel;
            }
        }

        var managers =
            await this.operationService.GetDefaultAcceptorPositionIgnorePrefixAsync(
                processType,
                operationUserId,
                medianPrice.Procurement.Budget ?? 0,
                medianPrice.Procurement.SupplyMethodCode.Value,
                medianPrice.Procurement.SupplyMethodCode.Value is SupplyMethodConstant.Eighty ? default : (string?)medianPrice.Procurement.SupplyMethodSpecialTypeCode,
                ct);

        var positionNamePrefix = this.operationService.AddPositionNamePrefix(managers);

        var result =
            positionNamePrefix.Select(m => new SectionApprove(m.PositionName))
                              .DefaultIfEmpty(new SectionApprove(string.Empty));

        var commandNumber = managers.FirstOrDefault()?.CommandNumber;

        var commandText = this.commandTextService.GetCommandText(
            CommandTextProgram.MedianPrice,
            managers,
            medianPrice.Procurement.SupplyMethodCode,
            medianPrice.Procurement.Budget ?? 0,
            supplyMethodSpecialType: medianPrice.Procurement.SupplyMethodSpecialTypeCode,
            supplyMethodSpecialName: medianPrice.Procurement.SupplyMethodSpecialType?.Label,
            commandNumber: commandNumber);

        return new SectionApproverDto(result, commandText);
    }

    private async Task<CreatorReplaceDto?> GetCreatorReplaceAsync(PpMedianPrice medianPrice, UserId? creatorUserId, CancellationToken ct)
    {
        var hasCreatorStatus =
            medianPrice.Status is not (
                MedianPriceStatus.Draft or
                MedianPriceStatus.Edit or
                MedianPriceStatus.Rejected);

        if (!hasCreatorStatus)
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
                    medianPrice.Id.ToString(),
                    "ส่งบุคคล/คณะกรรมการกำหนดราคากลาง",
                    ct);

        if (sendToCommitteeApproveByUser == null)
        {
            return null;
        }

        var creatorInCommitteeExisting =
            medianPrice.Acceptors
                       .FirstOrDefault(a =>
                           a.Type == AcceptorType.MedianPriceCommittee &&
                           a.UserId == sendToCommitteeApproveByUser.Id);

        var creatorPositionOnBoardLabel =
            creatorInCommitteeExisting?.CommitteePosition?.Label
            ?? string.Empty;

        return new CreatorReplaceDto(
            sendToCommitteeApproveByUser.Id.Value,
            sendToCommitteeApproveByUser.FullName,
            sendToCommitteeApproveByUser.FullName,
            sendToCommitteeApproveByUser.Employee.View?.FullPositionName ?? string.Empty,
            creatorPositionOnBoardLabel);
    }

    private record SectionApproverDto(IEnumerable<SectionApprove> SectionApprove, string CommandText);

    protected async ValueTask ReplaceDocumentsAsync(
        PpMedianPrice medianPrice,
        bool isReplace,
        CancellationToken ct,
        MedianPriceStatus? previousStatus = null)
    {
        var documentService = this.Resolve<IDocumentService>();
        var statusForReplace = previousStatus ?? medianPrice.Status;
        var replaceDto =
            await this.MapToReplaceDtoAsync(
                medianPrice.Procurement,
                medianPrice,
                ct,
                null,
                medianPrice.Status == MedianPriceStatus.WaitingApproval || medianPrice.Status == MedianPriceStatus.Approved);

        var replaceTemplate = statusForReplace is MedianPriceStatus.WaitingApproval or MedianPriceStatus.RejectToAssignee
            ? medianPrice.LastedNotReplacedWaitingApprovalDocument
            : medianPrice.LastedNotReplacedDocument;

        if (replaceTemplate is not null)
        {
            var finalFileId =
                await documentService.CopyDocumentTemplateAsync(
                    replaceTemplate.FileId,
                    contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto),
                    parentDirectory: $"{DocumentTemplateGroups.Mdp}/{medianPrice.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
                    cancellationToken: ct);

            if (finalFileId is null)
            {
                this.ThrowError("ไม่สามารถคัดลอกเอกสารที่ส่งเห็นชอบ");
            }

            medianPrice.AddDocumentHistory(finalFileId.Value, isReplace);
        }
    }

    private static BudgetAllocationInfoReplaceDto MapBudgetAllocationReplace(PpMedianPriceBudgetAllocations budgetAllocation)
    {
        return new BudgetAllocationInfoReplaceDto(
            budgetAllocation.Id,
            budgetAllocation.ReferenceDate
                            .ToThaiDateString(
                                thaiNumber: false,
                                format: "dd MMMM yyyy"),
            budgetAllocation.Budget.ToCurrencyStringWithComma(),
            budgetAllocation.Budget.ThaiBahtText(),
            budgetAllocation.ReferenceMedianPrice.ToCurrencyStringWithComma(),
            budgetAllocation.ReferenceMedianPrice.ThaiBahtText(),
            [
                .. budgetAllocation.Details
                                   .Map(MapDetail)
                                   .OrderBy(o => o.Sequence)
            ]);

        static BudgetAllocationDetailInfoReplaceDto MapDetail(PpMedianPriceBudgetAllocationsDetail detail)
        {
            return detail switch
            {
                PpMedianPriceBudgetAllocationsWithDetail d => new BudgetAllocationsWithDetailReplaceDto(
                    d.Id,
                    d.Sequence,
                    d.Source,
                    d.ReferenceBudge.ToCurrencyStringWithComma(),
                    d.ReferenceBudge.ThaiBahtText()),
                PpMedianPriceBudgetAllocationsWithoutDetail d => new BudgetAllocationsWithoutDetailReplaceDto(
                    d.Id,
                    d.Sequence,
                    d.Source),
                _ => throw new NotSupportedException($"Unsupported budget allocation detail type: {detail.GetType()}"),
            };
        }
    }

    private static MedianPriceStaffInfoReplaceDto MapStaffReplace(PpMedianPriceStaff staff)
    {
        var staffDetails =
            staff.Details
                 .OfType<PpMedianPriceStaffPersonal>()
                 .Map(sd => new MedianPriceStaffDetailReplaceDto(
                     sd.Sequence,
                     sd.Description,
                     sd.PersonalCount))
                 .OrderBy(sd => sd.Sequence)
                 .ToArray();

        var staffInfo =
            new MedianPriceStaffInfoReplaceDto(
                staff.PersonnelCompensation.ToCurrencyStringWithComma(),
                staff.PersonnelCompensation.ThaiBahtText(),
                staffDetails);

        return staffInfo;
    }

    private static MedianPriceConsultantInfoReplaceDto MapConsultantReplace(PpMedianPriceStaff staff)
    {
        var consultantTypes =
            staff.Details
                 .OfType<PpMedianPriceStaffConsultantTypes>()
                 .Map(ct =>
                     new MedianPriceConsultantDetailReplaceDto(
                         ct.Sequence,
                         ct.Description))
                 .OrderBy(ct => ct.Sequence)
                 .ToArray();

        var consultantQualifications =
            staff.Details
                 .OfType<PpMedianPriceStaffConsultantQualifications>()
                 .Map(cq =>
                     new MedianPriceConsultantDetailReplaceDto(
                         cq.Sequence,
                         cq.Description))
                 .OrderBy(cq => cq.Sequence)
                 .ToArray();

        var consultantInfo =
            new MedianPriceConsultantInfoReplaceDto(
                staff.PersonnelCompensation.ToCurrencyStringWithComma(),
                staff.PersonnelCompensation.ThaiBahtText(),
                staff.PersonnelCount,
                consultantTypes,
                consultantQualifications);

        return consultantInfo;
    }

    private static MedianPriceAcceptorReplaceInfo MapAcceptorReplace(PpMedianPriceAcceptor acceptor)
    {
        var actionLabel =
            acceptor.Type switch
            {
                AcceptorType.Approver => "เห็นชอบ",
                AcceptorType.MedianPriceCommittee =>
                    acceptor.Status switch
                    {
                        AcceptorStatus.Approved => "เห็นชอบ",
                        AcceptorStatus.Rejected => "ไม่เห็นชอบ",
                        AcceptorStatus.UnableToPerformDuties => acceptor.Remark,
                        _ => string.Empty,
                    },
                _ => string.Empty,
            };

        return new MedianPriceAcceptorReplaceInfo(
            acceptor.Id.Value,
            actionLabel,
            acceptor.Type,
            acceptor.UserId.Value,
            acceptor.Sequence,
            acceptor.FullName ?? string.Empty,
            acceptor.PositionName,
            acceptor.BusinessUnitName,
            acceptor.Status,
            acceptor.Remark,
            acceptor.ActionAt,
            Optional(acceptor.CommitteePositionsCode)
                .Map(v => v.Value)
                .IfNoneUnsafe((string?)null),
            acceptor.CommitteePosition?.Label,
            acceptor.IsUnableToPerformDuties,
            acceptor.IsCurrentApprover(),
            acceptor.User?.Employee?.PrimaryDepartment != null ? (string)acceptor.User.Employee.PrimaryDepartment.Id : string.Empty,
            acceptor.CommitteePosition?.Label,
            string.Empty);
    }

    private static async Task SendNotificationAssigneeAsync(PpMedianPrice ppMedianPrice, CancellationToken ct)
    {
        foreach (var targetUserId in ppMedianPrice.Assignees.Where(x => x.Type != AssigneeType.Director).SelectMany(a => a.GetAssigneeNotificationTargets()))
        {
            await Notification
                  .Crate(
                      targetUserId,
                      NotificationConstant.Assignment.Title,
                      string.Format(NotificationConstant.Assignment.Message, ProgramConstant.PreProcurementMedianPrice.Name, ppMedianPrice.ReferenceNumber),
                      NotificationProgram.Procurement)
                  .SetReferenceId(ppMedianPrice.Id.Value)
                  .SetLinkUrl(
                      string.Format(ProgramConstant.Procurement.Url, ppMedianPrice.Procurement.Id),
                      "ดูรายละเอียด")
                  .PublishAsync(ct);
        }
    }
}