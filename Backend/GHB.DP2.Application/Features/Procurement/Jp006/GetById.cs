namespace GHB.DP2.Application.Features.Procurement.Jp006;

using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.Jp006.Abstract;
using GHB.DP2.Application.Features.Procurement.Jp006.Dto;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PPurchaseOrder;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record GetJp006ByIdRequest(
    Guid ProcurementId,
    Guid? Jp006Id);

public class GetJp006ByIdEndpoint : Jp006EndpointBase<GetJp006ByIdRequest, Results<Ok<GetJp006ByIdResponse>, NotFound<string>>>
{
    public GetJp006ByIdEndpoint(
        ILogger<AssigneePurchaseOrderEndpoint> logger,
        IOperationService operationService,
        ICommandTextService commandTextService,
        IFileServiceClient fileServiceClient,
        Dp2DbContext dbContext)
        : base(logger, operationService, commandTextService, fileServiceClient, dbContext)
    {
    }

    public override void Configure()
    {
        this.Get("procurement/{ProcurementId:guid}/jp006/{Jp006Id:guid?}");
        this.Description(b => b
                              .WithTags(nameof(Jp006))
                              .WithName("GetPJp006")
                              .Produces<Created<Guid>>(StatusCodes.Status201Created)
                              .ProducesProblem(StatusCodes.Status400BadRequest)
                              .ProducesProblem(StatusCodes.Status500InternalServerError));
    }

    protected override async ValueTask<Results<Ok<GetJp006ByIdResponse>, NotFound<string>>> HandleRequestAsync(GetJp006ByIdRequest req, CancellationToken ct)
    {
        var query =
            req.Jp006Id.IsNull()
                ? this.GetByProcurementIdAsync(ProcurementId.From(req.ProcurementId), ct)
                : this.GetByIdAsync(
                    ProcurementId.From(req.ProcurementId),
                    PurchaseOrderId.From(req.Jp006Id.Value),
                    ct: ct).Map(this.MapPJp006);

        var result = await query;

        if (result is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลรายการจัดซื้อจัดจ้างที่ระบุ");
        }

        return TypedResults.Ok(result);
    }
}