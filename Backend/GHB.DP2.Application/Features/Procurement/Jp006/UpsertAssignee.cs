namespace GHB.DP2.Application.Features.Procurement.Jp006;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.Jp006.Abstract;
using GHB.DP2.Application.Features.Procurement.Jp006.Dto;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PPurchaseOrder;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record UpsertAssigneeRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid ProcurementId,
    Guid Jp006Id,
    IEnumerable<Jp006AssigneeInfo> Assignees);

public class UpsertAssigneeEndpoint : Jp006EndpointBase<UpsertAssigneeRequest, Results<Ok<Guid>, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public UpsertAssigneeEndpoint(
        ILogger<AssigneePurchaseOrderEndpoint> logger,
        IOperationService operationService,
        ICommandTextService commandTextService,
        IFileServiceClient fileServiceClient,
        Dp2DbContext dbContext)
        : base(logger, operationService, commandTextService, fileServiceClient, dbContext)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Put("procurement/{ProcurementId:guid}/jp006/{Jp006Id:guid}/assignee");
        this.Options(b =>
            b.WithTags(nameof(Jp006))
             .WithName("UpsertJp006Assignee")
             .Produces<Ok<Guid>>()
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status500InternalServerError));
    }

    protected override async ValueTask<Results<Ok<Guid>, NotFound<string>, BadRequest<string>>> HandleRequestAsync(UpsertAssigneeRequest req, CancellationToken ct)
    {
        var jp006 =
            await this.dbContext.PJp006S
                      .Include(j => j.Assignees)
                      .SingleOrDefaultAsync(
                          j => j.Id == Domain.Procurement.PPurchaseOrder.PurchaseOrderId.From(req.Jp006Id) &&
                               j.ProcurementId == ProcurementId.From(req.ProcurementId),
                          ct);

        if (jp006 is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลการจัดซื้อจัดจ้างที่ระบุ");
        }

        if (jp006.Status is not PurchaseOrderStatus.WaitingAssign)
        {
            return TypedResults.BadRequest("ไม่สามารถแก้ไขผู้มอบหมายได้ในสถานะปัจจุบัน");
        }

        await this.UpsertAssignee(jp006, req.Assignees, ct, UserId.From(req.UserId));
        this.dbContext.PJp006S.Update(jp006);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok(jp006.Id.Value);
    }
}