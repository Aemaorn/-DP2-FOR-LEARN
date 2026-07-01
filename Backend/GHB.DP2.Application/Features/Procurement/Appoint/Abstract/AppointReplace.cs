namespace GHB.DP2.Application.Features.Procurement.Appoint.Abstract;

using Codehard.Common.Extensions;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.Procurement.Procurement.Abstact;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.PpAppoint;
using GHB.DP2.Domain.SystemUtility;
using GreatFriends.ThaiBahtText;

public abstract partial class AppointEndpointBase<TRequest, TResponse>
{
    // Example for map response DTO for GetListMapping
    protected async Task<AppointReplaceDto> MapToReplaceDto(
        PpAppoint appoint,
        CancellationToken ct,
        SuUser? currentEmp = null,
        bool isPreview = false)
    {
        var lastedHistory =
            appoint.DocumentHistories
                   .WhereIf(
                       appoint.Status is
                           AppointStatus.Edit or
                           AppointStatus.Cancelled,
                       d => d.StatusState == AppointStatus.Draft)
                   .OrderVersions()
                   .FirstOrDefault();

        AppointCreatorDto? creatorReplace = null;

        if (currentEmp is not null)
        {
            creatorReplace = new AppointCreatorDto(
                currentEmp.Id,
                "ผู้จัดทำ",
                currentEmp.Employee.View?.FullName ?? string.Empty,
                currentEmp.Employee.View?.FullPositionName ?? string.Empty,
                string.Empty);
        }

        var hasCreatorStatus =
            appoint.Status is not (
                AppointStatus.Draft or
                AppointStatus.Edit or
                AppointStatus.Rejected);

        if (hasCreatorStatus && creatorReplace is null)
        {
            var sendToCommitteeApproveByUser =
                await this.GetLastActivityCreatedByAsync(
                    appoint.Id.ToString(),
                    ActivityLogActionTypeConstant.SendApprove,
                    ct);

            if (sendToCommitteeApproveByUser is not null && (appoint.Status == AppointStatus.WaitingApproval || appoint.Status == AppointStatus.Approved))
            {
                creatorReplace = new AppointCreatorDto(
                    sendToCommitteeApproveByUser.Id,
                    "ผู้จัดทำ",
                    sendToCommitteeApproveByUser.Employee.View?.FullName ?? string.Empty,
                    sendToCommitteeApproveByUser.Employee.View?.FullPositionName ?? string.Empty,
                    string.Empty);
            }
        }

        var committeeTorIsCreator =
            appoint.TorDraftCommittees
                   .Any(c => c.CommitteePositionsCode == SuParameterCodeConstant.PosBoard006);

        var committeeMedianPriceIsCreator =
            appoint.MedianPriceCommittees
                   .Any(c => c.CommitteePositionsCode == SuParameterCodeConstant.PosBoard006);

        var committeeTorSection =
            committeeTorIsCreator
                ? "ผู้จัดทำ"
                : "คณะกรรมการ";

        var committeeMedianPriceSection =
            committeeMedianPriceIsCreator
                ? "ผู้จัดทำ"
                : "คณะกรรมการ";

        var reason = appoint.Reason;
        var listRemark = appoint.Acceptors.Select(x => x.Remark).ToList();
        var remarkText = listRemark.Any() ? string.Join(", ", listRemark) : "ไม่มีหมายเหตุ";

        var approvedAcceptors = new List<AppointAcceptorReplace>();

        if (!isPreview)
        {
            var acceptors =
                appoint.Acceptors
                       .OrderBy(a => a.Sequence)
                       .Map(DelegatorExtensions.DelegatorToAcceptor)
                       .Select(a => new AppointAcceptorReplace(
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
                           a.IsActive,
                           CurrentAcceptor(appoint.Acceptors, a.Id.Value, appoint.Status),
                           string.Empty,
                           string.Empty,
                           "เห็นชอบ"))
                       .ToList();

            if (acceptors.Any())
            {
                acceptors[^1] =
                    acceptors.Last() with { Action = "อนุมัติ" };
            }

            approvedAcceptors =
                [.. acceptors.Where(a => a.Status == AcceptorStatus.Approved)];
        }

        var memorandumDate =
            appoint.MemorandumDate.ToThaiDateString(thaiNumber: false);

        var procurementNumber =
            appoint.Procurement.ProcurementNumber.HasValue
                ? appoint.Procurement.ProcurementNumber.Value.ToString()
                : string.Empty;

        var operatorUser =
            creatorReplace?.UserId ??
            UserId.From(appoint.AuditInfo.CreatedBy);

        var processType = SectionProcessType.AppointPreProcurement;
        var isStock = appoint.Procurement.IsStock;
        var isCommercialMaterial = appoint.Procurement.IsCommercialMaterial;

        if (appoint.Procurement.SupplyMethodCode.Value == SupplyMethodConstant.Eighty)
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

        var managers =
            await this.operationService.GetDefaultAcceptorPositionIgnorePrefixAsync(
                processType,
                operatorUser.Value,
                appoint.Procurement.Budget ?? 0,
                appoint.Procurement.SupplyMethodCode.Value,
                appoint.Procurement.SupplyMethodCode.Value is SupplyMethodConstant.Eighty ? default : (string?)appoint.Procurement.SupplyMethodSpecialTypeCode,
                ct);

        var positionNamePrefix = this.operationService.AddPositionNamePrefix(managers);

        var sectionApproveName =
            positionNamePrefix.Select(m => new SectionApprove(m.PositionName));

        var commandNumber = managers.FirstOrDefault()?.CommandNumber;

        var commandText = this.commandTextService.GetCommandText(
            CommandTextProgram.Appoint,
            managers,
            appoint.Procurement.SupplyMethodCode,
            appoint.Procurement.Budget ?? 0,
            supplyMethodSpecialType: appoint.Procurement.SupplyMethodSpecialTypeCode,
            supplyMethodSpecialName: appoint.Procurement.SupplyMethodSpecialType?.Label,
            commandNumber: commandNumber);

        return new AppointReplaceDto(
            reason,
            remarkText,
            memorandumDate,
            commandText,
            committeeTorSection,
            committeeMedianPriceSection,
            sectionApproveName,
            new ProcurementReplaceDto(
                appoint.Procurement.PlanId.HasValue ? (Guid)appoint.Procurement.PlanId : null,
                appoint.AppointNumber.Value,
                appoint.Procurement.Type,
                appoint.Procurement.Step,
                appoint.Procurement.Department.Name,
                appoint.Procurement.DepartmentId,
                appoint.Procurement.Plan?.PlanNumber.ToString() ?? string.Empty,
                appoint.Procurement.Name,
                (appoint.Procurement.Budget ?? 0).ToCurrencyStringWithComma(),
                (appoint.Procurement.Budget ?? 0).ThaiBahtText(),
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
            new AppointReplace(
                appoint.Id.Value,
                appoint.ProcurementId.Value,
                appoint.AppointNumber.Value,
                memorandumDate,
                appoint.MemorandumNumber,
                appoint.Telephone,
                appoint.Reason,
                appoint.Status,
                appoint.ChangeReason,
                appoint.CancelReason,
                appoint.IsChange,
                appoint.IsCancel),
            appoint.TorDraftCommittees
                   .OrderBy(c => c.Sequence)
                   .Select(c => new AppointTorDraftCommitteeReplace(
                       c.Id.Value,
                       c.SuUserId.Value,
                       c.FullName,
                       c.FullPositionName,
                       c.User.Employee.View?.BusinessUnitId.Value?.ToString(),
                       c.CommitteePositions.Label,
                       c.CommitteePositionsCode.Value,
                       c.Sequence)),
            appoint.TorDraftCommitteeDuties
                   .OrderBy(cd => cd.Sequence)
                   .Select(cd => new DutiesReplace(
                       cd.Id.Value,
                       cd.Description,
                       cd.Sequence)),
            appoint.MedianPriceCommittees
                   .OrderBy(c => c.Sequence)
                   .Select(c => new AppointMedianPriceCommitteeReplace(
                       c.Id.Value,
                       c.SuUserId.Value,
                       c.FullName,
                       c.FullPositionName,
                       c.User.Employee.View?.BusinessUnitId.Value?.ToString(),
                       c.CommitteePositionsCode.Value,
                       c.CommitteePositions.Label,
                       c.Sequence)),
            appoint.MedianPriceCommitteeDuties
                   .OrderBy(cd => cd.Sequence)
                   .Select(cd => new DutiesReplace(
                       cd.Id.Value,
                       cd.Description,
                       cd.Sequence)),
            approvedAcceptors,
            lastedHistory?.FileId.Value,
            appoint.TorDraftCommittees.All(x => x.IsCommittee()),
            appoint.MedianPriceCommittees.All(x => x.IsCommittee()),
            creatorReplace);
    }
}