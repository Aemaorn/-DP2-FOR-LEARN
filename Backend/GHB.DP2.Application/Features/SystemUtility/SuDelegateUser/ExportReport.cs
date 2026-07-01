namespace GHB.DP2.Application.Features.SystemUtility.SuDelegateUser;

using System.Reflection;
using ClosedXML.Excel;
using GHB.DP2.Application.Features.SystemUtility.SuDelegateUser.Abstract;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using GHB.DP2.Application.Services;

public class ExportListSuDelegateUserRequest
{
    public string? Department { get; init; }

    public DateTimeOffset? DelegatorStartDate { get; init; }

    public DateTimeOffset? DelegatorEndDate { get; init; }

    public string? DelegatorName { get; init; }

    public string? DelegatorPositionName { get; init; }

    public string? DelegateeName { get; init; }

    public string? DelegateePositionName { get; init; }
}

public record ExportDelegateeListDto(
    DelegatorId Id,
    string DelegatorName,
    string DelegatorPositionName,
    DateTimeOffset DelegatorStartDate,
    DateTimeOffset DelegatorEndDate,
    string? DelegateeName,
    string? DelegateePositionName,
    string? UpdateBy,
    DateTimeOffset? UpdatedAt);

public class ExportListSuDelegateUser : SuDelegateUserEndpointBase<ExportListSuDelegateUserRequest, Results<Ok<byte[]>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public ExportListSuDelegateUser(
        Dp2DbContext dbContext,
        IPermissionValidationService permissionValidationService,
        ILogger<ExportListSuDelegateUser> logger)
        : base(dbContext, permissionValidationService, logger)
    {
        this.dbContext = dbContext;
    }

    protected override string? GetProgramPath() => "/st/st001";

    public override void Configure()
    {
        this.Options(x => x.WithTags("SuDelegateUser"));
        this.Get("/st/st001/export");
        this.AuditLog("กำหนดผู้รับมอบหมาย", "รายงานผู้รับมอบหมาย");
    }

    protected override async ValueTask<Results<Ok<byte[]>, NotFound<string>>> HandleRequestAsync(ExportListSuDelegateUserRequest req, CancellationToken ct)
    {
        var startDate = DateTimeOffset.UtcNow;

        if (req.DelegatorStartDate.HasValue)
        {
            startDate = req.DelegatorStartDate.Value.AddHours(-23).AddMinutes(-59);
        }

        var endTime = DateTimeOffset.UtcNow;

        if (req.DelegatorEndDate.HasValue)
        {
            endTime = req.DelegatorEndDate.Value.AddHours(23).AddMinutes(59);
        }

        var listData = await this.dbContext.SuDelegatees
                                 .Include(d => d.SuDelegator)
                                 .ThenInclude(auditableEntity => auditableEntity.AuditInfo)
                                 .Include(d => d.RawBusinessUnit)
                                 .WhereIfTrue(
                                     !string.IsNullOrWhiteSpace(req.Department),
                                     d => d.RawBusinessUnit.OrganizationLevel == req.Department)
                                 .WhereIfTrue(
                                     req.DelegatorStartDate != null,
                                     d => d.SuDelegator.DelegationStartDate >= startDate)
                                 .WhereIfTrue(
                                     req.DelegatorEndDate != null,
                                     d => d.SuDelegator.DelegationEndDate <= endTime)
                                 .WhereIfTrue(
                                     !string.IsNullOrWhiteSpace(req.DelegatorName),
                                     d => EF.Functions.ILike(d.SuDelegator.UserFullName, $"%{req.DelegatorName}%"))
                                 .WhereIfTrue(
                                     !string.IsNullOrWhiteSpace(req.DelegatorPositionName),
                                     d => EF.Functions.ILike(d.SuDelegator.FullPositionName, $"%{req.DelegatorPositionName}%"))
                                 .WhereIfTrue(
                                     !string.IsNullOrWhiteSpace(req.DelegateeName),
                                     d => EF.Functions.ILike(d.UserFullName, $"%{req.DelegateeName}%"))
                                 .WhereIfTrue(
                                     !string.IsNullOrWhiteSpace(req.DelegateePositionName),
                                     d => EF.Functions.ILike(d.FullPositionName, $"%{req.DelegateePositionName}%"))
                                 .OrderByDescending(d => d.SuDelegator.DelegationStartDate)
                                 .ThenBy(d => d.BusinessUnitId)
                                 .ThenBy(d => d.Sequence)
                                 .ToListAsync(ct);

        var result = listData.Select(d => new ExportDelegateeListDto(
            d.SuDelegator.Id,
            d.SuDelegator.UserFullName,
            d.SuDelegator.FullPositionName,
            d.SuDelegator.DelegationStartDate,
            d.SuDelegator.DelegationEndDate,
            d.UserFullName,
            d.FullPositionName ?? string.Empty,
            d.SuDelegator.AuditInfo.CreatedByName,
            d.SuDelegator.AuditInfo.CreatedAt));

        var currentUserId = this.GetUserIdFromClaims();
        var currentUser = currentUserId.HasValue
            ? await this.dbContext.SuUsers
                        .Include(u => u.Employee)
                        .Where(u => u.Id == currentUserId.Value)
                        .SingleOrDefaultAsync(ct)
            : null;

        var bytes = ExportExcelFile.ExportToExcel(result, currentUser?.FullName ?? "System");

        return TypedResults.Ok(bytes);
    }
}

public static class ExportExcelFile
{
    public static byte[] ExportToExcel(IEnumerable<ExportDelegateeListDto> exportDelegatees, string exportBy = "System")
    {
        var asmLocation = Assembly.GetExecutingAssembly().Location;
        var executingDirectory = Path.GetDirectoryName(asmLocation)!;
        var templateFile = Path.Combine(executingDirectory, "Templates", "DelegateUserReport.xlsx");

        using (var workbook = new XLWorkbook(templateFile))
        {
            var worksheet = workbook.Worksheet("delegate");

            var startRow = 6;
            var sequence = 1;

            var datetimeNow = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).AddYears(543).ToString("dd/MM/yyyy HH:mm:ss");

            worksheet.Cell(3, 9).Value = exportBy;
            worksheet.Cell(4, 9).Value = datetimeNow;

            foreach (var delegator in exportDelegatees)
            {
                worksheet.Cell(startRow, 1).Value = sequence++;
                worksheet.Cell(startRow, 2).Value = delegator.DelegatorName;
                worksheet.Cell(startRow, 3).Value = delegator.DelegatorPositionName;
                worksheet.Cell(startRow, 4).Value = delegator.DelegateeName;
                worksheet.Cell(startRow, 5).Value = delegator.DelegateePositionName;
                worksheet.Cell(startRow, 6).Value = delegator.DelegatorStartDate.ToOffset(TimeSpan.FromHours(7)).AddYears(543).ToString("dd/MM/yyyy");
                worksheet.Cell(startRow, 7).Value = delegator.DelegatorEndDate.ToOffset(TimeSpan.FromHours(7)).AddYears(543).ToString("dd/MM/yyyy");
                worksheet.Cell(startRow, 8).Value = delegator.UpdateBy;
                worksheet.Cell(startRow, 9).Value = delegator.UpdatedAt?.ToOffset(TimeSpan.FromHours(7)).AddYears(543).ToString("dd/MM/yyyy HH:mm:ss");

                startRow++;
            }

            var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return stream.ToArray();
        }
    }
}