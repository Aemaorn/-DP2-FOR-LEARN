namespace GHB.DP2.Application.Features.Procurement.Jp006;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.Jp006.Abstract;
using GHB.DP2.Application.Features.Procurement.Jp006.Dto;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PPurchaseOrder;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record RecallToCommentRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    PurchaseOrderId Jp006Id,
    ProcurementId ProcurementId,
    IEnumerable<UpdateJp006Entrepreneur> Entrepreneurs,
    IEnumerable<Jp006AcceptorInfo>? Acceptors,
    IEnumerable<Jp006AssigneeInfo>? Assignees);

public class RecallToCommentEndpoint : Jp006EndpointBase<RecallToCommentRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public RecallToCommentEndpoint(
        ILogger<RecallToCommentEndpoint> logger,
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
        this.Put("procurement/{ProcurementId:guid}/jp006/{Jp006Id:guid}/recall-to-comment");
        this.Description(b => b
                              .WithTags(nameof(Jp006))
                              .WithName("RecallToCommentJp006")
                              .Produces<Ok>()
                              .ProducesProblem(StatusCodes.Status400BadRequest)
                              .ProducesProblem(StatusCodes.Status404NotFound)
                              .ProducesProblem(StatusCodes.Status500InternalServerError));
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(RecallToCommentRequest req, CancellationToken ct)
    {
        // 1. Validate procurement exists
        var procurementExisting = await this.ValidateProcurementAsync(req.ProcurementId.Value, ct);

        // 2. Load jp006 with all includes
        var jp006 = await this.GetByIdAsync(req.ProcurementId, req.Jp006Id, ct);

        if (jp006 is null)
        {
            return TypedResults.NotFound($"ไม่พบรายการจัดซื้อจัดจ้างที่มีรหัส {req.Jp006Id}");
        }

        // 3. Validate status is WaitingApproval
        if (jp006.Status != PurchaseOrderStatus.WaitingApproval)
        {
            return TypedResults.BadRequest($"สถานะปัจจุบันไม่ใช่ WaitingApproval ไม่สามารถเรียกคืนได้");
        }

        // 4. Update form data
        await UpdateJp006Endpoint.UpsertEntrepreneursAsync(jp006, req.Entrepreneurs);

        if (req.Acceptors is not null)
        {
            await this.UpsertAcceptors(jp006, req.Acceptors, procurementExisting.DepartmentId, UserId.From(req.UserId));
        }

        if (req.Assignees is not null)
        {
            await this.UpsertAssignee(jp006, req.Assignees, CancellationToken.None, UserId.From(req.UserId));
        }

        // 5. Clear assignee comments from previous round
        foreach (var assignee in jp006.Assignees.Where(a => a.Type is AssigneeType.Assignee or AssigneeType.Director))
        {
            assignee.ResetAction();

            if (assignee.Type == AssigneeType.Assignee)
            {
                assignee.Pending();
            }

            if (assignee.Type == AssigneeType.Director)
            {
                assignee.Draft();
            }
        }

        // 6. Set status: WaitingApproval → WaitingComment
        jp006.SetAssigned();

        jp006.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Update,
            $"เรียกคืนสั่งซื้อ/สั่งจ้าง(จพ.006) กลับไปรอความเห็น",
            jp006.Status.ToString()));

        await this.RestoreJorPorCommentDocument(jp006.Procurement, jp006, PurchaseOrderDocumentType.Jp006, ct);

        // 9. Save and send notifications
        await this.dbContext.SaveChangesAsync(ct);

        _ = SendNotificationAssigneeAsync(jp006, CancellationToken.None);

        return TypedResults.Ok();
    }

    private static async Task SendNotificationAssigneeAsync(PPurchaseOrder jp06, CancellationToken ct)
    {
        foreach (var targetUserId in jp06.Assignees.SelectMany(a => a.GetAssigneeNotificationTargets()))
        {
            await Notification
                  .Crate(
                      targetUserId,
                      NotificationConstant.Comment.Title,
                      string.Format(NotificationConstant.Comment.Message, ProgramConstant.PreProcurementJorPor06.Name, jp06.PurchaseOrderNumber),
                      NotificationProgram.Procurement)
                  .SetReferenceId(jp06.Id.Value)
                  .SetLinkUrl(
                      string.Format(ProgramConstant.Procurement.Url, jp06.Procurement.Id),
                      "ดูรายละเอียด")
                  .PublishAsync(ct);
        }
    }
}