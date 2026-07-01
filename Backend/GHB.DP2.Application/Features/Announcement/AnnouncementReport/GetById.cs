namespace GHB.DP2.Application.Features.Announcement.AnnouncementReport;

using global::GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetAnnouncementReportByIdRequest(Guid Id);

public record GetAnnouncementReportByIdResponse(
    Guid Id,
    int? Year,
    string? Discretion,
    string? AnnouncementReportTypeCode,
    string? AnnouncementReportType,
    bool? IsActive,
    bool? IsDp,
    Guid? DocumentId,
    string? DocumentName,
    string? DocumentUrl);

public class GetAnnouncementReportByIdEndpoint : EndpointBase<GetAnnouncementReportByIdRequest, Results<Ok<GetAnnouncementReportByIdResponse>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public GetAnnouncementReportByIdEndpoint(
        ILogger<GetAnnouncementReportByIdEndpoint> logger,
        Dp2DbContext dbContext)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Get("announcement-report/detail/{Id:guid}");
        this.Description(b => b
            .WithTags("AnnouncementReport")
            .WithName("GetAnnouncementReportById")
            .AllowAnonymous()
            .Produces<GetAnnouncementReportByIdResponse>(StatusCodes.Status200OK)
            .Produces<string>(StatusCodes.Status404NotFound));
    }

    protected override async ValueTask<Results<Ok<GetAnnouncementReportByIdResponse>, NotFound<string>>> HandleRequestAsync(
        GetAnnouncementReportByIdRequest req,
        CancellationToken ct)
    {
        var data = await this.dbContext.AnnouncementReports
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.Id == Domain.AnnouncementInfo.AnnouncementReportId.From(req.Id))
            .Select(x => new GetAnnouncementReportByIdResponse(
                x.Id.Value,
                x.Year,
                x.Discretion,
                x.AnnouncementReportTypeCode != null ? (string)x.AnnouncementReportTypeCode! : null,
                x.AnnouncementCategory != null ? x.AnnouncementCategory.Label : null,
                x.IsActive,
                x.IsDp,
                x.DocumentId.HasValue ? x.DocumentId.Value.Value : (Guid?)null,
                x.DocumentName,
                x.DocumentUrl))
            .FirstOrDefaultAsync(ct);

        if (data is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูล");
        }

        return TypedResults.Ok(data);
    }
}
