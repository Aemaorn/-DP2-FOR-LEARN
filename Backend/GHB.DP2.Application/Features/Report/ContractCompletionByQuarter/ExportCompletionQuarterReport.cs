namespace GHB.DP2.Application.Features.Report.ContractCompletionByQuarter;

using Codehard.Common.Extensions;
using GHB.DP2.Application.Common;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.Report.RpContractCompletionByQuarter;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

public record ExportContractDraftVendorListRequest(Guid Id);

public class ExcelStyleConfig
{
    public uint Header { get; } = 2u;

    public uint Normal { get; } = 3u;

    public uint Number2 { get; } = 5u;

    public uint Date { get; } = 6u;

    public uint Center { get; } = 7u;

    public uint HeaderBoldWrapLightBlue { get; } = 8u;

    public uint HeaderBoldLeft { get; } = 9u;
}

public sealed record ComplementQuarterExportDto(
    string ContractDraftNumber,
    string DepartmentName,
    string ContractSignedDate,
    string ContractTypeCode,
    string ContractTypeName,
    string ContractName,
    string ContactPeriod,
    string VendorName,
    decimal Budget,
    string ContractStartDate,
    string ContractEndDate,
    string SupplyMethodSpecialName);

public class ExportCompletionQuarterReportEndpoint : Endpoint<ExportContractDraftVendorListRequest>
{
    private readonly Dp2DbContext dbContext;
    private readonly string dateFormat = "d MMM yy";
    private readonly string emptyCell = "-";

    public ExportCompletionQuarterReportEndpoint(Dp2DbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(builder =>
            builder.WithTags("Report/ContractCompletionByQuarter")
                   .ProducesProblem(StatusCodes.Status404NotFound)
                   .ProducesProblem(StatusCodes.Status500InternalServerError));
        this.Get("report/contract-completion-by-quarter/{Id:guid}/export");
    }

    public override async Task HandleAsync(ExportContractDraftVendorListRequest req, CancellationToken ct)
    {
        var contractCompletionByQuarter = await this.GetContractCompletionByQuarterAsync(req.Id, ct);
        if (contractCompletionByQuarter is null)
        {
            await this.SendNotFoundResponseAsync(ct);
            return;
        }

        var quarterData = await this.GetQuarterDataAsync(contractCompletionByQuarter, ct);
        var excelBytes = this.GenerateExcelReport(contractCompletionByQuarter, quarterData);

        await this.SendExcelFileAsync(excelBytes, ct);
    }

    private async Task<RpContractCompletionByQuarter?> GetContractCompletionByQuarterAsync(Guid id, CancellationToken ct)
    {
        return await this.dbContext.RpContractCompletionByQuarters
                         .Include(cq => cq.Details)
                         .ThenInclude(d => d.CaContractDraftVendor)
                         .FirstOrDefaultAsync(cq => cq.Id == RpContractCompletionByQuarterId.From(id), ct);
    }

    private async Task SendNotFoundResponseAsync(CancellationToken ct)
    {
        this.HttpContext.Response.StatusCode = StatusCodes.Status404NotFound;
        await this.HttpContext.Response
                  .WriteAsJsonAsync(
                      new { Message = "ไม่พบข้อมูลรายงานสัญญาแล้วเสร็จตามไตรมาสที่เลือก" },
                      cancellationToken: ct);
    }

    private async Task SendExcelFileAsync(byte[] excelBytes, CancellationToken ct)
    {
        var fileName = $"completion_quarter_report_{DateTimeOffset.Now:yyyyMMddHHmmss}.xlsx";
        await this.SendBytesAsync(
            excelBytes,
            fileName,
            contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            cancellation: ct);
    }

    private byte[] GenerateExcelReport(
        RpContractCompletionByQuarter contractCompletionByQuarter,
        ComplementQuarterExportDto[] quarterData)
    {
        using var stream = new MemoryStream();
        using var excelDocument = ExportExcel.Create(stream);

        var colWidths = new[] { 7d, 20d, 25d, 15d, 15d, 30d, 20d, 25d, 15d, 20d, 20d, 15d, };
        var cellStyle = new ExcelStyleConfig();

        this.AddAllSheets(excelDocument, contractCompletionByQuarter, quarterData, colWidths, cellStyle);
        excelDocument.Finish();

        return stream.ToArray();
    }

