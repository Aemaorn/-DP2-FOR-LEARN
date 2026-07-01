namespace GHB.DP2.Application.Features.ContractManagement.DeliveryAcceptance;

using GHB.DP2.Application.Features.ContractManagement.DeliveryAcceptance.Abstract;
using GHB.DP2.Domain.ContractManagement.CmDeliveryAcceptance;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record UpdateManualDeliveryAcceptanceRequest(
    Guid Id,
    string? DepartmentId,
    string? SupplyMethodCode,
    string? SupplyMethodTypeCode,
    string? SupplyMethodSpecialTypeCode,
    string? Name,
    decimal? Budget,
    bool? IsCommercialMaterial);

public class UpdateManualDeliveryAcceptanceEndpoint
    : DeliveryAcceptanceEndpointBase<UpdateManualDeliveryAcceptanceRequest, Results<NoContent, NotFound<string>, BadRequest<string>>>
{
    public UpdateManualDeliveryAcceptanceEndpoint(
        Dp2DbContext dbContext,
        ILogger<UpdateManualDeliveryAcceptanceEndpoint> logger)
        : base(dbContext, logger)
    {
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("ContractManagement/DeliveryAcceptance")
             .WithName("UpdateManualDeliveryAcceptance")
             .Produces<NoContent>()
             .Produces<string>(StatusCodes.Status404NotFound)
             .Produces<string>(StatusCodes.Status400BadRequest));
        this.Put("delivery-acceptance/{Id:guid}/manual");
    }

    protected override async ValueTask<Results<NoContent, NotFound<string>, BadRequest<string>>> HandleRequestAsync(
        UpdateManualDeliveryAcceptanceRequest req,
        CancellationToken ct)
    {
        var entity = await this.GetById(CmDeliveryAcceptanceId.From(req.Id), ct);

        if (entity.SourceType != SourceType.Manual)
        {
            return TypedResults.BadRequest("ไม่สามารถแก้ไขได้ เนื่องจากไม่ใช่เอกสารที่สร้างแบบไม่อ้างอิง");
        }

        entity.UpdateManual(
            !string.IsNullOrWhiteSpace(req.DepartmentId) ? BusinessUnitId.From(req.DepartmentId) : null,
            !string.IsNullOrWhiteSpace(req.SupplyMethodCode) ? ParameterCode.From(req.SupplyMethodCode) : null,
            !string.IsNullOrWhiteSpace(req.SupplyMethodTypeCode) ? ParameterCode.From(req.SupplyMethodTypeCode) : null,
            !string.IsNullOrWhiteSpace(req.SupplyMethodSpecialTypeCode) ? ParameterCode.From(req.SupplyMethodSpecialTypeCode) : null,
            req.Name,
            req.Budget,
            req.IsCommercialMaterial);

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }
}
