namespace GHB.DP2.Application.Features.Procurement.PrincipleApproval;

using System.ComponentModel;
using Codehard.Common.Extensions;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.Procurement.Invite;
using GHB.DP2.Application.Features.Procurement.PrincipleApproval.Abstract;
using GHB.DP2.Application.Features.Procurement.PrincipleApproval.Constants;
using GHB.DP2.Application.Features.Procurement.Procurement;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PPrincipleApproval;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using GreatFriends.ThaiBahtText;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record GetByIdPrincipleApprovalRequest(Guid ProcurementId, Guid? Id);

public class GetByIdPrincipleApprovalEndpoint : PrincipleApprovalEndpointBase<GetByIdPrincipleApprovalRequest, Results<Ok<PrincipleApprovalResponseDto>, NotFound<string>>>
{
    public GetByIdPrincipleApprovalEndpoint(
        ILogger<GetByIdPrincipleApprovalEndpoint> logger,
        Dp2DbContext dbContext)
        : base(logger, dbContext)
    {
    }

    public override void Configure()
    {
        this.Get("procurement/{procurementId:guid}/principle-approval/{id:guid?}");
        this.Description(b => b
                              .WithTags("Procurement/PrincipleApproval")
                              .WithName("GetPrincipleApprovalById")
                              .AllowAnonymous()
                              .Produces<PrincipleApprovalResponseDto>(StatusCodes.Status200OK)
                              .Produces<string>(StatusCodes.Status404NotFound));
    }

    protected override async ValueTask<Results<Ok<PrincipleApprovalResponseDto>, NotFound<string>>> HandleRequestAsync(GetByIdPrincipleApprovalRequest req, CancellationToken ct)
    {
        if (req.Id is null)
        {
            return TypedResults.Ok(PrincipleApprovalResponseDto.Default());
        }

        var approval = await this.GetPPrincipleApprovalById(PPrincipleApprovalId.From(req.Id.Value), ProcurementId.From(req.ProcurementId), ct);

        var response = this.MapToResponse(approval);

        return TypedResults.Ok(response);
    }
}

public record PrincipleApprovalDocumentVersionResponse(
    [property: Description("รหัสไฟล์")] Guid FileId,
    [property: Description("เวอร์ชัน")] string Version,
    [property: Description("วันที่สร้าง")] DateTimeOffset CreatedAt,
    [property: Description("ชื่อผู้สร้าง")] string CreatedByName,
    [property: Description("เป็นเวอร์ชันปัจจุบัน")] bool IsCurrent);