    private void AddAllSheets(
        ExportExcel excelDocument,
        RpContractCompletionByQuarter contractCompletionByQuarter,
        ComplementQuarterExportDto[] quarterData,
        double[] colWidths,
        ExcelStyleConfig cellStyle)
    {
        this.AddSheet(excelDocument, contractCompletionByQuarter, quarterData, colWidths, cellStyle, null);
        this.AddSheet(excelDocument, contractCompletionByQuarter, quarterData, colWidths, cellStyle, ContractTypeConstant.Buy);
        this.AddSheet(excelDocument, contractCompletionByQuarter, quarterData, colWidths, cellStyle, ContractTypeConstant.Hire);
        this.AddSheet(excelDocument, contractCompletionByQuarter, quarterData, colWidths, cellStyle, ContractTypeConstant.Rent);
    }

    private void AddSheet(
        ExportExcel excelDocument,
        RpContractCompletionByQuarter contractCompletionByQuarter,
        ComplementQuarterExportDto[] quarterData,
        double[] colWidths,
        ExcelStyleConfig cellStyle,
        string? contractTypeCode)
    {
        var sheetNameLabel = GetSheetNameLabel(
            contractCompletionByQuarter.Quarter,
            contractCompletionByQuarter.Year,
            contractTypeCode);

        var sheetHeaderLabel = GetSheetHeaderLabel(
            contractCompletionByQuarter.Quarter,
            contractCompletionByQuarter.Year,
            contractTypeCode);

        AddSheetHeader(excelDocument, sheetNameLabel, sheetHeaderLabel, colWidths, cellStyle);

        var filteredData = FilterDataByContractType(quarterData, contractTypeCode);
        this.AddRowData(excelDocument, filteredData, cellStyle);
    }

    private static void AddSheetHeader(
        ExportExcel excelDocument,
        string sheetNameLabel,
        string sheetHeaderLabel,
        double[] colWidths,
        ExcelStyleConfig cellStyle)
    {
        excelDocument
            .AddSheet(sheetNameLabel, colWidths, 2)
            .RowStyledWithHeight(
                35,
                (sheetHeaderLabel, cellStyle.HeaderBoldLeft))
            .Merges("A1:L1")
            .RowStyledWithHeight(
                60,
                ("ลำดับที่", cellStyle.HeaderBoldWrapLightBlue),
                ("เลขที่สัญญา", cellStyle.HeaderBoldWrapLightBlue),
                ("ฝ่ายงาน-ผู้รับผิดชอบ", cellStyle.HeaderBoldWrapLightBlue),
                ("วันทำสัญญา", cellStyle.HeaderBoldWrapLightBlue),
                ("ประเภทสัญญา", cellStyle.HeaderBoldWrapLightBlue),
                ("รายการสัญญา", cellStyle.HeaderBoldWrapLightBlue),
                ("ระยะเวลา (ปี)", cellStyle.HeaderBoldWrapLightBlue),
                ("ผู้ขาย/ผู้รับจ้าง", cellStyle.HeaderBoldWrapLightBlue),
                ("ค่าพัสดุ", cellStyle.HeaderBoldWrapLightBlue),
                ($"วันที่เริมสัญญา/\nเริ่มรับประกันฯ", cellStyle.HeaderBoldWrapLightBlue),
                ($"วันที่ครบกำหนดสัญญา/\nครบกำหนดประกันฯ", cellStyle.HeaderBoldWrapLightBlue),
                ("วิธีการ", cellStyle.HeaderBoldWrapLightBlue));
    }

    private static ComplementQuarterExportDto[] FilterDataByContractType(
        ComplementQuarterExportDto[] quarterData,
        string? contractTypeCode)
    {
        if (contractTypeCode == ContractTypeConstant.Rent)
        {
            return [.. quarterData
                .Where(d =>
                    d.ContractTypeCode == ContractTypeConstant.Rent ||
                    d.ContractTypeCode == ContractRentalTypeConstant.Rent)];
        }

        return [.. quarterData
            .WhereIf(
                contractTypeCode is not null,
                d => d.ContractTypeCode == contractTypeCode)];
    }

    private void AddRowData(ExportExcel excelDocument, ComplementQuarterExportDto[] rowData, ExcelStyleConfig cellStyle)
    {
        if (!rowData.Any())
        {
            AddEmptyDataRow(excelDocument, cellStyle);
            return;
        }

        var rowSeq = 1;
        foreach (var r in rowData)
        {
            this.AddDataRow(excelDocument, r, rowSeq++, cellStyle);
        }
    }

    private static void AddEmptyDataRow(ExportExcel excelDocument, ExcelStyleConfig cellStyle)
    {
        excelDocument.RowStyledWithHeight(30, ("ไม่พบข้อมูล", cellStyle.Center))
                     .Merges("A3:L3");
    }

