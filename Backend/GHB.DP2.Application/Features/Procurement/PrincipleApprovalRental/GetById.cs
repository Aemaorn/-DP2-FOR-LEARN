namespace GHB.DP2.Application.Features.Procurement.PrincipleApprovalRental;

using System.ComponentModel;
using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Features.Procurement.Invite.Abstract;
using GHB.DP2.Application.Features.Procurement.PrincipleApprovalRental.Abstact;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PPrincipleApproval;
using GHB.DP2.Domain.Procurement.PPrincipleApprovalRental;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetByIdPrincipleApprovalRentalRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid ProcurementId,
    Guid? Id);

public class GetByIdPrincipleApprovalRentalEndpoint : PrincipleApprovalRentalEndpointBase<GetByIdPrincipleApprovalRentalRequest, Results<Ok<PrincipleApprovalRentalResponseDto>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public GetByIdPrincipleApprovalRentalEndpoint(
        ILogger<GetByIdPrincipleApprovalRentalEndpoint> logger,
        Dp2DbContext dbContext,
        IFileServiceClient fileServiceClient)
        : base(logger, dbContext, fileServiceClient)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Get("procurement/{procurementId:guid}/principle-approval-rental/{id:guid?}");
        this.Description(b => b
                              .WithTags("Procurement/PrincipleApprovalRental")
                              .WithName("GetPrincipleApprovalRentalById")
                              .Produces<PrincipleApprovalRentalResponseDto>(StatusCodes.Status200OK)
                              .Produces<string>(StatusCodes.Status404NotFound));
    }

    protected override async ValueTask<Results<Ok<PrincipleApprovalRentalResponseDto>, NotFound<string>>> HandleRequestAsync(GetByIdPrincipleApprovalRentalRequest req, CancellationToken ct)
    {
        var principleApproval = await this.dbContext.PPrincipleApprovals
                                          .Where(w => w.ProcurementId == ProcurementId.From(req.ProcurementId))
                                          .SelectMany(s => s.PrincipleApprovalCommittees)
                                          .Where(w => w.GroupType == CommitteeGroupType.RentCommittee)
                                          .ToListAsync(ct);

        if (!principleApproval.Any())
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลคณะกรรมการจัดเช่าที่มีอำนาจในการจัดการขออนุมัติเช่า");
        }

        var approvalRental = await this.dbContext.PPrincipleApprovalRentals
                                       .Include(p => p.Acceptors)
                                       .ThenInclude(pPrincipleApprovalRentalAcceptor => pPrincipleApprovalRentalAcceptor.CommitteePosition)
                                       .Include(pPrincipleApproval => pPrincipleApproval.Budgets)
                                       .ThenInclude(pPrincipleApprovalBudget => pPrincipleApprovalBudget.PrincipleApprovalRentalBudgetDetails)
                                       .Include(pPrincipleApproval => pPrincipleApproval.RentalAnalyses)
                                       .ThenInclude(pPrincipleApprovalBudget => pPrincipleApprovalBudget.PrincipleApprovalRentalRentalAnalysisDetails)
                                       .Include(pPrincipleApprovalRental => pPrincipleApprovalRental.Entrepreneurs)
                                       .ThenInclude(pPrincipleApprovalRentalEntrepreneurs => pPrincipleApprovalRentalEntrepreneurs.Vendor)
                                       .ThenInclude(i => i.EntrepreneurTypeInfo)
                                       .Include(e => e.Entrepreneurs)
                                       .ThenInclude(s => s.EntrepreneursShareholders)
                                       .Include(pPrincipleApprovalRental => pPrincipleApprovalRental.PerfSupportData)
                                       .Include(pPrincipleApprovalRental => pPrincipleApprovalRental.PerfSupportDataDetails)
                                       .Include(pPrincipleApprovalRental => pPrincipleApprovalRental.RoiLoanAndDepositSummaries)
                                       .Include(pPrincipleApprovalRental => pPrincipleApprovalRental.RoiPerfResults)
                                       .Include(pPrincipleApprovalRental => pPrincipleApprovalRental.Assignees)
                                       .Include(pPrincipleApprovalRental => pPrincipleApprovalRental.ComparingAttachments)
                                       .Include(pPrincipleApprovalRental => pPrincipleApprovalRental.RentTypeCodeInfo)
                                       .Include(pPrincipleApprovalRental => pPrincipleApprovalRental.Procurement)
                                       .ThenInclude(procurement => procurement.Department)
                                       .Include(pPrincipleApprovalRental => pPrincipleApprovalRental.Procurement)
                                       .ThenInclude(procurement => procurement.SupplyMethod)
                                       .Include(pPrincipleApprovalRental => pPrincipleApprovalRental.Procurement)
                                       .ThenInclude(procurement => procurement.SupplyMethodType)
                                       .Include(pPrincipleApprovalRental => pPrincipleApprovalRental.Procurement)
                                       .ThenInclude(procurement => procurement.SupplyMethodSpecialType)
                                       .Include(pPrincipleApprovalRental => pPrincipleApprovalRental.Procurement)
                                       .ThenInclude(procurement => procurement.Plan)
                                       .AsSplitQuery()
                                       .SingleOrDefaultAsync(x => x.Id == PPrincipleApprovalRentalId.From(req.Id.Value), cancellationToken: ct);

        if (approvalRental is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลอนุมัติเช่า ที่มีรหัส {req.Id}");
        }

        var response = this.MapToResponse(approvalRental, principleApproval.Any(w => w.SuUserId == UserId.From(req.UserId)));

        return TypedResults.Ok(response);
    }
}

