namespace GHB.DP2.Application.Features.Procurement.Jp006;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.Jp006.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PPurchaseOrder;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record AssigneeCommentRequest
{
    public Guid ProcurementId { get; init; }

    public Guid Jp006Id { get; init; }

    [FromClaim(JwtRegisteredClaimNames.Sub)]
    public Guid UserId { get; init; }

    public string? Remark { get; init; }
}

public class AssigneePurchaseOrderEndpoint : Jp006EndpointBase<AssigneeCommentRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public AssigneePurchaseOrderEndpoint(
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
        this.Put("procurement/{ProcurementId:guid}/jp006/{Jp006Id:guid}/assignee-comment");
        this.Options(b =>
            b.WithTags(nameof(Jp006))
             .WithName("AssigneeCommentPurchase")
             .Produces<Ok>()
             .ProducesProblem(StatusCodes.Status400BadRequest)
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status500InternalServerError));
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(AssigneeCommentRequest req, CancellationToken ct)
    {
        // Fetch median price data
        var purchaseOrder = await this.FetchJorPor006(req, ct);

        if (purchaseOrder is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลจพ.006");
        }

        // Validate that the assignee is status is not Assigned
        if (purchaseOrder.Status != PurchaseOrderStatus.WaitingComment)
        {
            return TypedResults.BadRequest("ผู้รับผิดชอบจพ.006นี้ไม่ได้อยู่ในสถานะที่สามารถเพิ่มความคิดเห็นได้");
        }

        // Add comment to median price
        var assignee =
            purchaseOrder.Assignees
                         .Select(DelegatorExtensions.DelegatorToAssignee)
                         .FirstOrDefault(a => a.Delegatee?.SuUserId == null
                                                ? a.UserId == req.UserId
                                                : a.Delegatee?.SuUserId == UserId.From(req.UserId));

        if (assignee is null)
        {
            return TypedResults.BadRequest("ผู้ใช้ไม่อยู่ในรายชื่อผู้รับผิดจพ.006");
        }

        if (string.IsNullOrWhiteSpace(req.Remark))
        {
            return TypedResults.BadRequest("กรุณาระบุความคิดเห็น");
        }

        purchaseOrder.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Comment,
            $"เจ้าหน้าที่พัสดุให้ความเห็น ข้อมูลขออนุุมัติสั่งซื้อ/สั่งจ้าง(จพ.006)",
            purchaseOrder.Status.ToString(),
            req.Remark));

        this.dbContext.PJp006S.Update(purchaseOrder);

        var currentUser =
                purchaseOrder.Assignees
                       .FirstOrDefault(a => a.Id == assignee.Id);

        if (currentUser is null)
        {
            return TypedResults.BadRequest("ผู้ใช้ไม่อยู่ในรายชื่อผู้รับผิดจพ.006");
        }

        // Add comment to the assignee
        currentUser
            .SetDelegatee(assignee.DelegateeId)
            .SetRemark(req.Remark);

        await this.ReplaceDocumentJorPorComment(
            purchaseOrder.Procurement,
            purchaseOrder,
            PurchaseOrderDocumentType.Jp006,
            req.UserId,
            ct);

        // Save changes to the database
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private async ValueTask<PPurchaseOrder?> FetchJorPor006(AssigneeCommentRequest req, CancellationToken ct)
    {
        return await this.dbContext.PJp006S
                         .Include(c => c.Procurement)
                         .Include(mp => mp.Assignees)
                         .Include(mp => mp.DocumentHistories)
                         .FirstOrDefaultAsync(
                             mp =>
                                 mp.ProcurementId == ProcurementId.From(req.ProcurementId) &&
                                 mp.Id == PurchaseOrderId.From(req.Jp006Id),
                             ct);
    }
}