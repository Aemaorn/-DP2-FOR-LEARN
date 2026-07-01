namespace GHB.DP2.Application.Features.Procurement.PurchaseOrderApproval;

using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PPurchaseOrderApproval;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record CreatePurchaseOrderApprovalBudgetRequest(
    Guid ProcurementId,
    Guid ApprovalId,
    string Description,
    decimal BudgetAmount);

public class CreateBudgetEndpoint : EndpointBase<CreatePurchaseOrderApprovalBudgetRequest, Created<Guid>>
{
    private readonly Dp2DbContext dbContext;

    public CreateBudgetEndpoint(ILogger<CreateBudgetEndpoint> logger, Dp2DbContext dbContext)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Post("procurement/{ProcurementId:guid}/create-purchase-order-approval/{ApprovalId:guid}/budget");
    }

    protected override async ValueTask<Created<Guid>> HandleRequestAsync(CreatePurchaseOrderApprovalBudgetRequest req, CancellationToken ct)
    {
        var approval = await this.dbContext.PPurchaseOrderApprovals
            .FirstOrDefaultAsync(
                x => x.Id == PurchaseOrderApprovalId.From(req.ApprovalId)
                     && x.ProcurementId == ProcurementId.From(req.ProcurementId),
                ct);

        if (approval is null)
        {
            this.ThrowError(
               $"ไม่พบข้อมูลที่มีรหัส {req.ProcurementId}",
               StatusCodes.Status404NotFound);
        }

        var lastBudget = await this.dbContext.PPurchaseOrderApprovalBudgets
            .Where(x => x.PPurchaseOrderApproval.Id == approval.Id)
            .OrderByDescending(x => x.Sequence)
            .FirstOrDefaultAsync(ct);

        var nextSequence = (lastBudget?.Sequence ?? 0) + 1;

        var entity = PPurchaseOrderApprovalBudget.Create(
            approval,
            nextSequence,
            req.Description.Trim(),
            req.BudgetAmount);

        this.dbContext.PPurchaseOrderApprovalBudgets.Add(entity);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Created(string.Empty, entity.Id.Value);
    }
}
