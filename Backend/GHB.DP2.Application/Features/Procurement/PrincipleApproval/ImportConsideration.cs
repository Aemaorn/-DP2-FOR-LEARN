namespace GHB.DP2.Application.Features.Procurement.PrincipleApproval;

using GHB.DP2.Application.Common;
using GHB.DP2.Application.Features.Procurement.PrincipleApproval.Constants;
using GHB.DP2.Domain.Procurement.PPrincipleApproval;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record ImportConsiderationRequest(IFormFile File);

public record ImportConsiderationResponse(
    IEnumerable<PerfSupportDataDetailDto> PerfSupportDataDetails,
    IEnumerable<RoiLoanAndDepositSummaryDto> RoiLoanAndDepositSummaries,
    IEnumerable<RoiPerfResultDto> RoiPerfResults);

public class PerfSupportDataDetailDto
{
    public int Sequence { get; private set; }

    [ExcelColumn(1)]
    public string? ActivityDescription { get; init; }

    [ExcelColumn(2)]
    public decimal? AccountCountYear1 { get; init; }

    [ExcelColumn(3)]
    public decimal? AmountYear1 { get; init; }

    [ExcelColumn(4)]
    public decimal? AccountCountYear2 { get; init; }

    [ExcelColumn(5)]
    public decimal? AmountYear2 { get; init; }

    public PerfSupportDataDetailDto WithSequence(int sequence)
    {
        this.Sequence = sequence;

        return this;
    }
}

public class RoiLoanAndDepositSummaryDto
{
    public int Sequence { get; private set; }

    [ExcelColumn(1)]
    public string ActivityDescription { get; init; }

    [ExcelColumn(2)]
    public decimal? AmountYear1 { get; init; }

    [ExcelColumn(3)]
    public decimal? AmountYear2 { get; init; }

    [ExcelColumn(4)]
    public decimal? AmountYear3 { get; init; }

    public RoiLoanAndDepositSummaryDto WithSequence(int sequence)
    {
        this.Sequence = sequence;

        return this;
    }
}

public class RoiPerfResultDto
{
    public int Sequence { get; private set; }

    public PerformanceResultGroup PerformanceResultGroup { get; private set; }

    [ExcelColumn(1)]
    public int Year { get; init; }

    [ExcelColumn(2)]
    public decimal AccountActual { get; init; }

    [ExcelColumn(3)]
    public decimal AccountGrowth { get; init; }

    [ExcelColumn(4)]
    public decimal AmountTarget { get; init; }

    [ExcelColumn(5)]
    public decimal AmountActual { get; init; }

    [ExcelColumn(6)]
    public decimal AmountRate { get; init; }

    [ExcelColumn(7)]
    public decimal AmountGrowth { get; init; }

    public RoiPerfResultDto WithSequence(int sequence)
    {
        this.Sequence = sequence;

        return this;
    }

    public RoiPerfResultDto WithPerformanceResultGroup(PerformanceResultGroup group)
    {
        this.PerformanceResultGroup = group;

        return this;
    }
}

public class ImportConsiderationEndpoint : EndpointBase<ImportConsiderationRequest, Ok<ImportConsiderationResponse>>
{
    public ImportConsiderationEndpoint(ILogger<ImportConsiderationEndpoint> logger)
        : base(logger)
    {
    }

    public override void Configure()
    {
        this.Options(builder =>
            builder.WithTags(nameof(PrincipleApprovalRental))
                   .Produces<ImportConsiderationResponse>()
                   .ProducesProblem(StatusCodes.Status400BadRequest)
                   .ProducesProblem(StatusCodes.Status500InternalServerError));
        this.Description(d => d.Accepts<ImportConsiderationRequest>("multipart/form-data"));
        this.AllowFileUploads();
        this.Post("procurement/{ProcurementId:guid}/principle-approval/import-consideration");
    }

    protected override ValueTask<Ok<ImportConsiderationResponse>> HandleRequestAsync(ImportConsiderationRequest req, CancellationToken ct)
    {
        var file = req.File;

        if (file.Length == 0)
        {
            this.ThrowError("File is required", StatusCodes.Status400BadRequest);
        }

        try
        {
            var fileStream = file.OpenReadStream();

            // Try to import with better error handling
            var perfSupportDataDetails =
                ImportExcel<PerfSupportDataDetailDto>
                    .FromStream(
                        fileStream,
                        ConsiderationSheet.PerfSupportDataDetails,
                        headerRow: 2,
                        startDataRow: 3)
                    .Select((item, index) => item.WithSequence(index + 1));

            var roiLoanAndDepositSummaries =
                ImportExcel<RoiLoanAndDepositSummaryDto>
                    .FromStream(
                        fileStream,
                        ConsiderationSheet.RoiLoanAndDepositSummaries,
                        headerRow: 2,
                        startDataRow: 3)
                    .Select((item, index) => item.WithSequence(index + 1));

            var depositRemaining =
                ImportExcel<RoiPerfResultDto>
                    .FromStream(
                        fileStream,
                        ConsiderationSheet.DepositRemaining,
                        headerRow: 2,
                        startDataRow: 3)
                    .Select((item, index) =>
                        item.WithSequence(index + 1)
                            .WithPerformanceResultGroup(PerformanceResultGroup.DepositRemaining));

            var loanExisting =
                ImportExcel<RoiPerfResultDto>
                    .FromStream(
                        fileStream,
                        ConsiderationSheet.LoanExisting,
                        headerRow: 2,
                        startDataRow: 3)
                    .Select((item, index) =>
                        item.WithSequence(index + 1)
                            .WithPerformanceResultGroup(PerformanceResultGroup.LoanExisting));

            var loanNew =
                ImportExcel<RoiPerfResultDto>
                    .FromStream(
                        fileStream,
                        ConsiderationSheet.LoanNew,
                        headerRow: 2,
                        startDataRow: 3)
                    .Select((item, index) =>
                        item.WithSequence(index + 1)
                            .WithPerformanceResultGroup(PerformanceResultGroup.LoanNew));

            var roiPerfResults = depositRemaining
                                 .Concat(loanExisting)
                                 .Concat(loanNew);

            return ValueTask.FromResult(
                TypedResults.Ok(
                    new ImportConsiderationResponse(
                        perfSupportDataDetails,
                        roiLoanAndDepositSummaries,
                        roiPerfResults)));
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