public record PrincipleApprovalRentalDocumentVersionResponse(
    [property: Description("รหัสไฟล์")] Guid FileId,
    [property: Description("เวอร์ชัน")] string Version,
    [property: Description("วันที่สร้าง")] DateTimeOffset CreatedAt,
    [property: Description("ชื่อผู้สร้าง")] string CreatedByName,
    [property: Description("เป็นเวอร์ชันปัจจุบัน")] bool IsCurrent);

public record PrincipleApprovalRentalResponseDto(
    [property: Description("รหัสขออนุมัติหลักการเช่า")]
    Guid? Id,
    [property: Description("รหัสการจัดซื้อจัดจ้าง")]
    Guid? ProcurementId,
    [property: Description("รหัสเอกสารเช่า")]
    Guid? DocumentId,
    [property: Description("เอกสารเช่า เปลี่ยนแปลง")]
    bool? IsDocumentReplace,
    [property: Description("เอกสารเช่า เปลี่ยนแปลง")]
    bool? IsDocumentIdReplaced,
    [property: Description("ประวัติเวอร์ชันเอกสารเช่า")]
    PrincipleApprovalRentalDocumentVersionResponse[]? DocumentVersions,
    [property: Description("รหัสเอกสารผู้ชนะ")]
    Guid? WinnerDocumentId,
    [property: Description("เอกสารผู้ชนะ เปลี่ยนแปลง")]
    bool? IsWinnerDocumentReplace,
    [property: Description("เอกสารผู้ชนะ เปลี่ยนแปลง")]
    bool? IsWinnerDocumentIdReplaced,
    [property: Description("ประวัติเวอร์ชันเอกสารผู้ชนะ")]
    PrincipleApprovalRentalDocumentVersionResponse[]? WinnerDocumentVersions,
    [property: Description("ประเภทการใช้สัญญา")]
    UseContractType UseContract,
    [property: Description("ที่ทำการสาขา")]
    string BranchLocation,
    [property: Description("รหัสแบบขออนุมัติเช่า")]
    string RentTypeCode,
    [property: Description("ชื่อแบบขออนุมัติเช่า")]
    string RentTypeName,
    [property: Description("ตั้งแต่วันที่")]
    DateTimeOffset RentalStartDate,
    [property: Description("ถึงวันที่")]
    DateTimeOffset RentalEndDate,
    [property: Description("ระยะเวลาเช่า (ปี)")]
    int RentalDurationYear,
    [property: Description("ระยะเวลาเช่า (เดือน)")]
    int RentalDurationMonth,
    [property: Description("ระยะเวลาเช่า (วัน)")]
    int RentalDurationDay,
    [property: Description("อัตราค่าเช่าเดือนละไม่เกิน")]
    decimal MaxMonthlyRent,
    [property: Description("รวมเป็นจำนวนเงิน")]
    decimal TotalRentalAmount,
    [property: Description("สัญญาครบกำหนดในวันที่")]
    DateTimeOffset ExpectedContractDate,
    [property: Description("รายละเอียดสถานที่เช่า")]
    string RentalLocationDetails,
    [property: Description("รหัสตำบล")]
    string SubDistrictCode,
    [property: Description("ชื่อตำบล")]
    string SubDistrictName,
    [property: Description("รหัสอำเภอ")]
    string DistrictCode,
    [property: Description("ชื่ออำเภอ")]
    string DistrictName,
    [property: Description("รหัสจังหวัด")]
    string ProvinceCode,
    [property: Description("ชื่อจังหวัด")]
    string ProvinceName,
    [property: Description("ราคาอ้างอิง")] decimal? ReferencePriceAmount,
    [property: Description("มูลค่าปัจจุบันสุทธิ (NPV)")]
    decimal? AnalysisSummaryNpv,
    [property: Description("ระยะเวลาคืนทุน (Payback Period)")]
    decimal? AnalysisSummaryPaybackYearPeriod,
    [property: Description("ระยะเวลาคืนทุนส่วนลด (Discounted Payback Period)")]
    decimal? AnalysisSummaryDiscountedPaybackYearPeriod,
    [property: Description("เบอร์โทรศัพท์")]
    string? PhoneNumber,
    [property: Description("สถานะ")] PPrincipleApprovalRentalStatus Status,
    [property: Description("ผู้อนุมัติ")] IEnumerable<PrincipleApprovalRentalAcceptorResponseDto> Acceptors,
    [property: Description("ผู้รับมอบหมาย")]
    IEnumerable<PrincipleApprovalRentalAssigneeResponseDto> Assignees,
    [property: Description("ข้อมูลการสนับสนุนผลการดำเนินงาน")]
    PrincipleApprovalRentalPerfSupportDataResponseDto? PerfSupportData,
    [property: Description("รายละเอียดการสนับสนุนผลการดำเนินงาน")]
    IEnumerable<PrincipleApprovalRentalPerfSupportDataDetailResponseDto> PerfSupportDataDetails,
    [property: Description("ข้อมูลสรุปผลตอบแทนจากสินเชื่อและเงินฝาก")]
    IEnumerable<PrincipleApprovalRentalRoiLoanAndDepositSummaryResponseDto>? RoiLoanAndDepositSummaries,
    [property: Description("ผลการดำเนินงานตามผลตอบแทน")]
    IEnumerable<PrincipleApprovalRentalRoiPerfResultResponseDto>? RoiPerfResults,
    [property: Description("ข้อมูลงบประมาณ")]
    IEnumerable<PrincipleApprovalRentalBudgetDto>? Budgets,
    [property: Description("การวิเคราะห์การเช่า")]
    IEnumerable<PrincipleApprovalRentalRentalAnalysisDto>? RentalAnalyses,
    [property: Description("ข้อมูลผู้ประกอบการ")]
    IEnumerable<PrincipleApprovalRentalEntrepreneursResponseDto> Entrepreneurs,
    [property: Description("ไฟล์แนบเปรียบเทียบ")]
    IEnumerable<ComparingAttachmentsDto> ComparingAttachments,
    [property: Description("มีสิทธิ์แก้ไข")]
    bool HasPermission,
    [property: Description("วันที่เอกสาร")]
    DateTimeOffset? DocumentDate = null);