public record PrincipleApprovalResponseDto(
    [property: Description("รหัสขออนุมัติหลักการ")]
    Guid? Id,
    [property: Description("รหัสการจัดซื้อจัดจ้าง")]
    Guid? ProcurementId,
    [property: Description("ข้อมูลการจัดซื้อจัดจ้าง")]
    ProcurementDto? Procurement,
    [property: Description("สถานที่ตั้งสาขา")]
    string? BranchLocation,
    [property: Description("รหัสเทมเพลตเอกสาร")]
    Guid? DocumentTemplateId,
    [property: Description("รหัสเทมเพลตเอกสาร เปลี่ยนแปลง")]
    bool? IsDocumentTemplateIdReplace,
    [property: Description("ประวัติเวอร์ชันเอกสาร")]
    PrincipleApprovalDocumentVersionResponse[]? DocumentVersions,
    [property: Description("รหัสประเภทการเช่า")]
    string? RentTypeCode,
    [property: Description("วันที่เริ่มเช่า")]
    DateTimeOffset? RentalStartDate,
    [property: Description("วันที่สิ้นสุดการเช่า")]
    DateTimeOffset? RentalEndDate,
    [property: Description("ระยะเวลาการเช่า (ปี)")]
    int? RentalDurationYear,
    [property: Description("ระยะเวลาการเช่า (เดือน)")]
    int? RentalDurationMonth,
    [property: Description("ระยะเวลาการเช่า (วัน)")]
    int? RentalDurationDay,
    [property: Description("ค่าเช่าสูงสุดต่อเดือน")]
    decimal? MaxMonthlyRent,
    [property: Description("ค่าเช่ารวมทั้งสิ้น")]
    decimal? TotalRentalAmount,
    [property: Description("วันที่คาดว่าจะทำสัญญา")]
    DateTimeOffset? ExpectedContractDate,
    [property: Description("รายละเอียดสถานที่เช่า")]
    string? RentalLocationDetails,
    [property: Description("รหัสตำบล")] string? SubDistrictCode,
    [property: Description("ชื่อตำบล")] string? SubDistrictName,
    [property: Description("รหัสอำเภอ")] string? DistrictCode,
    [property: Description("ชื่ออำเภอ")] string? DistrictName,
    [property: Description("รหัสจังหวัด")] string? ProvinceCode,
    [property: Description("ชื่อจังหวัด")] string? ProvinceName,
    [property: Description("ราคาอ้างอิง")] decimal? ReferencePriceAmount,
    [property: Description("มูลค่าปัจจุบันสุทธิ (NPV)")]
    decimal? AnalysisSummaryNpv,
    [property: Description("ระยะเวลาคืนทุน (Payback Period)")]
    decimal? AnalysisSummaryPaybackYearPeriod,
    [property: Description("ระยะเวลาคืนทุนส่วนลด (Discounted Payback Period)")]
    decimal? AnalysisSummaryDiscountedPaybackYearPeriod,
    string? PhoneNumber,
    [property: Description("สถานะ")] PPrincipleApprovalStatus Status,
    [property: Description("ผู้อนุมัติ")] IEnumerable<PrincipleApprovalAcceptorResponseDto> Acceptors,
    [property: Description("ผู้รับมอบหมาย")]
    IEnumerable<PrincipleApprovalAssigneeResponseDto> Assignees,
    [property: Description("คณะกรรมการ")] IEnumerable<PrincipleApprovalCommitteeResponseDto>? Committees,
    [property: Description("ข้อมูลการสนับสนุนผลการดำเนินงาน")]
    PrincipleApprovalPerfSupportDataResponseDto? PerfSupportData,
    [property: Description("รายละเอียดการสนับสนุนผลการดำเนินงาน")]
    IEnumerable<PrincipleApprovalPerfSupportDataDetailResponseDto> PerfSupportDataDetails,
    [property: Description("ข้อมูลสรุปผลตอบแทนจากสินเชื่อและเงินฝาก")]
    IEnumerable<PrincipleApprovalRoiLoanAndDepositSummaryResponseDto>? RoiLoanAndDepositSummaries,
    [property: Description("ผลการดำเนินงานตามผลตอบแทน")]
    IEnumerable<PrincipleApprovalRoiPerfResultResponseDto>? RoiPerfResults,
    [property: Description("ข้อมูลงบประมาณ")]
    IEnumerable<PrincipleApprovalBudgetDto>? Budgets,
    [property: Description("การวิเคราะห์การเช่า")]
    IEnumerable<PrincipleApprovalRentalAnalysisDto>? RentalAnalyses,
    [property: Description("เป็นคณะกรรมการจัดเช่า")]
    bool IsRentCommittee,
    [property: Description("เป็นคณะกรรมการตรวจรับ")]
    bool IsAcceptanceCommittee,
    IEnumerable<EmailAttachment> Attachments,
    [property: Description("วันที่เอกสาร")]
    DateTimeOffset? DocumentDate = null)
{
    public static PrincipleApprovalResponseDto Default() =>
        new(
            null,
            null,
            null,
            null,
            null,
            null,
            null, // DocumentVersions
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null, // PhoneNumber
            PPrincipleApprovalStatus.Draft,
            [],
            [],
            [],
            null,
            PrincipleApprovalConstant.DefaultPerfSupportDataDetails(),
            PrincipleApprovalConstant.DefaultRoiLoanAndDepositSummaries(),
            [],
            [],
            PrincipleApprovalConstant
                .DefaultRentalAnalysis()
                .OrderBy(x => x.Type)
                .ThenBy(x => x.Sequence),
            true,
            true,
            [],
            null);

    public static PrincipleApprovalResponseDto? MapToResponse(Procurement? procurement)
    {
        if (procurement is null)
        {
            return null;
        }

        return new(
            null,
            null,
            new ProcurementDto(
                procurement.PlanId.HasValue ? (Guid)procurement.PlanId.Value : null,
                procurement.ProcurementNumber,
                procurement.Type,
                procurement.Step,
                procurement.Department.Name,
                procurement.DepartmentId,
                procurement.Plan?.PlanNumber.ToString(),
                procurement.Name,
                procurement.Budget,
                procurement.Budget.ThaiBahtText(),
                procurement.BudgetYear,
                procurement.SupplyMethod.Label,
                procurement.SupplyMethodCode,
                procurement.SupplyMethodType?.Label ?? string.Empty,
                procurement.SupplyMethodTypeCode,
                procurement.SupplyMethodSpecialType?.Label ?? string.Empty,
                procurement.SupplyMethodSpecialTypeCode,
                procurement.Status,
                procurement.ExpectingProcurementAt,
                procurement.IsStock,
                procurement.IsCommercialMaterial,
                procurement.Plan?.Type,
                procurement.ProcessType),
            null,
            null,
            null,
            null, // DocumentVersions
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            PPrincipleApprovalStatus.Draft,
            [],
            [],
            [],
            null,
            PrincipleApprovalConstant.DefaultPerfSupportDataDetails(),
            PrincipleApprovalConstant.DefaultRoiLoanAndDepositSummaries(),
            [],
            [],
            PrincipleApprovalConstant
                .DefaultRentalAnalysis()
                .OrderBy(x => x.Type)
                .ThenBy(x => x.Sequence),
            true,
            true,
            [],
            null);
    }

    public static PrincipleApprovalResponseDto? MapToResponse(
        PPrincipleApproval? approval)
    {
        if (approval is null)
        {
            return null;
        }

        var perfSupportData = approval.PerfSupportData.FirstOrDefault();
        var assignee = approval.PrincipleApprovalAssignees;
        var committee = approval.PrincipleApprovalCommittees;
        var perfSupportDataDetailData = approval.PerfSupportDataDetails;
        var roiLoanAndDepositSummaries = approval.RoiLoanAndDepositSummaries;
        var roiPerfResults = approval.RoiPerfResults;

        var useDraftTemplate = approval.Status == PPrincipleApprovalStatus.Rejected;

        var lastedApprovalHistory =
            approval.DocumentHistories
                    .WhereIf(
                        useDraftTemplate,
                        dh =>
                            dh.StatusState == PPrincipleApprovalStatus.Draft)
                    .OrderVersions()
                    .FirstOrDefault();

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
            lastedApprovalHistory?.FileId.Value,
            lastedApprovalHistory?.IsReplaced,
            approval.DocumentHistories
                    .OrderVersions()
                    .Select((d, index) => new PrincipleApprovalDocumentVersionResponse(
                        d.FileId.Value,
                        d.Version,
                        d.CreatedAt,
                        d.CreatedByName ?? string.Empty,
                        index == 0))
                    .ToArray(),
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
                    .Where(a => DelegatorExtensions.IsDelegatableType(a.Type))
                    .Select(DelegatorExtensions.DelegatorToAcceptor)
                    .ToList()
                    .Union(approval.PrincipleApprovalAcceptors
                        .Where(a => !DelegatorExtensions.IsDelegatableType(a.Type)))
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
                        a.IsCurrentApprover()))
                    .OrderBy(x => x.Sequence),
            assignee.Select(DelegatorExtensions.DelegatorToAssignee)
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
                        a.ActionAt))
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
                    .Select(a => new EmailAttachment(a.Id.Value, a.FileName, a.FileId, a.Sequence)),
            approval.DocumentDate);
    }
}

