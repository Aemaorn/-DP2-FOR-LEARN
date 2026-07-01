namespace GHB.DP2.Application.Features.ContractAgreement.ContractDraft;

using GHB.DP2.Application.Common;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.Report.RpAuditAndRevenue;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

public record ExportContractDraftVendorListRequest(
    string? Keyword,
    string? DepartmentCode,
    int? BudgetYear,
    string? SupplyMethodCode,
    string? Columns);

public class ExportVendorListEndpoint : Endpoint<ExportContractDraftVendorListRequest>
{
    private readonly Dp2DbContext dbContext;

    public ExportVendorListEndpoint(Dp2DbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(builder =>
            builder.WithTags(nameof(ContractDraft))
                   .ProducesProblem(StatusCodes.Status404NotFound)
                   .ProducesProblem(StatusCodes.Status500InternalServerError));
        this.Get("contract-draft/export-vendors");
    }

    public override async Task HandleAsync(ExportContractDraftVendorListRequest req, CancellationToken ct)
    {
        using var stream = new MemoryStream();

        var cellStyle = new
        {
            header = 2u,
            headerRed = 15u,
            headerCream = 16u,
            normal = 3u,
            number2 = 5u,
            date = 6u,
            center = 7u,
        };

        var allColWidths = new[]
        {
            6d,  // 0  ลำดับ
            20d, // 1  วันที่รับเรื่องเข้าส่วนบริหารสัญญา
            22d, // 2  เลขที่ใบสั่งซื้อ-สั่งจ้าง (PO)
            16d, // 3  วันที่ (PO)
            20d, // 4  วิธีการจัดซื้อจัดจ้าง
            16d, // 5  ฝ่ายงาน-ผู้รับผิดชอบ
            16d, // 6  ส่วนงาน-ผู้รับผิดชอบ
            28d, // 7  เลขที่หนังสือแจ้งผู้รับจ้างลงนามสัญญา
            20d, // 8  วันที่แจ้งผู้รับจ้างมาลงนาม
            22d, // 9  เลขที่สัญญา จพ.(สบส.)
            16d, // 10 วันที่ทำสัญญา
            20d, // 11 ประเภทสัญญา
            50d, // 12 ชื่อโครงการ (สัญญา)
            18d, // 13 ค่าจ้าง (รวมภาษี)
            30d, // 14 บริษัทคู่ค้า/ผู้รับจ้าง
            24d, // 15 กรรมการบริษัท
            24d, // 16 ผู้รับมอบอำนาจ/ผู้ลงนามสัญญา
            24d, // 17 ผู้มอบอำนาจลงนามสัญญา
            22d, // 18 ประเภทหลักประกันสัญญา
            18d, // 19 วงเงินหลักประกันสัญญา
            22d, // 20 วันที่ส่ง LG ตรวจสอบกับธนาคารผู้ออก LG
            22d, // 21 วันที่ธนาคารตอบกลับ LG
            22d, // 22 วันที่เริ่มหนังสือค้ำหลักประกันสัญญา (LG)
            22d, // 23 วันที่สิ้นสุดหนังสือค้ำหลักประกันสัญญา (LG)
            28d, // 24 ระยะเวลาแล้วเสร็จ/ส่งมอบงาน/ระยะเวลาของสัญญา
            16d, // 25 วันที่เริ่มต้นของสัญญา
            16d, // 26 วันที่สิ้นสุดของสัญญา
            28d, // 27 ระยะเวลารับประกันความชำรุดบกพร่อง
            20d, // 28 วันที่เริ่มต้นรับประกันสัญญา
            20d, // 29 วันที่สิ้นสุดรับประกันสัญญา
            22d, // 30 วันที่อนุมัติคืนหลักประกันสัญญา
            22d, // 31 วันที่รับคืนหลักประกันสัญญา
            22d, // 32 วันที่ส่งต้นฉบับให้ผู้รับจ้างลงนามในสัญญา
            22d, // 33 วันที่รับต้นฉบับสัญญาคืนจากผู้รับจ้าง
            22d, // 34 จำนวนวันที่คู่สัญญาลงนาม (7 วัน)
            22d, // 35 วันที่ผู้มีอำนาจลงนามในสัญญา (ธอส.)
            22d, // 36 วันที่ได้รับเรื่องคืนจากผู้มีอำนาจลงนาม (ธอส.)
            20d, // 37 ผู้มีอำนาจลงนามในสัญญา
            20d, // 38 วันที่รายงาน สตง./สรรพากร
            16d, // 39 วันที่อนุมัติจ้าง
            20d, // 40 วันที่เอกสารลงนามครบถ้วนพร้อมลงนาม
            20d, // 41 สบส.-วันที่ตรวจสอบ COI
            22d, // 42 สบส.-วันที่ตรวจสอบ CDD/KYC Watchlist
            20d, // 43 สบส.-วันที่ตรวจสอบผู้ทิ้งงาน
            20d, // 44 สบส.-วันที่คู่สัญญาลงนาม
            20d, // 45 วันที่ธนาคารลงนาม
            28d, // 46 รวมวันทำสัญญาลงนามแล้วเสร็จทั้ง 2 ฝ่าย
            20d, // 47 ผู้รับผิดชอบ
            16d, // 48 วันที่บันทึกข้อมูล
            28d, // 49 ทะเบียนกล่องส่งเก็บบริษัทจัดเก็บเอกสาร
            20d, // 50 หมายเหตุ
        };

        (object Label, uint Style)[] allHeaders =
        [
            ("ลำดับ", cellStyle.headerCream),
            ("วันที่รับเรื่องเข้าส่วนบริหารสัญญา", cellStyle.headerCream),
            ("เลขที่ใบสั่งซื้อ-สั่งจ้าง (PO)", cellStyle.headerCream),
            ("วันที่ (PO)", cellStyle.headerCream),
            ("วิธีการจัดซื้อจัดจ้าง", cellStyle.headerCream),
            ("ฝ่ายงาน-ผู้รับผิดชอบ", cellStyle.headerCream),
            ("ส่วนงาน-ผู้รับผิดชอบ", cellStyle.headerCream),
            ("เลขที่หนังสือแจ้งผู้รับจ้างลงนามสัญญา จพ.(สบส.)", cellStyle.headerCream),
            ("วันที่แจ้งผู้รับจ้างมาลงนาม", cellStyle.headerCream),
            ("เลขที่สัญญา จพ.(สบส.)", cellStyle.headerCream),
            ("วันที่ทำสัญญา", cellStyle.headerCream),
            ("ประเภทสัญญา", cellStyle.headerCream),
            ("ชื่อโครงการ (สัญญา)", cellStyle.headerCream),
            ("ค่าจ้าง (รวมภาษี)", cellStyle.headerCream),
            ("บริษัทคู่ค้า/ผู้รับจ้าง", cellStyle.headerCream),
            ("กรรมการบริษัท", cellStyle.headerCream),
            ("ผู้รับมอบอำนาจ/ผู้ลงนามสัญญา", cellStyle.headerCream),
            ("ผู้มอบอำนาจลงนามสัญญา", cellStyle.headerCream),
            ("ประเภทหลักประกันสัญญา", cellStyle.headerCream),
            ("วงเงินหลักประกันสัญญา", cellStyle.headerCream),
            ("วันที่ส่ง LG ตรวจสอบกับธนาคารผู้ออก LG", cellStyle.headerCream),
            ("วันที่ธนาคารตอบกลับ LG", cellStyle.headerCream),
            ("วันที่เริ่มหนังสือค้ำหลักประกันสัญญา (LG)", cellStyle.headerCream),
            ("วันที่สิ้นสุดหนังสือค้ำหลักประกันสัญญา (LG)", cellStyle.headerCream),
            ("ระยะเวลาแล้วเสร็จ/ส่งมอบงาน/ระยะเวลาของสัญญา", cellStyle.headerCream),
            ("วันที่เริ่มต้นของสัญญา", cellStyle.headerCream),
            ("วันที่สิ้นสุดของสัญญา", cellStyle.headerCream),
            ("ระยะเวลารับประกันความชำรุดบกพร่องของสัญญา", cellStyle.headerCream),
            ("วันที่เริ่มต้นรับประกันสัญญา", cellStyle.headerCream),
            ("วันที่สิ้นสุดรับประกันสัญญา", cellStyle.headerCream),
            ("วันที่อนุมัติคืนหลักประกันสัญญา", cellStyle.headerCream),
            ("วันที่รับคืนหลักประกันสัญญา", cellStyle.headerCream),
            ("วันที่ส่งต้นฉบับให้ผู้รับจ้างลงนามในสัญญา", cellStyle.headerCream),
            ("วันที่รับต้นฉบับสัญญาคืนจากผู้รับจ้าง", cellStyle.headerCream),
            ("จำนวนวันที่คู่สัญญาลงนาม (7 วัน)", cellStyle.headerCream),
            ("วันที่ผู้มีอำนาจลงนามในสัญญา (ธอส.)", cellStyle.headerCream),
            ("วันที่ได้รับเรื่องคืนจากผู้มีอำนาจลงนาม (ธอส.)", cellStyle.headerCream),
            ("ผู้มีอำนาจลงนามในสัญญา", cellStyle.headerCream),
            ("วันที่รายงาน สตง./สรรพากร", cellStyle.headerCream),
            ("วันที่อนุมัติจ้าง", cellStyle.headerCream),
            ("วันที่เอกสารลงนามครบถ้วนพร้อมลงนาม", cellStyle.headerCream),
            ("สบส.-วันที่ตรวจสอบ COI", cellStyle.headerCream),
            ("สบส.-วันที่ตรวจสอบ CDD/KYC Watchlist", cellStyle.headerCream),
            ("สบส.-วันที่ตรวจสอบผู้ทิ้งงาน", cellStyle.headerCream),
            ("สบส.-วันที่คู่สัญญาลงนาม", cellStyle.headerCream),
            ("วันที่ธนาคารลงนาม", cellStyle.headerCream),
            ("รวมวันทำสัญญาลงนามแล้วเสร็จทั้ง 2 ฝ่าย", cellStyle.headerCream),
            ("ผู้รับผิดชอบ", cellStyle.headerCream),
            ("วันที่บันทึกข้อมูล", cellStyle.headerCream),
            ("ทะเบียนกล่องส่งเก็บบริษัทจัดเก็บเอกสาร", cellStyle.headerCream),
            ("หมายเหตุ", cellStyle.headerCream),
        ];

        var parsedColumns = string.IsNullOrWhiteSpace(req.Columns)
            ? []
            : req.Columns.Split(',', StringSplitOptions.RemoveEmptyEntries)
                         .Select(s => int.TryParse(s.Trim(), out var n) ? (int?)n : null)
                         .Where(n => n.HasValue && n.Value >= 0 && n.Value < allColWidths.Length)
                         .Select(n => n!.Value)
                         .ToList();

        var activeColumns = parsedColumns.Count > 0
            ? parsedColumns
            : Enumerable.Range(0, allColWidths.Length).ToList();

        var filteredWidths = activeColumns.Select(i => allColWidths[i]).ToArray();
        var filteredHeaders = activeColumns.Select(i => allHeaders[i]).ToArray();

        using var excelDocument =
            ExportExcel.Create(stream)
                       .AddSheet("ข้อมูลสัญญา", filteredWidths, 1)
                       .RowStyled(filteredHeaders);

        var rowData = await GetRowsData(ct);

        if (rowData.Any())
        {
            var index = 1;
            foreach (var r in rowData)
            {
                (object Value, uint Style)[] allDataCells =
                [
                    (index.ToString(), cellStyle.center),
                    (r.ReceivedDate, cellStyle.center),
                    (r.PoNumber, cellStyle.center),
                    (string.Empty, cellStyle.normal),
                    (r.SupplyMethodName, cellStyle.normal),
                    (r.DepartmentShortName, cellStyle.normal),
                    (string.Empty, cellStyle.normal),
                    (string.Empty, cellStyle.normal),
                    (string.Empty, cellStyle.normal),
                    (r.ContractNumber, cellStyle.center),
                    (r.ContractDraftCreatedAt, cellStyle.center),
                    (r.ContractTypeName, cellStyle.normal),
                    (r.ContractName, cellStyle.normal),
                    (r.Budget, cellStyle.number2),
                    (r.VendorName, cellStyle.normal),
                    (string.Empty, cellStyle.normal),
                    (string.Empty, cellStyle.normal),
                    (string.Empty, cellStyle.normal),
                    (r.GuaranteeTypeName, cellStyle.normal),
                    (r.GuaranteeAmount, cellStyle.number2),
                    (string.Empty, cellStyle.normal),
                    (string.Empty, cellStyle.normal),
                    (r.BankCollateralStartDate, cellStyle.center),
                    (r.BankCollateralEndDate, cellStyle.center),
                    (r.DeliveryPeriod, cellStyle.normal),
                    (r.ContractStartDate, cellStyle.center),
                    (r.ContractEndDate, cellStyle.center),
                    (r.WarrantyPeriod, cellStyle.normal),
                    (r.WarrantyStartDate, cellStyle.center),
                    (r.WarrantyEndDate, cellStyle.center),
                    (r.GuaranteeReturnApprovalDate, cellStyle.center),
                    (string.Empty, cellStyle.normal),
                    (string.Empty, cellStyle.normal),
                    (string.Empty, cellStyle.normal),
                    (string.Empty, cellStyle.normal),
                    (string.Empty, cellStyle.normal),
                    (string.Empty, cellStyle.normal),
                    (string.Empty, cellStyle.normal),
                    (r.AuditRevenueDocumentDate, cellStyle.center),
                    (string.Empty, cellStyle.normal),
                    (r.ContractSignedDate, cellStyle.center),
                    (string.Empty, cellStyle.normal),
                    (string.Empty, cellStyle.normal),
                    (string.Empty, cellStyle.normal),
                    (r.ContractSignedDate, cellStyle.center),
                    (string.Empty, cellStyle.normal),
                    (string.Empty, cellStyle.normal),
                    (r.AssigneeName, cellStyle.normal),
                    (string.Empty, cellStyle.normal),
                    (string.Empty, cellStyle.normal),
                    (string.Empty, cellStyle.normal),
                ];

                var filteredCells = activeColumns.Select(i => allDataCells[i]).ToArray();
                excelDocument.RowStyled(filteredCells);
                index++;
            }
        }
        else
        {
            excelDocument.RowStyled(("ไม่พบข้อมูล", cellStyle.center))
                         .Merges("A2:AZ2");
        }

        excelDocument.Finish();

        var fileName = $"รายงานบริหารสัญญา_{DateTimeOffset.Now:yyyyMMddHHmmss}.xlsx";
        var content = stream.ToArray();

        await this.SendBytesAsync(
            content,
            fileName,
            contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            cancellation: ct);

        async Task<List<ContractDraftVendorExportRow>> GetRowsData(CancellationToken ct)
        {
            List<CaContractDraftVendor> vendors =
                await this.dbContext.CaContractDraftVendors
                          .Where(v => v.Status == ContractDraftVendorStatus.Approved && v.ContractSignedDate != null)
                          .WhereIfTrue(
                              !string.IsNullOrWhiteSpace(req.SupplyMethodCode),
                              v => v.ContractDraft.Procurement.SupplyMethodCode == ParameterCode.From(req.SupplyMethodCode!))
                          .WhereIfTrue(
                              !string.IsNullOrWhiteSpace(req.Keyword),
                              v => EF.Functions.ILike(v.ContractName, $"%{req.Keyword}%") ||
                                   EF.Functions.ILike((string)v.ContractNumber, $"%{req.Keyword}%") ||
                                   EF.Functions.ILike(v.Vendor.VendorInfo.EstablishmentName, $"%{req.Keyword}%") ||
                                   EF.Functions.ILike(v.PoNumber, $"%{req.Keyword}%"))
                          .WhereIfTrue(
                              !string.IsNullOrWhiteSpace(req.DepartmentCode),
                              v => v.ContractDraft.Procurement.DepartmentId == BusinessUnitId.From(req.DepartmentCode!))
                          .WhereIfTrue(
                              req.BudgetYear.HasValue,
                              v => v.ContractDraft.Procurement.BudgetYear == req.BudgetYear)
                          .Include(v => v.ContractDraft)
                              .ThenInclude(cd => cd.Procurement)
                                  .ThenInclude(p => p.Department)
                          .Include(v => v.ContractDraft)
                              .ThenInclude(cd => cd.Procurement)
                                  .ThenInclude(p => p.SupplyMethodSpecialType)
                          .Include(v => v.ContractType)
                          .Include(v => v.Vendor)
                              .ThenInclude(vendor => vendor.VendorInfo)
                          .Include(v => v.DraftTermsConditions)
                          .Include(v => v.PaymentTerms)
                          .AsSplitQuery()
                          .ToListAsync(cancellationToken: ct);

            var vendorIds = vendors.Select(v => v.Id).ToList();

            var procurementIds = vendors
                .Select(v => v.ContractDraft.Procurement.Id)
                .Distinct()
                .ToList();

            var guaranteeTypeCodes = vendors
                .Select(v => v.DraftTermsConditions?.Guarantee?.TypeCode)
                .Where(c => c != null)
                .Distinct()
                .ToList();

            var guaranteeTypeLabels =
                await this.dbContext.SuParameters
                          .Where(p => guaranteeTypeCodes.Contains(p.Code))
                          .ToDictionaryAsync(p => p.Code, p => p.Label, cancellationToken: ct);

            var purchaseOrderApprovals =
                await this.dbContext.PPurchaseOrderApprovals
                          .Where(poa => procurementIds.Contains(poa.ProcurementId))
                          .Include(poa => poa.Assignees)
                          .Include(poa => poa.Contracts)
                          .ToListAsync(cancellationToken: ct);

            var guaranteeReturns =
                await this.dbContext.CmContractGuaranteeReturns
                          .Where(gr => vendorIds.Contains(gr.ContractDraftVendorId))
                          .Include(gr => gr.Acceptors)
                          .ToListAsync(cancellationToken: ct);

            var auditRevenueDates =
                await this.dbContext.Set<RpAuditAndRevenueDetail>()
                          .Where(d => vendorIds.Contains(d.CaContractDraftVendor.Id))
                          .Select(d => new { VendorId = d.CaContractDraftVendor.Id, d.RpAuditAndRevenue.DocumentDate })
                          .ToListAsync(cancellationToken: ct);

            var auditRevenueDateByVendor = auditRevenueDates
                .GroupBy(x => x.VendorId)
                .ToDictionary(g => g.Key, g => (DateTimeOffset?)g.Max(x => x.DocumentDate));

            return
                [.. vendors
                    .Select(v =>
                    {
                        var procurement = v.ContractDraft.Procurement;
                        var purchaseOrders = purchaseOrderApprovals
                            .Where(poa => poa.ProcurementId == procurement.Id)
                            .ToList();

                        var topAssignee =
                            purchaseOrders
                                .SelectMany(poa => poa.Assignees)
                                .Where(a => a.Type == AssigneeType.Assignee)
                                .MaxBy(a => a.Sequence);

                        var poContract =
                            purchaseOrders
                                .SelectMany(poa => poa.Contracts)
                                .FirstOrDefault(c => c.PoNumber == v.PoNumber);

                        var guarantee = v.DraftTermsConditions?.Guarantee;
                        var hasWarranty = v.DraftTermsConditions?.Warranty?.HasWarranty == true;
                        var warrantyPeriod = v.DraftTermsConditions?.Warranty?.WarrantyPeriod;

                        var warrantyStartDate = v.EndDate.HasValue ? v.EndDate.Value.AddDays(1) : (DateTimeOffset?)null;
                        var warrantyEndDate = ComputeWarrantyEndDate(warrantyStartDate, warrantyPeriod);

                        var maxLeadTime = v.PaymentTerms.Any()
                            ? v.PaymentTerms.Max(pt => pt.LeadTime)
                            : null;

                        var guaranteeReturnApprovalDate =
                            guaranteeReturns
                                .Where(gr => gr.ContractDraftVendorId == v.Id)
                                .SelectMany(gr => gr.Acceptors)
                                .MaxBy(a => a.Sequence)
                                ?.ActionAt;

                        auditRevenueDateByVendor.TryGetValue(v.Id, out var auditRevenueDate);

                        return new ContractDraftVendorExportRow(
                            ReceivedDate: topAssignee?.AuditInfo.CreatedAt.ToThaiDateString("d MMM yyyy") ?? string.Empty,
                            PoNumber: v.PoNumber ?? string.Empty,
                            SupplyMethodName: procurement.SupplyMethodSpecialType?.Label ?? string.Empty,
                            DepartmentShortName: procurement.Department.ShortName,
                            ContractNumber: poContract?.ContractNumber ?? string.Empty,
                            ContractDraftCreatedAt: v.ContractDraft.AuditInfo.CreatedAt.ToThaiDateString("d MMM yyyy"),
                            ContractTypeName: v.ContractType?.Label ?? string.Empty,
                            ContractName: v.ContractName,
                            Budget: v.Agreement?.TotalAmount,
                            VendorName: v.Vendor.VendorInfo.EstablishmentName,
                            GuaranteeTypeName: guarantee?.TypeCode is { } typeCode && guaranteeTypeLabels.TryGetValue(typeCode, out var typeLabel) ? typeLabel : string.Empty,
                            GuaranteeAmount: guarantee?.Amount,
                            BankCollateralStartDate: guarantee?.BankCollateralStartDate.ToThaiDateString("d MMM yyyy") ?? string.Empty,
                            BankCollateralEndDate: guarantee?.BankCollateralEndDate.ToThaiDateString("d MMM yyyy") ?? string.Empty,
                            DeliveryPeriod: maxLeadTime.HasValue ? $"{maxLeadTime} วัน" : string.Empty,
                            ContractStartDate: v.StartDate.ToThaiDateString("d MMM yyyy"),
                            ContractEndDate: v.EndDate.ToThaiDateString("d MMM yyyy"),
                            WarrantyPeriod: hasWarranty ? BuildWarrantyPeriodText(warrantyPeriod) : string.Empty,
                            WarrantyStartDate: warrantyStartDate.ToThaiDateString("d MMM yyyy"),
                            WarrantyEndDate: warrantyEndDate.ToThaiDateString("d MMM yyyy"),
                            GuaranteeReturnApprovalDate: guaranteeReturnApprovalDate.ToThaiDateString(),
                            AuditRevenueDocumentDate: auditRevenueDate.ToThaiDateString("d MMM yyyy"),
                            ContractSignedDate: v.ContractSignedDate.ToThaiDateString("d MMM yyyy"),
                            AssigneeName: topAssignee?.FullName ?? string.Empty);
                    })
                    .OrderBy(r => r.ContractNumber)
                    .ThenBy(r => r.VendorName)];
        }

        static string BuildWarrantyPeriodText(RentalDurationInfo? period)
        {
            if (period is null)
            {
                return string.Empty;
            }

            var parts = new List<string>();

            if (period.Day is > 0)
            {
                parts.Add($"{period.Day} วัน");
            }

            if (period.Month is > 0)
            {
                parts.Add($"{period.Month} เดือน");
            }

            if (period.Year is > 0)
            {
                parts.Add($"{period.Year} ปี");
            }

            return string.Join(" ", parts);
        }

        static DateTimeOffset? ComputeWarrantyEndDate(DateTimeOffset? start, RentalDurationInfo? period)
        {
            if (start is null || period is null)
            {
                return null;
            }

            return start.Value
                        .AddYears(period.Year ?? 0)
                        .AddMonths(period.Month ?? 0)
                        .AddDays(period.Day ?? 0);
        }
    }
}

public sealed record ContractDraftVendorExportRow(
    string ReceivedDate,
    string PoNumber,
    string SupplyMethodName,
    string DepartmentShortName,
    string ContractNumber,
    string ContractDraftCreatedAt,
    string ContractTypeName,
    string ContractName,
    decimal? Budget,
    string VendorName,
    string GuaranteeTypeName,
    decimal? GuaranteeAmount,
    string BankCollateralStartDate,
    string BankCollateralEndDate,
    string DeliveryPeriod,
    string ContractStartDate,
    string ContractEndDate,
    string WarrantyPeriod,
    string WarrantyStartDate,
    string WarrantyEndDate,
    string GuaranteeReturnApprovalDate,
    string AuditRevenueDocumentDate,
    string ContractSignedDate,
    string AssigneeName);