public record PrincipleApprovalRentalAcceptorResponseDto(
    [property: Description("รหัสผู้อนุมัติ")]
    Guid Id,
    [property: Description("ประเภทผู้อนุมัติ")]
    AcceptorType AcceptorType,
    [property: Description("รหัสผู้ใช้")] Guid UserId,
    [property: Description("รหัสพนักงาน")] string EmployeeCode,
    [property: Description("ชื่อเต็ม")] string FullName,
    [property: Description("ชื่อตำแหน่ง")] string PositionName,
    [property: Description("ชื่อหน่วยงาน")]
    string DepartmentName,
    [property: Description("ลำดับ")] int Sequence,
    [property: Description("สถานะการอนุมัติ")]
    AcceptorStatus Status,
    [property: Description("หมายเหตุ")] string? Remark,
    [property: Description("วันที่ดำเนินการ")]
    DateTimeOffset? ActionAt = default,
    [property: Description("รหัสตำแหน่งกรรมการ")]
    string? CommitteePositionsCode = default,
    [property: Description("ชื่อตำแหน่งกรรมการ")]
    string? CommitteePositionName = default,
    [property: Description("ไม่สามารถปฏิบัติหน้าที่ได้")]
    bool? IsUnableToPerformDuties = default,
    [property: Description("เป็นผู้อนุมัติปัจจุบัน")]
    bool IsCurrent = false,
    [property: Description("รหัสผู้ใช้งานผู้ปฏิบัติหน้าที่แทน")]
    Guid? DelegateeUserId = default);