public record PrincipleApprovalAcceptorResponseDto(
    [property: Description("รหัสผู้อนุมัติ")]
    Guid Id,
    [property: Description("ประเภทผู้อนุมัติ")]
    AcceptorType AcceptorType,
    [property: Description("รหัสผู้ใช้งาน")]
    Guid UserId,
    [property: Description("รหัสพนักงาน")] string EmployeeCode,
    [property: Description("ชื่อ-สกุล")] string FullName,
    [property: Description("ตำแหน่ง")] string PositionName,
    [property: Description("ชื่อหน่วยงาน")]
    string DepartmentName,
    [property: Description("ลำดับ")] int Sequence,
    [property: Description("สถานะการอนุมัติ")]
    AcceptorStatus Status,
    [property: Description("หมายเหตุ")] string? Remark,
    [property: Description("วันที่ดำเนินการ")]
    DateTimeOffset? ActionAt = default,
    [property: Description("เป็นผู้อนุมัติปัจจุบัน")]
    bool IsCurrent = false,
    Guid? DelegateeUserId = default
);

public record PrincipleApprovalAssigneeResponseDto(
    [property: Description("รหัสผู้รับมอบหมาย")]
    Guid Id,
    [property: Description("กลุ่มผู้รับมอบหมาย")]
    AssigneeGroup AssigneeGroup,
    [property: Description("ประเภทผู้รับมอบหมาย")]
    AssigneeType AssigneeType,
    [property: Description("รหัสผู้ใช้งาน")]
    Guid UserId,
    [property: Description("รหัสพนักงาน")] string EmployeeCode,
    [property: Description("ชื่อ-สกุล")] string FullName,
    [property: Description("ตำแหน่ง")] string PositionName,
    [property: Description("ชื่อหน่วยงาน")]
    string DepartmentName,
    [property: Description("หมายเหตุ")] string? Remark,
    [property: Description("ลำดับ")] int Sequence,
    AssigneeStatus Status,
    [property: Description("วันที่ดำเนินการ")]
    DateTimeOffset? ActionAt = default,
    Guid? DelegateeUserId = default);

