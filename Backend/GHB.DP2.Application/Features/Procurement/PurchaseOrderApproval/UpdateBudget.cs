namespace GHB.DP2.Application.Features.Procurement.PurchaseOrderApproval;

using GHB.DP2.Domain.Procurement.PPurchaseOrderApproval;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public record UpdatePurchaseOrderApprovalBudgetRequest(
    Guid BudgetId,
    string Description,
    decimal BudgetAmount);

public class UpdateBudgetEndpoint : EndpointBase<UpdatePurchaseOrderApprovalBudgetRequest, Created<Guid>>
{
    private readonly Dp2DbContext dbContext;

    public UpdateBudgetEndpoint(ILogger<UpdateBudgetEndpoint> logger, Dp2DbContext dbContext)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Put("procurement/update-purchase-order-approval/budget/{BudgetId:guid}");
    }

    protected override async ValueTask<Created<Guid>> HandleRequestAsync(UpdatePurchaseOrderApprovalBudgetRequest req, CancellationToken ct)
    {
        var entity = await this.dbContext.PPurchaseOrderApprovalBudgets
            .FirstOrDefaultAsync(
                x => x.Id == PPurchaseOrderApprovalBudgetId.From(req.BudgetId),
                ct);

        if (entity is null)
        {
            this.ThrowError(
               $"ไม่พบข้อมูลที่มีรหัส {req.BudgetId}",
               StatusCodes.Status404NotFound);
        }

        entity.Update(
            req.Description.Trim(),
            req.BudgetAmount);

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Created(string.Empty, entity.Id.Value);
    }
}
