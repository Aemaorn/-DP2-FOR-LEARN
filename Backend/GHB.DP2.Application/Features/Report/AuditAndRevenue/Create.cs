namespace GHB.DP2.Application.Features.Report.AuditAndRevenue;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client.Abstractions;
using FluentValidation;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Report.AuditAndRevenue.Abstract;
using GHB.DP2.Domain.Report.RpAuditAndRevenue;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record CreateReportAuditAndRevenueRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)] Guid UserId,
    RpAuditAndRevenueStatus Status,
    DateTimeOffset DocumentDate,
    DateTimeOffset SignStartDate,
    DateTimeOffset SignEndDate,
    DateTimeOffset DeliveryDate,
    IEnumerable<AuditAndRevenueDetailDto>? Details,
    IEnumerable<AcceptorRequest>? ApprovalAcceptors);

public class CreateContractGuaranteeReturnValidator : Validator<CreateReportAuditAndRevenueRequest>
{
    public CreateContractGuaranteeReturnValidator()
    {
        this.RuleFor(x => x.DocumentDate)
            .NotNull()
            .WithMessage("กรุณาระบุข้อมูลวันที่เอกสาร");

        this.RuleFor(x => x.SignStartDate)
            .NotNull()
            .WithMessage("กรุณาระบุข้อมูลวันที่ลงนามในสัญญาเริ่มต้น");

        this.RuleFor(x => x.SignEndDate)
            .NotNull()
            .WithMessage("กรุณาระบุข้อมูลวันที่ลงนามในสัญญาสิ้นสุด");

        this.When(x => x.Status == RpAuditAndRevenueStatus.WaitingApproval, () =>
        {
            this.RuleFor(x => x.ApprovalAcceptors)
                .NotNull().WithMessage("ต้องระบุผู้มีอำนาจเห็นชอบ/อนุมัติอย่างน้อย 1 คน")
                .Must(a => a != null && a.Any())
                .WithMessage("ต้องระบุผู้มีอำนาจเห็นชอบ/อนุมัติอย่างน้อย 1 คน");

            this.RuleFor(x => x.Details)
                .NotNull().WithMessage("ต้องมีข้อมูลรายการสัญญาอย้างน้อย 1 รายการ")
                .Must(a => a != null && a.Any())
                .WithMessage("ต้องมีข้อมูลรายการสัญญาอย้างน้อย 1 รายการ");
        });
    }
}

public class CreateReportAuditAndRevenueEndpoint : AuditAndRevenueEndpoint<CreateReportAuditAndRevenueRequest, Created<Guid>>
{
    private readonly Dp2DbContext dbContext;

    public CreateReportAuditAndRevenueEndpoint(ILogger<CreateReportAuditAndRevenueEndpoint> logger, Dp2DbContext dbContext, IFileServiceClient fileServiceClient, IOperationService operationService)
        : base(logger, dbContext, fileServiceClient, operationService)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Post("report/audit-revenue");
        this.Description(b => b
                              .WithTags("Report/AuditAndRevenue")
                              .WithName("CreateReportAuditAndRevenue")
                              .WithSummary("สร้างรายงานการตรวจสอบและรายได้")
                              .AllowAnonymous()
                              .Produces<Created<Guid>>(StatusCodes.Status201Created)
                              .ProducesProblem(StatusCodes.Status400BadRequest)
                              .ProducesProblem(StatusCodes.Status500InternalServerError));
    }

    private async Task<string> GenerateRunningNumberAsync(DateTimeOffset documentDate, CancellationToken ct)
    {
        var year = (documentDate.Year + 543) % 100;
        var yearStr = year.ToString("D2");
        var prefix = $"DP{yearStr}";

        var latestDoc = await this.dbContext.RpAuditAndRevenues
            .Where(x => x.DocumentNumber.StartsWith(prefix))
            .OrderByDescending(x => x.DocumentNumber)
            .FirstOrDefaultAsync(ct);

        int nextSeq = 1;
        if (latestDoc != null && latestDoc.DocumentNumber.Length >= prefix.Length + 5)
        {
            var seqStr = latestDoc.DocumentNumber.Substring(prefix.Length, 5);
            if (int.TryParse(seqStr, out var lastSeq))
            {
                nextSeq = lastSeq + 1;
            }
        }

        return $"DP{yearStr}{nextSeq:00000}";
    }

    protected override async ValueTask<Created<Guid>> HandleRequestAsync(CreateReportAuditAndRevenueRequest req, CancellationToken ct)
    {
        var runningNumber = await this.GenerateRunningNumberAsync(req.DocumentDate, ct);

        var entity = RpAuditAndRevenue.Create()
                                      .SetValues(
                                          runningNumber,
                                          req.DocumentDate,
                                          req.SignStartDate,
                                          req.SignEndDate,
                                          req.DeliveryDate)
                                      .SetStatus(req.Status);

        if (req.ApprovalAcceptors != null)
        {
            this.UpsertAcceptors(entity, req.ApprovalAcceptors, req.Status, UserId.From(req.UserId));
        }

        if (req.Details != null)
        {
            this.UpsertDetails(entity, req.Details);
        }

        await this.SetDefaultDocumentTemplate(entity, ct);
        this.dbContext.RpAuditAndRevenues.Add(entity);

        await this.dbContext.SaveChangesAsync(CancellationToken.None);

        // Reload entity with includes needed for document replacement
        var reloaded = await this.dbContext.RpAuditAndRevenues
            .Include(x => x.Details)
            .ThenInclude(d => d.CaContractDraftVendor)
            .ThenInclude(v => v.Vendor)
            .ThenInclude(v => v.VendorInfo)
            .Include(x => x.Details)
            .ThenInclude(d => d.CaContractDraftVendor)
            .ThenInclude(v => v.ContractType)
            .Include(x => x.Acceptors)
            .ThenInclude(a => a.User)
            .ThenInclude(u => u.Employee)
            .Include(x => x.AuditInfo)
            .Include(x => x.DocumentHistories)
            .FirstAsync(x => x.Id == entity.Id, CancellationToken.None);

        var allDocumentTypes = new[]
        {
            RpAuditAndRevenueDocumentType.AuditReport,
            RpAuditAndRevenueDocumentType.AuditGeneralReport,
            RpAuditAndRevenueDocumentType.RevenueReport,
        };

        foreach (var docType in allDocumentTypes)
        {
            await this.ManageDocumentForCreateAsync(reloaded, docType, UserId.From(req.UserId), CancellationToken.None);
        }

        await this.dbContext.SaveChangesAsync(CancellationToken.None);

        return TypedResults.Created(string.Empty, entity.Id.Value);
    }
}