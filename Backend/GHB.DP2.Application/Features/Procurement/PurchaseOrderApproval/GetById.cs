namespace GHB.DP2.Application.Features.Procurement.PurchaseOrderApproval;

using System.IdentityModel.Tokens.Jwt;
using GHB.DP2.Application.Features.Procurement.PurchaseOrderApproval.Abstract;
using GHB.DP2.Application.Features.Procurement.PurchaseOrderApproval.Dto;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PPurchaseOrderApproval;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetByIdPurchaseOrderApprovalRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)] Guid UserId,
    Guid ProcurementId,
    Guid? Id);

public class GetByIdPurchaseOrderApprovalEndpoint : PurchaseOrderApprovalEndpointBase<GetByIdPurchaseOrderApprovalRequest, Results<Ok<PurchaseOrderApprovalResponseDto>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public GetByIdPurchaseOrderApprovalEndpoint(ILogger<GetByIdPurchaseOrderApprovalEndpoint> logger, Dp2DbContext dbContext)
        : base(logger, dbContext)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Get("procurement/{procurementId:guid}/purchase-order-approval/{id:guid?}");
        this.Description(b => b
                              .WithTags("Procurement/PurchaseOrderApproval")
                              .WithName("GetPurchaseOrderApprovalById")
                              .Produces<PurchaseOrderApprovalResponseDto>(StatusCodes.Status200OK)
                              .Produces<string>(StatusCodes.Status404NotFound));
    }

    protected override async ValueTask<Results<Ok<PurchaseOrderApprovalResponseDto>, NotFound<string>>> HandleRequestAsync(GetByIdPurchaseOrderApprovalRequest req, CancellationToken ct)
    {
        var procurement = await this.dbContext.Procurements
                                    .SingleOrDefaultAsync(f => f.Id == ProcurementId.From(req.ProcurementId), ct);

        if (procurement is null)
        {
            this.ThrowError(
                "ไม่พบข้อมูลจัดซื้อจัดจ้าง",
                StatusCodes.Status404NotFound);
        }

        if (req.Id is null)
        {
            var contractData = await this.InitialData(procurement, UserId.From(req.UserId), ct);

            return TypedResults.Ok(contractData);
        }

        var approval = await this.GetDataById(PurchaseOrderApprovalId.From(req.Id.Value), UserId.From(req.UserId), ct);

        return TypedResults.Ok(approval);
    }
}