public record PrincipleApprovalRentalAssigneeResponseDto(
    [property: Description("รหัสผู้รับมอบหมาย")]
    Guid Id,
    [property: Description("กลุ่มผู้รับมอบหมาย")]
    AssigneeGroup AssigneeGroup,
    [property: Description("ประเภทผู้รับมอบหมาย")]
    AssigneeType AssigneeType,
    [property: Description("รหัสผู้ใช้")] Guid UserId,
    [property: Description("รหัสพนักงาน")] string EmployeeCode,
    [property: Description("ชื่อเต็ม")] string FullName,
    [property: Description("ชื่อตำแหน่ง")] string PositionName,
    [property: Description("ชื่อหน่วยงาน")]
    string DepartmentName,
    [property: Description("หมายเหตุ")] string? Remark,
    [property: Description("ลำดับ")] int Sequence,
    [property: Description("วันที่ดำเนินการ")]
    DateTimeOffset? ActionAt = default,
    [property: Description("รหัสผู้ใช้งานผู้ปฏิบัติหน้าที่แทน")]
    Guid? DelegateeUserId = default
);

public record PrincipleApprovalRentalPerfSupportDataResponseDto(
    [property: Description("รหัสข้อมูลการสนับสนุนผลการดำเนินงาน")]
    Guid Id,
    [property: Description("ปริมาณธุรกรรม")]
    int? TransactionVolume,
    [property: Description("รายละเอียดกิจกรรม")]
    string? ActivityDescription,
    [property: Description("ปีระยะเวลา")] int? PeriodYear,
    [property: Description("เดือนเริ่มต้น")]
    int? StartMonth,
    [property: Description("เดือนสิ้นสุด")]
    int? EndMonth
);

public record PrincipleApprovalRentalPerfSupportDataDetailResponseDto(
    [property: Description("รหัสรายละเอียดข้อมูลการสนับสนุน")]
    Guid? Id,
    [property: Description("ลำดับ")] int Sequence,
    [property: Description("รายละเอียดกิจกรรม")]
    string ActivityDescription,
    [property: Description("จำนวนบัญชีปี 1")]
    decimal? AccountCountYear1,
    [property: Description("จำนวนเงินปี 1")]
    decimal? AmountYear1,
    [property: Description("จำนวนบัญชีปี 2")]
    decimal? AccountCountYear2,
    [property: Description("จำนวนเงินปี 2")]
    decimal? AmountYear2
);