public record PrincipleApprovalCommitteeResponseDto(
    [property: Description("รหัสคณะกรรมการ")]
    Guid Id,
    [property: Description("ประเภทกลุ่มคณะกรรมการ")]
    CommitteeGroupType GroupType,
    [property: Description("รหัสผู้ใช้งาน")]
    UserId UserId,
    [property: Description("ชื่อ-สกุล")] string FullName,
    [property: Description("ตำแหน่งเต็ม")] string FullPositionName,
    [property: Description("รหัสตำแหน่งคณะกรรมการ")]
    ParameterCode CommitteePositionsCode,
    [property: Description("ชื่อตำแหน่งคณะกรรมการ")]
    string CommitteePositionsName,
    [property: Description("ลำดับ")] int Sequence
);

public record PrincipleApprovalPerfSupportDataResponseDto(
    [property: Description("รหัสข้อมูลการสนับสนุนผลการดำเนินงาน")]
    Guid Id,
    [property: Description("ปริมาณธุรกรรม")]
    int? TransactionVolume,
    [property: Description("คำอธิบายกิจกรรม")]
    string? ActivityDescription,
    [property: Description("ปีข้อมูล")] int? PeriodYear,
    [property: Description("เดือนเริ่มต้น")]
    int? StartMonth,
    [property: Description("เดือนสิ้นสุด")]
    int? EndMonth
);

