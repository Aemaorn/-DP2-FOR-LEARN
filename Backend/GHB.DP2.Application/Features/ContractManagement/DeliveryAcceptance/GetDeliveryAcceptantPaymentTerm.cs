namespace GHB.DP2.Application.Features.ContractManagement.DeliveryAcceptance;

using GHB.DP2.Domain.ContractManagement.CmDeliveryAcceptance;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetDeliveryAcceptancePaymentTermListRequest(
    Guid ContractDraftVendorId);

public record DeliveryAcceptancePaymentTermDto(
    Guid Id,
    int? InstallmentNo,
    string? MigoNumber,
    DateTimeOffset? ReceiveDate,
    decimal Amount);

public record DeliveryDto(
    DateTimeOffset? DeliveryDate,
    string? ConsiderationResult,
    List<DeliveryItemDto> Items);

public record DeliveryItemDto(
    string Description,
    int Quantity,
    decimal Price,
    decimal Total);

public class GetDeliveryAcceptancePaymentTermEndpoint
    : EndpointBase<
        GetDeliveryAcceptancePaymentTermListRequest,
        Ok<List<DeliveryAcceptancePaymentTermDto>>>
{
    private readonly Dp2DbContext dbContext;

    public GetDeliveryAcceptancePaymentTermEndpoint(
        Dp2DbContext dbContext,
        ILogger<GetDeliveryAcceptancePaymentTermEndpoint> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("ContractManagement/DeliveryAcceptance")
             .WithName("GetDeliveryAcceptancePaymentTerm")
             .Produces<Ok>());
        this.Get("contract/{ContractDraftVendorId:guid}/delivery-acceptance/payment-term");
    }

    protected override async ValueTask<Ok<List<DeliveryAcceptancePaymentTermDto>>> HandleRequestAsync(
        GetDeliveryAcceptancePaymentTermListRequest req,
        CancellationToken ct)
    {
        var deliveryAcceptance = await this.dbContext.CmDeliveryAcceptances
                                           .Include(x => x.Periods)
                                           .Include(cmDeliveryAcceptance => cmDeliveryAcceptance.Periods).Include(cmDeliveryAcceptance => cmDeliveryAcceptance.Periods)
                                           .ThenInclude(auditableEntity => auditableEntity.AuditInfo)
                                           .FirstOrDefaultAsync(
                                               cancellationToken: ct);

        if (deliveryAcceptance == null)
        {
            this.ThrowError(
                $"ไม่พบรายการส่งมอบตรวจรับที่ตรวจรับแล้ว",
                StatusCodes.Status404NotFound);
        }

        var result = deliveryAcceptance.Periods
                                       .Where(w => w.Status == CmDeliveryAcceptancePeriodStatus.Approved)
                                       .OrderBy(x => x.AuditInfo.CreatedAt)
                                       .Select((period, index) => new DeliveryAcceptancePaymentTermDto(
                                           Id: period.Id.Value,
                                           InstallmentNo: null,
                                           MigoNumber: string.Empty,
                                           ReceiveDate: period.AcceptanceDate,
                                           Amount: period.AcceptedAmount ?? 0)).ToList();

        return TypedResults.Ok(result);
    }
}