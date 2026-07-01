namespace GHB.DP2.Application.Features.Procurement.Procurement;

using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PPrincipleApproval;
using GHB.DP2.Domain.Procurement.PPrincipleApprovalRental;
using GHB.DP2.Domain.Procurement.PpAppoint;
using GHB.DP2.Domain.Procurement.PpPurchaseRequisition;
using GHB.DP2.Domain.Procurement.PPurchaseOrderApproval;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record DeleteProcurementRequest(
    Guid Id);

public class DeleteProcurement : EndpointBase<DeleteProcurementRequest, Results<NoContent, NotFound<string>, Conflict<string>>>
{
    private readonly Dp2DbContext dbContext;

    public DeleteProcurement(
        Dp2DbContext dbContext,
        ILogger<DeleteProcurement> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags(nameof(Procurement))
             .WithName("DeleteProcurement")
             .Produces<NoContent>()
             .Produces<NotFound>());
        this.Delete("procurement/{Id:guid}");
    }

    protected override async ValueTask<Results<NoContent, NotFound<string>, Conflict<string>>> HandleRequestAsync(DeleteProcurementRequest req, CancellationToken ct)
    {
        var procurementId = ProcurementId.From(req.Id);

        var data = await this.dbContext.Procurements
                             .SingleOrDefaultAsync(w => w.Id == procurementId, ct);

        if (data is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลการจัดซื้อจัดจ้าง");
        }

        var appointStatus = await this.dbContext.PpAppoints
            .Where(a => a.ProcurementId == procurementId && a.IsActive)
            .Select(a => (AppointStatus?)a.Status)
            .FirstOrDefaultAsync(ct);

        var purchaseRequisitionStatus = await this.dbContext.PpPurchaseRequisitions
            .Where(pr => pr.ProcurementId == procurementId && !pr.IsDeleted)
            .Select(pr => (PurchaseRequisitionStatus?)pr.Status)
            .FirstOrDefaultAsync(ct);

        var purchaseOrderApprovalStatus = await this.dbContext.PPurchaseOrderApprovals
            .Where(poa => poa.ProcurementId == procurementId && !poa.IsDeleted)
            .Select(poa => (PurchaseOrderApprovalStatus?)poa.Status)
            .FirstOrDefaultAsync(ct);

        var principleApprovalStatus = await this.dbContext.PPrincipleApprovals
            .Where(pa => pa.ProcurementId == procurementId)
            .Select(pa => (PPrincipleApprovalStatus?)pa.Status)
            .FirstOrDefaultAsync(ct);

        var principleApprovalRentalStatus = await this.dbContext.PPrincipleApprovalRentals
            .Where(par => par.ProcurementId == procurementId)
            .Select(par => (PPrincipleApprovalRentalStatus?)par.Status)
            .FirstOrDefaultAsync(ct);

        var canDelete = data.Status is ProcurementStatus.Cancelled ||
            (appointStatus, purchaseRequisitionStatus, purchaseOrderApprovalStatus, principleApprovalStatus, principleApprovalRentalStatus) switch
            {
                ({ } status, _, _, _, _) => status is AppointStatus.Draft or AppointStatus.Edit or AppointStatus.Rejected,
                (_, { } status, _, _, _) => status is PurchaseRequisitionStatus.Draft or PurchaseRequisitionStatus.Edit or PurchaseRequisitionStatus.Rejected,
                (_, _, { } status, _, _) => status is PurchaseOrderApprovalStatus.Draft or PurchaseOrderApprovalStatus.Edit or PurchaseOrderApprovalStatus.Rejected,
                (_, _, _, { } status, _) => status is PPrincipleApprovalStatus.Draft or PPrincipleApprovalStatus.Edit or PPrincipleApprovalStatus.Rejected,
                (_, _, _, _, { } status) => status is PPrincipleApprovalRentalStatus.Draft or PPrincipleApprovalRentalStatus.Edit or PPrincipleApprovalRentalStatus.Rejected,
                _ => data.Status is ProcurementStatus.Draft or ProcurementStatus.InProgress,
            };

        if (!canDelete)
        {
            return TypedResults.Conflict("ไม่สามารถลบข้อมูลการจัดซื้อจัดจ้างที่กำลังดำเนินการแล้วได้");
        }

        this.dbContext.Procurements.Remove(data);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }
}