    private void AddDataRow(ExportExcel excelDocument, ComplementQuarterExportDto r, int rowSeq, ExcelStyleConfig cellStyle)
    {
        excelDocument.RowStyledWithHeight(
            30,
            (rowSeq, cellStyle.Center),
            (r.ContractDraftNumber, cellStyle.Center),
            (r.DepartmentName, cellStyle.Normal),
            (string.IsNullOrWhiteSpace(r.ContractSignedDate) ? this.emptyCell : r.ContractSignedDate, cellStyle.Date),
            (r.ContractTypeName, cellStyle.Normal),
            (r.ContractName, cellStyle.Normal),
            (r.ContactPeriod, cellStyle.Center),
            (r.VendorName, cellStyle.Normal),
            (r.Budget, cellStyle.Number2),
            (string.IsNullOrWhiteSpace(r.ContractStartDate) ? this.emptyCell : r.ContractStartDate, cellStyle.Date),
            (string.IsNullOrWhiteSpace(r.ContractEndDate) ? this.emptyCell : r.ContractEndDate, cellStyle.Date),
            (string.IsNullOrWhiteSpace(r.SupplyMethodSpecialName) ? this.emptyCell : r.SupplyMethodSpecialName, cellStyle.Center));
    }

    private static string GetSheetNameLabel(int quarter, int year, string? contractTypeCode)
    {
        var quarterLabel = "Q" + quarter + "-" + year.ToString().Substring(2, 2);

        var supplyMethodTypeLabel =
            contractTypeCode is null
                ? "สัญญาทั้งหมด"
                : contractTypeCode switch
                {
                    ContractTypeConstant.Buy => "ซื้อขาย",
                    ContractTypeConstant.Hire => "จ้าง",
                    ContractTypeConstant.Rent => "เช่า",
                    _ => string.Empty,
                };

        return $"{supplyMethodTypeLabel} {quarterLabel}";
    }

    private static string GenerateQuarterLabel(int quarter, int thaiYear)
    {
        return quarter switch
        {
            1 => $"ไตรมาส 1/{thaiYear} (1 มกราคม {thaiYear} - 31 มีนาคม {thaiYear})",
            2 => $"ไตรมาส 2/{thaiYear} (1 เมษายน {thaiYear} - 30 มิถุนายน {thaiYear})",
            3 => $"ไตรมาส 3/{thaiYear} (1 กรกฎาคม {thaiYear} - 30 กันยายน {thaiYear})",
            4 => $"ไตรมาส 4/{thaiYear} (1 ตุลาคม {thaiYear} - 31 ธันวาคม {thaiYear})",
            _ => string.Empty,
        };
    }

    private static string GetSheetHeaderLabel(int quarter, int year, string? contractTypeCode)
    {
        var quarterLabel = GenerateQuarterLabel(quarter, year);

        var supplyMethodTypeLabel =
            contractTypeCode is null
                ? string.Empty
                : contractTypeCode switch
                {
                    ContractTypeConstant.Buy => "(ประเภทสัญญาซื้อขาย)",
                    ContractTypeConstant.Hire => "(ประเภทสัญญาจ้าง)",
                    ContractTypeConstant.Rent => "(ประเภทสัญญาเช่า)",
                    _ => string.Empty,
                };

        return $"รายงานสัญญาแล้วเสร็จทั้งหมด {supplyMethodTypeLabel} {quarterLabel}";
    }

    private async Task<ComplementQuarterExportDto[]> GetQuarterDataAsync(
        RpContractCompletionByQuarter contractCompletionByQuarter,
        CancellationToken ct)
    {
        var contractDraftVendorIds = contractCompletionByQuarter
            .Details
            .Select(d => d.CaContractDraftVendor.Id)
            .ToArray();

        var contractDraftVendors = await this.GetContractDraftVendorsAsync(contractDraftVendorIds, ct);
        var result = contractDraftVendors
            .Select(this.MapToComplementQuarterExportDto)
            .OrderBy(r => r.ContractDraftNumber)
            .ThenBy(r => r.VendorName)
            .ToArray();

        return result;
    }

