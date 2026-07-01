namespace GHB.DP2.Application.Features.ContractManagement.DeliveryAcceptance.Abstract;

using GHB.DP2.Domain.ContractManagement.CmDeliveryAcceptance;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public abstract class DeliveryAcceptanceEndpointBase<TRequest, TResponse>
    : EndpointBase<TRequest, TResponse>
    where TResponse : IResult
    where TRequest : notnull
{
    protected readonly Dp2DbContext dbContext;

    protected DeliveryAcceptanceEndpointBase(
        Dp2DbContext dbContext,
        ILogger logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    protected async Task<CmDeliveryAcceptance> GetById(
        CmDeliveryAcceptanceId deliveryAcceptanceId,
        CancellationToken ct)
    {
        var deliveryAcceptanceExisting =
            await this.dbContext.CmDeliveryAcceptances
                      .Include(da => da.Periods)
                      .ThenInclude(p => p.Acceptors)
                      .Include(da => da.Periods)
                      .ThenInclude(p => p.PaymentTerms)
                      .Include(da => da.Department)
                      .Include(da => da.SupplyMethod)
                      .Include(da => da.SupplyMethodType)
                      .Include(da => da.SupplyMethodSpecialType)
                      .FirstOrDefaultAsync(
                          da => da.Id == deliveryAcceptanceId,
                          ct);

        if (deliveryAcceptanceExisting is null)
        {
            this.ThrowError(
                r => deliveryAcceptanceId,
                $"ไม่พบข้อมูลการส่งมอบและตรวจรับ {deliveryAcceptanceId}.",
                StatusCodes.Status404NotFound);
        }

        return deliveryAcceptanceExisting;
    }
}