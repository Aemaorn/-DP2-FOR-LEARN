namespace GHB.DP2.Application.Features.Procurement.PrincipleApproval;

using GHB.DP2.Application.Common;
using GHB.DP2.Application.Features.Procurement.PrincipleApproval.Constants;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PPrincipleApproval;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

public record ExportConsiderationRequest(
    Guid ProcurementId,
    Guid? Id);

public class ExportConsiderationEndpoint : Endpoint<ExportConsiderationRequest>
{
    private readonly Dp2DbContext dbContext;

    public ExportConsiderationEndpoint(Dp2DbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(builder =>
            builder.WithTags(nameof(PrincipleApprovalRental))
                   .ProducesProblem(StatusCodes.Status404NotFound)
                   .ProducesProblem(StatusCodes.Status500InternalServerError));
        this.Get(
            "procurement/{ProcurementId:guid}/principle-approval/{Id:guid?}/export-consideration",
            "procurement/{ProcurementId:guid}/principle-approval/export-consideration");
    }

    public override async Task HandleAsync(ExportConsiderationRequest req, CancellationToken ct)
    {
        var approvalRentalAsync =
            req.Id is null
                ? this.dbContext.Procurements
                      .FirstOrDefaultAsync(p => p.Id == ProcurementId.From(req.ProcurementId), ct)
                      .Map(PrincipleApprovalResponseDto.MapToResponse)
                : this.dbContext.PPrincipleApprovals
                      .Include(pPrincipleApproval => pPrincipleApproval.PerfSupportDataDetails)
                      .Include(pPrincipleApproval => pPrincipleApproval.RoiLoanAndDepositSummaries)
                      .Include(pPrincipleApproval => pPrincipleApproval.Procurement)
                      .Include(pPrincipleApproval => pPrincipleApproval.RoiPerfResults)
                      .FirstOrDefaultAsync(
                          pp => pp.Id == PPrincipleApprovalId.From(req.Id.Value) &&
                                pp.ProcurementId == ProcurementId.From(req.ProcurementId),
                          ct)
                      .Map(PrincipleApprovalResponseDto.MapToResponse);

        var approvalRental = await approvalRentalAsync;

        if (approvalRental is null)
        {
            await this.SendAsync(
                "Principle Approval Rental not found",
                StatusCodes.Status404NotFound,
                ct);

            return;
        }

        var perfSupportDataDetails =
            approvalRental.PerfSupportDataDetails;

        var roiLoanAndDepositSummaries =
            approvalRental.RoiLoanAndDepositSummaries?.ToArray() ??
            [.. PrincipleApprovalConstant.DefaultRoiLoanAndDepositSummaries()];

        var roiPerfResults =
            approvalRental.RoiPerfResults?.ToArray() ?? [];

        using var stream = new MemoryStream();

        using var excelDocument =
            ExportExcel.Create(stream)
                         .AddSheet(
                             ConsiderationSheet.PerfSupportDataDetails,
                             [30d, 20d, 20d, 20d, 20d, 20d,],
                             2)
                         .RowStyled(
                             ("รายการ", 2u),
                             ("ม.ค. – ธ.ค. 2565", 1u),
                             (string.Empty, 1u),
                             ("ม.ค. – ธ.ค. 2566", 1u),
                             (string.Empty, 1u))
                         .RowStyled(
                             (string.Empty, 2u),
                             ("จำนวนบัญชี", 2u),
                             ("จำนวนเงิน (ลบ.)", 2u),
                             ("จำนวนบัญชี", 2u),
                             ("จำนวนเงิน (ลบ.)", 2u))
                         .Merges("A1:A2", "B1:C1", "D1:E1");

        foreach (var item in perfSupportDataDetails)
        {
            excelDocument.Row(
                item.ActivityDescription,
                item.AccountCountYear1 ?? 0,
                item.AmountYear1 ?? 0,
                item.AccountCountYear2 ?? 0,
                item.AmountYear2 ?? 0);
        }

        excelDocument.AddSheet(
                         ConsiderationSheet.RoiLoanAndDepositSummaries,
                         [42d, 18d, 18d, 18d],
                         freezeTopRows: 2)
                     .TwoRowGroupHeader(
                         "รายการ",
                         ("ปีที่ 1", "ม.ค. – ธ.ค. 2564"),
                         ("ปีที่ 2", "ม.ค. – ธ.ค. 2565"),
                         ("ปีที่ 3", "ม.ค. – ธ.ค. 2566"));

        foreach (var item in roiLoanAndDepositSummaries)
        {
            excelDocument.Row(
                item.ActivityDescription,
                item.AmountYear1 ?? 0,
                item.AmountYear2 ?? 0,
                item.AmountYear3 ?? 0);
        }

        excelDocument
            .AddSheet(
                ConsiderationSheet.DepositRemaining,
                [10d, 16d, 14d, 16d, 16d, 16d, 16d],
                freezeTopRows: 2)
            .TwoLevelGroupHeader(
                leftTitle: "ปี",
                ("ปีที่ 1", ["ทำได้จริง", "Growth (%)", "เป้าหมาย"]),
                ("จำนวนเงิน (ล้านบาท)", ["ทำได้จริง", "คาดเป็น", "Growth (%)"]));

        foreach (var item in
                 roiPerfResults.Where(r => r.PerformanceResultGroup == PerformanceResultGroup.DepositRemaining))
        {
            excelDocument.Row(
                item.Year,
                item.AccountActual,
                item.AmountTarget,
                item.AccountGrowth,
                item.AmountActual,
                item.AmountRate,
                item.AmountGrowth);
        }

        excelDocument
            .AddSheet(
                ConsiderationSheet.LoanExisting,
                [10d, 16d, 14d, 16d, 16d, 16d, 16d],
                freezeTopRows: 2)
            .TwoLevelGroupHeader(
                leftTitle: "ปี",
                ("ปีที่ 1", ["ทำได้จริง", "Growth (%)", "เป้าหมาย"]),
                ("จำนวนเงิน (ล้านบาท)", ["ทำได้จริง", "คาดเป็น", "Growth (%)"]));

        foreach (var item in
                 roiPerfResults.Where(r => r.PerformanceResultGroup == PerformanceResultGroup.LoanExisting))
        {
            excelDocument.Row(
                item.Year,
                item.AccountActual,
                item.AmountTarget,
                item.AccountGrowth,
                item.AmountActual,
                item.AmountRate,
                item.AmountGrowth);
        }

        excelDocument.AddSheet(
                         ConsiderationSheet.LoanNew,
                         [10d, 16d, 14d, 16d, 16d, 16d, 16d],
                         freezeTopRows: 2)
                     .TwoLevelGroupHeader(
                         leftTitle: "ปี",
                         ("ปีที่ 1", ["ทำได้จริง", "Growth (%)", "เป้าหมาย"]),
                         ("จำนวนเงิน (ล้านบาท)", ["ทำได้จริง", "คาดเป็น", "Growth (%)"]));

        foreach (var item in
                 roiPerfResults.Where(r => r.PerformanceResultGroup == PerformanceResultGroup.LoanNew))
        {
            excelDocument.Row(
                item.Year,
                item.AccountActual,
                item.AmountTarget,
                item.AccountGrowth,
                item.AmountActual,
                item.AmountRate,
                item.AmountGrowth);
        }

        excelDocument.Finish();

        var fileName = $"consideration_{approvalRental.Procurement?.ProcurementNumber?.Value}.xlsx";

        var content = stream.ToArray();

        await this.SendBytesAsync(
            content,
            fileName,
            contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            cancellation: ct);
    }
}