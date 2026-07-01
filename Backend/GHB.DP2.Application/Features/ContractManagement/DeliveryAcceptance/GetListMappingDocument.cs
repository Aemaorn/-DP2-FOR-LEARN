namespace GHB.DP2.Application.Features.ContractManagement.DeliveryAcceptance;

using GHB.DP2.Application.Extensions.Document;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public class GetListMappingDeliveryAcceptanceDocumentEndpoint : EndpointBase<Ok<ResponseDtoDescriptionExtension>>
{
    public GetListMappingDeliveryAcceptanceDocumentEndpoint(ILogger<GetListMappingDeliveryAcceptanceDocumentEndpoint> logger)
        : base(logger)
    {
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("ContractManagement/DeliveryAcceptance"));
        this.Get("delivery-acceptance/mapping-document");
    }

    protected override async ValueTask<Ok<ResponseDtoDescriptionExtension>> HandleRequestAsync(CancellationToken ct)
    {
        await Task.CompletedTask;

        var dtoType = typeof(GetById.GetDeliveryAcceptanceByIdResponse);

        var result = DtoDescriptionExtensions.ToDictionary(dtoType);

        return TypedResults.Ok(result);
    }
}