public record PrincipleApprovalPerfSupportDataDetailResponseDto(
    [property: Description("รหัสรายละเอียด")]
    Guid? Id,
    [property: Description("ลำดับ")] int Sequence,
    [property: Description("คำอธิบายกิจกรรม")]
    string ActivityDescription,
    [property: Description("จำนวนบัญชีปีที่ 1")]
    decimal? AccountCountYear1,
    [property: Description("จำนวนเงินปีที่ 1")]
    decimal? AmountYear1,
    [property: Description("จำนวนบัญชีปีที่ 2")]
    decimal? AccountCountYear2,
    [property: Description("จำนวนเงินปีที่ 2")]
    decimal? AmountYear2
);

public record PrincipleApprovalRoiLoanAndDepositSummaryResponseDto(
    [property: Description("รหัสสรุปผลตอบแทน")]
    Guid? Id,
    [property: Description("ลำดับ")] int Sequence,
    [property: Description("คำอธิบายกิจกรรม")]
    string ActivityDescription,
    [property: Description("จำนวนเงินปีที่ 1")]
    decimal? AmountYear1,
    [property: Description("จำนวนเงินปีที่ 2")]
    decimal? AmountYear2,
    [property: Description("จำนวนเงินปีที่ 3")]
    decimal? AmountYear3
);

public record PrincipleApprovalRoiPerfResultResponseDto(
    [property: Description("รหัสผลการดำเนินงาน")]
    Guid Id,
    [property: Description("ลำดับ")] int Sequence,
    [property: Description("กลุ่มผลการดำเนินงาน")]
    PerformanceResultGroup PerformanceResultGroup,
    [property: Description("ปี")] int Year,
    [property: Description("ผลจริงจากบัญชี")]
    decimal AccountActual,
    [property: Description("อัตราการเติบโตจากบัญชี")]
    decimal AccountGrowth,
    [property: Description("เป้าหมายจำนวนเงิน")]
    decimal AmountTarget,
    [property: Description("ผลจริงจำนวนเงิน")]
    decimal AmountActual,
    [property: Description("อัตราจำนวนเงิน")]
    decimal AmountRate,
    [property: Description("อัตราการเติบโตจำนวนเงิน")]
    decimal AmountGrowth
);

public record PrincipleApprovalBudgetDetail(
    [property: Description("รหัสรายละเอียดงบประมาณ")]
    Guid Id,
    [property: Description("ลำดับ")] int Sequence,
    [property: Description("หน่วยงาน")] string Department,
    [property: Description("ประเภทงบประมาณ")]
    string BudgetType,
    [property: Description("รหัสโครงการ")] string? ProjectCode,
    [property: Description("เลขที่บัญชี")] string AccountNo,
    [property: Description("งบประมาณ")] decimal Budget
);

public record PrincipleApprovalBudgetDto(
    [property: Description("รหัสงบประมาณ")]
    Guid Id,
    [property: Description("ลำดับ")] int Sequence,
    [property: Description("คำอธิบาย")] string Description,
    [property: Description("จำนวนเงินงบประมาณ")]
    decimal BudgetAmount,
    [property: Description("รายละเอียดงบประมาณ")]
    IEnumerable<PrincipleApprovalBudgetDetail> Details
);

public record PrincipleApprovalRentalAnalysisDto(
    [property: Description("รหัสการวิเคราะห์การเช่า")]
    Guid? Id,
    [property: Description("ลำดับ")] int Sequence,
    [property: Description("ประเภทการวิเคราะห์")]
    RentalAnalysisType Type,
    [property: Description("คำอธิบาย")] string Description,
    [property: Description("รายละเอียดการวิเคราะห์")]
    IEnumerable<PrincipleApprovalRentalAnalysisDetail> Details
);

public record PrincipleApprovalRentalAnalysisDetail(
    [property: Description("รหัสรายละเอียดการวิเคราะห์")]
    Guid? Id,
    [property: Description("ปี")] int Year,
    [property: Description("จำนวนเงิน")] decimal Amount
);