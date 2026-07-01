namespace GHB.DP2.Application.Features.Procurement.Appoint;

using GHB.DP2.Infrastructure;
using GHB.DP2.Infrastructure.Services.ExcelImportAndExport;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

public class ExportAppointEndpoint : EndpointWithoutRequest
{
    private readonly Dp2DbContext dbContext;
    private readonly IExcelExportService excelExportService;

    public ExportAppointEndpoint(
        Dp2DbContext dbContext,
        IExcelExportService excelExportService)
    {
        this.dbContext = dbContext;
        this.excelExportService = excelExportService;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("Procurement/Appoint"));
        this.Get("appointments/export");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var appoint = await this.dbContext.PpAppoints
                                .AsNoTracking()
                                .Select(a => new
                                {
                                    a.Id,
                                    a.ReferenceId,
                                    a.ProcurementId,
                                    a.AppointNumber,
                                    a.MemorandumDate,
                                    a.MemorandumNumber,
                                    a.Telephone,
                                    a.Reason,
                                })
                                .ToListAsync(ct);

        var response = await this.excelExportService.ExportToExcelStreamAsync(appoint, cancellationToken: ct);

        await this.SendStreamAsync(response, cancellation: ct);
    }
}