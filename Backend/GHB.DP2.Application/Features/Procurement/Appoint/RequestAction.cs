namespace GHB.DP2.Application.Features.Procurement.Appoint;

using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.Appoint.Abstract;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PpAppoint;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record RequestActionAppointRequest(
    Guid Id,
    bool IsEdit,
    bool IsCancel,
    string Reason);

public class RequestActionAppointEndpoint : AppointEndpointBase<RequestActionAppointRequest, Results<Created<Guid>, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public RequestActionAppointEndpoint(
        Dp2DbContext dbContext,
        IOperationService operationService,
        ICommandTextService commandTextService,
        ILogger<RequestActionAppointEndpoint> logger)
        : base(dbContext, logger, operationService, commandTextService)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("Procurement/Appoint"));
        this.Post("appointments/{id}/request-action");
    }

    protected override async ValueTask<Results<Created<Guid>, NotFound<string>, BadRequest<string>>> HandleRequestAsync(RequestActionAppointRequest request, CancellationToken ct)
    {
        var appointData = await this.dbContext.PpAppoints
                                    .AsNoTracking()
                                    .Include(x => x.TorDraftCommittees)
                                    .Include(x => x.TorDraftCommittees)
                                    .Include(x => x.TorDraftCommitteeDuties)
                                    .Include(x => x.MedianPriceCommittees)
                                    .Include(x => x.MedianPriceCommitteeDuties)
                                    .Include(x => x.Acceptors)
                                    .Include(ppAppoint => ppAppoint.Procurement)
                                    .SingleOrDefaultAsync(x => x.Id == PpAppointId.From(request.Id) && x.IsActive, ct);

        if (appointData == null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูล");
        }

        if (appointData.Status != AppointStatus.Approved)
        {
            return TypedResults.BadRequest("สถานะไม่ถูกต้อง");
        }

        var cloneData = appointData.Clone(request.IsEdit, request.IsCancel, request.Reason);

        if (request.IsCancel)
        {
            appointData.SetCancelReason(request.Reason);
            appointData.Procurement.SetCancelledProcurement();
        }

        if (request.IsEdit)
        {
            appointData.SetChangeReason(request.Reason);
        }

        appointData.SetActive(false);
        appointData.Procurement.SetProcessType(ProcessType.Appoint);

        await this.SetDefaultDocumentTemplate(cloneData, appointData.Procurement.SupplyMethodCode, appointData.Procurement.Budget, ct, request.IsEdit, request.IsCancel);

        this.dbContext.PpAppoints.Add(cloneData);
        this.dbContext.PpAppoints.Update(appointData);

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Created(string.Empty, cloneData.Id.Value);
    }
}