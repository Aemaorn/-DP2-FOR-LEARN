namespace GHB.DP2.Application.Features.Report.AuditAndRevenue;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Extensions;
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

public record RejectAuditAndRevenueRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid Id,
    string? Remark
);

public class RejectAuditAndRevenueEndpoint : AuditAndRevenueEndpoint<RejectAuditAndRevenueRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public RejectAuditAndRevenueEndpoint(ILogger<RejectAuditAndRevenueEndpoint> logger, Dp2DbContext dbContext, IFileServiceClient fileServiceClient, IOperationService operationService)
        : base(logger, dbContext, fileServiceClient, operationService)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Post("report/audit-revenue/{id:guid}/reject");
        this.Description(b => b
                              .WithTags("Report/AuditAndRevenue")
                              .WithName("RejectAuditAndRevenue")
                              .WithSummary("ปฏิเสธรายงานการตรวจสอบและรายได้")
                              .AllowAnonymous()
                              .Produces(StatusCodes.Status200OK)
                              .Produces<string>(StatusCodes.Status404NotFound)
                              .Produces<string>(StatusCodes.Status400BadRequest));
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(RejectAuditAndRevenueRequest req, CancellationToken ct)
    {
        var entity = await this.dbContext.RpAuditAndRevenues
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
                               .FirstOrDefaultAsync(x => x.Id == RpAuditAndRevenueId.From(req.Id), ct);

        if (entity is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลรายงานรหัส {req.Id}");
        }

        if (entity.Status != RpAuditAndRevenueStatus.WaitingApproval)
        {
            return TypedResults.BadRequest("ปฏิเสธได้เฉพาะสถานะ รอผู้มีอำนาจเห็นชอบ/อนุมัตื เท่านั้น");
        }

        var acceptors = entity.Acceptors
                              .Where(a => a.IsActive)
                              .OrderBy(a => a.Sequence)
                              .Select(DelegatorExtensions.DelegatorToAcceptor)
                              .ToList();

        var current = acceptors.FirstOrDefault(a => a.Delegatee?.SuUserId == null
                                                            ? a.UserId == req.UserId
                                                            : a.Delegatee?.SuUserId == UserId.From(req.UserId));

        if (current == null)
        {
            return TypedResults.BadRequest("ไม่พบผู้อนุมัติในกลุ่มหรือขั้นตอนนี้");
        }

        var currentAcceptorUser = entity.Acceptors.FirstOrDefault(a => a.Id == current.Id);

        if (currentAcceptorUser is null)
        {
            this.ThrowError(
                "ไม่พบผู้อนุมัติที่ใช้งานได้",
                StatusCodes.Status400BadRequest);
        }

        currentAcceptorUser
            .SetDelegatee(current.DelegateeId)
            .Reject(remark: req.Remark);

        entity.SetStatus(RpAuditAndRevenueStatus.Rejected, req.Remark);

        var allDocumentTypes = new[]
        {
            RpAuditAndRevenueDocumentType.AuditReport,
            RpAuditAndRevenueDocumentType.AuditGeneralReport,
            RpAuditAndRevenueDocumentType.RevenueReport,
        };

        foreach (var docType in allDocumentTypes)
        {
            await this.ManageDocumentForRejectAsync(entity, docType);
        }

        this.dbContext.RpAuditAndRevenues.Update(entity);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }
}