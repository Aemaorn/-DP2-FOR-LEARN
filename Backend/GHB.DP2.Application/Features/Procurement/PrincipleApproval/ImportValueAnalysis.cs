namespace GHB.DP2.Application.Features.Procurement.PrincipleApproval;

using GHB.DP2.Application.Common;
using GHB.DP2.Domain.Procurement.PPrincipleApproval;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record ImportValueAnalysisRequest(
    IFormFile File);

public record ImportValueAnalysisResponse(
    IEnumerable<PrincipleApprovalRentalAnalysisDto>? RentalAnalyses,
    decimal? AnalysisSummaryNpv,
    decimal? AnalysisSummaryPaybackYearPeriod,
    decimal? AnalysisSummaryDiscountedPaybackYearPeriod);

public class AnalysisYearDynamicRow
{
    [ExcelColumn(1)]
    public string Description { get; init; }

    [ExcelDynamicColumns(@"^\d{4}$")]
    public Dictionary<int, decimal?> YearValues { get; init; }
}

public class AnalysisSummary
{
    [ExcelColumn(1)]
    public string Description { get; init; }

    [ExcelColumn(2)]
    public decimal? Quantity { get; init; }

    [ExcelColumn(3)]
    public string? Unit { get; init; }
}

public class ImportValueAnalysisEndpoint : EndpointBase<ImportValueAnalysisRequest, Ok<ImportValueAnalysisResponse>>
{
    public ImportValueAnalysisEndpoint(ILogger<ImportValueAnalysisEndpoint> logger)
        : base(logger)
    {
    }

    public override void Configure()
    {
        this.Options(builder =>
            builder.WithTags(nameof(PrincipleApprovalRental))
                   .ProducesProblem(StatusCodes.Status400BadRequest)
                   .ProducesProblem(StatusCodes.Status500InternalServerError)
                   .Produces<Ok<ImportValueAnalysisResponse>>());
        this.Description(d => d.Accepts<ImportValueAnalysisRequest>("multipart/form-data"));
        this.AllowFileUploads();
        this.Post("procurement/{ProcurementId:guid}/principle-approval/import-analysis");
    }

    protected override ValueTask<Ok<ImportValueAnalysisResponse>> HandleRequestAsync(ImportValueAnalysisRequest req, CancellationToken ct)
    {
        var file = req.File;

        if (file.Length == 0)
        {
            this.ThrowError("File is required", StatusCodes.Status400BadRequest);
        }

        try
        {
            var fileStream = file.OpenReadStream();

            var rentalAnalysesGeneral =
                ImportExcel<AnalysisYearDynamicRow>
                    .FromStream(
                        fileStream,
                        Constants.ValueAnalysisSheet.General)
                    .Select((data, index) =>
                        new PrincipleApprovalRentalAnalysisDto(
                            default,
                            index + 1,
                            RentalAnalysisType.General,
                            data.Description,
                            data.YearValues.Select(kv =>
                                new PrincipleApprovalRentalAnalysisDetail(
                                    null,
                                    kv.Key,
                                    kv.Value ?? 0m)).ToArray()));

            var rentalAnalysesProfitAndLoss =
                ImportExcel<AnalysisYearDynamicRow>
                    .FromStream(
                        fileStream,
                        Constants.ValueAnalysisSheet.ProfitAndLoss)
                    .Select((data, index) =>
                        new PrincipleApprovalRentalAnalysisDto(
                            default,
                            index + 1,
                            RentalAnalysisType.ProfitAndLoss,
                            data.Description,
                            data.YearValues.Select(kv =>
                                new PrincipleApprovalRentalAnalysisDetail(
                                    null,
                                    kv.Key,
                                    kv.Value ?? 0m)).ToArray()));

            var rentalAnalyses =
                rentalAnalysesGeneral.Concat(rentalAnalysesProfitAndLoss)
                                     .ToArray();

            var analysisSummaries =
                ImportExcel<AnalysisSummary>
                    .FromStream(
                        fileStream,
                        Constants.ValueAnalysisSheet.Summary);

            var analysisSummaryNpv =
                analysisSummaries.FirstOrDefault(s =>
                    s.Description?.Contains(Constants.AnalysisSummary.Npv) ?? false)?.Quantity;

            var analysisSummaryPaybackYearPeriod =
                analysisSummaries.FirstOrDefault(s =>
                    s.Description?.Contains(Constants.AnalysisSummary.PaybackPeriod) ?? false)?.Quantity;

            var analysisSummaryDiscountedPaybackYearPeriod =
                analysisSummaries.FirstOrDefault(s =>
                    s.Description?.Contains(Constants.AnalysisSummary.DiscountedPaybackPeriod) ?? false)?.Quantity;

            return ValueTask.FromResult(
                TypedResults.Ok(
                    new ImportValueAnalysisResponse(
                        rentalAnalyses,
                        analysisSummaryNpv,
                        analysisSummaryPaybackYearPeriod,
                        analysisSummaryDiscountedPaybackYearPeriod)));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Sheet") && ex.Message.Contains("not found"))
        {
            this.Logger.LogError(ex, "Sheet not found in Excel file");
            this.ThrowError($"ไม่พบ sheet ชื่อ 'ข้อมูลประกอบผลการดำเนินงาน' ในไฟล์ Excel: {ex.Message}", StatusCodes.Status400BadRequest);
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Error importing Excel file");
            this.ThrowError($"เกิดข้อผิดพลาดในการ import ไฟล์ Excel: {ex.Message}", StatusCodes.Status500InternalServerError);
        }

        return default; // This will never be reached due to ThrowError above
    }
}