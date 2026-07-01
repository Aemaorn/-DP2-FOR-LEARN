namespace GHB.DP2.Application.Features.Procurement.PurchaseOrderApproval;

using GHB.DP2.Domain.Procurement.PPurchaseOrderApproval;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record DeletePurchaseOrderApprovalBudgetRequest(
    Guid Id);

public class DeleteBudget : EndpointBase<DeletePurchaseOrderApprovalBudgetRequest, Results<NoContent, NotFound<string>, Conflict<string>>>
{
    private readonly Dp2DbContext dbContext;

    public DeleteBudget(
        Dp2DbContext dbContext,
        ILogger<DeleteBudget> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Delete("procurement/delete-purchase-order-approval/Budget/{Id:guid}");
    }

    protected override async ValueTask<Results<NoContent, NotFound<string>, Conflict<string>>> HandleRequestAsync(DeletePurchaseOrderApprovalBudgetRequest req, CancellationToken ct)
    {
        var budget = await this.dbContext.PPurchaseOrderApprovalBudgets
                               .Include(a => a.PPurchaseOrderApproval)
                               .ThenInclude(c => c.PurchaseOrderApprovalEntrepreneurs)
                               .FirstOrDefaultAsync(w => w.Id == PPurchaseOrderApprovalBudgetId.From(req.Id), ct);

        if (budget is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูล");
        }

        this.dbContext.PPurchaseOrderApprovalBudgets.Remove(budget);
        await this.dbContext.SaveChangesAsync(ct);

        if (budget.PPurchaseOrderApproval is not null)
        {
            await this.RemoveContractAsync(budget.PPurchaseOrderApproval, ct);
        }

        return TypedResults.NoContent();
    }

    private async Task RemoveContractAsync(PPurchaseOrderApproval purchaseOrderApproval, CancellationToken ct)
    {
        if (purchaseOrderApproval is not null && purchaseOrderApproval.PurchaseOrderApprovalEntrepreneurs.Any())
        {
            foreach (var toRemove in purchaseOrderApproval.PurchaseOrderApprovalEntrepreneurs)
            {
                if (toRemove is not null)
                {
                    this.dbContext.PPurchaseOrderApprovalEntrepreneurs.Remove(toRemove);
                }
            }

            await this.dbContext.SaveChangesAsync(ct);
        }
    }
}
