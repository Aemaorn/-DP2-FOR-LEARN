namespace GHB.DP2.Application.Features.Dashboard.ProcurementProgress;

using System.IdentityModel.Tokens.Jwt;
using GHB.DP2.Domain.Dashboard;
using GHB.DP2.Domain.Plan;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record UpsertProcurementProgressRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid PlanId,
    DateOnly? PlanDate,
    DateOnly? PurchaseOrderDate,
    DateOnly? DocPrepareNotifyDate,
    DateOnly? ContractDate,
    ProcurementProgressStatus? Status);

public record UpsertProcurementProgressResponse(Guid SummaryId);

public class UpsertProcurementProgress
    : EndpointBase<UpsertProcurementProgressRequest, Ok<UpsertProcurementProgressResponse>>
{
    private readonly Dp2DbContext dbContext;

    public UpsertProcurementProgress(Dp2DbContext dbContext, ILogger<UpsertProcurementProgress> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("Dashboard")
             .WithName("UpsertProcurementProgress")
             .Produces<Ok>());
        this.Post("dashboard/procurement-progress/upsert");
    }

    protected override async ValueTask<Ok<UpsertProcurementProgressResponse>> HandleRequestAsync(
        UpsertProcurementProgressRequest req,
        CancellationToken ct)
    {
        var planId = PlanId.From(req.PlanId);

        var existing = await this.dbContext.ProcurementProgressSummaries
            .FirstOrDefaultAsync(s => s.PlanId == planId, ct);

        var userName = "System";
        var userId = UserId.From(req.UserId);
        var user = await this.dbContext.SuUsers
            .Include(u => u.Employee)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user?.Employee is not null)
        {
            userName = user.Employee.View?.FullName ?? userName;
        }

        if (existing is null)
        {
            existing = ProcurementProgressSummary.Create(
                planId,
                req.PlanDate,
                req.PurchaseOrderDate,
                req.DocPrepareNotifyDate,
                req.ContractDate);
            existing.Update(req.PlanDate, req.PurchaseOrderDate, req.DocPrepareNotifyDate, req.ContractDate, req.Status);
            existing.Update(req.UserId, userName);
            await this.dbContext.ProcurementProgressSummaries.AddAsync(existing, ct);
        }
        else
        {
            existing.Update(
                req.PlanDate,
                req.PurchaseOrderDate,
                req.DocPrepareNotifyDate,
                req.ContractDate,
                req.Status);
            existing.Update(req.UserId, userName);
        }

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok(new UpsertProcurementProgressResponse(existing.Id.Value));
    }
}
