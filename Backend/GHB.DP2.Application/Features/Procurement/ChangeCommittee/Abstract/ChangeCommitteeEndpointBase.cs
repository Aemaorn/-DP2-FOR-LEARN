namespace GHB.DP2.Application.Features.Procurement.ChangeCommittee.Abstract;

using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.Procurement.Abstact;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.ChangeCommittee;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using GreatFriends.ThaiBahtText;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public abstract class ChangeCommitteeEndpointBase<TRequest, TResponse> : EndpointBase<TRequest, TResponse>
    where TResponse : IResult
    where TRequest : notnull
{
    private readonly Dp2DbContext dbContext;

    protected ChangeCommitteeEndpointBase(Dp2DbContext dbContext, ILogger logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    protected async Task<CommitteeChanges?> GetChangeCommitteeWithIncludesAsync(CommitteeChangeId id, CancellationToken ct)
    {
        return await this.dbContext.CommitteeChanges
                                   .Include(c => c.Acceptors)
                                   .ThenInclude(a => a.User)
                                   .ThenInclude(u => u.Employee)
                                   .ThenInclude(e => e.View)
                                   .Include(c => c.Procurement)
                                   .ThenInclude(p => p.Plan)
                                   .Include(c => c.Procurement)
                                   .ThenInclude(p => p.Department)
                                   .Include(c => c.Procurement)
                                   .ThenInclude(p => p.SupplyMethod)
                                   .Include(c => c.Procurement)
                                   .ThenInclude(p => p.SupplyMethodType)
                                   .Include(c => c.Procurement)
                                   .ThenInclude(p => p.SupplyMethodSpecialType)
                                   .Include(c => c.DocumentHistories)
                                   .Include(c => c.Assignees)
                                   .FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    private static string MapCommitteeTypeName(CommitteeType committeeType) => committeeType switch
    {
        CommitteeType.TOR => "บุคคล/คณะกรรมการจัดทำร่างขอบเขตงาน",
        CommitteeType.MedianPrice => "บุคคล/คณะกรรมการกำหนดราคากลาง",
        CommitteeType.ProcurementCommittee => "ผู้จัดซื้อจัดจ้าง/คณะกรรมการจัดซื้อจัดจ้าง",
        CommitteeType.InspectionCommittee => "ผู้ตรวจรับ/คณะกรรมการตรวจรับพัสดุ",
        CommitteeType.MaintenanceInspectionCommittee => "คณะกรรมการตรวจรับพัสดุ (งานจ้างบริการบำรุงรักษา)",
        CommitteeType.ConstructionSupervisor => "ผู้ควบคุมงาน (เฉพาะงานก่อสร้าง)",
        _ => committeeType.ToString(),
    };

    private static IEnumerable<CommitteeChangeRowReplace> BuildCommitteeChangeRows(
        IEnumerable<CommitteeReplace> oldCommittees,
        IEnumerable<CommitteeReplace> newCommittees)
    {
        var oldDict = oldCommittees.ToDictionary(c => c.Sequence);
        var newDict = newCommittees.ToDictionary(c => c.Sequence);

        var sequences = oldDict.Keys.Union(newDict.Keys).OrderBy(s => s);

        return sequences.Select(seq =>
        {
            var oldCommittee = oldDict.GetValueOrDefault(seq);
            var newCommittee = newDict.GetValueOrDefault(seq);

            var isSame = oldCommittee is not null && newCommittee is not null &&
                         string.Equals(oldCommittee.FullName, newCommittee.FullName, StringComparison.OrdinalIgnoreCase) &&
                         string.Equals(oldCommittee.FullPositionName, newCommittee.FullPositionName, StringComparison.OrdinalIgnoreCase) &&
                         string.Equals(oldCommittee.PositionOnBoard, newCommittee.PositionOnBoard, StringComparison.OrdinalIgnoreCase);

            return new CommitteeChangeRowReplace(
                seq,
                oldCommittee?.FullName ?? "-",
                oldCommittee?.FullPositionName ?? string.Empty,
                oldCommittee?.PositionOnBoard ?? "-",
                newCommittee?.FullName ?? "-",
                newCommittee?.FullPositionName ?? string.Empty,
                newCommittee?.PositionOnBoard ?? "-",
                isSame ? "คงเดิม" : "ใหม่");
        }).ToList();
    }

    protected async ValueTask SetDefaultDocumentTemplate(
        CommitteeChanges changeCommittee,
        bool isReplace,
        bool isJorPorComment,
        CancellationToken ct)
    {
        var documentService = this.Resolve<IDocumentService>();

        var fileId = await documentService.GetDocumentTemplateAsync(
            dt => dt.Group == DocumentTemplateGroups.CommitteeChange &&
                  dt.IsActive &&
                  (!isJorPorComment
                      ? dt.AdditionalInfo == null
                      : EF.Functions.JsonExists(dt.AdditionalInfo!, nameof(SuDocumentTemplate.IsJorPorComment)) &&
                        dt.AdditionalInfo!.RootElement
                            .GetProperty(nameof(SuDocumentTemplate.IsJorPorComment))
                            .GetBoolean() == isJorPorComment),
            ct);

        if (fileId.HasValue)
        {
            changeCommittee.AddDocumentHistory(fileId.Value, isReplace);
        }
    }

    protected async ValueTask ReplaceDocumentsAsync(
        CommitteeChanges changeCommittee,
        bool isReplace,
        CancellationToken ct)
    {
        var changeCommitteeWithIncludes = await this.GetChangeCommitteeWithIncludesAsync(changeCommittee.Id, ct);

        if (changeCommitteeWithIncludes is null)
        {
            return;
        }

        var replaceTemplate = changeCommitteeWithIncludes.LastedNotReplacedCommitteeDocument;

        if (replaceTemplate is null)
        {
            return;
        }

        var hasAcceptor = changeCommitteeWithIncludes.Status is CommitteeChangeStatus.WaitingApproval or CommitteeChangeStatus.Approved;
        var documentService = this.Resolve<IDocumentService>();
        var replaceDto = await this.MapToReplaceDto(changeCommitteeWithIncludes, hasAcceptor, ct);
        var fontName = GetFontName(changeCommitteeWithIncludes);
        var copiedFileId = await documentService.CopyDocumentTemplateAsync(
            replaceTemplate.FileId,
            contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto, fontName),
            parentDirectory: $"{DocumentTemplateGroups.CommitteeChange}/{changeCommitteeWithIncludes.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
            cancellationToken: ct);

        if (copiedFileId.HasValue)
        {
            changeCommitteeWithIncludes.AddDocumentHistory(copiedFileId.Value, isReplace);
            await this.dbContext.SaveChangesAsync(ct);
        }
    }

    protected static string GetFontName(CommitteeChanges data) =>
        SupplyMethodConstant.GetDocumentFontName(data.Procurement.SupplyMethodCode);

    protected async Task<CommitteeChangeReplaceDto> MapToReplaceDto(CommitteeChanges data, bool hasAcceptor, CancellationToken ct)
    {
        var acceptorDate =
            data.Status is not (CommitteeChangeStatus.Draft or CommitteeChangeStatus.Edit or CommitteeChangeStatus.Rejected)
                ? data.DocumentDate?.ToThaiDateString(includeBuddhistEra: false) ?? DateTimeOffset.Now.ToThaiDateString(includeBuddhistEra: false)
                : null;

        var procurement = data.Procurement;

        var procurementDto = new ProcurementReplaceDto(
            procurement.PlanId.HasValue ? (Guid)procurement.PlanId : null,
            procurement.ProcurementNumber.HasValue ? procurement.ProcurementNumber.Value.ToString() : null,
            procurement.Type,
            procurement.Step,
            procurement.Department.Name,
            procurement.DepartmentId,
            procurement.Plan?.PlanNumber.ToString(),
            procurement.Name,
            (procurement.Budget ?? 0).ToCurrencyStringWithComma(),
            (procurement.Budget ?? 0).ThaiBahtText(),
            procurement.BudgetYear,
            procurement.SupplyMethod.Label,
            procurement.SupplyMethodCode,
            procurement.SupplyMethodType?.Label,
            procurement.SupplyMethodTypeCode,
            procurement.SupplyMethodSpecialType?.Label,
            procurement.SupplyMethodSpecialTypeCode,
            procurement.Status,
            procurement.ExpectingProcurementAt,
            procurement.IsStock,
            procurement.IsCommercialMaterial,
            procurement.Plan?.Type,
            procurement.ProcessType);

        var acceptorReplaces = new List<AcceptorReplace>();

        if (hasAcceptor)
        {
            var acceptors =
                data.Acceptors
                       .Where(x => x.Type == AcceptorType.Approver)
                       .OrderBy(a => a.Sequence)
                       .Map(DelegatorExtensions.DelegatorToAcceptor)
                       .Select(a => new AcceptorReplace(
                           a.Id.Value,
                           a.Type,
                           a.UserId.Value,
                           a.EmployeeCode.Value,
                           a.FullName,
                           a.PositionName,
                           a.BusinessUnitName,
                           a.Sequence,
                           a.DelegateeId?.Value,
                           a.Status,
                           a.ActionAt,
                           a.Remark,
                           string.Empty,
                           string.Empty,
                           "เห็นชอบ",
                           string.Empty))
                       .ToList();

            if (acceptors.Any())
            {
                acceptors[^1] =
                    acceptors.Last() with { Action = "อนุมัติ" };
            }

            acceptorReplaces =
                [.. acceptors.Where(a => a.Status == AcceptorStatus.Approved)];
        }

        var oldCommittees = data.OldCommittees
                                .OrderBy(c => c.Sequence)
                                .Select(c => new CommitteeReplace(
                                    c.SuUserId,
                                    c.Sequence,
                                    string.Empty,
                                    string.Empty,
                                    c.FullName,
                                    c.FullPositionName,
                                    c.CommitteePositionsName,
                                    null))
                                .ToArray();

        var newCommittees = data.NewCommittees
                                .OrderBy(c => c.Sequence)
                                .Select(c => new CommitteeReplace(
                                    c.SuUserId,
                                    c.Sequence,
                                    string.Empty,
                                    string.Empty,
                                    c.FullName,
                                    c.FullPositionName,
                                    c.CommitteePositionsName,
                                    null))
                                .ToArray();

        var committeeRows = BuildCommitteeChangeRows(oldCommittees, newCommittees);

        var committeesReplace = data.Status is CommitteeChangeStatus.WaitingAssign
            or CommitteeChangeStatus.WaitingComment
            or CommitteeChangeStatus.WaitingApproval
            or CommitteeChangeStatus.Approved
            ? (IEnumerable<AcceptorReplace>)[.. data.Acceptors
                .Where(a => a.Type != AcceptorType.Approver)
                .Map(MapAcceptorReplace)
                .OrderBy(a => a.Sequence)]
            : [];

        var lastAssignee = data.Status is CommitteeChangeStatus.WaitingComment
            or CommitteeChangeStatus.WaitingApproval
            or CommitteeChangeStatus.Approved
            ? data.Assignees
                .Where(a => a.Type == AssigneeType.Assignee)
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

        IEnumerable<SectionApprove>? sectionApproveName = null;

        if (data.SourceType == SourceType.Appoint)
        {
            var processType = SectionProcessType.AppointPreProcurement;
            var isStock = procurement.IsStock;
            var isCommercialMaterial = procurement.IsCommercialMaterial;

            if (procurement.SupplyMethodCode.Value == SupplyMethodConstant.Eighty)
            {
                if (isStock)
                {
                    processType = SectionProcessType.AppointPreProcurementStock;
                }
                else if (isCommercialMaterial)
                {
                    processType = SectionProcessType.AppointPreProcurementCommercialParcel;
                }
            }

            var operationService = this.Resolve<IOperationService>();

            var managers =
                await operationService.GetDefaultAcceptorPositionIgnorePrefixAsync(
                    processType,
                    data.AuditInfo.CreatedBy,
                    data.Procurement.Budget ?? 0,
                    data.Procurement.SupplyMethodCode.Value,
                    data.Procurement.SupplyMethodCode.Value is SupplyMethodConstant.Eighty ? default : (string?)data.Procurement.SupplyMethodSpecialTypeCode,
                    ct);

            var positionNamePrefix = operationService.AddPositionNamePrefix(managers);

            sectionApproveName =
                positionNamePrefix.Select(m => new SectionApprove(m.PositionName));
        }

        if (data.SourceType == SourceType.Jp005)
        {
            var processType = SectionProcessType.ApprovePurchaseRequest;
            var isCommercialMaterial = procurement.IsCommercialMaterial;

            if (isCommercialMaterial && procurement.SupplyMethodCode.Value == SupplyMethodConstant.Eighty)
            {
                processType = SectionProcessType.ApprovePurchaseRequestCommercialParcel;
            }

            var lastedAssigneeJp04 = data.Assignees
                .Where(a => a.Type == AssigneeType.Assignee)
                .OrderBy(a => a.Sequence)
                .LastOrDefault();

            var operationServiceJp005 = this.Resolve<IOperationService>();

            var managers = await operationServiceJp005.GetDefaultAcceptorPositionAsync(
                processType,
                lastedAssigneeJp04?.UserId.Value ?? data.AuditInfo.CreatedBy,
                procurement.Budget ?? 0,
                procurement.SupplyMethodCode.Value,
                procurement.SupplyMethodCode.Value is SupplyMethodConstant.Eighty ? default : (string?)procurement.SupplyMethodSpecialTypeCode,
                ct);

            sectionApproveName = operationServiceJp005.AddPositionNamePrefix(managers)
                .Select(m => new SectionApprove(m.PositionName))
                .DefaultIfEmpty(new SectionApprove(string.Empty));
        }

        if (data.SourceType == SourceType.PurchaseRequisition)
        {
            sectionApproveName = new[] { new SectionApprove(string.Format("ผู้อำนวยการ{0}", procurement.Department.Name)) };
        }

        if (data.SourceType == SourceType.PurchaseRequisition)
        {
            var processType = SectionProcessType.ApprovePurchaseOrder;
            var isStock = procurement.IsStock;
            var isCommercialMaterial = procurement.IsCommercialMaterial;

            if (procurement.SupplyMethodCode.Value == SupplyMethodConstant.Eighty)
            {
                if (isStock && isCommercialMaterial)
                {
                    processType = SectionProcessType.ApprovePurchaseOrderCommercialParcel;
                }
            }

            var operationService = this.Resolve<IOperationService>();

            var managers =
                await operationService.GetDefaultAcceptorPositionIgnorePrefixAsync(
                    processType,
                    data.AuditInfo.CreatedBy,
                    data.Procurement.Budget ?? 0,
                    data.Procurement.SupplyMethodCode.Value,
                    data.Procurement.SupplyMethodCode.Value is SupplyMethodConstant.Eighty ? default : (string?)data.Procurement.SupplyMethodSpecialTypeCode,
                    ct);

            var positionNamePrefix = operationService.AddPositionNamePrefix(managers);

            sectionApproveName =
                positionNamePrefix.Select(m => new SectionApprove(m.PositionName));
        }

        if (data.SourceType == SourceType.PrincipleApproval)
        {
            sectionApproveName = new[] { new SectionApprove("กรรมการผู้จัดการ\r\nผ่าน ผู้อำนวยการฝ่ายจัดหาและการพัสดุ") };
        }

        return new CommitteeChangeReplaceDto(
            string.Empty,
            string.Empty,
            procurementDto,
            acceptorReplaces,
            acceptorDate,
            MapCommitteeTypeName(data.CommitteeType),
            data.Remark,
            data.SourceType.ToString(),
            sectionApproveName,
            oldCommittees,
            newCommittees,
            committeeRows,
            committeesReplace,
            jorPorCommentReplace);
    }

    private static AcceptorReplace MapAcceptorReplace(CommitteeChangeAcceptor acceptor)
    {
        var actionLabel = acceptor.Type switch
        {
            AcceptorType.Approver => "เห็นชอบ",
            _ => acceptor.Status switch
            {
                AcceptorStatus.Approved => "เห็นชอบ",
                AcceptorStatus.Rejected => "ไม่เห็นชอบ",
                _ => string.Empty,
            },
        };

        if (acceptor.IsUnableToPerformDuties)
        {
            actionLabel = acceptor.Remark ?? "ไม่สามารถปฏิบัติงานได้";
        }

        return new AcceptorReplace(
            acceptor.Id.Value,
            acceptor.Type,
            acceptor.UserId.Value,
            acceptor.EmployeeCode.Value,
            acceptor.FullName,
            acceptor.PositionName,
            acceptor.BusinessUnitName,
            acceptor.Sequence,
            acceptor.DelegateeId?.Value,
            acceptor.Status,
            acceptor.ActionAt,
            acceptor.Remark,
            string.Empty,
            string.Empty,
            actionLabel,
            acceptor.CommitteePosition?.Label ?? string.Empty);
    }
}
