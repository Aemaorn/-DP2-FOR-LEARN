namespace GHB.DP2.Application.Features.Procurement.PurchaseOrderApproval;

using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.Procurement.PurchaseOrderApproval.Dto;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public class GetListMappingPurchaseOrderApprovalDocumentEndpoint : EndpointBase<Ok<ResponseDtoDescriptionExtension>>
{
    public GetListMappingPurchaseOrderApprovalDocumentEndpoint(ILogger<GetListMappingPurchaseOrderApprovalDocumentEndpoint> logger)
        : base(logger)
    {
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("Procurement/PurchaseOrderApproval"));
        this.Get("procurement/purchase-order-approval/mapping-document");
    }

    protected override async ValueTask<Ok<ResponseDtoDescriptionExtension>> HandleRequestAsync(CancellationToken ct)
    {
        await Task.CompletedTask;

        var dtoType = typeof(PurchaseOrderApprovalResponseDto);

        var result = DtoDescriptionExtensions.ToDictionary(dtoType);

        return TypedResults.Ok(result);
    }
}