    private async Task<List<CaContractDraftVendor>> GetContractDraftVendorsAsync(
        ContractDraftVendorId[] contractDraftVendorIds,
        CancellationToken ct)
    {
        return await this.dbContext.CaContractDraftVendors
                      .Include(dv => dv.ContractType)
                      .Include(dv => dv.Vendor)
                      .ThenInclude(v => v.VendorInfo)
                      .Include(dv => dv.ContractDraft)
                      .ThenInclude(cd => cd.Procurement)
                      .ThenInclude(p => p.Department)
                      .Include(cv => cv.ContractDraft)
                      .ThenInclude(cd => cd.Procurement)
                      .ThenInclude(p => p.SupplyMethodSpecialType)
                      .Include(dv => dv.DraftTermsConditions)
                      .ThenInclude(tc => tc.Warranty)
                      .ThenInclude(w => w.WarrantyPeriod)
                      .AsNoTracking()
                      .Where(dv => contractDraftVendorIds.Contains(dv.Id))
                      .ToListAsync(ct);
    }

    private ComplementQuarterExportDto MapToComplementQuarterExportDto(CaContractDraftVendor v)
    {
        var (startDate, endDate) = this.CalculateContractDates(v);
        var contractPeriod = this.CalculateContractPeriod(v, startDate, endDate);

        return new ComplementQuarterExportDto(
            v.ContractNumber,
            v.ContractDraft.Procurement.Department.Name,
            v.ContractSignedDate.ToThaiDateString(format: this.dateFormat),
            v.ContractType?.Code.Value ?? string.Empty,
            v.ContractType?.Label ?? this.emptyCell,
            v.ContractName,
            contractPeriod,
            v.Vendor.VendorInfo.EstablishmentName,
            v.Budget,
            startDate.ToThaiDateString(format: this.dateFormat),
            endDate.ToThaiDateString(format: this.dateFormat),
            v.ContractDraft.Procurement.SupplyMethodSpecialType?.Label ?? string.Empty);
    }

    private (DateTime? StartDate, DateTime? EndDate) CalculateContractDates(CaContractDraftVendor v)
    {
        var contractStartDate = v.StartDate?.DateTime;
        var contractEndDate = v.EndDate?.DateTime;

        if (v.DraftTermsConditions.Warranty.HasWarranty ?? false)
        {
            contractStartDate = contractEndDate?.AddDays(1) ?? contractStartDate;
            contractEndDate = CalculateWarrantyEndDate(v, contractEndDate);
        }

        return (contractStartDate, contractEndDate);
    }

    private static DateTime? CalculateWarrantyEndDate(CaContractDraftVendor v, DateTime? contractEndDate)
    {
        var warrantyPeriod = v.DraftTermsConditions.Warranty.WarrantyPeriod;
        if (warrantyPeriod is null)
        {
            return contractEndDate;
        }

        var warrantyDuration = new
        {
            Years = warrantyPeriod.Year,
            Months = warrantyPeriod.Month,
            Days = warrantyPeriod.Day,
        };

        return contractEndDate?
            .AddYears((int)warrantyDuration.Years)
            .AddMonths((int)warrantyDuration.Months)
            .AddDays((double)warrantyDuration.Days);
    }

    private string CalculateContractPeriod(CaContractDraftVendor v, DateTimeOffset? contractStartDate, DateTimeOffset? contractEndDate)
    {
        var contractPeriod = contractStartDate.HasValue && contractEndDate.HasValue
            ? contractStartDate.Value.ToThaiDateDiffLabel(contractEndDate.Value)
            : this.emptyCell;

        if (HasWarrantyPeriod(v))
        {
            var warrantyText = GenerateWarrantyText(v);
            contractPeriod = $"{contractPeriod} \n(รับประกัน {warrantyText})";
        }

        return contractPeriod;
    }

    private static bool HasWarrantyPeriod(CaContractDraftVendor v)
    {
        var warrantyPeriod = v.DraftTermsConditions.Warranty.WarrantyPeriod;
        return (warrantyPeriod?.Year ?? 0) > 0 ||
               (warrantyPeriod?.Month ?? 0) > 0 ||
               (warrantyPeriod?.Day ?? 0) > 0;
    }

    private static string GenerateWarrantyText(CaContractDraftVendor v)
    {
        var warrantyPeriod = v.DraftTermsConditions.Warranty.WarrantyPeriod;
        var warrantyDetails = new[]
        {
            warrantyPeriod?.Year > 0 ? $"{warrantyPeriod.Year} ปี " : string.Empty,
            warrantyPeriod?.Month > 0 ? $"{warrantyPeriod.Month} เดือน " : string.Empty,
            warrantyPeriod?.Day > 0 ? $"{warrantyPeriod.Day} วัน" : string.Empty,
        };

        return string.Join(string.Empty, warrantyDetails);
    }
}