public record PrincipleApprovalRentalRoiLoanAndDepositSummaryResponseDto(
    [property: Description("รหัสข้อมูลสรุปผลตอบแทน")]
    Guid? Id,
    [property: Description("ลำดับ")] int Sequence,
    [property: Description("รายละเอียดกิจกรรม")]
    string ActivityDescription,
    [property: Description("จำนวนเงินปี 1")]
    decimal? AmountYear1,
    [property: Description("จำนวนเงินปี 2")]
    decimal? AmountYear2,
    [property: Description("จำนวนเงินปี 3")]
    decimal? AmountYear3
);

public record PrincipleApprovalRentalRoiPerfResultResponseDto(
    [property: Description("รหัสผลการดำเนินงาน")]
    Guid Id,
    [property: Description("ลำดับ")] int Sequence,
    [property: Description("กลุ่มผลการดำเนินงาน")]
    PerformanceResultGroup PerformanceResultGroup,
    [property: Description("ปี")] int Year,
    [property: Description("จำนวนบัญชีจริง")]
    decimal AccountActual,
    [property: Description("จำนวนบัญชีเติบโต")]
    decimal AccountGrowth,
    [property: Description("เป้าหมายจำนวนเงิน")]
    decimal AmountTarget,
    [property: Description("จำนวนเงินจริง")]
    decimal AmountActual,
    [property: Description("อัตราจำนวนเงิน")]
    decimal AmountRate,
    [property: Description("จำนวนเงินเติบโต")]
    decimal AmountGrowth
);

public record PrincipleApprovalRentalBudgetDetail(
    [property: Description("รหัสรายละเอียดงบประมาณ")]
    Guid Id,
    [property: Description("ลำดับ")] int Sequence,
    [property: Description("หน่วยงาน")] string Department,
    [property: Description("ประเภทงบประมาณ")]
    string BudgetType,
    [property: Description("รหัสโครงการ")] string? ProjectCode,
    [property: Description("หมายเลขบัญชี")]
    string AccountNo,
    [property: Description("งบประมาณ")] decimal Budget
);

public record PrincipleApprovalRentalBudgetDto(
    [property: Description("รหัสงบประมาณ")]
    Guid Id,
    [property: Description("ลำดับ")] int Sequence,
    [property: Description("คำอธิบาย")] string Description,
    [property: Description("จำนวนงบประมาณ")]
    decimal BudgetAmount,
    [property: Description("รายละเอียดงบประมาณ")]
    IEnumerable<PrincipleApprovalRentalBudgetDetail> Details
);

public record PrincipleApprovalRentalRentalAnalysisDto(
    [property: Description("รหัสการวิเคราะห์การเช่า")]
    Guid Id,
    [property: Description("ลำดับ")] int Sequence,
    [property: Description("ประเภทการวิเคราะห์")]
    RentalAnalysisType Type,
    [property: Description("คำอธิบาย")] string Description,
    [property: Description("รายละเอียดการวิเคราะห์")]
    IEnumerable<PrincipleApprovalRentalRentalAnalysisDetail> Details
);

public record PrincipleApprovalRentalRentalAnalysisDetail(
    [property: Description("รหัสรายละเอียดการวิเคราะห์")]
    Guid Id,
    [property: Description("ปี")] int Year,
    [property: Description("จำนวนเงิน")] decimal Amount
);

