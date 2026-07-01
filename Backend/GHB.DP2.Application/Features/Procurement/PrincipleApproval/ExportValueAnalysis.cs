namespace GHB.DP2.Application.Features.Procurement.PrincipleApproval;

using System.Linq;
using GHB.DP2.Application.Common;
using GHB.DP2.Application.Features.Procurement.PrincipleApproval.Constants;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PPrincipleApproval;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

public record ExportValueAnalysisRequest(
    Guid ProcurementId,
    Guid? Id);

public class ExportValueAnalysisEndpoint : Endpoint<ExportValueAnalysisRequest>
{
    private readonly Dp2DbContext dbContext;

    public ExportValueAnalysisEndpoint(Dp2DbContext dbContext)
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
            "procurement/{ProcurementId:guid}/principle-approval/{Id:guid?}/export-analysis",
            "procurement/{ProcurementId:guid}/principle-approval/export-analysis");
    }

    public override async Task HandleAsync(ExportValueAnalysisRequest req, CancellationToken ct)
    {
        var approvalRentalAsync =
            req.Id is null
                ? this.dbContext.Procurements
                      .FirstOrDefaultAsync(p => p.Id == ProcurementId.From(req.ProcurementId), ct)
                      .Map(PrincipleApprovalResponseDto.MapToResponse)
                : this.dbContext.PPrincipleApprovals
                      .Include(pPrincipleApproval => pPrincipleApproval.Procurement)
                      .Include(pPrincipleApproval => pPrincipleApproval.PrincipleApprovalRentalAnalyses)
                      .ThenInclude(pPrincipleApprovalRentalAnalysis => pPrincipleApprovalRentalAnalysis.PrincipleApprovalRentalAnalysisDetails)
                      .FirstOrDefaultAsync(
                          pp => pp.Id == PPrincipleApprovalId.From(req.Id!.Value) &&
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

        var years = PrincipleApprovalConstant.DefaultAnalysisYears()
                                             .Select(y => y.ToString())
                                             .ToArray();

        var rentalAnalyses =
            approvalRental.RentalAnalyses?.ToArray() ?? [.. PrincipleApprovalConstant.DefaultRentalAnalysis()];

        using var stream = new MemoryStream();
        using var excelDocument =
            ExportExcel.Create(stream)
                       .AddSheet(
                           ValueAnalysisSheet.General,
                           [26d, 16d, 16d, 16d, 16d, 16d],
                           freezeTopRows: 1)
                       .HeaderRow("รายการ", years);

        WriteGrid(excelDocument, rentalAnalyses.Where(r => r.Type == RentalAnalysisType.General));

        excelDocument.AddSheet(
                         ValueAnalysisSheet.ProfitAndLoss,
                         [26d, 16d, 16d, 16d, 16d, 16d],
                         freezeTopRows: 1)
                     .HeaderRow("งบกำไรขาดทุน", years);

        WriteGrid(excelDocument, rentalAnalyses.Where(r => r.Type == RentalAnalysisType.ProfitAndLoss));

        excelDocument.AddSheet(
                         ValueAnalysisSheet.Summary,
                         [34d, 24d, 24d],
                         freezeTopRows: 1)
                     .HeaderRow("ความคุ้มค่าโครงการ", ["จำนวน", "หน่วย"]);
        excelDocument.Row(Constants.AnalysisSummary.Npv, approvalRental.AnalysisSummaryNpv ?? decimal.Zero, "ล้านบาท");
        excelDocument.Row(Constants.AnalysisSummary.PaybackPeriod, approvalRental.AnalysisSummaryPaybackYearPeriod ?? decimal.Zero, "ปี");
        excelDocument.Row(Constants.AnalysisSummary.DiscountedPaybackPeriod, approvalRental.AnalysisSummaryDiscountedPaybackYearPeriod ?? decimal.Zero, "ปี");

        excelDocument.Finish();

        var fileName = $"ValueAnalysis_{approvalRental.Procurement?.ProcurementNumber?.Value}.xlsx";

        var content = stream.ToArray();

        await this.SendBytesAsync(
            content,
            fileName,
            contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            cancellation: ct);
    }

    private static ExportExcel WriteGrid(
        ExportExcel exportExcel,
        IEnumerable<PrincipleApprovalRentalAnalysisDto> items)
    {
        var years = PrincipleApprovalConstant.DefaultAnalysisYears().ToArray();

        foreach (var item in items.OrderBy(i => i.Sequence))
        {
            var mapYears = item.Details.ToDictionary(d => d.Year, d => d.Amount);
            var row = new object[1 + years.Length];
            row[0] = item.Description;

            for (var i = 0; i < years.Length; i++)
            {
                row[i + 1] = mapYears.GetValueOrDefault(years[i], 0.00m);
            }

            exportExcel.Row(row);
        }

        return exportExcel;
    }
}