public record PrincipleApprovalRentalEntrepreneursResponseDto(
    [property: Description("รหัสผู้ประกอบการ")]
    Guid? Id,
    [property: Description("รหัสผู้ขาย")] Guid VendorId,
    [property: Description("ลำดับ")] int Sequence,
    [property: Description("ส่งอีเมล")] bool EmailSend,
    [property: Description("ผลการตรวจสอบ Watchlist")]
    bool WatchlistResult,
    [property: Description("หมายเหตุผลการตรวจสอบ Watchlist")]
    string? WatchlistResultRemark,
    [property: Description("วันที่ตรวจสอบ Watchlist")]
    DateTimeOffset? WatchlistResultAt,
    [property: Description("ผลการตรวจสอบ COI")]
    bool CoiResult,
    [property: Description("หมายเหตุผลการตรวจสอบ COI")]
    string? CoiResultRemark,
    [property: Description("วันที่ตรวจสอบ COI")]
    DateTimeOffset? CoiResultAt,
    [property: Description("ผลการตรวจสอบ EGP")]
    bool EgpResult,
    [property: Description("หมายเหตุผลการตรวจสอบ EGP")]
    string? EgpResultRemark,
    [property: Description("วันที่ตรวจสอบ EGP")]
    DateTimeOffset? EgpResultAt,
    [property: Description("เลขประจำตัวผู้เสียภาษี")]
    string EntrepreneurTaxId,
    [property: Description("รหัสประเภทผู้ประกอบการ")]
    string EntrepreneurType,
    [property: Description("ประเภทผู้ประกอบการ")]
    string EntrepreneurTypeLabel,
    [property: Description("ชื่อผู้ประกอบการ")]
    string EntrepreneurName,
    [property: Description("อีเมลผู้ประกอบการ")]
    string EntrepreneurEmail,
    Dto.EntrepreneursPriceDetailDto[] Details,
    PrincipleApprovalRentalEntrepreneursShareholderDto[] Shareholders,
    [property: Description("ผลการตรวจสอบความขัดแย้งทางผลประโยชน์")]
    QualificationResultDto? CoiCheckerResult,
    [property: Description("ผลการตรวจสอบ Watchlist")]
    QualificationResultDto? WatchlistCheckerResult,
    [property: Description("รหัสสาขา SAP")]
    string? SapBranchNumber = null
);

public record PrincipleApprovalRentalEntrepreneursShareholderDto(
    [property: Description("รหัสผู้ถือหุ้น")]
    Guid Id,
    [property: Description("ลำดับ")] int Sequence,
    [property: Description("เลขที่ผู้เสียภาษี")]
    string TaxId,
    [property: Description("ชื่อจริง")] string FirstName,
    [property: Description("นามสกุล")] string LastName,
    [property: Description("เป็นกรรมการหรือถือหุ้น 20%")]
    bool? IsDirector,
    [property: Description("เป็นผู้ถือหุ้น")]
    bool? IsShareholder,
    [property: Description("เป็นนิติบุคคล")]
    bool? IsJuristic,
    [property: Description("ประเภทการตรวจสอบ")]
    string? CheckType,
    [property: Description("ผลการตรวจสอบ Watchlist")]
    bool? WatchlistResult,
    [property: Description("หมายเหตุผลการตรวจสอบ Watchlist")]
    string? WatchlistResultRemark,
    [property: Description("วันที่ตรวจสอบ Watchlist")]
    DateTimeOffset? WatchlistResultAt,
    [property: Description("ผลการตรวจสอบความขัดแย้งทางผลประโยชน์")]
    bool? CoiResult,
    [property: Description("หมายเหตุผลการตรวจสอบความขัดแย้งทางผลประโยชน์")]
    string? CoiResultRemark,
    [property: Description("วันที่ตรวจสอบความขัดแย้งทางผลประโยชน์")]
    DateTimeOffset? CoiResultAt,
    [property: Description("ผลการตรวจสอบ eGP")]
    bool? EgpResult,
    [property: Description("หมายเหตุ eGP")]
    string? EgpRemark,
    [property: Description("วันที่ตรวจสอบ eGP")]
    DateTimeOffset? EgpResultAt,
    [property: Description("ผลการตรวจสอบความขัดแย้งทางผลประโยชน์")]
    QualificationResultDto? CoiCheckerResult,
    [property: Description("ผลการตรวจสอบ Watchlist")]
    QualificationResultDto? WatchlistCheckerResult
);

public record PrincipleApprovalRentalComparingAttachmentsDto(
    [property: Description("รหัสไฟล์แนบ")]
    Guid Id,
    [property: Description("ชื่อไฟล์")]
    string FileName,
    [property: Description("ลำดับ")]
    int Sequence,
    [property: Description("เป็นไฟล์สาธารณะ")]
    bool IsPublic,
    [property: Description("ผู้สร้าง")]
    string CreatedBy
);